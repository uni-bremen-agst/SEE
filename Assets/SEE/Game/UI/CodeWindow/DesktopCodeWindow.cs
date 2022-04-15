using System;
using System.IO;
using System.Linq;
using System.Security;
using Cysharp.Threading.Tasks;
using SEE.Controls;
using SEE.Game.UI.Notification;
using SEE.GO;
using SEE.IDE;
using SEE.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static SEE.Utils.CRDT;

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
        /// Contains the start and end index of the selected (highlighted with the mouse) text
        /// from the code window.
        /// </summary>
        private Tuple<int, int> selectedText;

        /// <summary>
        /// Represents the TextMeshInputField from the code window in which the user can edit text.
        /// </summary>
        private TMP_InputField TextMeshInputField;

        /// <summary>
        /// The old index of the carret inside the text. Used to calculate the real index if the carret
        /// position change is slower or faster than the real input.
        /// </summary>
        private int oldIDX = -1;

        /// <summary>
        /// Indicates that a change was made in the CodeWindow and the inputlistener has to react.
        /// </summary>
        private bool valueHasChanged = false;

        /// <summary>
        /// A timestamp set after every manipulation of the code window that is used to know when
        /// the <see cref="oldIDX"/> should be reset.
        /// </summary>
        private float oldIDXCoolDown = 0f;

        /// <summary>
        /// The type of a remote operation.
        /// </summary>
        public enum OperationType
        {
            /// <summary>
            /// Add a character to the CodeWindow.
            /// </summary>
            Add,

            /// <summary>
            /// Remove a character from the CodeWindow.
            /// </summary>
            Delete
        }

        /// <summary>
        /// An attempt to fix the bug that the TMP selects too many characters if you select
        /// a word at the end of a line with ctrl + rightArrow.
        /// </summary>
        private bool fixSelection = false;

        /// <summary>
        /// The key that has been pressed last.
        /// </summary>
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

                // Add the text of the code window to the crdt if the crdt is empty.
                if (ICRDT.IsEmpty(Title))
                {
                    TextMeshInputField.enabled = false;
                    AddStringStart().Forget();
                }
                else
                {
                    EnterFromTokens(SEEToken.fromString(RemoveLineNumbers(ICRDT.PrintString(Title)), TokenLanguage.fromFileExtension(Path.GetExtension(FilePath)?.Substring(1))));
                    TextMeshInputField.text = TextMesh.text = Text;
                }

                // Change Listener that listens for remote changes in the crdt to add them to the code window
                ICRDT.GetChangeEvent(Title).AddListener(UpdateCodeWindow);
                TextMeshInputField.onTextSelection.AddListener((text, start, end) => {
                    int clean = GetCleanIndex(end);
                    int richIdx = GetRichIndex(clean - 1);
                    if (TextMeshInputField.text[richIdx].Equals('\n'))
                    {
                        clean--;
                        fixSelection = true;
                    }
                   selectedText = new Tuple<int, int>(GetCleanIndex(start), clean);
                });

                TextMeshInputField.onEndTextSelection.AddListener((text, start, end) => { selectedText = null; });
                TextMeshInputField.onValueChanged.AddListener((text) => { valueHasChanged = true; });

                // Updates the entries in the CodeWindow.
                void UpdateCodeWindow(char c, int idx, OperationType type)
                {
                    switch (type)
                    {
                        case OperationType.Add:
                            TextMeshInputField.text = TextMeshInputField.text.Insert(GetRichIndex(idx), c.ToString());
                            if (TextMeshInputField.caretPosition > idx)
                            {
                                TextMeshInputField.caretPosition++;
                            }
                            break;
                        case OperationType.Delete:
                            TextMeshInputField.text = TextMeshInputField.text.Remove(GetRichIndex(idx), 1);
                            if (TextMeshInputField.caretPosition > idx)
                            {
                                TextMeshInputField.caretPosition--;
                            }
                            break;
                    }
                }
            }

            // Get button for IDE interaction and register events.
            codeWindow.transform.Find("Dragger/IDEButton").gameObject.GetComponent<Button>()
                .onClick.AddListener(() =>
                {
                    IDEIntegration.Instance?.OpenFile(FilePath, SolutionPath, markedLine).Forget();
                });


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
            // Input handling of the code window.
            if (TextMeshInputField.isFocused)
            {
                // resets the old index after the cooldown expires.
                if (Time.time > oldIDXCoolDown)
                {
                    oldIDX = -1;
                }

                SEEInput.KeyboardShortcutsEnabled = false;

                // Saves the changes made inside the code window.
                if (SEEInput.SaveCodeWindow())
                {
                    try
                    {
                        File.WriteAllText(FilePath, RemoveLineNumbers(ICRDT.PrintString(Title)));
                        ShowNotification.Info("Successful Saving", "File " + Title + " was saved succesfully");
                    }
                    catch (Exception e) when (e is DirectoryNotFoundException || e is PathTooLongException || e is IOException
                    || e is NotSupportedException || e is ArgumentNullException || e is UnauthorizedAccessException || e is SecurityException)
                    {
                        ShowNotification.Error("Saving Failed", e.Message);
                    }
                }

                // Undo / Redo handling.
                if (SEEInput.CodeWindowUndo())
                {
                    ShowNotification.Info("Undo", "");
                    try
                    {
                        ICRDT.Undo(Title);
                    }
                    catch(UndoNotPossibleExcpetion e)
                    {
                        ShowNotification.Error("Undo failure", e.Message);
                    }
                }
                if (SEEInput.CodeWindowRedo())
                {
                    ShowNotification.Info("Redo", "");
                    try
                    {
                        ICRDT.Redo(Title);
                    }
                    catch(RedoNotPossibleException e)
                    {
                        ShowNotification.Error("Redo failure", e.Message);
                    }
                }

                // Renew the syntax highliting (currently only on user request).
                if (SEEInput.ReCalculateSyntaxHighliting())
                {
                    ShowNotification.Info("Reloading Code", "");
                    EnterFromTokens(SEEToken.fromString(RemoveLineNumbers(ICRDT.PrintString(Title)), TokenLanguage.fromFileExtension(Path.GetExtension(FilePath)?.Substring(1))));
                    TextMeshInputField.text = TextMesh.text = Text;
                    ShowNotification.Info("Reloading Code Complete", "Recalculating Syntaxhighliting finished");
                }

                // https://stackoverflow.com/questions/56373604/receive-any-keyboard-input-and-use-with-switch-statement-on-unity/56373753
                // Get the input.
                int idx = TextMeshInputField.caretPosition;
                string input = Input.inputString;

                // Remove special chars that should not be in the string.
                if (input.Contains("\b"))
                {
                    input = input.Replace("\b", "");
                }
                if (input.Contains("\r"))
                {
                    input = input.Replace("\r", "");
                }
                if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
                {
                    input = "";
                }

                // Insert the input string into the crdt.
                if (!string.IsNullOrEmpty(input) && valueHasChanged)
                {
                    valueHasChanged = false;
                    if (idx == oldIDX)
                    {
                        idx++;
                    }
                    else if (oldIDX > -1 && idx > oldIDX + 1)
                    {
                        idx = oldIDX + 1;
                    }
                    oldIDX = idx;
                    oldIDXCoolDown = Time.time + 0.1f;
                    DeleteSelectedText();
                    ICRDT.AddString(input, idx - input.Length, Title);
                    oldKeyCode = KeyCode.A;
                }

                // Handle other special keys such as delete, ctrl+v...
                if ((Input.GetKey(KeyCode.Return) || Input.GetKey(KeyCode.KeypadEnter)) && valueHasChanged)
                {
                    ReturnPressed(idx);
                    oldKeyCode = KeyCode.Return;
                    valueHasChanged = false;
                }

                if (Input.GetKey(KeyCode.Delete) && valueHasChanged)
                {
                    valueHasChanged = false;
                    if (!DeleteSelectedText())
                    {
                        ICRDT.DeleteString(idx, idx, Title);
                    }
                    oldKeyCode = KeyCode.Delete;
                }

                if (Input.GetKey(KeyCode.Backspace) && valueHasChanged)
                {
                    if (oldIDX == idx)
                    {
                        if (idx == 0)
                        {
                            return;
                        }
                        idx--;
                    }
                    oldIDX = idx;
                    oldIDXCoolDown = Time.time + 0.1f;
                    valueHasChanged = false;
                    if (!DeleteSelectedText())
                    {
                        ICRDT.DeleteString(idx, idx, Title);
                        oldKeyCode = KeyCode.Backspace;
                    }
                    else if (fixSelection)
                    {
                        TextMeshInputField.text = TextMeshInputField.text.Insert(GetRichIndex(idx), "\n");
                        fixSelection = false;
                        oldKeyCode = KeyCode.None;
                    }
                }

                if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && Input.GetKeyDown(KeyCode.V))
                {
                    if (!string.IsNullOrEmpty(GUIUtility.systemCopyBuffer))
                    {
                        DeleteSelectedText();
                        ICRDT.AddString(GUIUtility.systemCopyBuffer, idx - GUIUtility.systemCopyBuffer.Length, Title);
                    }
                }
                if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && Input.GetKeyDown(KeyCode.X))
                {
                    DeleteSelectedText();
                }

                // Catches the changes in the code window that happen on a frame shift
                // so that the code does not recognize any more that the key was pressed.
                if (valueHasChanged && oldKeyCode != KeyCode.None)
                {
                    switch (oldKeyCode)
                    {
                        case KeyCode.Backspace:
                            ICRDT.DeleteString(idx, idx, Title);
                            break;
                        case KeyCode.Delete:
                            ICRDT.DeleteString(idx + 1, idx + 1, Title);
                            break;
                        case KeyCode.KeypadEnter:
                            ReturnPressed(idx);
                            break;
                        case KeyCode.Return:
                            ReturnPressed(idx);
                            break;
                        default:
                            ICRDT.AddString(input, idx - 1, Title);
                            break;
                    }
                    oldKeyCode = KeyCode.None;
                    valueHasChanged = false;
                }
                else
                {
                    oldKeyCode = KeyCode.None;
                }
            }
            else
            {
                SEEInput.KeyboardShortcutsEnabled = true;
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

        /// <summary>
        /// An asynchrone method to add the text into the crdt while the user can already read the content of the file.
        /// </summary>
        /// <returns></returns>
        private async UniTask AddStringStart()
        {
            ShowNotification.Info("Loading editor", "The Editable file is loading, please wait", 10);
            string cleanText = await AsyncGetCleanText();
            await ICRDT.AsyncAddString(cleanText, 0, Title, true);
            TextMeshInputField.enabled = true;
            ShowNotification.Info("Editor ready", "You now can use the editor");
        }

        /// <summary>
        /// Removes the line numbers from the text.
        /// </summary>
        /// <param name="textWithNumbers">The text with line numbers</param>
        /// <returns>The text without the line numbers</returns>
        private string RemoveLineNumbers(string textWithNumbers)
        {
            string textWithOutNumbers = string.Join("\n", textWithNumbers.Split('\n').Select((x, i) =>
            {
                if (x.Length > 0)
                {
                    return x.Substring(neededPadding + 1);
                }
                else
                {
                    return x;
                }
            }).ToList());

            return textWithOutNumbers;
        }

        /// <summary>
        /// Handles the case when the return key is pressed.
        /// </summary>
        /// <param name="idx">the index at which the return should be added</param>
        private void ReturnPressed(int idx)
        {
            valueHasChanged = false;
            if (idx == oldIDX)
            {
                idx++;
            }
            oldIDX = idx;
            oldIDXCoolDown = Time.time + 0.1f;
            if(DeleteSelectedText() && fixSelection)
                {
                    TextMeshInputField.text = TextMeshInputField.text.Insert(GetRichIndex(idx), "\n");
                    fixSelection = false;
                }
            TextMeshInputField.text = TextMeshInputField.text.Insert(GetRichIndex(idx), new string(' ', neededPadding + 1));
            TextMeshInputField.MoveToEndOfLine(false, false);
            ICRDT.AddString("\n" + new string(' ', neededPadding + 1), idx - 1, Title);
        }

        /// <summary>
        /// Deletes the selected text.
        /// </summary>
        /// <returns>returns false if no text was selected and true if text was selected and deleted.</returns>
        private bool DeleteSelectedText()
        {
            if (selectedText != null)
            {
                if (selectedText.Item2 < selectedText.Item1 - 1)
                {
                    ICRDT.DeleteString(selectedText.Item2, selectedText.Item1 - 1, Title);
                }
                else
                {
                    ICRDT.DeleteString(selectedText.Item1, selectedText.Item2 - 1, Title);
                }
                return true;
            }
            return false;
        }
    }
}
