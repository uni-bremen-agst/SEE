using Microsoft.VisualStudio.Shared.VSCodeDebugProtocol.Messages;
using System.Collections.Generic;
using System.Linq;
using StackFrame = Microsoft.VisualStudio.Shared.VSCodeDebugProtocol.Messages.StackFrame;

namespace SEE.UI.DebugAdapterProtocol
{
    /// <summary>
    /// This part of the <see cref="DebugAdapterProtocolSession"/> class lets code outside the UI
    /// (the debugger tools of the AI agent in <c>SEE.Tools.AI</c>) observe and control the session.
    ///
    /// <para>
    /// All members must be used on the main thread. The control methods only queue the
    /// request, exactly like the buttons of the debug controls; they return immediately.
    /// </para>
    /// </summary>
    public partial class DebugAdapterProtocolSession
    {
        /// <summary>
        /// Whether <see cref="StackFrames"/> and <see cref="VariablesOfFrame"/> show the state of
        /// the last stop. False while the debuggee runs and while that state is still being fetched.
        /// </summary>
        private volatile bool stateFetched;

        /// <summary>The reason of the last stop (e.g. "Breakpoint", "Step"), or null.</summary>
        private volatile string lastStopReason;

        /// <summary>Whether the debug adapter is initialized and its host is running.</summary>
        internal bool IsReady => capabilities != null && adapterHost is { IsRunning: true };

        /// <summary>Whether the debuggee is currently running (not paused).</summary>
        internal bool IsDebuggeeRunning => IsRunning;

        /// <summary>
        /// Whether the stack frames and variables of the last stop have been fetched; see
        /// <see cref="stateFetched"/>.
        /// </summary>
        internal bool StateFetched => stateFetched && !IsRunning;

        /// <summary>The reason of the last stop (e.g. "Breakpoint", "Step"), or null.</summary>
        internal string LastStopReason => lastStopReason;

        /// <summary>
        /// The stack frames of the main thread at the last stop, innermost first.
        /// Only meaningful if <see cref="StateFetched"/>.
        /// </summary>
        internal IReadOnlyList<StackFrame> StackFrames => stackFrames;

        /// <summary>
        /// The variables of the stack frame with the given index (0 = innermost) of the main
        /// thread, by scope, as fetched at the last stop. Nested variables are not included.
        /// </summary>
        /// <param name="frameIndex">Index into <see cref="StackFrames"/>.</param>
        /// <returns>The variables by scope; empty if the frame or its variables are unknown.</returns>
        internal IReadOnlyDictionary<Scope, List<Variable>> VariablesOfFrame(int frameIndex)
        {
            if (frameIndex < 0 || frameIndex >= stackFrames.Count || MainThread == null
                || !variables.TryGetValue(MainThread, out Dictionary<StackFrame, Dictionary<Scope, List<Variable>>> frames)
                || !frames.TryGetValue(stackFrames[frameIndex], out Dictionary<Scope, List<Variable>> scopes))
            {
                return new Dictionary<Scope, List<Variable>>();
            }
            return scopes;
        }

        /// <summary>Queues a continue request (like the Continue button).</summary>
        internal void Continue() => OnContinue();

        /// <summary>Queues a step-over request (like the Step Over button).</summary>
        internal void StepOver() => OnNext();

        /// <summary>Queues a step-in request (like the Step In button).</summary>
        internal void StepIn() => OnStepIn();

        /// <summary>Queues a step-out request (like the Step Out button).</summary>
        internal void StepOut() => OnStepOut();

        /// <summary>
        /// Records that the state of a new stop is being fetched; called when
        /// <see cref="IsRunning"/> changes.
        /// </summary>
        /// <param name="running">The new value of <see cref="IsRunning"/>.</param>
        private void OnRunningChanged(bool running)
        {
            stateFetched = false;
            if (!running)
            {
                // Runs after the actions that fetch threads, stack frames and variables.
                actions.Enqueue(() => stateFetched = !IsRunning);
            }
        }

        /// <summary>Records the reason of a stop.</summary>
        /// <param name="stoppedEvent">The stopped event.</param>
        private void RecordStop(StoppedEvent stoppedEvent)
        {
            lastStopReason = stoppedEvent.Reason.ToString();
        }

        /// <summary>The breakpoints of <see cref="DebugBreakpointManager"/> as (path, line) pairs.</summary>
        /// <returns>All breakpoints, ordered by path and line.</returns>
        internal static IEnumerable<(string path, int line)> AllBreakpoints() =>
            DebugBreakpointManager.Breakpoints
                                  .SelectMany(file => file.Value.Keys.Select(line => (file.Key, line)))
                                  .OrderBy(b => b.Key).ThenBy(b => b.line);
    }
}
