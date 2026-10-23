using System.Text.Json;
using System.Text.Json.Nodes;
using System.Security.Cryptography;
using System.Text;
using System.Runtime.CompilerServices;
using XMLDocNormalizer.Execution.Analysis;
using XMLDocNormalizer.Checks.Infrastructure.Exception.Flow.Canonical;
using XMLDocNormalizer.HistoricalWorker;

namespace XMLDocNormalizer.Execution.Historical
{
    /// <summary>Validates a complete protocol envelope before importing the existing canonical result.</summary>
    internal static class HistoricalWorkerResponseValidation
    {
        private static readonly ConditionalWeakTable<HistoricalWorkerCallResult, DeclaredPolicyFacts> policyReceipts = new();
        private static readonly ConditionalWeakTable<HistoricalWorkerCallResult, HistoricalFindingFacts> findingReceipts = new();
        internal static bool TryGetFindingFacts(HistoricalWorkerCallResult? call, out HistoricalFindingFacts? facts)
        {
            facts = null;
            return call?.Success == true && findingReceipts.TryGetValue(call, out facts);
        }
        internal static bool TryGetDeclaredPolicyFacts(HistoricalWorkerCallResult? call, out DeclaredPolicyFacts? facts)
        {
            facts = null;
            return call?.Success == true && policyReceipts.TryGetValue(call, out facts);
        }
        internal static HistoricalWorkerCallResult Validate(string stdout, string stderr, int exitCode,
            WorkerAnalysisInput input, int? processId = null, HistoricalWorkerClient.DocumentationExecution? documentationExecution = null)
        {
            HistoricalWorkerCallResult Fail(HistoricalWorkerClientFailureCode code, string message, WorkerFailure? failure = null)
                => new(null, null, new(code, message, failure), stderr, processId);

            if (string.IsNullOrWhiteSpace(stdout) && exitCode != 0)
            {
                return Fail(HistoricalWorkerClientFailureCode.WorkerCrash, "Worker exited without a response.");
            }

            try
            {
                bool executedDocumentation = HistoricalWorkerClient.TryConsumeDocumentationExecution(documentationExecution,
                    input, stdout, stderr, exitCode, processId, out var matchesExecutedWorker);
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
                    if (response.Result != null || response.Provenance != null || response.DeclaredPolicy != null || response.DeclaredDocumentation != null || response.FindingDirect != null || response.Failure == null || string.IsNullOrWhiteSpace(response.Failure.Message)
                        || response.Failure.Details == null || response.Failure.Details.Any(detail => detail == null)
                        || (response.Operation != "analyze" && response.Operation != "invalid"))
                    {
                        return Fail(HistoricalWorkerClientFailureCode.MalformedResponse, "Invalid failure envelope.");
                    }
                    if (exitCode != 1)
                    {
                        return Fail(HistoricalWorkerClientFailureCode.NonZeroExit, "Failure response has an inconsistent exit code.");
                    }
                    if (response.Identity != null && !ValidIdentity(response.Identity, input))
                    {
                        return Fail(HistoricalWorkerClientFailureCode.IdentityMismatch, "Failure runtime identity does not match.");
                    }
                    // Preserve only the actual identity that passed the checks above; never admit a result or receipt.
                    return new(null, response.Identity, new(response.Failure.Code == WorkerFailureCode.UnsupportedProtocolVersion
                        ? HistoricalWorkerClientFailureCode.ProtocolMismatch : HistoricalWorkerClientFailureCode.StructuredFailure,
                        response.Failure.Message, response.Failure), stderr, processId);
                }
                if (exitCode != 0)
                {
                    return Fail(HistoricalWorkerClientFailureCode.NonZeroExit, "Worker success has a non-zero exit code.");
                }
                if (response.Operation != "analyze" || response.Failure != null || response.Result == null)
                {
                    return Fail(HistoricalWorkerClientFailureCode.MalformedResponse, "Missing analysis result or inconsistent response.");
                }
                if (!ValidIdentity(response.Identity, input))
                {
                    return Fail(HistoricalWorkerClientFailureCode.IdentityMismatch, "Worker runtime identity does not match the exact historical engine.");
                }
                if (response.Result.Uncertainties.Length != 0 || response.Result.Entries.Any(entry => entry.PathsTruncated))
                {
                    return Fail(HistoricalWorkerClientFailureCode.IncompleteResult, "Historical analysis is incomplete.");
                }
                WorkerAnalysisProvenance expected = WorkerProtocol.Provenance(input);
                if (response.Provenance != expected || !ValidCanonicalResult(response.Result, input)
                    || !ValidReferenceIdentities(response.Result, input.Context) || !ValidDeclaredPolicy(response.DeclaredPolicy, response.Result, input))
                {
                    return Fail(HistoricalWorkerClientFailureCode.MalformedResponse, "Canonical identity, location or input provenance is invalid.");
                }
                DeclaredFindingDocumentationFacts? documentation = null;
                if (input.Context?.Finding is { } finding)
                {
                    var direct = response.FindingDirect;
                    if (!WorkerInputValidation.IsSupported(input) || direct == null || response.DeclaredPolicy != null
                        || direct.Uncertainties.Length != 0 || direct.Entries.Any(entry => entry.PathsTruncated)
                        || !ValidCanonicalResult(direct, input) || !ValidReferenceIdentities(direct, input.Context)
                        || direct.Entries.SelectMany(entry => entry.Paths).SelectMany(path => path.Steps).Any(step => step.FilePath != input.LogicalPath)
                        || finding.Execution == WorkerDeclaredExecution.DirectEvidence && !direct.Equals(response.Result)
                        || !HistoricalWorkerDeclaredDocumentationProjection.TryValidateFinding(input, response.DeclaredDocumentation, direct, response.Result, out documentation)
                        || !executedDocumentation || matchesExecutedWorker?.Invoke(response.Identity!.Worker) != true)
                    { return Fail(HistoricalWorkerClientFailureCode.MalformedResponse, "Independent Finding facts require complete original mode/root/ownership/type evidence and exact B21 execution."); }
                    var admitted = new HistoricalWorkerCallResult(response.Result, response.Identity, null, stderr, processId, response.Provenance,
                        DeclaredDocumentation: response.DeclaredDocumentation);
                    findingReceipts.Add(admitted, new(direct, response.Result, documentation!, finding.Documentation!.Mode!.Value, finding.Execution,
                        JsonSerializer.Serialize(input.Context.Scope, WorkerProtocol.JsonOptions), documentationExecution!));
                    return admitted;
                }
                if (response.FindingDirect != null) { return Fail(HistoricalWorkerClientFailureCode.MalformedResponse, "Unrequested independent Finding evidence."); }
                if (response.DeclaredPolicy == null && response.DeclaredDocumentation != null)
                { return Fail(HistoricalWorkerClientFailureCode.MalformedResponse, "Documentation facts require validated B17 evidence."); }
                if (response.DeclaredPolicy != null && !HistoricalWorkerDeclaredDocumentationProjection.TryValidate(input,
                    response.DeclaredDocumentation, response.DeclaredPolicy, response.Result, out documentation))
                { return Fail(HistoricalWorkerClientFailureCode.MalformedResponse, "Documentation identity, completeness, ownership or original context is invalid."); }
                if (documentation != null && (!executedDocumentation || matchesExecutedWorker?.Invoke(response.Identity!.Worker) != true))
                { return Fail(HistoricalWorkerClientFailureCode.MalformedResponse, "Documentation binding requires its exact single-use completed Worker execution attestation."); }
                var call = new HistoricalWorkerCallResult(response.Result, response.Identity, null, stderr, processId, response.Provenance, response.DeclaredPolicy, response.DeclaredDocumentation);
                if (response.DeclaredPolicy != null)
                {
                    policyReceipts.Add(call, new(response.DeclaredPolicy, response.Result, documentation,
                    documentation == null ? null : documentationExecution, input.Context?.DeclaredExecution));
                }
                return call;
            }
            catch (Exception exception) when (exception is JsonException or ArgumentException or InvalidOperationException or NotSupportedException or NullReferenceException)
            {
                return Fail(exitCode == 0 ? HistoricalWorkerClientFailureCode.MalformedResponse
                    : HistoricalWorkerClientFailureCode.NonZeroExit, "Worker did not return valid protocol data.");
            }
        }

