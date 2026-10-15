using System.Diagnostics;
using System.Text.Json;
using XMLDocNormalizer.Execution.Historical;
using XMLDocNormalizer.HistoricalWorker;
using Xunit.Sdk;

namespace XMLDocNormalizerTests.Worker
{
    public sealed class HistoricalWorkerLifetimeTests
    {
        [Fact]
        public async Task LiveOriginal_CannotProduceAnExitObservation()
        {
            await WithPeer(async process =>
            {
                Assert.False(process.HasExited);
                Assert.Throws<InvalidOperationException>(() => OwnedProcessExit.Capture(process));
                await Release(process);
            });
        }

        [Fact]
        public async Task ExitedOriginal_IsBoundToTheRetainedObject_BeforeAndAfterDisposal()
        {
            OwnedProcessExit? observed = null;
            await WithPeer(async process =>
            {
                await Release(process);
                observed = OwnedProcessExit.Capture(process);
                Assert.True(observed.HasExited); Assert.Equal(0, observed.ExitCode);
                Assert.Equal(process.Id, observed.ProcessId);
                Assert.Same(observed, OwnedProcessExit.Capture(process));
            });
            Assert.True(observed!.HasExited); Assert.NotEqual(Guid.Empty, observed.ExecutionId);
        }

        [Fact]
        public async Task SameNumericPidReplacement_CannotChangeOriginalExitOrBorrowItsAuthority()
        {
            var original = await Client().AnalyzeAsync(Input());
            var exited = HistoricalWorkerLifetimeAssertions.Stopped(original);
            // Deterministic reused-ID model at the local call boundary: no OS PID recycling required.
            var replacement = new HistoricalWorkerCallResult(null, null, null, ProcessId: exited.ProcessId);
            Assert.Equal(original.ProcessId, replacement.ProcessId);
            Assert.False(HistoricalWorkerClient.TryGetProcessExit(replacement, out _));
            Assert.Null(replacement.ProcessExit);
            Assert.ThrowsAny<XunitException>(() => HistoricalWorkerLifetimeAssertions.Stopped(replacement));
            Assert.Same(exited, HistoricalWorkerLifetimeAssertions.Stopped(original));
            Assert.False(HistoricalWorkerClient.TryGetProcessExit(original with { }, out _));
        }

        [Fact]
        public async Task IndependentOwnedChildrenHaveDifferentExecutionIdentities()
        {
            OwnedProcessExit? first = null;
            await WithPeer(async process => { await Release(process); first = OwnedProcessExit.Capture(process); });
            await WithPeer(async process =>
            {
                Assert.False(process.HasExited);
                Assert.True(first!.HasExited); // The other live child cannot turn the original into a leak.
                await Release(process);
                Assert.NotEqual(first.ExecutionId, OwnedProcessExit.Capture(process).ExecutionId);
            });
        }

        [Fact]
        public void MissingInvalidOrDisposedOwnership_IsNotSilentlyStopped()
        {
            Assert.Throws<ArgumentNullException>(() => OwnedProcessExit.Capture(null!));
            using var unstarted = new Process();
            Assert.Throws<InvalidOperationException>(() => OwnedProcessExit.Capture(unstarted));
            unstarted.Dispose();
            Assert.ThrowsAny<InvalidOperationException>(() => OwnedProcessExit.Capture(unstarted));
            Assert.False(HistoricalWorkerClient.TryGetProcessExit(null, out _));
            foreach (int? id in new int?[] { null, -1, 0, 12345 })
            {
                var unknown = new HistoricalWorkerCallResult(null, null, null, ProcessId: id);
                Assert.Null(unknown.ProcessExit);
                Assert.ThrowsAny<XunitException>(() => HistoricalWorkerLifetimeAssertions.Stopped(unknown));
            }
        }

        [Fact]
        public async Task OriginalExitRacingWithObservation_CannotReportAnUnobservedExit()
        {
            await WithPeer(async process =>
            {
                // Close the actual child while observing the retained object. Either result is safe.
                var release = Release(process);
                OwnedProcessExit? observed = null;
                try { observed = OwnedProcessExit.Capture(process); }
                catch (InvalidOperationException) { Assert.Null(observed); }
                await release;
                var complete = OwnedProcessExit.Capture(process);
                Assert.True(complete.HasExited);
                if (observed != null) { Assert.Same(observed, complete); }
            });
        }

