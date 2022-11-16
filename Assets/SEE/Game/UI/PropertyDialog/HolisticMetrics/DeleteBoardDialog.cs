using SEE.Controls;
using SEE.Controls.Actions.HolisticMetrics;
using SEE.Game.HolisticMetrics;
using UnityEngine;

namespace SEE.Game.UI.PropertyDialog.HolisticMetrics
{
    /// <summary>
    /// This class implements a property dialog that lets the player select a board that they want to delete.
    /// </summary>
    internal class DeleteBoardDialog : HolisticMetricsDialog
    {
        /// <summary>
        /// The property dialog of this dialog.
        /// </summary>
        private PropertyDialog propertyDialog;

        /// <summary>
        /// The selection of which board is supposed to be deleted.
        /// </summary>
        private SelectionProperty selectedBoardName;
        
        /// <summary>
        /// Displays this dialog to the player.
        /// </summary>
        internal void Open()
        {
            dialog = new GameObject("Delete board dialog");
            PropertyGroup group = dialog.AddComponent<PropertyGroup>();
            group.Name = "Delete board dialog";

            selectedBoardName = dialog.AddComponent<SelectionProperty>();
            selectedBoardName.Name = "Select board";
            selectedBoardName.Description = "Select the board you want to delete";
            selectedBoardName.AddOptions(BoardsManager.GetNames());
            selectedBoardName.Value = BoardsManager.GetNames()[0];
            group.AddProperty(selectedBoardName);
            
            propertyDialog = dialog.AddComponent<PropertyDialog>();
            propertyDialog.Title = "Select board";
            propertyDialog.Description = "Select the board you want to delete";
            propertyDialog.Icon = Resources.Load<Sprite>("Materials/ModernUIPack/Minus");
            propertyDialog.AddGroup(group);
            
            propertyDialog.OnConfirm.AddListener(DeleteBoard);
            propertyDialog.OnCancel.AddListener(EnableKeyboardShortcuts);

            SEEInput.KeyboardShortcutsEnabled = false;
            propertyDialog.DialogShouldBeShown = true;
        }

        /// <summary>
        /// This method will be executed when the player confirms their selection. It will delete the board and close
        /// the dialog.
        /// </summary>
        private void DeleteBoard()
        {
            // Delete the board
            new DeleteBoardAction(selectedBoardName.Value).Execute();
            
            EnableKeyboardShortcuts();
        }
    }
}