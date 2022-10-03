using System;
using System.IO;
using System.Linq;
using UnityEngine;
using Michsky.UI.ModernUIPack;
using SEE.Controls;
using SEE.Game.HolisticMetrics.Metrics;
using TMPro;

namespace SEE.Game.HolisticMetrics
{
    /// <summary>
    /// This class controls the holistic metrics menu (loads data into ints menus, controls what happens when buttons
    /// are pushed and so on.
    /// </summary>
    internal class MenuManager : MonoBehaviour
    {
        /// <summary>
        /// Reference to the BoardsManager.
        /// </summary>
        [SerializeField] private BoardsManager boardsManager;
        
        /// <summary>
        /// Reference to the menu GameObject. This is the menu which allows the user to customize the metrics board.
        /// </summary>
        [SerializeField] private GameObject menu;

        /// <summary>
        /// the dropdown menu that will allow the user to select a metrics board on which to add a new widget.
        /// </summary>
        [SerializeField] private CustomDropdown selectBoardOnWhichToAdd;

        /// <summary>
        /// The dropdown menu that will allow the user to select a metric type.
        /// </summary>
        [SerializeField] private CustomDropdown selectMetricToAdd;

        /// <summary>
        /// The dropdown menu that will allow the user to select a widget.
        /// </summary>
        [SerializeField] private CustomDropdown selectWidgetToAdd;

        /// <summary>
        /// Reference to the TMP input field for entering a name under which to create the new metrics board.
        /// </summary>
        [SerializeField] private TMP_InputField createBoardInputField;

        /// <summary>
        /// The dropdown menu that will allow the user to select a metrics board to save.
        /// </summary>
        [SerializeField] private CustomDropdown selectBoardToSave;

        /// <summary>
        /// Holds a reference to the TMP input field for entering a name under which to save the selected metrics board.
        /// </summary>
        [SerializeField] private TMP_InputField saveBoardInputField;

        /// <summary>
        /// Holds a reference to the dropdown menu where the player can select a file from which to load a board.
        /// </summary>
        [SerializeField] private CustomDropdown loadBoardDropdown;

        /// <summary>
        /// Holds a reference to the notification manager for holistic metrics notifications.
        /// </summary>
        [SerializeField] private NotificationManager notificationManager;

        /// <summary>
        /// When Start() is called, this will be filled with the types of all classes that inherit from class "Metric".
        /// </summary>
        private Type[] metricTypes;

        /// <summary>
        /// When Start() is called, this will be filled with all widget prefabs from
        /// Assets/Resources/Prefabs/HolisticMetrics.
        /// </summary>
        private GameObject[] widgetPrefabs;


        /// <summary>
        /// Dynamically gets types of all classes that inherit from Metric and puts them in an array. Also dynamically
        /// gets all widget prefabs from the Resources folder and puts them in an array. After filling the array, the
        /// dropdowns in the menu are filled with the array values.
        /// </summary>
        private void Start()
        {
            menu.SetActive(true);
            
            metricTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(domainAssembly => domainAssembly.GetTypes())
                .Where(type => type.IsSubclassOf(typeof(Metric)))
                .ToArray();

            string widgetPrefabsPath = Path.Combine("Prefabs", "HolisticMetrics", "Widgets");
            widgetPrefabs = Resources.LoadAll<GameObject>(widgetPrefabsPath);

            foreach (Type metricType in metricTypes)
            {
                selectMetricToAdd.CreateNewItem(metricType.Name, null);
            }

            foreach (GameObject widgetPrefab in widgetPrefabs)
            {
                selectWidgetToAdd.CreateNewItem(widgetPrefab.name, null);
            }

            ToggleMenu();
        }

