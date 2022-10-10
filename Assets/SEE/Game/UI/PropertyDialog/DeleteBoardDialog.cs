using SEE.Controls;
using SEE.Game.HolisticMetrics;
using UnityEngine;

namespace SEE.Game.UI.PropertyDialog
{
    public class DeleteBoardDialog
    {
        
        private GameObject dialog;

        private PropertyDialog propertyDialog;

        private SelectionProperty selectedBoardName;
        
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
            BoardsManager.Delete(selectedBoardName.Value);

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