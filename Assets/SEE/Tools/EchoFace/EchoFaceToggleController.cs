using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

// Namespace documentation is provided in EchoFace.cs.
namespace SEE.Tools.EchoFace
{
    /// <summary>
    /// Allows toggling <see cref="EchoFace"/> on and off at runtime via a
    /// hotkey, resetting all blendshapes and bone rotations to their rest
    /// pose when disabling it, and shows a short on-screen popup indicating
    /// the new state.
    /// </summary>
    /// <remarks>
    /// This component creates its own popup UI (a <see cref="Canvas"/>,
    /// panel, and <see cref="TextMeshProUGUI"/> text) at runtime if none is
    /// assigned, reusing an existing screen-space-overlay canvas if one is
    /// found. It is intended to be attached to the same character prefab
    /// as <see cref="EchoFace"/>, or otherwise assigned a reference to it.
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
        /// The name of the child transform holding the
        /// <see cref="SkinnedMeshRenderer"/> whose blendshapes should be
        /// reset to zero when <see cref="EchoFace"/> is disabled. Only used
        /// if <see cref="skinnedMeshRenderer"/> is not assigned manually.
        /// </summary>
        [Header("Blendshapes (Reset on Disable)")]
        [SerializeField]
        private string bodyTransformName = "CC_Base_Body";

        /// <summary>
        /// The skinned mesh renderer whose blendshapes are reset to zero
        /// when <see cref="EchoFace"/> is disabled. If not assigned in the
        /// Inspector, an attempt is made to auto-assign it from a child
        /// named <see cref="bodyTransformName"/> during <see cref="Awake"/>.
        /// </summary>
        [SerializeField]
        private SkinnedMeshRenderer skinnedMeshRenderer;

        /// <summary>
        /// The name of the head bone transform to search for, whose
        /// rotation is cached as the rest pose and restored when
        /// <see cref="EchoFace"/> is disabled.
        /// </summary>
        [Header("Bones (Reset on Disable)")]
        [SerializeField]
        private string headTransformName = "CC_Base_Head";

        /// <summary>
        /// The name of the left eye bone transform to search for, whose
        /// rotation is cached as the rest pose and restored when
        /// <see cref="EchoFace"/> is disabled.
        /// </summary>
        [SerializeField]
        private string leftEyeName = "CC_Base_L_Eye";

        /// <summary>
        /// The name of the right eye bone transform to search for, whose
        /// rotation is cached as the rest pose and restored when
        /// <see cref="EchoFace"/> is disabled.
        /// </summary>
        [SerializeField]
        private string rightEyeName = "CC_Base_R_Eye";

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
        /// The key that toggles <see cref="EchoFace"/> on and off.
        /// </summary>
        [Header("Input")]
        [SerializeField]
        private KeyCode toggleKey = KeyCode.T;

        /// <summary>
        /// The root of the popup panel created by <see cref="EnsurePopupUI"/>.
        /// </summary>
        private GameObject popupPanel;

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
        /// The head bone transform found under <see cref="headTransformName"/>,
        /// or <c>null</c> if it could not be found.
        /// </summary>
        private Transform headTransform;

        /// <summary>
        /// The left eye bone transform found under <see cref="leftEyeName"/>,
        /// or <c>null</c> if it could not be found.
        /// </summary>
        private Transform leftEyeTransform;

        /// <summary>
        /// The right eye bone transform found under <see cref="rightEyeName"/>,
        /// or <c>null</c> if it could not be found.
        /// </summary>
        private Transform rightEyeTransform;

        /// <summary>
        /// The local rotation of <see cref="headTransform"/> cached in
        /// <see cref="CacheHeadAndEyes"/>, restored when
        /// <see cref="EchoFace"/> is disabled.
        /// </summary>
        private Quaternion headRestRotation;

        /// <summary>
        /// The local rotation of <see cref="leftEyeTransform"/> cached in
        /// <see cref="FindEyeBones"/>, restored when <see cref="EchoFace"/>
        /// is disabled.
        /// </summary>
        private Quaternion leftEyeRestRotation;

        /// <summary>
        /// The local rotation of <see cref="rightEyeTransform"/> cached in
        /// <see cref="FindEyeBones"/>, restored when <see cref="EchoFace"/>
        /// is disabled.
        /// </summary>
        private Quaternion rightEyeRestRotation;

        /// <summary>
        /// Unity lifecycle method. Resolves <see cref="echoFace"/> and
        /// <see cref="skinnedMeshRenderer"/> if not assigned, caches the
        /// rest rotations of the head and eye bones, and creates the popup
        /// UI. Disables this component if no <see cref="EchoFace"/>
        /// instance can be found.
        /// </summary>
        private void Awake()
        {
            // Find EchoFace if not assigned.
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

            // Find SkinnedMeshRenderer if not assigned.
            if (skinnedMeshRenderer == null)
            {
                skinnedMeshRenderer = transform.Find(bodyTransformName)?.GetComponent<SkinnedMeshRenderer>();
                if (skinnedMeshRenderer == null)
                {
                    Debug.LogWarning($"[EchoFaceToggleController] SkinnedMeshRenderer not found under '{bodyTransformName}'. Blendshape reset will be skipped.");
                }
            }

            // Find head & eyes and cache their rest rotation.
            CacheHeadAndEyes();

            // Create the popup UI.
            EnsurePopupUI();
            popupPanel.SetActive(false);

            Debug.Log("[EchoFaceToggleController] Ready. ToggleKey = " + toggleKey);
        }

