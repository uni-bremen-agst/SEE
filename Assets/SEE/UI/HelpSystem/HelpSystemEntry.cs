// Copyright 2022 Thore Frenzel.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and
// associated documentation files (the "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the
// following conditions:
//
// The above copyright notice and this permission notice shall be included in all copies or substantial
// portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT
// LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO
// EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER
// IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR
// THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Linq;
using Crosstales.RTVoice;
using DynamicPanels;
using Michsky.UI.ModernUIPack;
using SEE.Game.Avatars;
using SEE.GO;
using SEE.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.Video;

namespace SEE.UI.HelpSystem
{
    /// <summary>
    /// An entry in the help system. It consists of visual, acoustic, and textual
    /// help information.
    /// </summary>
    internal class HelpSystemEntry : PlatformDependentComponent
    {
        /// <summary>
        /// Whether the menu shall be shown.
        /// </summary>
        public bool EntryShown { get; set; }

        /// <summary>
        /// True if the entry is running, else false.
        /// </summary>
        public bool IsPlaying { get; set; }

        /// <summary>
        /// Path to the HelpSystemEntry prefab. It contains the video player,
        /// the text area for the help text, and the buttons for controlling the video
        /// as well as the button to close the dialog and the button to go back to the
        /// parent in the help menu.
        /// </summary>
        private const string helpSystemEntryPrefab = "Prefabs/UI/HelpSystemEntry";

        /// <summary>
        /// Path to the HelpSystemEntrySpace prefab. It is a panel in which
        /// the HelpSystemEntry is placed.
        /// </summary>
        private const string helpSystemEntrySpacePrefab = "Prefabs/UI/HelpSystemEntrySpace";

        /// <summary>
        /// The name of this help-system entry. Displayed to the user.
        /// </summary>
        private const string title = "No Title";

        /// <summary>
        /// The path name of the game object holding the TextMeshPro component in which the
        /// instructions are printed textually. This game object is a descendant of the
        /// prefab <see cref="helpSystemEntryPrefab"/>.
        /// </summary>
        private const string textFieldPath = "Content/Lower Video/Scrollable/TextField";

        /// <summary>
        /// The path name of the game object holding the <see cref="VideoPlayer"/> component that
        /// is used to play the help videos. This game object is a descendant of the
        /// prefab <see cref="helpSystemEntryPrefab"/>.
        /// </summary>
        private const string videoPlayerPath = "Video Player";

        /// <summary>
        /// The video-player which is responsible for interaction with the video such as play, pause, skip, etc.
        /// </summary>
        private VideoPlayer videoPlayer;

        /// <summary>
        /// The pause or rather the pause/play button which pauses or plays the video, respectively.
        /// </summary>
        private ButtonManagerBasicIcon pauseButton;

        /// <summary>
        /// The forward-button, which skips a specific time of the video forwards.
        /// </summary>
        private ButtonManagerBasicIcon forwardButton;

        /// <summary>
        /// The forward-button, which skips a specific time of the video backwards.
        /// </summary>
        private ButtonManagerBasicIcon backwardButton;

        /// <summary>
        /// The helpSystemEntry GameObject.
        /// It will be instantiated from the prefab <see cref="helpSystemEntryPrefab"/>.
        /// It will be a child of <see cref="helpSystemSpace"/>.
        /// </summary>
        private GameObject helpSystemEntry;

        /// <summary>
        /// The helpSystemEntrySpace-GameObject which contains the helpSystemEntry.
        /// It is nessecary because of the dynamic panel for scaling the entry.
        /// It will be instantiated from the prefab <see cref="helpSystemEntrySpacePrefab"/>.
        /// It will be a child of <see cref="PlatformDependentComponent.Canvas"/>.
        /// </summary>
        private GameObject helpSystemSpace;

        /// <summary>
        /// A instance of tmp which displays the current progress of the video such as "1s / 35 s".
        /// </summary>
        private TextMeshProUGUI progress;

        /// <summary>
        /// The game object holding a TextMeshPro component in which the instructions are printed.
        /// This game object is a descendant of <see cref="helpSystemEntry"/> with the named
        /// path <see cref="textFieldPath"/>.
        /// </summary>
        private GameObject instructionsDisplay;

        /// <summary>
        /// True, if the current help entry is already displayed, else false.
        /// </summary>
        private bool isAdded;

