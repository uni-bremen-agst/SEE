using System;
using System.Collections;
using System.Collections.Generic;
using SEE.Controls;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Namespace documentation is provided in EchoFace.cs.
namespace SEE.Tools.EchoFace
{
    /// <summary>
    /// Allows toggling <see cref="EchoFace"/> on and off at runtime via a
    /// key binding and shows a short on-screen popup indicating the new state.
    /// While <see cref="EchoFace"/> is active, also disables a configurable list
    /// of conflicting components (such as SALSA LipSync), restoring them to their
    /// original enabled state once <see cref="EchoFace"/> is switched off again.
    /// </summary>
    /// <remarks>
    /// This component creates its own dedicated popup UI (a <see cref="Canvas"/>,
    /// panel, and <see cref="TextMeshProUGUI"/> text) at runtime if none is
    /// assigned. It is intended to be attached to the same character prefab
    /// as <see cref="EchoFace"/>, or otherwise assigned a reference to it.
    /// Conflicting components are resolved automatically in <see cref="Awake"/>
    /// by matching configured names against the runtime type name of every
    /// <see cref="Behaviour"/> attached to <c>echoFace.gameObject</c>, so no
    /// manual Inspector wiring is required.
    /// </remarks>
    internal class EchoFaceToggleController : MonoBehaviour
    {
        /// <summary>
        /// The <see cref="EchoFace"/> component to toggle. If not assigned
        /// in the Inspector, the first instance found in the scene via
        /// <see cref="Object.FindObjectOfType{T}()"/> is used in
        /// <see cref="Awake"/>. If none is found, this component disables itself.
        /// </summary>
        [Header("Target")]
        [SerializeField]
        private EchoFace echoFace;

        /// <summary>
        /// Class names of components on <c>echoFace.gameObject</c> that conflict
        /// with EchoFace animation and should be disabled while EchoFace is active.
        /// Their enabled state is restored when EchoFace is disabled again.
        /// </summary>
        [Header("Conflicting Components (Disabled while EchoFace is Active)")]
        [SerializeField]
        private List<string> conflictingComponentNames = new()
        {
            "SALSA",
            "EmoteR",
            "Silence Analyzer",
            "Eyes",
            "Queue Processor",
            "SalsaDissonanceLink",
        };

        /// <summary>
        /// How long, in seconds, the state-change popup remains visible
        /// before it is hidden again.
        /// </summary>
        [Header("Popup UI")]
        [SerializeField]
        private float popupDuration = 2f;

        /// <summary>
        /// The background color of the popup panel.
        /// </summary>
        [SerializeField]
        private Color backgroundColor = new(0f, 0f, 0f, 0.65f);

        /// <summary>
        /// The root of the popup panel created by <see cref="EnsurePopupUI"/>.
        /// </summary>
        private GameObject popupPanel;

        /// <summary>
        /// The canvas that contains the popup panel and text.
        /// </summary>
        private GameObject popupCanvasGO;

        /// <summary>
        /// The text component of the popup panel, whose text is set in
        /// <see cref="ShowPopup"/>.
        /// </summary>
        private TextMeshProUGUI popupText;

        /// <summary>
        /// The currently running coroutine that hides the popup after
        /// <see cref="popupDuration"/> seconds, or <c>null</c> if no popup
        /// is currently being shown.
        /// </summary>
        private Coroutine hideRoutine;

        /// <summary>
        /// The conflicting components resolved by <see cref="ResolveConflictingComponents"/>
        /// from <see cref="conflictingComponentNames"/>, excluding any that could not be found.
        /// </summary>
        private readonly List<Behaviour> resolvedConflictingComponents = new();

        /// <summary>
        /// Caches the <see cref="Behaviour.enabled"/> state that each resolved
        /// component had at the time <see cref="CacheInitialComponentStates"/> was called.
        /// </summary>
        private readonly Dictionary<Behaviour, bool> initialEnabledStates = new();

        /// <summary>
        /// Unity lifecycle method. Resolves <see cref="echoFace"/> if not assigned,
        /// caches the initial states of conflicting components, and creates the popup
        /// UI. Disables this component if no <see cref="EchoFace"/> instance can be found.
        /// </summary>
        private void Awake()
        {
            if (echoFace == null)
            {
                echoFace = FindObjectOfType<EchoFace>();
                if (echoFace == null)
                {
                    Debug.LogError("[EchoFaceToggleController] EchoFace not found.");
                    enabled = false;
                    return;
                }
            }

            // Start with EchoFace disabled to avoid conflicts with other components
            echoFace.enabled = false;

            ResolveConflictingComponents();
            CacheInitialComponentStates();

            EnsurePopupUI();
            popupPanel.SetActive(false);

            Debug.Log("[EchoFaceToggleController] Ready.");
        }

        /// <summary>
        /// Unity lifecycle method. Toggles <see cref="EchoFace"/> on and off via
        /// <see cref="SEEInput.ToggleEchoFace"/>. When enabling <see cref="EchoFace"/>,
        /// disables conflicting components. When disabling <see cref="EchoFace"/>,
        /// restores them to their original enabled state (facial rest pose is handled
        /// automatically by EchoFace's OnDisable). Shows a popup indicating the new state.
        /// </summary>
        private void Update()
        {
            if (!SEEInput.ToggleEchoFace())
            {
                return;
            }

            bool newState = !echoFace.enabled;
            echoFace.enabled = newState;

            if (newState)
            {
                DisableConflictingComponents();
            }
            else
            {
                RestoreConflictingComponents();
            }

            ShowPopup($"EchoFace {(newState ? "enabled" : "disabled")}");
        }

