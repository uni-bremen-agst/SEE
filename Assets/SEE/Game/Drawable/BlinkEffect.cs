using Assets.SEE.Controls.Actions.Drawable;
using SEE.Controls.Actions;
using SEE.Game;
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
        private MeshCollider collider;


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
            if (collider != null && collider.convex)
            {
                collider.isTrigger = false;
                collider.convex = false;
            } 
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
            if (renderer == null && line.CompareTag(Tags.Line))
            {
                renderer = line.GetComponent<LineRenderer>();
                collider = line.GetComponent<MeshCollider>();
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