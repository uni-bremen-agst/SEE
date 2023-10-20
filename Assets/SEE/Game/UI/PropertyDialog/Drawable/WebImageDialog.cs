using System.Collections;
using UnityEngine;
using SEE.Game.UI.PropertyDialog.HolisticMetrics;
using SEE.Game.UI.PropertyDialog;
using SEE.Controls;
using UnityEngine.Events;

namespace SEE.Game.UI.PropertyDialog.Drawable
{
    /// <summary>
    /// This class manages the dialog for adding and downloading an image from web.
    /// </summary>
    internal class WebImageDialog : HolisticMetricsDialog
    {
        /// <summary>
        /// The in the dialog written http.
        /// </summary>
        private string http = "";

        /// <summary>
        /// The in the dialog written file name.
        /// </summary>
        private string fileName = "";

        /// <summary>
        /// The input field where the player can enter a link for a http adress.
        /// </summary>
        private StringProperty httpTextProperty;

        /// <summary>
        /// The input field where the player can enter a file name for saving the downloaded image.
        /// </summary>
        private StringProperty fileNameTextProperty;

        /// <summary>
        /// This method instantiates and then displays the dialog to the player.
        /// </summary>
        internal void Open()
        {
            dialog = new GameObject("Download image from web dialog");
            PropertyGroup group = dialog.AddComponent<PropertyGroup>();
            group.Name = "Web image dialog";

            httpTextProperty = dialog.AddComponent<StringProperty>();
            httpTextProperty.Name = "Http";
            httpTextProperty.Description = "Insert the http adress of the image";

            group.AddProperty(httpTextProperty);

            fileNameTextProperty = dialog.AddComponent<StringProperty>();
            fileNameTextProperty.Name = "Filename";
            fileNameTextProperty.Description = "Insert a desired filename for the image to be saved. If the name is already in use, a suffix will be added.";

            group.AddProperty(fileNameTextProperty);

            propertyDialog = dialog.AddComponent<PropertyDialog>();
            propertyDialog.Title = "Download an image from web";
            propertyDialog.Description = "Insert the http adress of the image and a desired filename; then hit OK button.";
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
            http = httpTextProperty.Value;
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
            if (gotInput)
            {
                gotInput = false;
                httpOut = http;
                fileNameOut = fileName;
                return true;
            }
            httpOut = "";
            fileNameOut = "";
            return false;
        }
    }
}