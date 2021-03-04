using SEE.GO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SEE.Game.UI.CodeWindow
{
    /// <summary>
    /// This part of the <see cref="CodeWindow"/> class contains the desktop specific UI code.
    /// </summary>
    public partial class CodeWindow
    {

        /// <summary>
        /// Scrollbar which controls the currently visible area of the code window.
        /// </summary>
        private ScrollRect scrollRect;

        /// <summary>
        /// Shows or hides the code window on Desktop platforms.
        /// </summary>
        /// <param name="show">Whether the code window should be shown.</param>
        private void ShowDesktop(bool show)
        {
            if (codeWindow) 
            {
                codeWindow.SetActive(show);
            }
        }
        
        protected override void StartDesktop()
        {
            if (Title == null || Text == null)
            {
                Debug.LogError("Title and text must be defined when setting up CodeWindow!\n");
                return;
            }

            GameObject group = GameObject.Find(CODE_WINDOWS_NAME);
            if (group == null)
            {
                group = new GameObject {name = CODE_WINDOWS_NAME};
                group.transform.parent = Canvas.transform;
                group.transform.localScale = Vector3.one;
                group.transform.localPosition = Vector3.zero;
            }

            Object codeWindowPrefab = Resources.Load<GameObject>(CODE_WINDOW_PREFAB);
            codeWindow = Instantiate(codeWindowPrefab, group.transform, false) as GameObject;
            if (!codeWindow)
            {
                Debug.LogError("Couldn't instantiate codeWindow.\n");
                return;
            }
            
            // Set resolution to preferred values
            if (codeWindow.TryGetComponentOrLog(out RectTransform rect))
            {
                rect.sizeDelta = Resolution;
                rect.position = Vector3.zero;
            }

            // Set title, text and preferred font size
            codeWindow.transform.Find("Dragger/Title").gameObject.GetComponent<TextMeshProUGUI>().text = Title;
            if (codeWindow.transform.Find("Content/Scrollable/Code").gameObject.TryGetComponentOrLog(out TextMesh))
            {
                TextMesh.text = Text;
                TextMesh.fontSize = FontSize;
            }
            
            // Listen to scrollbar events
            if (codeWindow.transform.Find("Content/Scrollable").gameObject.TryGetComponentOrLog(out scrollRect))
            {
                ScrollEvent = scrollRect.onValueChanged;
            }
            
            // Calculate excess lines (see documentation for excessLines for more details)
            TextMesh.ForceMeshUpdate();
            if (lines > 0 && codeWindow.transform.Find("Content/Scrollable").gameObject.TryGetComponentOrLog(out rect))
            {
                excessLines = Mathf.CeilToInt(rect.rect.height / TextMesh.textInfo.lineInfo[0].lineHeight) - 1;
            }
            
            // Position code window in center of screen
            codeWindow.transform.localPosition = Vector3.zero;

            // Animate scrollbar to scroll to desired line
            VisibleLine = Mathf.Max(0, Mathf.FloorToInt(PreStartLine)-1);
        }
    }
}