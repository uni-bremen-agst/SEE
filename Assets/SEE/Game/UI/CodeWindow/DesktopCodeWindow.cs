using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using SEE.Controls;
using SEE.Game.UI.Notification;
using SEE.GO;
using SEE.Utils;
using Sirenix.Utilities.Editor;
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

        private TMP_InputField.TextSelectionEvent textSelection = null;

        private Tuple<int, int> selectedText;

        /// <summary>
        /// Saves the Current time for the Cooldown
        /// </summary>
        public float timeStamp = 0; //Time.time;

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
            Debug.LogWarning("TEST1");
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
            if (codeWindow.transform.Find("Content/Scrollable/Code").gameObject.TryGetComponentOrLog(out TextMesh)
            && codeWindow.transform.Find("Content/Scrollable/Code").gameObject.TryGetComponentOrLog(out TextMeshInputField))
            {
                //TextMesh.text = Text;
                TextMesh.fontSize = FontSize;

                TextMeshInputField.enabled = true;
                TextMeshInputField.interactable = true;
                int neededPadding = 1; // TODO: Use real padding
                //FIXME: startIndex too big
                List<string> textWitzhOutNumbers = Text.Split('\n')
                                                                   .Select((x, i) =>
                                                                   {
                                                                       string cleanLine = GetCleanLine(i);
                                                                       if (cleanLine.Length > 0)
                                                                       {
                                                                           return cleanLine.Substring(neededPadding);
                                                                       }
                                                                       else
                                                                       {
                                                                           return cleanLine;
                                                                       }

                                                                   }).ToList();
                TextMeshInputField.text = Text;//string.Join("\n", textWitzhOutNumbers); 
                                               //TextMeshInputField.caretPosition = 1;
                if(ICRDT.PrintString(Title) == null || ICRDT.PrintString(Title) == "")
                {
                    ICRDT.AddString(Text, 0, Title);
                }
                Debug.LogWarning("TEST2");
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
            Debug.LogWarning("TEST3");
        }

        /// <summary>
        /// Tooltip containing all issue descriptions.
        /// </summary>
        private Tooltip.Tooltip issueTooltip;

        protected override void UpdateDesktop()
        {
            Debug.LogWarning("TEST4");
            //Input Handling
            if (TextMeshInputField.isFocused)
            {
                SEEInput.KeyboardShortcutsEnabled = false;
                TextMeshInputField.onTextSelection.AddListener((text, start, end) => { selectedText = new Tuple<int, int>(start, end); });
                TextMeshInputField.onEndTextSelection.AddListener((text, start, end) => { selectedText = null; });

                //https://stackoverflow.com/questions/56373604/receive-any-keyboard-input-and-use-with-switch-statement-on-unity/56373753
                //get the input
                var input = Input.inputString;
                //ignore null input to avoid unnecessary computation
                if (!string.IsNullOrEmpty(input))
                {
                    if (selectedText != null)
                    {
                        ICRDT.DeleteString(selectedText.Item1, selectedText.Item2, Title);
                    }
                    ICRDT.AddChar(input[0], TextMeshInputField.stringPosition -1, Title);
                } 

                if (Input.GetKey(KeyCode.Delete) && ICRDT.PrintString(Title).Length > TextMeshInputField.stringPosition && timeStamp <= Time.time)
                {
                    timeStamp = Time.time + 0.100000f;
                    if (selectedText != null)
                    {
                        ICRDT.DeleteString(selectedText.Item1, selectedText.Item2, Title);
                    }
                    else
                    {
                        ICRDT.DeleteChar(TextMeshInputField.stringPosition, Title);
                    }
                }

                if (((Input.GetKey(KeyCode.Backspace)  && timeStamp <= Time.time) || Input.GetKeyDown(KeyCode.Backspace)) && TextMeshInputField.stringPosition > 0)
                {
                    timeStamp = Time.time + 0.100000f;
                    Debug.LogWarning("DELETE");
                    if (selectedText != null)
                    {
                        ICRDT.DeleteString(selectedText.Item1, selectedText.Item2, Title);
                    }
                    else
                    {
                        ICRDT.DeleteChar(TextMeshInputField.stringPosition+1, Title);
                    }

                }
                if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && Input.GetKeyDown(KeyCode.V))
                {
                    if (Clipboard.CanPaste<string>())
                    {
                        if (selectedText != null)
                        {
                            ICRDT.DeleteString(selectedText.Item1, selectedText.Item2, Title);
                        }
                        ICRDT.AddString(Clipboard.Paste<string>(), TextMeshInputField.stringPosition, Title);

                    }
                }
                if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && Input.GetKeyDown(KeyCode.X))
                {
                    if (selectedText != null)
                    {
                        ICRDT.DeleteString(selectedText.Item1, selectedText.Item2, Title);
                    }
                }
               
            }
            SEEInput.KeyboardShortcutsEnabled = true;
            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.W))
            {
                Debug.Log("FILE:; " + Title);
                Debug.Log(ICRDT.PrintString(Title));
               
            }
            
          
            //Debug.Log("FILE:; " + Title);
            // Debug.Log(ICRDT.PrintString(Title));

            // Show issue info on click (on hover would be too expensive)
            if (issueDictionary.Count != 0 && Input.GetMouseButtonDown(0))
            {
                // Passing camera as null causes the screen space rather than world space camera to be used
                int link = TMP_TextUtilities.FindIntersectingLink(TextMesh, Input.mousePosition, null);
                if (link != -1)
                {
                    char linkId = TextMesh.textInfo.linkInfo[link].GetLinkID()[0];
                    issueTooltip ??= gameObject.AddComponent<Tooltip.Tooltip>();
                    // Display tooltip containing all issue descriptions
                    UniTask.WhenAll(issueDictionary[linkId].Select(x => x.ToDisplayString()))
                           .ContinueWith(x => issueTooltip.Show(string.Join("\n", x), 0f));
                }
                else if (issueTooltip != null)
                {
                    // Hide tooltip by clicking somewhere else
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
            try
            {
                TextMesh.ForceMeshUpdate();
            }
            catch (IndexOutOfRangeException)
            {
                //FIXME: Use multiple TMPs: Either one as an overlay, or split the main TMP up into multiple ones.
                ShowNotification.Error("File too big", "This file is too large to be displayed correctly.");
            }

            if (lines > 0 && codeWindow.transform.Find("Content/Scrollable").gameObject.TryGetComponentOrLog(out RectTransform rect))
            {
                excessLines = Mathf.CeilToInt(rect.rect.height / TextMesh.textInfo.lineInfo[0].lineHeight) - 2;
            }
        }
    }
}