        private static bool ValidReferenceIdentities(CanonicalExceptionFlowAnalysisResult result, WorkerCompilationContext? context)
            => result.Entries.All(entry => ValidReferenceType(entry.ExceptionType, context));

        private static bool ValidReferenceType(CanonicalTypeIdentity type, WorkerCompilationContext? context)
        {
            if (context?.ReferenceProfile != WorkerProtocol.ExactReferenceProfile) { return true; }
            var contexts = context.Solution?.Projects!.Select(project => project.Input!.Context!).ToArray() ?? [context];
            return type.Module?.Ordinal == 0
                && type.Module.ModuleVersionId == null
                && contexts.Any(project => type.Assembly!.Equals(project.TargetIdentity)
                    ? type.Module.Name == project.TargetModuleName
                    : project.References!.Any(reference => type.Assembly.Equals(reference.Identity)
                        && type.Module.Name == reference.ModuleName
                        // Existing canonical adapter intentionally emits no MVID, even for PE-backed symbols.
                        // Exact PE MVID is checked in Worker binding and covered by ReferenceWorldSha256 instead.
                        && type.Module.ModuleVersionId == null));
        }

        /// <summary>Structural/input-binding validation only; the Worker owns symbol facts, Main owns eligibility.</summary>
        private static bool ValidDeclaredPolicy(WorkerDeclaredPolicyEvidence? evidence, CanonicalExceptionFlowAnalysisResult summary, WorkerAnalysisInput input)
        {
            if (input.Context?.DeclaredPolicy == null) { return evidence == null; }
            if (!WorkerInputValidation.IsSupported(input) || !WorkerDeclaredPolicyContract.HasSupportedRequest(input) || evidence == null
                || evidence.Profile != WorkerDeclaredPolicyContract.Profile || evidence.Mode != input.Context.DeclaredPolicy.Mode
                || evidence.ReportingSourcePaths == null || input.Context.Scope?.OrderedSourcePaths == null
                || !evidence.ReportingSourcePaths.SequenceEqual(input.Context.Scope.OrderedSourcePaths, StringComparer.Ordinal)
                || evidence.Declarations == null || evidence.DeclarationCount != evidence.Declarations.Length
                || evidence.Declarations.Length > WorkerDeclaredPolicyContract.MaximumDeclarations || evidence.DirectResult == null
                || evidence.DirectResult.Uncertainties.Length != 0 || evidence.DirectResult.Entries.Any(entry => entry.PathsTruncated)
                || !ValidCanonicalResult(evidence.DirectResult, input) || !ValidReferenceIdentities(evidence.DirectResult, input.Context)
                || evidence.DirectResult.Entries.SelectMany(entry => entry.Paths).SelectMany(path => path.Steps).Any(step => step.FilePath != input.LogicalPath))
            { return false; }
            if (input.Context.DeclaredExecution == WorkerDeclaredExecution.DirectEvidence && !summary.Equals(evidence.DirectResult)) { return false; }
            var declarations = new HashSet<CanonicalTypeIdentity>();
            var keys = new List<string>();
            foreach (var declaration in evidence.Declarations)
            {
                if (declaration?.Type is not { } type || !ValidPolicyType(type) || !ValidReferenceType(type, input.Context)
                    || !type.Assembly!.Equals(input.Context.TargetIdentity) || type.Module!.Name != input.Context.TargetModuleName
                    || !declarations.Add(type) || declaration.DeclaringSourcePaths == null || declaration.DeclaringSourcePaths.Length == 0
                    || !declaration.DeclaringSourcePaths.SequenceEqual(evidence.ReportingSourcePaths.Where(path => declaration.DeclaringSourcePaths.Contains(path, StringComparer.Ordinal)), StringComparer.Ordinal))
                { return false; }
                keys.Add(JsonSerializer.Serialize(type, WorkerProtocol.JsonOptions));
            }
            if (!keys.SequenceEqual(keys.OrderBy(key => key, StringComparer.Ordinal), StringComparer.Ordinal)
                || !declarations.SetEquals(input.Context.DeclaredPolicy.ExpectedDeclarations!)) { return false; }
            foreach (var entry in summary.Entries.Concat(evidence.DirectResult.Entries))
            {
                if (!ValidPolicyType(entry.ExceptionType) || entry.ExceptionType.Assembly!.Equals(input.Context.TargetIdentity) && !declarations.Contains(entry.ExceptionType))
                { return false; }
            }
            return true;
        }

