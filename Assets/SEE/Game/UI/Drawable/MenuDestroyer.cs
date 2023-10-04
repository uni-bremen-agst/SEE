using SEE.Controls.Actions.Drawable;
using Assets.SEE.Game.Drawable;
using SEE.Controls.Actions;
using SEE.Game;
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
                if (this.gameObject.CompareTag(Tags.Line))
                {
                    Destroy(this.gameObject.transform.parent.gameObject);
                }
                else
                {
                    Destroy(this.gameObject);
                }
            }
        }

        private void OnDestroy()
        {
            if (allowedState == ActionStateTypes.MoveRotator)
            {
                MoveRotatorAction.Reset();
            }
            
            if (allowedState == ActionStateTypes.DrawShapes && !this.gameObject.CompareTag(Tags.Line))
            {
                DrawShapesAction.Reset();
            }
        }
    }
}