        /// <summary>
        /// Unity lifecycle method. Toggles <see cref="EchoFace"/> on and
        /// off when <see cref="toggleKey"/> is pressed, resetting
        /// blendshapes and bone rotations when disabling it, and shows a
        /// popup indicating the new state.
        /// </summary>
        private void Update()
        {
            if (!Input.GetKeyDown(toggleKey))
            {
                return;
            }

            bool newState = !echoFace.enabled;
            echoFace.enabled = newState;

            // When disabling: reset everything.
            if (!newState)
            {
                ResetAllBlendShapes(skinnedMeshRenderer);
                ResetEyesAndHead();
            }

            ShowPopup($"EchoFace {(newState ? "enabled" : "disabled")}");
        }

        /// <summary>
        /// Sets all blendshape weights on the given renderer's mesh to zero.
        /// </summary>
        /// <param name="meshRenderer">
        /// The skinned mesh renderer to reset. If <c>null</c>, or if it has
        /// no shared mesh, the method returns without doing anything.
        /// </param>
        private void ResetAllBlendShapes(SkinnedMeshRenderer meshRenderer)
        {
            if (meshRenderer == null)
            {
                return;
            }

            Mesh mesh = meshRenderer.sharedMesh;
            if (mesh == null)
            {
                return;
            }

            int count = mesh.blendShapeCount;
            for (int i = 0; i < count; i++)
            {
                meshRenderer.SetBlendShapeWeight(i, 0f);
            }
        }

        /// <summary>
        /// Finds the head bone transform named <see cref="headTransformName"/>
        /// under this <see cref="GameObject"/>, caches its rest rotation,
        /// and resolves the eye bones via <see cref="FindEyeBones"/>.
        /// </summary>
        private void CacheHeadAndEyes()
        {
            headTransform = FindDeepChild(transform, headTransformName);
            if (headTransform == null)
            {
                Debug.LogWarning($"[EchoFaceToggleController] Head transform '{headTransformName}' not found. Head/Eye reset will be skipped.");
                return;
            }

            // Cache the rest pose of the head
            headRestRotation = headTransform.localRotation;

            // Resolve the eye bones and cache their rest rotations
            FindEyeBones(headTransform);
        }

        /// <summary>
        /// Finds the left and right eye bone transforms under the given
        /// head transform and caches their rest rotations.
        /// </summary>
        /// <param name="head">
        /// The head transform to search under. If <c>null</c>, the method
        /// returns without doing anything.
        /// </param>
        private void FindEyeBones(Transform head)
        {
            if (head == null)
            {
                return;
            }

            leftEyeTransform = FindDeepChild(head, leftEyeName);
            rightEyeTransform = FindDeepChild(head, rightEyeName);

            if (leftEyeTransform == null || rightEyeTransform == null)
            {
                return;
            }

            leftEyeRestRotation = leftEyeTransform.localRotation;
            rightEyeRestRotation = rightEyeTransform.localRotation;
        }

        /// <summary>
        /// Restores the head and eye bone transforms to their cached rest
        /// rotations, if they were found.
        /// </summary>
        private void ResetEyesAndHead()
        {
            // Reset eyes (if found).
            if (leftEyeTransform != null)
            {
                leftEyeTransform.localRotation = leftEyeRestRotation;
            }

            if (rightEyeTransform != null)
            {
                rightEyeTransform.localRotation = rightEyeRestRotation;
            }

            // Reset head (if found).
            if (headTransform != null)
            {
                headTransform.localRotation = headRestRotation;
            }
        }

        /// <summary>
        /// Recursively finds a child transform by name.
        /// </summary>
        /// <param name="parent">
        /// The transform whose descendants are searched. If <c>null</c>,
        /// <c>null</c> is returned.
        /// </param>
        /// <param name="name">The name of the child transform to find.</param>
        /// <returns>
        /// The first matching descendant transform, searched breadth-first
        /// among direct children before recursing; or <c>null</c> if no
        /// matching descendant exists.
        /// </returns>
        private Transform FindDeepChild(Transform parent, string name)
        {
            if (parent == null)
            {
                return null;
            }

            Transform directChild = parent.Find(name);
            if (directChild != null)
            {
                return directChild;
            }

            foreach (Transform child in parent)
            {
                Transform found = FindDeepChild(child, name);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        /// <summary>
        /// Ensures the popup UI exists, creating a screen-space-overlay
        /// canvas, panel, and <see cref="TextMeshProUGUI"/> text if
        /// necessary. Reuses an existing screen-space-overlay canvas in the
        /// scene if one is found.
        /// </summary>
        private void EnsurePopupUI()
        {
            // Find or create canvas (overlay).
            Canvas canvas = null;
            foreach (Canvas c in FindObjectsOfType<Canvas>())
            {
                if (c.renderMode == RenderMode.ScreenSpaceOverlay)
                {
                    canvas = c;
                    break;
                }
            }

            if (canvas == null)
            {
                GameObject canvasGO = new(
                    "PopupCanvas",
                    typeof(Canvas),
                    typeof(CanvasScaler),
                    typeof(GraphicRaycaster)
                );

                canvas = canvasGO.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 5000;

                CanvasScaler scaler = canvasGO.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new(1920, 1080);
            }
            else
            {
                canvas.sortingOrder = Mathf.Max(canvas.sortingOrder, 5000);
            }

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
            // Note: "new()" cannot be used here because this iterator method
            // returns the non-generic IEnumerator, so "yield return" has no
            // inferable target type for the constructed object.
            yield return new WaitForSecondsRealtime(popupDuration);
            if (popupPanel != null)
            {
                popupPanel.SetActive(false);
            }

            hideRoutine = null;
        }
    }
}
