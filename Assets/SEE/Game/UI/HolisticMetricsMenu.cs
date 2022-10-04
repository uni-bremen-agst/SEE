using System.Collections.Generic;
using System.Linq;
using SEE.Controls;
using SEE.Game.HolisticMetrics;
using SEE.Game.UI.Menu;
using SEE.Game.UI.Notification;
using SEE.Game.UI.PropertyDialog;
using SEE.Utils;
using UnityEngine;

namespace SEE.Game.UI
{
    /// <summary>
    /// The content of this class is inspired by OpeningDialog.cs because I think it makes sense to implement the menu
    /// like other menus in the SEE project; I don't think it would help to reinvent the wheel here.
    /// </summary>
    public class HolisticMetricsMenu : MonoBehaviour
    {
        private SimpleMenu menu;
        private BoardsManager boardsManager;

        private void Start()
        {
            menu = CreateMenu();
            boardsManager = GameObject.Find("HolisticMetricsManager").GetComponent<BoardsManager>();
        }
        
        private void Update()
        {
            if (SEEInput.ToggleHolisticMetricsMenu())
            {
                menu.ToggleMenu();
            }
        }

        private SimpleMenu CreateMenu()
        {
            GameObject actionMenuGO = new GameObject { name = "Holistic metrics menu" };
            IList<MenuEntry> entries = SelectionEntries();
            SimpleMenu actionMenu = actionMenuGO.AddComponent<SimpleMenu>();
            actionMenu.AllowNoSelection(true);
            actionMenu.Title = "Holistic metrics menu";
            actionMenu.Description = "Add / change the metrics board(s).";
            actionMenu.AddEntries(entries);
            // TODO: Ask why it is not being hidden when clicking on items
            actionMenu.HideAfterSelection(true);
            return actionMenu;
        }

        private IList<MenuEntry> SelectionEntries()
        {
            return new List<MenuEntry>
            {
                new MenuEntry(
                    action: NewBoard,
                    title: "New board",
                    description: "Add a new metrics board to the scene",
                    entryColor: Color.green.Darker()),
                new MenuEntry(
                    action: DeleteBoard,
                    title: "Delete board",
                    description: "Delete a board from the scene",
                    entryColor: Color.red.Darker()),
                new MenuEntry(
                    action: AddWidget,
                    title: "Add widget",
                    description: "Add a widget to a board",
                    entryColor: Color.green.Darker()),
                new MenuEntry(
                    action: RemoveWidget,
                    title: "Remove widget",
                    description: "Remove a widget from a board",
                    entryColor: Color.red.Darker()),
                new MenuEntry(
                    action: SaveBoardConfiguration,
                    title: "Save board",
                    description: "Save a board from the scene to a file",
                    entryColor: Color.blue),
                new MenuEntry(
                    action: LoadBoardConfiguration,
                    title: "Load board",
                    description: "Load a board from a file into the scene",
                    entryColor: Color.blue)
            };
        }

        private void NewBoard()
        {
            menu.ToggleMenu();
            // Refer to OpeningDialog Settings() !!!!!
            
            // First get the position where the player wants to position the new board
            
            // When the player has clicked on the ground, show him a dialog where he can rotate the board with a slider
            // and where he can enter a name for the board and where he can click ok to add the board.
            ShowNotification.Error(
                "Could not add the board", 
                "Reason: A board with that name already exists in the scene");
        }

        private void DeleteBoard()
        {
            menu.ToggleMenu();

            if (boardsManager.GetNames().Any())
            {
                new DeleteBoardDialog(boardsManager).Open();
            }
            else
            {
                ShowNotification.Warn(
                    "No boards in the scene",
                    "There are no metrics boards in the scene");
            }
        }

        private void AddWidget()
        {
            menu.ToggleMenu();
        }

        private void RemoveWidget()
        {
            menu.ToggleMenu();
        }

        private void SaveBoardConfiguration()
        {
            menu.ToggleMenu();

            if (boardsManager.GetNames().Any())
            {
                new SaveBoardConfigurationDialog(boardsManager).Open();
            }
            else
            {
                ShowNotification.Warn(
                    "No boards in the scene",
                    "There are no metrics boards in the scene");
            }
        }

        private void LoadBoardConfiguration()
        {
            menu.ToggleMenu();
            
            if (ConfigurationManager.GetSavedFileNames().Any())
            {
                new LoadBoardConfigurationDialog(boardsManager).Open();
            }
            else
            {
                ShowNotification.Warn(
                    "No files to choose from",
                    "The folder containing the board configurations is empty");
            }
        }
    }
}