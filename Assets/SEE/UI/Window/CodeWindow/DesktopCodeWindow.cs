using System;
using System.Linq;
using Cysharp.Threading.Tasks;
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
using System.Collections.Generic;
using Microsoft.VisualStudio.Shared.VSCodeDebugProtocol.Messages;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using SEE.Controls;
using SEE.UI.DebugAdapterProtocol;
using SEE.Utils.Markdown;
using UnityEngine.Assertions;

namespace SEE.UI.Window.CodeWindow
{
    /// <summary>
    /// This part of the <see cref="CodeWindow"/> class contains the desktop specific UI code.
    /// </summary>
    public partial class CodeWindow
    {
        /// <summary>
        /// The scroll area.
        /// </summary>
        private GameObject scrollable;

        /// <summary>
        /// The container for breakpoints.
        /// </summary>
        private GameObject breakpoints;

        /// <summary>
        /// Scrollbar which controls the currently visible area of the code window.
        /// </summary>
        private ScrollRect scrollRect;

        protected override void StartDesktop()
        {
            if (text == null)
            {
                Debug.LogError("Text must be defined when setting up CodeWindow!\n");
                Destroyer.Destroy(this);
                return;
            }

            base.StartDesktop();
            ActivateWindowDraggerButtons();

            scrollable = PrefabInstantiator.InstantiatePrefab(codeWindowPrefab, Window.transform.Find("Content"), false);
            scrollable.name = "Scrollable";

            // Initialize context menu, if necessary.
            if (lspHandler != null)
            {
                contextMenu = ContextMenuHandler.FromCodeWindow(this);
            }

            // Set text and preferred font size
            GameObject code = scrollable.transform.Find("Code").gameObject;
            if (code.TryGetComponentOrLog(out textMesh))
            {
                textMesh.fontSize = FontSize;
                textMesh.text = text;
            }

            breakpoints = code.transform.Find("Breakpoints").gameObject;
            DebugBreakpointManager.OnBreakpointAdded += OnBreakpointAdded;
            DebugBreakpointManager.OnBreakpointRemoved += OnBreakpointRemoved;

            AbstractSEECity city = SceneQueries.City(transform.gameObject);
            if (city != null)
            {
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
                scrollRect.horizontalNormalizedPosition = 0;
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

            SetupBreakpoints();
        }

        protected override void StartVR()
        {
            StartDesktop();
        }

        /// <summary>
        /// Sets up the breakpoints.
        /// </summary>
        private void SetupBreakpoints()
        {
            bool needsBreakpoints = FilePath != null;

            // destroys previous breakpoints
            foreach (Transform child in breakpoints.transform)
            {
                Destroyer.Destroy(child.gameObject);
            }

            // disables container (not absolutely necessary, but indicates whether it works correctly)
            breakpoints.SetActive(needsBreakpoints);

            // updates indentation of code depending on breakpoints
            float width = textMesh.textInfo.lineInfo[0].lineHeight;
            Vector4 margin = textMesh.margin;
            margin.x = needsBreakpoints ? width : 0;
            textMesh.margin = margin;

            // doesn't need breakpoints without file path
            if (!needsBreakpoints)
            {
                return;
            }

            Dictionary<int, SourceBreakpoint> fileBreakpoints = DebugBreakpointManager.Breakpoints.GetValueOrDefault(FilePath.Replace("/", "\\"));
            for (int i = 0; i <= lines; i++)
            {
                int line = i + 1;

                float height = textMesh.textInfo.lineInfo[i].lineHeight;

                GameObject breakpoint = PrefabInstantiator.InstantiatePrefab(breakpointPrefab, breakpoints.transform, false);
                ((RectTransform)breakpoint.transform).sizeDelta = new Vector2(width, height);

                TextMeshProUGUI buttonMesh = breakpoint.MustGetComponent<TextMeshProUGUI>();
                buttonMesh.color = fileBreakpoints != null && fileBreakpoints.ContainsKey(line) ? breakpointColorActive : breakpointColorInactive;

                Button button = breakpoint.MustGetComponent<Button>();
                button.onClick.AddListener(() => DebugBreakpointManager.ToggleBreakpoint(FilePath.Replace("/", "\\"), line));
            }
        }

        protected override void UpdateDesktop()
        {
            if (WindowSpaceManager.ManagerInstance[WindowSpaceManager.LocalPlayer].ActiveWindow == this)
            {
                // Right-click opens menu with LSP actions.
                if (Input.GetMouseButtonDown(1))
                {
                    if (lspHandler != null)
                    {
                        // We use the word detection instead of the character detection because the latter
                        // needs the cursor to be precisely over a character, while the former works more broadly.
                        if (DetectHoveredWord() is { } word)
                        {
                            int character = word.firstCharacterIndex;
                            (int line, int column) = GetSourcePosition(character);
                            contextMenu.Show(line - 1, column - 1, Input.mousePosition, word.GetWord());
                        }
                        return;
                    }
                }
                if (issueDictionary.Count != 0)
                {
                    // Show issue info by leveraging links we created earlier.
                    // Passing camera as null causes the screen space rather than world space camera to be used.
                    int link = TMP_TextUtilities.FindIntersectingLink(textMesh, Input.mousePosition, null);
                    if (link != -1)
                    {
                        TriggerIssueHoverAsync(link).Forget();
                        return;
                    }
                }

                TMP_WordInfo? hoveredWord = DetectHoveredWord();
                // Detect hovering over words (only while the code window is not being scrolled).
                if (scrollingTo == 0 && !lastHoveredWord.Equals(hoveredWord))
                {
                    if (lspHandler != null)
                    {
                        TriggerLspHoverAsync(hoveredWord).Forget();
                    }

                    if (lastHoveredWord != null)
                    {
                        OnWordHoverEnd?.Invoke(this, lastHoveredWord.Value);
                        RemoveUnderline(lastHoveredWord.Value);
                    }
                    if (hoveredWord != null)
                    {
                        OnWordHoverBegin?.Invoke(this, hoveredWord.Value);

                        // NOTE: We are not using SEEInput because:
                        //       a) Any reasonable key here would conflict with our existing set of keys.
                        //          We would need to implement context-dependent key bindings first.
                        //       b) We need to differentiate between "key is in a pressed state", "key was pressed",
                        //          and "key was released", which goes beyond the general interface of SEEInput.
                        if (Input.GetKey(KeyCode.LeftControl))
                        {
                            UnderlineHoveredWord(hoveredWord.Value);
                        }
                    }

                    lastHoveredWord = hoveredWord;
                }
                else if (Input.GetKeyUp(KeyCode.LeftControl) && lastHoveredWord != null)
                {
                    RemoveUnderline(lastHoveredWord.Value);
                }
                else if (Input.GetKeyDown(KeyCode.LeftControl) && lastHoveredWord != null)
                {
                    UnderlineHoveredWord(lastHoveredWord.Value);
                }

                if (Input.GetMouseButton(0) && Input.GetKey(KeyCode.LeftControl) && lastHoveredWord != null)
                {
                    RemoveUnderline(lastHoveredWord.Value);
                    GoToDefinition(lastHoveredWord.Value);
                }

                if (Input.GetKey(KeyCode.LeftControl) && Input.mouseScrollDelta.y is float delta and not 0)
                {
                    // Change font size by scrolled amount.
                    FontSize = Mathf.Clamp(FontSize + delta, 5, 72);
                    textMesh.fontSize = FontSize;
                    RecalculateExcessLines();
                }
            }

            const string startUnderline = "</noparse><u><color=\"orange\"><noparse>";
            const string endUnderline = "</noparse></color></u><noparse>";

            return;

            TMP_WordInfo? DetectHoveredWord()
            {
                int index = TMP_TextUtilities.FindIntersectingWord(textMesh, Input.mousePosition, null);
                return index >= 0 && index < textMesh.textInfo.wordCount ? textMesh.textInfo.wordInfo[index] : null;
            }

            void UnderlineHoveredWord(TMP_WordInfo word)
            {
                int start = textMesh.textInfo.characterInfo[word.firstCharacterIndex].index;
                int end = textMesh.textInfo.characterInfo[word.lastCharacterIndex].index + 1;
                // We need to change the rich text tags to underline the word.
                textMesh.text = text = text[..start] + startUnderline + text[start..end] + endUnderline + text[end..];
                textMesh.ForceMeshUpdate();
            }

            void RemoveUnderline(TMP_WordInfo word)
            {
                // Start and end characters do not include the underline tags (if they exist),
                // so we need to adjust them.
                if (word.lastCharacterIndex >= textMesh.textInfo.characterCount)
                {
                    return;
                }
                int start = textMesh.textInfo.characterInfo[word.firstCharacterIndex].index - startUnderline.Length;
                int end = textMesh.textInfo.characterInfo[word.lastCharacterIndex].index + 1 + endUnderline.Length;

                if (start >= 0 && end <= text.Length)
                {
                    string underlinedPart = text[start..end];
                    if (underlinedPart.StartsWith(startUnderline) && underlinedPart.EndsWith(endUnderline))
                    {
                        text = text[..start] + underlinedPart[startUnderline.Length..^endUnderline.Length] + text[end..];
                        textMesh.text = text;
                        textMesh.ForceMeshUpdate();
                    }
                }
            }

            void GoToDefinition(TMP_WordInfo word)
            {
                (int line, int column) = GetSourcePosition(word.firstCharacterIndex);
                contextMenu.ShowDefinition(line - 1, column - 1, word.GetWord());
            }
        }

        /// <summary>
        /// Triggers the hover event for issues.
        /// </summary>
        private async UniTaskVoid TriggerIssueHoverAsync(int link)
        {
            char linkId = textMesh.textInfo.linkInfo[link].GetLinkID()[0];
            // Display tooltip containing all issue descriptions
            IEnumerable<string> issueTexts = await UniTask.WhenAll(issueDictionary[linkId].Select(x => x.ToCodeWindowStringAsync()));
            Tooltip.ActivateWith(string.Join('\n', issueTexts), Tooltip.AfterShownBehavior.HideUntilActivated);
        }

        /// <summary>
        /// Triggers the hover event for the Language Server Protocol.
        /// Should only be called when the <paramref name="hoveredWord"/> changes.
        /// </summary>
        /// <param name="hoveredWord">The word that is currently hovered over.</param>
        private async UniTaskVoid TriggerLspHoverAsync(TMP_WordInfo? hoveredWord)
        {
            Assert.IsNotNull(lspHandler);

            if (hoveredWord == null)
            {
                Tooltip.Deactivate();
            }
            else
            {
                (int line, int column) = GetSourcePosition(hoveredWord.Value.firstCharacterIndex);
                if (column > 0)
                {
                    Hover hoverInfo = await lspHandler.HoverAsync(FilePath, line - 1, column - 1);
                    if (hoverInfo?.Contents != null && lastHoveredWord != null)
                    {
                        Tooltip.ActivateWith(hoverInfo.Contents.ToRichText());
                    }
                }
            }
        }

        protected override void UpdateVR()
        {
            UpdateDesktop();
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
                // FIXME (#250): Use multiple TMPs: Either one as an overlay, or split the main TMP up into multiple ones.
                ShowNotification.Error("File too big", "This file is too large to be displayed correctly.");
            }

            if (lines > 0 && Window.transform.Find("Content/Scrollable").gameObject.TryGetComponentOrLog(out RectTransform rect))
            {
                excessLines = Mathf.CeilToInt(rect.rect.height / textMesh.textInfo.lineInfo[0].lineHeight) - 2;
            }
        }

