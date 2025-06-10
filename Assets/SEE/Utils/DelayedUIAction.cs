using System;
using System.Collections;
using UnityEngine;

namespace SEE.Utils
{
    /// <summary>
    /// Executes a provided action after one frame delay (e.g., to wait for Unity UI to initialize).
    /// Auto-destroys itself after execution.
    /// </summary>
    public class DelayedUIAction : MonoBehaviour
    {
        private Action action;

        /// <summary>
        /// Starts delayed execution of the provided action.
        /// </summary>
        /// <param name="a">The action to invoke after one frame.</param>
        public void Run(Action a)
        {
            action = a;
            StartCoroutine(ExecuteAfterFrame());
        }

        private IEnumerator ExecuteAfterFrame()
        {
            yield return null; // wait one frame
            action?.Invoke();
            Destroy(this); // remove this component after use
        }
    }
}