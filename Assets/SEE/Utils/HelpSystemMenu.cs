using SEE.Game.UI.Menu;
using System.Collections.Generic;
using UnityEngine;

public class HelpSystemMenu : MonoBehaviour
{
    /// <summary>
    /// All menus and submenus are needed for the help-system
    /// </summary>
    private List<SimpleMenu<MenuEntry>> menus = new List<SimpleMenu<MenuEntry>>();

    /// <summary>
    /// The main menu from where the user can navigate to sub-menus.
    /// </summary>
    private SimpleMenu mainMenu;

    private NestedMenu nestedMenu;

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
    public void CreateStartMenu(GameObject attachTo = null)
    {
        // Note: A ?? expression can't be used here, or Unity's overloaded null-check will be overridden.
        GameObject modeMenuGO = GameObject.Find("HelpSystem");
        if(modeMenuGO == null)
        {
            modeMenuGO = new GameObject("HelpSystem");
        }
        mainMenu = modeMenuGO.AddComponent<SimpleMenu>();
        mainMenu.Title = "Help System";
        mainMenu.Description = "What do you want to know ?";
        mainMenu.Icon = Resources.Load<Sprite>("Materials/Notification/error");

        NestedMenu navigation = CreateNewNestedMenu(modeMenuGO, "Navigation", "Choose your specific use-case", "Materials/Notification/error", mainMenu);
        NestedMenu playerMenu = CreateNewNestedMenu(modeMenuGO, "Player Menu", "Choose your specific use-case", "Materials/Notification/error", mainMenu);
        NestedMenu architecture = CreateNewNestedMenu(modeMenuGO, "Architecture", "Choose your specific use-case", "Materials/Notification/error", mainMenu);
        NestedMenu evolution = CreateNewNestedMenu(modeMenuGO, "Evolution", "Choose your specific use-case", "Materials/Notification/error", mainMenu);
        NestedMenu debugging = CreateNewNestedMenu(modeMenuGO, "Debugging", "Choose your specific use-case", "Materials/Notification/error", mainMenu);
        NestedMenu quality = CreateNewNestedMenu(modeMenuGO, "Quality", "Choose your specific use-case", "Materials/Notification/error", mainMenu);

        // Entry´s for main Menu - title has to be unique!
        CreateNewRefEntry("Player Menu", "All Use-Cases in context of Player Menu", Color.gray, "Materials/Notification/info", mainMenu, playerMenu, mainMenu);
        CreateNewRefEntry("Navigation", "All Use-Cases in context of simple navigation in SEE", Color.gray, "Materials/Notification/info", mainMenu, navigation, mainMenu);
        CreateNewRefEntry("Architecture", "All Use-Cases in context of Architecture-comparision", Color.gray, "Materials/Notification/info", mainMenu, architecture, mainMenu);
        CreateNewRefEntry("Evolution", "All Use-Cases in context of Software-Evolution", Color.gray, "Materials/Notification/info", mainMenu, evolution, mainMenu);
        CreateNewRefEntry("Debugging", "All Use-Cases in context of Debugging", Color.gray, "Materials/Notification/info", mainMenu, debugging, mainMenu);
        CreateNewRefEntry("Quality", "All Use-Cases in context of Software-Quality", Color.gray, "Materials/Notification/info", mainMenu, quality, mainMenu);
    }

    /// <summary>
    /// Executes a switch between two menus. That means, menu1 will be
    /// closed while menu2 will be opened, so there are only one menu active at
    /// the same time.
    /// </summary>
    /// <param name="activeMenu">The menu which should be closed</param>
    /// <param name="inactiveMenu">The menu which should be opened</param>
    public void SwitchMenu(SimpleMenu<MenuEntry> activeMenu, SimpleMenu<MenuEntry> inactiveMenu)
    {
        activeMenu.ToggleMenu();
        inactiveMenu.ToggleMenu();
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

    /// <summary>
    /// Creates a new NestedMenu for the help-system.
    /// </summary>
    /// <param name="objectToAddMenu">the gameObject where the menu has to be added</param>
    /// <param name="title"> the title of the new menu</param>
    /// <param name="description">the subtitle or rather description of the menu</param>
    /// <param name="iconPath">the path to the icon for the new menu</param>
    /// <param name="parent">the parent of the </param>
    /// <returns>the new SimpleMenu</returns>
    public NestedMenu CreateNewNestedMenu(GameObject objectToAddMenu, string title, string description, string iconPath, SimpleMenu<MenuEntry> parent)
    {
        NestedMenu nestedMenu = objectToAddMenu.AddComponent<NestedMenu>();
        nestedMenu.Icon = Resources.Load<Sprite>(iconPath);
        nestedMenu.Title = title;
        nestedMenu.Description = description;
        nestedMenu.parent = parent;
        nestedMenu.ToggleMenu();
        if (mainMenu != null)
        {
       
        }
        return nestedMenu;
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
        SimpleMenu<MenuEntry> sourceMenu, SimpleMenu<MenuEntry> targetMenu, SimpleMenu<MenuEntry> menuToAdd, bool active = true)
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
}
