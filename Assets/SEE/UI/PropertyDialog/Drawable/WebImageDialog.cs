using SEE.Controls;
using SEE.UI.PropertyDialog.HolisticMetrics;
using UnityEngine;

namespace SEE.UI.PropertyDialog.Drawable
{
    /// <summary>
    /// This class manages the dialog for adding and downloading an image from web.
    /// </summary>
    internal class WebImageDialog : HolisticMetricsDialog
    {
        /// <summary>
        /// The in the dialog written url.
        /// </summary>
        private string url = "";

        /// <summary>
        /// The in the dialog written file name.
        /// </summary>
        private string fileName = "";

        /// <summary>
        /// The input field where the player can enter a link for an url adress.
        /// </summary>
        private StringProperty urlTextProperty;

        /// <summary>
        /// The input field where the player can enter a file name for saving the downloaded image.
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
            urlTextProperty.Description = "Insert the url adress of the image";

            group.AddProperty(urlTextProperty);

            fileNameTextProperty = Dialog.AddComponent<StringProperty>();
            fileNameTextProperty.Name = "Filename";
            fileNameTextProperty.Description = "Insert a desired filename for the image to be saved. " +
                "If the name is already in use, a suffix will be added.";

            group.AddProperty(fileNameTextProperty);

            PropertyDialog = Dialog.AddComponent<PropertyDialog>();
            PropertyDialog.Title = "Download an image from web";
            PropertyDialog.Description = "Insert the url adress of the image and a desired filename; " +
                "then hit OK button.";
            PropertyDialog.Icon = Resources.Load<Sprite>("Materials/ModernUIPack/Document");
            PropertyDialog.AddGroup(group);

            PropertyDialog.OnConfirm.AddListener(TransmitText);
            PropertyDialog.OnCancel.AddListener(Cancel);

            SEEInput.KeyboardShortcutsEnabled = false;
            PropertyDialog.DialogShouldBeShown = true;
        }

        /// <summary>
        /// This method gets called when the player confirms the dialog. It will transmit the written text
        /// and then closes the dialog and re-enables the keyboard shortcuts.
        /// </summary>
        private void TransmitText()
        {
            GotInput = true;
            url = urlTextProperty.Value;
            fileName = fileNameTextProperty.Value;
            Close();
        }

        /// <summary>
        /// Fetches the input the player gave us.
        /// </summary>
        /// <param name="httpOut">If <see cref="HolisticMetricsDialog.gotInput"/>, this will be the
        /// <see cref="textOut"/>. Otherwise it's empty.</param>
        /// <returns><see cref="HolisticMetricsDialog.gotInput"/></returns>
        internal bool GetUserInput(out string httpOut, out string fileNameOut)
        {
            if (GotInput)
            {
                GotInput = false;
                httpOut = url;
                fileNameOut = fileName;
                return true;
            }
            httpOut = "";
            fileNameOut = "";
            return false;
        }
    }
}