using Assets.SEE.Controls.Actions.Drawable;
using SEE.Controls.Actions;
using System.Collections;
using UnityEngine;

namespace Assets.SEE.Game.UI.Drawable
{
    public class MenuDestroyer : MonoBehaviour
    {
        private ActionStateType allowedState;


        public void SetAllowedState(ActionStateType state)
        {
            this.allowedState = state;
        }

        private void Update()
        {
            if (GlobalActionHistory.Current() != allowedState)
            {
                Destroy(this.gameObject);
            }
        }

        private void OnDestroy()
        {
            if (allowedState == ActionStateTypes.MoveRotator)
            {
                MoveRotatorAction.Reset();
            }
        }
    }
}