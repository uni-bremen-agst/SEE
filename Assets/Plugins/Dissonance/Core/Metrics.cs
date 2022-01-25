#if DISSONANCE_METRICS_LOG_TO_CONSOLE
    #define DISSONANCE_METRICS_ENABLED
#endif

#if DISSONANCE_METRICS_UNITY_USER_REPORTING
    #define DISSONANCE_METRICS_ENABLED
#endif

using System.Threading;
using JetBrains.Annotations;

namespace Dissonance
{
    /// <summary>
    /// Capture metrics which may be attached to an error report.
    ///  - To use `UnityUserReporting` define the compiler symbol `DISSONANCE_METRICS_UNITY_USER_REPORTING`
    /// </summary>
    /// <remarks>
    /// To add a new metric services:
    /// 1. Choose a new symbol to represent your metrics service. e.g. `DISSONANCE_METRICS_SERVICE_NAME_HERE`.
    /// 2. Add a new section at the top of the file which defines `DISSONANCE_METRICS_ENABLED` iff your `DISSONANCE_METRICS_SERVICE_NAME_HERE` symbol is defined.
    /// 3. In the `InternalSampleMetric` method add a new block which sends to your metrics service iff `DISSONANCE_METRICS_SERVICE_NAME_HERE` is defined.
    /// </remarks>
    public static class Metrics
    {
        private static readonly Log Log = Logs.Create(LogCategory.Core, typeof(Metrics).Name);

        #if DISSONANCE_METRICS_ENABLED
        /// <summary>
        /// This buffer transfers metrics from other threads
        /// </summary>
        private static readonly Datastructures.TransferBuffer<MetricEvent> MetricsFromOtherThreads = new Datastructures.TransferBuffer<MetricEvent>(512);

        /// <summary>
        /// `TransferBuffer` allows one reader thread and one writer thread. Protect writing with a lock to ensure there's only one writer.
        /// </summary>
        private static readonly object TransferWriteLock = new object();
        #endif

        private static Thread _main;

        internal static void WriteMultithreadedMetrics()
        {
            if (_main == null)
            {
                _main = Thread.CurrentThread;
                Log.Trace("Set main thread for logging to ManagedThreadId:{0}", _main.ManagedThreadId);
            }

            #if DISSONANCE_METRICS_ENABLED
                MetricEvent msg;
                // ReSharper disable once InconsistentlySynchronizedField (Justification: this is ok because the lock prevents multiple _writers_ and there's only ever one reader)
                while (MetricsFromOtherThreads.Read(out msg))
                    InternalSampleMetric(msg.Name, msg.Value);
            #endif
        }

        // ReSharper disable once UnusedMember.Local (Justification: It's used when one of the metrics providers is enabled)
#pragma warning disable IDE0051 // Remove unused private members
        private static void InternalSampleMetric(string name, double value)
#pragma warning restore IDE0051 // Remove unused private members
        {
            // This will only ever be called on the main thread!

            #if DISSONANCE_METRICS_LOG_TO_CONSOLE
                Log.Trace("`{0}`: {1}", name, value);
            #endif

            #if DISSONANCE_METRICS_UNITY_USER_REPORTING
                Unity.Cloud.UserReporting.Plugin.UnityUserReporting.CurrentClient.SampleMetric(name, value);
            #endif
        }

        /// <summary>
        /// Get an ID for a metric based on a general category and a unique ID for this metric stream (e.g. player name).
        /// Will return null (i.e. allocate nothing) if metrics are disabled.
        /// </summary>
        /// <param name="category"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        [CanBeNull]
        public static string MetricName(string category, string id)
        {
            #if DISSONANCE_METRICS_ENABLED
                return string.Format("Dissonance::{0}::{1}", category, id);
            #else
                return null;
            #endif
        }

        /// <summary>
        /// Get an ID for a metric based on a general category this metric stream.
        /// Will return null (i.e. allocate nothing) if metrics are disabled.
        /// </summary>
        /// <param name="category"></param>
        /// <returns></returns>
        [CanBeNull]
        public static string MetricName(string category)
        {
#if DISSONANCE_METRICS_ENABLED
            return string.Format("Dissonance::{0}", category);
#else
            return null;
#endif
        }

        /// <summary>
        /// Log a single value for the given metric
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        public static void Sample([CanBeNull] string name, float value)
        {
#if DISSONANCE_METRICS_ENABLED
            if (name == null)
                return;

            if (_main == null || _main == Thread.CurrentThread)
            {
                InternalSampleMetric(name, value);
            }
            else
            {
                lock (TransferWriteLock)
                    MetricsFromOtherThreads.TryWrite(new MetricEvent(name, value));
            }
#endif
        }

        // ReSharper disable once UnusedType.Local (Justification: used if a metrics provider is enabled)
        private struct MetricEvent
        {
            // ReSharper disable MemberCanBePrivate.Local (Justification: These fields require public if a metrics provider is enabled)
            public readonly string Name;
            public readonly double Value;
            // ReSharper restore MemberCanBePrivate.Local

            public MetricEvent(string name, double value)
            {
                Name = name;
                Value = value;
            }
        }
    }
}