        /// <summary>
        /// Removes listeners.
        /// </summary>
        protected override void OnDestroy()
        {
            DebugBreakpointManager.OnBreakpointAdded -= OnBreakpointAdded;
            DebugBreakpointManager.OnBreakpointRemoved -= OnBreakpointRemoved;
            if (lspHandler != null)
            {
                lspHandler.CloseDocument(FilePath);
            }
            base.OnDestroy();
        }

        /// <summary>
        /// Returns the line and column of the source code position of the given <paramref name="characterIndex"/>.
        /// This will be the line and column as they would be displayed in a text editor (i.e., 1-based, and
        /// with neither rich tags nor line numbers being counted).
        /// </summary>
        /// <param name="characterIndex">The character index within the code window's TextMeshPro
        /// to get the source position for.</param>
        /// <returns>The line and column of the source code position of the given <paramref name="characterIndex"/>.</returns>
        private (int line, int column) GetSourcePosition(int characterIndex)
        {
            int line = textMesh.textInfo.characterInfo[characterIndex].lineNumber;
            int column = characterIndex - CodeWindowOffsets[line] - neededPadding;
            return (line + 1, column);
        }

        /// <summary>
        /// Sets the color for added breakpoints.
        /// </summary>
        /// <param name="path">The file path.</param>
        /// <param name="line">The source code line.</param>
        private void OnBreakpointAdded(string path, int line)
        {
            if (path == FilePath.Replace("/", "\\"))
            {
                GameObject breakpoint = scrollable.transform.Find("Code/Breakpoints").GetChild(line - 1).gameObject;

                TextMeshProUGUI buttonMesh = breakpoint.MustGetComponent<TextMeshProUGUI>();
                buttonMesh.color = breakpointColorActive;
            }
        }

        /// <summary>
        /// Sets the color for removed breakpoints.
        /// </summary>
        /// <param name="path">The file path.</param>
        /// <param name="line">The source code line.</param>
        private void OnBreakpointRemoved(string path, int line)
        {
            if (path == FilePath)
            {
                GameObject breakpoint = scrollable.transform.Find("Code/Breakpoints").GetChild(line - 1).gameObject;

                TextMeshProUGUI buttonMesh = breakpoint.MustGetComponent<TextMeshProUGUI>();
                buttonMesh.color = breakpointColorInactive;
            }
        }
    }
}
