using SEE.Controls.Actions;
using System.Collections;
using UnityEngine;

namespace Assets.SEE.Game.UI.Drawable
{
    public class DrawableLineMenuDestroyer : MonoBehaviour
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
    }
}