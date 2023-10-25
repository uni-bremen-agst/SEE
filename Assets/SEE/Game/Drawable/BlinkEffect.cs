using SEE.Controls.Actions.Drawable;
using SEE.Controls.Actions;
using SEE.Game;
using SEE.Utils;
using System;
using System.Collections;
using UnityEngine;
using Assets.SEE.Game.UI.Drawable;
using SEE.Game.Drawable.Configurations;
using UnityEngine.UI;

namespace Assets.SEE.Game.Drawable
{
    /// <summary>
    /// Component that is required to show which 
    /// drawable type object has been selected. It makes the respective object blink.
    /// </summary>
    public class BlinkEffect : MonoBehaviour
    {
        private bool loopOn;
        private ActionStateType allowedState;
        new Renderer renderer;
        Canvas canvas;
        new MeshCollider collider;


        public void SetAllowedActionStateType(ActionStateType allowedState)
        {
            this.allowedState = allowedState;
        }

        IEnumerator Blink()
        {
            while (loopOn)
            {
                if (renderer != null)
                {
                    renderer.enabled = false;
                    yield return new WaitForSeconds(0.2f);
                    renderer.enabled = true;
                    yield return new WaitForSeconds(0.5f);
                }
                else
                {
                    canvas.enabled = false;
                    yield return new WaitForSeconds(0.2f);
                    canvas.enabled = true;
                    yield return new WaitForSeconds(0.5f);
                }

                if (allowedState != null && GlobalActionHistory.Current() != allowedState)
                {
                    Deactivate();
                }
            }
        }

        public void Deactivate()
        {
            loopOn = false;
            if (renderer != null)
            {
                renderer.enabled = true;
            }
            else
            {
                canvas.enabled = true;
            }
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
            else if (obj.GetComponent<Canvas>() != null)
            {
                canvas = obj.GetComponent<Canvas>();
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