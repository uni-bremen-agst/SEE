using System;

namespace SEE.Controls.Actions
{
    /// <summary>
    /// Provides user action that depend upon a particular state the user can be in. 
    /// A user state determines what kinds of actions are triggered for a given
    /// interaction.
    /// </summary>
    [Obsolete("This class will disappear soon.")]
    public static class ActionState
    {
        private static ActionStateType value = ActionStateType.Move;

        private static MobileActionStateType mobileValue = MobileActionStateType.Select;
        
        /// <summary>
        /// The type of the state-based action. Upon changing this type,
        /// the event <see cref="OnStateChangedFn"/> will be triggered with
        /// the currently set action type.
        /// </summary>
        public static ActionStateType Value
        {
            get => value;
            set
            {
                // Note: We will trigger the OnStateChanged even if the same
                // kind of action is to be executed again. This way we can
                // let the user specify points in time at which the original
                // state when a kind of actions was started can be restored.
                //if (!Equals(ActionState.value, value))
                {
                    ActionState.value = value;
                    OnStateChanged?.Invoke(ActionState.value);
                }
            }
        }

        public static MobileActionStateType MobileValue
        {
            get => mobileValue;
            set
            {
                // Note: We will trigger the OnStateChanged even if the same
                // kind of action is to be executed again. This way we can
                // let the user specify points in time at which the original
                // state when a kind of actions was started can be restored.
                //if (!Equals(ActionState.value, value))
                {
                    ActionState.mobileValue = mobileValue;
                    OnMobileStateChanged?.Invoke(ActionState.mobileValue);
                }
            }
        }

        /// <summary>
        /// Whether the given type of the state-based action is currently active.
        /// </summary>
        /// <param name="type">The type to check</param>
        /// <returns><code>true</code> if the given type if currently active,
        /// <code>false</code> otherwise.</returns>
        public static bool Is(ActionStateType type)
        {
            return Equals(value, type);
        }

        /// <summary>
        /// A delegate to be called upon a change of the action state.
        /// </summary>
        /// <param name="value">the new action state</param>
        public delegate void OnStateChangedFn(ActionStateType value);

        public delegate void OnMobileStateChangedFn(MobileActionStateType mobileValue);
        /// <summary>
        /// Event that is triggered when the action is assigned a new action state to.
        /// </summary>
        public static event OnStateChangedFn OnStateChanged;

        public static event OnMobileStateChangedFn OnMobileStateChanged;
    }
}
