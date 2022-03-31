namespace SEE.DataModel
{
    /// <summary>
    /// The event information about the change of the state of the observed subject.
    /// This class is intended to be specialized for more specific change events.
    /// </summary>
    public abstract class ChangeEvent
    {
        /// <summary>
        /// A textual representation of the event.
        /// Must be human-readable, distinguishable from other <see cref="ChangeEvent"/>s, and
        /// contain all relevant information about the event.
        /// The name of the class needn't be included, as <see cref="ToString"/> will contain it.
        /// </summary>
        /// <returns>Textual representation of the event.</returns>
        protected abstract string Description();

        public override string ToString() => $"{GetType().Name}: {Description()}";
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