        private static bool ValidPolicyType(CanonicalTypeIdentity type, bool exception = true, int depth = 0)
            => depth < 16 && type.Kind == CanonicalTypeIdentityKind.Named && Enum.IsDefined(type.NamedTypeKind)
                && type.NamedTypeKind != CanonicalNamedTypeKind.Unknown && (!exception || type.NamedTypeKind == CanonicalNamedTypeKind.Class)
                && type.Assembly != null && type.Module != null && type.Module.Assembly.Equals(type.Assembly)
                && (type.ContainingType != null ? type.NamespaceName == null
                    : type.NamespaceName != null && type.NamespaceName.Length <= 4096 && !type.NamespaceName.Any(char.IsControl))
                && !string.IsNullOrWhiteSpace(type.MetadataName) && type.MetadataName.Length <= 256 && !type.MetadataName.Any(char.IsControl)
                && !type.MetadataName.Contains('`') && type.TypeArguments.Length == 0 && !type.IsUnboundGenericType && !type.IsNativeIntegerType
                && type.ElementType == null && type.ArrayRank == 0 && type.TypeParameterScope == null && type.TypeParameterOrdinal == null
                && type.TypeParameterOwnerAssembly == null && type.TypeParameterOwnerModule == null && type.TypeParameterOwnerDeclarationId == null
                && type.FunctionPointerCallingConvention == null && type.FunctionPointerReturn == null && type.FunctionPointerParameters.Length == 0
                && (type.ContainingType == null || type.ContainingType.Assembly!.Equals(type.Assembly)
                    && type.ContainingType.Module!.Equals(type.Module) && ValidPolicyType(type.ContainingType, false, depth + 1));

