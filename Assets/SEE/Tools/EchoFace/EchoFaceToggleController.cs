using UnityEngine;
using TMPro;
using System.Collections;

public class EchoFaceToggleController : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("Wird automatisch in Awake() gesucht, falls leer")]
    public EchoFace echoFace;

    [Header("UI")]
    public TextMeshProUGUI popupText;
    public float popupDuration = 2f;

    [Header("Input")]
    public KeyCode toggleKey = KeyCode.T;

    void Awake()
    {
        // Falls nicht im Inspector gesetzt → automatisch suchen
        if (echoFace == null)
        {
            echoFace = FindObjectOfType<EchoFace>();

            if (echoFace == null)
            {
                Debug.LogError("EchoFaceToggleController: Keine EchoFace-Komponente in der Scene gefunden!");
                enabled = false;
                return;
            }
        }

        // Popup initial ausblenden
        if (popupText != null)
            popupText.gameObject.SetActive(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            ToggleEchoFace();
        }
    }

    void ToggleEchoFace()
    {
        bool newState = !echoFace.enabled;
        echoFace.enabled = newState;

        ShowPopup(newState);
    }

    void ShowPopup(bool enabled)
    {
        if (popupText == null)
            return;

        popupText.text =
            $"EchoFace {(enabled ? "aktiviert" : "deaktiviert")}";

        popupText.gameObject.SetActive(true);

        StopAllCoroutines();
        StartCoroutine(HidePopupAfterDelay());
    }

    IEnumerator HidePopupAfterDelay()
    {
        yield return new WaitForSeconds(popupDuration);

        if (popupText != null)
            popupText.gameObject.SetActive(false);
    }
}
