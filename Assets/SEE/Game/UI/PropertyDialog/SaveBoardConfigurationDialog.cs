using SEE.Controls;
using SEE.Game.HolisticMetrics;
using UnityEngine;

namespace SEE.Game.UI.PropertyDialog
{
    public class SaveBoardConfigurationDialog
    {
        private GameObject dialog;

        private PropertyDialog propertyDialog;

        private SelectionProperty selectedBoard;

        private StringProperty fileName;

        private readonly BoardsManager boardsManager;

        internal SaveBoardConfigurationDialog(BoardsManager boardsManagerReference)
        {
            boardsManager = boardsManagerReference;
        }

        internal void Open()
        {
            dialog = new GameObject("Save board configuration dialog");
            PropertyGroup group = dialog.AddComponent<PropertyGroup>();
            group.Name = "Save board configuration dialog";

            selectedBoard = dialog.AddComponent<SelectionProperty>();
            selectedBoard.Name = "Select board";
            selectedBoard.Description = "Select the board of which to save the configuration";
            selectedBoard.AddOptions(boardsManager.GetNames());
            selectedBoard.Value = boardsManager.GetNames()[0];
            group.AddProperty(selectedBoard);

            fileName = dialog.AddComponent<StringProperty>();
            fileName.Name = "File name";
            fileName.Description =
                "Enter a file name under which to save the configuration. If the file already exists," +
                "it will be overwritten.";
            group.AddProperty(fileName);
            
            propertyDialog = dialog.AddComponent<PropertyDialog>();
            propertyDialog.Title = "Specify board configuration";
            propertyDialog.Description = "Select the board to save and give the configuration file a name";
            propertyDialog.AddGroup(group);
            
            propertyDialog.OnConfirm.AddListener(SaveBoardConfiguration);
            propertyDialog.OnCancel.AddListener(EnableKeyboardShortcuts);
            
            SEEInput.KeyboardShortcutsEnabled = false;
            propertyDialog.DialogShouldBeShown = true;
        }

        private void SaveBoardConfiguration()
        {
            SEEInput.KeyboardShortcutsEnabled = true;
            
            BoardController boardController = boardsManager.FindControllerByName(selectedBoard.Value);
            ConfigurationManager.SaveBoard(boardController, fileName.Value);
            
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