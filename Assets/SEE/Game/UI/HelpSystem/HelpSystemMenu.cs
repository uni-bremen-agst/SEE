using System.Collections.Generic;
using SEE.Controls;
using SEE.Game.UI.HelpSystem;
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

        // Hint: Description is responsible for the title, title is useless
        playerMenuEntries = new List<MenuEntry>
        {
            HelpSystemBuilder.CreateNewHelpSystemEntry("Add Edge", "Add Edge Description", Color.magenta, "Assets/SEE/Videos/AddEdge.mp4", addEdge, entry),
            HelpSystemBuilder.CreateNewHelpSystemEntry("Add Node", "Add Node Description", Color.magenta, "Assets/SEE/Videos/AddNode.mp4", addNode, entry),
            HelpSystemBuilder.CreateNewHelpSystemEntry("Add Line", "Add Line Description", Color.magenta, "Assets/SEE/Videos/AddEdge.mp4", addLine, entry)
        };

        architectureEntries = new List<MenuEntry>
        {
            HelpSystemBuilder.CreateNewRefEntry(playerMenuEntries,"Player Menu", "Player Menu Description", Color.magenta),
            HelpSystemBuilder.CreateNewHelpSystemEntry("Map Architecture", "Mapping description", Color.magenta, "Assets/SEE/Videos/AddEdge.mp4",placeholder, entry)
        };

        mainMenuEntries = new List<MenuEntry>
        {
            HelpSystemBuilder.CreateNewRefEntry(architectureEntries, "Architecture", "Use-Cases related to the architecture", Color.magenta),
            HelpSystemBuilder.CreateNewRefEntry(evolutionEntries, "Evolution", "Use-Cases related to software-evolution", Color.red),
            HelpSystemBuilder.CreateNewRefEntry(debuggingEntries, "Debugging", "Use-Cases related to software-debugging ", Color.blue),
            HelpSystemBuilder.CreateNewRefEntry(qualityEntries, "Quality", "Use-Cases related to the software-quality", Color.cyan)
        };

        mainMenu = HelpSystemBuilder.CreateMainMenu("Help System", "Find your specific Use-Case", "Materials/Notification/info", mainMenuEntries);
    }
}