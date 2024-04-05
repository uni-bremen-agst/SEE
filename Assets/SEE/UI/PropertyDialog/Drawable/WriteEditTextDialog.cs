using SEE.Controls;
using SEE.Game.UI.PropertyDialog.HolisticMetrics;
using UnityEngine;
using UnityEngine.Events;

namespace SEE.Game.UI.PropertyDialog.Drawable
{
    /// <summary>
    /// This class manages the dialog for adding/editing a text drawable type.
    /// </summary>
    internal class WriteEditTextDialog : HolisticMetricsDialog
    {
        /// <summary>
        /// The in the dialog written text.
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
            dialog = new GameObject("Write/Edit Text dialog");
            PropertyGroup group = dialog.AddComponent<PropertyGroup>();
            group.Name = "Write/Edit Text dialog";

            /// Adds a text property to the dialog.
            textProperty = dialog.AddComponent<StringProperty>();
            textProperty.Name = "Text";
            textProperty.Description = "Write or edit the text that should be transmit";
            if (text != "")
            {
                textProperty.Value = text;
            }
            
            group.AddProperty(textProperty);

            /// Adds the property dialog to the dialog.
            propertyDialog = dialog.AddComponent<PropertyDialog>();
            propertyDialog.Title = "Write/Edit Text";
            propertyDialog.Description = "Write or edit the text; then hit OK button.";
            propertyDialog.Icon = Resources.Load<Sprite>("Materials/ModernUIPack/Document");
            propertyDialog.AddGroup(group);

            propertyDialog.OnConfirm.AddListener(TransmitText);
            propertyDialog.OnCancel.AddListener(Cancel);

            SEEInput.KeyboardShortcutsEnabled = false;
            propertyDialog.DialogShouldBeShown = true;
        }

        /// <summary>
        /// This method gets called when the player confirms the dialog. It will transmit the written text
        /// and then closes the dialog and re-enables the keyboard shortcuts.
        /// </summary>
        private void TransmitText()
        {
            gotInput = true;
            text = textProperty.Value;
            text = text.Replace("\\n", "\n");
            text = text.Replace("\\t", "\t");
            if (stringAction != null)
            {
                stringAction.Invoke(text);
            }
            Close();
        }

        /// <summary>
        /// Fetches the input the player gave us.
        /// </summary>
        /// <param name="textOut">If <see cref="HolisticMetricsDialog.gotInput"/>, this will be the
        /// <see cref="textOut"/>. Otherwise it's empty.</param>
        /// <returns><see cref="HolisticMetricsDialog.gotInput"/></returns>
        internal bool GetUserInput(out string textOut)
        {
            if (gotInput)
            {
                gotInput = false;
                textOut = text;
                return true;
            }
            textOut = "";
            return false;
        }

        /// <summary>
        /// Init the given text to the input field.
        /// </summary>
        /// <param name="initText">The text that should be displayed in the input field when the dialog is started.</param>
        internal void SetStringInit(string initText)
        {
            text = initText;
        }
    }
}