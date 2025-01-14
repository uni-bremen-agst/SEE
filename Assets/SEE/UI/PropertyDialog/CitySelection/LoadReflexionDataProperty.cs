using SEE.Controls;
using SEE.Game.City;
using SEE.UI.PropertyDialog.HolisticMetrics;
using SEE.Utils;
using SEE.Utils.Paths;
using System;
using UnityEngine;

namespace SEE.UI.PropertyDialog.CitySelection
{
    /// <summary>
    /// This class manages the dialog for loading the data for a reflexion city.
    /// </summary>
    internal class LoadReflexionDataProperty : HolisticMetricsDialog
    {
        /// <summary>
        /// The data GXL which the player selected.
        /// </summary>
        private static string dataGXL;

        /// <summary>
        /// The implementation project folder.
        /// </summary>
        private static string implProjectFolder;

        /// <summary>
        /// This file picker lets the player pick an data GXL.
        /// </summary>
        private FilePathProperty selectedGXL;

        /// <summary>
        /// The file picker lets the player pick the project folder of the implementation GXL.
        /// </summary>
        private FilePathProperty selectedProject;

        /// <summary>
        /// The input types
        /// </summary>
        private enum InputType
        {
            NULL,
            Architecture,
            Implementation
        }

        /// <summary>
        /// The selected input type.
        /// </summary>
        private InputType inputType = InputType.NULL;

        /// <summary>
        /// This method instantiates the dialog and then displays it to the player.
        /// </summary>
        /// <param name="openForImplementation">If the dialog is intended to be opened
        /// for selecting the implementation data.</param>
        public void Open(bool openForImplementation = true)
        {
            string type = openForImplementation ? "implementation" : "architecture";
            string upperType = char.ToUpper(type[0]) + type.Substring(1);
            Dialog = new GameObject($"Load {type} dialog");
            PropertyGroup group = Dialog.AddComponent<PropertyGroup>();
            group.Name = $"Load {type} dialog";

            selectedGXL = Dialog.AddComponent<FilePathProperty>();
            selectedGXL.Name = $"{upperType} GXL.";
            selectedGXL.Description = $"The {type} GXL.";
            selectedGXL.PickMode = SimpleFileBrowser.FileBrowser.PickMode.Files;
            selectedGXL.FallbackDirectory = Application.streamingAssetsPath;
            selectedGXL.Value = Application.streamingAssetsPath + "/reflexion/";
            group.AddProperty(selectedGXL);

            if (openForImplementation)
            {
                selectedProject = Dialog.AddComponent<FilePathProperty>();
                selectedProject.Name = "Project Folder.";
                selectedProject.Description = "The project folder of the implementation GXL.";
                selectedProject.PickMode = SimpleFileBrowser.FileBrowser.PickMode.Folders;
                selectedProject.FallbackDirectory = Application.streamingAssetsPath;
                selectedProject.Value = Application.streamingAssetsPath + "/reflexion/";
                group.AddProperty(selectedProject);
            }

            /// Adds the property dialog to the dialog.
            PropertyDialog = Dialog.AddComponent<PropertyDialog>();
            PropertyDialog.Title = $"Load the {type}";
            PropertyDialog.Description = openForImplementation
                ? $"Select an {type} GXL and the project folder; then hit the OK button."
                : $"Select an {type} GXL; then hit the OK button.";
            PropertyDialog.Icon = Resources.Load<Sprite>("Materials/ModernUIPack/Picker");
            PropertyDialog.AddGroup(group);

            PropertyDialog.OnConfirm.AddListener(OnConfirm);
            PropertyDialog.OnCancel.AddListener(Cancel);

            SEEInput.KeyboardShortcutsEnabled = false;
            PropertyDialog.DialogShouldBeShown = true;
        }

        /// <summary>
        /// This method gets called when the player confirms the dialog. It will save the selected city type in a
        /// variable and set <see cref="HolisticMetricsDialog.GotInput"/> to true.
        /// </summary>
        private void OnConfirm()
        {
            SEEInput.KeyboardShortcutsEnabled = true;
            dataGXL = selectedGXL.Value;
            if (selectedProject != null)
            {
                implProjectFolder = selectedProject.Value;
                inputType = InputType.Implementation;
            } else
            {
                inputType = InputType.Architecture;
            }
            GotInput = true;
            Destroyer.Destroy(Dialog);
        }

        /// <summary>
        /// Fetches the implementation data given by the player.
        /// </summary>
        /// <param name="gxl">The fetched gxl as data path.</param>
        /// <param name="projectFolder">The fetched project folder as data path.</param>
        /// <returns>The value of <see cref="HolisticMetricsDialog.GotInput"/></returns>
        internal bool TryGetImplementationDataPaths(out DataPath gxl, out DataPath projectFolder)
        {
            if (GotInput && inputType == InputType.Implementation)
            {
                gxl = new(dataGXL);
                projectFolder = new(implProjectFolder);
                GotInput = false;
                return true;
            }
            gxl = null;
            projectFolder = null;
            return false;
        }

        /// <summary>
        /// Fetches the architecture data given by the player.
        /// </summary>
        /// <param name="gxl">The fetched gxl as data path.</param>
        /// <returns>The value of <see cref="HolisticMetricsDialog.GotInput"/></returns>
        internal bool TryGetArchitectureDataPath(out DataPath gxl)
        {
            if (GotInput && inputType == InputType.Architecture)
            {
                gxl = new(dataGXL);
                GotInput = false;
                return true;
            }
            gxl = null;
            return false;
        }

        /// <summary>
        /// Indicator of whether input data is available
        /// or if the dialog has been canceled.
        /// </summary>
        /// <returns>True iff no input is available and the dialog was not canceled.</returns>
        internal bool WaitForInputOrCancel()
        {
            bool isCanceled = WasCanceled();
            bool waiting = !GotInput && !isCanceled;
            return waiting;
        }
    }
}
