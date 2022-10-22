using SEE.Controls;
using SEE.Game.HolisticMetrics;
using SEE.Game.UI.Notification;
using SEE.Game.HolisticMetrics.Components;
using UnityEngine;

namespace SEE.Game.UI.PropertyDialog.HolisticMetrics
{
    /// <summary>
    /// This class manages the dialog for adding a metrics board to the scene.
    /// </summary>
    internal class AddBoardDialog
    {
        /// <summary>
        /// The dialog GameObject.
        /// </summary>
        private GameObject dialog;

        /// <summary>
        /// The property dialog.
        /// </summary>
        private PropertyDialog propertyDialog;

        /// <summary>
        /// The input field where the player can enter a name for the new board.
        /// </summary>
        private StringProperty boardName;

        /// <summary>
        /// This method instantiates and then displays the dialog to the player.
        /// </summary>
        internal void Open()
        {
            dialog = new GameObject("Add board dialog");
            PropertyGroup group = dialog.AddComponent<PropertyGroup>();
            group.Name = "Add board dialog";

            boardName = dialog.AddComponent<StringProperty>();
            boardName.Name = "Enter name (unique)";
            boardName.Description = "Enter the title of the board. This has to be unique.";
            group.AddProperty(boardName);
            
            propertyDialog = dialog.AddComponent<PropertyDialog>();
            propertyDialog.Title = "Add board";
            propertyDialog.Description = "Configure the board; then hit OK button.";
            propertyDialog.Icon = Resources.Load<Sprite>("Materials/ModernUIPack/Plus");
            propertyDialog.AddGroup(group);

            propertyDialog.OnConfirm.AddListener(AddBoard);
            propertyDialog.OnCancel.AddListener(EnableKeyboardShortcuts);

            SEEInput.KeyboardShortcutsEnabled = false;
            propertyDialog.DialogShouldBeShown = true;
        }

        /// <summary>
        /// This method gets called when the player confirms the configuration. This method puts the name the player
        /// entered into a new board configuration and then passes is on to the BoardAdder that it will attach to the
        /// floor. That BoardAdder will wait for the player to click on the ground and then proceed with creating the
        /// board.
        /// </summary>
        private void AddBoard()
        {
            BoardConfig boardConfiguration = new BoardConfig()
            {
                Title = boardName.Value
            };
            Object.Destroy(dialog);
            SEEInput.KeyboardShortcutsEnabled = true;
            GameObject.Find("/DemoWorld/Plane").AddComponent<BoardAdder>();
            BoardAdder.Setup(boardConfiguration);

            ShowNotification.Info(
                "Position the board",
                "Left click on the ground where you want to add the board");
        }

        /// <summary>
        /// This method will be called when the player cancels or confirms the dialog. It will close the dialog and
        /// reenable the keyboard shortcuts.
        /// </summary>
        private void EnableKeyboardShortcuts()
        {
            // Destroy the dialog GameObject
            Object.Destroy(dialog);

            SEEInput.KeyboardShortcutsEnabled = true;
        }
    }
}