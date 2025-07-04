using SEE.Controls;
using UnityEngine;

namespace SEE.UI.PropertyDialog.Drawable
{
    /// <summary>
    /// This class manages the dialog for adding and downloading an image from the web.
    /// </summary>
    internal class WebImageDialog : BasePropertyDialog
    {
        /// <summary>
        /// The URL contained in the dialog.
        /// </summary>
        private string url = "";

        /// <summary>
        /// The filename contained in the dialog.
        /// </summary>
        private string filename = "";

        /// <summary>
        /// The input field where the player can enter a URL.
        /// </summary>
        private StringProperty urlTextProperty;

        /// <summary>
        /// The input field where the player can enter a filename for saving the
        /// downloaded image.
        /// </summary>
        private StringProperty fileNameTextProperty;

        /// <summary>
        /// This method instantiates and then displays the dialog to the player.
        /// </summary>
        internal void Open()
        {
            Dialog = new GameObject("Download image from web dialog");
            PropertyGroup group = Dialog.AddComponent<PropertyGroup>();
            group.Name = "Web image dialog";

            urlTextProperty = Dialog.AddComponent<StringProperty>();
            urlTextProperty.Name = "URL";
            urlTextProperty.Description = "Insert the URL of the image";

            group.AddProperty(urlTextProperty);

            fileNameTextProperty = Dialog.AddComponent<StringProperty>();
            fileNameTextProperty.Name = "Filename";
            fileNameTextProperty.Description = "Insert a desired filename for the image to be saved. " +
                "If the name is already in use, a suffix will be added.";

            group.AddProperty(fileNameTextProperty);

            PropertyDialog = Dialog.AddComponent<PropertyDialog>();
            PropertyDialog.Title = "Download an image from the web";
            PropertyDialog.Description = "Insert the URL of the image and a desired filename; " +
                "then hit the OK button.";
            PropertyDialog.Icon = Resources.Load<Sprite>("Materials/ModernUIPack/Document");
            PropertyDialog.AddGroup(group);

            PropertyDialog.OnConfirm.AddListener(TransmitText);
            PropertyDialog.OnCancel.AddListener(Cancel);

            SEEInput.KeyboardShortcutsEnabled = false;
            PropertyDialog.DialogShouldBeShown = true;
        }

        /// <summary>
        /// This method gets called when the player confirms the dialog. It will transmit
        /// the written text and then closes the dialog and re-enables the keyboard shortcuts.
        /// </summary>
        private void TransmitText()
        {
            GotInput = true;
            url = urlTextProperty.Value;
            filename = fileNameTextProperty.Value;
            Close();
        }

        /// <summary>
        /// Fetches the input the player gave us.
        /// </summary>
        /// <param name="url">If <see cref="BasePropertyDialog.gotInput"/>, this will be the
        /// URL the user provided. Otherwise it is empty.</param>
        /// <param name="filename">If <see cref="BasePropertyDialog.gotInput"/>, this will be the
        /// filename the user provided. Otherwise it is empty.</param>
        /// <returns><see cref="BasePropertyDialog.gotInput"/></returns>
        internal bool TryGetUserInput(out string url, out string filename)
        {
            if (GotInput)
            {
                GotInput = false;
                url = this.url;
                filename = this.filename;
                return true;
            }
            url = "";
            filename = "";
            return false;
        }
    }
}
