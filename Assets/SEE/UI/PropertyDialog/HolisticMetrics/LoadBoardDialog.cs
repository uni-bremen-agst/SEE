using SEE.Controls;
using SEE.Game.HolisticMetrics;
using UnityEngine;
using SEE.Utils;
using SEE.UI.Notification;

namespace SEE.UI.PropertyDialog.HolisticMetrics
{
    /// <summary>
    /// This class manages the dialog for loading a board from a configuration file.
    /// </summary>
    internal class LoadBoardDialog : HolisticMetricsDialog
    {
        /// <summary>
        /// The name of the file which the player selected.
        /// </summary>
        private static string filename;

        /// <summary>
        /// This input field lets the player pick a file from which to load the board configuration.
        /// </summary>
        private SelectionProperty selectedFile;

        /// <summary>
        /// This method instantiates the dialog and then displays it to the player.
        /// </summary>
        public void Open()
        {
            string[] savedFileNames = ConfigManager.GetSavedFileNames();
            if (savedFileNames.Length == 0)
            {
                ShowNotification.Error("No saved boards", "There are no saved files to load a board from.");
                return;
            }

            const string title = "Select file";
            const string description = "Select the file to load the board configuration from.";

            Dialog = new GameObject("Load board configuration dialog");
            PropertyGroup group = Dialog.AddComponent<PropertyGroup>();
            group.Name = "Load board configuration dialog";

            selectedFile = Dialog.AddComponent<SelectionProperty>();
            selectedFile.Name = title;
            selectedFile.Description = description;
            selectedFile.AddOptions(savedFileNames);
            selectedFile.Value = savedFileNames[0];
            group.AddProperty(selectedFile);

            PropertyDialog = Dialog.AddComponent<PropertyDialog>();
            PropertyDialog.Title = title;
            PropertyDialog.Description = description;
            PropertyDialog.Icon = Resources.Load<Sprite>("Materials/ModernUIPack/Document");
            PropertyDialog.AddGroup(group);

            PropertyDialog.OnConfirm.AddListener(OnConfirm);
            PropertyDialog.OnCancel.AddListener(Cancel);

            SEEInput.KeyboardShortcutsEnabled = false;
            PropertyDialog.DialogShouldBeShown = true;
        }

        /// <summary>
        /// This method gets called when the player confirms the dialog. It will save the selected filename in a
        /// variable and set <see cref="HolisticMetricsDialog.GotInput"/> to true.
        /// </summary>
        private void OnConfirm()
        {
            SEEInput.KeyboardShortcutsEnabled = true;
            filename = selectedFile.Value;
            GotInput = true;
            Destroyer.Destroy(Dialog);
        }

        /// <summary>
        /// Fetches the filename given by the player.
        /// </summary>
        /// <param name="nameOfFile">If given and not yet fetched, this will be the filename the player selected.
        /// </param>
        /// <returns>The value of <see cref="HolisticMetricsDialog.GotInput"/></returns>
        internal bool TryGetFilename(out string nameOfFile)
        {
            if (GotInput)
            {
                nameOfFile = filename;
                GotInput = false;
                return true;
            }

            nameOfFile = null;
            return false;
        }
    }
}
