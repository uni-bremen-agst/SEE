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

            LinkedList<LinkedListEntry> addEdge = new LinkedList<LinkedListEntry>();
            addEdge.AddLast(new LinkedListEntry(1, "Press Space for opening the player menu", 0));
            addEdge.AddLast(new LinkedListEntry(2, "Leftklick on the entry add edge", 8));
            addEdge.AddLast(new LinkedListEntry(3, "Lefklick on a node to select the start node", 18));
            addEdge.AddLast(new LinkedListEntry(4, "Lefklick on a node to select the target node", 23));
            addEdge.AddLast(new LinkedListEntry(5, "Press Key F11 to  remove the selected start node", 36));

            LinkedList<LinkedListEntry> addNode = new LinkedList<LinkedListEntry>();
            addNode.AddLast(new LinkedListEntry(1, "Press Space for opening the player menu", 0));
            addNode.AddLast(new LinkedListEntry(2, "Leftklick on the entry add node", 10));
            addNode.AddLast(new LinkedListEntry(3, "Leftklick on the city where a new node should be added", 14));
            addNode.AddLast(new LinkedListEntry(4, "The new node will be inserted automatically", 17));
            addNode.AddLast(new LinkedListEntry(5, "If you want to change the scale or the metrics, choose edit or scale node.", 20));

            LinkedList<LinkedListEntry> drawLine = new LinkedList<LinkedListEntry>();
            drawLine.AddLast(new LinkedListEntry(1, "Press Space for opening the player menu", 0));
            drawLine.AddLast(new LinkedListEntry(2, "Leftklick on the entry draw line", 5));
            drawLine.AddLast(new LinkedListEntry(3, "Leftklick at the place where the line should be started. Hold the mouse-button for drawing", 10));
            drawLine.AddLast(new LinkedListEntry(4, "Release the left mouse button for stop drawing the line. You can start drawing another", 15));

            playerMenuEntries = new List<MenuEntry>
        {
            HelpSystemBuilder.CreateNewHelpSystemEntry("Add Edge", "Add Edge Description", Color.magenta, "Assets/SEE/Videos/AddEdge.mp4", addEdge, entry),
            HelpSystemBuilder.CreateNewHelpSystemEntry("Add Node", "Add Node Description", Color.magenta, "Assets/SEE/Videos/AddNode.mp4", addNode, entry),
            HelpSystemBuilder.CreateNewHelpSystemEntry("Add Line", "Add Line Description", Color.magenta, "Assets/SEE/Videos/AddEdge.mp4", drawLine, entry)
        };

            architectureEntries = new List<MenuEntry>
        {
            HelpSystemBuilder.CreateNewRefEntry(playerMenuEntries,"Player Menu", "Player Menu Description", Color.magenta),
            HelpSystemBuilder.CreateNewHelpSystemEntry("Map Architecture", "Mapping description", Color.magenta, "Assets/SEE/Videos/AddEdge.mp4",null, entry)
        };

            mainMenuEntries = new List<MenuEntry>
        {
            HelpSystemBuilder.CreateNewRefEntry(architectureEntries, "Architecture", "Use-Cases related to the architecture", Color.magenta),
            HelpSystemBuilder.CreateNewRefEntry(evolutionEntries, "Evolution", "Use-Cases related to software-evolution", Color.red),
            HelpSystemBuilder.CreateNewRefEntry(debuggingEntries, "Debugging", "Use-Cases related to software-debugging ", Color.blue),
            HelpSystemBuilder.CreateNewRefEntry(qualityEntries, "Quality", "Use-Cases related to the software-quality", Color.cyan)
        };

            mainMenu = HelpSystemBuilder.CreateMainMenu("Help System", "Find your specific Use-Case", "Materials/Notification/info", mainMenuEntries);
        }
    }
}