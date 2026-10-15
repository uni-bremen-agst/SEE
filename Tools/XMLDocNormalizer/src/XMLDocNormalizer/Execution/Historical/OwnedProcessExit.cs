using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace XMLDocNormalizer.Execution.Historical
{
    /// <summary>Observation of an exited owned Process, never a later lookup of a reusable PID.</summary>
    internal sealed class OwnedProcessExit
    {
        private static readonly ConditionalWeakTable<Process, OwnedProcessExit> observations = new();
        private OwnedProcessExit(int processId, int exitCode)
        { ProcessId = processId; ExitCode = exitCode; ExecutionId = Guid.NewGuid(); }

        public Guid ExecutionId { get; }
        public int ProcessId { get; }
        public int ExitCode { get; }
        public bool HasExited => true;

        // The caller must still own the original object. Access/query/disposal errors propagate.
        internal static OwnedProcessExit Capture(Process process)
        {
            ArgumentNullException.ThrowIfNull(process);
            if (!process.HasExited) { throw new InvalidOperationException("The original owned process is still running."); }
            return observations.GetValue(process, owned => new(owned.Id, owned.ExitCode));
        }
    }
}
