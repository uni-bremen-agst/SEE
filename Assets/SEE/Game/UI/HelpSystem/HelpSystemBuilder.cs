using SEE.Controls;
using SEE.Game.UI.Menu;
using SEE.GO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public static class HelpSystemBuilder
{
    public static MenuEntry CreateNewHelpSystemEntry(string title, string description, Color entryColor, string iconPath, HelpSystemEntry entry = null)
    {
        MenuEntry helpSystemEntry = new MenuEntry(
            action: new UnityAction(() => { Execute(entry); }),
            title: title,
            description: description,
            entryColor: entryColor,
            icon: Resources.Load<Sprite>(iconPath));

        return helpSystemEntry;
    }

    public static NestedMenuEntry CreateNewRefEntry(List<MenuEntry> innerEntries, string title, string description, Color entryColor, string iconPath)
    {
        NestedMenuEntry refEntry = new NestedMenuEntry(
            innerEntries: innerEntries,
            title: title,
            description: description,
            entryColor: entryColor,
            icon: Resources.Load<Sprite>(iconPath));

        return refEntry;
    }

    public static NestedMenu CreateMainMenu(string helpSystemGO, string title, string description, string icon, List<MenuEntry> mainMenuEntries)
    {
        NestedMenu mainMenu = GameObject.Find(helpSystemGO).AddComponent<NestedMenu>();
        mainMenu.Title = title;
        mainMenu.Description = description;
        mainMenu.Icon = Resources.Load<Sprite>(icon);
        foreach(MenuEntry entry in mainMenuEntries)
        {
            mainMenu.AddEntry(entry);
        }

        return mainMenu;
    }

    public static void Execute(HelpSystemEntry helpSystem)
    {
        Debug.Log("called");
        GameObject go = GameObject.Find("HelpSystem");
        Debug.Log(go);
        go.TryGetComponentOrLog(out NestedMenu menu);
        menu.ToggleMenu();
        Debug.Log(helpSystem);
        helpSystem.Manager.OpenWindow();
    }
}
