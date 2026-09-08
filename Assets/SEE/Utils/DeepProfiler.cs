using JetBrains.Profiler.Api;
using System;
using System.Threading;
using UnityEngine;

namespace SEE.Utils
{
    /// <summary>
    /// Confines a dotTrace capture to a section of code. Every method that is
    /// transitively called within that section is profiled, without any of them
    /// having to be instrumented individually.
    ///
    /// Example use:
    ///
    ///   using (DeepProfiler.Capture("loading city"))
    ///   {
    ///       await LoadCityAsync(...);
    ///   }
    ///
    /// or, if the two code positions are not in the same lexical scope:
    ///
    ///   DeepProfiler.Start("loading city");
    ///   ...
    ///   DeepProfiler.Save();
    ///
    /// This requires dotTrace to be attached to this process with data collection
    /// not yet started (see <see cref="IsReady"/>). If no profiler is attached,
    /// every operation here does nothing except emit a warning once, which is why
    /// these calls may remain in the code.
    /// </summary>
    /// <remarks>
    /// dotTrace collects data for the process as a whole, hence there can be only
    /// one capture at a time. Nested and overlapping captures are counted; only the
    /// outermost one starts the collection and saves it, and the resulting snapshot
    /// covers the union of their extents.
    /// </remarks>
    internal static class DeepProfiler
    {
        /// <summary>
        /// The number of captures currently in progress. Only the outermost
        /// capture, that is, the one raising this from zero to one and later
        /// lowering it back to zero, talks to the profiler.
        /// </summary>
        private static int activeCaptures;

        /// <summary>
        /// The name of the action of the outermost capture currently in progress,
        /// or null if no capture is in progress. Used only to name the action in
        /// the message reporting that the snapshot was saved.
        /// </summary>
        private static string activeAction;

        /// <summary>
        /// Whether <see cref="WarnIfNotReady(string)"/> has already emitted its
        /// warning. It is emitted only once per session so that a capture in a
        /// frequently executed code path cannot flood the console.
        /// </summary>
        private static bool warningEmitted;

        /// <summary>
        /// Whether a profiler is attached to this process and ready to accept
        /// commands. This is false in every ordinary run of SEE.
        /// </summary>
        internal static bool IsReady => MeasureProfiler.GetFeatures().HasFlag(MeasureFeatures.Ready);

        /// <summary>
        /// Starts collecting profiling data for <paramref name="action"/>. If a
        /// capture is already in progress, this call merely nests within it and
        /// the profiler is left alone.
        ///
        /// Every call must be matched by a call to <see cref="Save"/> or
        /// <see cref="Drop"/>.
        /// </summary>
        /// <param name="action">The name of the action to be profiled. It becomes
        /// the name of the collected data block in dotTrace. Must not be null.</param>
        internal static void Start(string action)
        {
            if (Interlocked.Increment(ref activeCaptures) > 1)
            {
                // A capture is already in progress and will save the data.
                return;
            }
            activeAction = action;
            WarnIfNotReady(action);
            MeasureProfiler.StartCollectingData(action);
        }

        /// <summary>
        /// Stops collecting profiling data and writes the snapshot to disk, where
        /// dotTrace can open it. Leaves the profiler alone if this call closes a
        /// nested capture rather than the outermost one.
        /// </summary>
        internal static void Save()
        {
            if (!Close(out string action))
            {
                return;
            }
            bool ready = IsReady;
            MeasureProfiler.SaveData();
            if (ready)
            {
                Debug.Log($"Profiling snapshot for {action} was saved.\n");
            }
        }

        /// <summary>
        /// Stops collecting profiling data and discards it. This is intended for a
        /// section that turns out not to be worth a snapshot after all, for
        /// instance because the operation it profiles was canceled.
        ///
        /// Leaves the profiler alone if this call closes a nested capture rather
        /// than the outermost one.
        /// </summary>
        internal static void Drop()
        {
            if (!Close(out string _))
            {
                return;
            }
            MeasureProfiler.DropData();
        }

        /// <summary>
        /// Returns a scope that starts collecting profiling data for
        /// <paramref name="action"/> now and saves it when the scope is disposed.
        /// The scope may enclose an <c>await</c>; it is disposed wherever the
        /// continuation resumes.
        /// </summary>
        /// <param name="action">The name of the action to be profiled. It becomes
        /// the name of the collected data block in dotTrace. Must not be null.</param>
        /// <returns>The scope, which must be disposed to save the snapshot; a
        /// <c>using</c> statement is the intended way to do that.</returns>
        internal static IDisposable Capture(string action)
        {
            return new CaptureScope(action);
        }

        /// <summary>
        /// Closes one capture and tells whether it was the outermost one, that is,
        /// whether the caller is the one that must talk to the profiler.
        /// </summary>
        /// <param name="action">The name of the action of the capture just closed,
        /// or null if it was not the outermost one.</param>
        /// <returns>True if the capture just closed was the outermost one.</returns>
        private static bool Close(out string action)
        {
            action = null;
            int remaining = Interlocked.Decrement(ref activeCaptures);
            if (remaining > 0)
            {
                return false;
            }
            if (remaining < 0)
            {
                // More captures were closed than were ever started.
                Interlocked.Exchange(ref activeCaptures, 0);
                Debug.LogError("A profiling capture was closed that was never started.\n");
                return false;
            }
            action = activeAction;
            activeAction = null;
            return true;
        }

        /// <summary>
        /// Emits a warning if no profiler is attached, because a capture will then
        /// yield no snapshot at all. The warning is emitted at most once per
        /// session.
        /// </summary>
        /// <param name="action">The name of the action that was to be profiled.</param>
        private static void WarnIfNotReady(string action)
        {
            if (IsReady || warningEmitted)
            {
                return;
            }
            warningEmitted = true;
            Debug.LogWarning($"No profiler is attached, hence no data will be collected for {action}. "
                             + "Attach dotTrace to this process with data collection not yet started.\n");
        }

        /// <summary>
        /// A capture that saves its data when it is disposed. This is what
        /// <see cref="Capture(string)"/> returns.
        /// </summary>
        private sealed class CaptureScope : IDisposable
        {
            /// <summary>
            /// Whether <see cref="Dispose"/> has already been called, so that
            /// disposing twice does not close two captures.
            /// </summary>
            private bool disposed;

            /// <summary>
            /// Constructor. Starts collecting profiling data for
            /// <paramref name="action"/>.
            /// </summary>
            /// <param name="action">The name of the action to be profiled.</param>
            internal CaptureScope(string action)
            {
                Start(action);
            }

            /// <summary>
            /// Stops collecting profiling data and saves it. Has no effect when
            /// called more than once.
            /// </summary>
            public void Dispose()
            {
                if (disposed)
                {
                    return;
                }
                disposed = true;
                Save();
            }
        }
    }
}
