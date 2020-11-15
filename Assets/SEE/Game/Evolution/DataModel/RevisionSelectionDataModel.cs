//Copyright 2020 Florian Garbade

//Permission is hereby granted, free of charge, to any person obtaining a
//copy of this software and associated documentation files (the "Software"),
//to deal in the Software without restriction, including without limitation
//the rights to use, copy, modify, merge, publish, distribute, sublicense,
//and/or sell copies of the Software, and to permit persons to whom the Software
//is furnished to do so, subject to the following conditions:

//The above copyright notice and this permission notice shall be included in
//all copies or substantial portions of the Software.

//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
//INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
//PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
//LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
//TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE
//USE OR OTHER DEALINGS IN THE SOFTWARE.

using SEE.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace SEE.Game.Evolution
{
    /// <summary>
    /// Data model for the selection of a revision consisting of a drop-down
    /// menu listing all available revisions and a close button. These data are
    /// shown to the user as part of the revision-selection canvas.
    /// </summary>
    public class RevisionSelectionDataModel : MonoBehaviour
    {
        /// <summary>
        /// Drop-down menu that allows a user to select the revision to be shown.
        /// It contains all available revisions.
        /// </summary>
        public Dropdown RevisionDropdown; // serialized by Unity

        /// <summary>
        /// Button to close this dialog.
        /// </summary>
        public Button CloseViewButton; // serialized by Unity

        /// <summary>
        /// Checks whether all fields are initialized.
        /// </summary>
        private void Start()
        {
            RevisionDropdown.AssertNotNull("RevisionDropdown");
            CloseViewButton.AssertNotNull("CloseViewButton");
        }
    }
}