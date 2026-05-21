using SEE.Controls;
using SEE.Game.HolisticMetrics;
using SEE.UI.Notification;
using UnityEngine;

namespace SEE.UI.PropertyDialog.HolisticMetrics
{
    /// <summary>
    /// This class manages the dialog for saving a board from the scene to a configuration file.
    /// </summary>
    internal class SaveBoardDialog : BasePropertyDialog
    {
        /// <summary>
        /// The name of the file in which to store the <see cref="BoardConfig"/>.
        /// </summary>
        private string filename;

        /// <summary>
        /// The <see cref="widgetsManager"/> of the board to save to disk.
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
            string[] boardNames = BoardsManager.GetNames();
            if (boardNames.Length == 0)
            {
                ShowNotification.Error("No boards", "There are no boards to save.");
                return;
            }

            Dialog = new GameObject("Save board configuration dialog");
            PropertyGroup group = Dialog.AddComponent<PropertyGroup>();
            group.Name = "Save board configuration dialog";

            selectedBoard = Dialog.AddComponent<SelectionProperty>();
            selectedBoard.Name = "Select board";
            selectedBoard.Description = "Select the board of which to save the configuration.";
            selectedBoard.AddOptions(boardNames);
            selectedBoard.Value = boardNames[0];
            group.AddProperty(selectedBoard);

            fileName = Dialog.AddComponent<StringProperty>();
            fileName.Name = "File name";
            fileName.Description =
                "Enter a file name under which to save the configuration. If the file exists already, " +
                "it will be overwritten.";
            group.AddProperty(fileName);

            PropertyDialog = Dialog.AddComponent<PropertyDialog>();
            PropertyDialog.Title = "Specify board configuration";
            PropertyDialog.Description = "Select the board to save and give the configuration file a name.";
            PropertyDialog.Icon = Resources.Load<Sprite>("Materials/ModernUIPack/Document");
            PropertyDialog.AddGroup(group);

            PropertyDialog.OnConfirm.AddListener(SaveBoardConfiguration);
            PropertyDialog.OnCancel.AddListener(Cancel);

            SEEInput.KeyboardShortcutsEnabled = false;
            PropertyDialog.DialogShouldBeShown = true;
        }

        /// <summary>
        /// This method gets called when the player confirms the dialog. It will
        /// save the name of the selected board in <see cref="filename"/> and then
        /// close the dialog and re-enable the keyboard shortcuts.
        /// </summary>
        /// <remarks>It does not actually save the board. This will be done
        /// by the client.</remarks>
        private void SaveBoardConfiguration()
        {
            GotInput = true;
            filename = fileName.Value;
            widgetsManager = BoardsManager.Find(selectedBoard.Value);
            Close();
        }

        /// <summary>
        /// Fetches the input the player gave us.
        /// </summary>
        /// <param name="filenameOut">If <see cref="BasePropertyDialog.GotInput"/>, this will be the
        /// <see cref="filename"/>. Otherwise null.</param>
        /// <param name="widgetsManagerOut">If <see cref="BasePropertyDialog.GotInput"/>, this will be the
        /// <see cref="widgetsManager"/>. Otherwise null.</param>
        /// <returns><see cref="BasePropertyDialog.GotInput"/>.</returns>
        internal bool GetUserInput(out string filenameOut, out WidgetsManager widgetsManagerOut)
        {
            if (GotInput)
            {
                GotInput = false;
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
