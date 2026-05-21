using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Linq;
using MoreLinq;
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
        /// The main thread ID.
        /// Needs to be set by the main thread at the start of the application.
        /// </summary>
        public static int MainThreadId = 0;

        /// <summary>
        /// Converts the given <paramref name="task"/> of enumerables to an asynchronous UniTask enumerable.
        /// </summary>
        /// <param name="task">The task of enumerables to convert.</param>
        /// <param name="logErrors">Whether to log errors that occur during conversion instead of throwing them.</param>
        /// <typeparam name="T">The type of the elements in the enumerable.</typeparam>
        /// <returns>An asynchronous UniTask enumerable that emits the elements of the enumerable.</returns>
        public static IUniTaskAsyncEnumerable<T> AsUniTaskAsyncEnumerable<T>(this UniTask<IEnumerable<T>> task,
                                                                             bool logErrors = false)
        {
            return task.ToUniTaskAsyncEnumerable().SelectMany(x =>
            {
                if (x == null)
                {
                    return UniTaskAsyncEnumerable.Empty<T>();
                }

                if (!logErrors)
                {
                    return x.ToUniTaskAsyncEnumerable();
                }
                else
                {
                    try
                    {
                        return x.ToUniTaskAsyncEnumerable();
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Error converting enumerable to UniTaskAsyncEnumerable: {e}");
                        return UniTaskAsyncEnumerable.Empty<T>();
                    }
                }
            });
        }

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
                    throw new TimeoutException($"Task did not complete within {timeout}. (This may also be due to an exception that can't be caught here.)");
                }
                else
                {
                    return defaultValue;
                }
            }
            return result;
        }

        /// <summary>
        /// Runs the given <paramref name="task"/> with a <paramref name="timeout"/>.
        /// Note that a timeout of <see cref="TimeSpan.Zero"/> will cause no timeout to be applied.
        ///
        /// If the task does not complete within the given timeout and <paramref name="throwOnTimeout"/> is set to true,
        /// a <see cref="TimeoutException"/> is thrown.
        /// </summary>
        /// <param name="task">The task to run. It should accept a <see cref="CancellationToken"/> as an argument,
        /// which will be used to cancel the task if it exceeds the timeout.</param>
        /// <param name="timeout">The maximum time to wait for the task to complete.</param>
        /// <param name="throwOnTimeout">Whether to throw a <see cref="TimeoutException"/> if the task times out.</param>
        /// <exception cref="TimeoutException">Thrown if the task does not complete within the given timeout and
        /// <paramref name="throwOnTimeout"/> is set to <c>true</c>.</exception>
        /// <returns><c>true</c> if the task completed within the timeout, <c>false</c> otherwise.</returns>
        public static async UniTask<bool> RunWithTimeoutAsync(Func<CancellationToken, UniTask> task, TimeSpan timeout,
                                                        bool throwOnTimeout = true)
        {
            return await RunWithTimeoutAsync(async token =>
            {
                await task(token);
                return true;
            }, timeout, throwOnTimeout: throwOnTimeout, defaultValue: false);
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
        /// Iterates over the given <paramref name="items"/> in batches of size <paramref name="batchSize"/>.
        /// Between each batch, the method yields control back to the main thread so that another frame
        /// can be rendered and the game can remain responsive. This is useful for processing large
        /// amounts of data without causing frame drops, as only <paramref name="batchSize"/> items are
        /// handled per frame.
        ///
        /// To iterate over the batches, use `await foreach`.
        /// </summary>
        /// <param name="items">The items to iterate over.</param>
        /// <param name="batchSize">The size of each batch.</param>
        /// <param name="token">The cancellation token to use.</param>
        /// <typeparam name="T">The type of the items.</typeparam>
        /// <returns>An asynchronous enumerable that emits the items in batches.</returns>
        public static IUniTaskAsyncEnumerable<T> BatchPerFrame<T>(this IEnumerable<T> items, int batchSize = 1000,
                                                                  CancellationToken token = default)
        {
            return UniTaskAsyncEnumerable.Create<IEnumerable<T>>(async (writer, _) =>
            {
                foreach (T[] batch in items.Batch(batchSize))
                {
                    token.ThrowIfCancellationRequested();
                    await writer.YieldAsync(batch);
                    await UniTask.Yield();
                }
            }).SelectMany(x => x.ToUniTaskAsyncEnumerable());
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
            T result = action();
            if (notOnMainThread)
            {
                await UniTask.SwitchToThreadPool();
            }
            return result;
        }

        /// <summary>
        /// Runs the given action on the main thread.
        /// <p>
        /// <b>Important:</b> This method should only be used if the caller is running on the thread pool.
        /// Additionally, be aware that switching between threads can be very expensive.
        /// If you're using this from inside a MonoBehaviour, it's recommended to use a ConcurrentQueue of Actions
        /// instead, which you can then run on the main thread in <see cref="MonoBehaviour.Update"/>.
        /// </p>
        /// </summary>
        /// <param name="action">The action to run on the main thread.</param>
        /// <remarks>
        /// When running on the thread pool, resources such as <see cref="UnityEngine.Object"/> or MonoBehaviors
        /// cannot be accessed. This method allows you to side-step this limitation by switching to the main thread
        /// to execute the given action.
        /// </remarks>
        public static async UniTask RunOnMainThreadAsync(Action action)
        {
            await RunOnMainThreadAsync<byte>(() =>
            {
                action();
                return default;
            });
        }

        /// <summary>
        /// Whether the current thread is the main thread.
        /// </summary>
        public static bool IsMainThread => Thread.CurrentThread.ManagedThreadId == MainThreadId;

        /// <summary>
        /// Whether the current thread is the main thread and the application is running.
        /// </summary>
        public static bool IsRunningOnMainThread => IsMainThread && (!Application.isEditor || Application.isPlaying);
    }
}
