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

using System;
using System.Collections.Generic;
using SEE.Controls;
using SEE.Game.UI.Menu;
using UnityEngine;

namespace SEE.Game.UI.HelpSystem
{
    /// <summary>
    /// The menu of the help system.
    /// </summary>
    public class HelpSystemMenu : MonoBehaviour
    {
        /// <summary>
        /// The name of the PersonalAssistant GameObject
        /// </summary>
        private const string PersonalAssistantName = "PersonalAssistant";

        /// <summary>
        /// The personal assistant.
        /// </summary>
        private static Transform personalAssistant;

        /// <summary>
        /// The NestedMenu of the HelpSystem - responsible for the navigation
        /// inside of the use cases.
        /// </summary>
        private NestedMenu mainMenu;

        /// <summary>
        /// True, if an entry is currently displayed. That means,
        /// that there should be no interaction possible with the collider of the
        /// personal assistant for opening the menu while an entry is opened.
        /// </summary>
        public static bool IsEntryOpened { get; set; } = false;

        /// <summary>
        /// Creates the menu. Sets <see cref="personalAssistant"/> if not already set.
        /// </summary>
        private void Start()
        {
            if (personalAssistant == null)
            {
                GameObject personalAssistantObject = GameObject.Find(PersonalAssistantName);
                if (personalAssistantObject == null)
                {
                    Debug.LogError($"There is no personal assistant named {PersonalAssistantName}\n");
                    enabled = false;
                    return;
                }
                personalAssistant = personalAssistantObject.transform;
                mainMenu = CreateStartMenu();
            }
        }