        /// <summary>
        /// The currently displayed help entry.
        /// </summary>
        private HelpEntry currentHelpEntry;

        /// <summary>
        /// The text inside of the HelpSystemEntry. This is the place where the instructions are
        /// printed. It is a component attached to <see cref="instructionsDisplay"/>.
        /// </summary>
        private TextMeshProUGUI text;

        /// <summary>
        /// Sets <see cref="instructionsDisplay"/>.
        /// </summary>
        protected override void StartDesktop()
        {
            /// Note: <see cref="GetTextField()"/> called below accesses <see cref="helpSystemEntry"/>
            /// that is why we need to instantiate it first.
            SetUpHelpSystemEntryAndSpace();
            helpSystemSpace.SetActive(false);
            helpSystemEntry.SetActive(false);
            instructionsDisplay = GetTextField();
        }

        protected override void StartVR()
        {
            StartDesktop();
        }

        /// <summary>
        /// Instantiates the help system entry and the space if necessary, that is,
        /// if <see cref="helpSystemEntry"/> is null.
        /// </summary>
        private void SetUpHelpSystemEntryAndSpace()
        {
            if (helpSystemSpace == null)
            {
                helpSystemSpace = PrefabInstantiator.InstantiatePrefab(helpSystemEntrySpacePrefab, Canvas.transform, false);
                helpSystemEntry = PrefabInstantiator.InstantiatePrefab(helpSystemEntryPrefab, helpSystemSpace.transform, false);
            }
        }

        /// <summary>
        /// Returns the game object holding a TextMeshPro component in which the instructions
        /// are printed. It is the descendant of <see cref="helpSystemEntry"/> with the
        /// object-name path <see cref="textFieldPath"/>.
        /// </summary>
        /// <returns>the text field where the instructions are printed</returns>
        private GameObject GetTextField()
        {
            return helpSystemEntry.transform.Find(textFieldPath).gameObject;
        }

        /// <summary>
        /// Implementation of <see cref="UpdateDesktop"/>. If the help entry is shown,
        /// updates the presented help information.
        /// </summary>
        protected override void UpdateDesktop()
        {
            base.UpdateDesktop();
            if (EntryShown)
            {
                if (videoPlayer.time == 0)
                {
                    if (instructionsDisplay == null)
                    {
                        instructionsDisplay = GetTextField();
                    }
                    instructionsDisplay.GetComponent<TextMeshProUGUI>().text = string.Empty;
                    isAdded = false;
                    currentHelpEntry = null;
                }
                if (HelpSystemBuilder.CurrentEntries != null)
                {
                    LinkedList<HelpEntry> currentEntries = HelpSystemBuilder.CurrentEntries;
                    if (currentEntries.Count > 0)
                    {
                        currentHelpEntry ??= currentEntries.First();
                    }
                    UpdateProgress();

                    if (currentHelpEntry.Index <= currentEntries.Count)
                    {
                        if (Mathf.Round((float)videoPlayer.time) == currentHelpEntry.CumulatedTime && !isAdded)
                        {
                            if (currentHelpEntry?.Index - 1 == HelpSystemBuilder.CurrentEntries.Count)
                            {
                                instructionsDisplay.GetComponent<TextMeshProUGUI>().text = string.Empty;
                            }
                            isAdded = true;
                            TextMeshProUGUI tmp = instructionsDisplay.GetComponent<TextMeshProUGUI>();
                            tmp.text += currentHelpEntry.Text + "\n";
                            PersonalAssistantBrain.Instance?.Say(currentHelpEntry.Text);
                            currentHelpEntry = currentEntries.Find(currentHelpEntry)?.Next?.Value;
                        }
                        if (videoPlayer.time > currentHelpEntry?.CumulatedTime && isAdded)
                        {
                            isAdded = false;
                        }
                    }
                }
            }
        }

        protected override void UpdateVR()
        {
            UpdateDesktop();
        }

        /// <summary>
        /// Sets the TextMeshPro text which represents the progress of the video. It is
        /// split in parts of texts, e.g., 1/5 or 2/5, instead of time.
        /// </summary>
        private void UpdateProgress()
        {
            int currentProgress = currentHelpEntry.Index - 1 != 0
                ? currentHelpEntry.Index - 1
                : HelpSystemBuilder.CurrentEntries.Count;
            progress.text = currentProgress + "/" + HelpSystemBuilder.CurrentEntries.Count;
        }

