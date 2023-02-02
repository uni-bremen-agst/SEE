using SEE.Controls;
using SEE.Game.HolisticMetrics;
using SEE.Game.HolisticMetrics.Components;
using UnityEngine;

namespace SEE.Game.UI.PropertyDialog.HolisticMetrics
{
    /// <summary>
    /// This class manages the dialog for saving a board from the scene to a configuration file.
    /// </summary>
    internal class SaveBoardDialog : HolisticMetricsDialog
    {
        /// <summary>
        /// The filename the user entered for the file in which the <see cref="BoardConfig"/> will be saved.
        /// </summary>
        private string filename;

        /// <summary>
        /// The <see cref="widgetsManager"/> of the widget to save to disk.
        /// </summary>
        private WidgetsManager widgetsManager;

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
            propertyDialog.OnCancel.AddListener(Cancel);
            
            SEEInput.KeyboardShortcutsEnabled = false;
            propertyDialog.DialogShouldBeShown = true;
        }

        /// <summary>
        /// This method gets called when the player confirms the dialog. It will save the selected board under the given
        /// name and then closes the dialog and re-enables the keyboard shortcuts.
        /// </summary>
        private void SaveBoardConfiguration()
        {
            gotInput = true;
            filename = fileName.Value;
            widgetsManager = BoardsManager.Find(selectedBoard.Value);
            Close();
        }
        
        /// <summary>
        /// Fetches the user input of this dialog.
        /// </summary>
        /// <param name="filenameOut">If the return value is true, this gets assigned the filename the player entered
        /// </param>
        /// <param name="widgetsManagerOut">If the return value is true, this gets assigned the WidgetsManager that the
        /// player selected</param>
        /// <returns>Whether there is a user input present that wasn't already fetched</returns>
        internal bool GetUserInput(out string filenameOut, out WidgetsManager widgetsManagerOut)
        {
            if (gotInput)
            {
                gotInput = false;
                filenameOut = filename;
                widgetsManagerOut = widgetsManager;
                return true;
            }

            filenameOut = null;
            widgetsManagerOut = null;
            return false;
        }
    }
}