using SEE.Controls;
using UnityEngine;

namespace SEE.Game.UI.PropertyDialog.HolisticMetrics
{
    /// <summary>
    /// This class manages the dialog for adding a metrics board to the scene.
    /// </summary>
    internal class AddBoardDialog : HolisticMetricsDialog
    {
        /// <summary>
        /// The input field where the player can enter a name for the new board.
        /// </summary>
        private StringProperty boardName;

        /// <summary>
        /// This method instantiates and then displays the dialog to the player.
        /// </summary>
        internal void Open()
        {
            gotInput = false;

            dialog = new GameObject("Add board dialog");
            PropertyGroup group = dialog.AddComponent<PropertyGroup>();
            group.Name = "Add board dialog";

            boardName = dialog.AddComponent<StringProperty>();
            boardName.Name = "Enter name (unique)";
            boardName.Description = "Enter the title of the board. This has to be unique.";
            group.AddProperty(boardName);
            group.GetReady();

            propertyDialog = dialog.AddComponent<PropertyDialog>();
            propertyDialog.Title = "Add board";
            propertyDialog.Description = "Configure the board; then hit OK button.";
            propertyDialog.Icon = Resources.Load<Sprite>("Materials/ModernUIPack/Plus");
            propertyDialog.AddGroup(group);

            propertyDialog.OnConfirm.AddListener(AddBoard);
            propertyDialog.OnCancel.AddListener(Cancel);

            SEEInput.KeyboardShortcutsEnabled = false;
            propertyDialog.DialogShouldBeShown = true;
        }

        /// <summary>
        /// This method gets called when the player confirms the configuration. This method puts the name the player
        /// entered into a new board configuration and then passes is on to the BoardAdder that it will attach to the
        /// floor. That BoardAdder will wait for the player to click on the ground and then proceed with creating the
        /// board.
        /// </summary>
        private void AddBoard()
        {
            gotInput = true;
            Close();
        }

        /// <summary>
        /// Fetches the name given by the player.
        /// </summary>
        /// <param name="name">The name given by the player, if present and not yet fetched. Otherwise a dummy value.
        /// </param>
        /// <returns>The value of <see cref="HolisticMetricsDialog.gotInput"/></returns>
        internal bool TryGetName(out string name)
        {
            if (gotInput)
            {
                name = boardName.Value;
                return true;
            }

            name = null;
            return false;
        }
    }
}
