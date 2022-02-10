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
#if !UNITY_ANDROID
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

namespace SEE.Game.UI.HelpSystem
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
        /// Path to the HelpSystemEntry prefab.
        /// </summary>
        private const string HELP_SYSTEM_ENTRY_PREFAB = "Prefabs/UI/HelpSystemEntry";

        /// <summary>
        /// Path to the HelpSystemEntrySpace prefab.
        /// </summary>
        private const string HELP_SYSTEM_ENTRY_SPACE_PREFAB = "Prefabs/UI/HelpSystemEntrySpace";

        /// <summary>
        /// The name of this help-system entry. Displayed to the user.
        /// </summary>
        private const string title = "No Title";

        /// <summary>
        /// The path name of the game object holding the TextMeshPro component in which the
        /// instructions are printed textually. This game object is a descendant of the
        /// prefab <see cref="HELP_SYSTEM_ENTRY_PREFAB"/>.
        /// </summary>
        private const string TextFieldPath = "Content/Lower Video/Scrollable/TextField";

        /// <summary>
        /// The path name of the game object holding the <see cref="VideoPlayer"/> component that
        /// is used to play the help videos. This game object is a descendant of the
        /// prefab <see cref="HELP_SYSTEM_ENTRY_PREFAB"/>.
        /// </summary>
        private const string VideoPlayerPath = "Video Player";

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
        /// The helpSystemEntry-GameObject.
        /// </summary>
        private GameObject helpSystemEntry;

        /// <summary>
        /// The helpSystemEntrySpace-GameObject which contains the helpSystemEntry.
        /// It is nessecary because of the dynamic panel for scaling the entry.
        /// </summary>
        private GameObject helpSystemSpace;

        /// <summary>
        /// A instance of tmp which displays the current progress of the video such as "1s / 35 s".
        /// </summary>
        private TextMeshProUGUI progress;

        /// <summary>
        /// The game object holding a TextMeshPro component in which the instructions are printed.
        /// This game object is a descendant of <see cref="helpSystemEntry"/> with the named
        /// path <see cref="TextFieldPath"/>.
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
            instructionsDisplay = GetTextField();
        }

        /// <summary>
        /// Returns the game object holding a TextMeshPro component in which the instructions
        /// are printed. It is the descendant of <see cref="helpSystemEntry"/> with the
        /// object-name path <see cref="TextFieldPath"/>.
        /// </summary>
        /// <returns>the text field where the instructions are printed</returns>
        private GameObject GetTextField()
        {
            return helpSystemEntry.transform.Find(TextFieldPath).gameObject;
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
                if (HelpSystemBuilder.currentEntries != null)
                {
                    LinkedList<HelpEntry> currentEntries = HelpSystemBuilder.currentEntries;
                    if (currentEntries.Count > 0)
                    {
                        currentHelpEntry ??= currentEntries.First();
                    }
                    UpdateProgress();

                    if (currentHelpEntry.Index <= currentEntries.Count)
                    {
                        if (Mathf.Round((float)videoPlayer.time) == currentHelpEntry.CumulatedTime && !isAdded)
                        {
                            if (currentHelpEntry?.Index - 1 == HelpSystemBuilder.currentEntries.Count)
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

        /// <summary>
        /// Sets the TextMeshPro text which represents the progress of the video. It is
        /// split in parts of texts, e.g., 1/5 or 2/5, instead of time.
        /// </summary>
        private void UpdateProgress()
        {
            int currentProgress = currentHelpEntry.Index - 1 != 0 ? currentHelpEntry.Index - 1
                                                                : HelpSystemBuilder.currentEntries.Count;
            progress.text = currentProgress + "/" + HelpSystemBuilder.currentEntries.Count;
        }

        /// <summary>
        /// Returns the video player where the instruction videos can be played.
        /// </summary>
        /// <returns>video player for instruction videos</returns>
        /// <exception cref="Exception">thrown if none can be found</exception>
        public VideoPlayer GetVideoPlayer()
        {
            Transform videoPlayer = helpSystemEntry.transform.Find(VideoPlayerPath);
            if (videoPlayer == null)
            {
                throw new Exception($"Help-system entry {helpSystemEntry.GetFullName()} has no video player {VideoPlayerPath}.");
            }
            if (videoPlayer.gameObject.TryGetComponent(out VideoPlayer result))
            {
                return result;
            }
            else
            {
                throw new Exception($"Help-system entry {helpSystemEntry.GetFullName()} has a video player {VideoPlayerPath} without {typeof(VideoPlayer)} component.");
            }
        }

        /// <summary>
        /// Shows the HelpSystemEntry with the inserted values. Per default, it will be started directly by showing the entry.
        /// </summary>
        public void ShowEntry()
        {
            helpSystemSpace = PrefabInstantiator.InstantiatePrefab(HELP_SYSTEM_ENTRY_SPACE_PREFAB, Canvas.transform, false);
            helpSystemEntry = PrefabInstantiator.InstantiatePrefab(HELP_SYSTEM_ENTRY_PREFAB, helpSystemSpace.transform, false);
            HelpSystemBuilder.EntrySpace = helpSystemSpace;
            helpSystemSpace.transform.localScale = new Vector3(1.7f, 1.7f);
            RectTransform dynamicPanel = helpSystemSpace.transform.GetChild(2).GetComponent<RectTransform>();
            dynamicPanel.sizeDelta = new Vector2(550, 425);
            helpSystemEntry.transform.Find(TextFieldPath).gameObject.TryGetComponentOrLog(out text);
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

            if (!helpSystemSpace.TryGetComponentOrLog(out DynamicPanelsCanvas PanelsCanvas))
            {
                Destroy(this);
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

            Panel panel = PanelUtils.CreatePanelFor((RectTransform)helpSystemEntry.transform, PanelsCanvas);
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
            Destroy(helpSystemSpace);
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
            if (HelpSystemBuilder.currentEntries.Find(currentHelpEntry).Previous == null)
            {
                currentHelpEntry = HelpSystemBuilder.currentEntries.Last.Value;
            }
            else
            {
                currentHelpEntry = HelpSystemBuilder.currentEntries.Find(currentHelpEntry).Previous.Value;
            }
            TextMeshProUGUI tmp = instructionsDisplay.GetComponent<TextMeshProUGUI>();
            tmp.text = tmp.text.Substring(0, tmp.text.Length - (currentHelpEntry.Text.Length + 1));
            videoPlayer.time = currentHelpEntry.CumulatedTime;

            // play the previous keyword again
            if (videoPlayer.time - 1f < currentHelpEntry.CumulatedTime)
            {
                string textToBeRemoved;
                if (HelpSystemBuilder.currentEntries.Find(currentHelpEntry).Previous == null)
                {
                    currentHelpEntry = HelpSystemBuilder.currentEntries.Last.Value;
                    textToBeRemoved = currentHelpEntry.Text;
                }
                else
                {
                    currentHelpEntry = HelpSystemBuilder.currentEntries.Find(currentHelpEntry).Previous.Value;
                    textToBeRemoved = currentHelpEntry.Text;
                }
                try
                {
                    videoPlayer.time = currentHelpEntry.CumulatedTime;
                    tmp.text = tmp.text.Substring(0, tmp.text.Length - (textToBeRemoved.Length + 1));
                }
                catch (ArgumentOutOfRangeException)
                {
                    tmp.text = string.Empty;
                    foreach (HelpEntry s in HelpSystemBuilder.currentEntries)
                    {
                        tmp.text += s.Text + "\n";
                    }
                    tmp.text = tmp.text.Substring(0, tmp.text.Length - (HelpSystemBuilder.currentEntries.Last.Value.Text.Length + 1));
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
            Destroy(helpSystemSpace);
        }

        /// <summary>
        /// Toggles the "IsPlaying" state. If the entry is running, it will be paused; if it is paused,
        /// it will be played on.
        /// </summary>
        public void TogglePlaying()
        {
            helpSystemEntry.transform.Find("Content/Lower Video/Buttons/Pause")
                        .gameObject.TryGetComponentOrLog(out pauseButton);
            if (!IsPlaying)
            {
                // FIXME: Should this resource really be loaded each time playing is toggled?
                pauseButton.buttonIcon = Resources.Load<Sprite>("Materials/40+ Simple Icons - Free/Pause_Simple_Icons_UI");
                pauseButton.UpdateUI();
                videoPlayer.Play();
                Speaker.Instance.PauseOrUnPause();
                IsPlaying = true;
            }
            else
            {
                // FIXME: Should this resource really be loaded each time playing is toggled?
                pauseButton.buttonIcon = Resources.Load<Sprite>("Materials/40+ Simple Icons - Free/RewindOneFrameForward_Simple_Icons_UI");
                pauseButton.UpdateUI();
                videoPlayer.Pause();
                Speaker.Instance.Pause();
                IsPlaying = false;
            }
        }
    }
}
#endif