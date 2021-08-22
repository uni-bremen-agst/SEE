using System;
using System.Collections.Generic;
using System.Linq;
using Crosstales.RTVoice;
using Crosstales.RTVoice.Model;
using Crosstales.RTVoice.Model.Enum;
using DynamicPanels;
using Michsky.UI.ModernUIPack;
using SEE.Game.UI.Menu;
using SEE.GO;
using SEE.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.Video;

namespace SEE.Game.UI.HelpSystem
{
    public class HelpSystemEntry : PlatformDependentComponent
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
        /// Brief description of what this menu controls.
        /// Will be displayed to the user above the choices.
        /// The text may <i>not be longer than 3 lines!</i>
        /// </summary>
        private const string description = "No description added.";

        /// <summary>
        /// The name of this menu. Displayed to the user.
        /// </summary>
        private const string titleManager = "Unnamed Menu";

        /// <summary>
        /// Icon for this menu. Displayed along the title.
        /// Default is a generic settings (gear) icon.
        /// </summary>
        private const string icon = "Materials/Notification/info";

        /// <summary>
        /// The video-player which is responsible for interaction with the video such as play, pause, skip etc.
        /// </summary>
        private VideoPlayer videoPlayer;

        /// <summary>
        /// The pause or rather the pause/play- button which pauses or plays the video
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
        /// 
        /// </summary>
        GameObject keywordDisplay;

        /// <summary>
        /// True, if the current keyword is already displayed at the entry, else false.
        /// </summary>
        private bool isAdded;

        /// <summary>
        /// The currently displayed keyword of all keywords.
        /// </summary>
        LinkedListEntry currentKeyword;

        /// <summary>
        /// The audio source used to say the text. Will be retrieved from the character.
        /// </summary>
        private AudioSource audioSource;

        /// <summary>
        /// The voice used to speak. Will be retrieved from the available voices on
        /// the current system.
        /// </summary>
        private Voice voice;

        private TextMeshProUGUI text;

        protected override void StartDesktop()
        {
            if (!GameObject.Find("PersonalAssistant").TryGetComponent(out audioSource))
            {
                Debug.LogError("No AudioSource found.\n");
                enabled = false;
            }
            keywordDisplay = GameObject.Find("Code");
            voice = Speaker.Instance.VoiceForGender(Gender.FEMALE, culture: "en-US");
        }

        protected override void UpdateDesktop()
        {
            base.UpdateDesktop();
            if (EntryShown)
            {
                if (videoPlayer.time == 0)
                {
                    keywordDisplay = keywordDisplay != null ? keywordDisplay : GameObject.Find("Code");
                    TextMeshProUGUI tmp = keywordDisplay.GetComponent<TextMeshProUGUI>();
                    tmp.text = string.Empty;
                    isAdded = false;
                    currentKeyword = null;
                }
                if (HelpSystemBuilder.currentEntries != null)
                {
                    LinkedList<LinkedListEntry> currentEntries = HelpSystemBuilder.currentEntries;
                    currentKeyword ??= currentEntries.First();
                    SetTmpProgress();

                    if (currentKeyword.Index <= currentEntries.Count)
                    {
                        if (Mathf.Round((float)videoPlayer.time) == currentKeyword.CumulatedTime && !isAdded)
                        {
                            if (currentKeyword?.Index - 1 == HelpSystemBuilder.currentEntries.Count)
                            {
                                TextMeshProUGUI tmpTemp = keywordDisplay.GetComponent<TextMeshProUGUI>();
                                tmpTemp.text = string.Empty;
                            }
                            isAdded = true;
                            TextMeshProUGUI tmp = keywordDisplay.GetComponent<TextMeshProUGUI>();
                            tmp.text += currentKeyword.Text + "\n";
                            Speaker.Instance.Speak(currentKeyword.Text, audioSource, voice: voice);
                            currentKeyword = currentEntries.Find(currentKeyword)?.Next?.Value;
                        }
                        if (videoPlayer.time > currentKeyword?.CumulatedTime && isAdded)
                        {
                            isAdded = false;
                        }
                    }
                }
                text.fontSize = 30 * ((helpSystemSpace.transform.Find("DynamicPanel").GetComponent<RectTransform>().rect.width) / 550);
            }
        }

