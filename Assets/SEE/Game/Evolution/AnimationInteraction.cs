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

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SEE.Controls;
using SEE.Game.City;
using SEE.GO;
using SEE.Net.Actions.Animation;
using SEE.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace SEE.Game.Evolution
{
    /// <summary>
    /// The AnimationInteraction manages user inputs and interfaces for the animation of the
    /// evolution.
    /// </summary>
    public class AnimationInteraction : MonoBehaviour
    {
        /// <summary>
        /// Whether the UI for selecting revisions is currently shown.
        /// </summary>
        public bool IsRevisionSelectionOpen { get; private set; } = false;

        /// <summary>
        /// The in-game animation canvas shown while viewing the animations. It contains
        /// the panel for the instructions shown to the user (explaining the
        /// keys) and a panel for the currently shown revision, the total number
        /// of revisions and the auto-play toggle. If the ESC key is hit, the
        /// RevisionSelectionCanvas is shown again.
        /// </summary>
        private GameObject animationCanvas; // serialized by Unity

        /// <summary>
        /// The user-data model for AnimationCanvas.
        /// </summary>
        private AnimationDataModel animationDataModel; // not serialized; will be set in Init()

        /// <summary>
        /// The in-game canvas containing the menu for selecting the shown graph revision.
        /// It is shown when the user enters the ESC key. Beside the revision selection
        /// menu, it also contains a close button. If this button is pressed, the
        /// AnimationCanvas is shown again.
        /// </summary>
        private GameObject revisionSelectionCanvas; // serialized by Unity

        /// <summary>
        /// The factor applied to the animation speed when fast-forwarding, or the divisor when slowing down.
        /// </summary>
        private float additionalAnimationFactor = 2;

        /// <summary>
        /// The original animation factor, used to reset the animation factor after fast-forwarding or slowing down.
        /// </summary>
        private  float originalAnimationFactor = 1;

        /// <summary>
        /// The user-data model for RevisionSelectionCanvas.
        /// </summary>
        private RevisionSelectionDataModel revisionSelectionDataModel; // not serialized; will be set in Init()

        /// <summary>
        /// The evolution renderer doing the rendering and animations of the graphs.
        /// </summary>
        private EvolutionRenderer evolutionRenderer; // not serialized; will be set in property EvolutionRenderer

        /// <summary>
        /// The container for the markers, needed for serialization
        /// </summary>
        private SliderMarkerContainer sliderMarkerContainer; // not serialized; will be set in Init()

        /// <summary>
        /// The currently selected marker
        /// </summary>
        private Button selectedMarker;

        /// <summary>
        /// A dictionary linking markers and comments, needed for saving the comments on application quit and deleting the comments
        /// </summary>
        private readonly Dictionary<Button, InputField> markerDictionary = new();

        /// <summary>
        /// Specifies whether the animation is currently being fast-forwarded
        /// </summary>
        private bool isFastForward;

        /// <summary>
        /// Specifies whether the animation is currently being fast-backwarded
        /// </summary>
        private bool isFastBackward;

        /// <summary>
        /// The evolution renderer doing the rendering and animations of the graphs.
        /// </summary>
        public EvolutionRenderer EvolutionRenderer
        {
            set
            {
                evolutionRenderer = value;
                Init();
            }
        }

        /// <summary>
        /// Path to the prefab of the AnimationCanvas excluding the file extension ".prefab".
        /// </summary>
        private const string animationCanvasPrefab = "Prefabs/Animation/AnimationCanvas";

        /// <summary>
        /// The name of the game object instantiated via <see cref="animationCanvasPrefab"/>.
        /// </summary>
        private const string animationCanvasGameObjectName = "AnimationCanvas";

        /// <summary>
        /// Path to the prefab of the AnimationCanvas excluding the file extension ".prefab".
        /// </summary>
        private const string revisionSelectionCanvasPrefab = "Prefabs/Animation/RevisionSelectionCanvas";

        /// <summary>
        /// The name of the game object instantiated via <see cref="revisionSelectionCanvasPrefab"/>.
        /// </summary>
        private const string revisionSelectionCanvasGameObjectName = "RevisionSelectionCanvas";

        /// <summary>
        /// The name of the graph attribute providing the commit ID.
        /// </summary>
        private const string commitIdAttributeName = "CommitId";

        /// <summary>
        /// The name of the graph attribute providing the name of the author of a commit.
        /// </summary>
        private const string commitAuthorAttributeName = "CommitAuthor";

        /// <summary>
        /// The name of the graph attribute providing the name of the timestamp of a commit.
        /// </summary>
        private const string commitTimestampAttributeName = "CommitTimestamp";

        /// <summary>
        /// The name of the graph attribute providing the commit message.
        /// </summary>
        private const string commitMessageAttributeName = "CommitMessage";

        private void Init()
        {
            animationCanvas = GetCanvas(animationCanvasGameObjectName, animationCanvasPrefab);
            revisionSelectionCanvas = GetCanvas(revisionSelectionCanvasGameObjectName, revisionSelectionCanvasPrefab);

            StartCoroutine(SetAnimationCanvasCamera());

            revisionSelectionDataModel = revisionSelectionCanvas.GetComponent<RevisionSelectionDataModel>();
            animationDataModel = animationCanvas.GetComponent<AnimationDataModel>();

            revisionSelectionDataModel.AssertNotNull("revisionSelectionDataModel");
            animationDataModel.AssertNotNull("animationDataModel");

            revisionSelectionDataModel.CloseViewButton.onClick.AddListener(ToggleMode);
            revisionSelectionDataModel.RevisionDropdown.onValueChanged.AddListener(OnDropDownChanged);

            animationDataModel.Slider.minValue = 1;
            animationDataModel.Slider.maxValue = evolutionRenderer.GraphCount - 1;
            animationDataModel.Slider.value = evolutionRenderer.CurrentGraphIndex;

            animationDataModel.PlayButton.onClick.AddListener(OnClickPlayButton);
            animationDataModel.FastForwardButton.onClick.AddListener(OnClickFastForwardButton);
            animationDataModel.ReverseButton.onClick.AddListener(OnClickReverseButton);
            animationDataModel.FastBackwardButton.onClick.AddListener(OnClickFastBackwardButton);

            if (animationDataModel.Slider.gameObject.TryGetComponentOrLog(out SliderDrag sliderDrag))
            {
                sliderDrag.EvolutionRenderer = evolutionRenderer;
            }

            try
            {
                sliderMarkerContainer = SliderMarkerContainer.Load(Path.Combine(Application.persistentDataPath, "sliderMarkers.xml"));
            }
            catch (FileNotFoundException)
            {
                sliderMarkerContainer = new SliderMarkerContainer();
            }

            foreach (SliderMarker sliderMarker in sliderMarkerContainer.SliderMarkers)
            {
                Vector3 markerPos = new(sliderMarker.MarkerX, sliderMarker.MarkerY, sliderMarker.MarkerZ);
                string comment = sliderMarker.Comment;
                AddMarker(markerPos, comment);
            }

            SetMode(false);
            OnShownGraphHasChanged();
            evolutionRenderer.RegisterOnNewGraph(OnShownGraphHasChanged);
            originalAnimationFactor = evolutionRenderer.AnimationLagFactor;
        }

        /// <summary>
        /// Waits until a camera becomes a available. When a camera is
        /// available, the world camera of the <see cref="animationCanvas"/>
        /// will be set to this camera.
        ///
        /// Intended to be run as a co-routine.
        /// </summary>
        /// <returns>whether to continue the co-routine</returns>
        private IEnumerator SetAnimationCanvasCamera()
        {
            Canvas canvas = animationCanvas.GetComponent<Canvas>();
            Camera camera = MainCamera.Camera;

            while (camera == null)
            {
                yield return new WaitForSeconds(0.5f);
                camera = MainCamera.Camera;
            }
            canvas.worldCamera = camera;
        }

        /// <summary>
        /// If a child with <paramref name="canvasGameObjectName"/> exists in <see cref="gameObject"/>,
        /// this child will be returned. If no such child exists, a new child with that name will
        /// be created under <see cref="gameObject"/> as an instantiation of the given <paramref name="canvasPrefab"/>.
        /// If <paramref name="canvasPrefab"/> cannot be loaded, the component will be disabled, and an exception
        /// be thrown.
        /// </summary>
        /// <param name="canvasGameObjectName">name of the child</param>
        /// <param name="canvasPrefab">prefab path to instantiate the child if it does not exist</param>
        /// <returns>the resulting canvas</returns>
        private GameObject GetCanvas(string canvasGameObjectName, string canvasPrefab)
        {
            GameObject result;
            Transform canvasTransform = gameObject.transform.Find(canvasGameObjectName);
            if (canvasTransform != null)
            {
                result = canvasTransform.gameObject;
            }
            else
            {
                GameObject prefab = Resources.Load<GameObject>(canvasPrefab);
                if (prefab != null)
                {
                    result = Instantiate(prefab, gameObject.transform, true);
                    result.name = canvasGameObjectName;
                }
                else
                {
                    enabled = false;
                    throw new Exception($"Prefab {canvasPrefab} not found.");
                }
            }
            return result;
        }

        /// <summary>
        /// Saves the marker data on application quit.
        /// </summary>
        private void OnApplicationQuit()
        {
            foreach (KeyValuePair<Button, InputField> p in markerDictionary)
            {
                SliderMarker sliderMarker = sliderMarkerContainer.GetSliderMarkerForLocation(p.Key.transform.position);
                sliderMarker.SetComment(p.Value.text);
            }

            sliderMarkerContainer?.Save(Path.Combine(Application.persistentDataPath, "sliderMarkers.xml"));
        }

        #region Button Actions

        /// <summary>
        /// Local callback for handling actions for when the Play/Pause button has been clicked.
        /// </summary>
        private void OnClickPlayButton()
        {
            if (!evolutionRenderer.IsAutoPlayReverse)
            {
                PressPlay();
                new PressPlayNetAction(gameObject.FullName()).Execute();
            }
        }

        /// <summary>
        /// Handles actions for when the Play/Pause button has been clicked.
        /// </summary>
        public void PressPlay()
        {
            if (isFastBackward)
            {
                additionalAnimationFactor = 2;
                evolutionRenderer.AnimationLagFactor = originalAnimationFactor * additionalAnimationFactor;
                isFastBackward = false;
                animationDataModel.FastBackwardButtonText.text = "◄◄";
            }
            if (!evolutionRenderer.IsAutoPlayForward)
            {
                animationDataModel.PlayButtonText.text = "ll";
                evolutionRenderer.ToggleAutoPlay();
            }
            else
            {
                animationDataModel.PlayButtonText.text = "►";
                evolutionRenderer.ToggleAutoPlay();
            }
        }


        /// <summary>
        /// Local callback for handling actions for when the Reverse/Pause button has been clicked.
        /// </summary>
        private void OnClickReverseButton()
        {
            if (!evolutionRenderer.IsAutoPlayForward)
            {
                PressReverse();
                new PressReverseNetAction(gameObject.FullName()).Execute();
            }
        }

        /// <summary>
        /// Handles actions for when the Reverse/Pause button has been clicked.
        /// </summary>
        public void PressReverse()
        {
            if (isFastForward)
            {
                additionalAnimationFactor = 2;
                evolutionRenderer.AnimationLagFactor = originalAnimationFactor * additionalAnimationFactor;
                isFastForward = false;
                animationDataModel.FastForwardButtonText.text = "►►";
            }
            if (!evolutionRenderer.IsAutoPlayReverse)
            {
                animationDataModel.ReverseButtonText.text = "ll";
                evolutionRenderer.ToggleAutoPlayReverse();
            }
            else
            {
                animationDataModel.ReverseButtonText.text = "◄";
                evolutionRenderer.ToggleAutoPlayReverse();
            }
        }

        /// <summary>
        /// Callback to handle actions for when the fast forward button has been clicked.
        /// Also resets the fast backward button.
        /// If the animation is playing backwards it does nothing.
        /// </summary>
        private void OnClickFastForwardButton()
        {
            if (!evolutionRenderer.IsAutoPlayReverse)
            {
                PressFastForward();
                new PressFastForwardNetAction(gameObject.FullName()).Execute();
            }
        }

        /// <summary>
        /// Handles actions for when the fast forward button has been clicked.
        /// Also resets the fast backward button.
        /// </summary>
        public void PressFastForward()
        {
            if (isFastBackward)
            {
                additionalAnimationFactor = 2;
                isFastBackward = false;
                animationDataModel.FastBackwardButtonText.text = "◄◄";
            }
            switch (additionalAnimationFactor)
            {
                case 2:
                    isFastForward = true;
                    additionalAnimationFactor = 1;
                    animationDataModel.FastForwardButtonText.text = "►►2x";
                    break;
                case 1:
                    isFastForward = true;
                    additionalAnimationFactor = 0.5f;
                    animationDataModel.FastForwardButtonText.text = "►►4x";
                    break;
                case 0.5f:
                    isFastForward = false;
                    additionalAnimationFactor = 2;
                    animationDataModel.FastForwardButtonText.text = "►►";
                    break;
            }
            evolutionRenderer.AnimationLagFactor = originalAnimationFactor * additionalAnimationFactor;
        }

        /// <summary>
        /// Callback to handle actions for when the fast forward button has been clicked.
        /// If the animation is playing forwards it does nothing.
        /// </summary>
        private void OnClickFastBackwardButton()
        {
            if (!evolutionRenderer.IsAutoPlayForward)
            {
                PressFastBackward();
                new PressFastBackwardNetAction(gameObject.FullName()).Execute();
            }
        }

        /// <summary>
        /// Handles actions for when the fast forward button has been clicked.
        /// </summary>
        public void PressFastBackward()
        {
            // TODO: There is a lot of opportunity for refactoring here, e.g., when comparing this method
            //       with OnClickFastForwardButton(). It also seems weird that the additionalAnimationFactor
            //       is set to 2 by default, with the 2x option setting it to 1, rather than it starting at 1
            //       and then being set to 0.5 by the 2x option.
            if (isFastForward)
            {
                additionalAnimationFactor = 2;
                isFastForward = false;
                animationDataModel.FastForwardButtonText.text = "►►";
            }
            switch (additionalAnimationFactor)
            {
                case 2:
                    isFastBackward = true;
                    additionalAnimationFactor = 1;
                    animationDataModel.FastBackwardButtonText.text = "◄◄2x";
                    break;
                case 1:
                    isFastBackward = true;
                    additionalAnimationFactor = 0.5f;
                    animationDataModel.FastBackwardButtonText.text = "◄◄4x";
                    break;
                case 0.5f:
                    isFastBackward = false;
                    additionalAnimationFactor = 2;
                    animationDataModel.FastBackwardButtonText.text = "◄◄";
                    break;
            }
            evolutionRenderer.AnimationLagFactor = originalAnimationFactor * additionalAnimationFactor;
        }

        /// <summary>
        /// Handles actions for when a marker is clicked.
        /// </summary>
        /// <param name="clickedMarker"> Marker that has been clicked. </param>
        private void OnClickMarker(Button clickedMarker)
        {
            selectedMarker = clickedMarker;
            string commentName = clickedMarker.GetHashCode() + "-comment";
            if (clickedMarker.transform.Find(commentName) != null)
            {
                GameObject comment = clickedMarker.transform.Find(commentName).gameObject;
                comment.SetActive(!comment.activeSelf);
            }
        }

        #endregion

        #region Marker Handling

        /// <summary>
        /// Adds an InputField to enter comments to the specified marker.
        /// </summary>
        /// <param name="marker"> Marker </param>
        /// <param name="comment"> comment to be added to the InputField, optional </param>
        /// <returns> Created InputField </returns>
        private InputField AddCommentToMarker(Button marker, string comment = null)
        {
            string commentName = marker.GetHashCode() + "-comment";
            InputField commentField = Instantiate(animationDataModel.CommentPrefab, marker.transform, false);
            Vector3 commentPos = new(1500f, 0, 0);
            commentField.transform.localScale = new Vector3(16f, 1f, 1f);
            commentField.transform.localPosition = commentPos;
            commentField.name = commentName;
            if (comment != null)
            {
                commentField.text = comment;
            }
            markerDictionary.Add(marker, commentField);
            return commentField;
        }


        /// <summary>
        /// Adds a new marker at the specified position.
        /// </summary>
        /// <param name="markerPos"> Position to add the marker at </param>
        /// <param name="comment"> Comment to be added to the marker, optional </param>
        private void AddMarker(Vector3 markerPos, string comment = null)
        {
            Button newMarker = Instantiate(animationDataModel.MarkerPrefab, animationDataModel.Slider.transform, false);
            newMarker.transform.position = markerPos;
            newMarker.onClick.AddListener(() => OnClickMarker(newMarker));
            if (sliderMarkerContainer.GetSliderMarkerForLocation(markerPos) == null)
            {
                SliderMarker newSliderMarker = new()
                {
                    MarkerX = markerPos.x,
                    MarkerY = markerPos.y,
                    MarkerZ = markerPos.z
                };
                sliderMarkerContainer.SliderMarkers.Add(newSliderMarker);
            }
            InputField commentField = AddCommentToMarker(newMarker, comment);
            commentField.gameObject.SetActive(false);
        }

        /// <summary>
        /// Removes the specified marker.
        /// </summary>
        /// <param name="marker"> Marker to remove </param>
        private void RemoveMarker(Button marker)
        {
            SliderMarker sliderMarker = sliderMarkerContainer.GetSliderMarkerForLocation(marker.transform.position);
            sliderMarkerContainer.SliderMarkers.Remove(sliderMarker);
            InputField comment = markerDictionary[marker];
            markerDictionary.Remove(marker);
            Destroyer.Destroy(comment.gameObject);
            Destroyer.Destroy(marker.gameObject);
        }

        #endregion

        /// <summary>
        /// Handles the user input as follows:
        ///   KeyBindings.Previous         => previous graph revision is shown
        ///   KeyBindings.Next             => next graph revision is shown
        ///   KeyBindings.ToggleAutoPlay   => auto-play mode is toggled
        ///   KeyBindings.SetMarker        => create new marker
        ///   KeyBindings.DeleteMarker     => delete selected marker
        ///   KeyBindings.IncreaseAnimationSpeed => double animation speed
        ///   KeyBindings.DecreaseAnimationSpeed => halve animation speed
        ///   ESC => toggle between the two canvases AnimationCanvas and RevisionSelectionCanvas
        /// </summary>
        private void Update()
        {
            if (evolutionRenderer == null)
            {
                return;
            }
            bool userIsHoveringCity = AbstractSEECity.UserIsHoveringCity(evolutionRenderer.gameObject);

            if (!IsRevisionSelectionOpen)
            {
                if (userIsHoveringCity && SEEInput.Previous())
                {
                    evolutionRenderer.ShowPreviousGraphAsync();
                }
                else if (userIsHoveringCity && SEEInput.Next())
                {
                    evolutionRenderer.ShowNextGraphAsync();
                }
                else if (userIsHoveringCity && SEEInput.ToggleAutoPlay())
                {
                    evolutionRenderer.ToggleAutoPlay();
                }
                else if (userIsHoveringCity && SEEInput.SetMarker())
                {
                    Vector3 handlePos = animationDataModel.Slider.handleRect.transform.position;
                    Vector3 markerPos = new(handlePos.x, handlePos.y + .08f, handlePos.z);
                    if (sliderMarkerContainer.GetSliderMarkerForLocation(markerPos) == null)
                    {
                        AddMarker(markerPos);
                    }
                }
                else if (userIsHoveringCity && SEEInput.DeleteMarker())
                {
                    RemoveMarker(selectedMarker);
                }
                else if (userIsHoveringCity && SEEInput.IncreaseAnimationSpeed())
                {
                    evolutionRenderer.AnimationLagFactor = Mathf.Max(0.25f, evolutionRenderer.AnimationLagFactor / 2);
                }
                else if (userIsHoveringCity && SEEInput.DecreaseAnimationSpeed())
                {
                    evolutionRenderer.AnimationLagFactor = Mathf.Min(16.0f, evolutionRenderer.AnimationLagFactor * 2);
                }
            }
            if (userIsHoveringCity && SEEInput.ToggleEvolutionCanvases())
            {
                ToggleMode();
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
        private void ToggleMode()
        {
            SetMode(!IsRevisionSelectionOpen);
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
            IsRevisionSelectionOpen = enabled;

            animationCanvas.SetActive(!IsRevisionSelectionOpen);
            revisionSelectionCanvas.SetActive(IsRevisionSelectionOpen);
            evolutionRenderer.SetAutoPlayAsync(false);
            if (IsRevisionSelectionOpen)
            {
                // if revision-selection mode is enabled, we re-fill the drop-down
                // selection menu with all available graph indices.
                revisionSelectionDataModel.RevisionDropdown.ClearOptions();
                List<Dropdown.OptionData> options = Enumerable
                    .Range(1, evolutionRenderer.GraphCount)
                    .Select(i => new Dropdown.OptionData(i.ToString()))
                    .ToList();
                revisionSelectionDataModel.RevisionDropdown.AddOptions(options);
                revisionSelectionDataModel.RevisionDropdown.value = evolutionRenderer.CurrentGraphIndex;
            }
        }

        /// <summary>
        /// Returns the value of the string attribute named <paramref name="attributeName"/> of
        /// the current graph <see cref="evolutionRenderer.GraphCurrent"/>.
        /// If the graph does not have the requested <paramref name="attributeName"/>,
        /// the empty string is returned.
        /// </summary>
        /// <param name="attributeName">name of the string attribute</param>
        /// <returns>attribute value or the empty string</returns>
        private string GetAttributeOfCurrentGraph(string attributeName)
        {
            evolutionRenderer.GraphCurrent.TryGetString(attributeName, out string result);
            return result;
        }

        /// <summary>
        /// Returns commit ID of the current graph <see cref="evolutionRenderer.GraphCurrent"/>.
        /// </summary>
        /// <returns>commit ID</returns>
        private string CurrentCommitId()
        {
            return GetAttributeOfCurrentGraph(commitIdAttributeName);
        }

        /// <summary>
        /// Returns the author of the commit of the current graph <see cref="evolutionRenderer.GraphCurrent"/>.
        /// </summary>
        /// <returns>commit author</returns>
        private string CurrentAuthor()
        {
            return GetAttributeOfCurrentGraph(commitAuthorAttributeName);
        }

        /// <summary>
        /// Returns timestamp commit of the current graph <see cref="evolutionRenderer.GraphCurrent"/>.
        /// </summary>
        /// <returns>commit timestamp</returns>
        private string CurrentCommitTimestamp()
        {
            return GetAttributeOfCurrentGraph(commitTimestampAttributeName);
        }

        /// <summary>
        /// Returns commit message of the current graph <see cref="evolutionRenderer.GraphCurrent"/>.
        /// </summary>
        /// <returns>commit message</returns>
        private string CurrentCommitMessage()
        {
            return GetAttributeOfCurrentGraph(commitMessageAttributeName);
        }

        /// <summary>
        /// Event function that updates all shown data for the user;
        /// e.g. the revision number shown in the animation canvas.
        /// This method is called as a callback of the evolution renderer
        /// when any of the graph data have changed.
        /// </summary>
        private void OnShownGraphHasChanged()
        {
            animationDataModel.RevisionNumberText.text = (evolutionRenderer.CurrentGraphIndex + 1) + " / " + evolutionRenderer.GraphCount;
            animationDataModel.CommitInformationText.text = AllOrNothing("Commit #", CurrentCommitId())
                + AllOrNothing("Author: ", CurrentAuthor())
                + AllOrNothing("Timestamp: ", CurrentCommitTimestamp())
                + AllOrNothing("Message:\n", CurrentCommitMessage());
            animationDataModel.Slider.value = evolutionRenderer.CurrentGraphIndex;

            static string AllOrNothing(string prefix, string postfix)
            {
                if (string.IsNullOrWhiteSpace(postfix))
                {
                    return "";
                }
                else
                {
                    return prefix + postfix + "\n";
                }
            }
        }

        /// <summary>
        /// Event function that changes the shown revision to the given value index.
        /// This method is called as a callback when the user selects an entry in
        /// the RevisionDropdown box.
        /// </summary>
        /// <param name="value">the revision index selected from the drop-down box</param>
        private void OnDropDownChanged(int value)
        {
            if (value != evolutionRenderer.CurrentGraphIndex)
            {
                evolutionRenderer.TryShowSpecificGraph(value);
            }
        }
    }
}
