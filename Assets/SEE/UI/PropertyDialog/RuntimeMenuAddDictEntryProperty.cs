using SEE.Controls;
using SEE.Utils;
using System.Collections;
using UnityEngine;

namespace SEE.UI.PropertyDialog
{
    /// <summary>
    /// This class manages the dialog for adding a new dict entry.
    /// </summary>
    internal class RuntimeMenuAddDictEntryProperty : BasePropertyDialog
    {
        /// <summary>
        /// The key name which the player entered.
        /// </summary>
        private static string key;

        /// <summary>
        /// This is the input field where the player can enter a key name.
        /// </summary>
        private StringProperty selectedName;

        /// <summary>
        /// The city where the new node type should be added.
        /// </summary>
        private readonly IDictionary dict;

        /// <summary>
        /// The constructor of this class.
        /// </summary>
        /// <param name="dict">The dictionary to which an entry should be added.</param>
        public RuntimeMenuAddDictEntryProperty(IDictionary dict)
        {
            this.dict = dict;
        }

        /// <summary>
        /// This method instantiates the dialog and then displays it to the player.
        /// </summary>
        public void Open()
        {
            Dialog = new GameObject("Add entry dialog");
            PropertyGroup group = Dialog.AddComponent<PropertyGroup>();
            group.Name = "Add entry dialog";

            selectedName = Dialog.AddComponent<StringProperty>();
            selectedName.Name = "Key";
            selectedName.Description = "Enter a key for the new entry.";
            group.AddProperty(selectedName);

            // Adds the property dialog to the dialog.
            PropertyDialog = Dialog.AddComponent<PropertyDialog>();
            PropertyDialog.Title = "Add new entry";
            PropertyDialog.Description = "Select a key; then hit the OK button.";
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
                selectedName.ChangeToValidationFailed("The entered key must not be empty.");
            }
            else if (dict.Contains(selectedName.Value))
            {
                selectedName.ChangeToValidationFailed("Key is already in use. Please enter a unique key.");
            }
            else
            {
                SEEInput.KeyboardShortcutsEnabled = true;
                key = selectedName.Value;
                GotInput = true;
                Destroyer.Destroy(Dialog);
            }
        }

        /// <summary>
        /// Fetches the key given by the player.
        /// </summary>
        /// <param name="key">If given and not yet fetched, this will be the key name the player chosen.</param>
        /// <returns>The value of <see cref="BasePropertyDialog.GotInput"/></returns>
        internal bool TryGetKey(out string key)
        {
            if (GotInput)
            {
                key = RuntimeMenuAddDictEntryProperty.key;
                GotInput = false;
                return true;
            }
            key = null;
            return false;
        }
    }
}
