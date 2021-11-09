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
        private Performance() { }

        private Performance(string action, Stopwatch sw)
        {
            this.action = action;
            stopWatch = sw;
        }

        private readonly Stopwatch stopWatch;

        private readonly string action;

        private double totalTimeInMilliSeconds = 0.0;

        /// <summary>
        /// Returns a new performance time stamp and emits given action.
        /// </summary>
        /// <param name="action">name of action started to be printed</param>
        /// <returns></returns>
        public static Performance Begin(string action)
        {
            Stopwatch sw = new Stopwatch();
            Performance result = new Performance(action, sw);
            sw.Start();
            return result;
        }

        /// <summary>
        /// Emits the elapsed time from the start of the performance time span
        /// until now. Reports it to Debug.Log along with the action name.
        /// </summary>
        /// <param name="print">if true, the elapsed time will be printed</param>
        public void End(bool print = false)
        {
            stopWatch.Stop();
            TimeSpan ts = stopWatch.Elapsed;
            totalTimeInMilliSeconds = ts.TotalMilliseconds;
            if (print)
            {
                Debug.LogFormat("Action {0} finished in {1} [h:m:s:ms] elapsed time).\n", action, GetElapsedTime());
            }
        }

        /// <summary>
        /// Returns the elapsed time between the calls of Begin(string) and End(bool)
        /// in the format h:m:s:ms.
        /// </summary>
        /// <returns>elapsed time</returns>
        public string GetElapsedTime()
        {
            TimeSpan ts = TimeSpan.FromMilliseconds(totalTimeInMilliSeconds);
            return string.Format("{0:00}:{1:00}:{2:00}.{3:00}", ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds / 10);
        }

        /// <summary>
        /// Returns the elapsed time in milliseconds between the calls of Begin(string)
        /// and End(bool).
        /// </summary>
        /// <returns>elapsed time in milliseconds</returns>
        public double GetTimeInMilliSeconds()
        {
            return totalTimeInMilliSeconds;
        }
    }
}