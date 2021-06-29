using System.Collections.Generic;
using SEE.Game.UI.Menu;
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

    public NestedMenu mainMenu;

    public static bool newRendering = false;

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
                if (hit.transform == GameObject.Find(PersonalAssistant).transform && mainMenu.MenuShown == false)
                {
                    mainMenu.ShowMenu(true);
                }
        }
    }

    private const string RefIcon = "Materials/ModernUIPack/Plus";

    private const string EntryIcon = "Materials/ModernUIPack/Eye";

    private void CreateStartMenu()
    {
        // Important note: You have to define the lowest hierachy-level first. 
        // That means, the mainMenu will be defined at the end and the lowest entry-list first.

        List<MenuEntry> mainMenuEntries = new List<MenuEntry>();
        List<MenuEntry> architectureEntries = new List<MenuEntry>();
        List<MenuEntry> playerMenuEntries = new List<MenuEntry>();
        List<MenuEntry> evolutionEntries = new List<MenuEntry>();
        List<MenuEntry> debuggingEntries = new List<MenuEntry>();
        List<MenuEntry> qualityEntries = new List<MenuEntry>();

        playerMenuEntries = new List<MenuEntry>
        {
            HelpSystemBuilder.CreateNewHelpSystemEntry("Add Edge", "Add Edge Description", Color.green, EntryIcon),
            HelpSystemBuilder.CreateNewHelpSystemEntry("Add Node", "Add Node Description", Color.green, EntryIcon),
            HelpSystemBuilder.CreateNewHelpSystemEntry("Add Line", "Add Line Description", Color.green, EntryIcon)
        };

        architectureEntries = new List<MenuEntry>
        {
            HelpSystemBuilder.CreateNewRefEntry(playerMenuEntries,"Player Menu", "Add Edge Description", Color.green, RefIcon),
            HelpSystemBuilder.CreateNewHelpSystemEntry("Map Architecture", "Mapping description", Color.green, EntryIcon)
        };

        mainMenuEntries = new List<MenuEntry>
        {
            HelpSystemBuilder.CreateNewRefEntry(architectureEntries, "Architecture", "ArchitectureDescription", Color.green, RefIcon),
            HelpSystemBuilder.CreateNewRefEntry(evolutionEntries, "Evolution", "EvolutionDescription", Color.red, RefIcon),
            HelpSystemBuilder.CreateNewRefEntry(debuggingEntries, "Debugging", "DebuggingDescription", Color.blue, RefIcon),
            HelpSystemBuilder.CreateNewRefEntry(qualityEntries, "Quality", "QualityDescription", Color.cyan,RefIcon)
        };

        mainMenu = HelpSystemBuilder.CreateMainMenu(HelpSystem, "MainMenu", "MainDescription", "Materials/Notification/info", mainMenuEntries);
    }
}