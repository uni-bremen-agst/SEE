#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.Shared.VSCodeDebugProtocol.Messages;
using Newtonsoft.Json.Linq;
using SEE.Graphs;
using SEE.UI.DebugAdapterProtocol;
using StackFrame = Microsoft.VisualStudio.Shared.VSCodeDebugProtocol.Messages.StackFrame;

namespace SEE.Tools.AI
{
    /// <summary>
    /// The debugger tools of <see cref="AgentCommandServer"/>: they observe and control SEE's
    /// existing debug session (<see cref="DebugAdapterProtocolSession"/>) and set breakpoints
    /// through <see cref="DebugBreakpointManager"/>, like the code window does.
    ///
    /// The user starts the session with SEE's Run button. Continue and step only send the
    /// request and return immediately; <see cref="Status"/> tells where the debuggee stopped.
    /// Stack frames and variables are those fetched by the session at the last stop.
    /// All methods run on the main thread.
    /// </summary>
    internal class AgentDebugger
    {
        /// <summary>Values longer than this are cut.</summary>
        private const int maxValueLength = 200;

        /// <summary>Maps source locations onto nodes.</summary>
        private readonly SourceLocator sourceLocator;

        /// <summary>Creates the debugger tools.</summary>
        /// <param name="sourceLocator">Maps source locations onto nodes.</param>
        internal AgentDebugger(SourceLocator sourceLocator)
        {
            this.sourceLocator = sourceLocator;
        }

        /// <summary>
        /// Whether a session runs, whether the debuggee is paused, where, and why; plus all breakpoints.
        /// </summary>
        /// <param name="focus">Whether to focus the building of the current location.</param>
        /// <returns>The JSON answer; ok also if there is no session.</returns>
        internal JObject Status(bool focus)
        {
            DebugAdapterProtocolSession session = DebugAdapterProtocolManager.ActiveSession;
            JObject result = new()
            {
                ["ok"] = true,
                ["session"] = session != null,
                ["breakpoints"] = new JArray(DebugAdapterProtocolSession.AllBreakpoints()
                                                 .Select(b => new JObject { ["file"] = b.path, ["line"] = b.line }))
            };
            if (session == null)
            {
                result["message"] = "No debug session. The user starts one with the Run button of SEE's debugger; "
                                    + "breakpoints set now are sent when it starts.";
                return result;
            }
            result["running"] = session.IsDebuggeeRunning;
            if (!session.IsReady)
            {
                result["message"] = "The debug session is starting.";
                return result;
            }
            if (session.IsDebuggeeRunning)
            {
                result["message"] = "The debuggee is running (not stopped at a breakpoint).";
                return result;
            }
            result["stop_reason"] = session.LastStopReason;
            result["state_fetched"] = session.StateFetched;
            if (!session.StateFetched)
            {
                result["message"] = "The debuggee has stopped; its state is still being fetched. Ask again.";
                return result;
            }

            int index = session.StackFrames.ToList().FindIndex(frame => frame.Source != null);
            if (index < 0)
            {
                result["message"] = $"Stopped ({session.LastStopReason}), but no stack frame has source code.";
                return result;
            }
            JObject location = DescribeFrame(session.StackFrames[index], index);
            result["location"] = location;
            string message = $"Stopped ({session.LastStopReason}) in {location["function"]} at "
                             + $"{location["file"]}:{location["line"]}.";
            string focusId = (string)location["focus_id"];
            if (focus && focusId != null)
            {
                message += " " + AgentCityEffects.Instance.Focus(focusId).Message;
            }
            result["message"] = message;
            return result;
        }

