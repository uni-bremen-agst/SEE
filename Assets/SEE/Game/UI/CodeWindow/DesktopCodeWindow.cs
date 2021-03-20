using System.Linq;
using SEE.GO;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
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

            Object codeWindowPrefab = Resources.Load<GameObject>(CODE_WINDOW_PREFAB);
            codeWindow = Instantiate(codeWindowPrefab, Canvas.transform, false) as GameObject;
            if (!codeWindow)
            {
                Debug.LogError("Couldn't instantiate codeWindow.\n");
                return;
            }

            // Set resolution to preferred values
            if (codeWindow.TryGetComponentOrLog(out RectTransform rect))
            {
                rect.sizeDelta = Resolution;
            }

            // Set title, text and preferred font size
            codeWindow.transform.Find("Dragger/Title").gameObject.GetComponent<TextMeshProUGUI>().text = Title;
            if (codeWindow.transform.Find("Content/Scrollable/Code").gameObject.TryGetComponentOrLog(out TextMesh))
            {
                TextMesh.text = Text;
                TextMesh.fontSize = FontSize;
            }

            // Register events to find out when window was scrolled in 
            // For this, we have to register two events in two components, namely Scrollbar and ScrollRect, with
            // OnEndDrag and OnScroll.
            if (codeWindow.transform.Find("Content/Scrollable").gameObject.TryGetComponentOrLog(out scrollRect))
            {
                if (scrollRect.gameObject.TryGetComponentOrLog(out EventTrigger trigger))
                {
                    trigger.triggers.ForEach(x => x.callback.AddListener(_ => ScrollEvent.Invoke()));
                    if (!trigger.triggers.Any())
                    {
                        Debug.LogError("Event Trigger in 'ScrollRect' isn't set up correctly. "
                                       + "Triggers for the 'EndDrag' and 'Scroll' event need to be added.\n");
                    }
                }

                if (scrollRect.transform.Find("Scrollbar").gameObject.TryGetComponentOrLog(out trigger))
                {
                    trigger.triggers.ForEach(x => x.callback.AddListener(_ => ScrollEvent.Invoke()));
                    if (!trigger.triggers.Any())
                    {
                        Debug.LogError("Event Trigger in 'Scrollbar' isn't set up correctly. "
                                       + "Triggers for the 'EndDrag' and 'Scroll' event need to be added.\n");
                    }
                }
            }
            
            RecalculateExcessLines();

            // Position code window in center of screen
            codeWindow.transform.localPosition = Vector3.zero;

            // Animate scrollbar to scroll to desired line
            VisibleLine = Mathf.Max(0, Mathf.FloorToInt(PreStartLine)-1);
        }

        /// <summary>
        /// Recalculates the <see cref="excessLines"/> using the current window height and line height of the text.
        /// This method should be called every time the window height or the line height changes.
        /// For more information, see the documentation of <see cref="excessLines"/>.
        /// </summary>
        public void RecalculateExcessLines()
        {
            TextMesh.ForceMeshUpdate();
            if (lines > 0 && codeWindow.transform.Find("Content/Scrollable").gameObject.TryGetComponentOrLog(out RectTransform rect))
            {
                excessLines = Mathf.CeilToInt(rect.rect.height / TextMesh.textInfo.lineInfo[0].lineHeight) - 2;
            }
        }
    }
}