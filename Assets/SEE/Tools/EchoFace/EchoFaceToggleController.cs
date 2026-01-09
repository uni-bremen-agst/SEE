using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class EchoFaceToggleController : MonoBehaviour
{
    [Header("Target")]
    public EchoFace echoFace;

    [Header("Blendshapes (Reset bei Deaktivierung)")]
    public string bodyTransformName = "CC_Base_Body";

    public SkinnedMeshRenderer skinnedMeshRenderer;

    [Header("Bones (Reset bei Deaktivierung)")]
    public string headTransformName = "CC_Base_Head";
    public string leftEyeName = "CC_Base_L_Eye";
    public string rightEyeName = "CC_Base_R_Eye";

    [Header("Popup UI")]
    public float popupDuration = 2f;
    public Color backgroundColor = new Color(0f, 0f, 0f, 0.65f);

    [Header("Input")]
    public KeyCode toggleKey = KeyCode.T;

    GameObject popupPanel;
    TextMeshProUGUI popupText;
    Coroutine hideRoutine;

    Transform headTransform;
    Transform leftEyeTransform;
    Transform rightEyeTransform;

    Quaternion headRestRotation;
    Quaternion leftEyeRestRotation;
    Quaternion rightEyeRestRotation;

    void Awake()
    {
        // EchoFace finden, falls nicht gesetzt
        if (echoFace == null)
        {
            echoFace = FindObjectOfType<EchoFace>();
            if (echoFace == null)
            {
                Debug.LogError("[EchoFaceToggleController] EchoFace nicht gefunden.");
                enabled = false;
                return;
            }
        }

        // SkinnedMeshRenderer finden, falls nicht gesetzt
        if (skinnedMeshRenderer == null)
        {
            skinnedMeshRenderer = transform.Find(bodyTransformName)?.GetComponent<SkinnedMeshRenderer>();
            if (skinnedMeshRenderer == null)
            {
                Debug.LogWarning($"[EchoFaceToggleController] SkinnedMeshRenderer nicht gefunden unter '{bodyTransformName}'. Blendshape-Reset wird übersprungen.");
            }
        }

        // Head & Eyes finden + Rest-Rotation cachen
        CacheHeadAndEyes();

        // Popup UI erstellen
        EnsurePopupUI();
        popupPanel.SetActive(false);

        Debug.Log("[EchoFaceToggleController] Ready. ToggleKey = " + toggleKey);
    }

    void Update()
    {
        if (!Input.GetKeyDown(toggleKey))
            return;

        bool newState = !echoFace.enabled;
        echoFace.enabled = newState;

        // Beim Deaktivieren: alles zurücksetzen
        if (!newState)
        {
            ResetAllBlendShapes(skinnedMeshRenderer);
            ResetEyesAndHead();
        }

        ShowPopup($"EchoFace {(newState ? "aktiviert" : "deaktiviert")}");
    }

    void ResetAllBlendShapes(SkinnedMeshRenderer smr)
    {
        if (smr == null) return;

        var mesh = smr.sharedMesh;
        if (mesh == null) return;

        int count = mesh.blendShapeCount;
        for (int i = 0; i < count; i++)
            smr.SetBlendShapeWeight(i, 0f);
    }

    void CacheHeadAndEyes()
    {
        headTransform = FindDeepChild(transform, headTransformName);
        if (headTransform == null)
        {
            Debug.LogWarning($"[EchoFaceToggleController] Head transform '{headTransformName}' nicht gefunden. Head/Eye-Reset wird übersprungen.");
            return;
        }

        // Restpose vom Kopf cachen
        headRestRotation = headTransform.localRotation;

        // Augen suchen & Restpose cachen
        FindEyeBones(headTransform);
    }

    void FindEyeBones(Transform head)
    {
        if (head == null) return;

        leftEyeTransform = FindDeepChild(head, leftEyeName);
        rightEyeTransform = FindDeepChild(head, rightEyeName);

        if (leftEyeTransform == null || rightEyeTransform == null)
        {
            Debug.LogWarning("[EchoFaceToggleController] Eye bone transforms not found. Eye reset will be skipped.");
            return;
        }

        leftEyeRestRotation = leftEyeTransform.localRotation;
        rightEyeRestRotation = rightEyeTransform.localRotation;
    }

    void ResetEyesAndHead()
    {
        // Augen zurücksetzen (falls gefunden)
        if (leftEyeTransform != null)
            leftEyeTransform.localRotation = leftEyeRestRotation;

        if (rightEyeTransform != null)
            rightEyeTransform.localRotation = rightEyeRestRotation;

        // Kopf zurücksetzen (falls gefunden)
        if (headTransform != null)
            headTransform.localRotation = headRestRotation;
    }

    Transform FindDeepChild(Transform parent, string name)
    {
        if (parent == null) return null;

        // Direct child
        var directChild = parent.Find(name);
        if (directChild != null) return directChild;

        // Recursive
        foreach (Transform child in parent)
        {
            var found = FindDeepChild(child, name);
            if (found != null) return found;
        }

        return null;
    }

    void EnsurePopupUI()
    {
        // Canvas finden/erstellen (Overlay)
        Canvas canvas = null;
        foreach (var c in FindObjectsOfType<Canvas>())
        {
            if (c.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                canvas = c;
                break;
            }
        }

        if (canvas == null)
        {
            var canvasGO = new GameObject(
                "PopupCanvas",
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster)
            );

            canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 5000;

            var scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
        }
        else
        {
            canvas.sortingOrder = Mathf.Max(canvas.sortingOrder, 5000);
        }

        // Panel (Box)
        popupPanel = new GameObject("PopupPanel", typeof(RectTransform), typeof(Image));
        popupPanel.transform.SetParent(canvas.transform, false);

        var panelImage = popupPanel.GetComponent<Image>();
        panelImage.color = backgroundColor;
        panelImage.raycastTarget = false;

        var panelRT = popupPanel.GetComponent<RectTransform>();
        panelRT.anchorMin = new Vector2(0.5f, 1f);
        panelRT.anchorMax = new Vector2(0.5f, 1f);
        panelRT.pivot = new Vector2(0.5f, 1f);
        panelRT.anchoredPosition = new Vector2(0f, -40f);
        panelRT.sizeDelta = new Vector2(900f, 120f);

        // TMP-Text als Kind
        var textGO = new GameObject("PopupText (TMP)", typeof(RectTransform));
        textGO.transform.SetParent(popupPanel.transform, false);

        popupText = textGO.AddComponent<TextMeshProUGUI>();
        popupText.alignment = TextAlignmentOptions.Center;
        popupText.fontSize = 40;
        popupText.color = Color.white;
        popupText.enableWordWrapping = false;
        popupText.raycastTarget = false;
        popupText.text = "";

        var textRT = textGO.GetComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = new Vector2(20f, 10f);
        textRT.offsetMax = new Vector2(-20f, -10f);
    }

    void ShowPopup(string message)
    {
        if (popupPanel == null || popupText == null)
        {
            Debug.LogWarning("[EchoFaceToggleController] Popup UI fehlt. Message: " + message);
            return;
        }

        popupText.text = message;
        popupPanel.SetActive(true);

        if (hideRoutine != null)
            StopCoroutine(hideRoutine);

        hideRoutine = StartCoroutine(CoHidePopup());
    }

    IEnumerator CoHidePopup()
    {
        yield return new WaitForSecondsRealtime(popupDuration);
        if (popupPanel != null) popupPanel.SetActive(false);
        hideRoutine = null;
    }
}
