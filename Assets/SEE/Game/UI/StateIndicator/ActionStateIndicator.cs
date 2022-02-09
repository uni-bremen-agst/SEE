using SEE.Controls.Actions;
using SEE.Utils;

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
                ChangeState(newState.Name, newState.Color.WithAlpha(0.5f));
                ChangeState(newState.Name, newState.Color.WithAlpha(0.5f));
            }
        }
    }
}