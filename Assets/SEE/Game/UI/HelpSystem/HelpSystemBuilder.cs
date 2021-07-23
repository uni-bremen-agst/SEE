using SEE.Controls;
using SEE.Game.UI.Menu;
using SEE.GO;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Video;

public static class HelpSystemBuilder
{
    /// <summary>
    /// The name of the HelpSystem GameObject.
    /// </summary>
    public const string HelpSystemGO = "HelpSystem";

    /// <summary>
    /// The path to the default-icon for an HelpSystemEntry in the nested menu.
    /// </summary>
    private const string EntryIcon = "Materials/ModernUIPack/Eye";

    /// <summary>
    /// The path to the default-icon for an RefEntry in the nested menu.
    /// </summary>
    private const string RefIcon = "Materials/ModernUIPack/Plus";

    /// <summary>
    /// Creates a new HelpSystemEntry. That means, it should be inserted as the last element of a branch inside of the help-system-menu. 
    /// As a difference to a normal HelpSystemMenu-Entry, onclick, there will be started an HelpSystemEntry which explains the specific use-Case.
    /// </summary>
    /// <param name="title">The title of the HelpSystemMenu-Entry.</param>
    /// <param name="description">The description of the HelpSystemMenu-Entry, displayed as a tooltip.</param>
    /// <param name="entryColor">The color of the HelpSystemMenu-Entry.</param>
    /// <param name="keywords">The keywords which will be displayed at the bottom of the HelpSystemEntry.</param>
    /// <param name="entry">The HelpSystemEntry where these values should be inserted.</param>
    /// <returns>A new HelpSystemMenu-Entry.</returns>
    public static MenuEntry CreateNewHelpSystemEntry(string title, string description, Color entryColor, string videoPath, List<string> keywords, HelpSystemEntry entry = null)
    {
        MenuEntry helpSystemEntry = new MenuEntry(
            action: new UnityAction(() => { Execute(entry, title, keywords, videoPath); }),
            title: title,
            description: description,
            entryColor: entryColor,
            icon: Resources.Load<Sprite>(EntryIcon));

        return helpSystemEntry;
    }

    /// <summary>
    /// Creates a new Ref-Entry for the HelpSystemMenu. That means, this entry contains a list of further entries,
    /// which are opened as the lower hierachy-layer onclick. These entries are only responsible for the structure of the HelpSystemMenu,
    /// they are not executing an HelpSystemEntry.
    /// </summary>
    /// <param name="innerEntries">The inner Entries, which are displayed onclick as the lower hierachy-layer.</param>
    /// <param name="title">The title of the RefEntry.</param>
    /// <param name="description">The description of the RefEntry, displayed as a tooltip.</param>
    /// <param name="entryColor">The color of the Ref-Entry.</param>
    /// <param name="iconPath">The path of the icon of the RefEntry.</param>
    /// <returns>A new NestedMenuEntry.</returns>
    public static NestedMenuEntry CreateNewRefEntry(List<MenuEntry> innerEntries, string title, string description, Color entryColor)
    {
        NestedMenuEntry refEntry = new NestedMenuEntry(
            innerEntries: innerEntries,
            title: title,
            description: description,
            entryColor: entryColor,
            icon: Resources.Load<Sprite>(RefIcon));

        return refEntry;
    }

    /// <summary>
    /// Creates the Main-Menu of the HelpSystemMenu. 
    /// More specific, it creates the highest Hierachy-Layer, 
    /// where new Layers can be attached to with the functions above.
    /// </summary>
    /// <param name="title">The title of the HelpSystem-MainMenu.</param>
    /// <param name="description">The description of the HelpSystem-MainMenu.</param>
    /// <param name="icon">The icon of the HelpSystem-MainMenu.</param>
    /// <param name="mainMenuEntries">The MenuEntries which are displayed inside of the MainMenu for more hierachy-layers.</param>
    /// <returns>The Main-Menu as a NestedMenu.</returns>
    public static NestedMenu CreateMainMenu(string title, string description, string icon, List<MenuEntry> mainMenuEntries)
    {
        NestedMenu mainMenu = GameObject.Find(HelpSystemGO).AddComponent<NestedMenu>();
        mainMenu.Title = title;
        mainMenu.Description = description;
        mainMenu.Icon = Resources.Load<Sprite>(icon);
        foreach (MenuEntry entry in mainMenuEntries)
        {
            mainMenu.AddEntry(entry);
        }

        return mainMenu;
    }

    /// <summary>
    /// Starts the execution of the HelpSystemEntry. That means, it starts the video, displays a list of keywords and sets
    /// the title and description of the HelpSystemEntry as selected.
    /// </summary>
    /// <param name="helpSystem">The HelpSystemEntry which will display the given params.</param>
    /// <param name="entryTitle">The title of the HelpSystemEntry.</param>
    /// <param name="entryDescription">The description of the HelpSystemEntry.</param>
    public static void Execute(HelpSystemEntry helpSystem, string entryTitle, List<string> keywords, string videoPath)
    {
        GameObject go = GameObject.Find(HelpSystemGO);
        go.TryGetComponentOrLog(out NestedMenu menu);
        PlayerSettings.LocalPlayer.TryGetComponentOrLog(out HelpSystemEntry entry);
        GameObject.FindGameObjectWithTag("VideoPlayer").TryGetComponentOrLog(out VideoPlayer videoPlayer);
        if (videoPlayer == null)
        {
            throw new System.Exception("No Video-Player found");
        }
        if (entryTitle != null)
        {
            entry.Manager.titleText = entryTitle;
            entry.Manager.descriptionText = entryTitle;
        }
        else
        {
            entry.Manager.descriptionText = "placeholder";
            entry.Manager.titleText = "placeholder";
        }
        GameObject keywordDisplay = GameObject.Find("Code");
        TextMeshProUGUI tmp = keywordDisplay.GetComponent<TextMeshProUGUI>();
        string keywordsAsString = "";
        foreach (string keyword in keywords)
        {
            keywordsAsString += "- " + keyword + "\n";
        }
        keywordsAsString += "\n";
        tmp.text = keywordsAsString;
        entry.Manager.UpdateUI();
        videoPlayer.url = videoPath;
        videoPlayer.Play();
        entry.IsPlaying = true;
        menu.ToggleMenu();
        helpSystem.Manager.OpenWindow();
        HelpSystemMenu.IsEntryOpened = true;
    }
}
