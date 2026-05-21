using SEE.Controls.Actions;
using SEE.Controls.Actions.Drawable;
using SEE.Game.Drawable.Configurations;
using UnityEngine;

namespace SEE.UI.Drawable
{
    /// <summary>
    /// This class ensures that the static attributes of an action will be resetted after
    /// an actionstate type change.
    /// </summary>
    public class ValueResetter : MonoBehaviour
    {
        /// <summary>
        /// The allowed state type for the action.
        /// </summary>
        private ActionStateType allowedState;

        /// <summary>
        /// Sets the allowed state.
        /// </summary>
        /// <param name="state">The allowed state.</param>
        public void SetAllowedState(ActionStateType state)
        {
            allowedState = state;
        }

        /// <summary>
        /// Checks every frame if the action state has changes.
        /// If it changes and it's not the allowed state, this component will be destroyed.
        /// </summary>
        private void Update()
        {
            if (GlobalActionHistory.Current() != allowedState)
            {
                Destroy(gameObject.GetComponent<ValueResetter>());
            }
        }

        /// <summary>
        /// If this component will be destroyed, it calls the reset method for the
        /// appropriate action.
        /// The static attributes are needed in the actions to select a new
        /// <see cref="DrawableType"/> object directly after the action.
        /// </summary>
        private void OnDestroy()
        {
            if (allowedState == ActionStateTypes.WriteText)
            {
                WriteTextAction.Reset();
            }

            if (allowedState == ActionStateTypes.Edit)
            {
                EditAction.Reset();
            }

            if (allowedState == ActionStateTypes.CutCopyPaste)
            {
                CutCopyPasteAction.Reset();
            }

            if (allowedState == ActionStateTypes.Scale)
            {
                ScaleAction.Reset();
            }

            if (allowedState == ActionStateTypes.LayerChanger)
            {
                LayerChangeAction.Reset();
            }
        }
    }
}
