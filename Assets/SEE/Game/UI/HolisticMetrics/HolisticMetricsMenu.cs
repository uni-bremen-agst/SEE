using System.Collections.Generic;
using SEE.Controls;
using SEE.Game.UI.Menu;
using SEE.Game.UI.Notification;
using UnityEngine;

namespace SEE.Game.UI.HolisticMetrics
{
    /// <summary>
    /// The content of this class is inspired by OpeningDialog.cs because I think it makes sense to implement the menu
    /// like other menus in the SEE project; I don't think it would help to reinvent the wheel here.
    /// </summary>
    public class HolisticMetricsMenu : MonoBehaviour
    {
        private SimpleMenu menu;

        private void Start()
        {
            menu = CreateMenu();
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
                    description: "Add a new metrics board to the scene")
            };
        }

        private static void NewBoard()
        {
            // Refer to OpeningDialog Settings() !!!!!
            
            // First get the position where the player wants to position the new board
            
            // When the player has clicked on the ground, show him a dialog where he can rotate the board with a slider
            // and where he can enter a name for the board and where he can click ok to add the board.
            ShowNotification.Warn(
                "Could not add the board", 
                "Reason: A board with that name already exists in the scene");
        }
    }
}