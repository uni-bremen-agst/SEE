using System.Collections.Generic;

namespace SEE.DataModel
{
    public abstract class Observable
    {
        /// <summary>
        /// The list of currently registered observers that need to be notified
        /// upon a change of the state.
        /// </summary>
        private readonly IList<Observer> observers = new List<Observer>();

        /// <summary>
        /// Registers given observer. The observer will then be notified via
        /// its Update(ChangeState) message when a change occurs.
        /// </summary>
        /// <param name="observer">observer to be registered</param>
        public void Register(Observer observer)
        {
            observers.Add(observer);
        }

        /// <summary>
        /// Unregisters given observer. The observer will no longer be notified when 
        /// a change occurs.
        /// </summary>
        /// <param name="observer">observer to be unregistered</param>
        public void Deregister(Observer observer)
        {
            observers.Remove(observer);
        }

        /// <summary>
        /// Notifies all registered observers with given change information about a change of the
        /// state. This method must be called whenever a change of the state occurs.
        /// </summary>
        /// <param name="change">information about the change of the state to be passed on to the 
        /// observers</param>
        protected void Notify(ChangeEvent change)
        {
            foreach (Observer observer in observers)
            {
                observer.HandleChange(change);
            }
        }
    }
}