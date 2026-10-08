using System.Text.Json;
using System.Text.Json.Nodes;
using System.Security.Cryptography;
using System.Text;
using XMLDocNormalizer.Checks.Infrastructure.Exception.Flow.Canonical;
using XMLDocNormalizer.HistoricalWorker;

namespace XMLDocNormalizer.Execution.Historical
{
    /// <summary>Validates a complete protocol envelope before importing the existing canonical result.</summary>
    internal static class HistoricalWorkerResponseValidation
    {
        internal static HistoricalWorkerCallResult Validate(string stdout, string stderr, int exitCode,
            WorkerAnalysisInput input, int? processId = null)
        {
            HistoricalWorkerCallResult Fail(HistoricalWorkerClientFailureCode code, string message, WorkerFailure? failure = null)
                => new(null, null, new(code, message, failure), stderr, processId);

            if (string.IsNullOrWhiteSpace(stdout) && exitCode != 0)
            {
                return Fail(HistoricalWorkerClientFailureCode.WorkerCrash, "Worker exited without a response.");
            }

            try
            {
                using JsonDocument document = JsonDocument.Parse(stdout, new JsonDocumentOptions { MaxDepth = 64 });
                JsonElement root = document.RootElement;
                RejectDuplicateProperties(root);
                if (root.ValueKind != JsonValueKind.Object
                    || !root.TryGetProperty("protocolVersion", out JsonElement version) || !version.TryGetInt32(out int number))
                {
                    return Fail(HistoricalWorkerClientFailureCode.MalformedResponse, "Response protocol version is missing.");
                }
                if (number != WorkerProtocol.Version)
                {
                    return Fail(HistoricalWorkerClientFailureCode.ProtocolMismatch, "Unsupported response protocol version.");
                }

                WorkerResponse? response = JsonSerializer.Deserialize<WorkerResponse>(stdout, WorkerProtocol.JsonOptions);
                if (response == null || !JsonNode.DeepEquals(JsonNode.Parse(stdout),
                    JsonNode.Parse(JsonSerializer.Serialize(response, WorkerProtocol.JsonOptions))))
                {
                    return Fail(HistoricalWorkerClientFailureCode.MalformedResponse, "Response is incomplete or not canonical protocol data.");
                }

                if (!response.Success)
                {
                    if (response.Result != null || response.Provenance != null || response.Failure == null || string.IsNullOrWhiteSpace(response.Failure.Message)
                        || response.Failure.Details == null || response.Failure.Details.Any(detail => detail == null)
                        || (response.Operation != "analyze" && response.Operation != "invalid"))
                    {
                        return Fail(HistoricalWorkerClientFailureCode.MalformedResponse, "Invalid failure envelope.");
                    }
                    if (exitCode != 1)
                    {
                        return Fail(HistoricalWorkerClientFailureCode.NonZeroExit, "Failure response has an inconsistent exit code.");
                    }
                    if (response.Identity != null && !ValidIdentity(response.Identity))
                    {
                        return Fail(HistoricalWorkerClientFailureCode.IdentityMismatch, "Failure runtime identity does not match.");
                    }
                    return Fail(response.Failure.Code == WorkerFailureCode.UnsupportedProtocolVersion
                        ? HistoricalWorkerClientFailureCode.ProtocolMismatch : HistoricalWorkerClientFailureCode.StructuredFailure,
                        response.Failure.Message, response.Failure);
                }
                if (exitCode != 0)
                {
                    return Fail(HistoricalWorkerClientFailureCode.NonZeroExit, "Worker success has a non-zero exit code.");
                }
                if (response.Operation != "analyze" || response.Failure != null || response.Result == null)
                {
                    return Fail(HistoricalWorkerClientFailureCode.MalformedResponse, "Missing analysis result or inconsistent response.");
                }
                if (!ValidIdentity(response.Identity))
                {
                    return Fail(HistoricalWorkerClientFailureCode.IdentityMismatch, "Worker runtime identity does not match the exact historical engine.");
                }
                if (response.Result.Uncertainties.Length != 0 || response.Result.Entries.Any(entry => entry.PathsTruncated))
                {
                    return Fail(HistoricalWorkerClientFailureCode.IncompleteResult, "Historical analysis is incomplete.");
                }
                WorkerAnalysisProvenance expected = new(Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(input.Source!))),
                    input.TypeMetadataName!, input.MethodName!, "worker-input.cs", "solution-transitive", "net8-runtime-bounded-v1", "12", "enable");
                if (response.Provenance != expected || !ValidCanonicalResult(response.Result, input.Source!))
                {
                    return Fail(HistoricalWorkerClientFailureCode.MalformedResponse, "Canonical identity, location or input provenance is invalid.");
                }
                return new(response.Result, response.Identity, null, stderr, processId, response.Provenance);
            }
            catch (Exception exception) when (exception is JsonException or ArgumentException or InvalidOperationException or NotSupportedException or NullReferenceException)
            {
                return Fail(exitCode == 0 ? HistoricalWorkerClientFailureCode.MalformedResponse
                    : HistoricalWorkerClientFailureCode.NonZeroExit, "Worker did not return valid protocol data.");
            }
        }

        private static bool ValidCanonicalResult(CanonicalExceptionFlowAnalysisResult result, string source)
        {
            string[] lines = source.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n').Split('\n');
            return result.Entries.All(entry => entry.EvidenceKind == CanonicalExceptionFlowEvidenceKind.Proven
                && entry.ExceptionType.Kind == CanonicalTypeIdentityKind.Named
                && !string.IsNullOrWhiteSpace(entry.ExceptionType.MetadataName)
                && entry.ExceptionType.Assembly != null && entry.ExceptionType.Module != null
                && entry.ExceptionType.Module.Assembly.Equals(entry.ExceptionType.Assembly)
                && entry.Paths.Length != 0 && entry.Paths.All(path => path.Steps.Length != 0
                    && path.Steps.All(step => step.FilePath == "worker-input.cs" && !string.IsNullOrWhiteSpace(step.SymbolName)
                        && step.Line is int line && line > 0 && line <= lines.Length
                        && step.Column is int column && column > 0 && column <= lines[line - 1].Length + 1)));
        }

        private static bool ValidIdentity(WorkerIdentity? identity)
        {
            const string version = "5.0.0-2.25451.107+2db1f5ee2bdda2e8d873769325fabede32e420e0";
            if (identity == null || identity.ProtocolVersion != WorkerProtocol.Version || identity.WorkerVersion != "1.0"
                || identity.ReferenceProfile != "net8-runtime-bounded-v1" || identity.RuntimeAwaitInformationAvailable
                || !ValidAssembly(identity.Worker, "XMLDocNormalizer.HistoricalWorker")
                || !ValidAssembly(identity.Analyzer, "XMLDocNormalizer.ExceptionFlow.Historical")
                || identity.LoadedRoslyn == null || identity.LoadedRoslyn.Length != 2)
            {
                return false;
            }
            return Match("Microsoft.CodeAnalysis", "dc7738cc-6dca-4d34-9c44-29b53a7caa93",
                "660C3D626C4B8F4CF8C231FBEF0FB6B4DB4FFFCC89EF5B31AAECA1CF4D7F66A1")
                && Match("Microsoft.CodeAnalysis.CSharp", "0f9c1dcf-4eb1-47f8-81b2-733db5887be7",
                    "B0EC1DDCA4C97DCF15845FF4BCEB5499C3989025197D5D65E04310E29C09217D");

            bool Match(string name, string mvid, string sha)
                => identity.LoadedRoslyn.Count(item => item != null && item.Name == name && item.AssemblyVersion == "5.0.0.0"
                    && item.InformationalVersion == version && item.Mvid == mvid && item.Sha256 == sha) == 1;
        }

        private static bool ValidAssembly(WorkerAssemblyIdentity? identity, string name)
            => identity != null && identity.Name == name && identity.AssemblyVersion == "1.0.0.0"
                && !string.IsNullOrWhiteSpace(identity.InformationalVersion) && Guid.TryParse(identity.Mvid, out Guid mvid)
                && mvid != Guid.Empty && identity.Sha256?.Length == 64 && identity.Sha256.All(Uri.IsHexDigit);

        private static void RejectDuplicateProperties(JsonElement element)
        {
            if (element.ValueKind == JsonValueKind.Object)
            {
                HashSet<string> names = new(StringComparer.Ordinal);
                foreach (JsonProperty property in element.EnumerateObject())
                {
                    if (!names.Add(property.Name)) { throw new JsonException("Duplicate protocol field."); }
                    RejectDuplicateProperties(property.Value);
                }
            }
            else if (element.ValueKind == JsonValueKind.Array)
            {
                foreach (JsonElement item in element.EnumerateArray()) { RejectDuplicateProperties(item); }
            }
        }
    }
}
