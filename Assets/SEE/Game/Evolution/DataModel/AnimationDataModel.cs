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
        /// TextField for the animation time in seconds, that is, the time in 
        /// seconds for showing a single graph revision during auto-play animation.
        /// </summary>
        public Text AnimationLagText; // serialized by Unity

        /// <summary>
        /// TextField for the shown revision in game.
        /// </summary>
        public Text RevisionNumberText; // serialized by Unity

        /// <summary>
        /// Toggle that shows whether auto-play mode is active.
        /// </summary>
        public Toggle AutoplayToggle; // serialized by Unity

        /// <summary>
        /// Checks if all fields are initialized.
        /// </summary>
        void Start()
        {
            AnimationLagText.AssertNotNull("AnimationLagText");
            RevisionNumberText.AssertNotNull("RevisionNumberText");
            AutoplayToggle.AssertNotNull("AutoplayToggle");
        }
    }
}