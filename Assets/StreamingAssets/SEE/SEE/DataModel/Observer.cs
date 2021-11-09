namespace SEE.DataModel
{
    /// <summary>
    /// The event information about the change of the state of the observed subject.
    /// This class is intended to be specialized for more specific change events.
    /// </summary>
    public abstract class ChangeEvent
    {
        public override string ToString()
        {
            return GetType().Name;
        }
    }

    /// <summary>
    /// Common interface of all observers of instances of Observable.
    /// </summary>
    public interface Observer
    {
        /// <summary>
        /// This method is intended to be called by the Observable when its state has
        /// changed. The given parameter gives more details about the change.
        /// </summary>
        /// <param name="changeEvent">details about the change of the state</param>
        void Update(ChangeEvent changeEvent);
    }
}