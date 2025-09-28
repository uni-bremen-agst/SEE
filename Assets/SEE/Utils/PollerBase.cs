using System.Timers;

namespace SEE.Utils
{
    /// <summary>
    /// A base class for pollers which execute polls at regular intervals.
    /// </summary>
    public abstract class PollerBase
    {
        /// <summary>
        /// The time interval in seconds between two polls.
        /// </summary>
        public int PollingInterval = 5;

        /// <summary>
        /// The timer which triggers the polls.
        /// </summary>
        protected Timer timer;

        /// <summary>
        /// Starts the actual poller.
        /// </summary>
        public virtual void Start()
        {
            // Create a timer with a PollingInterval-second interval (1000 milliseconds)
            timer = new(PollingInterval * 1000)
            {
                // This ensures the timer repeats. It's the default, but it's good practice to be explicit.
                AutoReset = true,
            };

            // Start the timer
            timer.Start();
        }

        /// <summary>
        /// Stops the poller.
        /// </summary>
        public virtual void Stop()
        {
            // Always stop and dispose of the timer when you're finished
            timer.Stop();
            timer.Dispose();
            timer = null;
        }
    }
}
