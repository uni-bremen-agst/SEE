using SEE.Controls;
using UnityEngine;
using UnityEngine.Events;

namespace SEE.UI.PropertyDialog.Drawable
{
    /// <summary>
    /// This class manages the dialog for adding/editing a text drawable type.
    /// </summary>
    internal class WriteEditTextDialog : BasePropertyDialog
    {
        /// <summary>
        /// The text provided in the dialog.
        /// </summary>
        private string text = "";

        /// <summary>
        /// The input field where the player can enter a text for a drawable text type.
        /// </summary>
        private StringProperty textProperty;

        /// <summary>
        /// The action to be executed when the text has been edited.
        /// </summary>
        private UnityAction<string> stringAction;

        /// <summary>
        /// This method instantiates and then displays the dialog to the player.
        /// </summary>
        internal void Open(UnityAction<string> stringAction = null)
        {
            /// Sets the string action, if it's not null.
            if (stringAction != null)
            {
                this.stringAction = stringAction;
            }

            /// Creates the dialog.
            Dialog = new GameObject("Write/Edit Text dialog");
            PropertyGroup group = Dialog.AddComponent<PropertyGroup>();
            group.Name = "Write/Edit Text dialog";

            /// Adds a text property to the dialog.
            textProperty = Dialog.AddComponent<StringProperty>();
            textProperty.Name = "Text";
            textProperty.Description = "Write or edit the text that should be transmitted.";
            if (text != "")
            {
                textProperty.Value = text;
            }

            group.AddProperty(textProperty);

            /// Adds the property dialog to the dialog.
            PropertyDialog = Dialog.AddComponent<PropertyDialog>();
            PropertyDialog.Title = "Write/Edit Text";
            PropertyDialog.Description = "Write or edit the text; then hit the OK button.";
            PropertyDialog.Icon = Resources.Load<Sprite>("Materials/ModernUIPack/Document");
            PropertyDialog.AddGroup(group);

            PropertyDialog.OnConfirm.AddListener(TransmitText);
            PropertyDialog.OnCancel.AddListener(Cancel);

            SEEInput.KeyboardShortcutsEnabled = false;
            PropertyDialog.DialogShouldBeShown = true;
        }

        /// <summary>
        /// This method gets called when the player confirms the dialog. It will transmit the
        /// written text and then close the dialog and re-enable the keyboard shortcuts.
        /// </summary>
        private void TransmitText()
        {
            GotInput = true;
            text = textProperty.Value;
            text = text.Replace("\\n", "\n");
            text = text.Replace("\\t", "\t");
            stringAction?.Invoke(text);
            Close();
        }

        /// <summary>
        /// Fetches the input the player gave us.
        /// </summary>
        /// <param name="text">If <see cref="BasePropertyDialog.gotInput"/>, this will be the
        /// the text provided by the user. Otherwise it is empty.</param>
        /// <returns><see cref="BasePropertyDialog.gotInput"/></returns>
        internal bool TryGetUserInput(out string text)
        {
            if (GotInput)
            {
                GotInput = false;
                text = this.text;
                return true;
            }
            text = "";
            return false;
        }

        /// <summary>
        /// Initializes the input field by the given <paramref name="initText"/>.
        /// </summary>
        /// <param name="initText">The text that should be displayed in the input field when the
        /// dialog is started.</param>
        internal void SetStringInit(string initText)
        {
            text = initText;
        }
    }
}
