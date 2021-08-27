using System.Collections.Generic;
using SEE.Controls;
using SEE.Game.UI.Menu;
using SEE.GO;
using UnityEngine;

namespace SEE.Game.UI.HelpSystem
{
    public class HelpSystemMenu : MonoBehaviour
    {
        /// <summary>
        /// The name of the PersonalAssistant-GameObject
        /// </summary>
        public const string PersonalAssistant = "PersonalAssistant";

        /// <summary>
        /// The name of the HelpSystem-GameObject
        /// </summary>
        public const string HelpSystem = "HelpSystem";

        /// <summary>
        /// The NestedMenu of the HelpSystem - responsible for the navigation
        /// inside of the use-cases.
        /// </summary>
        public NestedMenu mainMenu;

        /// <summary>
        /// True, if an entry is currently displayed. That means, 
        /// that there should be no interaction possible with the collider of see 
        /// for opening the nestedMenu while Entry is opened.
        /// </summary>
        public static bool IsEntryOpened { get; set; } = false;

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
                    if (hit.transform == GameObject.Find(PersonalAssistant).transform && mainMenu.MenuShown == false && !IsEntryOpened)
                    {
                        mainMenu.ShowMenu(true);
                    }
            }
        }

        /// <summary>
        /// Contains all Hierachy-Layers of the Help-System-Menu from the lowest to the highest layer.
        /// It creates all Sub-Menu's, RefEntries and HelpSystemEntrys and should be expanded by the developers.
        /// </summary>
        private void CreateStartMenu()
        {
            PlayerSettings.LocalPlayer.TryGetComponentOrLog(out HelpSystemEntry entry);

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
            // The LinkedListEntries needs an index starting with number 1 ascending, the displayed and said text and the start-position, 
            // where the text should appear and said by SEE. The first start-position has to be at point 0.
            // The linked list has to be inserted into a menu entry at the end of the function.

            LinkedList<LinkedListEntry> addEdge = new LinkedList<LinkedListEntry>();
            addEdge.AddLast(new LinkedListEntry(1, "Press Space for opening the player menu", 0));
            addEdge.AddLast(new LinkedListEntry(2, "Left click on the entry new edge", 6));
            addEdge.AddLast(new LinkedListEntry(3, "Left click on a node to select the start node", 12));
            addEdge.AddLast(new LinkedListEntry(4, "Left click on a node to select the target node", 18));
            addEdge.AddLast(new LinkedListEntry(5, "Press key F11 to reset the selected start node", 27));

            LinkedList<LinkedListEntry> editNode = new LinkedList<LinkedListEntry>();
            editNode.AddLast(new LinkedListEntry(1, "Press Space for opening the player menu", 0));
            editNode.AddLast(new LinkedListEntry(2, "Left click on the entry edit node", 8));
            editNode.AddLast(new LinkedListEntry(3, "Left click on the node you want to edit", 14));
            editNode.AddLast(new LinkedListEntry(4, "You can see all editable metrics inside of this new window", 21));
            editNode.AddLast(new LinkedListEntry(5, "Insert new values, if so desired", 25));
            editNode.AddLast(new LinkedListEntry(6, "Finish the editing process by pushing the Okay button", 30));
            editNode.AddLast(new LinkedListEntry(6, "Your newly inserted values are now visible", 34));

            LinkedList<LinkedListEntry> searchNode = new LinkedList<LinkedListEntry>();
            searchNode.AddLast(new LinkedListEntry(1, "Press key F for opening the search menu", 0));
            searchNode.AddLast(new LinkedListEntry(2, "Insert the name of the node or part of it into the input field", 5));
            searchNode.AddLast(new LinkedListEntry(3, "Press the button Okay", 13));
            searchNode.AddLast(new LinkedListEntry(4, "You can see the 5 best matching nodes ascending by their matching score", 16));
            searchNode.AddLast(new LinkedListEntry(5, "If you left click on one node entry, the respective node will be highlighted", 23));
            searchNode.AddLast(new LinkedListEntry(6, "The node is blinking and a light beam strikes for a few seconds", 30));

            LinkedList<LinkedListEntry> simpleNavigation = new LinkedList<LinkedListEntry>();
            simpleNavigation.AddLast(new LinkedListEntry(1, "To switch your point of view, hold the mouse button and move the mouse", 0));
            simpleNavigation.AddLast(new LinkedListEntry(2, "Press Key W or S to move forwards or backwards", 10));
            simpleNavigation.AddLast(new LinkedListEntry(3, "If your view is unlocked, you can move to the right or left with Key A or D", 20));

            LinkedList<LinkedListEntry> switchTable = new LinkedList<LinkedListEntry>();
            switchTable.AddLast(new LinkedListEntry(1, "By default, your focussed view is locked on the architecture-table", 0));
            switchTable.AddLast(new LinkedListEntry(2, "Press key L for unlock your view to move free in space", 10));
            switchTable.AddLast(new LinkedListEntry(3, "Now you can move free in space to other tables", 20));
            switchTable.AddLast(new LinkedListEntry(4, "If you press key L again, your view is focussed on the architecture again", 28));

            LinkedList<LinkedListEntry> zoomIntoCity = new LinkedList<LinkedListEntry>();
            zoomIntoCity.AddLast(new LinkedListEntry(1, "Hover with the mouse over the city you want to zoom into", 0));
            zoomIntoCity.AddLast(new LinkedListEntry(2, "Now, you can scroll the mouse wheel up for zooming", 5));
            zoomIntoCity.AddLast(new LinkedListEntry(3, "The zooming is focussed on the mouse pointer", 10));
            zoomIntoCity.AddLast(new LinkedListEntry(2, "If you want to zoom out, scroll with the mouse wheel down", 15));
            zoomIntoCity.AddLast(new LinkedListEntry(2, "If the city is not centered anymore, press key R to reset the layout to centered", 26));

            LinkedList<LinkedListEntry> hideNode1 = new LinkedList<LinkedListEntry>();

            LinkedList<LinkedListEntry> playEvolution = new LinkedList<LinkedListEntry>();

            // Important note: You have to define the lowest hierachy-level first. 
            // That means, the mainMenu will be defined at the end and the lowest entry-list first.
            // A list has to be filled with values before the higher hierachy-level will be filled with values and so on.

            playerMenuEntries = new List<MenuEntry>
            {
            HelpSystemBuilder.CreateNewHelpSystemEntry("Add Edge", "How to add a new edge", Color.magenta, "Assets/SEE/Videos/AddEdge.mp4", addEdge, entry),
            HelpSystemBuilder.CreateNewHelpSystemEntry("Edit Node", "How to edit an existing node", Color.magenta, "Assets/SEE/Videos/EditNode.mp4", editNode, entry),
            };

            architectureEntries = new List<MenuEntry>
            {
            HelpSystemBuilder.CreateNewRefEntry(playerMenuEntries,"Player Menu", "Player Menu Description", Color.magenta),
            HelpSystemBuilder.CreateNewHelpSystemEntry("Search for a node", "How to search for a node by source-name", Color.magenta, "Assets/SEE/Videos/searchNode.mp4", searchNode, entry)
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
            HelpSystemBuilder.CreateNewRefEntry(architectureEntries, "Architecture", "Use-Cases related to the architecture", Color.magenta),
            HelpSystemBuilder.CreateNewRefEntry(evolutionEntries, "Evolution", "Use-Cases related to software-evolution", Color.red),
            HelpSystemBuilder.CreateNewRefEntry(debuggingEntries, "Debugging", "Use-Cases related to software-debugging ", Color.blue),
            HelpSystemBuilder.CreateNewRefEntry(qualityEntries, "Quality", "Use-Cases related to the software-quality", Color.cyan),
            HelpSystemBuilder.CreateNewRefEntry(navigationEntries, "Navigation", "Use-Cases related to the navigation in SEE", Color.green)
            };

            mainMenu = HelpSystemBuilder.CreateMainMenu("Help System", "Find your specific Use-Case", "Materials/Notification/info", mainMenuEntries);
        }
    }
}