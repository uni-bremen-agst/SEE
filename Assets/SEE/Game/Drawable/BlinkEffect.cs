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
using HighlightPlus;

namespace Assets.SEE.Game.Drawable
{
    /// <summary>
    /// Component that is required to show which 
    /// drawable type object has been selected. It makes the respective object blink.
    /// </summary>
    public class BlinkEffect : MonoBehaviour
    {
        /// <summary>
        /// Represents the status of whether the loop is active.
        /// </summary>
        private bool loopOn;
        /// <summary>
        /// The render of the attached game object.
        /// </summary>
        new Renderer renderer;
        /// <summary>
        /// The renders of the attached game object (for mind map nodes)
        /// </summary>
        Renderer[] renderers;
        /// <summary>
        /// The canvas of the attached game object.
        /// </summary>
        Canvas canvas;
        /// <summary>
        /// The highlight effect of the attached game object.
        /// </summary>
        HighlightEffect highlight;

        /// <summary>
        /// Executed as long as the Blink Effect Component is active.
        /// It ensures that the corresponding renderer/canvas/highlight effect is toggled on and off, thus creating a blinking effect.
        /// </summary>
        /// <returns>Nothing, only the seconds to wait.</returns>
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
                else if(renderers != null)
                {
                    foreach(Renderer renderer in renderers)
                    {
                        renderer.enabled = false;
                    }
                    yield return new WaitForSeconds(0.2f);
                    foreach (Renderer renderer in renderers)
                    {
                        renderer.enabled = true;
                    }
                    yield return new WaitForSeconds(0.5f);
                }
                else if (canvas != null)
                {
                    canvas.enabled = false;
                    yield return new WaitForSeconds(0.2f);
                    canvas.enabled = true;
                    yield return new WaitForSeconds(0.5f);
                }
                else
                {
                    highlight.enabled = false;
                    yield return new WaitForSeconds(0.2f);
                    highlight.enabled = true;
                    yield return new WaitForSeconds(0.5f);
                }
            }
        }

        /// <summary>
        /// Deactivates the blink effect.
        /// It enables the renderer, the canvas, or the child renderers (depending on what is present).
        /// If a highlight effect was used it will destroyed.
        /// Subsequently, the Blink Effect Component is destroyed.
        /// </summary>
        public void Deactivate()
        {
            loopOn = false;
            if (renderer != null)
            {
                renderer.enabled = true;
            }
            else if (renderers != null)
            {
                foreach (Renderer renderer in renderers)
                {
                    renderer.enabled = true;
                }
            }
            else if (canvas != null)
            {
                canvas.enabled = true;
            }
            else
            {
                Destroyer.Destroy(highlight);
            }
            Destroy(this);
        }

        /// <summary>
        /// Executed upon assigning the component. 
        /// It searches for a renderer, child renderers, or a highlight effect. 
        /// If none of these components are present, a highlight effect is created, 
        /// and then the blink loop is initiated.
        /// </summary>
        private void Start()
        {
            GameObject obj = this.gameObject;
            if (renderer == null && obj.GetComponent<Renderer>() != null)
            {
                renderer = obj.GetComponent<Renderer>();
            }
            else if (obj.GetComponentsInChildren<Renderer>().Length > 0)
            {
                renderers = obj.GetComponentsInChildren<Renderer>();
            }
            else if (obj.GetComponent<Canvas>() != null)
            {
                canvas = obj.GetComponent<Canvas>();
            } else if (obj.GetComponent<HighlightEffect>() != null)
            {
                highlight = obj.GetComponent<HighlightEffect>();
            } else
            {
                highlight = GameHighlighter.Enable(obj);
            }
            loopOn = true;
            StartCoroutine(Blink());
        }
    }
}