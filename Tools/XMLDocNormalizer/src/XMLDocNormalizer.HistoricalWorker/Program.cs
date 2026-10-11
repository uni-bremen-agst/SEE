using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace XMLDocNormalizer.HistoricalWorker
{
    /// <summary>One stdin JSON request, one stdout JSON response, then process exit.</summary>
    internal static class Program
    {
        /// <summary>Separates protocol failures from stderr diagnostics and never runs input code.</summary>
        private static async Task<int> Main()
        {
            Console.InputEncoding = new UTF8Encoding(false, true);
            Console.OutputEncoding = new UTF8Encoding(false);
            WorkerResponse response;
            string input = "";
            try
            {
                input = await ReadBoundedRequest();
                using var document = JsonDocument.Parse(input, new JsonDocumentOptions { MaxDepth = 64 });
                WorkerProtocol.RejectDuplicateProperties(document.RootElement);
                WorkerRequest? request = JsonSerializer.Deserialize<WorkerRequest>(input, WorkerProtocol.JsonOptions);
                if (request?.Payload?.Context?.ReferenceProfile == WorkerProtocol.ExactReferenceProfile
                    && !JsonNode.DeepEquals(JsonNode.Parse(input), JsonNode.Parse(JsonSerializer.Serialize(request, WorkerProtocol.JsonOptions))))
                { throw new JsonException("Exact reference input is not canonical protocol data."); }
                if (input.Length > (request?.Payload == null ? WorkerProtocol.MaximumRequestCharacters : WorkerProtocol.RequestLimit(request.Payload)))
                { throw new InvalidDataException("Request too large for profile."); }
                response = Execute(request);
            }
            catch (JsonException)
            {
                response = input.Length > WorkerProtocol.MaximumRequestCharacters
                    ? Failure(WorkerFailureCode.InvalidRequest, "Request exceeds the legacy character budget.")
                    : Failure(WorkerFailureCode.MalformedInput, "Request is not one valid protocol JSON object.");
            }
            catch (DecoderFallbackException)
            {
                response = Failure(WorkerFailureCode.MalformedInput, "Request is not valid UTF-8.");
            }
            catch (InvalidDataException)
            {
                response = Failure(WorkerFailureCode.InvalidRequest, "Request exceeds the character budget.");
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine("Historical Worker failure: " + exception);
                response = Failure(WorkerFailureCode.UnexpectedWorkerFailure, "Worker could not complete the request.");
            }

            string output;
            try
            {
                output = JsonSerializer.Serialize(response, WorkerProtocol.JsonOptions);
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine("Historical response failure: " + exception);
                response = Failure(WorkerFailureCode.UnexpectedWorkerFailure, "Worker could not serialize the response.");
                output = JsonSerializer.Serialize(response, WorkerProtocol.JsonOptions);
            }

            Console.Out.WriteLine(output);
            return response.Success ? 0 : 1;
        }

        /// <summary>Validates the bounded transport command before invoking the real engine.</summary>
        private static WorkerResponse Execute(WorkerRequest? request)
        {
            if (request?.ProtocolVersion == null || string.IsNullOrWhiteSpace(request.Operation))
            {
                return Failure(WorkerFailureCode.InvalidRequest, "Protocol version and operation are required.");
            }

            if (request.ProtocolVersion != WorkerProtocol.Version)
            {
                return Failure(WorkerFailureCode.UnsupportedProtocolVersion, "Unsupported protocol version.");
            }

            if (request.Operation != "identity" && request.Operation != "analyze")
            {
                return Failure(WorkerFailureCode.InvalidRequest, "Unsupported operation.");
            }

            if (request.Operation == "identity" && request.Payload != null)
            {
                return Failure(WorkerFailureCode.InvalidRequest, "Identity does not accept an analysis payload.");
            }

            if (request.Operation == "analyze" && !WorkerInputValidation.IsSupported(request.Payload))
            {
                return Failure(WorkerFailureCode.InvalidRequest, "Analyze requires bounded source and a type/method selector.");
            }

            WorkerIdentity identity = WorkerRuntimeIdentity.ReadAndValidate();
            if (request.Operation == "analyze")
            { identity = identity with { ReferenceProfile = request.Payload!.Context?.ReferenceProfile ?? "net8-runtime-bounded-v1" }; }
            return request.Operation == "identity"
                ? new WorkerResponse(WorkerProtocol.Version, "identity", true, identity)
                : HistoricalAnalysis.Analyze(request.Payload!, identity);
        }

        /// <summary>Limits input before parsing; EOF is the one-request framing boundary.</summary>
        private static async Task<string> ReadBoundedRequest()
        {
            StringBuilder input = new();
            char[] buffer = new char[4096];
            int count;
            while ((count = await Console.In.ReadAsync(buffer)) != 0)
            {
                if (input.Length + count > WorkerProtocol.MaximumSolutionRequestCharacters)
                {
                    throw new InvalidDataException("Request too large.");
                }

                input.Append(buffer, 0, count);
            }

            return input.ToString();
        }

        /// <summary>Creates a transport failure without any result payload.</summary>
        private static WorkerResponse Failure(WorkerFailureCode code, string message)
            => new(WorkerProtocol.Version, "invalid", false, Failure: new(code, message, []));
    }
}