        /// <summary>
        /// Sets or removes a breakpoint through <see cref="DebugBreakpointManager"/>; the session
        /// (if any) sends it to the debug adapter, and code windows show it.
        /// </summary>
        /// <param name="file">Absolute path, or a path a node of a city is declared in.</param>
        /// <param name="line">The line (1-based).</param>
        /// <param name="add">Whether to set (true) or remove (false) the breakpoint.</param>
        /// <returns>The JSON answer.</returns>
        internal JObject SetBreakpoint(string file, int? line, bool add)
        {
            if (string.IsNullOrWhiteSpace(file) || line is not > 0)
            {
                return AgentCommandServer.Error("A breakpoint needs file and line (at least 1).");
            }
            if (!TryAbsolutePath(file, line.Value, out string path, out string problem))
            {
                return AgentCommandServer.Error(problem);
            }
            bool changed = DebugBreakpointManager.SetBreakpoint(path, line.Value, add);
            string what = $"{path}:{line}";
            string message = add
                ? changed ? $"Breakpoint set at {what}." : $"There already is a breakpoint at {what}."
                : changed ? $"Breakpoint at {what} removed." : $"There is no breakpoint at {what}.";
            if (changed)
            {
                message += DebugAdapterProtocolManager.ActiveSession != null
                    ? " It was sent to the debug adapter."
                    : " It is sent to the debug adapter when a debug session starts.";
            }
            return new() { ["ok"] = true, ["message"] = message, ["file"] = path, ["line"] = line.Value };
        }

        /// <summary>Lets the paused debuggee continue; returns immediately.</summary>
        /// <returns>The JSON answer.</returns>
        internal JObject Continue()
        {
            if (!TryGetPausedSession(out DebugAdapterProtocolSession session, out JObject error))
            {
                return error;
            }
            session.Continue();
            return new()
            {
                ["ok"] = true,
                ["message"] = "Continue sent; the debuggee runs until the next breakpoint. "
                              + "Call debug_status to see where it stopped."
            };
        }

        /// <summary>Executes one step of the paused debuggee; returns immediately.</summary>
        /// <param name="kind">"over" (default), "in" or "out".</param>
        /// <returns>The JSON answer.</returns>
        internal JObject Step(string kind)
        {
            kind = string.IsNullOrWhiteSpace(kind) ? "over" : kind.Trim().ToLowerInvariant();
            if (kind is not ("over" or "in" or "out"))
            {
                return AgentCommandServer.Error($"Unknown step kind '{kind}'; use over, in or out.");
            }
            if (!TryGetPausedSession(out DebugAdapterProtocolSession session, out JObject error))
            {
                return error;
            }
            switch (kind)
            {
                case "in":
                    session.StepIn();
                    break;
                case "out":
                    session.StepOut();
                    break;
                default:
                    session.StepOver();
                    break;
            }
            return new()
            {
                ["ok"] = true,
                ["message"] = $"Step {kind} sent. Call debug_status to see where the debuggee stopped."
            };
        }

        /// <summary>The stack frames of the last stop, innermost first, with their nodes.</summary>
        /// <param name="limit">Maximum number of frames.</param>
        /// <returns>The JSON answer with a "frames" array.</returns>
        internal JObject Stack(int limit)
        {
            if (!TryGetFetchedState(out DebugAdapterProtocolSession session, out JObject error))
            {
                return error;
            }
            IReadOnlyList<StackFrame> frames = session.StackFrames;
            JArray described = new(frames.Take(System.Math.Max(1, limit)).Select(DescribeFrame));
            return new()
            {
                ["ok"] = true,
                ["message"] = $"{frames.Count} stack frame(s), showing {described.Count}.",
                ["frames"] = described
            };
        }

        /// <summary>The variables of one stack frame at the last stop, by scope.</summary>
        /// <param name="frameIndex">The frame (0 = innermost, see <see cref="Stack"/>).</param>
        /// <param name="limit">Maximum number of variables per scope.</param>
        /// <returns>The JSON answer with a "scopes" array.</returns>
        internal JObject Variables(int frameIndex, int limit)
        {
            if (!TryGetFetchedState(out DebugAdapterProtocolSession session, out JObject error))
            {
                return error;
            }
            if (frameIndex < 0 || frameIndex >= session.StackFrames.Count)
            {
                return AgentCommandServer.Error($"There is no frame {frameIndex}; "
                                                + $"the stack has {session.StackFrames.Count} frame(s).");
            }
            JArray scopes = new();
            foreach ((Scope scope, List<Variable> variables) in session.VariablesOfFrame(frameIndex))
            {
                scopes.Add(new JObject
                {
                    ["scope"] = scope.Name,
                    ["variables"] = new JArray(variables.Take(System.Math.Max(1, limit)).Select(v => new JObject
                    {
                        ["name"] = v.Name,
                        ["value"] = Shorten(v.Value),
                        ["type"] = v.Type
                    })),
                    ["count"] = variables.Count
                });
            }
            StackFrame frame = session.StackFrames[frameIndex];
            return new()
            {
                ["ok"] = true,
                ["message"] = $"Variables of {frame.Name} (frame {frameIndex}) at the last stop.",
                ["scopes"] = scopes
            };
        }