        private static bool ValidCanonicalResult(CanonicalExceptionFlowAnalysisResult result, WorkerAnalysisInput input)
        {
            var sources = new Dictionary<string, string[]>(StringComparer.Ordinal);
            var projects = input.Context?.Solution?.Projects!.Select(project => project.Input!).ToArray() ?? [input];
            foreach (var project in projects)
            {
                sources.Add(WorkerInputValidation.GetLogicalPath(project), Lines(project.Source!));
                foreach (var source in project.Context?.AdditionalSources ?? []) { sources.Add(source.LogicalPath, Lines(source.Text)); }
            }
            return result.Entries.All(entry => entry.EvidenceKind == CanonicalExceptionFlowEvidenceKind.Proven
                && entry.ExceptionType.Kind == CanonicalTypeIdentityKind.Named
                && !string.IsNullOrWhiteSpace(entry.ExceptionType.MetadataName)
                && entry.ExceptionType.Assembly != null && entry.ExceptionType.Module != null
                && entry.ExceptionType.Module.Assembly.Equals(entry.ExceptionType.Assembly)
                && entry.Paths.Length != 0 && entry.Paths.All(path => path.Steps.Length != 0
                    && path.Steps.All(step => step.FilePath != null && sources.TryGetValue(step.FilePath, out var lines)
                        && !string.IsNullOrWhiteSpace(step.SymbolName)
                        && step.Line is int line && line > 0 && line <= lines.Length
                        && step.Column is int column && column > 0 && column <= lines[line - 1].Length + 1)));

            static string[] Lines(string text) => text.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n').Split('\n');
        }

        private static bool ValidIdentity(WorkerIdentity? identity, WorkerAnalysisInput input)
        {
            const string version = "5.0.0-2.25451.107+2db1f5ee2bdda2e8d873769325fabede32e420e0";
            if (identity == null || identity.ProtocolVersion != WorkerProtocol.Version || identity.WorkerVersion != "1.0"
                || identity.ReferenceProfile != (input.Context?.ReferenceProfile ?? "net8-runtime-bounded-v1") || identity.RuntimeAwaitInformationAvailable
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
