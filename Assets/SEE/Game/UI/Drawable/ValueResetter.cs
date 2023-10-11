using SEE.Controls.Actions.Drawable;
using SEE.Controls.Actions;
using SEE.Game;
using System.Collections;
using UnityEngine;
using SEE.Utils;

namespace Assets.SEE.Game.UI.Drawable
{
    public class ValueResetter : MonoBehaviour
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
                Destroy(gameObject.GetComponent<ValueResetter>());
            }
        }

        private void OnDestroy()
        {
            /*
            if (allowedState == ActionStateTypes.MoveRotator)
            {
                MoveRotatorAction.Reset();
            }

            if (allowedState == ActionStateTypes.DrawShapes && !this.gameObject.CompareTag(Tags.Line))
            {
                DrawShapesAction.Reset();
            }
            */
            if (allowedState == ActionStateTypes.WriteText)
            {
                WriteTextAction.Reset();
            }
        }
    }
}