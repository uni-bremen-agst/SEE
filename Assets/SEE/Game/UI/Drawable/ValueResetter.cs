using SEE.Controls.Actions.Drawable;
using SEE.Controls.Actions;
using SEE.Game;
using System.Collections;
using UnityEngine;
using SEE.Utils;
using Assets.SEE.Controls.Actions.Drawable;

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