        /// <summary>
        /// Sets the TextMeshPro-Text which represents the progress of the video. It is
        /// splitted in parts of texts, e.g. 1/5 or 2/5 instead of time.
        /// </summary>
        private void SetTmpProgress()
        {
            int currentProgress;
            if (currentKeyword.Index - 1 != 0)
            {
                currentProgress = currentKeyword.Index - 1;
            }
            else
            {
                currentProgress = HelpSystemBuilder.currentEntries.Count;
            }
            progress.text = currentProgress + "/" + HelpSystemBuilder.currentEntries.Count;
        }

        /// <summary>
        /// Shows the HelpSystemEntry with the inserted values. Per default - it will be started directly by showing the entry.
        /// </summary>
        public void ShowEntry()
        {
            helpSystemSpace = PrefabInstantiator.InstantiatePrefab(HELP_SYSTEM_ENTRY_SPACE_PREFAB, Canvas.transform, false);
            helpSystemEntry = PrefabInstantiator.InstantiatePrefab(HELP_SYSTEM_ENTRY_PREFAB, helpSystemSpace.transform, false);
            HelpSystemBuilder.EntrySpace = helpSystemSpace;
            helpSystemSpace.transform.localScale = new Vector3(1.7f, 1.7f);
            helpSystemEntry.transform.Find("Content/Lower Video/Scrollable/Code")
              .gameObject.TryGetComponentOrLog(out text);
            text.fontSize = 30;

            helpSystemEntry.transform.Find("Buttons/Content/Back").gameObject.TryGetComponentOrLog(out ButtonManagerWithIcon manager);
            manager.clickEvent.AddListener(Back);
            helpSystemEntry.transform.Find("Buttons/Content/Close").gameObject.TryGetComponentOrLog(out ButtonManagerWithIcon manager2);
            manager2.clickEvent.AddListener(Close);
            GameObject.FindGameObjectWithTag("VideoPlayer").TryGetComponentOrLog(out videoPlayer);

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

            helpSystemEntry.transform.Find("Content/Lower Video/progress").gameObject.TryGetComponentOrLog(out progress);

            Panel panel = PanelUtils.CreatePanelFor((RectTransform)helpSystemEntry.transform, PanelsCanvas);
            PanelTab tab = panel.GetTab((RectTransform)helpSystemEntry.transform);
            tab.Label = "";
            GameObject headline = (GameObject)Instantiate(Resources.Load("Prefabs/UI/HeadlineHelpSystem"), 
                                                          helpSystemSpace.transform.Find("DynamicPanel/PanelHeader").gameObject.transform, false);
            HelpSystemBuilder.Headline = headline;
            headline.GetComponent<TextMeshProUGUI>().text = titleManager;

            PanelNotificationCenter.OnTabClosed += panelTab =>
            {
                if (panelTab.Panel == panel)
                {
                    Close();
                }
            };
        }

        /// <summary>
        /// Closes the HelpSystemEntry, stops the displayed video and resets the HelpSystemMenu too to the start.
        /// </summary>
        public void Close()
        {
            GameObject go = GameObject.Find(HelpSystemBuilder.HelpSystemGO);
            if (videoPlayer == null)
            {
                throw new Exception("No Video-Player found");
            }

            go.TryGetComponentOrLog(out NestedMenu menu);
            menu.ResetToBase();
            videoPlayer.Stop();
            IsPlaying = false;
            HelpSystemMenu.IsEntryOpened = false;
            Destroy(helpSystemSpace);
            EntryShown = false;

        }

        /// <summary>
        /// Replays the HelpSystemEntry after finishing. It starts from the beginning again.
        /// </summary>
        public void Replay() { }

        /// <summary>
        /// Skips the video forwards.
        /// </summary>
        /// <param name="deltaTime">The time which has to be skipped</param>
        public void Forward()
        {
            if (videoPlayer == null)
            {
                throw new Exception("No Video-Player found");
            }
            videoPlayer.time = currentKeyword.CumulatedTime;
        }

