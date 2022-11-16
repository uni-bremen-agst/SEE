using System.Collections.Generic;
using System.Linq;
using SEE.Controls;
using SEE.Game.HolisticMetrics;
using SEE.Game.UI.Menu;
using SEE.Game.UI.Notification;
using SEE.Game.UI.PropertyDialog.HolisticMetrics;
using SEE.Utils;
using UnityEngine;
using ActionHistory = SEE.Controls.Actions.HolisticMetrics.ActionHistory;

namespace SEE.Game.UI.HolisticMetrics
{
    /// <summary>
    /// This class controls the holistic metrics menu that allows the player to add, change, delete and save metrics
    /// boards.
    /// The content of this class is inspired by OpeningDialog.cs because I think it makes sense to implement the menu
    /// like other menus in the SEE project; I don't think it would help to reinvent the wheel here.
    /// </summary>
    public class HolisticMetricsMenu : MonoBehaviour
    {
        /// <summary>
        /// The menu instance that represents the holistic metrics menu
        /// </summary>
        private SimpleMenu menu;

        /// <summary>
        /// When this component starts, it will instantiate the menu with all the menu entries and everything.
        /// </summary>
        private void Start()
        {
            menu = CreateMenu();
        }
        
        /// <summary>
        /// In every Update step, we want to check whether the player has pressed the key for toggling the holistic
        /// metrics menu. In that case, we toggle the menu.
        /// </summary>
        private void Update()
        {
            if (SEEInput.ToggleHolisticMetricsMenu())
            {
                menu.ToggleMenu();
            }
        }

        /// <summary>
        /// This method returns a new holistic metrics menu with all the menu entries and everything.
        /// </summary>
        /// <returns>A new holistic metrics menu</returns>
        private SimpleMenu CreateMenu()
        {
            GameObject actionMenuGO = new GameObject { name = "Holistic metrics menu" };
            IList<MenuEntry> entries = SelectionEntries();
            SimpleMenu actionMenu = actionMenuGO.AddComponent<SimpleMenu>();
            actionMenu.AllowNoSelection(true);
            actionMenu.Title = "Holistic metrics menu";
            actionMenu.Description = "Add / change the metrics board(s).";
            actionMenu.AddEntries(entries);
            actionMenu.HideAfterSelection(true);
            return actionMenu;
        }

        /// <summary>
        /// Returns a list of all menu entries that belong into the holistic metrics menu.
        /// </summary>
        /// <returns>The list of menu entries for the holistic metrics menu</returns>
        private IList<MenuEntry> SelectionEntries()
        {
            return new List<MenuEntry>
            {
                new MenuEntry(
                    action: Undo,
                    title: "Undo last action",
                    description: "Revert the last holistic metrics action",
                    entryColor: Color.grey,
                    icon: Resources.Load<Sprite>("Materials/ModernUIPack/Arrow Bold")),
                new MenuEntry(
                    action: Redo,
                    title: "Redo last action",
                    description: "Re-does the action that was last undone",
                    entryColor: Color.grey,
                    icon: Resources.Load<Sprite>("Materials/ModernUIPack/Arrow Bold reverse")),
                new MenuEntry(
                    action: NewBoard,
                    title: "New board",
                    description: "Add a new metrics board to the scene",
                    entryColor: Color.green.Darker(),
                    icon: Resources.Load<Sprite>("Materials/ModernUIPack/Plus")),
                new MenuEntry(
                    action : MoveBoard,
                    title: "Move boards",
                    description: "Toggles the move buttons under the boards",
                    entryColor: Color.yellow,
                    icon: Resources.Load<Sprite>("Materials/40+ Simple Icons - Free/Scale_Simple_Icons_UI")),
                new MenuEntry(
                    action: DeleteBoard,
                    title: "Delete board",
                    description: "Delete a board from the scene",
                    entryColor: Color.red.Darker(),
                    icon: Resources.Load<Sprite>("Materials/ModernUIPack/Minus")),
                new MenuEntry(
                    action: AddWidget,
                    title: "Add widget",
                    description: "Add a widget to a board",
                    entryColor: Color.green.Darker(),
                    icon: Resources.Load<Sprite>("Materials/ModernUIPack/Plus")),
                new MenuEntry(
                    action: MoveWidgets,
                    title: "Move widgets",
                    description: "Widgets can be dragged around (or deactivates this)",
                    entryColor: Color.yellow,
                    icon: Resources.Load<Sprite>("Materials/40+ Simple Icons - Free/Scale_Simple_Icons_UI")),
                new MenuEntry(
                    action: RemoveWidget,
                    title: "Remove widget",
                    description: "Remove a widget from a board",
                    entryColor: Color.red.Darker(),
                    icon: Resources.Load<Sprite>("Materials/ModernUIPack/Minus")),
                new MenuEntry(
                    action: SaveBoardConfiguration,
                    title: "Save board",
                    description: "Save a board from the scene to a file",
                    entryColor: Color.blue,
                    icon: Resources.Load<Sprite>("Materials/ModernUIPack/Document")),
                new MenuEntry(
                    action: LoadBoardConfiguration,
                    title: "Load board",
                    description: "Load a board from a file into the scene",
                    entryColor: Color.blue,
                    icon: Resources.Load<Sprite>("Materials/ModernUIPack/Document"))
            };
        }

