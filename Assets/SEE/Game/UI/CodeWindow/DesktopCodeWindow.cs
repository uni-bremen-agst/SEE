using System.Linq;
using Cysharp.Threading.Tasks;
using SEE.GO;
using SEE.Net.Dashboard.Model.Issues;
using SEE.Utils;
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

            codeWindow = PrefabInstantiator.InstantiatePrefab(CODE_WINDOW_PREFAB, Canvas.transform, false);

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

            // Register events to find out when window was scrolled in.
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
            VisibleLine = Mathf.Clamp(Mathf.FloorToInt(PreStartLine), 1, lines);
        }

        private Tooltip.Tooltip issueTooltip;

        protected override void UpdateDesktop()
        {
            // Passing camera as null causes the screen space rather than world space camera to be used
            if (issueDictionary.Count != 0 && Input.GetMouseButtonDown(0))
            {
                // Show issue info on click
                int link = TMP_TextUtilities.FindIntersectingLink(TextMesh, Input.mousePosition, null);
                if (link != -1 && int.TryParse(TextMesh.textInfo.linkInfo[link].GetLinkID(), out int issueId) 
                               && issueDictionary.TryGetValue(issueId, out Issue issue))
                {
                    issueTooltip ??= gameObject.AddComponent<Tooltip.Tooltip>();
                    issue.ToDisplayString().ContinueWith(x => issueTooltip.Show(x, 0f));
                }
                else if (issueTooltip != null)
                {
                    issueTooltip.Hide();
                }
            } 
            else if (issueDictionary.Count != 0 && Input.GetMouseButtonDown(1) && issueTooltip != null)
            {
                // Hide tooltip by right-clicking
                issueTooltip.Hide();
            }

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
