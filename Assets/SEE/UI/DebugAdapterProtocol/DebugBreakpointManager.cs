using Microsoft.VisualStudio.Shared.VSCodeDebugProtocol.Messages;
using System;
using System.Collections.Generic;

namespace SEE.UI.DebugAdapterProtocol
{
    /// <summary>
    /// Manages the breakpoints of all code windows.
    /// </summary>
    public static class DebugBreakpointManager
    {
        /// <summary>
        /// Triggers when a breakpoint was added.
        /// </summary>
        public static event Action<string, int> OnBreakpointAdded;

        /// <summary>
        /// Triggers when a breakpoint was removed.
        /// </summary>
        public static event Action<string, int> OnBreakpointRemoved;

        /// <summary>
        /// All breakpoints as a readonly collection.
        /// </summary>
        public static IReadOnlyDictionary<string, Dictionary<int, SourceBreakpoint>> Breakpoints => breakpoints;

        /// <summary>
        /// All breakpoints.
        /// </summary>
        private static readonly Dictionary<string, Dictionary<int, SourceBreakpoint>> breakpoints = new();

        /// <summary>
        /// Adds a breakpoint.
        /// </summary>
        /// <param name="path">The file path.</param>
        /// <param name="line">The source code line.</param>
        /// <returns>Whether the breakpoints changed as a result of this call.</returns>
        public static bool AddBreakpoint(string path, int line)
        {
            if (!breakpoints.ContainsKey(path))
            {
                breakpoints.Add(path, new());
            }
            Dictionary<int, SourceBreakpoint> fileBreakpoints = breakpoints[path];
            if (!fileBreakpoints.ContainsKey(line))
            {
                fileBreakpoints.Add(line, new SourceBreakpoint()
                {
                    Line = line,
                });
                OnBreakpointAdded?.Invoke(path, line);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Removes a breakpoint.
        /// </summary>
        /// <param name="path">The file path.</param>
        /// <param name="line">The source code line.</param>
        /// <returns>Whether the breakpoints changed as a result of this call.</returns>
        public static bool RemoveBreakpoint(string path, int line)
        {
            if (breakpoints.TryGetValue(path, out var fileBreakpoints))
            {
                if (fileBreakpoints.Remove(line))
                {
                    OnBreakpointRemoved?.Invoke(path, line);
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Adds or removes a breakpoint.
        /// </summary>
        /// <param name="path">The file path.</param>
        /// <param name="line">The source code line.</param>
        /// <param name="add">Whether to add or remove.</param>
        /// <returns>Whether the breakpoints changed as a result of this call.</returns>
        public static bool SetBreakpoint(string path, int line, bool add)
        {
            if (add)
            {
                return AddBreakpoint(path, line);
            }
            else
            {
                return RemoveBreakpoint(path, line);
            }
        }

        /// <summary>
        /// Toggles a breakpoint.
        /// </summary>
        /// <param name="path">The file path.</param>
        /// <param name="line">The source code line.</param>
        public static void ToggleBreakpoint(string path, int line)
        {
            if (!breakpoints.ContainsKey(path))
            {
                breakpoints.Add(path, new()
            {
                {line, new SourceBreakpoint(line) }
            });
                OnBreakpointAdded(path, line);
            }
            else if (!breakpoints[path].ContainsKey(line))
            {
                breakpoints[path].Add(line, new SourceBreakpoint(line));
                OnBreakpointAdded(path, line);
            }
            else
            {
                breakpoints[path].Remove(line);
                OnBreakpointRemoved(path, line);
            }
        }
    }
}