        /// <summary>
        /// This method repeats the holistic metrics action undone last and closes the menu.
        /// </summary>
        private void Redo()
        {
            menu.ToggleMenu();
            ActionHistory.Redo();
        }
        
        /// <summary>
        /// This method reverts the last holistic metrics action and closes the menu.
        /// </summary>
        private void Undo()
        {
            menu.ToggleMenu();
            ActionHistory.Undo();
        }
        
        /// <summary>
        /// This method gets called when the player clicks on the "New board" button. It will close the menu and open
        /// the dialog for adding a new board to the scene.
        /// </summary>
        private void NewBoard()
        {
            menu.ToggleMenu();
            new AddBoardDialog().Open();
        }
        
        /// <summary>
        /// This method gets called when the player clicks on the "Move boards" button. If there are any boards in the
        /// scene, it will toggle the moving option for the boards which means that there will be orange buttons under
        /// the boards with which the boards can be dragged around. If there are no boards, the player will be notified
        /// about that.
        /// </summary>
        private void MoveBoard()
        {
            menu.ToggleMenu();

            if (BoardsManager.GetNames().Any())
            {
                if (BoardsManager.ToggleMoving())  // If true that means that is is now activated
                {
                    ShowNotification.Info(
                        "Move the boards around as desired",
                        "Click the button under a board to move that board around. Don't forget to " +
                        " deactivate this mode when done.");   
                }
            }
            else
            {
                ShowNotification.Warn(
                    "No boards in the scene",
                    "There are not metrics boards that could be moved.");
            }
        }

        /// <summary>
        /// This method will be called when the player clicks on the "Delete board" button. If there are any metrics
        /// boards in the scene, it will open the dialog for deleting a metrics board. Otherwise it will notify the
        /// player.
        /// </summary>
        private void DeleteBoard()
        {
            menu.ToggleMenu();

            if (BoardsManager.GetNames().Any())
            {
                new DeleteBoardDialog().Open();
            }
            else
            {
                ShowNotification.Warn(
                    "No boards in the scene",
                    "There are no metrics boards in the scene");
            }
        }
        
        /// <summary>
        /// This method will be called when the player clicks on the "Add widget" button. It will check whether there
        /// are any metrics boards in the scene. If so, then it will show the dialog for adding a new widget to a board.
        /// Otherwise, it will notify the player that there are no metrics boards in the scene.
        /// </summary>
        private void AddWidget()
        {
            menu.ToggleMenu();

            if (BoardsManager.GetNames().Any())
            {
                new AddWidgetDialog().Open();
            }
            else
            {
                ShowNotification.Warn(
                    "No boards in the scene",
                    "There are no metrics boards on which you could add a widget");
            }
        }

        /// <summary>
        /// This method will be called when the player clicks on the "Move widgets" button. It toggles that the widgets
        /// can be moved. When the moving has been activated now (not deactivated), a notification will be shown to the
        /// player that contains instructions for how to move widgets.
        /// </summary>
        private void MoveWidgets()
        {
            menu.ToggleMenu();

            if (BoardsManager.ToggleWidgetsMoving())
            {
                ShowNotification.Info(
                    "Click hold widget",
                    "Click and hold on a widget, then move the cursor to move the widget. Don't" +
                    " forget to deactivate this move when done.");
            }
        }

        /// <summary>
        /// This method will be called when the player clicks on the "Remove widget" button. It will check if there are
        /// any boards in the scene and if so, it will add a WidgetDeleter to every widget so it can be clicked and
        /// deleted. Otherwise, it will notify the player.
        /// </summary>
        private void RemoveWidget()
        {
            menu.ToggleMenu();

            if (BoardsManager.GetNames().Any())
            {
                // TODO: Also check if any of the boards has any widgets
                // TODO: Let the player cancel this
                ShowNotification.Info(
                    "Select the widget to delete",
                    "Click on a widget to delete it");
                BoardsManager.AddWidgetDeleters();
            }
            else
            {
                ShowNotification.Warn(
                    "No boards in the scene",
                    "Therefore there cannot be any widgets you could remove");
            }
        }

        /// <summary>
        /// This method gets called when the player clicks on the "Save board" button. It will check whether or not
        /// there are any metrics boards in the scene and if that is true, it will show the dialog for saving a metrics
        /// board. Otherwise, it will notify the player.
        /// </summary>
        private void SaveBoardConfiguration()
        {
            menu.ToggleMenu();

            if (BoardsManager.GetNames().Any())
            {
                new SaveBoardConfigurationDialog().Open();
            }
            else
            {
                ShowNotification.Warn(
                    "No boards in the scene",
                    "There are no metrics boards in the scene");
            }
        }

        /// <summary>
        /// This method will get called when the player clicks on the "Load board" button. It will check if there are
        /// any metrics boards configurations saved in the directory for metrics boards configurations. If there are
        /// any, it will let the player choose one. Otherwise, it will notify the player.
        /// </summary>
        private void LoadBoardConfiguration()
        {
            menu.ToggleMenu();
            
            if (ConfigManager.GetSavedFileNames().Any())
            {
                new LoadBoardConfigurationDialog().Open();
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