        /// <summary>
        /// Shows the menu when the user clicks on the personal assistant or otherwise requests help.
        /// </summary>
        private void Update()
        {
            if (Input.GetMouseButtonDown(0) || SEEInput.Help())
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    if (hit.transform == personalAssistant && !mainMenu.MenuShown && !IsEntryOpened)
                    {
                        mainMenu.ShowMenu(true);
                    }
                }
            }
        }

        /// <summary>
        /// Contains all hierarchy layers of the help-system menu from the lowest to the highest layer.
        /// It creates all sub-menus, RefEntries and HelpSystemEntrys and should be expanded by the developers.
        /// </summary>
        /// <param name="menuHolder">the game object holding the <see cref="HelpSystemEntry"/> which is the root of the help menu</param>
        /// <returns>the created help menu</returns>
        private static NestedMenu CreateStartMenu()
        {
            HelpSystemEntry entry = HelpSystemBuilder.GetHelpMenuRootEntry();

            // Initialisation of all Lists for the NestedMenuEntries or MenuEntries.
            List<MenuEntry> mainMenuEntries = new List<MenuEntry>();
            List<MenuEntry> architectureEntries = new List<MenuEntry>();
            List<MenuEntry> playerMenuEntries = new List<MenuEntry>();
            List<MenuEntry> evolutionEntries = new List<MenuEntry>();
            List<MenuEntry> debuggingEntries = new List<MenuEntry>();
            List<MenuEntry> qualityEntries = new List<MenuEntry>();
            List<MenuEntry> navigationEntries = new List<MenuEntry>();
            List<MenuEntry> playerNavigationEntries = new List<MenuEntry>();
            List<MenuEntry> cityNavigationEntries = new List<MenuEntry>();

            // Hint for LinkedLists:
            // These lists are important for the Voice-Output of SEE and the displayed notes.
            // The LinkedListEntries needs an index starting with number 1 ascending, the displayed and said text and the start position,
            // where the text should appear and said by SEE. The first start position has to be at point 0.
            // The linked list has to be inserted into a menu entry at the end of the function.

            LinkedList<LinkedListEntry> addEdge = new LinkedList<LinkedListEntry>();
            addEdge.AddLast(new LinkedListEntry(1, "Press Space for opening the player menu.", 0));
            addEdge.AddLast(new LinkedListEntry(2, "Left click on the entry new edge.", 6));
            addEdge.AddLast(new LinkedListEntry(3, "Left click on a node to select the start node.", 12));
            addEdge.AddLast(new LinkedListEntry(4, "Left click on a node to select the target node.", 18));
            addEdge.AddLast(new LinkedListEntry(5, "Press key F11 to reset the selected start node.", 27));

            LinkedList<LinkedListEntry> editNode = new LinkedList<LinkedListEntry>();
            editNode.AddLast(new LinkedListEntry(1, "Press Space for opening the player menu.", 0));
            editNode.AddLast(new LinkedListEntry(2, "Left click on the entry edit node.", 8));
            editNode.AddLast(new LinkedListEntry(3, "Left click on the node you want to edit.", 14));
            editNode.AddLast(new LinkedListEntry(4, "You can see all editable metrics inside of this new window.", 21));
            editNode.AddLast(new LinkedListEntry(5, "Insert new values, if so desired.", 25));
            editNode.AddLast(new LinkedListEntry(6, "Finish the editing process by pushing the Okay button.", 30));
            editNode.AddLast(new LinkedListEntry(7, "Your newly inserted values are now visible.", 34));

            LinkedList<LinkedListEntry> searchNode = new LinkedList<LinkedListEntry>();
            searchNode.AddLast(new LinkedListEntry(1, "Press key F for opening the search menu.", 0));
            searchNode.AddLast(new LinkedListEntry(2, "Insert the name of the node or part of it into the input field.", 5));
            searchNode.AddLast(new LinkedListEntry(3, "Press the button Okay.", 13));
            searchNode.AddLast(new LinkedListEntry(4, "You can see the 5 best matching nodes, ascending by their matching score.", 16));
            searchNode.AddLast(new LinkedListEntry(5, "If you left click on one node entry, the respective node will be highlighted.", 23));
            searchNode.AddLast(new LinkedListEntry(6, "The node is blinking and a light beam strikes for a few seconds.", 30));

            LinkedList<LinkedListEntry> simpleNavigation = new LinkedList<LinkedListEntry>();
            simpleNavigation.AddLast(new LinkedListEntry(1, "To switch your point of view, hold the right mouse button and move the mouse.", 0));
            simpleNavigation.AddLast(new LinkedListEntry(2, "Press Key W or S to move forwards or backwards.", 10));
            simpleNavigation.AddLast(new LinkedListEntry(3, "If your view is unlocked, you can move to the right or left with Key A or D.", 20));

            LinkedList<LinkedListEntry> switchTable = new LinkedList<LinkedListEntry>();
            switchTable.AddLast(new LinkedListEntry(1, "By default, your focussed view may be locked on a code city.", 0));
            switchTable.AddLast(new LinkedListEntry(2, "Press key L for unlock your view to move free in space.", 10));
            switchTable.AddLast(new LinkedListEntry(3, "Now you can move free in space to other tables.", 20));
            switchTable.AddLast(new LinkedListEntry(4, "If you press key L again, your view is focussed on the architecture again.", 28));

            LinkedList<LinkedListEntry> zoomIntoCity = new LinkedList<LinkedListEntry>();
            zoomIntoCity.AddLast(new LinkedListEntry(1, "Hover with the mouse over the city you want to zoom into.", 0));
            zoomIntoCity.AddLast(new LinkedListEntry(2, "Now, you can scroll the mouse wheel up for zooming.", 6));
            zoomIntoCity.AddLast(new LinkedListEntry(3, "The zooming is focussed on the mouse pointer.", 10));
            zoomIntoCity.AddLast(new LinkedListEntry(4, "If you want to zoom out, scroll with the mouse wheel down.", 15));
            zoomIntoCity.AddLast(new LinkedListEntry(5, "If the city is not centered anymore, press key R to reset the layout to centered.", 26));

            LinkedList<LinkedListEntry> hideNode = new LinkedList<LinkedListEntry>();
            hideNode.AddLast(new LinkedListEntry(1, "For hiding one or more nodes press space to open the player menu and select hide node.", 0));
            hideNode.AddLast(new LinkedListEntry(2, "Scroll down and select hide selected or hide unselected.",9 ));
            hideNode.AddLast(new LinkedListEntry(3, "Select the node to hide or stay by left clicking on it.", 22));
            hideNode.AddLast(new LinkedListEntry(4, "For the selection of multiple nodes, hold key Control while clicking.", 30));
            hideNode.AddLast(new LinkedListEntry(5, "If you are ready, confirm your selection with the button done.", 39));
            hideNode.AddLast(new LinkedListEntry(6, "Hint: If you hide a parent node, all connected edges and child nodes will be hidden too.", 50));

            LinkedList<LinkedListEntry> playEvolution = new LinkedList<LinkedListEntry>();
            playEvolution.AddLast(new LinkedListEntry(1, "You can start the evolution with the play button at the navigation bar.", 0));
            playEvolution.AddLast(new LinkedListEntry(2, "You can see the current version of all versions on the lower right side.", 10));
            playEvolution.AddLast(new LinkedListEntry(3, "If you want to pause the evolution, press the pause button, which appears after pushing the play button.", 20));
            playEvolution.AddLast(new LinkedListEntry(4, "The other play button is for backward playing and works as same as the normal play button.", 27));
            playEvolution.AddLast(new LinkedListEntry(5, "Pushing multiple times on the double play buttons determines the play speed.", 34));
            playEvolution.AddLast(new LinkedListEntry(6, "You also can skip by moving the dragger of the navigation bar by left click on it.", 44));
            playEvolution.AddLast(new LinkedListEntry(7, "Yellow beams represent changes in classes.", 49));
            playEvolution.AddLast(new LinkedListEntry(8, "Green beams represent new classes.", 52));
            playEvolution.AddLast(new LinkedListEntry(9, "Red beams represent deleted classes.", 55));

            // Important note: You have to define the lowest hierachy-level first.
            // That means, the mainMenu will be defined at the end and the lowest entry-list first.
            // A list has to be filled with values before the higher hierachy-level will be filled with values and so on.

            playerMenuEntries = new List<MenuEntry>
            {
            HelpSystemBuilder.CreateNewHelpSystemEntry("Add Edge", "How to add a new edge", Color.magenta, "Assets/SEE/Videos/AddEdge.mp4", addEdge, entry),
            HelpSystemBuilder.CreateNewHelpSystemEntry("Edit Node", "How to edit an existing node", Color.magenta, "Assets/SEE/Videos/EditNode.mp4", editNode, entry),
            HelpSystemBuilder.CreateNewHelpSystemEntry("Hide Node", "How to hide node(s)", Color.magenta, "Assets/SEE/Videos/hideNode.mp4", hideNode, entry),
            };

            architectureEntries = new List<MenuEntry>
            {
            HelpSystemBuilder.CreateNewRefEntry(playerMenuEntries,"Player Menu", "Player Menu use cases", Color.magenta),
            HelpSystemBuilder.CreateNewHelpSystemEntry("Search for a node", "How to search for a node by source-name", Color.magenta, "Assets/SEE/Videos/searchNode.mp4", searchNode, entry)
            };

            evolutionEntries = new List<MenuEntry>
            {
            HelpSystemBuilder.CreateNewHelpSystemEntry("Navigation bar", "How to navigate between versions of evolution", Color.red, "Assets/SEE/Videos/evolution.mp4", playEvolution, entry)
            };

            playerNavigationEntries = new List<MenuEntry>
            {
            HelpSystemBuilder.CreateNewHelpSystemEntry("Switch table", "How to switch between tables", Color.green, "Assets/SEE/Videos/toggleFocus.mp4", switchTable, entry),
            HelpSystemBuilder.CreateNewHelpSystemEntry("Simple navigation", "How to edit an existing node", Color.green, "Assets/SEE/Videos/navigation.mp4", simpleNavigation, entry),
            };

            cityNavigationEntries = new List<MenuEntry>
            {
            HelpSystemBuilder.CreateNewHelpSystemEntry("Zoom into Code-Cities", "How to zoom into code-cities", Color.green, "Assets/SEE/Videos/zoomIntoCodeCity.mp4", zoomIntoCity, entry),
            };

            navigationEntries = new List<MenuEntry>
            {
            HelpSystemBuilder.CreateNewRefEntry(playerNavigationEntries,"Player navigation", "How to navigate in SEE", Color.green),
            HelpSystemBuilder.CreateNewRefEntry(cityNavigationEntries, "City navigation", "How to navigate a code city", Color.green)
            };

            mainMenuEntries = new List<MenuEntry>
            {
            HelpSystemBuilder.CreateNewRefEntry(architectureEntries, "Architecture", "Use cases related to the architecture", Color.magenta),
            HelpSystemBuilder.CreateNewRefEntry(evolutionEntries, "Evolution", "Use cases related to software-evolution", Color.red),
            HelpSystemBuilder.CreateNewRefEntry(debuggingEntries, "Debugging", "Use cases related to software-debugging ", Color.blue),
            HelpSystemBuilder.CreateNewRefEntry(qualityEntries, "Quality", "Use cases related to the software-quality", Color.cyan),
            HelpSystemBuilder.CreateNewRefEntry(navigationEntries, "Navigation", "Use cases related to the navigation in SEE", Color.green)
            };

            return HelpSystemBuilder.CreateMainMenu("Help System", "Find your specific use case", "Materials/Notification/info", mainMenuEntries);
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