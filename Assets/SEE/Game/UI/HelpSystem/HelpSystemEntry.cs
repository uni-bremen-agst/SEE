using Crosstales.RTVoice;
using Crosstales.RTVoice.Model;
using DynamicPanels;
using Michsky.UI.ModernUIPack;
using SEE.Game.UI.Menu;
using SEE.GO;
using SEE.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
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
        public bool IsPlaying { get; set; } = false;

        /// <summary>
        /// Path to the HelpSystemEntry prefab.
        /// </summary>
        private const string HELP_SYSTEM_ENTRY_PREFAB = "Prefabs/UI/HelpSystemEntry";

        /// <summary>
        /// Path to the HelpSystemEntrySpace prefab.
        /// </summary>
        private const string HELP_SYSTEM_ENTRY_SPACE_PREFAB = "Prefabs/UI/HelpSystemEntrySpace";

        /// <summary>
        /// The modal window manager which contains the actual menu.
        /// </summary>
        public ModalWindowManager Manager;

        /// <summary>
        /// Brief description of what this menu controls.
        /// Will be displayed to the user above the choices.
        /// The text may <i>not be longer than 3 lines!</i>
        /// </summary>
        private string description = "TEST No description added.";


        /// <summary>
        /// The name of this menu. Displayed to the user.
        /// </summary>
        private string titleManager = "TEST Unnamed Menu";

        /// <summary>
        /// Icon for this menu. Displayed along the title.
        /// Default is a generic settings (gear) icon.
        /// </summary>
        private string icon = "Materials/Notification/info";

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

        GameObject keywordDisplay;

        public static int timeSum;

        private bool isAdded;

        LinkedListEntry currentEntry;

        /// <summary>
        /// The audio source used to say the text. Will be retrieved from the character.
        /// </summary>
        private AudioSource audioSource;

        /// <summary>
        /// The voice used to speak. Will be retrieved from the available voices on
        /// the current system.
        /// </summary>
        private Voice voice;

        protected override void StartDesktop()
        {
            if (!GameObject.Find("PersonalAssistant").TryGetComponent(out audioSource))
            {
                Debug.LogError("No AudioSource found.\n");
                enabled = false;
            }
            keywordDisplay = GameObject.Find("Code");
            voice = Speaker.Instance.VoiceForGender(Crosstales.RTVoice.Model.Enum.Gender.FEMALE, culture: "en-US");
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
                    currentEntry = null;
                }
                if (HelpSystemBuilder.currentEntries != null)
                {
                    LinkedList<LinkedListEntry> currentEntries = HelpSystemBuilder.currentEntries;
                    currentEntry ??= currentEntries.First();
                    SetTmpProgress();

                    if (currentEntry.Index <= currentEntries.Count)
                    {
                        if (Mathf.Round((float)videoPlayer.time) == currentEntry.CumulatedTime && !isAdded)
                        {
                            if (currentEntry?.Index - 1 == HelpSystemBuilder.currentEntries.Count)
                            {
                                TextMeshProUGUI tmpTemp = keywordDisplay.GetComponent<TextMeshProUGUI>();
                                tmpTemp.text = string.Empty;
                            }
                            isAdded = true;
                            TextMeshProUGUI tmp = keywordDisplay.GetComponent<TextMeshProUGUI>();
                            tmp.text += currentEntry.Text + "\n";
                            Speaker.Instance.Speak(currentEntry.Text, audioSource, voice: voice);
                            currentEntry = currentEntries.Find(currentEntry).Next?.Value;
                        }
                        if (videoPlayer.time > currentEntry?.CumulatedTime && isAdded)
                        {
                            isAdded = false;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Sets the TextMeshPro-Text which represents the progress of the video. It is
        /// splitted in parts of texts, e.g. 1/5 or 2/5 instead of time.
        /// </summary>
        private void SetTmpProgress()
        {
            int currentProgress;
            if (currentEntry.Index - 1 != 0)
            {
                currentProgress = currentEntry.Index - 1;
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
            RectTransform rectTransform = (RectTransform)helpSystemEntry.transform;
            ModalWindowManager[] managers = Canvas.GetComponentsInChildren<ModalWindowManager>();
            foreach (ModalWindowManager m in managers)
            {
                Manager = m;
            }

            Manager.titleText = titleManager;
            Manager.descriptionText = description;
            Manager.icon = Resources.Load<Sprite>(icon);
            Manager.onConfirm.AddListener(Back);
            Manager.onCancel.AddListener(Close);
            GameObject.FindGameObjectWithTag("VideoPlayer").TryGetComponentOrLog(out videoPlayer);

            if (!helpSystemSpace.TryGetComponentOrLog(out DynamicPanelsCanvas PanelsCanvas))
            {
                Destroy(this);
            }

            helpSystemEntry.transform.Find("Main Content/Movable Window/Content/RawImageVideo/Buttons/Pause")
                           .gameObject.TryGetComponentOrLog(out pauseButton);
            pauseButton.clickEvent.AddListener(() =>
            {
                TogglePlaying();
            });
            helpSystemEntry.transform.Find("Main Content/Movable Window/Content/RawImageVideo/Buttons/Forward")
                          .gameObject.TryGetComponentOrLog(out forwardButton);
            forwardButton.clickEvent.AddListener(() =>
            {
                Forward();
            });
            helpSystemEntry.transform.Find("Main Content/Movable Window/Content/RawImageVideo/Buttons/Back")
                        .gameObject.TryGetComponentOrLog(out backwardButton);
            backwardButton.clickEvent.AddListener(() =>
            {
                Backward();
            });

            helpSystemEntry.transform.Find("Main Content/Movable Window/Dragger/progress")
                   .gameObject.TryGetComponentOrLog(out progress);

            Panel panel = PanelUtils.CreatePanelFor((RectTransform)helpSystemEntry.transform, PanelsCanvas);
        }

        /// <summary>
        /// Closes the HelpSystemEntry, stops the displayed video and resets the HelpSystemMenu too to the start.
        /// </summary>
        public void Close()
        {
            Manager.CloseWindow();
            GameObject go = GameObject.Find(HelpSystemBuilder.HelpSystemGO);
            if (videoPlayer == null)
            {
                throw new System.Exception("No Video-Player found");
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
            // TODO: videoPlayer.time = next.kumulierteZeit
            // TODO: tmp add bzw. remove text
            // TODO: voice von currentEntry abspielen
            // TODO: Abstrahieren der Logik von Forward und Backward -> skipChapter oder so
            ;
            if (videoPlayer == null)
            {
                throw new System.Exception("No Video-Player found");
            }
            videoPlayer.time = currentEntry.CumulatedTime;
            TextMeshProUGUI tmp = keywordDisplay.GetComponent<TextMeshProUGUI>();
        }

        /// <summary>
        /// Skips the video backwards.
        /// </summary>
        /// <param name="deltaTime">The time which has to be skipped.</param>
        public void Backward()
        {
            // TODO: previous - if previous is nullref - index out of bounds
            TextMeshProUGUI tmp = keywordDisplay.GetComponent<TextMeshProUGUI>();
            if (videoPlayer == null)
            {
                throw new System.Exception("No Video-Player found");
            }
            // normal - selben eintrag nochmal
            if (HelpSystemBuilder.currentEntries.Find(currentEntry).Previous == null)
            {
                currentEntry = HelpSystemBuilder.currentEntries.Last.Value;
            }
            else
            {
                currentEntry = HelpSystemBuilder.currentEntries.Find(currentEntry).Previous.Value;
            }
            string textToBeRemoved2 = currentEntry.Text;
            tmp.text = tmp.text.Substring(0, tmp.text.Length - (textToBeRemoved2.Length + 1));
            videoPlayer.time = currentEntry.CumulatedTime;

            // ein eintrag zurück
            if (videoPlayer.time - 1f < currentEntry.CumulatedTime)
            {
                string textToBeRemoved;
                Debug.Log("vorheriges");
                if (HelpSystemBuilder.currentEntries.Find(currentEntry).Previous == null)
                {
                    currentEntry = HelpSystemBuilder.currentEntries.Last.Value;
                    textToBeRemoved = currentEntry.Text;
                }
                else
                {
                    currentEntry = HelpSystemBuilder.currentEntries.Find(currentEntry).Previous.Value;
                    textToBeRemoved = currentEntry.Text;
                }
                try
                {
                    videoPlayer.time = currentEntry.CumulatedTime;
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
        /// Pauses the running HelpSystemEntry. That means after playing on the entry will be played from the same 
        /// state of progress as before pausing.
        /// </summary>
        public void Back()
        {
            GameObject go = GameObject.Find(HelpSystemBuilder.HelpSystemGO);
            go.TryGetComponentOrLog(out NestedMenu menu);
            Manager.CloseWindow();
            menu.ToggleMenu();
            if (videoPlayer == null)
            {
                throw new System.Exception("No Video-Player found");
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