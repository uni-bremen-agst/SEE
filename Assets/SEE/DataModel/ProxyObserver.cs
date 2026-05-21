using System;
using System.Collections.Generic;

namespace SEE.DataModel
{
    public abstract partial class Observable<T>
    {
        /// <summary>
        /// An observer which acts as a proxy for a <see cref="baseObservable"/>.
        ///
        /// This observer can subscribe to other observables of the same type as the <see cref="baseObservable"/>,
        /// and transparently route all of their events as a notification to the <see cref="baseObservable"/>.
        /// </summary>
        protected class ProxyObserver : IObserver<T>
        {
            /// <summary>
            /// Set of all disposables associated with the observables this observer observes.
            /// </summary>
            private readonly ISet<IDisposable> disposables = new HashSet<IDisposable>();

            /// <summary>
            /// The observable this observer belongs to.
            /// </summary>
            private readonly Observable<T> baseObservable;

            /// <summary>
            /// Function which is used to map the received event to a new one.
            /// If not given, this is the identity function.
            /// </summary>
            private readonly Func<T, T> mapEvent;

            /// <summary>
            /// Constructor.
            /// </summary>
            /// <param name="baseObservable">The observable this observer belongs to.</param>
            /// <param name="modifyEvent">Function which maps from received event to a new one
            /// (default: identity function).</param>
            public ProxyObserver(Observable<T> baseObservable, Func<T, T> modifyEvent = null)
            {
                this.baseObservable = baseObservable;
                mapEvent = modifyEvent ?? (item => item);
            }

            public void OnCompleted()
            {
                // Should never be called.
                throw new NotImplementedException();
            }

            public void OnError(Exception error) => throw error;

            public void OnNext(T value) => baseObservable.Notify(mapEvent(value));

            /// <summary>
            /// Disposes all <see cref="disposables"/>, thereby unsubscribing from all observables.
            /// </summary>
            public void Reset()
            {
                foreach (IDisposable disposable in disposables)
                {
                    disposable.Dispose();
                }

                disposables.Clear();
            }

            /// <summary>
            /// Adds a new <paramref name="disposable"/>. Should be called whenever we subscribe to a new observable.
            /// </summary>
            /// <param name="disposable">The new observable's disposable.</param>
            public void AddDisposable(IDisposable disposable)
            {
                if (!disposables.Contains(disposable))
                {
                    disposables.Add(disposable);
                }
            }
        }
    }
}