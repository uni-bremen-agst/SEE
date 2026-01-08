using System.Timers;

namespace SEE.Utils
{
    /// <summary>
    /// A base class for pollers which execute polls at regular intervals.
    /// </summary>
    public abstract class PollerBase
    {
        /// <summary>
        /// Backing attribute for <see cref="PollingInterval"/>.
        /// </summary>
        private int pollingInterval = 5;

        /// <summary>
        /// The number of milliseconds in a second.
        /// </summary>
        private const int millisecondsPerSecond = 1000;

        /// <summary>
        /// The time interval in seconds between two polls.
        /// </summary>
        public int PollingInterval
        {
            get => pollingInterval;
            set
            {
                if (value <= 0)
                {
                    throw new System.ArgumentOutOfRangeException("Polling interval must be positive.");
                }
                pollingInterval = value;
                if (timer != null)
                {
                    timer.Interval = pollingInterval * millisecondsPerSecond;
                }
            }
        }

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
            timer = new(PollingInterval * millisecondsPerSecond)
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
            if (timer != null)
            {
                timer.Stop();
                timer.Dispose();
                timer = null;
            }
        }
    }
}
