using System;
using System.Collections.Generic;

namespace SEE.DataModel
{
    public abstract partial class Observable<T>
    {
        /// <summary>
        /// An observer which acts as a proxy for a <see cref="BaseObservable"/>.
        ///
        /// This observer can subscribe to other observables of the same type as the <see cref="BaseObservable"/>,
        /// and transparently route all of their events as a notification to the <see cref="BaseObservable"/>.
        /// </summary>
        protected class ProxyObserver : IObserver<T>
        {
            /// <summary>
            /// Set of all disposables associated with the observables this observer observes.
            /// </summary>
            private readonly ISet<IDisposable> Disposables = new HashSet<IDisposable>();

            /// <summary>
            /// The observable this observer belongs to.
            /// </summary>
            private readonly Observable<T> BaseObservable;

            /// <summary>
            /// Function which is used to map the received event to a new one.
            /// If not given, this is the identity function.
            /// </summary>
            private readonly Func<T, T> MapEvent;

            /// <summary>
            /// Constructor.
            /// </summary>
            /// <param name="baseObservable">the observable this observer belongs to</param>
            /// <param name="modifyEvent">function which maps from received event to a new one
            /// (default: identity function)</param>
            public ProxyObserver(Observable<T> baseObservable, Func<T, T> modifyEvent = null)
            {
                BaseObservable = baseObservable;
                MapEvent = modifyEvent ?? (item => item);
            }

            public void OnCompleted()
            {
                // Should never be called.
                throw new NotImplementedException();
            }

            public void OnError(Exception error) => throw error;

            public void OnNext(T value) => BaseObservable.Notify(MapEvent(value));

            /// <summary>
            /// Disposes all <see cref="Disposables"/>, thereby unsubscribing from all observables.
            /// </summary>
            public void Reset()
            {
                foreach (IDisposable disposable in Disposables)
                {
                    disposable.Dispose();
                }

                Disposables.Clear();
            }

            /// <summary>
            /// Adds a new <paramref name="disposable"/>. Should be called whenever we subscribe to a new observable.
            /// </summary>
            /// <param name="disposable">the new observable's disposable</param>
            public void AddDisposable(IDisposable disposable)
            {
                if (!Disposables.Contains(disposable))
                {
                    Disposables.Add(disposable);
                }
            }
        }
    }
}