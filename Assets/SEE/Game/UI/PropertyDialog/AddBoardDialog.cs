using SEE.Controls;
using SEE.Game.HolisticMetrics;
using SEE.Game.UI.Notification;
using UnityEngine;

namespace SEE.Game.UI.PropertyDialog
{
    internal class AddBoardDialog
    {
        private GameObject dialog;

        private PropertyDialog propertyDialog;

        private StringProperty boardName;

        private static GameObject sliderPrefab;

        internal void Open()
        {
            dialog = new GameObject("Add board dialog");
            PropertyGroup group = dialog.AddComponent<PropertyGroup>();
            group.Name = "Add board dialog";

            boardName = dialog.AddComponent<StringProperty>();
            boardName.Name = "Enter name (unique)";
            boardName.Description = "Enter the title of the board. This has to be unique.";
            group.AddProperty(boardName);
            
            propertyDialog = dialog.AddComponent<PropertyDialog>();
            propertyDialog.Title = "Add board";
            propertyDialog.Description = "Configure the board; then hit OK button.";
            propertyDialog.AddGroup(group);

            propertyDialog.OnConfirm.AddListener(AddBoard);
            propertyDialog.OnCancel.AddListener(EnableKeyboardShortcuts);

            SEEInput.KeyboardShortcutsEnabled = false;
            propertyDialog.DialogShouldBeShown = true;
        }

        private void AddBoard()
        {
            BoardConfiguration boardConfiguration = new BoardConfiguration()
            {
                Title = boardName.Value
            };
            Object.Destroy(dialog);
            SEEInput.KeyboardShortcutsEnabled = true;
            GameObject.Find("/DemoWorld/Plane").AddComponent<BoardAdder>();
            BoardAdder.Setup(boardConfiguration);

            ShowNotification.Info(
                "Position the board",
                "Left click on the ground where you want to add the board");
        }

        private void EnableKeyboardShortcuts()
        {
            // Destroy the dialog GameObject
            Object.Destroy(dialog);

            SEEInput.KeyboardShortcutsEnabled = true;
        }
    }
}