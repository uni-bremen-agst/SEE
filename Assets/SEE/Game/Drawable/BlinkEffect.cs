using SEE.Controls.Actions.Drawable;
using SEE.Controls.Actions;
using SEE.Game;
using SEE.Utils;
using System;
using System.Collections;
using UnityEngine;
using Assets.SEE.Game.UI.Drawable;
using SEE.Game.Drawable.Configurations;

namespace Assets.SEE.Game.Drawable
{
    public class BlinkEffect : MonoBehaviour
    {
        private bool loopOn;
        private ActionStateType allowedState;
        new Renderer renderer;
        new MeshCollider collider;


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

                if (allowedState != null && GlobalActionHistory.Current() != allowedState)
                {
                    Deactivate();
                }
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

        public bool GetLoopStatus()
        {
            return loopOn;
        }

        private void Start()
        {
            GameObject obj = this.gameObject;
            if (renderer == null && obj.GetComponent<Renderer>() != null)
            {
                renderer = obj.GetComponent<Renderer>();
            }
            if (collider == null && obj.GetComponent<MeshCollider>() != null)
            {
                collider = obj.GetComponent<MeshCollider>();
            }
            loopOn = true;
            StartCoroutine(Blink());
        }
    }
}