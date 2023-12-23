using System;
using System.IO;
using System.Linq;
using System.Security;
using Cysharp.Threading.Tasks;
using SEE.Controls;
using SEE.Game;
using SEE.Game.City;
using SEE.UI.Notification;
using SEE.GO;
using SEE.IDE;
using SEE.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static SEE.Utils.CRDT;

namespace SEE.UI.Window.CodeWindow
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
        /// Constant defining whether code window manipulation (i.e., editing source code) should be enabled.
        /// TODO: Let the user set this, e.g., within the code city menu.
        /// FIXME: My Unity editor crashes whenever I set this to true and open a code window.
        /// </summary>
        private const bool inputEnabled = false;

        /// <summary>
        /// Represents the TextMeshInputField from the code window in which the user can edit text.
        /// </summary>
        private TMP_InputField textMeshInputField;

        /// <summary>
        /// The old index of the caret inside the text. Used to calculate the real index if the caret
        /// position change is slower or faster than the real input.
        /// </summary>
        private int oldIDX = -1;

        /// <summary>
        /// Indicates that a change was made in the CodeWindow and the inputlistener has to react.
        /// </summary>
        private bool valueHasChanged;

        /// <summary>
        /// A timestamp set after every manipulation of the code window that is used to know when
        /// the <see cref="oldIDX"/> should be reset.
        /// </summary>
        private float oldIDXCoolDown;

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
        private bool fixSelection;

        /// <summary>
        /// The key that has been pressed last.
        /// </summary>
        private KeyCode oldKeyCode;

        protected override void StartDesktop()
        {
            if (text == null)
            {
                Debug.LogError("Text must be defined when setting up CodeWindow!\n");
                return;
            }

            base.StartDesktop();

            GameObject scrollable = PrefabInstantiator.InstantiatePrefab(codeWindowPrefab, Window.transform.Find("Content"), false);
            scrollable.name = "Scrollable";

            // Set text and preferred font size
            GameObject code = scrollable.transform.Find("Code").gameObject;
            if (code.TryGetComponentOrLog(out textMesh) && code.TryGetComponentOrLog(out textMeshInputField))
            {
                textMesh.fontSize = FontSize;
                textMeshInputField.interactable = inputEnabled;
                textMeshInputField.text = textMesh.text = text;

                if (inputEnabled)
                {
                    // Add the text of the code window to the crdt if the crdt is empty.
                    if (ICRDT.IsEmpty(Title))
                    {
                        textMeshInputField.enabled = false;
                        AddStringStartAsync().Forget();
                    }
                    else
                    {
                        EnterFromTokens(SEEToken.FromString(RemoveLineNumbers(ICRDT.PrintString(Title)), TokenLanguage.FromFileExtension(Path.GetExtension(FilePath)?[1..])));
                        textMeshInputField.text = textMesh.text = text;
                    }

                    // Change Listener that listens for remote changes in the crdt to add them to the code window
                    ICRDT.GetChangeEvent(Title).AddListener(UpdateCodeWindow);
                    textMeshInputField.onTextSelection.AddListener((_, start, end) =>
                    {
                        int clean = GetCleanIndex(end);
                        int richIdx = GetRichIndex(clean - 1);
                        if (textMeshInputField.text[richIdx].Equals('\n'))
                        {
                            clean--;
                            fixSelection = true;
                        }

                        selectedText = new Tuple<int, int>(GetCleanIndex(start), clean);
                    });

                    textMeshInputField.onEndTextSelection.AddListener((_, _, _) => { selectedText = null; });
                    textMeshInputField.onValueChanged.AddListener(_ => { valueHasChanged = true; });

                    // Updates the entries in the CodeWindow.
                    void UpdateCodeWindow(char c, int idx, OperationType type)
                    {
                        switch (type)
                        {
                            case OperationType.Add:
                                textMeshInputField.text = textMeshInputField.text.Insert(GetRichIndex(idx), c.ToString());
                                if (textMeshInputField.caretPosition > idx)
                                {
                                    textMeshInputField.caretPosition++;
                                }

                                break;
                            case OperationType.Delete:
                                textMeshInputField.text = textMeshInputField.text.Remove(GetRichIndex(idx), 1);
                                if (textMeshInputField.caretPosition > idx)
                                {
                                    textMeshInputField.caretPosition--;
                                }

                                break;
                            default: throw new ArgumentOutOfRangeException(nameof(type), type, null);
                        }
                    }
                }
            }

            if (SceneQueries.GetCodeCity(transform).gameObject.TryGetComponentOrLog(out AbstractSEECity city)) {
                // Get button for IDE interaction and register events.
                Window.transform.Find("Dragger/IDEButton").gameObject.GetComponent<Button>()
                      .onClick.AddListener(() =>
                      {
                          IDEIntegration.Instance?.OpenFileAsync(FilePath, city.SolutionPath.Path, markedLine).Forget();
                      });
            }

            // Register events to find out when window was scrolled in.
            // For this, we have to register two events in two components, namely Scrollbar and ScrollRect, with
            // OnEndDrag and OnScroll.
            if (scrollable.TryGetComponentOrLog(out scrollRect))
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

            // Animate scrollbar to scroll to desired line
            ScrolledVisibleLine = Mathf.Clamp(Mathf.FloorToInt(preStartLine), 1, lines);
        }

        /// <summary>
        /// Tooltip containing all issue descriptions.
        /// </summary>
        private Tooltip.Tooltip issueTooltip;

        protected override void UpdateDesktop()
        {
            // Input handling of the code window.
            if (inputEnabled && textMeshInputField.isFocused)
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
                        ShowNotification.Info("Successful Saving", "File " + Title + " was saved successfully");
                    }
                    catch (Exception e) when (e is DirectoryNotFoundException or PathTooLongException
                                                  or IOException or NotSupportedException or ArgumentNullException
                                                  or UnauthorizedAccessException or SecurityException)
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
                    catch (UndoNotPossibleExcpetion e)
                    {
                        ShowNotification.Error("Undo Failure", e.Message);
                    }
                }

                if (SEEInput.CodeWindowRedo())
                {
                    ShowNotification.Info("Redo", "");
                    try
                    {
                        ICRDT.Redo(Title);
                    }
                    catch (RedoNotPossibleException e)
                    {
                        ShowNotification.Error("Redo Failure", e.Message);
                    }
                }

                // Renew the syntax highlighting (currently only on user request).
                if (SEEInput.ReCalculateSyntaxHighlighting())
                {
                    ShowNotification.Info("Reloading Code", "");
                    EnterFromTokens(SEEToken.FromString(RemoveLineNumbers(ICRDT.PrintString(Title)),
                                                        TokenLanguage.FromFileExtension(Path.GetExtension(FilePath)?[1..])));
                    textMeshInputField.text = textMesh.text = text;
                    ShowNotification.Info("Reloading Code Complete", "Recalculating syntax highlighting finished");
                }

                // https://stackoverflow.com/questions/56373604/receive-any-keyboard-input-and-use-with-switch-statement-on-unity/56373753
                // Get the input.
                int idx = textMeshInputField.caretPosition;
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
                        textMeshInputField.text = textMeshInputField.text.Insert(GetRichIndex(idx), "\n");
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
                int link = TMP_TextUtilities.FindIntersectingLink(textMesh, Input.mousePosition, null);
                if (link != -1)
                {
                    char linkId = textMesh.textInfo.linkInfo[link].GetLinkID()[0];
                    issueTooltip ??= gameObject.AddComponent<Tooltip.Tooltip>();
                    // Display tooltip containing all issue descriptions
                    UniTask.WhenAll(issueDictionary[linkId].Select(x => x.ToDisplayStringAsync()))
                           .ContinueWith(x => issueTooltip.Show(string.Join("\n", x), 0f))
                           .Forget();
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
        private void RecalculateExcessLines()
        {
            try
            {
                textMesh.ForceMeshUpdate();
            }
            catch (IndexOutOfRangeException)
            {
                //FIXME: Use multiple TMPs: Either one as an overlay, or split the main TMP up into multiple ones.
                ShowNotification.Error("File too big", "This file is too large to be displayed correctly.");
            }

            if (lines > 0 && Window.transform.Find("Content/Scrollable").gameObject.TryGetComponentOrLog(out RectTransform rect))
            {
                excessLines = Mathf.CeilToInt(rect.rect.height / textMesh.textInfo.lineInfo[0].lineHeight) - 2;
            }
        }

        /// <summary>
        /// An async method to add the text into the crdt while the user can already read the content of the file.
        /// </summary>
        /// <returns></returns>
        private async UniTask AddStringStartAsync()
        {
            ShowNotification.Info("Loading editor", "The Editable file is loading, please wait");
            string cleanText = await GetCleanTextAsync();
            await ICRDT.AddStringAsync(cleanText, 0, Title, true);
            textMeshInputField.enabled = true;
            ShowNotification.Info("Editor ready", "You now can use the editor");
        }

        /// <summary>
        /// Removes the line numbers from the text.
        /// </summary>
        /// <param name="textWithNumbers">The text with line numbers</param>
        /// <returns>The text without the line numbers</returns>
        private string RemoveLineNumbers(string textWithNumbers)
        {
            return string.Join("\n", textWithNumbers.Split('\n').Select((x, _) => x.Length > 0 ? x[(neededPadding + 1)..] : x).ToList());
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
            if (DeleteSelectedText() && fixSelection)
            {
                textMeshInputField.text = textMeshInputField.text.Insert(GetRichIndex(idx), "\n");
                fixSelection = false;
            }

            textMeshInputField.text = textMeshInputField.text.Insert(GetRichIndex(idx), new string(' ', neededPadding + 1));
            textMeshInputField.MoveToEndOfLine(false, false);
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
