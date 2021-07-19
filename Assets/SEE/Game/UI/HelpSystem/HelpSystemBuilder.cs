using SEE.Controls;
using SEE.Game.UI.Menu;
using SEE.GO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Video;

public static class HelpSystemBuilder
{
    public static string HelpSystemGO = "HelpSystem";

    public static MenuEntry CreateNewHelpSystemEntry(string title, string description, Color entryColor, string iconPath, string titleh, string desh, HelpSystemEntry entry = null)
    {
        MenuEntry helpSystemEntry = new MenuEntry(
            action: new UnityAction(() => { Execute(entry,titleh,desh); }),
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

    public static void Execute(HelpSystemEntry helpSystem, string titleh, string desh)
    {
        GameObject go = GameObject.Find(HelpSystemGO);
        go.TryGetComponentOrLog(out NestedMenu menu);
        PlayerSettings.LocalPlayer.TryGetComponentOrLog(out HelpSystemEntry entry);
        GameObject.FindGameObjectWithTag("VideoPlayer").TryGetComponentOrLog(out VideoPlayer videoPlayer);
        if(videoPlayer == null)
        {
            throw new System.Exception("No Video-Player found");
        }
        if (titleh != null)
        {
            entry.Manager.titleText = titleh;
            entry.Manager.descriptionText = desh;
        }
        else
        {
            entry.Manager.descriptionText = "placeholder";
            entry.Manager.titleText = "placeholder";
        }
        entry.Manager.UpdateUI();
        videoPlayer.Play();
        entry.IsPlaying = true;
        menu.ToggleMenu();
        helpSystem.Manager.OpenWindow();
    }
}
