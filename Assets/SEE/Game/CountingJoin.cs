namespace SEE.Game
{
    /// <summary>
    /// An execution join awaiting a given number of events until execution can continue.
    /// It is similar to a join in a Petri net.
    /// </summary>
    public abstract class CountingJoin
    {
        /// <summary>
        /// The number of outstanding events that need to be awaited before execution can continue.
        /// </summary>
        private int outstandingEvents;

        /// <summary>
        /// Defines the number of events to be waited for until execution can continue.
        /// </summary>
        /// <param name="expectedNumberOfEvents">the number of events to be awaited</param>
        public void Await(int expectedNumberOfEvents)
        {
            outstandingEvents = expectedNumberOfEvents;
        }

        /// <summary>
        /// Signals this <see cref="CountingJoin"/> that one awaited event has occurred.
        /// If there are no more other events to be awaited, <see cref="Continue"/> will be
        /// called. That method depends upon the concrete subclass.
        /// </summary>
        public void Finished()
        {
            outstandingEvents--;
            UnityEngine.Assertions.Assert.IsTrue(outstandingEvents >= 0);
            if (outstandingEvents == 0)
            {
                Continue();
            }
        }

        public void Skip()
        {
            Await(1);
            Finished();
        }

        /// <summary>
        /// The method to be called when the expected number of events have occurred.
        /// It will be called by <see cref="Finished"/>.
        /// </summary>
        protected abstract void Continue();
    }
}