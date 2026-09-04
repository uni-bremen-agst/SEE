using HighlightPlus;
using SEE.GO;
using SEE.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SEE.Game.Drawable
{
    /// <summary>
    /// Component that is required to show which object has been selected.
    /// It makes the respective object blink.
    /// </summary>
    public class BlinkEffect : MonoBehaviour
    {
        /// <summary>
        /// Whether the loop is active.
        /// </summary>
        private bool loopOn;

        /// <summary>
        /// The renderer of the attached game object.
        /// </summary>
        private new Renderer renderer;

        /// <summary>
        /// The renderers of the attached game object (for mind map nodes)
        /// </summary>
        private List<Renderer> renderers;

        /// <summary>
        /// The canvas of the attached game object.
        /// </summary>
        private Canvas canvas;

        /// <summary>
        /// The highlight effect of the attached game object.
        /// </summary>
        private HighlightEffect highlight;

        /// <summary>
        /// How long the attached object stays invisible within one blink.
        /// </summary>
        /// <remarks>A <see cref="WaitForSeconds"/> is immutable, hence a single
        /// instance can be shared by all blink effects instead of being created
        /// anew for every blink.</remarks>
        private static readonly WaitForSeconds invisibleDuration = new(0.2f);

        /// <summary>
        /// How long the attached object stays visible within one blink.
        /// </summary>
        /// <remarks>Shared for the same reason as <see cref="invisibleDuration"/>.</remarks>
        private static readonly WaitForSeconds visibleDuration = new(0.5f);

        /// <summary>
        /// Shows or hides whatever the attached game object blinks with: its
        /// renderer, its child renderers, its canvas, or its highlight effect.
        /// </summary>
        /// <remarks>Which of the four applies is decided once in <see cref="Start"/>,
        /// because it cannot change afterwards.</remarks>
        private Action<bool> setVisible;

        /// <summary>
        /// Executed as long as the Blink Effect Component is active.
        /// It ensures that the corresponding renderer/canvas/highlight effect
        /// is toggled on and off, thus creating a blinking effect.
        /// </summary>
        /// <returns>Nothing, only the seconds to wait.</returns>
        private IEnumerator Blink()
        {
            while (loopOn)
            {
                setVisible(false);
                yield return invisibleDuration;
                setVisible(true);
                yield return visibleDuration;
            }
        }

        /// <summary>
        /// Enables or disables every renderer in <see cref="renderers"/>.
        /// </summary>
        /// <param name="enable">Whether the renderers are to be enabled.</param>
        private void EnableRenderers(bool enable)
        {
            foreach (Renderer renderer in renderers)
            {
                if (renderer != null)
                {
                    renderer.enabled = enable;
                }
                else
                {
                    renderers.Remove(renderer);
                    break;
                }
            }
        }

        /// <summary>
        /// Deactivates the blink effect.
        /// It enables the renderer, the canvas, or the child renderers
        /// (depending on what is present).
        /// If a highlight effect was used, it will be destroyed.
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
                    if (renderer != null)
                    {
                        renderer.enabled = true;
                    }
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
        /// Deactivate the blink effect of the given object.
        /// </summary>
        /// <param name="obj">The object which blink effect should be deactivated.</param>
        public static void Deactivate(GameObject obj)
        {
            if (obj != null && obj.GetComponent<BlinkEffect>() != null)
            {
                obj.GetComponent<BlinkEffect>().Deactivate();
            }
        }

        /// <summary>
        /// Executed upon assigning the component.
        /// It searches for a renderer, child renderers, or a highlight effect.
        /// If none of these components are present, a highlight effect is created,
        /// and then the blink loop is initiated.
        /// </summary>
        private void Start()
        {
            GameObject obj = gameObject;

            if (renderer == null && obj.GetComponent<Renderer>() != null
                && obj.GetComponentsInChildren<Renderer>().Length == 1)
            {
                /// Sets the renderer if available.
                renderer = obj.GetComponent<Renderer>();
                setVisible = visible => renderer.enabled = visible;
            }
            else if (obj.GetComponentsInChildren<Renderer>().Length > 0)
            {
                /// Sets the renderers if available.
                renderers = obj.GetComponentsInChildren<Renderer>().ToList();
                setVisible = EnableRenderers;
            }
            else if (obj.GetComponent<Canvas>() != null)
            {
                /// Sets the canvas if available.
                /// Needed for an image.
                canvas = obj.GetComponent<Canvas>();
                setVisible = visible => canvas.enabled = visible;
            }
            else if (obj.GetComponent<HighlightEffect>() != null)
            {
                /// Sets the highlight if available.
                highlight = obj.GetComponent<HighlightEffect>();
                setVisible = visible => highlight.enabled = visible;
            }
            else
            {
                /// Creates a highlight effect, if none of the other cases apply.
                highlight = Highlighter.EnableGlowOutline(obj);
                setVisible = visible => highlight.enabled = visible;
            }
            loopOn = true;
            StartCoroutine(Blink());
        }

        /// <summary>
        /// Removes the renderer of the fill out.
        /// </summary>
        /// <param name="obj">The object which has a fill out.</param>
        public static void RemoveFillOutFromEffect(GameObject obj)
        {
            if (obj != null && obj.GetComponent<BlinkEffect>() != null)
            {
                GameObject fillOut = obj.FindDescendant(ValueHolder.FillOut);
                BlinkEffect effect = obj.GetComponent<BlinkEffect>();
                if (fillOut != null)
                {
                    effect.renderers.Remove(fillOut.GetComponent<Renderer>());
                }
            }
        }

        /// <summary>
        /// Adds the renderer of the fill out.
        /// </summary>
        /// <param name="obj">The object which has a fill out.</param>
        public static void AddFillOutToEffect(GameObject obj)
        {
            if (obj != null && (obj.GetComponent<BlinkEffect>() != null
                    || obj.GetComponentInParent<BlinkEffect>() != null))
            {
                GameObject fillOut = obj.FindDescendant(ValueHolder.FillOut);
                BlinkEffect effect = obj.GetComponent<BlinkEffect>() ?? obj.GetComponentInParent<BlinkEffect>();
                if (fillOut != null && fillOut.GetComponent<Renderer>() != null)
                {
                    if (effect.renderers != null)
                    {
                        effect.renderers.Add(fillOut.GetComponent<Renderer>());
                    }
                    else if (effect.renderer != null)
                    {
                        effect.Deactivate();
                        obj.AddComponent<BlinkEffect>();
                    }
                }
            }
        }

        /// <summary>
        /// Returns true if the effect contains the fill out renderer.
        /// </summary>
        /// <param name="obj">The object which has a fill out.</param>
        /// <returns>True if the blink effect is active for the fill out, otherwise false.</returns>
        public static bool CanFillOutBeAdded(GameObject obj)
        {
            BlinkEffect effect = obj.GetComponent<BlinkEffect>() ?? obj.GetComponentInParent<BlinkEffect>();
            if (obj != null && effect != null
                && effect.renderers != null
                && obj.FindDescendant(ValueHolder.FillOut) != null)
            {
                return !effect.renderers.Contains(obj.FindDescendant(ValueHolder.FillOut).GetComponent<Renderer>());
            }
            else if (obj != null && effect != null
                && effect.renderer != null)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Restarts the blinking coroutine when the object becomes active again.
        /// This is needed because page changes can deactivate and reactivate drawable objects.
        /// </summary>
        private void OnEnable()
        {
            if (renderer != null || renderers != null || canvas != null || highlight != null)
            {
                loopOn = true;
                StartCoroutine(Blink());
            }
        }

        /// <summary>
        /// Stops blinking while the object is inactive and restores the visible state.
        /// </summary>
        private void OnDisable()
        {
            loopOn = false;

            if (renderer != null)
            {
                renderer.enabled = true;
            }
            else if (renderers != null)
            {
                foreach (Renderer childRenderer in renderers.Where(r => r != null))
                {
                    childRenderer.enabled = true;
                }
            }
            else if (canvas != null)
            {
                canvas.enabled = true;
            }
            else if (highlight != null)
            {
                highlight.enabled = true;
            }
        }
    }
}
