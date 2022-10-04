using System;
using SEE.Controls;
using SEE.Game.HolisticMetrics;
using SEE.Game.UI.Notification;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SEE.Game.UI.PropertyDialog
{
    internal class LoadBoardConfigurationDialog
    {
        private GameObject dialog;

        private PropertyDialog propertyDialog;

        private SelectionProperty selectedFile;

        private ButtonProperty buttonProperty;

        private readonly BoardsManager boardsManager;

        public LoadBoardConfigurationDialog(BoardsManager boardsManagerReference)
        {
            boardsManager = boardsManagerReference;
        }

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
            propertyDialog.AddGroup(group);

            propertyDialog.OnConfirm.AddListener(LoadBoardConfiguration);
            propertyDialog.OnCancel.AddListener(EnableKeyboardShortcuts);

            SEEInput.KeyboardShortcutsEnabled = false;
            propertyDialog.DialogShouldBeShown = true;
        }

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
            boardsManager.CreateNewBoard(boardConfiguration);
        }

        private void EnableKeyboardShortcuts()
        {
            // Destroy the dialog GameObject
            Object.Destroy(dialog);
            
            SEEInput.KeyboardShortcutsEnabled = true;
        }
    }
}