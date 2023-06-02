using SEE.Controls;
using SEE.Game.HolisticMetrics;
using UnityEngine;
using SEE.Utils;

namespace SEE.Game.UI.PropertyDialog.HolisticMetrics
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
            dialog = new GameObject("Load board configuration dialog");
            PropertyGroup group = dialog.AddComponent<PropertyGroup>();
            group.Name = "Load board configuration dialog";

            selectedFile = dialog.AddComponent<SelectionProperty>();
            selectedFile.Name = "Select file";
            selectedFile.Description = "Select the file to load the board configuration from";
            selectedFile.AddOptions(ConfigManager.GetSavedFileNames());
            selectedFile.Value = ConfigManager.GetSavedFileNames()[0];
            group.AddProperty(selectedFile);

            propertyDialog = dialog.AddComponent<PropertyDialog>();
            propertyDialog.Title = "Select file";
            propertyDialog.Description = "Select file to load the board configuration from";
            propertyDialog.Icon = Resources.Load<Sprite>("Materials/ModernUIPack/Document");
            propertyDialog.AddGroup(group);

            propertyDialog.OnConfirm.AddListener(OnConfirm);
            propertyDialog.OnCancel.AddListener(Cancel);

            SEEInput.KeyboardShortcutsEnabled = false;
            propertyDialog.DialogShouldBeShown = true;
        }

        /// <summary>
        /// This method gets called when the player confirms the dialog. It will save the selected filename in a
        /// variable and set <see cref="HolisticMetricsDialog.gotInput"/> to true.
        /// </summary>
        private void OnConfirm()
        {
            SEEInput.KeyboardShortcutsEnabled = true;
            filename = selectedFile.Value;
            gotInput = true;
            Destroyer.Destroy(dialog);
        }

        /// <summary>
        /// Fetches the filename given by the player.
        /// </summary>
        /// <param name="nameOfFile">If given and not yet fetched, this will be the filename the player selected.
        /// </param>
        /// <returns>The value of <see cref="HolisticMetricsDialog.gotInput"/></returns>
        internal bool TryGetFilename(out string nameOfFile)
        {
            if (gotInput)
            {
                nameOfFile = filename;
                gotInput = false;
                return true;
            }

            nameOfFile = null;
            return false;
        }
    }
}
