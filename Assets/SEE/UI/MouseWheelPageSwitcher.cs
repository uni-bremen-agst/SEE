using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Utilities.WebRequestRest.Interfaces;
using SEE.Utils;
using SEE.UI.Notification;
namespace SEE.UI { 
    public class MouseWheelPageSwitcher : MonoBehaviour
{
    public TextMeshProUGUI textMesh;
    private GraphicRaycaster raycaster;
    private EventSystem eventSystem;
    void Start()
    {
        // Hol den Raycaster und EventSystem aus der Szene
        raycaster = GetComponentInParent<Canvas>().GetComponent<GraphicRaycaster>();
        eventSystem = EventSystem.current;
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0)) HandlePage(1);   // Linksklick
        if (Input.GetMouseButtonDown(1)) HandlePage(-1);  // Rechtsklick
        if (Input.GetMouseButtonDown(2)) HandleCopy();    // Mittelklick
    }

    void HandlePage(int index)
    {
        if (!IsPointerOverTextMesh()) return;
        int newPage = textMesh.pageToDisplay + index;
        newPage = Mathf.Clamp(newPage, 1, textMesh.textInfo.pageCount);
        textMesh.pageToDisplay = newPage;
    }

    void HandleCopy()
    {
        if (!IsPointerOverTextMesh()) return;
        ShowNotification.Info("Info", $"Copy Value:{textMesh.text}", 10, true);
        CopyTextToClipboard(textMesh.text);
    }

    bool IsPointerOverTextMesh()
    {
        PointerEventData pointerData = new PointerEventData(eventSystem)
        {
            position = Input.mousePosition
        };

        List<RaycastResult> results = new List<RaycastResult>();
        raycaster.Raycast(pointerData, results);

        foreach (RaycastResult result in results)
        {
            if (result.gameObject == textMesh.gameObject) return true;
        }
        return false;
    }

    void CopyTextToClipboard(string text)
    {
#if UNITY_EDITOR
        UnityEditor.EditorGUIUtility.systemCopyBuffer = text;
#else
            GUIUtility.systemCopyBuffer = text;
#endif
    }

}
}
