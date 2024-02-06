using SEE.DataModel.DG;
using Sirenix.Utilities;
using System;
using System.Collections.Generic;
using System.Text;

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
        private IList<IObserver<T>> observers = new List<IObserver<T>>();

        /// <summary>
        /// If set to true, no notifications will be sent to observers.
        /// Note that this only suppresses notifications themselves, not errors or completion.
        /// </summary>
        public bool SuppressNotifications { get; set; } = false;

        /// <summary>
        /// If set to true, events are not sent directly to the subscribers, but cached within <see cref="cachedChanges"/>.
        /// After <see cref="StartCaching"/> is called this property should be set to true.
        /// After <see cref="ReleaseCaching"/> is called this property should be set to false.
        /// </summary>
        private bool CachingActive { get; set; }

        /// <summary>
        /// Contains cached events.
        /// </summary>
        private IList<T> cachedChanges = new List<T>();

        /// <summary>
        /// Registers a new subscriber for this observable.
        /// </summary>
        /// <param name="observer">The new observer which shall subscribe to this observable</param>
        /// <returns>A disposable which can be used to unsubscribe from this observable</returns>
        public IDisposable Subscribe(IObserver<T> observer)
        {
            if (!observers.Contains(observer))
            {
                observers.Add(observer);
            }
            return new Unsubscriber<T>(observers, observer);
        }

        public string DumpSubscriptions() 
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("Observers: " + Environment.NewLine);
            foreach(IObserver<T> observer in observers)
            {
                stringBuilder.Append(observer.ToString() + Environment.NewLine);
            }
            return stringBuilder.ToString();
        }

        /// <summary>
        /// This methods handles the cloning of an Oberservable object. If an Observable object
        /// is cloned, the registered observers are not copied to the new Observable object.
        /// </summary>
        /// <param name="clone"></param>
        protected void HandleCloned(object clone)
        {
            Observable<T> observable = (Observable<T>)clone;
            observable.observers = new List<IObserver<T>>();
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
            private readonly IList<IObserver<U>> observers;

            /// <summary>
            /// The observer which is disposable by this class.
            /// </summary>
            private readonly IObserver<U> observer;

            public Unsubscriber(IList<IObserver<U>> observers, IObserver<U> observer)
            {
                this.observers = observers;
                this.observer = observer;
            }

            /// <summary>
            /// Disposes the registered <see cref="observer"/> by removing it
            /// from the list of active <see cref="observers"/>
            /// </summary>
            public void Dispose()
            {
                if (observers != null && observers.Contains(observer))
                {
                    observers.Remove(observer);
                }
            }
        }

        /// <summary>
        /// Activates the caching of changes. After this method is called changes are not directly 
        /// sent to the observers, but cached within <see cref="cachedChanges"/>.
        /// Postcondition: <see cref="CachingActive"/> is true. 
        /// </summary>
        public void StartCaching()
        {
            CachingActive = true;
        }

        /// <summary>
        /// This method Stops the caching of events. 
        /// After this method is called all subscribed oberservers are notified 
        /// about the cached changes.
        /// Postcondition: <see cref="CachingActive"/> is false. 
        ///                <see cref="cachedChanges"/> should be empty.
        /// </summary>
        public void ReleaseCaching()
        {
            CachingActive = false;
            foreach (T cachedChange in cachedChanges)
            {
                this.Notify(cachedChange);
            }
            cachedChanges.Clear();
        }

        /// <summary>
        /// Notifies all registered observers with given change information about a change of the
        /// state. But if <see cref="CachingActive"/> is set to true, the method first caches 
        /// reveived changes and does not directly notify the observers.
        /// This method must be called whenever a change of the state occurs.
        /// </summary>
        /// <param name="change">information about the change of the state to be passed on to the observers</param>
        protected void Notify(T change)
        {
            if (SuppressNotifications)
            {
                return;
            }

            if (CachingActive)
            {
                this.cachedChanges.Add(change);
                return;
            }

            foreach (IObserver<T> observer in observers)
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
            foreach (IObserver<T> observer in observers)
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
            foreach (IObserver<T> observer in observers)
            {
                observer.OnError(error);
            }
        }
    }
}