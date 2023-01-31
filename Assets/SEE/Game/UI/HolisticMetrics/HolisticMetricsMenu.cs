using System.Collections.Generic;
using System.Linq;
using SEE.Game.HolisticMetrics;
using SEE.Game.UI.Menu;
using SEE.Game.UI.Notification;
using SEE.Game.UI.PropertyDialog.HolisticMetrics;
using SEE.Utils;
using UnityEngine;

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
        /// This method will be called when the player clicks on the "Delete board" button. If there are any metrics
        /// boards in the scene, it will open the dialog for deleting a metrics board. Otherwise it will notify the
        /// player.
        /// </summary>
        private void DeleteBoard()
        {
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
        /// This method will be called when the player clicks on the "Remove widget" button. It will check if there are
        /// any boards in the scene and if so, it will add a WidgetDeleter to every widget so it can be clicked and
        /// deleted. Otherwise, it will notify the player.
        /// </summary>
        private void RemoveWidget()
        {
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