using UnityEngine;
using UnityEngine.Assertions;

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
        /// The number of outstanding events.
        /// </summary>
        public int OutstandingEvents { get => outstandingEvents; }

        private string name = string.Empty;

        /// <summary>
        /// Defines the number of events to be waited for until the <paramref name="callBack"/>
        /// can be executed.
        /// If <paramref name="expectedNumberOfEvents"/> is zero, the <paramref name="callBack"/>
        /// is executed right away.
        /// </summary>
        /// <param name="expectedNumberOfEvents">the number of events to be awaited</param>
        /// <param name="callBack">function to be called when there are no more outstanding
        /// events</param>
        public void Await(int expectedNumberOfEvents, CallBackFunction callBack, string stepName)
        {
            Debug.Log($"[CountingJoin] Changing from '{name}' to '{stepName}'.\n");
            name = stepName;
            Assert.IsTrue(expectedNumberOfEvents >= 0);
            if (outstandingEvents != 0) 
            {
                Debug.LogError($"[CountingJoin {name}] Early interruption while still awaiting {outstandingEvents} events.\n");
            }
            Assert.IsNotNull(callBack);
            this.callBack = callBack;
            outstandingEvents = expectedNumberOfEvents;
            Debug.Log($"[CountingJoin {name}] Awaiting {outstandingEvents} events.\n");
            if (outstandingEvents == 0)
            {
                Debug.Log($"[CountingJoin {name}] Callback {this.callBack}.\n");
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
            Debug.Log($"[CountingJoin.Finished {name}] Still waiting for {outstandingEvents} events.\n");
            outstandingEvents--;
            if (outstandingEvents < 0) 
            {
                string message = $"Assertion violation: the number of outstanding events must not be negative ({outstandingEvents}).\n";
                Debug.LogError(message);
                return;
                //throw new System.Exception(message);
            }
            // UnityEngine.Assertions.Assert.IsTrue(outstandingEvents >= 0);            
            if (outstandingEvents == 0)
            {
                Debug.Log($"[CountingJoin {name}] No outstanding events. Callback {this.callBack}.\n");
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
            Debug.Log($"[CountingJoin {name}] Skipping {outstandingEvents} events.\n");
            if (outstandingEvents > 0)
            {
                Debug.Log($"[CountingJoin {name}] Callback {this.callBack}.\n");
                outstandingEvents = 0;
                callBack();
            }
        }
    }
}