        /// <summary>
        /// Skips the video backwards.
        /// </summary>
        /// <param name="deltaTime">The time which has to be skipped.</param>
        public void Backward()
        {
            TextMeshProUGUI tmp = keywordDisplay.GetComponent<TextMeshProUGUI>();
            if (videoPlayer == null)
            {
                throw new Exception("No Video-Player found");
            }
            // play the current keyword again
            if (HelpSystemBuilder.currentEntries.Find(currentKeyword).Previous == null)
            {
                currentKeyword = HelpSystemBuilder.currentEntries.Last.Value;
            }
            else
            {
                currentKeyword = HelpSystemBuilder.currentEntries.Find(currentKeyword).Previous.Value;
            }
            string textToBeRemoved2 = currentKeyword.Text;
            tmp.text = tmp.text.Substring(0, tmp.text.Length - (textToBeRemoved2.Length + 1));
            videoPlayer.time = currentKeyword.CumulatedTime;

            // play the previous keyword again
            if (videoPlayer.time - 1f < currentKeyword.CumulatedTime)
            {
                string textToBeRemoved;
                if (HelpSystemBuilder.currentEntries.Find(currentKeyword).Previous == null)
                {
                    currentKeyword = HelpSystemBuilder.currentEntries.Last.Value;
                    textToBeRemoved = currentKeyword.Text;
                }
                else
                {
                    currentKeyword = HelpSystemBuilder.currentEntries.Find(currentKeyword).Previous.Value;
                    textToBeRemoved = currentKeyword.Text;
                }
                try
                {
                    videoPlayer.time = currentKeyword.CumulatedTime;
                    tmp.text = tmp.text.Substring(0, tmp.text.Length - (textToBeRemoved.Length + 1));
                }
                catch (ArgumentOutOfRangeException e)
                {
                    tmp.text = string.Empty;
                    foreach (LinkedListEntry s in HelpSystemBuilder.currentEntries)
                    {
                        tmp.text += s.Text + "\n";
                    }
                    tmp.text = tmp.text.Substring(0, tmp.text.Length - (HelpSystemBuilder.currentEntries.Last.Value.Text.Length + 1));
                }
            }
        }

        /// <summary>
        /// Closes the HelpSystemEntry and reopens the HelpSystemMenu for the option of selecting another entry.
        /// </summary>
        public void Back()
        {
            GameObject go = GameObject.Find(HelpSystemBuilder.HelpSystemGO);
            go.TryGetComponentOrLog(out NestedMenu menu);
            //TODO: Manager.CloseWindow();
            menu.ToggleMenu();
            if (videoPlayer == null)
            {
                throw new Exception("No Video-Player found");
            }
            videoPlayer.Stop();
            HelpSystemMenu.IsEntryOpened = false;
            EntryShown = false;
            Destroy(helpSystemSpace);
        }

        /// <summary>
        /// Toggles the "IsPlaying" - state. If the entry is running, it will be paused, if it is paused,
        /// it will be played on. 
        /// </summary>
        public void TogglePlaying()
        {
            helpSystemEntry.transform.Find("Main Content/Movable Window/Content/RawImageVideo/Buttons/Pause")
                        .gameObject.TryGetComponentOrLog(out pauseButton);
            helpSystemEntry.transform.Find("Main Content/Movable Window/Content/RawImageVideo/Buttons/Pause/Icon")
            .gameObject.TryGetComponentOrLog(out RectTransform rectTransform);
            if (!IsPlaying)
            {
                pauseButton.buttonIcon = Resources.Load<Sprite>("Materials/40+ Simple Icons - Free/Pause_Simple_Icons_UI");
                pauseButton.UpdateUI();
                videoPlayer.Play();
                Speaker.Instance.PauseOrUnPause();
                IsPlaying = true;
            }
            else
            {
                pauseButton.buttonIcon = Resources.Load<Sprite>("Materials/40+ Simple Icons - Free/RewindOneFrameForward_Simple_Icons_UI");
                pauseButton.UpdateUI();
                videoPlayer.Pause();
                Speaker.Instance.Pause();
                IsPlaying = false;
            }
        }
    }
}