using SEE.Controls.Actions.Drawable;
using SEE.Controls.Actions;
using SEE.Game;
using System.Collections;
using UnityEngine;
using SEE.Utils;
using Assets.SEE.Controls.Actions.Drawable;

namespace Assets.SEE.Game.UI.Drawable
{
    /// <summary>
    /// This class ensures that the static attributes of an action will be resetted after a actionstate type change.
    /// </summary>
    public class ValueResetter : MonoBehaviour
    {
        /// <summary>
        /// The allowed state type for the action.
        /// </summary>
        private ActionStateType allowedState;

        /// <summary>
        /// Metho to set the allowed state.
        /// </summary>
        /// <param name="state">the allowed state</param>
        public void SetAllowedState(ActionStateType state)
        {
            this.allowedState = state;
        }

        /// <summary>
        /// Checks every frame if the action state has changes.
        /// If it changes and it's not the allowed state this component will be destroyed.
        /// </summary>
        private void Update()
        {
            if (GlobalActionHistory.Current() != allowedState)
            {
                Destroy(gameObject.GetComponent<ValueResetter>());
            }
        }

        /// <summary>
        /// If this component will be destroyed it calls the reset method for the
        /// appropriate action.
        /// The static attributes are needed in the actions to select a new 
        /// drawable type object directly after the action.
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
        }
    }
}