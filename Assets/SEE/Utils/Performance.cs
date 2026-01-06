using System;
using System.Diagnostics;
using Debug = UnityEngine.Debug;

namespace SEE.Utils
{
    /// <summary>
    /// Allows us to measure and emit the elapsed time for long-running actions.
    ///
    /// Example use:
    ///
    ///   Performance p = Performance.Begin("loading graph data");
    ///   ... do something
    ///   p.End();
    /// </summary>
    public class Performance
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="action">The name of the action whose performance is to be measured.</param>
        /// <param name="sw">The <see cref="Stopwatch"/> to be used for the measurement.</param>
        private Performance(string action, Stopwatch sw)
        {
            this.action = action;
            stopWatch = sw;
        }

        /// <summary>
        /// The <see cref="Stopwatch"/> to be used for the measurement.
        /// </summary>
        private readonly Stopwatch stopWatch;

        /// <summary>
        /// The name of the action whose performance is to be measured.
        /// </summary>
        private readonly string action;

        /// <summary>
        /// The elapsed time in milliseconds between the calls of <see cref="Begin(string)"/>
        /// and <see cref="End(bool)"/>.
        /// </summary>
        private double totalTimeInMilliSeconds;

        /// <summary>
        /// Returns a new performance time stamp and emits given action.
        /// </summary>
        /// <param name="action">Name of action started to be printed.</param>
        /// <returns>Instance to be used to measure the performance.</returns>
        public static Performance Begin(string action)
        {
            Stopwatch sw = new();
            Performance result = new(action, sw);
            sw.Start();
            return result;
        }

        /// <summary>
        /// Emits the elapsed time from the start of the performance time span
        /// until now. Reports it to Debug.Log along with the action name.
        /// </summary>
        /// <param name="print">If true, the elapsed time will be printed.</param>
        public void End(bool print = false)
        {
            stopWatch.Stop();
            TimeSpan ts = stopWatch.Elapsed;
            totalTimeInMilliSeconds = ts.TotalMilliseconds;
            if (print)
            {
                Debug.Log($"Action {action} finished in {GetElapsedTime()} [h:m:s:ms] elapsed time).\n");
            }
        }

        /// <summary>
        /// Returns the elapsed time between the calls of Begin(string) and End(bool)
        /// in the format h:m:s:ms.
        /// </summary>
        /// <returns>Elapsed time.</returns>
        public string GetElapsedTime()
        {
            TimeSpan ts = TimeSpan.FromMilliseconds(totalTimeInMilliSeconds);
            return $"{ts.Hours:00}:{ts.Minutes:00}:{ts.Seconds:00}.{ts.Milliseconds / 10:00}";
        }

        /// <summary>
        /// Returns the elapsed time in milliseconds between the calls of <see cref="Begin(string)"/>
        /// and <see cref="End(bool)"/>.
        /// </summary>
        /// <returns>Elapsed time in milliseconds.</returns>
        public double GetTimeInMilliSeconds()
        {
            return totalTimeInMilliSeconds;
        }
    }
}
