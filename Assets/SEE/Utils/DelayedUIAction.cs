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
        /// <summary>
        /// The action to be executed after the delay.
        /// </summary>
        private Action action;

        /// <summary>
        /// Starts delayed execution of the provided action.
        /// </summary>
        /// <param name="action">The action to invoke after one frame.</param>
        public void Run(Action action)
        {
            this.action = action;
            StartCoroutine(ExecuteAfterFrame());
        }

        /// <summary>
        /// Runs the <see cref="action"/> after waiting for one frame.
        /// </summary>
        /// <returns>Enumerator to continue.</returns>
        private IEnumerator ExecuteAfterFrame()
        {
            yield return null; // wait one frame
            action?.Invoke();
            Destroyer.Destroy(this); // remove this component after use
        }
    }
}
