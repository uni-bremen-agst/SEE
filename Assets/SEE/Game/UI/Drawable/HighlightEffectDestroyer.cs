using HighlightPlus;
using SEE.Controls.Actions;
using SEE.Controls.Actions.Drawable;
using System.Collections;
using UnityEngine;

namespace Assets.SEE.Game.UI.Drawable
{
    /// <summary>
    /// This class destroys a highlight effect component, when the allowed state is leaving.
    /// </summary>
    public class HighlightEffectDestroyer : MonoBehaviour
    {
        /// <summary>
        /// The allowed action state for the highlight effect component
        /// </summary>
        private ActionStateType allowedState;

        /// <summary>
        /// Method to set the allowed action state.
        /// </summary>
        /// <param name="state">The allowed state</param>
        public void SetAllowedState(ActionStateType state)
        {
            this.allowedState = state;
        }

        /// <summary>
        /// Checks every update if the current selected action state is the allowed action state type.
        /// If not, the highlight effect component will be destroyed and this component too.
        /// Also it resets the values for the <see cref="SaveAction"/> if the allowed action state was the save state.
        /// </summary>
        private void Update()
        {
            if (GlobalActionHistory.Current() != allowedState)
            {
                Destroy(gameObject.GetComponent<HighlightEffect>());
                Destroy(this);
                if (allowedState == ActionStateTypes.Save)
                {
                    SaveAction.Reset();
                }
            }
        }
    }
}