        [Theory]
        [InlineData("real", null)]
        [InlineData("compile-error", "StructuredFailure")]
        [InlineData("crash", "WorkerCrash")]
        [InlineData("malformed", "MalformedResponse")]
        [InlineData("nonzero", "NonZeroExit")]
        [InlineData("utf8", "TransportFailure")]
        [InlineData("flood", "OutputLimit")]
        [InlineData("stderr-flood", "OutputLimit")]
        [InlineData("timeout", "Timeout")]
        [InlineData("cancel", "Cancelled")]
        public async Task RealNormalAndFailedExecutions_AllRequireActualOwnedCleanup(string scenario, string? failure)
        {
            bool fake = scenario is not ("real" or "compile-error");
            using var cancellation = new CancellationTokenSource();
            if (scenario == "cancel") { cancellation.CancelAfter(TimeSpan.FromSeconds(1)); }
            var client = Client(fake, scenario == "timeout" ? TimeSpan.FromSeconds(1) : TimeSpan.FromSeconds(30));
            var call = await client.AnalyzeAsync(Input(scenario == "compile-error" ? "invalid source" : fake ? scenario is "timeout" or "cancel" ? "hang" : scenario : null), cancellation.Token);
            if (failure == null) { Assert.True(call.Success, call.Failure?.Message); }
            else { Assert.False(call.Success); Assert.Equal(failure, call.Failure!.Code.ToString()); }
            var exit = HistoricalWorkerLifetimeAssertions.Stopped(call);
            var path = Path.Combine(Root(), "artifacts", "pid-g1", "cases", scenario + ".json");
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            await File.WriteAllTextAsync(path, JsonSerializer.Serialize(new { Scenario = scenario, call.Failure, call.ProcessId, Exit = exit, OriginalCall = true, NoFallback = true }, WorkerProtocol.JsonOptions));
        }

        [Fact]
        public async Task NoChildStarted_DoesNotMintAnExitObservation()
        {
            using var cancelled = new CancellationTokenSource(); cancelled.Cancel();
            var beforeStart = await Client().AnalyzeAsync(Input(), cancelled.Token);
            Assert.Null(beforeStart.ProcessId); Assert.Null(beforeStart.ProcessExit);
            var missingHost = new HistoricalWorkerClient(Path.Combine(Root(), "artifacts", "pid-g1", "missing-host"), Worker(), TimeSpan.FromSeconds(10));
            var failed = await missingHost.AnalyzeAsync(Input());
            Assert.Null(failed.ProcessId); Assert.Null(failed.ProcessExit);
        }

        [Fact]
        public void Boundary_PublishesObservationOnlyAfterExistingOwnedExitAndPipeCleanup()
        {
            string source = File.ReadAllText(Path.Combine(Root(), "src/XMLDocNormalizer/Execution/Historical/HistoricalWorkerClient.cs"));
            Assert.Contains("processExits.Add(returnedCall, OwnedProcessExit.Capture(process))", source);
            Assert.True(source.IndexOf("await process.WaitForExitAsync();", StringComparison.Ordinal) < source.IndexOf("processExits.Add", StringComparison.Ordinal));
            Assert.True(source.IndexOf("await Task.WhenAll(stdout, stderr, exchange)", StringComparison.Ordinal) < source.IndexOf("processExits.Add", StringComparison.Ordinal));
            Assert.DoesNotContain("GetProcessById", source);
            Assert.DoesNotContain("StartTime", source); // No cross-platform creation-time query/race is required.
        }

        private static async Task WithPeer(Func<Process, Task> action)
        {
            var start = new ProcessStartInfo("dotnet") { UseShellExecute = false, RedirectStandardInput = true, RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true };
            start.ArgumentList.Add(Peer());
            using var process = Process.Start(start)!;
            var stdout = process.StandardOutput.ReadToEndAsync(); var stderr = process.StandardError.ReadToEndAsync();
            try { await action(process); }
            finally
            {
                if (!process.HasExited) { process.Kill(entireProcessTree: true); }
                await process.WaitForExitAsync(); await Task.WhenAll(stdout, stderr);
            }
        }
        private static async Task Release(Process process)
        {
            await process.StandardInput.WriteAsync("{\"payload\":{\"source\":\"malformed\"}}"); process.StandardInput.Close();
            await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(10));
        }
        private static WorkerAnalysisInput Input(string? source = null) => new(source ?? "public static class Fixture { public static void Root() { throw null; } }", "Fixture", "Root", new());
        private static HistoricalWorkerClient Client(bool fake = false, TimeSpan? deadline = null) => new("dotnet", fake ? Peer() : Worker(), deadline ?? TimeSpan.FromSeconds(30));
        private static string Worker() => Path.Combine(Root(), "artifacts/exception-flow-historical/XMLDocNormalizer.HistoricalWorker/bin/Debug/net8.0/XMLDocNormalizer.HistoricalWorker.dll");
        private static string Peer() => Path.Combine(Root(), "artifacts/p5o2b4/test-process/bin/Debug/net8.0/BoundaryTestProcess.dll");
        private static string Root() => ExceptionFlowProductionCurrentParityTests.Root();
    }
}
