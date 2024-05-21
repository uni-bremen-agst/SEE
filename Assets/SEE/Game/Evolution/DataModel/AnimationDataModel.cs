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
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace SEE.Game.Evolution
{
    /// <summary>
    /// Data model for the animation-related user interactions consisting
    /// of a text field for the animation lag, a text field for the currently
    /// shown revision, and a toggle for the auto-play mode as well as
    /// buttons to start, stop, and speed up the animation. These data are
    /// shown to the user as part of the animation canvas.
    /// </summary>
    public class AnimationDataModel : MonoBehaviour
    {
        /// <summary>
        /// TextField for the shown revision in game.
        /// </summary>
        public Text RevisionNumberText;

        /// <summary>
        /// TextField for the shown commit in game.
        /// </summary>
        public Text CommitInformationText;

        /// <summary>
        /// Slider to show the progress of the animation.
        /// </summary>
        public Slider Slider;

        /// <summary>
        /// Button to play/pause the animation.
        /// </summary>
        public Button PlayButton;

        /// <summary>
        /// Button to fast forward the animation.
        /// </summary>
        public Button FastForwardButton;

        /// <summary>
        /// Button to reverse/pause the animation.
        /// </summary>
        public Button ReverseButton;

        /// <summary>
        /// Button to fast forward the animation.
        /// </summary>
        public Button FastBackwardButton;

        /// <summary>
        /// Prefab for creating markers
        /// </summary>
        public Button MarkerPrefab;

        /// <summary>
        /// Prefab for creating comments
        /// </summary>
        public InputField CommentPrefab;

        /// <summary>
        /// Text of the play button.
        /// </summary>
        public Text PlayButtonText;

        /// <summary>
        /// Text of the reverse button.
        /// </summary>
        public Text ReverseButtonText;

        /// <summary>
        /// Text of the fast-forward button.
        /// </summary>
        [FormerlySerializedAs("FastFowardButtonText")]
        public Text FastForwardButtonText;

        /// <summary>
        /// Text of the fast-backward button.
        /// </summary>
        public Text FastBackwardButtonText;

        /// <summary>
        /// Checks if all fields are initialized.
        /// </summary>
        private void Start()
        {
            RevisionNumberText.AssertNotNull("RevisionNumberText");
            CommitInformationText.AssertNotNull("CommitInformationText");
            Slider.AssertNotNull("Slider");
            PlayButton.AssertNotNull("PlayButton");
            FastForwardButton.AssertNotNull("FastForwardButton");
            ReverseButton.AssertNotNull("ReverseButton");
            FastBackwardButton.AssertNotNull("FastBackwardButton");
            MarkerPrefab.AssertNotNull("Marker");
            CommentPrefab.AssertNotNull("Comment");
            PlayButtonText.AssertNotNull("Text");
            ReverseButtonText.AssertNotNull("Text");
            FastForwardButtonText.AssertNotNull("Text");
            FastBackwardButtonText.AssertNotNull("Text");
        }

        public void EnableButtons(bool enable)
        {
            PlayButton.interactable = enable;
            FastForwardButton.interactable = enable;
            ReverseButton.interactable = enable;
            FastBackwardButton.interactable = enable;
        }
    }
}