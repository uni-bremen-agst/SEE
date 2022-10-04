using SEE.Controls;
using SEE.Game.HolisticMetrics;
using UnityEngine;

namespace SEE.Game.UI.PropertyDialog
{
    public class DeleteBoardDialog
    {
        private readonly BoardsManager boardsManager;
        
        private GameObject dialog;

        private PropertyDialog propertyDialog;

        private SelectionProperty selectedBoardName;
        
        internal DeleteBoardDialog(BoardsManager boardsManagerReference)
        {
            boardsManager = boardsManagerReference;
        }
        
        internal void Open()
        {
            dialog = new GameObject("Delete board dialog");
            PropertyGroup group = dialog.AddComponent<PropertyGroup>();
            group.Name = "Delete board dialog";

            selectedBoardName = dialog.AddComponent<SelectionProperty>();
            selectedBoardName.Name = "Select board";
            selectedBoardName.Description = "Select the board you want to delete";
            selectedBoardName.AddOptions(boardsManager.GetNames());
            selectedBoardName.Value = boardsManager.GetNames()[0];
            group.AddProperty(selectedBoardName);
            
            propertyDialog = dialog.AddComponent<PropertyDialog>();
            propertyDialog.Title = "Select board";
            propertyDialog.Description = "Select the board you want to delete";
            propertyDialog.AddGroup(group);
            
            propertyDialog.OnConfirm.AddListener(DeleteBoard);
            propertyDialog.OnCancel.AddListener(EnableKeyboardShortcuts);

            SEEInput.KeyboardShortcutsEnabled = false;
            propertyDialog.DialogShouldBeShown = true;
        }

        private void DeleteBoard()
        {
            SEEInput.KeyboardShortcutsEnabled = true;
            
            // Delete the board
            boardsManager.Delete(selectedBoardName.Value);

            // Destroy the dialog GameObject
            Object.Destroy(dialog);
        }
        
        private void EnableKeyboardShortcuts()
        {
            // Destroy the dialog GameObject
            Object.Destroy(dialog);
            
            SEEInput.KeyboardShortcutsEnabled = true;
        }
    }
}