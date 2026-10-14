using System.ComponentModel;
using System.Diagnostics;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using XMLDocNormalizer.HistoricalWorker;

namespace XMLDocNormalizer.Execution.Historical
{
    /// <summary>Explicit Main-owned one-request Historical process client, never an Analyzer or routing policy.</summary>
    internal sealed class HistoricalWorkerClient
    {
        /// <summary>Opaque Main-local capability. Construction alone conveys no authority; never IPC data.</summary>
        internal sealed class DocumentationExecution { }
        private sealed record WorkerImage(string Name, string Version, string Mvid, string Sha256);
        private sealed record CompletedDocumentationExchange(WorkerAnalysisInput Input, string Request, string Response,
            string Diagnostics, int ExitCode, int ProcessId, WorkerImage Image);
        private static readonly ConditionalWeakTable<DocumentationExecution, CompletedDocumentationExchange> documentationExecutions = new();
        internal const int MaximumResponseCharacters = 1048576;
        internal const int MaximumDiagnosticCharacters = 16384;
        private readonly string dotnetHost;
        private readonly string workerAssemblyPath;
        private readonly TimeSpan deadline;

        internal HistoricalWorkerClient(string dotnetHost, string workerAssemblyPath, TimeSpan deadline)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(dotnetHost);
            ArgumentException.ThrowIfNullOrWhiteSpace(workerAssemblyPath);
            if (deadline <= TimeSpan.Zero || deadline > TimeSpan.FromMinutes(5))
            {
                throw new ArgumentOutOfRangeException(nameof(deadline));
            }
            this.dotnetHost = dotnetHost;
            this.workerAssemblyPath = Path.GetFullPath(workerAssemblyPath);
            this.deadline = deadline;
        }

        /// <summary>Imports only a complete, identity-validated canonical result; failure is never empty success.</summary>
        internal async Task<HistoricalWorkerCallResult> AnalyzeAsync(WorkerAnalysisInput input, CancellationToken cancellationToken = default)
        {
            HistoricalWorkerCallResult Fail(HistoricalWorkerClientFailureCode code, string message, int? pid = null)
                => new(null, null, new(code, message), ProcessId: pid);
            if (!WorkerInputValidation.IsSupported(input))
            {
                return Fail(HistoricalWorkerClientFailureCode.UnsupportedInput, "Input is incomplete or outside the bounded historical profile.");
            }
            string request = JsonSerializer.Serialize(new WorkerRequest(WorkerProtocol.Version, "analyze", input), WorkerProtocol.JsonOptions);
            if (request.Length > WorkerProtocol.RequestLimit(input))
            {
                return Fail(HistoricalWorkerClientFailureCode.UnsupportedInput, "Serialized request exceeds the protocol budget.");
            }
            if (cancellationToken.IsCancellationRequested)
            {
                return Fail(HistoricalWorkerClientFailureCode.Cancelled, "Historical request was cancelled before process start.");
            }

            // Only the opt-in B19 trust boundary needs an execution-image witness. Legacy transport is unchanged.
            WorkerImage? documentationImage = null;
            if (input.Context?.DeclaredPolicy?.Documentation != null)
            {
                try { documentationImage = ReadWorkerImage(); }
                catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or BadImageFormatException)
                { return Fail(HistoricalWorkerClientFailureCode.StartFailure, "Documentation Worker image is unavailable: " + exception.Message); }
            }

            ProcessStartInfo start = new(dotnetHost)
            {
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                StandardInputEncoding = new UTF8Encoding(false, true),
                StandardOutputEncoding = new UTF8Encoding(false, true),
                StandardErrorEncoding = new UTF8Encoding(false, true)
            };
            start.ArgumentList.Add(workerAssemblyPath);
            using Process process = new() { StartInfo = start };
            try
            {
                if (!process.Start()) { return Fail(HistoricalWorkerClientFailureCode.StartFailure, "Worker could not start."); }
            }
            catch (Exception exception) when (exception is Win32Exception or IOException or InvalidOperationException or UnauthorizedAccessException)
            {
                return Fail(HistoricalWorkerClientFailureCode.StartFailure, "Worker could not start: " + exception.Message);
            }

