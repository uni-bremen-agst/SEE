using UnityEngine;

namespace SEE.Utils
{
    /// <summary>
    /// An execution join awaiting a given number of events until execution can continue.
    /// It is similar to a join in a Petri net.
    /// </summary>
    public class Join
    {
        public delegate void CallBackFunction();


        private CallBackFunction CallBack;

        /// <summary>
        /// The number of outstanding events that need to be awaited before execution can continue.
        /// </summary>
        private int outstandingEvents;

        /// <summary>
        /// Defines the number of events to be waited for until execution can continue.
        /// </summary>
        /// <param name="expectedNumberOfEvents">the number of events to be awaited</param>
        public void Await(int expectedNumberOfEvents, CallBackFunction callBack)
        {
            CallBack = callBack;
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
                CallBack();
            }
        }

        public void Skip()
        {
            outstandingEvents = 0;
            CallBack();
        }
    }
}