        /// <summary>
        /// Resolves the configured component names in <see cref="conflictingComponentNames"/>
        /// against the runtime type name of every <see cref="Behaviour"/> attached to
        /// <c>echoFace.gameObject</c>. The comparison ignores case and spaces.
        /// </summary>
        private void ResolveConflictingComponents()
        {
            resolvedConflictingComponents.Clear();

            if (echoFace == null || conflictingComponentNames == null)
            {
                return;
            }

            Behaviour[] attachedBehaviours = echoFace.GetComponents<Behaviour>();

            foreach (string targetName in conflictingComponentNames)
            {
                if (string.IsNullOrWhiteSpace(targetName))
                {
                    continue;
                }

                string normalizedTargetName = targetName.Replace(" ", string.Empty);
                Behaviour match = null;

                foreach (Behaviour behaviour in attachedBehaviours)
                {
                    if (behaviour != null
                        && string.Equals(behaviour.GetType().Name, normalizedTargetName, StringComparison.OrdinalIgnoreCase))
                    {
                        match = behaviour;
                        break;
                    }
                }

                if (match != null)
                {
                    resolvedConflictingComponents.Add(match);
                }
                else
                {
                    Debug.LogWarning($"[EchoFaceToggleController] Conflicting component '{targetName}' not found on '{echoFace.gameObject.name}'.");
                }
            }
        }

        /// <summary>
        /// Caches the current <see cref="Behaviour.enabled"/> state of each resolved
        /// conflicting component into <see cref="initialEnabledStates"/>.
        /// </summary>
        private void CacheInitialComponentStates()
        {
            foreach (Behaviour behaviour in resolvedConflictingComponents)
            {
                initialEnabledStates[behaviour] = behaviour.enabled;
            }
        }

        /// <summary>
        /// Disables all resolved conflicting components.
        /// </summary>
        private void DisableConflictingComponents()
        {
            foreach (Behaviour behaviour in resolvedConflictingComponents)
            {
                behaviour.enabled = false;
            }
        }

        /// <summary>
        /// Restores all resolved conflicting components to the state cached by
        /// <see cref="CacheInitialComponentStates"/>.
        /// </summary>
        private void RestoreConflictingComponents()
        {
            foreach (Behaviour behaviour in resolvedConflictingComponents)
            {
                if (initialEnabledStates.TryGetValue(behaviour, out bool initialEnabled))
                {
                    behaviour.enabled = initialEnabled;
                }
            }
        }

        /// <summary>
        /// Ensures the popup UI exists, creating a dedicated screen-space-overlay
        /// canvas, panel, and <see cref="TextMeshProUGUI"/> text.
        /// </summary>
        private void EnsurePopupUI()
        {
            popupCanvasGO = new(
                "EchoFacePopupCanvas",
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster)
            );

            Canvas canvas = popupCanvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 5000;

            CanvasScaler scaler = popupCanvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new(1920, 1080);

            // Panel (box).
            popupPanel = new("PopupPanel", typeof(RectTransform), typeof(Image));
            popupPanel.transform.SetParent(canvas.transform, false);

            Image panelImage = popupPanel.GetComponent<Image>();
            panelImage.color = backgroundColor;
            panelImage.raycastTarget = false;

            RectTransform panelRT = popupPanel.GetComponent<RectTransform>();
            panelRT.anchorMin = new(0.5f, 1f);
            panelRT.anchorMax = new(0.5f, 1f);
            panelRT.pivot = new(0.5f, 1f);
            panelRT.anchoredPosition = new(0f, -40f);
            panelRT.sizeDelta = new(900f, 120f);

            // TMP text as child.
            GameObject textGO = new("PopupText (TMP)", typeof(RectTransform));
            textGO.transform.SetParent(popupPanel.transform, false);

            popupText = textGO.AddComponent<TextMeshProUGUI>();
            popupText.alignment = TextAlignmentOptions.Center;
            popupText.fontSize = 40;
            popupText.color = Color.white;
            popupText.enableWordWrapping = false;
            popupText.raycastTarget = false;
            popupText.text = "";

            RectTransform textRT = textGO.GetComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.offsetMin = new(20f, 10f);
            textRT.offsetMax = new(-20f, -10f);
        }

        /// <summary>
        /// Displays the popup panel with the given message and starts a
        /// coroutine to hide it again after <see cref="popupDuration"/>
        /// seconds, restarting the timer if a popup is already showing.
        /// </summary>
        /// <param name="message">The message to display in the popup.</param>
        private void ShowPopup(string message)
        {
            if (popupPanel == null || popupText == null)
            {
                Debug.LogWarning("[EchoFaceToggleController] Popup UI is missing. Message: " + message);
                return;
            }

            popupText.text = message;
            popupPanel.SetActive(true);

            if (hideRoutine != null)
            {
                StopCoroutine(hideRoutine);
            }

            hideRoutine = StartCoroutine(CoHidePopup());
        }

        /// <summary>
        /// Coroutine that waits for <see cref="popupDuration"/> seconds
        /// (unaffected by <see cref="Time.timeScale"/>) and then hides the
        /// popup panel.
        /// </summary>
        /// <returns>An enumerator for use with <see cref="MonoBehaviour.StartCoroutine(IEnumerator)"/>.</returns>
        private IEnumerator CoHidePopup()
        {
            yield return new WaitForSecondsRealtime(popupDuration);
            if (popupPanel != null)
            {
                popupPanel.SetActive(false);
            }

            hideRoutine = null;
        }

        /// <summary>
        /// Unity lifecycle method. Cleans up the dynamically created popup UI
        /// (canvas or panel) to prevent orphaned UI GameObjects when the avatar is destroyed.
        /// </summary>
        private void OnDestroy()
        {
            if (popupCanvasGO != null)
            {
                Destroy(popupCanvasGO);
            }
            else if (popupPanel != null)
            {
                Destroy(popupPanel);
            }
        }
    }
}
