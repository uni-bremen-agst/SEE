using SEE.Controls.Actions;
using SEE.Utils;
#if UNITY_ANDROID
#else
using Valve.VR.InteractionSystem;
#endif

namespace SEE.Game.UI.StateIndicator
{
    /// <summary>
    /// Represents an indicator which displays the current <see cref="ActionStateType"/>.
    /// </summary>
    public class ActionStateIndicator : StateIndicator
    {
        /// <summary>
        /// Changes the indicator to display the new action state type.
        /// </summary>
        /// <param name="newState">New state which shall be displayed in the indicator</param>
        public void ChangeActionState(ActionStateType newState)
        {
            if (newState != null)
            {
#if UNITY_ANDROID
                ChangeState(newState.Name, newState.Color.WithAlpha(0.5f));
#else
                ChangeState(newState.Name, newState.Color.ColorWithAlpha(0.5f));
#endif
            }
        }
    }
}