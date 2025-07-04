using System;
using System.IO;
using System.Linq;
using DG.Tweening;
using Michsky.UI.ModernUIPack;
using SEE.GO;
using SEE.Utils;
using TMPro;
using UnityEngine;

namespace SEE.UI
{
    /// <summary>
    /// A box containing additional information which usually appears after hovering over any element for an
    /// amount of time and disappears when moving the pointer away from the element.
    ///
    /// It should be used via its static methods <see cref="ActivateWith"/> and <see cref="Deactivate"/>.
    /// You should not add this component to any game object yourself.
    ///
    /// Call <see cref="ActivateWith"/> when the mouse cursor enters the region of an object which should
    /// display a tooltip, and call <see cref="Deactivate"/> when the mouse cursor leaves the region.
    /// This class will render a tooltip on the screen with the given text if the mouse cursor stays in the region
    /// without moving for a certain amount of time.
    /// </summary>
    public class Tooltip : PlatformDependentComponent
    {
        // TODO (#596): Make this work on VR.

        /// <summary>
        /// The kinds of behavior the tooltip can have after it has been shown.
        /// </summary>
        public enum AfterShownBehavior
        {
            /// <summary>
            /// The tooltip will reappear after it has been shown (assuming no mouse movement for a while).
            /// </summary>
            Reappear,

            /// <summary>
            /// The tooltip will hide until the text changes.
            /// </summary>
            HideUntilChanged,

            /// <summary>
            /// The tooltip will hide until it is activated (i.e., <see cref="ActivateWith"/> is called) again.
            /// </summary>
            HideUntilActivated
        }

        /// <summary>
        /// The path to the prefab for the tooltip game object.
        /// Will be added as a child to the <see cref="Canvas"/>.
        /// </summary>
        private const string tooltipPrefab = "Prefabs/UI/Tooltip";

        /// <summary>
        /// Name of the tooltip game object.
        /// </summary>
        private const string tooltipName = "SEETooltip";

        /// <summary>
        /// The time it shall take to fade in the tooltip in seconds.
        /// </summary>
        private const float fadeInDuration = 0.3f;

        /// <summary>
        /// The time it will take to fade out in seconds.
        /// </summary>
        private const float fadeOutDuration = fadeInDuration / 2;

        /// <summary>
        /// The time to wait before showing the tooltip in seconds.
        /// </summary>
        private const float delay = 0.5f;

        /// <summary>
        /// Contains the text which shall be shown in the tooltip.
        /// </summary>
        private string text;

        /// <summary>
        /// The tooltip manager, which can (as its name implies) control tooltips.
        /// Note that this manager controls a single tooltip whose text can be changed.
        /// </summary>
        private TooltipManager tooltipManager;

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
        private Tweener fadeIn;

        /// <summary>
        /// The doTween sequence used for fading the tooltip out.
        ///
        /// This should not be interrupted or killed (unless the tooltip is destroyed),
        /// otherwise callbacks updating the text will not be called correctly.
        /// </summary>
        private Tweener fadeOut;

        /// <summary>
        /// The GameObject which contains the Tooltip UI element.
        /// </summary>
        [ManagedUI(toggleEnabled: true)]
        private GameObject tooltipGameObject;

        /// <summary>
        /// The last known position of the mouse.
        /// </summary>
        private Vector3 lastMousePosition;

        /// <summary>
        /// The text which shall be shown once, and then disappear until it's activated again.
        /// </summary>
        private string oneTimeText;

        /// <summary>
        /// The text which was last displayed.
        /// </summary>
        private string lastDisplayedText;

        /// <summary>
        /// The instance of the tooltip.
        ///
        /// Prefer using <see cref="Instance"/> instead of this field.
        /// </summary>
        private static Tooltip instance;

        /// <summary>
        /// The time in seconds that has passed since the last time the mouse was moved.
        /// </summary>
        private float waitTime;

        /// <summary>
        /// Whether the tooltip is currently fading out.
        /// </summary>
        private bool IsFadingOut => fadeOut is { active: true } && fadeOut.IsPlaying();

        /// <summary>
        /// Lazily initialized tooltip instance. Behaves like a singleton.
        /// </summary>
        private static Tooltip Instance
        {
            get
            {
                if (instance == null)
                {
                    // Find is expensive, but the tooltip is only created once.
                    GameObject go = GameObject.Find(tooltipName) ?? new GameObject(tooltipName);
                    return instance = go.AddComponent<Tooltip>();
                }
                else
                {
                    return instance;
                }
            }
        }

        /// <summary>
        /// Displays the tooltip with the given <paramref name="text"/> after some time without mouse movement.
        /// If <paramref name="text"/> is null, the tooltip will be hidden.
        /// </summary>
        /// <param name="text">The text which shall be displayed in the tooltip.</param>
        /// <param name="afterShownBehavior">The behavior of the tooltip after it has been shown.</param>
        private void ChangeText(string text, AfterShownBehavior afterShownBehavior = AfterShownBehavior.Reappear)
        {
            if (text != null)
            {
                switch (afterShownBehavior)
                {
                    case AfterShownBehavior.Reappear:
                        oneTimeText = null;
                        break;
                    case AfterShownBehavior.HideUntilChanged:
                        oneTimeText = text;
                        break;
                    case AfterShownBehavior.HideUntilActivated:
                        oneTimeText = text;
                        lastDisplayedText = null;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(afterShownBehavior), afterShownBehavior, null);
                }
            }
            if (text != this.text)
            {
                this.text = text;
                // We actually need to fade out the text first, otherwise the sudden change
                // will create a very jarring visual. The FadeOut() method will handle the text change.
                FadeOut();
            }
        }

