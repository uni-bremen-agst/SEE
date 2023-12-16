using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace SEE.Utils
{
    /// <summary>
    /// Contains utility methods for asynchronous programming.
    /// </summary>
    public static class AsyncUtils
    {
        // Assuming caller runs on thread pool: switches to main thread to execute given function and then switches back to thread pool.

        /// <summary>
        /// Runs the given action on the main thread and returns the result as a <see cref="UniTask"/>.
        /// <p>
        /// <b>Important:</b> This method should only be used if the caller is running on the thread pool.
        /// Additionally, be aware that switching between threads can be very expensive.
        /// </p>
        /// </summary>
        /// <param name="action">The action to run on the main thread.</param>
        /// <typeparam name="T">The return type of the action.</typeparam>
        /// <returns>A <see cref="UniTask"/> that returns the result of the action.</returns>
        /// <remarks>
        /// When running on the thread pool, resources such as <see cref="UnityEngine.Object"/> or MonoBehaviors
        /// cannot be accessed. This method allows you to side-step this limitation by switching to the main thread
        /// to execute the given action.
        /// </remarks>
        public static async UniTask<T> RunOnMainThread<T>(Func<T> action)
        {
            bool notOnMainThread = Thread.CurrentThread.ManagedThreadId != 1;
            if (notOnMainThread)
            {
                await UniTask.SwitchToMainThread();
            }
            else
            {
                Debug.LogWarning("RunOnMainThread called from main thread. This is not necessary.");
            }
            T result = action();
            if (notOnMainThread)
            {
                await UniTask.SwitchToThreadPool();
            }
            return result;
        }
    }
}
