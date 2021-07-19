using System.Collections.Generic;
using SEE.Controls;
using SEE.Game.UI.Menu;
using SEE.GO;
using UnityEngine;

public class HelpSystemMenu : MonoBehaviour
{
    /// <summary>
    /// The name of the PersonalAssistant-GameObject
    /// </summary>
    public const string PersonalAssistant = "PersonalAssistant";

    /// <summary>
    /// The name of the HelpSystem-GameObject
    /// </summary>
    public const string HelpSystem = "HelpSystem";

    /// <summary>
    /// The NestedMenu of the HelpSystem - responsible for the navigation
    /// inside of the use-cases.
    /// </summary>
    public NestedMenu mainMenu;

    /// <summary>
    /// True, if an entry is currently displayed. That means, 
    /// that there should be no interaction possible with the collider of see 
    /// for opening the nestedMenu while Entry is opened.
    /// </summary>
    public static bool IsEntryOpened { get; set; } = false;

    /// <summary>
    /// The path to the default-icon for an RefEntry in the nested menu.
    /// </summary>
    private const string RefIcon = "Materials/ModernUIPack/Plus";

    /// <summary>
    /// The path to the default-icon for an HelpSystemEntry in the nested menu.
    /// </summary>
    private const string EntryIcon = "Materials/ModernUIPack/Eye";

    // Start is called before the first frame update
    void Start()
    {
        CreateStartMenu();
    }

    public void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            RaycastHit hit;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out hit))
                if (hit.transform == GameObject.Find(PersonalAssistant).transform && mainMenu.MenuShown == false && !IsEntryOpened)
                {
                    mainMenu.ShowMenu(true);
                }
        }
    }

    /// <summary>
    /// Contains all Hierachy-Layers of the Help-System-Menu from the lowest to the highest layer.
    /// It creates all Sub-Menu's, RefEntries and HelpSystemEntrys and should be expanded by the developers.
    /// </summary>
    private void CreateStartMenu()
    {
        // Important note: You have to define the lowest hierachy-level first. 
        // That means, the mainMenu will be defined at the end and the lowest entry-list first.

        PlayerSettings.LocalPlayer.TryGetComponentOrLog(out HelpSystemEntry entry);
      
        List<MenuEntry> mainMenuEntries = new List<MenuEntry>();
        List<MenuEntry> architectureEntries = new List<MenuEntry>();
        List<MenuEntry> playerMenuEntries = new List<MenuEntry>();
        List<MenuEntry> evolutionEntries = new List<MenuEntry>();
        List<MenuEntry> debuggingEntries = new List<MenuEntry>();
        List<MenuEntry> qualityEntries = new List<MenuEntry>();

        List<string> addEdge = new List<string>
        {
            "PlayerMenu: Space",
            "Leftklick: Add Edge",
            "Leftklick: Start Node",
            "Leftklick: Target Node",
            "For Removing Start Node: F11",
        };

        List<string> addNode = new List<string>
        {
            "PlayerMenu: Space",
            "Leftklick: Add Node",
            "Leftklick: Position for new Node",
        };

        List<string> addLine = new List<string>
        {
            "PlayerMenu: Space",
            "Leftklick: Draw",
            "Leftklick with pressed mouse",
            "Release left mousebutton for ending"
        };

        List<string> placeholder = new List<string>
        {
            "TESTE",
        };

        playerMenuEntries = new List<MenuEntry>
        {        
            HelpSystemBuilder.CreateNewHelpSystemEntry("Add Edge", "Add Edge Description", Color.green, EntryIcon, "TEST1", "TEST1", addEdge, entry),
            HelpSystemBuilder.CreateNewHelpSystemEntry("Add Node", "Add Node Description", Color.green, EntryIcon, "TEST2", "TEST2", addNode, entry),
            HelpSystemBuilder.CreateNewHelpSystemEntry("Add Line", "Add Line Description", Color.green, EntryIcon, null, null, addLine, entry)
        };

        architectureEntries = new List<MenuEntry>
        {
            HelpSystemBuilder.CreateNewRefEntry(playerMenuEntries,"Player Menu", "Player Menu Description", Color.green, RefIcon),
            HelpSystemBuilder.CreateNewHelpSystemEntry("Map Architecture", "Mapping description", Color.green, EntryIcon, null, null, placeholder, entry)
        };

        mainMenuEntries = new List<MenuEntry>
        {
            HelpSystemBuilder.CreateNewRefEntry(architectureEntries, "Architecture", "Architecture Description", Color.green, RefIcon),
            HelpSystemBuilder.CreateNewRefEntry(evolutionEntries, "Evolution", "Evolution Description", Color.red, RefIcon),
            HelpSystemBuilder.CreateNewRefEntry(debuggingEntries, "Debugging", "Debugging Description", Color.blue, RefIcon),
            HelpSystemBuilder.CreateNewRefEntry(qualityEntries, "Quality", "Quality Description", Color.cyan,RefIcon)
        };

        mainMenu = HelpSystemBuilder.CreateMainMenu("MainMenu", "Main Description", "Materials/Notification/info", mainMenuEntries);
    }
}