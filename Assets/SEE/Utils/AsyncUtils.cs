using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Linq;
using UnityEngine;
using CancellationTokenSource = System.Threading.CancellationTokenSource;

namespace SEE.Utils
{
    /// <summary>
    /// Contains utility methods for asynchronous programming.
    /// </summary>
    public static class AsyncUtils
    {
        /// <summary>
        /// Runs the given <paramref name="task"/> with a <paramref name="timeout"/> and returns the result.
        /// Note that a timeout of <see cref="TimeSpan.Zero"/> will cause no timeout to be applied.
        ///
        /// If the task does not complete within the given timeout, a <see cref="TimeoutException"/> is thrown.
        /// Alternatively, if <paramref name="throwOnTimeout"/> is set to <c>false</c>, the method will return the
        /// <paramref name="defaultValue"/>.
        /// </summary>
        /// <param name="task">The task to run. It should accept a <see cref="CancellationToken"/> as an argument,
        /// which will be used to cancel the task if it exceeds the timeout.</param>
        /// <param name="timeout">The maximum time to wait for the task to complete.</param>
        /// <param name="defaultValue">The default value to return if the task times out.</param>
        /// <param name="throwOnTimeout">Whether to throw a <see cref="TimeoutException"/> if the task times out.</param>
        /// <typeparam name="T">The return type of the task.</typeparam>
        /// <returns>The result of the task, or the <paramref name="defaultValue"/> if the task times out
        /// and <paramref name="throwOnTimeout"/> is set to <c>false</c>.</returns>
        /// <exception cref="TimeoutException">Thrown if the task does not complete within the given timeout and
        /// <paramref name="throwOnTimeout"/> is set to <c>true</c>.</exception>
        public static async UniTask<T> RunWithTimeoutAsync<T>(Func<CancellationToken, UniTask<T>> task, TimeSpan timeout,
                                                              T defaultValue = default, bool throwOnTimeout = true)
        {
            if (timeout == TimeSpan.Zero)
            {
                // No timeout applied, so just await the task.
                return await task(CancellationToken.None);
            }

            CancellationTokenSource cts = new();
            (bool isTimeout, T result) = await task(cts.Token).TimeoutWithoutException(timeout, taskCancellationTokenSource: cts);
            if (isTimeout)
            {
                if (throwOnTimeout)
                {
                    throw new TimeoutException($"Task did not complete within {timeout}.");
                }
                else
                {
                    return defaultValue;
                }
            }
            return result;
        }

        /// <summary>
        /// Observes the given <paramref name="task"/>'s observable until it completes or <paramref name="timeout"/>
        /// elapses. The results are returned as an asynchronous enumerable that can, for example, be iterated
        /// over using `await foreach`.
        /// </summary>
        /// <param name="task">The task to observe. It should return an <see cref="IObservable{T}"/> that emits
        /// multiple values.</param>
        /// <param name="timeout">The maximum time to wait for the observable to complete.</param>
        /// <typeparam name="T">The type of the values emitted by the observable.</typeparam>
        /// <returns>An asynchronous enumerable that emits the values emitted by the observable until it completes
        /// or the timeout elapses.</returns>
        public static IUniTaskAsyncEnumerable<T> ObserveUntilTimeout<T>(Func<CancellationToken, IObservable<IEnumerable<T>>> task,
                                                                        TimeSpan timeout)
        {
            CancellationTokenSource cts = new();
            UniTask.Delay(timeout, cancellationToken: cts.Token).ContinueWith(() => cts.Cancel());
            IUniTaskAsyncEnumerable<T> cleanUp = UniTaskAsyncEnumerable.Create<T>((_, _) =>
            {
                cts.Cancel();
                return UniTask.CompletedTask;
            });
            return task(cts.Token).ToUniTaskAsyncEnumerable()
                                  .TakeUntilCanceled(cts.Token)
                                  .Where(x => x != null)  // May be necessary for some observables.
                                  .SelectMany(x => x.ToUniTaskAsyncEnumerable())
                                  .Concat(cleanUp);
        }

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
        public static async UniTask<T> RunOnMainThreadAsync<T>(Func<T> action)
        {
            bool notOnMainThread = !IsMainThread;
            if (notOnMainThread)
            {
                await UniTask.SwitchToMainThread();
            }
            else
            {
                Debug.LogWarning("RunOnMainThreadAsync called from main thread. This is not necessary.\n");
            }
            T result = action();
            if (notOnMainThread)
            {
                await UniTask.SwitchToThreadPool();
            }
            return result;
        }

        /// <summary>
        /// Whether the current thread is the main thread.
        /// </summary>
        public static bool IsMainThread => Thread.CurrentThread.ManagedThreadId == 1;
    }
}
