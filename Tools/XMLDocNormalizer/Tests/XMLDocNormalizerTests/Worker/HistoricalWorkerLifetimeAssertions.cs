using XMLDocNormalizer.Execution.Historical;

namespace XMLDocNormalizerTests.Worker
{
    internal static class HistoricalWorkerLifetimeAssertions
    {
        internal static OwnedProcessExit Stopped(HistoricalWorkerCallResult call)
        {
            Assert.NotNull(call.ProcessId);
            Assert.NotEqual(Environment.ProcessId, call.ProcessId.Value);
            Assert.True(HistoricalWorkerClient.TryGetProcessExit(call, out var exit), "Missing original-call owned Process exit observation.");
            Assert.NotNull(exit);
            Assert.True(exit.HasExited);
            Assert.Equal(call.ProcessId.Value, exit.ProcessId);
            Assert.NotEqual(Guid.Empty, exit.ExecutionId);
            return exit;
        }
    }
}
