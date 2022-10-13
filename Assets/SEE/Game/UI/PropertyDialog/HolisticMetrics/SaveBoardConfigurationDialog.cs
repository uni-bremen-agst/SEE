using SEE.Controls;
using SEE.Game.HolisticMetrics;
using SEE.Game.HolisticMetrics.Components;
using UnityEngine;

namespace SEE.Game.UI.PropertyDialog.HolisticMetrics
{
    public class SaveBoardConfigurationDialog
    {
        private GameObject dialog;

        private PropertyDialog propertyDialog;

        private SelectionProperty selectedBoard;

        private StringProperty fileName;

        internal void Open()
        {
            dialog = new GameObject("Save board configuration dialog");
            PropertyGroup group = dialog.AddComponent<PropertyGroup>();
            group.Name = "Save board configuration dialog";

            selectedBoard = dialog.AddComponent<SelectionProperty>();
            selectedBoard.Name = "Select board";
            selectedBoard.Description = "Select the board of which to save the configuration";
            selectedBoard.AddOptions(BoardsManager.GetNames());
            selectedBoard.Value = BoardsManager.GetNames()[0];
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
            propertyDialog.Icon = Resources.Load<Sprite>("Materials/ModernUIPack/Document");
            propertyDialog.AddGroup(group);
            
            propertyDialog.OnConfirm.AddListener(SaveBoardConfiguration);
            propertyDialog.OnCancel.AddListener(EnableKeyboardShortcuts);
            
            SEEInput.KeyboardShortcutsEnabled = false;
            propertyDialog.DialogShouldBeShown = true;
        }

        private void SaveBoardConfiguration()
        {
            SEEInput.KeyboardShortcutsEnabled = true;
            
            WidgetsManager boardsManager = BoardsManager.GetWidgetsManager(selectedBoard.Value);
            ConfigurationManager.SaveBoard(boardsManager, fileName.Value);
            
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