        /// <summary>
        /// Hides the tooltip.
        /// It will not be shown again until <see cref="ActivateWith"/> is called.
        /// </summary>
        public static void Deactivate()
        {
            Instance.ChangeText(null);
        }

        /// <summary>
        /// Displays the tooltip with the given <paramref name="text"/> after some time without mouse movement.
        /// </summary>
        /// <param name="text">The text which shall be displayed in the tooltip.</param>
        /// <param name="afterShownBehavior">The behavior of the tooltip after it has been shown.</param>
        public static void ActivateWith(string text, AfterShownBehavior afterShownBehavior = AfterShownBehavior.Reappear)
        {
            if (text == null)
            {
                Debug.LogWarning($"Prefer using {nameof(Deactivate)} instead of passing null to {nameof(ActivateWith)}.\n");
            }
            Instance.ChangeText(text, afterShownBehavior);
        }

        /// <summary>
        /// Whether the tooltip is currently active.
        /// Note that "active" does not necessarily mean that the tooltip is currently visible.
        /// </summary>
        public static bool IsActivated => Instance.text != null;

        /// <summary>
        /// Will hide the tooltip by fading it out if it's currently visible.
        /// If <see cref="FadeIn"/> has been called prior to this and is active, it will be halted.
        /// </summary>
        private void FadeOut()
        {
            if (HasStarted)
            {
                // If we're fading out already, we don't need to do anything.
                if (!IsFadingOut)
                {
                    // If we're still fading in right now, we need to stop that.
                    fadeIn?.Pause();

                    // Fade out. After we're done fading out, we should handle any pending text changes.
                    fadeOut = DOTween.To(() => canvasGroup != null ? canvasGroup.alpha : 0f, a =>
                    {
                        if (canvasGroup != null)
                        {
                            canvasGroup.alpha = a;
                        }
                    }, 0f, fadeOutDuration).OnComplete(() => textComp.text = text ?? "").Play();
                }
                waitTime = 0;
            }
        }

        /// <summary>
        /// Will show the tooltip by fading it in.
        /// </summary>
        private void FadeIn()
        {
            // Move to top of layer hierarchy, which is at the bottom
            SetLastSibling();
            fadeIn = DOTween.To(() => canvasGroup != null ? canvasGroup.alpha : 0f, a =>
            {
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = a;
                }
            }, 1f, fadeInDuration).OnComplete(() => lastDisplayedText = text).Play();
            waitTime = 0;
            return;

            void SetLastSibling()
            {
                if (tooltipManager != null && tooltipManager.gameObject != null)
                {
                    tooltipManager.gameObject.transform.SetAsLastSibling();
                }
            }
        }

        /// <summary>
        /// Will create a new tooltip UI GameObject by using a prefab.
        /// </summary>
        protected override void StartDesktop()
        {
            // Create new tooltip GameObject
            tooltipGameObject = PrefabInstantiator.InstantiatePrefab(tooltipPrefab, Canvas.transform, false);
            if (tooltipGameObject.TryGetComponentOrLog(out tooltipManager))
            {
                tooltipManager.allowUpdating = true;
                // Move tooltip to front of layer hierarchy
                tooltipManager.gameObject.transform.SetAsLastSibling();
                if (tooltipManager.tooltipObject.transform.Find("Anchor/Content").gameObject.TryGetComponentOrLog(out canvasGroup))
                {
                    // Get the actual text object
                    TextMeshProUGUI[] texts = tooltipManager.tooltipContent.GetComponentsInChildren<TextMeshProUGUI>();
                    textComp = texts.SingleOrDefault(x => x.name == "Description");
                    if (textComp == null)
                    {
                        Debug.LogError("Couldn't find Description text component for tooltip.\n");
                    }
                    else if (text != null)
                    {
                        // Having initialized all necessary components, we can now show the Tooltip.
                        ChangeText(text);
                    }
                }
            }
        }

        protected override void StartVR()
        {
            StartDesktop();
        }

        protected override void UpdateDesktop()
        {
            if (text == null)
            {
                waitTime = 0;
                return;
            }

            // If the mouse has moved, we should hide the tooltip.
            if (lastMousePosition != Input.mousePosition)
            {
                lastMousePosition = Input.mousePosition;
                if (oneTimeText != null && lastDisplayedText == oneTimeText)
                {
                    // This was a one-time tooltip, so we should hide it again now if it was displayed already.
                    text = null;
                }
                FadeOut();
                return;
            }

            waitTime += Time.deltaTime;
            // We wait for any fade out to complete before fading in.
            if (waitTime > delay && !IsFadingOut)
            {
                FadeIn();
            }
        }

        protected override void UpdateVR()
        {
            UpdateDesktop();
        }

        protected override void OnDisable()
        {
            fadeIn?.Kill();
        }
    }
}
