using System;
using SEE.Controls;
using SEE.Game.HolisticMetrics;
using SEE.Game.UI.Notification;
using SEE.Net.Actions.HolisticMetrics;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SEE.Game.UI.PropertyDialog.HolisticMetrics
{
    /// <summary>
    /// This class manages the dialog for loading a board from a configuration file.
    /// </summary>
    internal class LoadBoardConfigurationDialog
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
            selectedFile.AddOptions(ConfigurationManager.GetSavedFileNames());
            selectedFile.Value = ConfigurationManager.GetSavedFileNames()[0];
            group.AddProperty(selectedFile);

            propertyDialog = dialog.AddComponent<PropertyDialog>();
            propertyDialog.Title = "Select file";
            propertyDialog.Description = "Select file to load the board configuration from";
            propertyDialog.Icon = Resources.Load<Sprite>("Materials/ModernUIPack/Document");
            propertyDialog.AddGroup(group);

            propertyDialog.OnConfirm.AddListener(LoadBoardConfiguration);
            propertyDialog.OnCancel.AddListener(EnableKeyboardShortcuts);

            SEEInput.KeyboardShortcutsEnabled = false;
            propertyDialog.DialogShouldBeShown = true;
        }

        /// <summary>
        /// This method gets called when the player confirms the dialog. It will load the selected board and create it
        /// in the scene.
        /// </summary>
        private void LoadBoardConfiguration()
        {
            SEEInput.KeyboardShortcutsEnabled = true;
            
            // Load the board configuration from the file
            BoardConfiguration boardConfiguration;
            try
            {
                boardConfiguration = ConfigurationManager.LoadBoard(selectedFile.Value);
            }
            catch (Exception exception)
            {
                ShowNotification.Error(
                    "Problem loading the metrics board configuration, reason:", 
                    exception.Message);
                return;
            }
            
            // Destroy the dialog GameObject
            Object.Destroy(dialog);
            
            // Create a new board from the loaded configuration
            new CreateBoardNetAction(boardConfiguration).Execute();
        }

        /// <summary>
        /// This method will be called when the player confirms or cancels the dialog. It will reenable the keyboard
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