            int pid = process.Id;
            using CancellationTokenSource timeout = new(deadline);
            using CancellationTokenSource operation = CancellationTokenSource.CreateLinkedTokenSource(timeout.Token, cancellationToken);
            Task<string> stdout = ReadBoundedAsync(process.StandardOutput, MaximumResponseCharacters, operation.Token);
            Task<string> stderr = ReadBoundedAsync(process.StandardError, MaximumDiagnosticCharacters, operation.Token);
            Task exchange = ExchangeAsync();
            try
            {
                // WaitAsync covers input writing, process exit AND pipe EOF; no unbounded drain after timeout.
                List<Task> pending = [stdout, stderr, exchange];
                while (pending.Count != 0)
                {
                    Task completed = await Task.WhenAny(pending).WaitAsync(operation.Token);
                    await completed;
                    pending.Remove(completed);
                }
                string response = await stdout;
                string diagnostics = await stderr;
                DocumentationExecution? execution = null;
                if (documentationImage != null && process.ExitCode == 0)
                {
                    // This private mint is reached only after real input exchange, child exit and bounded pipe EOF.
                    // A second image read detects changes during the exchange, without loading any Historical code.
                    try
                    {
                        if (documentationImage != ReadWorkerImage())
                        { return Fail(HistoricalWorkerClientFailureCode.IdentityMismatch, "Documentation Worker image changed during execution.", pid); }
                    }
                    catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or BadImageFormatException)
                    { return Fail(HistoricalWorkerClientFailureCode.IdentityMismatch, "Documentation Worker image is no longer available.", pid); }
                    execution = new();
                    documentationExecutions.Add(execution, new(input, request, response, diagnostics, process.ExitCode, pid, documentationImage));
                }
                try { return HistoricalWorkerResponseValidation.Validate(response, diagnostics, process.ExitCode, input, pid, execution); }
                finally { if (execution != null) { documentationExecutions.Remove(execution); } }
            }
            catch (OperationCanceledException)
            {
                return Fail(cancellationToken.IsCancellationRequested ? HistoricalWorkerClientFailureCode.Cancelled
                    : HistoricalWorkerClientFailureCode.Timeout, "Historical request did not complete before cancellation/deadline.", pid);
            }
            catch (InvalidDataException)
            {
                return Fail(HistoricalWorkerClientFailureCode.OutputLimit, "Worker output exceeds the transport budget.", pid);
            }
            catch (Exception exception) when (exception is IOException or InvalidOperationException or DecoderFallbackException)
            {
                return Fail(HistoricalWorkerClientFailureCode.TransportFailure, "Historical process transport failed.", pid);
            }
            finally
            {
                operation.Cancel();
                try { if (!process.HasExited) { process.Kill(entireProcessTree: true); } }
                catch (InvalidOperationException) when (process.HasExited) { }
                await process.WaitForExitAsync();
                try { await Task.WhenAll(stdout, stderr, exchange); }
                catch (Exception exception) when (exception is OperationCanceledException or IOException or InvalidOperationException or InvalidDataException or DecoderFallbackException) { }
            }

            async Task ExchangeAsync()
            {
                try { await process.StandardInput.WriteAsync(request.AsMemory(), operation.Token); }
                catch (IOException) { /* A rejecting/crashing child can close stdin before its response is read. */ }
                finally { process.StandardInput.Close(); }
                await process.WaitForExitAsync(operation.Token);
            }
        }

        /// <summary>Consumes only an exact completed exchange branded by this client's private execution path.</summary>
        internal static bool TryConsumeDocumentationExecution(DocumentationExecution? execution, WorkerAnalysisInput input,
            string response, string diagnostics, int exitCode, int? processId, out Func<WorkerAssemblyIdentity?, bool>? matchesWorker)
        {
            matchesWorker = null;
            if (execution == null || !documentationExecutions.TryGetValue(execution, out var completed)
                || !documentationExecutions.Remove(execution)) { return false; }
            if (!ReferenceEquals(completed.Input, input) || completed.Request != JsonSerializer.Serialize(new WorkerRequest(WorkerProtocol.Version, "analyze", input), WorkerProtocol.JsonOptions)
                || completed.Response != response || completed.Diagnostics != diagnostics || completed.ExitCode != exitCode || completed.ProcessId != processId)
            { return false; }
            // Copy only the physical image identity; no caller DTO hash or mutable input survives as authority.
            WorkerImage image = completed.Image;
            matchesWorker = identity => identity != null && image.Name == "XMLDocNormalizer.HistoricalWorker"
                && identity.Name == image.Name && identity.AssemblyVersion == image.Version && identity.Mvid == image.Mvid && identity.Sha256 == image.Sha256;
            return true;
        }

        private WorkerImage ReadWorkerImage()
        {
            using FileStream stream = File.OpenRead(workerAssemblyPath);
            string hash = Convert.ToHexString(SHA256.HashData(stream));
            stream.Position = 0;
            using PEReader image = new(stream);
            MetadataReader reader = image.GetMetadataReader();
            if (!reader.IsAssembly) { throw new BadImageFormatException("Worker must be an assembly."); }
            var assembly = reader.GetAssemblyDefinition();
            // Return copied values only. No reader-backed struct escapes the owned PEReader lifetime.
            return new(reader.GetString(assembly.Name), assembly.Version.ToString(), reader.GetGuid(reader.GetModuleDefinition().Mvid).ToString("D"), hash);
        }

        private static async Task<string> ReadBoundedAsync(StreamReader reader, int limit, CancellationToken token)
        {
            StringBuilder text = new();
            char[] buffer = new char[4096];
            int count;
            while ((count = await reader.ReadAsync(buffer.AsMemory(), token)) != 0)
            {
                if (text.Length + count > limit) { throw new InvalidDataException("Worker output budget exceeded."); }
                text.Append(buffer, 0, count);
            }
            return text.ToString();
        }
    }
}
