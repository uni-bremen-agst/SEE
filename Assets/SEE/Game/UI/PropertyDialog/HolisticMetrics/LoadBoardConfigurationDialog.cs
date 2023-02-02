using SEE.Controls;
using SEE.Game.HolisticMetrics;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SEE.Game.UI.PropertyDialog.HolisticMetrics
{
    /// <summary>
    /// This class manages the dialog for loading a board from a configuration file.
    /// </summary>
    internal class LoadBoardConfigurationDialog : HolisticMetricsDialog
    {
        /// <summary>
        /// Whether this dialog has a filename in store that hasn't yet been fetched.
        /// </summary>
        private static bool gotFilename;

        /// <summary>
        /// The name of the file which the player selected.
        /// </summary>
        private static string filename;

        /// <summary>
        /// Whether this dialog was canceled.
        /// </summary>
        private static bool wasCanceled;
        
        /// <summary>
        /// The property dialog.
        /// </summary>
        private PropertyDialog propertyDialog;

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
        /// variable and set <see cref="gotFilename"/> to true.
        /// </summary>
        private void OnConfirm()
        {
            SEEInput.KeyboardShortcutsEnabled = true;
            filename = selectedFile.Value;
            gotFilename = true;
            Object.Destroy(dialog);
        }

        /// <summary>
        /// Gets called when the dialog is canceled.
        /// </summary>
        private void Cancel()
        {
            base.EnableKeyboardShortcuts();
            wasCanceled = true;
        }

        internal static bool GetFilename(out string nameOfFile)
        {
            if (gotFilename)
            {
                nameOfFile = filename;
                gotFilename = false;
                return true;
            }

            nameOfFile = null;
            return false;
        }

        /// <summary>
        /// Whether this dialog was canceled.
        /// </summary>
        /// <returns>Whether this dialog was canceled</returns>
        internal static bool WasCanceled()
        {
            if (wasCanceled)
            {
                wasCanceled = false;
                return true;
            }

            return false;
        }
    }
}