        /// <summary>
        /// This switches the menu's activeInHierarchy field value and if the menu is being toggled on, it also reloads
        /// the values in the dropdowns because they might have changed. However some of the dropdowns should not change
        /// at runtime (those that list the metric types and the widget types), so we only initialize these in the
        /// Start() method and then never change them.
        /// </summary>
        internal void ToggleMenu()
        {
            menu.SetActive(!menu.activeInHierarchy);
            if (menu.activeInHierarchy)
            {
                // Clear all the dropdowns
                selectBoardToSave.dropdownItems.Clear();
                selectBoardOnWhichToAdd.dropdownItems.Clear();
                loadBoardDropdown.dropdownItems.Clear();

                // Reload dropdowns that let the user choose a metrics board
                foreach (string boardName in boardsManager.GetNames())
                {
                    selectBoardToSave.CreateNewItem(boardName, null);
                    selectBoardOnWhichToAdd.CreateNewItem(boardName, null);
                }
                
                // Reload dropdown that lets the user choose a configuration file
                foreach (string fileName in ConfigurationManager.GetSavedFileNames())
                {
                    loadBoardDropdown.CreateNewItem(fileName, null);
                }
            }
        }

        /// <summary>
        /// Using the currently selected metrics board, metric and widget from the three dropdowns next to the button
        /// that calls this method, this method will spawn a widget on the metrics board. 
        /// </summary>
        public void AddWidget()
        {
            string selectedMetricsBoardTitle = selectBoardOnWhichToAdd.selectedText.text;
            BoardController canvasControllerForAdding = boardsManager.FindControllerByName(selectedMetricsBoardTitle);
            string selectedMetric = metricTypes[selectMetricToAdd.selectedItemIndex].Name;
            string selectedWidget = widgetPrefabs[selectWidgetToAdd.selectedItemIndex].name;
            ToggleMenu();
            
            // Show the user a popup telling him what to do
            notificationManager.title = "Hint";
            notificationManager.description = "Left click on the board where you want to add the widget";
            notificationManager.timer = 3f;
            notificationManager.UpdateUI();
            notificationManager.OpenNotification();
            
            WidgetConfiguration widgetConfiguration = new WidgetConfiguration()
            {
                MetricType = selectedMetric,
                WidgetName = selectedWidget
            };
            WidgetPositionGetter widgetPositionGetter = 
                canvasControllerForAdding.gameObject.AddComponent<WidgetPositionGetter>();
            widgetPositionGetter.Setup(widgetConfiguration);
        }

        /// <summary>
        /// Lets the player choose a position for the new board, then instantiates a new board there. This will be
        /// called by a menu button.
        /// </summary>
        public void CreateNewBoard()
        {
            BoardConfiguration boardConfiguration = new BoardConfiguration()
            {
                Title = createBoardInputField.text
            };
            ToggleMenu();
            boardsManager.CreateNewBoard(boardConfiguration);
        }

        /// <summary>
        /// Saves the currently selected metrics board (selected in the dropdown menu) to a file.
        /// </summary>
        public void SaveBoard()
        {
            string selectedName = selectBoardToSave.selectedText.text;
            BoardController selectedBoard = boardsManager.FindControllerByName(selectedName);
            ConfigurationManager.SaveBoard(selectedBoard, saveBoardInputField.text);
            ToggleMenu();
        }

        /// <summary>
        /// Loads the board saved in the file that is currently selected in the corresponding dropdown.
        /// </summary>
        public void LoadBoard()
        {
            string fileName = loadBoardDropdown.selectedText.text;
            BoardConfiguration boardConfiguration = ConfigurationManager.LoadBoard(fileName);
            boardsManager.CreateNewBoard(boardConfiguration);
            ToggleMenu();
        }

        /// <summary>
        /// This can be called when an input field is entered/exited. This will deactivate all other methods that listen
        /// for player input so that the player can enter text into an input field without accidentally interacting with
        /// anything else.
        /// </summary>
        public void ToggleInput()
        {
            SEEInput.KeyboardShortcutsEnabled = !SEEInput.KeyboardShortcutsEnabled;
        }
    }
}
