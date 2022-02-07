// Copyright 2022 Thore Frenzel.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and
// associated documentation files (the "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the
// following conditions:
//
// The above copyright notice and this permission notice shall be included in all copies or substantial
// portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT
// LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO
// EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER
// IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR
// THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System.Collections.Generic;
using System.IO;
using SEE.Game.UI.Menu;
using TMPro;
using UnityEngine;
using UnityEngine.Video;

namespace SEE.Game.UI.HelpSystem
{
    /// <summary>
    /// Contains all important functions for the creation of the helpSystemMenu.
    /// Contains the business logic of the helpSystemEntry execution, too.
    /// </summary>
    internal static class HelpSystemBuilder
    {
        /// <summary>
        /// The name of the HelpSystem GameObject. Must be present in the scene.
        /// </summary>
        private const string HelpSystemName = "HelpSystem";

        /// <summary>
        /// The root menu entry of the help system. It is assumed to be a component
        /// attached to the game object named <see cref="HelpSystemName"/>.
        /// </summary>
        private static HelpSystemEntry helpMenuRootEntry;

        /// <summary>
        /// Returns the root menu entry of the help system.
        /// Precondition: There must be a game object in the scene named <see cref="HelpSystemName"/>
        /// holding a <see cref="HelpSystemEntry"/> component.
        /// </summary>
        /// <returns>the root menu entry of the help system</returns>
        /// <exception cref="System.Exception">thrown if there is no game object in the scene
        /// named <see cref="HelpSystemName"/> or - if one exists - that object is not
        /// holding a <see cref="HelpSystemEntry"/> component</exception>
        public static HelpSystemEntry GetHelpMenuRootEntry()
        {
            if (helpMenuRootEntry == null)
            {
                GameObject helpSystem = HelpSystemObject();
                if (!helpSystem.TryGetComponent(out helpMenuRootEntry))
                {
                    throw new System.Exception($"The help system named {HelpSystemName} does not have a {typeof(HelpSystemEntry)} component.");
                }
            }
            return helpMenuRootEntry;
        }

        /// <summary>
        /// The help-system menu. It is assumed to be a component
        /// attached to the game object named <see cref="HelpSystemName"/>.
        /// </summary>
        private static HelpSystemMenu helpSystemMenu;

        /// <summary>
        /// Returns the help-system menu.
        /// Precondition: There must be a game object in the scene named <see cref="HelpSystemName"/>
        /// holding a <see cref="HelpSystemMenu"/> component.
        /// </summary>
        /// <returns>the help-system menu</returns>
        /// <exception cref="System.Exception">thrown if there is no game object in the scene
        /// named <see cref="HelpSystemName"/> or - if one exists - that object is not
        /// holding a <see cref="HelpSystemMenu"/> component</exception>
        public static HelpSystemMenu GetHelpSystemMenu()
        {
            if (helpSystemMenu == null)
            {
                GameObject helpSystem = HelpSystemObject();
                if (!helpSystem.TryGetComponent(out helpSystemMenu))
                {
                    throw new System.Exception($"The help system named {HelpSystemName} does not have a {typeof(HelpSystemMenu)} component.");
                }
            }
            return helpSystemMenu;
        }

        /// <summary>
        /// Returns the game object named <see cref="HelpSystemName"/>.
        /// </summary>
        /// <returns>the game object named <see cref="HelpSystemName"/></returns>
        /// <exception cref="System.Exception">throw if there is no such object in the scene</exception>
        private static GameObject HelpSystemObject()
        {
            GameObject helpSystem = GameObject.Find(HelpSystemName);
            if (helpSystem == null)
            {
                throw new System.Exception($"There is no help system named {HelpSystemName} in the scene.");
            }
            return helpSystem;
        }

        /// <summary>
        /// The path to the default-icon for an HelpSystemEntry in the nested menu.
        /// </summary>
        private const string EntryIcon = "Materials/ModernUIPack/Eye";

        /// <summary>
        /// The path to the default-icon for an RefEntry in the nested menu.
        /// </summary>
        private const string RefIcon = "Materials/ModernUIPack/Plus";

        /// <summary>
        /// The LinkedListEntries of the currently selected HelpSystemEntry.
        /// </summary>
        public static LinkedList<HelpEntry> currentEntries;

        /// <summary>
        /// The space where the entry is inside.
        /// </summary>
        public static GameObject EntrySpace;

        /// <summary>
        /// The headline gameObject of the helpSystemEntry or rather the headline which is inside of the dynamicPanel.
        /// </summary>
        public static GameObject Headline;

        /// <summary>
        /// Creates a new HelpSystemEntry. That means, it should be inserted as the last element of a branch inside of the help-system-menu.
        /// As a difference to a normal HelpSystemMenu-Entry, onclick, there will be started an HelpSystemEntry which explains the specific use-Case.
        /// </summary>
        /// <param name="title">The title of the HelpSystemMenu-Entry.</param>
        /// <param name="description">The description of the HelpSystemMenu-Entry, displayed as a tooltip.</param>
        /// <param name="entryColor">The color of the HelpSystemMenu-Entry.</param>
        /// <param name="videoPath">The path of the video to display.</param>
        /// <param name="keywords">The keywords which will be displayed at the bottom of the HelpSystemEntry.</param>
        /// <param name="entry">The HelpSystemEntry where these values should be inserted.</param>
        /// <returns>A new HelpSystemMenu-Entry.</returns>
        public static MenuEntry CreateNewHelpSystemEntry
            (string title,
            string description,
            Color entryColor,
            string videoPath,
            LinkedList<HelpEntry> keywords,
            HelpSystemEntry entry = null)
        {
            return new MenuEntry(
                action: () => { Execute(entry, title, keywords, videoPath); },
                title: title,
                description: description,
                entryColor: entryColor,
                icon: Resources.Load<Sprite>(EntryIcon));
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
        /// <returns>A new NestedMenuEntry.</returns>
        public static NestedMenuEntry CreateNewRefEntry(List<MenuEntry> innerEntries, string title, string description, Color entryColor)
        {
            return new NestedMenuEntry(
                innerEntries: innerEntries,
                title: title,
                description: description,
                entryColor: entryColor,
                icon: Resources.Load<Sprite>(RefIcon));
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
            NestedMenu mainMenu = GameObject.Find(HelpSystemName).AddComponent<NestedMenu>();
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
        /// <param name="videoPath">The path of the video which should be displayed.</param>
        /// <param name="instructions">All instructions which should be displayed and spoken aloud.</param>
        public static void Execute(HelpSystemEntry helpSystem, string entryTitle, LinkedList<HelpEntry> instructions, string videoPath)
        {
            helpSystem.EntryShown = true;
            helpSystem.ShowEntry();

            Headline.GetComponent<TextMeshProUGUI>().text = entryTitle != null ? entryTitle : "Placeholder";

            HelpSystemEntry entry = GetHelpMenuRootEntry();
            VideoPlayer videoPlayer = entry.GetVideoPlayer();
            videoPlayer.url = Path.Combine(Application.streamingAssetsPath, videoPath);
            videoPlayer.Play();
            videoPlayer.SetDirectAudioMute(0, true);

            entry.IsPlaying = true;
            GetHelpSystemMenu().ToggleMenu();
            currentEntries = instructions;
        }
    }
}
