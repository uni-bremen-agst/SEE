using Michsky.UI.ModernUIPack;
using SEE.Game.UI;
using SEE.Game.UI.Menu;
using SEE.GO;
using SEE.Utils;
using System.Collections.Generic;
using UnityEngine;

public partial class HelpSystemEntry : PlatformDependentComponent
{
    /// <summary>
    /// The title of the specific entry.
    /// </summary>
    private string title;

    /// <summary>
    /// The path of the video which displays the specific use-case.
    /// </summary>
    private string videoPath;

    /// <summary>
    /// A collection of notes for the user for help executing the specific use case e.g. "Press Key H" or "Left mouseclick on the node" etc.
    /// </summary>
    private List<string> keyPoints;

    /// <summary>
    /// The text spoken by the accustic help-assistant. It should containing all key-points and a short description of the things executed in the video. 
    /// </summary>
    private string audioText;

    /// <summary>
    /// Whether the menu shall be shown.
    /// </summary>
    public bool EntryShown { get; set; }

    /// <summary>
    /// True if the entry is running, else false.
    /// </summary>
    public bool IsPlaying { get; set; }
    public string TitleManager { get => titleManager; set => titleManager = value; }
    public string Description { get => description; set => description = value; }

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

    protected override void StartDesktop()
    {
        Debug.Log("HAALLLOO");
        GameObject helpSystemEntry = PrefabInstantiator.InstantiatePrefab(HELP_SYSTEM_ENTRY_PREFAB, Canvas.transform, false);
        RectTransform rectTransform = (RectTransform)helpSystemEntry.transform;
        ModalWindowManager[] managers = Canvas.GetComponentsInChildren<ModalWindowManager>();
        foreach(ModalWindowManager m in managers)
        {
            Debug.Log(m);
            Manager = m;
        }
        Manager.titleText = titleManager;
        Manager.descriptionText = description;
        Manager.icon = Resources.Load<Sprite>(icon);
        Manager.onConfirm.AddListener(Pause);
        Manager.onCancel.AddListener(Close);

    }

    protected override void UpdateDesktop()
    {
        base.UpdateDesktop();
        Debug.Log("HI");
        if (Input.GetKeyDown(KeyCode.V))
        {
            Debug.Log("H�?");
            Manager.CloseWindow();
        }
        if (Input.GetKeyDown(KeyCode.W))
        {
            Debug.Log("AH");
            Manager.OpenWindow();
        }
    }

    /// <summary>
    /// Shows the HelpSystemEntry with the inserted values. Per default - it will be started directly by showing the entry.
    /// </summary>
    public void ShowEntry()
    { 

    }

    /// <summary>
    /// Starts the HelpSystemEntry. That means, that the video will be played, the keyPoints will be displayed and the text will be read by SEE.
    /// Precondition: The entry has to be shown. If EntrySHwon == false, it doesnt start.
    /// </summary>
    public void StartEntry() { }

    /// <summary>
    /// Stops the HelpSystemEntry. That means, that the video will be resetted and SEE�s reading will be stopped.
    /// This does not means, that the Entry will be closed. The user can start it again, but from the beginning, again.
    /// </summary>
    public void Stop() { }

    public void Close() {
        Manager.CloseWindow();
        GameObject go = GameObject.Find(HelpSystemBuilder.HelpSystemGO);
        go.TryGetComponentOrLog(out NestedMenu menu);
        menu.ResetToBase();
    }

    /// <summary>
    /// Replays the HelpSystemEntry after finishing. It starts from the beginning again.
    /// </summary>
    public void Replay() { }

    /// <summary>
    /// Pauses the running HelpSystemEntry. That means after playing on the entry will be played from the same 
    /// state of progress as before pausing.
    /// </summary>
    public void Pause() {

        GameObject go = GameObject.Find(HelpSystemBuilder.HelpSystemGO);
        go.TryGetComponentOrLog(out NestedMenu menu);
        Manager.CloseWindow();
        menu.ToggleMenu();
    }

    /// <summary>
    /// Toggles the "IsPlaying" - state. If the entry is running, it will be paused, if it is paused,
    /// it will be played on. 
    /// </summary>
    public void TogglePlaying() { }


}
