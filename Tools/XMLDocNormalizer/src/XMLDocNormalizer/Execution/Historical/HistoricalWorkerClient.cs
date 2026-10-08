using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using XMLDocNormalizer.HistoricalWorker;

namespace XMLDocNormalizer.Execution.Historical
{
    /// <summary>Explicit Main-owned one-request Historical process client, never an Analyzer or routing policy.</summary>
    internal sealed class HistoricalWorkerClient
    {
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
            if (request.Length > WorkerProtocol.MaximumRequestCharacters)
            {
                return Fail(HistoricalWorkerClientFailureCode.UnsupportedInput, "Serialized request exceeds the protocol budget.");
            }
            if (cancellationToken.IsCancellationRequested)
            {
                return Fail(HistoricalWorkerClientFailureCode.Cancelled, "Historical request was cancelled before process start.");
            }

            ProcessStartInfo start = new(dotnetHost)
            {
                UseShellExecute = false, RedirectStandardInput = true, RedirectStandardOutput = true,
                RedirectStandardError = true, CreateNoWindow = true,
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
                return HistoricalWorkerResponseValidation.Validate(await stdout, await stderr, process.ExitCode, input, pid);
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