        /// <summary>
        /// Returns the video player where the instruction videos can be played.
        /// </summary>
        /// <returns>video player for instruction videos</returns>
        /// <exception cref="Exception">thrown if none can be found</exception>
        public VideoPlayer GetVideoPlayer()
        {
            Transform videoPlayer = helpSystemEntry.transform.Find(videoPlayerPath);
            if (videoPlayer == null)
            {
                throw new Exception($"Help-system entry {helpSystemEntry.FullName()} has no video player {videoPlayerPath}.");
            }
            if (videoPlayer.gameObject.TryGetComponent(out VideoPlayer result))
            {
                return result;
            }
            else
            {
                throw new Exception($"Help-system entry {helpSystemEntry.FullName()} has a video player {videoPlayerPath} without {typeof(VideoPlayer)} component.");
            }
        }

        /// <summary>
        /// Shows the HelpSystemEntry with the inserted values. Per default, it will be started directly by showing the entry.
        /// </summary>
        public void ShowEntry()
        {
            /// If the dialog was closed, <see cref="helpSystemSpace"/> and <see cref="helpSystemEntry"/>
            /// are null and need to be instantiated again.
            SetUpHelpSystemEntryAndSpace();
            helpSystemSpace.SetActive(true);
            helpSystemEntry.SetActive(true);
            helpSystemSpace.transform.localScale = new Vector3(1.7f, 1.7f);
            RectTransform dynamicPanel = helpSystemSpace.transform.GetChild(2).GetComponent<RectTransform>();
            dynamicPanel.sizeDelta = new Vector2(550, 425);
            helpSystemEntry.transform.Find(textFieldPath).gameObject.TryGetComponentOrLog(out text);
            text.fontSize = 18;

            {
                helpSystemEntry.transform.Find("Buttons/Content/Back").gameObject.TryGetComponentOrLog(out ButtonManagerWithIcon manager);
                manager.clickEvent.AddListener(Back);
            }
            {
                helpSystemEntry.transform.Find("Buttons/Content/Close").gameObject.TryGetComponentOrLog(out ButtonManagerWithIcon manager);
                manager.clickEvent.AddListener(Close);
            }
            videoPlayer = GetVideoPlayer();

            if (!helpSystemSpace.TryGetComponentOrLog(out DynamicPanelsCanvas panelsCanvas))
            {
                Destroyer.Destroy(this);
            }

            helpSystemEntry.transform.Find("Content/Lower Video/Buttons/Pause")
                           .gameObject.TryGetComponentOrLog(out pauseButton);
            pauseButton.clickEvent.AddListener(TogglePlaying);

            helpSystemEntry.transform.Find("Content/Lower Video/Buttons/Forward")
                           .gameObject.TryGetComponentOrLog(out forwardButton);
            forwardButton.clickEvent.AddListener(Forward);

            helpSystemEntry.transform.Find("Content/Lower Video/Buttons/Back")
                           .gameObject.TryGetComponentOrLog(out backwardButton);
            backwardButton.clickEvent.AddListener(Backward);

            helpSystemEntry.transform.Find("Content/Lower Video/Progress").gameObject.TryGetComponentOrLog(out progress);

            Panel panel = PanelUtils.CreatePanelFor((RectTransform)helpSystemEntry.transform, panelsCanvas);
            PanelTab tab = panel.GetTab((RectTransform)helpSystemEntry.transform);
            tab.Label = "";
            GameObject headline = (GameObject)Instantiate(Resources.Load("Prefabs/UI/HeadlineHelpSystem"),
                                                          helpSystemSpace.transform.Find("DynamicPanel/PanelHeader").gameObject.transform, false);
            HelpSystemBuilder.Headline = headline;
            headline.GetComponent<TextMeshProUGUI>().text = title;

            PanelNotificationCenter.OnTabClosed += panelTab =>
            {
                if (panelTab.Panel == panel)
                {
                    Close();
                }
            };
        }

