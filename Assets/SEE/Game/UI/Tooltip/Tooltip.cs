using System;
using System.Linq;
using DG.Tweening;
using Michsky.UI.ModernUIPack;
using SEE.GO;
using SEE.Utils;
using TMPro;
using UnityEngine;

namespace SEE.Game.UI.Tooltip
{
    /// <summary>
    /// A box containing additional information which usually appears after hovering over any element for an
    /// amount of time and disappears when moving the pointer away from the element.
    /// Note that this class will not implement the hover detection: A tooltip must be manually shown
    /// and hidden using the <see cref="Show"/> and <see cref="Hide"/> methods.
    /// </summary>
    public class Tooltip : PlatformDependentComponent
    {
        /// <summary>
        /// Contains the text which shall be shown in the tooltip if <see cref="Show"/> has been called before Start.
        /// </summary>
        private string textBeforeStart;

        /// <summary>
        /// The path to the prefab for the tooltip game object.
        /// Will be added as a child to the <see cref="Canvas"/>.
        /// </summary>
        private const string TOOLTIP_PREFAB = "Prefabs/UI/Tooltip";

        /// <summary>
        /// The time it shall take to fade in the tooltip.
        /// Note that changing this value will affect the <see cref="FADE_OUT_DURATION"/>
        /// as well as the default delay of <see cref="Show"/>.
        /// </summary>
        private const float FADE_IN_DURATION = 0.5f; // Note: This will affect fade out and delay

        /// <summary>
        /// The time it will take to fade out.
        /// </summary>
        private const float FADE_OUT_DURATION = FADE_IN_DURATION / 2;

        /// <summary>
        /// The tooltip manager, which can (as its name implies) control tooltips.
        /// Note that this manager controls a single tooltip whose text can be changed. If multiple tooltips
        /// are needed, more GameObjects with TooltipManagers need to be created.
        /// </summary>
        private TooltipManager TooltipManager;

        /// <summary>
        /// The text mesh pro containing the actual tooltip text.
        /// </summary>
        private TextMeshProUGUI textComp;

        /// <summary>
        /// The canvas group in which the tooltip is contained.
        /// </summary>
        private CanvasGroup canvasGroup;

        /// <summary>
        /// The doTween sequence used for fading the tooltip in.
        /// </summary>
        private Sequence fadeIn;

        /// <summary>
        /// The GameObject which contains the Tooltip UI element.
        /// </summary>
        private GameObject tooltipGameObject;

        /// <summary>
        /// Displays the tooltip with the given <paramref name="text"/> after the given <paramref name="delay"/>.
        /// By default, the <paramref name="delay"/> will be set to twice the <see cref="FADE_IN_DURATION"/>.
        /// </summary>
        /// <param name="text">The text which shall be displayed in the tooltip.</param>
        /// <param name="delay">The time after which the tooltip should start fading in.</param>
        /// <exception cref="ArgumentException">If <paramref name="delay"/> is negative.</exception>
        public void Show(string text, float delay = FADE_IN_DURATION * 2)
        {
            if (delay < 0f)
            {
                throw new ArgumentException($"{nameof(delay)} must be a positive number!");
            }

            if (HasStarted)
            {
                fadeIn = DOTween.Sequence();
                // A short explanation on why the following three lines are done the way they are:
                // Changing the text of the tooltip will cause it to suddenly jump to another position.
                // To avoid this jarring visual, we wait until the fade out duration has passed
                // and only then change the text, when it's no longer visible. After this, we wait for
                // the rest of our delay. The Min and Max calls are here in case delay is less than FADE_OUT_DURATION.
                fadeIn.AppendInterval(Mathf.Min(delay, FADE_OUT_DURATION));
                fadeIn.AppendCallback(() => textComp.text = text);
                fadeIn.AppendInterval(Mathf.Max(delay - FADE_OUT_DURATION, 0f));
                // Move to top of layer hierarchy, which is at the bottom

                fadeIn.AppendCallback(SetLastSibling);
                fadeIn.Append(DOTween.To(() => canvasGroup != null ? canvasGroup.alpha : 0f, a =>
                {
                    if (canvasGroup != null)
                    {
                        canvasGroup.alpha = a;
                    }
                }, 1f, FADE_IN_DURATION)); fadeIn.Play();
            }
            else
            {
                textBeforeStart = text;
            }
            void SetLastSibling()
            {
                if (TooltipManager != null && TooltipManager.gameObject != null)
                {
                    TooltipManager.gameObject.transform.SetAsLastSibling();
                }
            }
        }

        /// <summary>
        /// Will hide the tooltip by fading it out if it's currently visible.
        /// If <see cref="Show"/> has been called prior to this and is in its delay or fade-in phase, it will be halted.
        /// </summary>
        public void Hide()
        {
            if (HasStarted)
            {
                fadeIn?.Pause(); // if we're still fading in right now, we need to stop that
                // Fade out
                DOTween.To(() => canvasGroup != null ? canvasGroup.alpha : 0f, a =>
                {
                    if (canvasGroup != null)
                    {
                        canvasGroup.alpha = a;
                    }
                }, 0f, FADE_OUT_DURATION);
            }
            else
            {
                // We no longer want to show the tooltip on Start()
                textBeforeStart = null;
            }
        }

        /// <summary>
        /// Will create a new tooltip UI GameObject by using a prefab.
        /// </summary>
        protected override void StartDesktop()
        {
            // Create new tooltip GameObject
            tooltipGameObject = PrefabInstantiator.InstantiatePrefab(TOOLTIP_PREFAB, Canvas.transform, false);
            if (tooltipGameObject.TryGetComponentOrLog(out TooltipManager))
            {
                TooltipManager.allowUpdating = true;
                // Move tooltip to front of layer hierarchy
                TooltipManager.gameObject.transform.SetAsLastSibling();
                // tooltipObject only has 1 child, and will never have more than that
                if (TooltipManager.tooltipObject.transform.GetChild(0).gameObject.TryGetComponentOrLog(out canvasGroup))
                {
                    // Get the actual text object
                    TextMeshProUGUI[] texts = TooltipManager.tooltipContent.GetComponentsInChildren<TextMeshProUGUI>();
                    textComp = texts.SingleOrDefault(x => x.name == "Description");
                    if (textComp == null)
                    {
                        Debug.LogError("Couldn't find Description text component for tooltip.");
                    }
                    else if (textBeforeStart != null)
                    {
                        // Having initialized all necessary components, we can now show the Tooltip.
                        Show(textBeforeStart);
                    }
                }
            }
        }

        private void OnDestroy()
        {
            fadeIn.Kill();
            Destroy(tooltipGameObject);
        }

        private void OnDisable()
        {
            if (tooltipGameObject != null)
            {
                tooltipGameObject.SetActive(false);
            }
        }

        private void OnEnable()
        {
            if (tooltipGameObject != null)
            {
                tooltipGameObject.SetActive(true);
            }
        }

        protected override void StartHoloLens()
        {
            //TODO:https://docs.microsoft.com/de-de/windows/mixed-reality/mrtk-unity/features/ux-building-blocks/tooltip
            base.StartHoloLens();
        }
    }
}