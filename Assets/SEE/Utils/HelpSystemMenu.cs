using SEE.Game.UI.Menu;
using SEE.Utils;
using System;
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

    /// <summary>
    /// The name of the PersonalAssistant-GameObject
    /// </summary>
    public const string PersonalAssistant = "PersonalAssistant";

    /// <summary>
    /// The name of the HelpSystem-GameObject
    /// </summary>
    public const string HelpSystem = "HelpSystem";

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
                if (hit.transform == GameObject.Find(PersonalAssistant).transform)
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
    public void CreateStartMenu()
    {
        // Note: A ?? expression can't be used here, or Unity's overloaded null-check will be overridden.
        GameObject modeMenuGO = GameObject.Find(HelpSystem);
        if (modeMenuGO == null)
        {
            modeMenuGO = new GameObject(HelpSystem);
        }
        CreateMainMenu(modeMenuGO);

        NestedMenu nestedMenu = modeMenuGO.AddComponent<NestedMenu>();
        nestedMenu.ToggleMenu();

    }

    /// <summary>
    /// Creates the Main-Menu for the help-system with all sub-menus as entrys.
    /// </summary>
    /// <param name="modeMenuGO">The GameObject where the menu has to be attached.</param>
    public void CreateMainMenu(GameObject modeMenuGO)
    {
        mainMenu = modeMenuGO.AddComponent<SimpleMenu>();
        mainMenu.Title = "Help System";
        mainMenu.Description = "What do you want to know ?";
        mainMenu.Icon = Resources.Load<Sprite>("Materials/Notification/error");

        // Entry´s for main Menu - titel has to be unique!
        CreateNewRefEntry(modeMenuGO, "Navigation", "All Use-Cases in context of simple navigation in SEE", Color.gray, "Materials/Notification/info", mainMenu);
        CreateNewRefEntry(modeMenuGO, "Architecture", "All Use-Cases in context of Architecture-comparision", Color.gray, "Materials/Notification/info", mainMenu);
        CreateNewRefEntry(modeMenuGO, "Evolution", "All Use-Cases in context of Software-Evolution", Color.gray, "Materials/Notification/info", mainMenu);
        CreateNewRefEntry(modeMenuGO, "Debugging", "All Use-Cases in context of Debugging", Color.gray, "Materials/Notification/info", mainMenu);
        CreateNewRefEntry(modeMenuGO, "Quality", "All Use-Cases in context of Software-Quality", Color.gray, "Materials/Notification/info", mainMenu);
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
    public NestedMenu OverrideMenu(GameObject objectToAddMenu, string title, string description, string iconPath, SimpleMenu<MenuEntry> parent)
    {
        Destroyer.DestroyComponent(objectToAddMenu.GetComponent<NestedMenu>());
        NestedMenu nestedMenu = objectToAddMenu.AddComponent<NestedMenu>();
        nestedMenu.Icon = Resources.Load<Sprite>(iconPath);
        nestedMenu.Title = title;
        nestedMenu.Description = description;
        nestedMenu.parent = parent;
        if (objectToAddMenu.GetComponent<NestedMenu>().Entries.Count == 0)
        {
            Debug.Log(objectToAddMenu.GetComponent<NestedMenu>().Entries.Count);
            Debug.Log(nestedMenu.Entries.Count);
            nestedMenu.entries = GetEntries(title);
        }
        mainMenu.ToggleMenu();
        nestedMenu.ToggleMenu();

        return nestedMenu;
    }

    public List<MenuEntry> GetEntries(string s)
    {
        List<MenuEntry> entries = new List<MenuEntry>();
        switch (s)
        {
            case "Navigation":
                entries.Add(new ToggleMenuEntry(true, null, null, "Test1", "Description1", Color.red, Resources.Load<Sprite>("Materials/Notification/info")));
                entries.Add(new ToggleMenuEntry(true, null, null, "Test2"));
                Debug.Log("Das klappt");
                break;
            case "Architecture":
                entries.Add(new MenuEntry(null, "Test3"));
                entries.Add(new MenuEntry(null, "Test4"));
                break;
            default:
                Debug.Log("WHAT");
                throw new NotImplementedException();
        }
        return entries;
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
    /// <param name="menuToAdd">the menu to which this entry should be added</param>
    /// <param name="active">true, if the entry should be active, else false</param>
    public void CreateNewRefEntry(GameObject go, string title, string description, Color color, string iconPath,
        SimpleMenu<MenuEntry> menuToAdd, bool active = true)
    {
        ToggleMenuEntry newEntry = new ToggleMenuEntry(
          active: active,
          entryAction: () => OverrideMenu(go, title, description, iconPath, menuToAdd),
          exitAction: null,
          title: title,
          description: description,
          entryColor: color,
          icon: Resources.Load<Sprite>(iconPath)
          );

        menuToAdd.AddEntry(newEntry);
    }
}
