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

using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using SEE.Animation.Internal;

namespace SEE.Animation
{
    /// <summary>
    /// The AnimationInteraction manages user inputs and interfaces.
    /// </summary>
    public class AnimationInteraction : MonoBehaviour
    {
        /// <summary>
        /// The camera from the user.
        /// </summary>
        public FlyCamera FlyCamera;

        /// <summary>
        /// The in-game animation canvas shown while viewing the animations. It contains
        /// the panel for the instructions shown to the user (explaining the
        /// keys) and a panel for the currently shown revision, the total number
        /// of revisions and the auto-play toggle. If the ESC key is hit, the
        /// RevisionSelectionCanvas is shown again.
        /// </summary>
        public GameObject AnimationCanvas;

        /// <summary>
        /// The user-data model for AnimationCanvas.
        /// </summary>
        private AnimationDataModel animationDataModel;

        /// <summary>
        /// The in-game canvas containing the menu for selecting the shown graph revision. 
        /// It is shown when the user enters the ESC key. Beside the revision selection
        /// menu, it also contains a close button. If this button is pressed, the
        /// AnimationCanvas is shown again.
        /// </summary>
        public GameObject RevisionSelectionCanvas;

        /// <summary>
        /// The user-data model for RevisionSelectionCanvas.
        /// </summary>
        private RevisionSelectionDataModel revisionSelectionDataModel;

        /// <summary>
        /// The SEECityEvolution containing all necessary components for controlling the animations.
        /// </summary>
        public SEECityEvolution CityEvolution;

        /// <summary>
        /// Returns true if RevisionSelectionCanvas is currently shown.
        /// </summary>
        public bool IsRevisionSelectionOpen => !FlyCamera.IsEnabled;

        void Start()
        {
            revisionSelectionDataModel = RevisionSelectionCanvas.GetComponent<RevisionSelectionDataModel>();
            animationDataModel = AnimationCanvas.GetComponent<AnimationDataModel>();

            revisionSelectionDataModel.AssertNotNull("revisionSelectionDataModel");
            animationDataModel.AssertNotNull("animationDataModel");

            revisionSelectionDataModel.CloseViewButton.onClick.AddListener(ToogleMode);
            revisionSelectionDataModel.RevisionDropdown.onValueChanged.AddListener(OnDropDownChanged);

            SetMode(true);
            OnViewDataChanged();
            CityEvolution.ViewDataChangedEvent.AddListener(OnViewDataChanged);
        }

        /// <summary>
        /// Handles the user input as follows:
        ///   k   => previous graph revision is shown
        ///   l   => next graph revision is shown
        ///   tab => auto-play mode is toggled
        ///   0-9 => the time in between two revisions in auto-play mode is adjusted
        ///   ESC => toggle between the two canvases AnimationCanvas and RevisionSelectionCanvas
        /// </summary>
        void Update()
        {
            if (!IsRevisionSelectionOpen)
            {
                if (Input.GetKeyDown("k"))
                {
                    CityEvolution.ShowPreviousGraph();
                }
                else if (Input.GetKeyDown("l"))
                {
                    CityEvolution.ShowNextGraph();
                }
                else if (Input.GetKeyDown(KeyCode.Tab))
                {
                    CityEvolution.ToggleAutoPlay();
                }

                string[] animationTimeKeys = { "1", "2", "3", "4", "5", "6", "7", "8", "9", "0" };
                float[] animationTimeValues = { 0.1f, 0.5f, 1, 2, 3, 4, 5, 8, 16, 0 };
                for (int i = 0; i < animationTimeKeys.Length; i++)
                {
                    if (Input.GetKeyDown(animationTimeKeys[i]))
                    {
                        CityEvolution.AnimationLag = animationTimeValues[i];
                    }
                }
            }
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                SetMode(FlyCamera.IsEnabled);
            }
        }

        /// <summary>
        /// Toggles between the animation-interaction mode and the revision-selection
        /// mode. 
        /// In the animation-interaction mode, the user can see and control
        /// the animations of the graph revisions through the AnimationCanvas
        /// and freely move in the city. 
        /// In the revision-selection mode, the user can select the revision to be shown
        /// through the RevisionSelectionCanvas. No movement is possible in that mode.
        /// </summary>
        private void ToogleMode()
        {
            SetMode(FlyCamera.IsEnabled);
        }

        /// <summary>
        /// Toggles between the animation-interaction mode and the revision-selection
        /// mode. If <paramref name="enabled"/> is true, the revision-selection mode
        /// is activated; otherwise the animation-interaction mode is turned on.
        /// 
        /// In the revision-selection mode, the user can select the revision to be shown
        /// through the RevisionSelectionCanvas. No movement is possible in that mode.
        /// 
        /// In the animation-interaction mode, the user can see and control
        /// the animations of the graph revisions through the AnimationCanvas
        /// and freely move in the city. 
        /// 
        /// Both modes are mutually exclusive.
        /// 
        /// Auto-play animation is always turned off independent of <paramref name="enabled"/>.
        /// </summary>
        /// <param name="enabled">if true, revision-selection mode is turned on; otherwise
        /// animation-interaction mode is turned on</param>
        private void SetMode(bool enabled)
        {
            FlyCamera.IsEnabled = !enabled;
            AnimationCanvas.SetActive(!enabled);
            RevisionSelectionCanvas.SetActive(enabled);
            CityEvolution.SetAutoPlay(false);
            if (enabled)
            {
                // if revision-selection mode is enabled, we re-fill the drop-down
                // selection menu with all available graph indices.
                revisionSelectionDataModel.RevisionDropdown.ClearOptions();
                var options = Enumerable
                    .Range(1, CityEvolution.GraphCount)
                    .Select(i => new Dropdown.OptionData(i.ToString()))
                    .ToList();
                revisionSelectionDataModel.RevisionDropdown.AddOptions(options);
                revisionSelectionDataModel.RevisionDropdown.value = CityEvolution.CurrentGraphIndex;
            }
        }

        /// <summary>
        /// Event function that updates all shown data for the user;
        /// e.g. the revision number shown in the animation canvas.
        /// This method is called as a callback when any of the graph
        /// data have changed.
        /// </summary>
        private void OnViewDataChanged()
        {
            animationDataModel.RevisionNumberText.text = (CityEvolution.CurrentGraphIndex + 1) + " / " + CityEvolution.GraphCount;
            animationDataModel.AutoplayToggle.isOn = CityEvolution.IsAutoPlay;
            animationDataModel.AnimationLagText.text = "Revision animation lag: " + CityEvolution.AnimationLag + "s";
        }

        /// <summary>
        /// Event function that changes the shown revision to the given value index.
        /// This method is called as a callback when the user selects an entry in
        /// the RevisionDropdown box.
        /// </summary>
        /// <param name="value">the revision index selected from the drop-down box</param>
        private void OnDropDownChanged(int value)
        {
            if (value != CityEvolution.CurrentGraphIndex)
            {
                CityEvolution.TryShowSpecificGraph(value);
            }
        }
    }
}