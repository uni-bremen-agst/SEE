namespace SEE.Utils
{
    /// <summary>
    /// An execution join awaiting a given number of events until execution can continue.
    /// It is similar to a join in a Petri net.
    /// </summary>
    public class CountingJoin
    {
        /// <summary>
        /// The type of callback to be called when there are no more outstanding events.
        /// </summary>
        public delegate void CallBackFunction();

        /// <summary>
        /// The callback to be called when there are no more outstanding events.
        /// </summary>
        private CallBackFunction callBack;

        /// <summary>
        /// The number of outstanding events that need to be awaited before the
        /// callback can be executed.
        /// </summary>
        private int outstandingEvents;

        /// <summary>
        /// Defines the number of events to be waited for until the <paramref name="callBack"/>
        /// can be executed.
        /// If <paramref name="expectedNumberOfEvents"/> is zero, the <paramref name="callBack"/>
        /// is executed right away.
        /// </summary>
        /// <param name="expectedNumberOfEvents">the number of events to be awaited</param>
        /// <param name="callBack">function to be called when there are no more outstanding
        /// events</param>
        public void Await(int expectedNumberOfEvents, CallBackFunction callBack)
        {
            UnityEngine.Assertions.Assert.IsNotNull(callBack);
            this.callBack = callBack;
            outstandingEvents = expectedNumberOfEvents;
            if (outstandingEvents == 0)
            {
                this.callBack();
            }
        }

        /// <summary>
        /// Signals this <see cref="CountingJoin"/> that one awaited event has occurred.
        /// If there are no more other events to be awaited, the callback will be
        /// called.
        /// </summary>
        public void Finished()
        {
            outstandingEvents--;
            UnityEngine.Assertions.Assert.IsTrue(outstandingEvents >= 0);
            if (outstandingEvents == 0)
            {
                callBack();
            }
        }

        /// <summary>
        /// If there are still outstanding events, the callback is executed immediately
        /// and no more outstanding events are to be awaited. If no events are expected
        /// anymore, nothing happens.
        ///
        /// Precondition: <see cref="Await(int, CallBackFunction)"/> must have been called
        /// before with a non-null callback.
        /// </summary>
        public void Skip()
        {
            UnityEngine.Assertions.Assert.IsNotNull(callBack);
            if (outstandingEvents > 0)
            {
                outstandingEvents = 0;
                callBack();
            }
        }
    }
}
