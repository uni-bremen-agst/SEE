using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using Cysharp.Threading.Tasks;
using SEE.Controls;
using SEE.Game.UI.Notification;
using SEE.GO;
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

        private TMP_InputField.TextSelectionEvent textSelection = null;

        private Tuple<int, int> selectedText;

        /// <summary>
        /// Saves the Current time for the Cooldown
        /// </summary>
        public float timeStamp = 0;

        /// <summary>
        /// The old index, if changes happens faster than the carret moves
        /// </summary>
        private int oldIDX = -1;

        /// <summary>
        /// Indicates that a changes was made in the CodeWindow and the inputlistener has to react
        /// </summary>
        private bool valueHasChanged = false;

        private float oldIDXCoolDown = 0f;
        /// <summary>
        /// The Type of a remote operation
        /// </summary>
        public enum operationType
        {
            /// <summary>
            /// Add a char to the codewindow
            /// </summary>
            Add,

            /// <summary>
            /// Remove a char from the CodeWindow
            /// </summary>
            Delete
        }

        private KeyCode oldKeyCode;

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
            if (codeWindow.transform.Find("Content/Scrollable/Code").gameObject.TryGetComponentOrLog(out TextMesh)
            && codeWindow.transform.Find("Content/Scrollable/Code").gameObject.TryGetComponentOrLog(out TextMeshInputField))
            {
                TextMesh.fontSize = FontSize;
                TextMeshInputField.interactable = true;
                TextMeshInputField.text = TextMesh.text = Text; 

                if (ICRDT.IsEmpty(Title))
                {
                    TextMeshInputField.enabled = false;
                    AddStringStart().Forget();
                }
                else
                {
                   // TextMeshInputField.text = ICRDT.PrintString(Title);
                    EnterFromTokens(SEEToken.fromString(removeLineNumbers(ICRDT.PrintString(Title)), TokenLanguage.fromFileExtension(Path.GetExtension(FilePath)?.Substring(1))));
                    TextMeshInputField.text = TextMesh.text = Text;
                }

                //Change Listener
                ICRDT.GetChangeEvent(Title).AddListener(updateCodeWindow);
                TextMeshInputField.onTextSelection.AddListener((text, start, end) => { selectedText = new Tuple<int, int>(GetCleanIndex(start), GetCleanIndex(end)); });
                TextMeshInputField.onEndTextSelection.AddListener((text, start, end) => { selectedText = null; });
                TextMeshInputField.onValueChanged.AddListener((text) => { valueHasChanged = true;});

                //Updates the entries in the CodeWindow
                void updateCodeWindow(char c, int idx, operationType type)
                {
                    switch (type)
                    {
                        case operationType.Add:
                            TextMeshInputField.text = TextMeshInputField.text.Insert(GetRichIndex(idx), c.ToString());
                            if(TextMeshInputField.caretPosition > idx)
                            {
                                TextMeshInputField.caretPosition = TextMeshInputField.caretPosition + 1;
                            }
                            break;
                        case operationType.Delete:
                            TextMeshInputField.text = TextMeshInputField.text.Remove(GetRichIndex(idx), 1);
                            if (TextMeshInputField.caretPosition > idx)
                            {
                                TextMeshInputField.caretPosition = TextMeshInputField.caretPosition -1;
                            }
                            break;
                    }
                }
            }
            //Initial cooldown for a recalculating of the highliting
            timeStamp = Time.time + 5.0f;

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

        /// <summary>
        /// Tooltip containing all issue descriptions.
        /// </summary>
        private Tooltip.Tooltip issueTooltip;

        protected override void UpdateDesktop()
        {
            //Input Handling
            if (TextMeshInputField.isFocused)
            {
                 if(Time.time < oldIDXCoolDown)
                {
                    oldIDX = -1;
                }
                SEEInput.KeyboardShortcutsEnabled = false;
                if (SEEInput.SaveCodeWindow())
                {
                    try
                    {
                        File.WriteAllText(FilePath, removeLineNumbers(ICRDT.PrintString(Title)));
                        ShowNotification.Info("Saving Successfull", "File " + Title + " was saved succesfully");
                    }
                    catch(Exception e) when (e is DirectoryNotFoundException || e is PathTooLongException || e is IOException 
                    ||e is NotSupportedException || e is ArgumentNullException || e is UnauthorizedAccessException || e is SecurityException )
                    { 
                        ShowNotification.Error("Saving Failed", e.Message);
                    }
                }

                if (SEEInput.CodeWindowUndo())
                {
                    ICRDT.Undo(Title);
                }
                if (SEEInput.CodeWindowRedo())
                {
                    ICRDT.Redo(Title);
                }

               /* if (recalculadeSyntax && Time.time > timeStamp)
                {
                    timeStamp = Time.time + 5.0f;
                    recalculadeSyntax = false;
                    //EnterFromTokens(SEEToken.fromString(removeLineNumbers(ICRDT.PrintString(Title)), TokenLanguage.fromFileExtension(Path.GetExtension(FilePath)?.Substring(1))));
                    //TextMeshInputField.text = TextMesh.text = Text;
                }
                else if(Time.time > timeStamp)
                {
                    timeStamp = Time.time + 5.0f;
                } */

                int idx = TextMeshInputField.caretPosition;
                //https://stackoverflow.com/questions/56373604/receive-any-keyboard-input-and-use-with-switch-statement-on-unity/56373753
                //get the input
                string input = Input.inputString;
                if(input.Contains("\b"))
                {
                    input = input.Replace("\b", "");
                }

                if (input.Contains("\r"))
                {
                    input = input.Replace("\r", "");
                }
                
                if (!string.IsNullOrEmpty(input) && valueHasChanged)
                {
                    Debug.Log("INDX " + idx);
                    valueHasChanged = false;
                    if(idx == oldIDX)
                    {
                        idx++;
                    }
                    else if(oldIDX > -1 && idx > oldIDX + 1)
                    {
                        idx = oldIDX + 1;
                    }
                    oldIDX = idx;
                    oldIDXCoolDown = Time.time + 0.1f;
                    deleteSelectedText();
                    ICRDT.AddString(input, idx - 1, Title);
                }

                if((Input.GetKey(KeyCode.Return) || Input.GetKey(KeyCode.KeypadEnter)) &&  valueHasChanged)
                {
                    returnPressed(idx);
                } 

                if (Input.GetKey(KeyCode.Delete) && valueHasChanged)
                {
                    valueHasChanged = false;
                    if (!deleteSelectedText())
                    { 
                        ICRDT.DeleteString(idx , idx, Title);
                    }
                }

                if (Input.GetKey(KeyCode.Backspace) && valueHasChanged )
                {
                    if(oldIDX == idx)
                    {
                        if(idx == 0)
                        {
                            return;
                        }
                        idx--;
                    }
                    oldIDX = idx;
                    oldIDXCoolDown = Time.time + 0.1f; 
                    valueHasChanged = false;
                    if (!deleteSelectedText())
                    {
                        ICRDT.DeleteString(idx , idx, Title);
                    }
                    oldKeyCode = KeyCode.Backspace;

                }
                if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && Input.GetKeyDown(KeyCode.V))
                {
                  if (!string.IsNullOrEmpty(GUIUtility.systemCopyBuffer))
                    {
                        deleteSelectedText();
                        ICRDT.AddString(GUIUtility.systemCopyBuffer, idx - GUIUtility.systemCopyBuffer.Length, Title);
                    }
                }
                if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && Input.GetKeyDown(KeyCode.X))
                {
                    deleteSelectedText();
                }


                //catches the changes in the code window that happens on a frame shift
                //so that the code doesnot recognize any more that the key was pressed
                if (valueHasChanged)
                {
                    Debug.Log("Frameshift");
                    switch (oldKeyCode)
                    {
                        case KeyCode.Backspace:
                            ICRDT.DeleteString(idx, idx, Title);
                            break;
                        case KeyCode.Delete:
                            ICRDT.DeleteString(idx +1, idx +1, Title);
                            break;
                        case KeyCode.KeypadEnter:
                            returnPressed(idx);
                            break;
                        case KeyCode.Return:
                            returnPressed(idx);
                            break;
                        default:
                            ICRDT.AddString(input, idx - 1, Title);
                            break;
                    }
                }

            }
            else
            {
                SEEInput.KeyboardShortcutsEnabled = true;
            }
            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.W))
            {
                Debug.Log("FILE:; " + Title);
                Debug.Log(ICRDT.PrintString(Title));

            }

            valueHasChanged = false;

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
        private async UniTask AddStringStart() 
        {
            ShowNotification.Info("Loading editor", "The Editable file is loading, please wait", 10);
            string  cleanText = await AsyncGetCleanText();
            await ICRDT.AsyncAddString(cleanText, 0, Title, true);
            TextMeshInputField.enabled = true;
            ShowNotification.Info("Editor ready", "You now can use the editor");
        }

        private string removeLineNumbers(string textWithNumbers)
        {
            string textWithOutNumbers = string.Join("\n", textWithNumbers.Split('\n').Select((x, i) => {
                if (x.Length > 0)
                {
                    return x.Substring(neededPadding +1);
                }
                else
                {
                    return x;
                }
            }).ToList());

            return textWithOutNumbers;
        }
        private void returnPressed(int idx)
        {
            valueHasChanged = false;
            if (idx == oldIDX)
            {
                idx++;
            }
            oldIDX = idx;
            oldIDXCoolDown = Time.time + 0.1f;
            deleteSelectedText();
            TextMeshInputField.text = TextMeshInputField.text.Insert(GetRichIndex(idx), new string(' ', neededPadding + 1));
            TextMeshInputField.MoveToEndOfLine(false, false);
            ICRDT.AddString("\n" + new string(' ', neededPadding + 1), idx - 1, Title);
        }

        private bool deleteSelectedText()
        {
            if (selectedText != null)
            {
                ICRDT.DeleteString(selectedText.Item1, selectedText.Item2 -1, Title);
                return true;
            }
            return false;
        }
    }
}
