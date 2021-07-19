using Michsky.UI.ModernUIPack;
using SEE.Game.UI;
using SEE.Game.UI.Menu;
using SEE.GO;
using SEE.Utils;
using UnityEngine;
using UnityEngine.Video;

public partial class HelpSystemEntry : PlatformDependentComponent
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

    private VideoPlayer videoPlayer;

    protected override void StartDesktop()
    {
        GameObject helpSystemEntry = PrefabInstantiator.InstantiatePrefab(HELP_SYSTEM_ENTRY_PREFAB, Canvas.transform, false);
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
        GameObject.FindGameObjectWithTag("VideoPlayer").TryGetComponentOrLog(out VideoPlayer videoPlayer);
        this.videoPlayer = videoPlayer;
    }

    protected override void UpdateDesktop()
    {
        base.UpdateDesktop();
        if (Input.GetKeyDown(KeyCode.V))
        {
            videoPlayer.Pause();
        }
        if (Input.GetKeyDown(KeyCode.W))
        {
            videoPlayer.Play();
        }
        if (Input.GetKeyDown(KeyCode.L))
        {
            videoPlayer.url = "Assets/SEE/Videos/ZwischenstandAddNode.mp4";
            videoPlayer.Play();
        }
        if (Input.GetKeyDown(KeyCode.T))
        {
            TogglePlaying();
        }
    }

    /// <summary>
    /// Shows the HelpSystemEntry with the inserted values. Per default - it will be started directly by showing the entry.
    /// </summary>
    public void ShowEntry()
    {

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
    }

    /// <summary>
    /// Replays the HelpSystemEntry after finishing. It starts from the beginning again.
    /// </summary>
    public void Replay() { }

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
    }

    /// <summary>
    /// Toggles the "IsPlaying" - state. If the entry is running, it will be paused, if it is paused,
    /// it will be played on. 
    /// </summary>
    public void TogglePlaying()
    {
        if (!IsPlaying)
        {
            videoPlayer.Play();
            IsPlaying = true;
        }
        else
        {
            videoPlayer.Pause();
            IsPlaying = false;
        }
    }
}