        /// <summary>
        /// Closes the HelpSystemEntry, stops the displayed video and also resets the HelpSystemMenu to the start.
        /// </summary>
        public void Close()
        {
            HelpSystemBuilder.GetHelpSystemMenu().Reset();
            videoPlayer?.Stop();
            IsPlaying = false;
            PersonalAssistantBrain.Instance?.Stop();
            Destroyer.Destroy(helpSystemSpace);
            EntryShown = false;
        }

        /// <summary>
        /// Skips the video forwards.
        /// </summary>
        public void Forward()
        {
            if (videoPlayer == null)
            {
                throw new Exception("No video player found");
            }
            videoPlayer.time = currentHelpEntry.CumulatedTime;
        }

        /// <summary>
        /// Skips the video backwards.
        /// </summary>
        public void Backward()
        {
            if (videoPlayer == null)
            {
                throw new Exception("No video player found");
            }
            // play the current keyword again
            LinkedListNode<HelpEntry> previous = HelpSystemBuilder.CurrentEntries.Find(currentHelpEntry)?.Previous;
            currentHelpEntry = previous == null ? HelpSystemBuilder.CurrentEntries.Last.Value : previous.Value;
            TextMeshProUGUI tmp = instructionsDisplay.GetComponent<TextMeshProUGUI>();
            tmp.text = tmp.text[..^(currentHelpEntry.Text.Length + 1)];
            videoPlayer.time = currentHelpEntry.CumulatedTime;

            // play the previous keyword again
            if (videoPlayer.time - 1f < currentHelpEntry.CumulatedTime)
            {
                string textToBeRemoved;
                if (previous == null)
                {
                    currentHelpEntry = HelpSystemBuilder.CurrentEntries.Last.Value;
                    textToBeRemoved = currentHelpEntry.Text;
                }
                else
                {
                    currentHelpEntry = previous.Value;
                    textToBeRemoved = currentHelpEntry.Text;
                }
                try
                {
                    videoPlayer.time = currentHelpEntry.CumulatedTime;
                    tmp.text = tmp.text[..^(textToBeRemoved.Length + 1)];
                }
                catch (ArgumentOutOfRangeException)
                {
                    tmp.text = string.Empty;
                    foreach (HelpEntry s in HelpSystemBuilder.CurrentEntries)
                    {
                        tmp.text += s.Text + "\n";
                    }
                    tmp.text = tmp.text[..^(HelpSystemBuilder.CurrentEntries.Last.Value.Text.Length + 1)];
                }
            }
        }

        /// <summary>
        /// Closes the HelpSystemEntry and re-opens the HelpSystemMenu for the option of selecting another entry.
        /// </summary>
        public void Back()
        {
            HelpSystemBuilder.GetHelpSystemMenu().ToggleMenu();
            PersonalAssistantBrain.Instance?.Stop();
            videoPlayer?.Stop();
            EntryShown = false;
            Destroyer.Destroy(helpSystemSpace);
        }

        /// <summary>
        /// The icon for playing the video.
        /// </summary>
        private Sprite playIcon;

        /// <summary>
        /// The icon for pausing the video.
        /// </summary>
        private Sprite pauseIcon;

        /// <summary>
        /// Initializes the sprites to avoid loading them multiple times.
        /// </summary>
        private void InitializeIcons()
        {
            playIcon = Resources.Load<Sprite>("Materials/40+ Simple Icons - Free/RewindOneFrameForward_Simple_Icons_UI");
            pauseIcon = Resources.Load<Sprite>("Materials/40+ Simple Icons - Free/Pause_Simple_Icons_UI");
        }

        /// <summary>
        /// Toggles the "IsPlaying" state. If the entry is running, it will be paused; if it is paused,
        /// it will be played on.
        /// </summary>
        public void TogglePlaying()
        {
            if (playIcon == null || pauseIcon == null)
            {
                InitializeIcons();
            }

            helpSystemEntry.transform.Find("Content/Lower Video/Buttons/Pause")
                           .gameObject.TryGetComponentOrLog(out pauseButton);
            if (!IsPlaying)
            {
                pauseButton.buttonIcon = pauseIcon;
                pauseButton.UpdateUI();
                videoPlayer.Play();
                Speaker.Instance.PauseOrUnPause();
                IsPlaying = true;
            }
            else
            {
                pauseButton.buttonIcon = playIcon;
                pauseButton.UpdateUI();
                videoPlayer.Pause();
                Speaker.Instance.Pause();
                IsPlaying = false;
            }
        }
    }
}
