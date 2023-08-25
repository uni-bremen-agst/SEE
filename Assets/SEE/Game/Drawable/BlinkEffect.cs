using Assets.SEE.Controls.Actions.Drawable;
using SEE.Controls.Actions;
using SEE.Utils;
using System;
using System.Collections;
using UnityEngine;

namespace Assets.SEE.Game.Drawable
{
    public class BlinkEffect : MonoBehaviour
    {
        private bool loopOn;
        private ActionStateType allowedState;
        new Renderer renderer;


        public void SetAllowedActionStateType(ActionStateType allowedState)
        {
            this.allowedState = allowedState;
        }

        IEnumerator Blink()
        {
            while (loopOn)
            {
                renderer.enabled = false;
                yield return new WaitForSeconds(0.2f);
                renderer.enabled = true;
                yield return new WaitForSeconds(0.5f);
                
                if (GlobalActionHistory.Current() != allowedState)
                {
                    Deactivate();
                }
            }
        }

        private void OnDestroy()
        {
            if (GlobalActionHistory.Current() != ActionStateTypes.EditLine)
            {
                DrawableHelper.disableDrawableMenu();
            }
        }

        public void Deactivate()
        {
            loopOn = false;
            renderer.enabled = true;
            Destroy(this);
        }

        public void LoopReverse()
        {
            loopOn = !loopOn;
            if (loopOn)
            {
                Activate();
            } else
            {
                Deactivate();
            }
        }

        public bool GetLoopStatus()
        {
            return loopOn;
        }

        public void Activate(GameObject line)
        {
            if (renderer == null)
            {
                renderer = line.GetComponent<LineRenderer>();
            }
            loopOn = true;
            StartCoroutine(Blink());
        }

        private void Activate()
        {
            renderer = gameObject.GetComponent<LineRenderer>();
            loopOn = true;
            StartCoroutine(Blink());
        }
    }
}