using SEE.Controls;
using SEE.Game.City;
using SEE.Utils;
using UnityEngine;

namespace SEE.UI.PropertyDialog
{
    /// <summary>
    /// This class manages the dialog for adding a new node type.
    /// </summary>
    internal class AddNodeTypeProperty : BasePropertyDialog
    {
        /// <summary>
        /// The node type name which the player entered.
        /// </summary>
        private static string name;

        /// <summary>
        /// This input field where the player can enter a node type name.
        /// </summary>
        private StringProperty selectedName;

        /// <summary>
        /// The city where the new node type should be added.
        /// </summary>
        private AbstractSEECity city;

        /// <summary>
        /// This method instantiates the dialog and then displays it to the player.
        /// </summary>
        /// <param name="city">The city where the new node type should be added.</param>
        public void Open(AbstractSEECity city)
        {
            this.city = city;
            Dialog = new GameObject("Add node type dialog");
            PropertyGroup group = Dialog.AddComponent<PropertyGroup>();
            group.Name = "Add node type dialog";

            selectedName = Dialog.AddComponent<StringProperty>();
            selectedName.Name = "Node Type Name";
            selectedName.Description = "Enter a name for the new node type.";
            group.AddProperty(selectedName);

            // Adds the property dialog to the dialog.
            PropertyDialog = Dialog.AddComponent<PropertyDialog>();
            PropertyDialog.Title = "Add new node type";
            PropertyDialog.Description = "Select a node type name; then hit the OK button.";
            PropertyDialog.Icon = Resources.Load<Sprite>("Materials/ModernUIPack/Picker");
            PropertyDialog.AddGroup(group);

            PropertyDialog.OnConfirm.AddListener(OnConfirm);
            PropertyDialog.OnCancel.AddListener(Cancel);
            // Prevents the dialog from closing automatically upon confirmation.
            PropertyDialog.AllowClosing(false);

            SEEInput.KeyboardShortcutsEnabled = false;
            PropertyDialog.DialogShouldBeShown = true;
        }

        /// <summary>
        /// This method gets called when the player confirms the dialog. It will save the selected name in a
        /// variable and set <see cref="BasePropertyDialog.GotInput"/> to true.
        /// </summary>
        private void OnConfirm()
        {
            if (string.IsNullOrEmpty(selectedName.Value.Trim()))
            {
                selectedName.ChangeToValidationFailed("The entered node type name must not be empty.");
            }
            else if (city.NodeTypes.TryGetValue(selectedName.Value, out VisualNodeAttributes _))
            {
                selectedName.ChangeToValidationFailed("Name is already in use. Please enter a unique node type name.");
            }
            else
            {
                SEEInput.KeyboardShortcutsEnabled = true;
                name = selectedName.Value;
                GotInput = true;
                Destroyer.Destroy(Dialog);
            }
        }

        /// <summary>
        /// Fetches the node type name given by the player.
        /// </summary>
        /// <param name="nodeTypeName">If given and not yet fetched, this will be the node type name the player chosen.</param>
        /// <returns>The value of <see cref="BasePropertyDialog.GotInput"/></returns>
        internal bool TryGetNodeTypeName(out string nodeTypeName)
        {
            if (GotInput)
            {
                nodeTypeName = name;
                GotInput = false;
                return true;
            }
            nodeTypeName = null;
            return false;
        }
    }
}