        /// <summary>
        /// A stack frame with its source location and the node declared there.
        /// </summary>
        /// <param name="frame">The frame.</param>
        /// <param name="index">Its index (0 = innermost).</param>
        /// <returns>JSON with index, function, file, line, and node and focus_id if found.</returns>
        private JObject DescribeFrame(StackFrame frame, int index)
        {
            JObject result = new()
            {
                ["index"] = index,
                ["function"] = frame.Name,
                ["file"] = frame.Source?.Path,
                ["line"] = frame.Line
            };
            if (frame.Source?.Path != null
                && sourceLocator.TryLocate(frame.Source.Path, frame.Line, null, out Node node, out _, out _))
            {
                result["node"] = AgentCommandServer.Describe(node);
                result["focus_id"] = SourceLocator.DrawnNodeOrAncestor(node)?.ID;
            }
            return result;
        }

        /// <summary>
        /// The absolute path of <paramref name="file"/> in the form the code window uses for
        /// breakpoints (see <c>DesktopCodeWindow</c>), so both show the same breakpoints.
        /// </summary>
        /// <param name="file">An absolute path, or a path a node of a city is declared in.</param>
        /// <param name="line">The line; used to find a node declared in a relative file.</param>
        /// <param name="path">The absolute path, or null.</param>
        /// <param name="problem">Why no path was found, or null.</param>
        /// <returns>Whether a path was found.</returns>
        private bool TryAbsolutePath(string file, int line, out string path, out string problem)
        {
            path = null;
            problem = null;
            string absolute = file;
            if (!Path.IsPathRooted(file))
            {
                if (!sourceLocator.TryLocate(file, line, null, out Node node, out _, out problem)
                    && !sourceLocator.TryLocate(file, int.MaxValue, null, out node, out _, out problem))
                {
                    problem = $"'{file}' is not an absolute path, and no city knows it: {problem}";
                    return false;
                }
                absolute = node.AbsolutePlatformPath();
            }
            path = Path.GetFullPath(absolute).Replace('/', '\\');
            return true;
        }

        /// <summary>The active session if it is ready and the debuggee is paused.</summary>
        /// <param name="session">The session, or null.</param>
        /// <param name="error">The JSON failure, or null.</param>
        /// <returns>Whether such a session exists.</returns>
        private static bool TryGetPausedSession(out DebugAdapterProtocolSession session, out JObject error)
        {
            session = DebugAdapterProtocolManager.ActiveSession;
            error = session == null
                ? AgentCommandServer.Error("No debug session. Ask the user to start one with the Run button of SEE's debugger.")
                : !session.IsReady
                    ? AgentCommandServer.Error("The debug session is still starting.")
                    : session.IsDebuggeeRunning
                        ? AgentCommandServer.Error("The debuggee is running; it must be stopped at a breakpoint first.")
                        : null;
            return error == null;
        }

        /// <summary>The active session if the state of its last stop has been fetched.</summary>
        /// <param name="session">The session, or null.</param>
        /// <param name="error">The JSON failure, or null.</param>
        /// <returns>Whether such a session exists.</returns>
        private static bool TryGetFetchedState(out DebugAdapterProtocolSession session, out JObject error)
        {
            if (!TryGetPausedSession(out session, out error))
            {
                return false;
            }
            if (!session.StateFetched)
            {
                error = AgentCommandServer.Error("The debuggee has stopped; its state is still being fetched. Ask again.");
                return false;
            }
            return true;
        }

        /// <summary><paramref name="value"/> cut to <see cref="maxValueLength"/> characters.</summary>
        /// <param name="value">The value, or null.</param>
        /// <returns>The shortened value.</returns>
        private static string Shorten(string value) =>
            value == null || value.Length <= maxValueLength ? value : value.Substring(0, maxValueLength) + "…";
    }
}
#endif
