using SEE.Controls.Actions.Drawable;
using SEE.Controls.Actions;
using SEE.Game;
using SEE.Utils;
using System;
using System.Collections;
using UnityEngine;
using Assets.SEE.Game.UI.Drawable;

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
            if (GlobalActionHistory.Current() != ActionStateTypes.Edit && allowedState == ActionStateTypes.Edit)
            {
                if (!LineMenu.usedIn.Contains(GlobalActionHistory.Current()))
                {
                    LineMenu.disableLineMenu();
                }
                if (!TextMenu.usedIn.Contains(GlobalActionHistory.Current()))
                {
                    TextMenu.disableTextMenu();
                }
            }
            if (GlobalActionHistory.Current() != ActionStateTypes.MoveRotator && allowedState == ActionStateTypes.MoveRotator)
            {
                MoveRotatorAction.Reset();
            }
            if (GlobalActionHistory.Current() != ActionStateTypes.Scale && allowedState == ActionStateTypes.Scale)
            {
                ScaleAction.Reset();
            }
            if (GlobalActionHistory.Current() != ActionStateTypes.MovePoint && allowedState == ActionStateTypes.MovePoint)
            {
                MovePointAction.Reset();
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
            }
            else
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
                renderer = line.GetComponent<Renderer>();
                collider = line.GetComponent<MeshCollider>();
            }
            loopOn = true;
            StartCoroutine(Blink());
        }

        private void Activate()
        {
            renderer = gameObject.GetComponent<Renderer>();
            loopOn = true;
            StartCoroutine(Blink());
        }
    }
}