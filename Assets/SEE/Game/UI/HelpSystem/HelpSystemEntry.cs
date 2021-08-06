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

        private GameObject helpSystemSpace;

        /// <summary>
        /// A instance of tmp which displays the current progress of the video such as "1s / 35 s".
        /// </summary>
        private TextMeshProUGUI progress;


        protected override void StartDesktop()
        {
            Debug.Log("start");
        }

        protected override void UpdateDesktop()
        {
            base.UpdateDesktop();
            if (EntryShown) {
                progress.text = Mathf.Round((float)videoPlayer.time).ToString() + " s / " + Mathf.Round((float)videoPlayer.length).ToString() + " s";
            }
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
                Forward(10);
            });
            helpSystemEntry.transform.Find("Main Content/Movable Window/Content/RawImageVideo/Buttons/Back")
                        .gameObject.TryGetComponentOrLog(out backwardButton);
            backwardButton.clickEvent.AddListener(() =>
            {
                Backward(10);
            });

            helpSystemEntry.transform.Find("Main Content/Movable Window/Dragger/progress")
                   .gameObject.TryGetComponentOrLog(out progress);

            Panel panel = PanelUtils.CreatePanelFor((RectTransform)helpSystemEntry.transform, PanelsCanvas);
        }

        /// <summary>
        /// Stops the HelpSystemEntry. That means, that the video will be resetted and SEE´s reading will be stopped.
        /// This does not means, that the Entry will be closed. The user can start it again, but from the beginning, again.
        /// </summary>
        public void Stop() { }

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
        public void Forward(int deltaTime)
        {
            GameObject go = GameObject.Find(HelpSystemBuilder.HelpSystemGO);
            if (videoPlayer == null)
            {
                throw new System.Exception("No Video-Player found");
            }
            videoPlayer.time += deltaTime;
        }

        /// <summary>
        /// Skips the video backwards.
        /// </summary>
        /// <param name="deltaTime">The time which has to be skipped.</param>
        public void Backward(int deltaTime)
        {
            GameObject go = GameObject.Find(HelpSystemBuilder.HelpSystemGO);
            if (videoPlayer == null)
            {
                throw new System.Exception("No Video-Player found");
            }
            videoPlayer.time -= deltaTime;
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
                pauseButton.buttonIcon = Resources.Load<Sprite>("Materials/ModernUIPack/Pause");
                //rectTransform.localPosition = new Vector3(-0.1f, -50.12466f, 0);
                //rectTransform.localScale = new Vector3(1.2f, 0.94f, 1);
                pauseButton.UpdateUI();
                videoPlayer.Play();
                IsPlaying = true;

            }
            else
            {
                pauseButton.buttonIcon = Resources.Load<Sprite>("Materials/ModernUIPack/Play");
                //rectTransform.localPosition = new Vector3(1f, -50.22466f, 0f);
                //rectTransform.localScale = new Vector3(2f, 1.31f, 1);
                pauseButton.UpdateUI();
                videoPlayer.Pause();
                IsPlaying = false;
            }
        }
    }
}