using System;
using System.Collections.Generic;

namespace SEE.DataModel
{
    /// <summary>
    /// Represents an observable class, that is, a class which emits events that subscribers can be notified of.
    /// Observers need to implement <see cref="IObserver{T}"/> and register themselves using <see cref="Subscribe"/>.
    /// Unsubscribing works by calling <c>Dispose()</c> on the returned <see cref="IDisposable"/>.
    /// Observables may also emit errors or notify observers of completion, but this is not necessarily implemented.
    /// </summary>
    /// <typeparam name="T">the type of the event that is emitted to observers</typeparam>
    public abstract partial class Observable<T>: IObservable<T>
    {
        /// <summary>
        /// The list of currently registered observers that need to be notified upon a change of the state.
        /// </summary>
        private readonly IList<IObserver<T>> Observers = new List<IObserver<T>>();
        
        /// <summary>
        /// Registers a new subscriber for this observable.
        /// </summary>
        /// <param name="observer">The new observer which shall subscribe to this observable</param>
        /// <returns>A disposable which can be used to unsubscribe from this observable</returns>
        public IDisposable Subscribe(IObserver<T> observer)
        {
            if (!Observers.Contains(observer))
            {
                Observers.Add(observer);
            }
            return new Unsubscriber<T>(Observers, observer);
        }
        
        /// <summary>
        /// A disposable responsible for unsubscribing an observer from this observable.
        /// </summary>
        /// <typeparam name="U">type of the event emitted by the observer</typeparam>
        private class Unsubscriber<U>: IDisposable
        {
            /// <summary>
            /// The list of currently registered observers.
            /// </summary>
            private readonly IList<IObserver<U>> Observers;
            
            /// <summary>
            /// The observer which is disposable by this class.
            /// </summary>
            private readonly IObserver<U> Observer;

            public Unsubscriber(IList<IObserver<U>> observers, IObserver<U> observer)
            {
                Observers = observers;
                Observer = observer;
            }

            /// <summary>
            /// Disposes the registered <see cref="Observer"/> by removing it
            /// from the list of active <see cref="Observers"/>
            /// </summary>
            public void Dispose()
            {
                if (Observers != null && Observers.Contains(Observer))
                {
                    Observers.Remove(Observer);
                }
            }
        }

        /// <summary>
        /// Notifies all registered observers with given change information about a change of the
        /// state. This method must be called whenever a change of the state occurs.
        /// </summary>
        /// <param name="change">information about the change of the state to be passed on to the observers</param>
        protected void Notify(T change)
        {
            foreach (IObserver<T> observer in Observers)
            {
                observer.OnNext(change);
            }
        }

        /// <summary>
        /// Notifies all registered observers about the completion of event emission.
        /// After this method is called, no further notifications shall take place.
        /// </summary>
        protected void NotifyComplete()
        {
            foreach (IObserver<T> observer in Observers)
            {
                observer.OnCompleted();
            }
        }

        /// <summary>
        /// Notifies all registered observers with given information about an error that occurred.
        /// </summary>
        /// <param name="error">information about the error that occurred</param>
        protected void NotifyError(Exception error)
        {
            foreach (IObserver<T> observer in Observers)
            {
                observer.OnError(error);
            }
        }
    }
}