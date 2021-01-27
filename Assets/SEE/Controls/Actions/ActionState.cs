namespace SEE.Controls.Actions
{
    /// <summary>
    /// Provides user action that depend upon a particular state the user can be in. 
    /// A user state determines what kinds of actions are triggered for a given
    /// interaction.
    /// </summary>
    public static class ActionState
    {
        /// <summary>
        /// The type of a state-based action.
        /// </summary>
        public enum Type
        {
            Move,    // the user wants to move a node
            Rotate,  // the user wants to rotate a node
            Map      // the user wants to map an implementation node onto an architecture node (reflexion analysis)
        }

        private static Type value = 0;
        /// <summary>
        /// The type of the state-based action. Upon changing this type,
        /// the event <see cref="OnStateChangedFn"/> will be triggered with
        /// the currently set action type.
        /// </summary>
        public static Type Value
        {
            get => value;
            set
            {
                if (ActionState.value != value)
                {
                    ActionState.value = value;
                    OnStateChanged?.Invoke(ActionState.value);
                }
            }
        }

        /// <summary>
        /// A delegate to be called upon a change of the action state. 
        /// </summary>
        /// <param name="value">the new action state</param>
        public delegate void OnStateChangedFn(Type value);
        /// <summary>
        /// Event that is triggered when the action is assigned a new action state to.
        /// </summary>
        public static event OnStateChangedFn OnStateChanged;
    }
}
