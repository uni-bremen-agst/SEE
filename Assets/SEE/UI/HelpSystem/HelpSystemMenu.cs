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
using Newtonsoft.Json.Linq;
using SEE.Controls;
using SEE.Controls.KeyActions;
using SEE.UI.Menu;
using SEE.UI.Notification;
using UnityEngine;

namespace SEE.UI.HelpSystem
{
    /// <summary>
    /// The menu of the help system.
    /// </summary>
    internal class HelpSystemMenu : MonoBehaviour
    {
        /// <summary>
        /// The NestedMenu of the HelpSystem - responsible for the navigation
        /// inside of the use cases.
        /// </summary>
        private NestedListMenu mainMenu;

        /// <summary>
        /// File path the necessary information for the help system (in JSON).
        /// </summary>
        private readonly string jsonFilePath = Application.streamingAssetsPath + "/HelpSystem/helpSystem.json";

        /// <summary>
        /// Creates the menu.
        /// </summary>
        private void Start()
        {
            mainMenu = CreateStartMenu(jsonFilePath);
            if (KeyBindings.TryGetKeyCode(KeyAction.Help, out KeyCode helpKey))
            {
                ShowNotification.Info("Help System", $"Press {helpKey} to open help system.");
            }
            else
            {
                ShowNotification.Warn("Help System", "No keybinding for help system found.");
            }
        }

        /// <summary>
        /// Shows the help menu when the user requests help and content is provided by <see cref="jsonFilePath"/>.
        /// </summary>
        private void Update()
        {
            if (SEEInput.Help())
            {
                if (!System.IO.File.Exists(jsonFilePath))
                {
                    ShowNotification.Error("Missing Help System",
                        $"There is no content for the help system yet. File {jsonFilePath} is missing.");
                    return;
                }
                if (!mainMenu.ShowMenu)
                {
                    mainMenu.ShowMenu = true;
                }
            }
        }

        /// <summary>
        /// Contains all hierarchy layers of the help-system menu from the lowest to the highest layer.
        /// It creates all sub-menus, RefEntries and HelpSystemEntrys and can be expanded by providing an json file.
        /// </summary>
        /// <param name="menuHolder">the game object holding the <see cref="HelpSystemEntry"/> which is the root of the help menu</param>
        /// <returns>the created help menu</returns>
        private static NestedListMenu CreateStartMenu(string jsonFilePath)
        {
            HelpSystemEntry entry = HelpSystemBuilder.GetHelpMenuRootEntry(); // the root in unity

            string jsonContent = System.IO.File.ReadAllText(jsonFilePath);
            JObject jsonRoot = JObject.Parse(jsonContent);

            // Find the root menu entry node in the json
            JObject rootNode = (JObject)jsonRoot["HelpSystem"]["MenuEntry"];

            List<MenuEntry> mainMenuEntries = BuildMenuEntriesFromJson(rootNode, entry);

            return HelpSystemBuilder.CreateMainMenu(
                (string)rootNode["name"],
                (string)rootNode["description"],
                (string)rootNode["icon"],
                mainMenuEntries
            );
        }

        /// <summary>
        /// Parses <paramref name="jsonNode"/> and recursively build inner nodes (MenuEntries).
        /// When it arrives at an outer node, it calls <see cref="BuildHelpEntriesFromJson"/>
        /// for tutorial-like <see cref="HelpEntry"/>s.
        /// </summary>
        /// <param name="jsonNode">Json object of current level of the menu hierarchy.</param>
        /// <param name="parentEntry">Parent to attach the entries.</param>
        /// <returns>menuEntries for help menu</returns>
        private static List<MenuEntry> BuildMenuEntriesFromJson(JObject jsonNode, HelpSystemEntry parentEntry)
        {
            List<MenuEntry> menuEntries = new();

            foreach (JToken entry in jsonNode["MenuEntries"] ?? new JObject())
            {
                JObject node = (JObject)entry;

                if (node["type"].ToString() == "MenuEntry")
                {
                    List<MenuEntry> childEntries = BuildMenuEntriesFromJson(node, parentEntry);
                    menuEntries.Add(HelpSystemBuilder.CreateNewRefEntry(
                        childEntries,
                        (string)node["name"],
                        (string)node["description"],
                        ColorFromName((string)node["color"])
                    ));
                }
                else if (node["type"].ToString() == "HelpSystemEntry")
                {
                    LinkedList<HelpEntry> helpEntries = BuildHelpEntriesFromJson(node);
                    menuEntries.Add(HelpSystemBuilder.CreateNewHelpSystemEntry(
                        (string)node["name"],
                        (string)node["description"],
                        ColorFromName((string)node["color"]),
                        (string)node["video"],
                        helpEntries,
                        parentEntry
                    ));
                }
            }

            return menuEntries;
        }

        /// <summary>
        /// Gets information and builds tutorial-like <see cref="HelpEntry"/>.
        /// </summary>
        /// <param name="helpSystemNode">Node to attach the entry to</param>
        /// <returns>The help entries containing the explaining information</returns>
        private static LinkedList<HelpEntry> BuildHelpEntriesFromJson(JObject helpSystemNode)
        {
            LinkedList<HelpEntry> helpEntries = new();

            foreach (JToken entry in helpSystemNode["HelpEntries"] ?? new JArray())
            {
                JObject entryNode = (JObject)entry;
                int index = (int)entryNode["index"];
                string text = (string)entryNode["text"];
                int startPosition = (int)entryNode["startPosition"];

                helpEntries.AddLast(new HelpEntry(index, text, startPosition));
            }

            return helpEntries;
        }

        /// <summary>
        /// Converts some color name strings to a Unity Color object.
        /// </summary>
        /// <param name="colorName">The name of the color as a string.</param>
        /// <returns>Unity Color object.</returns>
        private static Color ColorFromName(string colorName)
        {
            if (string.IsNullOrWhiteSpace(colorName))
            {
                return Color.white;
            }

            return colorName.ToLower() switch
            {
                "magenta" => Color.magenta,
                "red" => Color.red,
                "blue" => Color.blue,
                "cyan" => Color.cyan,
                "green" => Color.green,
                _ => Color.white,
            };
        }

        /// <summary>
        /// Displays the menu when it is hidden, and vice versa.
        /// </summary>
        internal void ToggleMenu()
        {
            mainMenu.ToggleMenu();
        }

        /// <summary>
        /// Resets the menu to the state it was before any entry was selected.
        /// </summary>
        internal void Reset()
        {
            mainMenu.ResetToBase();
        }
    }
}
