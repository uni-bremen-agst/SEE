namespace SEE.Controls.Actions.HolisticMetrics
{
    /// <summary>
    /// This class is the base class for all holistic metric actions. It defines fields that are needed to maintain a
    /// command history.
    /// </summary>
    internal abstract class Action
    {
        /// <summary>
        /// Executes this action and adds it to the history. This should be called when executing the action for the
        /// first time.
        /// </summary>
        internal void Execute()
        {
            Do();
            HolisticMetricsActionHistory.Add(this);
        }

        /// <summary>
        /// Executes this action (again). This should only be called from the history (and is also called from this
        /// class).
        /// </summary>
        internal abstract void Do();
        
        /// <summary>
        /// Reverts this action. This should only be called from the history.
        /// </summary>
        internal abstract void Undo();
    }
}