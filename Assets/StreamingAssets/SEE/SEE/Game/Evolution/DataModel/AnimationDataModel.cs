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
    /// Data model for the animation-related user interactions consisting
    /// of a text field for the animation lag, a text field for the currently
    /// shown revision, and a toggle for the auto-play mode. These data are
    /// shown to the user as part of the animation canvas.
    /// </summary>
    public class AnimationDataModel : MonoBehaviour
    {
        /// <summary>
        /// TextField for the shown revision in game.
        /// </summary>
        public Text RevisionNumberText; // serialized by Unity

        /// <summary>
        /// Slider to show the progress of the animation.
        /// </summary>
        public Slider Slider; // serialized by Unity

        /// <summary>
        /// Button to play/pause the animation.
        /// </summary>
        public Button PlayButton; // serialized by Unity

        /// <summary>
        /// Button to fast forward the animation.
        /// </summary>
        public Button FastForwardButton; // serialized by Unity

        /// <summary>
        /// Button to reverse/pause the animation.
        /// </summary>
        public Button ReverseButton; //serialized by Unity

        /// <summary>
        /// Button to fast forward the animation.
        /// </summary>
        public Button FastBackwardButton; // serialized by Unity

        /// <summary>
        /// Prefab for creating markers
        /// </summary>
        public Button MarkerPrefab; // serialized by Unity

        /// <summary>
        /// Prefab for creating comments
        /// </summary>
        public InputField CommentPrefab; // serialized by Unity

        /// <summary>
        /// Text of the play button.
        /// </summary>
        public Text PlayButtonText; // serialized by Unity

        /// <summary>
        /// Text of the reverse button.
        /// </summary>
        public Text ReverseButtonText; // serialized by Unity

        /// <summary>
        /// Text of the fast-forward button.
        /// </summary>
        public Text FastFowardButtonText; // serialized by Unity

        /// <summary>
        /// Text of the fast-backward button.
        /// </summary>
        public Text FastBackwardButtonText; // serialized by Unity

        /// <summary>
        /// Checks if all fields are initialized.
        /// </summary>
        private void Start()
        {
            RevisionNumberText.AssertNotNull("RevisionNumberText");
            Slider.AssertNotNull("Slider");
            PlayButton.AssertNotNull("PlayButton");
            FastForwardButton.AssertNotNull("FastForwardButton");
            ReverseButton.AssertNotNull("ReverseButton");
            FastBackwardButton.AssertNotNull("FastBackwardButton");
            MarkerPrefab.AssertNotNull("Marker");
            CommentPrefab.AssertNotNull("Comment");
            PlayButtonText.AssertNotNull("Text");
            ReverseButtonText.AssertNotNull("Text");
            FastFowardButtonText.AssertNotNull("Text");
            FastBackwardButtonText.AssertNotNull("Text");
        }
    }
}