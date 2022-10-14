using SEE.Controls;
using SEE.Game.HolisticMetrics;
using SEE.Game.HolisticMetrics.Components;
using UnityEngine;

namespace SEE.Game.UI.PropertyDialog.HolisticMetrics
{
    /// <summary>
    /// This class manages the dialog for saving a board from the scene to a configuration file.
    /// </summary>
    public class SaveBoardConfigurationDialog
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
        /// The input field that lets the player select a board from the scene to be saved.
        /// </summary>
        private SelectionProperty selectedBoard;

        /// <summary>
        /// The input field that lets the player enter a name for the file in which to save the board configuration.
        /// </summary>
        private StringProperty fileName;

        /// <summary>
        /// This method instantiates the dialog and then displays it to the player.
        /// </summary>
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

        /// <summary>
        /// This method gets called when the player confirms the dialog. It will save the selected board under the given
        /// name and then closes the dialog and reenables the keyboard shortcuts.
        /// </summary>
        private void SaveBoardConfiguration()
        {
            WidgetsManager boardsManager = BoardsManager.GetWidgetsManager(selectedBoard.Value);
            ConfigurationManager.SaveBoard(boardsManager, fileName.Value);
            EnableKeyboardShortcuts();
        }

        /// <summary>
        /// This method gets called when the player confirms or cancels the dialog. It will reenable the keyboard
        /// shortcuts and close the dialog.
        /// </summary>
        private void EnableKeyboardShortcuts()
        {
            // Destroy the dialog GameObject
            Object.Destroy(dialog);
            
            SEEInput.KeyboardShortcutsEnabled = true;
        }
    }
}