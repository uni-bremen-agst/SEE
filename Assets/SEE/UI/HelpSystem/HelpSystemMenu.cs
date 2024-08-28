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
using SEE.Controls;
using SEE.UI.Menu;
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
        /// Creates the menu.
        /// </summary>
        private void Start()
        {
            mainMenu = CreateStartMenu();
        }

        /// <summary>
        /// Shows the help menu when the user requests help.
        /// </summary>
        private void Update()
        {
            if (SEEInput.Help())
            {
                if (!mainMenu.ShowMenu)
                {
                    mainMenu.ShowMenu = true;
                }
            }
        }

        /// <summary>
        /// Contains all hierarchy layers of the help-system menu from the lowest to the highest layer.
        /// It creates all sub-menus, RefEntries and HelpSystemEntrys and should be expanded by the developers.
        /// </summary>
        /// <param name="menuHolder">the game object holding the <see cref="HelpSystemEntry"/> which is the root of the help menu</param>
        /// <returns>the created help menu</returns>
        private static NestedListMenu CreateStartMenu()
        {
            HelpSystemEntry entry = HelpSystemBuilder.GetHelpMenuRootEntry();

            // FIXME: This should be loaded from a file and not be hard-coded here.

            // Initialisation of all Lists for the NestedMenuEntries or MenuEntries.
            List<MenuEntry> mainMenuEntries = new();
            List<MenuEntry> architectureEntries = new();
            List<MenuEntry> playerMenuEntries = new();
            List<MenuEntry> evolutionEntries = new();
            List<MenuEntry> debuggingEntries = new();
            List<MenuEntry> qualityEntries = new();
            List<MenuEntry> navigationEntries = new();
            List<MenuEntry> playerNavigationEntries = new();
            List<MenuEntry> cityNavigationEntries = new();

            // Hint for LinkedLists:
            // These lists are important for the Voice-Output of SEE and the displayed notes.
            // The LinkedListEntries needs an index starting with number 1 ascending, the displayed and said text and the start position,
            // where the text should appear and said by SEE. The first start position has to be at point 0.
            // The linked list has to be inserted into a menu entry at the end of the function.

            LinkedList<HelpEntry> addEdge = new();
            addEdge.AddLast(new HelpEntry(1, "Press Space for opening the player menu.", 0));
            addEdge.AddLast(new HelpEntry(2, "Left click on the entry new edge.", 6));
            addEdge.AddLast(new HelpEntry(3, "Left click on a node to select the start node.", 12));
            addEdge.AddLast(new HelpEntry(4, "Left click on a node to select the target node.", 18));
            addEdge.AddLast(new HelpEntry(5, "Press key F11 to reset the selected start node.", 27));

            LinkedList<HelpEntry> editNode = new();
            editNode.AddLast(new HelpEntry(1, "Press Space for opening the player menu.", 0));
            editNode.AddLast(new HelpEntry(2, "Left click on the entry edit node.", 8));
            editNode.AddLast(new HelpEntry(3, "Left click on the node you want to edit.", 14));
            editNode.AddLast(new HelpEntry(4, "You can see all editable metrics inside of this new window.", 21));
            editNode.AddLast(new HelpEntry(5, "Insert new values, if so desired.", 25));
            editNode.AddLast(new HelpEntry(6, "Finish the editing process by pushing the Okay button.", 30));
            editNode.AddLast(new HelpEntry(7, "Your newly inserted values are now visible.", 34));

            LinkedList<HelpEntry> searchNode = new();
            searchNode.AddLast(new HelpEntry(1, "Press key F for opening the search menu.", 0));
            searchNode.AddLast(new HelpEntry(2, "Insert the name of the node or part of it into the input field.", 5));
            searchNode.AddLast(new HelpEntry(3, "Press the button Okay.", 13));
            searchNode.AddLast(new HelpEntry(4, "You can see the 5 best matching nodes, ascending by their matching score.", 16));
            searchNode.AddLast(new HelpEntry(5, "If you left click on one node entry, the respective node will be highlighted.", 23));
            searchNode.AddLast(new HelpEntry(6, "The node is blinking and a light beam strikes for a few seconds.", 30));

            LinkedList<HelpEntry> simpleNavigation = new();
            simpleNavigation.AddLast(new HelpEntry(1, "To switch your point of view, hold the right mouse button and move the mouse.", 0));
            simpleNavigation.AddLast(new HelpEntry(2, "Press Key W or S to move forwards or backwards.", 10));
            simpleNavigation.AddLast(new HelpEntry(3, "If your view is unlocked, you can move to the right or left with Key A or D.", 20));

            LinkedList<HelpEntry> switchTable = new();
            switchTable.AddLast(new HelpEntry(1, "By default, your focussed view may be locked on a code city.", 0));
            switchTable.AddLast(new HelpEntry(2, "Press key L for unlock your view to move free in space.", 10));
            switchTable.AddLast(new HelpEntry(3, "Now you can move free in space to other tables.", 20));
            switchTable.AddLast(new HelpEntry(4, "If you press key L again, your view is focussed on the architecture again.", 28));

            LinkedList<HelpEntry> zoomIntoCity = new();
            zoomIntoCity.AddLast(new HelpEntry(1, "Hover with the mouse over the city you want to zoom into.", 0));
            zoomIntoCity.AddLast(new HelpEntry(2, "Now, you can scroll the mouse wheel up for zooming.", 6));
            zoomIntoCity.AddLast(new HelpEntry(3, "The zooming is focussed on the mouse pointer.", 10));
            zoomIntoCity.AddLast(new HelpEntry(4, "If you want to zoom out, scroll with the mouse wheel down.", 15));
            zoomIntoCity.AddLast(new HelpEntry(5, "If the city is not centered anymore, press key R to reset the layout to centered.", 26));

            LinkedList<HelpEntry> hideNode = new();
            hideNode.AddLast(new HelpEntry(1, "For hiding one or more nodes press space to open the player menu and select hide node.", 0));
            hideNode.AddLast(new HelpEntry(2, "Scroll down and select hide selected or hide unselected.", 9));
            hideNode.AddLast(new HelpEntry(3, "Select the node to hide or stay by left clicking on it.", 22));
            hideNode.AddLast(new HelpEntry(4, "For the selection of multiple nodes, hold key Control while clicking.", 30));
            hideNode.AddLast(new HelpEntry(5, "If you are ready, confirm your selection with the button done.", 39));
            hideNode.AddLast(new HelpEntry(6, "Hint: If you hide a parent node, all connected edges and child nodes will be hidden too.", 50));

            LinkedList<HelpEntry> playEvolution = new();
            playEvolution.AddLast(new HelpEntry(1, "You can start the evolution with the play button at the navigation bar.", 0));
            playEvolution.AddLast(new HelpEntry(2, "You can see the current version of all versions on the lower right side.", 10));
            playEvolution.AddLast(new HelpEntry(3, "If you want to pause the evolution, press the pause button, which appears after pushing the play button.", 20));
            playEvolution.AddLast(new HelpEntry(4, "The other play button is for backward playing and works as same as the normal play button.", 27));
            playEvolution.AddLast(new HelpEntry(5, "Pushing multiple times on the double play buttons determines the play speed.", 34));
            playEvolution.AddLast(new HelpEntry(6, "You also can skip by moving the dragger of the navigation bar by left click on it.", 44));
            playEvolution.AddLast(new HelpEntry(7, "Yellow beams represent changes in classes.", 49));
            playEvolution.AddLast(new HelpEntry(8, "Green beams represent new classes.", 52));
            playEvolution.AddLast(new HelpEntry(9, "Red beams represent deleted classes.", 55));

            // Important note: You have to define the lowest hierachy-level first.
            // That means, the mainMenu will be defined at the end and the lowest entry-list first.
            // A list has to be filled with values before the higher hierachy-level will be filled with values and so on.

            playerMenuEntries = new List<MenuEntry>
            {
                HelpSystemBuilder.CreateNewHelpSystemEntry("Add Edge", "How to add a new edge", Color.magenta, "Videos/AddEdge.mp4", addEdge, entry),
                HelpSystemBuilder.CreateNewHelpSystemEntry("Edit Node", "How to edit an existing node", Color.magenta, "Videos/EditNode.mp4", editNode, entry),
                HelpSystemBuilder.CreateNewHelpSystemEntry("Hide Node", "How to hide node(s)", Color.magenta, "Videos/hideNode.mp4", hideNode, entry),
            };

            architectureEntries = new List<MenuEntry>
            {
                HelpSystemBuilder.CreateNewRefEntry(playerMenuEntries, "Player Menu", "Player Menu use cases", Color.magenta),
                HelpSystemBuilder.CreateNewHelpSystemEntry("Search for a node", "How to search for a node by source-name", Color.magenta, "Videos/searchNode.mp4", searchNode, entry)
            };

            evolutionEntries = new List<MenuEntry>
            {
                HelpSystemBuilder.CreateNewHelpSystemEntry("Navigation bar", "How to navigate between versions of evolution", Color.red, "Videos/evolution.mp4", playEvolution, entry)
            };

            playerNavigationEntries = new List<MenuEntry>
            {
                HelpSystemBuilder.CreateNewHelpSystemEntry("Switch table", "How to switch between tables", Color.green, "Videos/toggleFocus.mp4", switchTable, entry),
                HelpSystemBuilder.CreateNewHelpSystemEntry("Simple navigation", "How to edit an existing node", Color.green, "Videos/navigation.mp4", simpleNavigation, entry),
            };

            cityNavigationEntries = new List<MenuEntry>
            {
                HelpSystemBuilder.CreateNewHelpSystemEntry("Zoom into Code-Cities", "How to zoom into code-cities", Color.green, "Videos/zoomIntoCodeCity.mp4", zoomIntoCity, entry),
            };

            navigationEntries = new List<MenuEntry>
            {
                HelpSystemBuilder.CreateNewRefEntry(playerNavigationEntries, "Player navigation", "How to navigate in SEE", Color.green),
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
