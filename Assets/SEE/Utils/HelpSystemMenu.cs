using SEE.Game.UI.Menu;
using System.Collections.Generic;
using UnityEngine;

public class HelpSystemMenu : MonoBehaviour
{
    /// <summary>
    /// All menus and submenus are needed for the help-system
    /// </summary>
    private List<SimpleMenu> menus = new List<SimpleMenu>();

    /// <summary>
    /// The main menu from where the user can navigate to sub-menus.
    /// </summary>
    private SimpleMenu mainMenu;

    // Start is called before the first frame update
    void Start()
    {
        CreateHelpSystemMenu();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            RaycastHit hit;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out hit))
                if (hit.transform == GameObject.Find("PersonalAssistant").transform)
                {
                    bool menuIsOpen = false;
                    foreach (SimpleMenu menu in menus)
                    {
                        if (menu.MenuShown)
                        {
                            menuIsOpen = true;
                        }
                    }
                    if (!menuIsOpen)
                    {
                        mainMenu.ToggleMenu();
                    }
                }
        }
    }

    /// <summary>
    /// Creates the Help-system menu. Therefore, all needed menus and their entrys has to be listed and created
    /// inside of this function.
    /// </summary>
    /// <param name="attachTo">The game object the menu should be attached to. If <c>null</c>, a
    /// new game object will be created.</param>
    public void CreateHelpSystemMenu(GameObject attachTo = null)
    {
        // Note: A ?? expression can't be used here, or Unity's overloaded null-check will be overridden.
        GameObject modeMenuGO = attachTo ? attachTo : new GameObject { name = "HelpSystem" };

        mainMenu = CreateNewMenu(modeMenuGO, "Functions", "Where do you need help?", "Materials/Notification/info");
        SimpleMenu navigation = CreateNewMenu(modeMenuGO, "Navigation", "Choose your specific use-case", "Materials/Notification/error");
        SimpleMenu playerMenu = CreateNewMenu(modeMenuGO, "Player Menu", "Choose your specific use-case", "Materials/Notification/error");
        SimpleMenu architecture = CreateNewMenu(modeMenuGO, "Architecture", "Choose your specific use-case", "Materials/Notification/error");
        SimpleMenu evolution = CreateNewMenu(modeMenuGO, "Evolution", "Choose your specific use-case", "Materials/Notification/error");
        SimpleMenu debugging = CreateNewMenu(modeMenuGO, "Debugging", "Choose your specific use-case", "Materials/Notification/error");
        SimpleMenu quality = CreateNewMenu(modeMenuGO, "Quality", "Choose your specific use-case", "Materials/Notification/error");

        // Add all menu큦 to list
        menus.Add(mainMenu);
        menus.Add(playerMenu);
        menus.Add(navigation);
        menus.Add(architecture);
        menus.Add(evolution);
        menus.Add(debugging);
        menus.Add(quality);

        // Entry큦 for main Menu - titel has to be unique!
        CreateNewRefEntry("Navigation", "All Use-Cases in context of simple navigation in SEE", Color.gray, "Materials/Notification/info", mainMenu, navigation, mainMenu);
        CreateNewRefEntry("Architecture", "All Use-Cases in context of Architecture-comparision", Color.gray, "Materials/Notification/info", mainMenu, architecture, mainMenu);
        CreateNewRefEntry("Evolution", "All Use-Cases in context of Software-Evolution", Color.gray, "Materials/Notification/info", mainMenu, evolution, mainMenu);
        CreateNewRefEntry("Debugging", "All Use-Cases in context of Debugging", Color.gray, "Materials/Notification/info", mainMenu, debugging, mainMenu);
        CreateNewRefEntry("Quality", "All Use-Cases in context of Software-Quality", Color.gray, "Materials/Notification/info", mainMenu, quality, mainMenu);
        // Entry큦 for Navigation-Menu 
        CreateNewRefEntry("Move in space", "How to navigate inside of the application", Color.gray, "Materials/Notification/info", navigation, mainMenu, navigation);
        CreateNewRefEntry("Switch table", "How to switch tables for other use-cases", Color.gray, "Materials/Notification/info", navigation, mainMenu, navigation);
        CreateNewRefEntry("Zoom", "How to zoom into a code-city", Color.gray, "Materials/Notification/info", navigation, mainMenu, navigation);
        CreateNewRefEntry("Lock Camera", "How to lock your POV to a specific table", Color.gray, "Materials/Notification/info", navigation, mainMenu, navigation);
        // Entry큦 for Architecture-Menu
        CreateNewRefEntry("Search for a node", "Description how to search for a specific node", Color.gray, "Materials/Notification/info", architecture, mainMenu, architecture);
        // Entry큦 for Evolution-Menu
        CreateNewRefEntry("Start Evolution", "How to start and use the evolution", Color.gray, "Materials/Notification/info", evolution, mainMenu, evolution);
        CreateNewRefEntry("Metric Charts", "How to show and use the metric charts", Color.gray, "Materials/Notification/info", evolution, mainMenu, evolution);
        // Entry큦 for Player-Menu
        CreateNewRefEntry("Open player Menu", "How to open the Player Menu for interacting with code-cities", Color.gray, "Materials/Notification/info", playerMenu, mainMenu, playerMenu);
        CreateNewRefEntry("Adding a node", "Description how to add a new node to a code-city", Color.gray, "Materials/Notification/info", playerMenu, mainMenu, playerMenu);
        CreateNewRefEntry("Adding an edge", "Description how to add a new edge to a code-city", Color.gray, "Materials/Notification/info", playerMenu, mainMenu, playerMenu);
        CreateNewRefEntry("Scale a node", "Description how to scale a node", Color.gray, "Materials/Notification/info", playerMenu, mainMenu, playerMenu);
        CreateNewRefEntry("Edit a node", "Description how to edit the metrics of an existing node", Color.gray, "Materials/Notification/info", playerMenu, mainMenu, playerMenu);
        CreateNewRefEntry("Delete node(s) or edge(s)", "Description how to delete either node(s) or edge(s)", Color.gray, "Materials/Notification/info", playerMenu, mainMenu, playerMenu);
        CreateNewRefEntry("Hide nodes(s) or edge(s)", "Description how to hide edge(s) or node(s) of a code-city", Color.gray, "Materials/Notification/info", playerMenu, mainMenu, playerMenu);
        CreateNewRefEntry("Draw lines", "Description how to Draw lines inside of the application", Color.gray, "Materials/Notification/info", playerMenu, mainMenu, playerMenu);
        CreateNewRefEntry("Undo or Redo an action", "Description how to undo or redo an action", Color.gray, "Materials/Notification/info", playerMenu, mainMenu, playerMenu);
        // Entry큦 for Debugging-Menu
        CreateNewRefEntry("Show code", "Description how to show the source-code of an object", Color.gray, "Materials/Notification/info", debugging, mainMenu, debugging);
        CreateNewRefEntry("Choose shown code-window","Description how to switch the shown code-window of an object", Color.gray, "Materials/Notification/info", debugging, mainMenu, debugging);
        CreateNewRefEntry("Animation speed", "Description how to in or decrease the animation speed", Color.gray, "Materials/Notification/info", debugging, mainMenu, debugging);
        // Entry큦 for Quality-Menu
    }

    /// <summary>
    /// Executes a switch between two menus. That means, menu1 will be
    /// closed while menu2 will be opened, so there are only one menu active at
    /// the same time.
    /// </summary>
    /// <param name="activeMenu">The menu which should be closed</param>
    /// <param name="inactiveMenu">The menu which should be opened</param>
    public void SwitchMenu(SimpleMenu activeMenu, SimpleMenu inactiveMenu)
    {
        activeMenu.ToggleMenu();
        inactiveMenu.ToggleMenu();
    }

    /// <summary>
    /// Creates a new RefEntry for a specific <see cref="SimpleMenu". RefEntry means,
    /// that this entry is only responsible for the navigation between menus and submenus
    /// e.g. the user selects an entry of menu1, menu1 will be closed and menu2, a specific submenu 
    /// will be opened new.
    /// Note: source and target menu does not nessecarily means parent and child menu,
    /// because another option could be a "backwards"-navigation instead of a "forwards"-navigation/>
    /// </summary>
    /// <param name="title">The title of the entry</param>
    /// <param name="description">the description of the entry, shown as a tooltip</param>
    /// <param name="color">the color of the entry</param>
    /// <param name="iconPath">the path to an icon for this entry</param>
    /// <param name="sourceMenu">the menu, from where has to be navigated</param>
    /// <param name="targetMenu">the menu to which has to be navigated</param>
    /// <param name="menuToAdd">the menu to which this entry should be added</param>
    /// <param name="active">true, if the entry should be active, else false</param>
    public void CreateNewRefEntry(string title, string description, Color color, string iconPath,
        SimpleMenu sourceMenu, SimpleMenu targetMenu, SimpleMenu menuToAdd, bool active = true)
    {
        ToggleMenuEntry newEntry = new ToggleMenuEntry(
          active: active,
          entryAction: () => SwitchMenu(sourceMenu, targetMenu),
          exitAction: null,
          title: title,
          description: description,
          entryColor: color,
          icon: Resources.Load<Sprite>(iconPath)
          );

        menuToAdd.AddEntry(newEntry);
    }

    /// <summary>
    /// Creates a new SimpleMenu-(sub)menu for the help-system.
    /// </summary>
    /// <param name="objectToAddMenu">the gameObject where the menu has to be added</param>
    /// <param name="title"> the title of the new menu</param>
    /// <param name="description">the subtitle or rather description of the menu</param>
    /// <param name="iconPath">the path to the icon for the new menu</param>
    /// <returns>the new SimpleMenu</returns>
    public SimpleMenu CreateNewMenu(GameObject objectToAddMenu, string title, string description, string iconPath)
    {
        SimpleMenu simpleMenu = objectToAddMenu.AddComponent<SimpleMenu>();
        simpleMenu.Icon = Resources.Load<Sprite>(iconPath);
        simpleMenu.Title = title;
        simpleMenu.Description = description;

        if (mainMenu != null)
        {
            CreateNewRefEntry("Play all", "Example", new Color(0.3f, 0.4f, 0.6f), "Materials/Notification/info", simpleMenu, mainMenu, simpleMenu);
            CreateNewRefEntry("Back", "Beschreibung2", Color.red, "Materials/Notification/info", simpleMenu, mainMenu, simpleMenu);
        }
        return simpleMenu;
    }
}
