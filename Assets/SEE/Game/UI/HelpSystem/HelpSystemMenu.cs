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
            // Important note: You have to define the lowest hierachy-level first. 
            // That means, the mainMenu will be defined at the end and the lowest entry-list first.

            PlayerSettings.LocalPlayer.TryGetComponentOrLog(out HelpSystemEntry entry);

            List<MenuEntry> mainMenuEntries = new List<MenuEntry>();
            List<MenuEntry> architectureEntries = new List<MenuEntry>();
            List<MenuEntry> playerMenuEntries = new List<MenuEntry>();
            List<MenuEntry> evolutionEntries = new List<MenuEntry>();
            List<MenuEntry> debuggingEntries = new List<MenuEntry>();
            List<MenuEntry> qualityEntries = new List<MenuEntry>();
            List<MenuEntry> navigationEntries = new List<MenuEntry>();
            List<MenuEntry> playerNavigationEntries = new List<MenuEntry>();
            List<MenuEntry> cityNavigationEntries = new List<MenuEntry>();


            LinkedList<LinkedListEntry> addEdge = new LinkedList<LinkedListEntry>();
            addEdge.AddLast(new LinkedListEntry(1, "Press Space for opening the player menu", 0));
            addEdge.AddLast(new LinkedListEntry(2, "Leftklick on the entry new edge", 6));
            addEdge.AddLast(new LinkedListEntry(3, "Lefklick on a node to select the start node", 12));
            addEdge.AddLast(new LinkedListEntry(4, "Lefklick on a node to select the target node", 18));
            addEdge.AddLast(new LinkedListEntry(5, "Press Key F11 to  remove the selected start node", 27));

            LinkedList<LinkedListEntry> editNode = new LinkedList<LinkedListEntry>();
            editNode.AddLast(new LinkedListEntry(1, "Press Space for opening the player menu", 0));
            editNode.AddLast(new LinkedListEntry(2, "Leftklick on the entry edit node", 8));
            editNode.AddLast(new LinkedListEntry(3, "Leftklick on the node you want to edit", 14));
            editNode.AddLast(new LinkedListEntry(4, "You can see all editable Metrics inside of this new window", 21));
            editNode.AddLast(new LinkedListEntry(5, "Insert new values, if you want to change some of them", 25));
            editNode.AddLast(new LinkedListEntry(6, "Finish the edit-process by pushing the okay-button", 30));
            editNode.AddLast(new LinkedListEntry(6, "You can see that your new values are inserted", 34));

            LinkedList<LinkedListEntry> drawLine = new LinkedList<LinkedListEntry>();
            drawLine.AddLast(new LinkedListEntry(1, "Press Space for opening the player menu", 0));
            drawLine.AddLast(new LinkedListEntry(2, "Leftklick on the entry draw line", 5));
            drawLine.AddLast(new LinkedListEntry(3, "Leftklick at the place where the line should be started. Hold the mouse-button for drawing", 10));
            drawLine.AddLast(new LinkedListEntry(4, "Release the left mouse button for stop drawing the line. You can start drawing another", 15));

            LinkedList<LinkedListEntry> simpleNav = new LinkedList<LinkedListEntry>();

            LinkedList<LinkedListEntry> switchTable = new LinkedList<LinkedListEntry>();

            LinkedList<LinkedListEntry> zoomIntoCity = new LinkedList<LinkedListEntry>();
            // Senseful structure: Entry1 -> Subentry1 -> Subsubentry1, Subsubentry2,.... -> Subentry2 ...

            playerMenuEntries = new List<MenuEntry>
        {
            HelpSystemBuilder.CreateNewHelpSystemEntry("Add Edge", "How to add a new edge", Color.magenta, "Assets/SEE/Videos/AddEdge.mp4", addEdge, entry),
            HelpSystemBuilder.CreateNewHelpSystemEntry("Edit Node", "How to edit an existing node", Color.magenta, "Assets/SEE/Videos/EditNode.mp4", editNode, entry),
        };

            architectureEntries = new List<MenuEntry>
        {
            HelpSystemBuilder.CreateNewRefEntry(playerMenuEntries,"Player Menu", "Player Menu Description", Color.magenta),
            HelpSystemBuilder.CreateNewHelpSystemEntry("Map Architecture", "Mapping description", Color.magenta, "Assets/SEE/Videos/AddEdge.mp4",null, entry)
        };

            playerNavigationEntries = new List<MenuEntry>
        {
            HelpSystemBuilder.CreateNewHelpSystemEntry("Switch table", "How to switch between tables", Color.green, "Assets/SEE/Videos/AddEdge.mp4", switchTable, entry),
            HelpSystemBuilder.CreateNewHelpSystemEntry("Simple navigation", "How to edit an existing node", Color.green, "Assets/SEE/Videos/EditNode.mp4", simpleNav, entry),
        };

            cityNavigationEntries = new List<MenuEntry>
        {
            HelpSystemBuilder.CreateNewHelpSystemEntry("Zoom into Code-Cities", "How to zoom into code-cities", Color.green, "Assets/SEE/Videos/AddEdge.mp4", zoomIntoCity, entry),
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