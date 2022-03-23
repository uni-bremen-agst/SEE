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
        private GameObject AnimationCanvas; // serialized by Unity

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
        private GameObject RevisionSelectionCanvas; // serialized by Unity

        /// <summary>
        /// The time in between two revisions in auto-play mode.
        /// </summary>
        private float animationTimeValue = 2;

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
        private readonly Dictionary<Button, InputField> markerDictionary = new Dictionary<Button, InputField>();

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
        private const string AnimationCanvasPrefab = "Prefabs/Animation/AnimationCanvas";

        /// <summary>
        /// The name of the game object instantiated via <see cref="AnimationCanvasPrefab"/>.
        /// </summary>
        private const string AnimationCanvasGameObjectName = "AnimationCanvas";

        /// <summary>
        /// Path to the prefab of the AnimationCanvas excluding the file extension ".prefab".
        /// </summary>
        private const string RevisionSelectionCanvasPrefab = "Prefabs/Animation/RevisionSelectionCanvas";

        /// <summary>
        /// The name of the game object instantiated via <see cref="RevisionSelectionCanvasPrefab"/>.
        /// </summary>
        private const string RevisionSelectionCanvasGameObjectName = "RevisionSelectionCanvas";

        /// <summary>
        /// The name of the graph attribute providing the commit id.
        /// </summary>
        private const string CommitIdAttributeName = "CommitId";

        /// <summary>
        /// The name of the graph attribute providing the name of the author of a commit.
        /// </summary>
        private const string CommitAuthorAttributeName = "CommitAuthor";

        /// <summary>
        /// The name of the graph attribute providing the name of the timestamp of a commit.
        /// </summary>
        private const string CommitTimestampAttributeName = "CommitTimestamp";

        /// <summary>
        /// The name of the graph attribute providing the commit message.
        /// </summary>
        private const string CommitMessageAttributeName = "CommitMessage";

        private void Init()
        {
            AnimationCanvas = GetCanvas(AnimationCanvasGameObjectName, AnimationCanvasPrefab);
            RevisionSelectionCanvas = GetCanvas(RevisionSelectionCanvasGameObjectName, RevisionSelectionCanvasPrefab);

            StartCoroutine(SetAnimationCanvasCamera());

            revisionSelectionDataModel = RevisionSelectionCanvas.GetComponent<RevisionSelectionDataModel>();
            animationDataModel = AnimationCanvas.GetComponent<AnimationDataModel>();

            revisionSelectionDataModel.AssertNotNull("revisionSelectionDataModel");
            animationDataModel.AssertNotNull("animationDataModel");

            revisionSelectionDataModel.CloseViewButton.onClick.AddListener(ToggleMode);
            revisionSelectionDataModel.RevisionDropdown.onValueChanged.AddListener(OnDropDownChanged);

            animationDataModel.Slider.minValue = 1;
            animationDataModel.Slider.maxValue = evolutionRenderer.GraphCount - 1;
            animationDataModel.Slider.value = evolutionRenderer.CurrentGraphIndex;

            animationDataModel.PlayButton.onClick.AddListener(TaskOnClickPlayButton);
            animationDataModel.FastForwardButton.onClick.AddListener(TaskOnClickFastForwardButton);
            animationDataModel.ReverseButton.onClick.AddListener(TaskOnClickReverseButton);
            animationDataModel.FastBackwardButton.onClick.AddListener(TaskOnClickFastBackwardButton);

            if (animationDataModel.Slider.TryGetComponent(out SliderDrag sliderDrag))
            {
                sliderDrag.EvolutionRenderer = evolutionRenderer;
            }
            else
            {
                Debug.LogError("SliderDrag script could not be loaded.\n");
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
                Vector3 markerPos = new Vector3(sliderMarker.MarkerX, sliderMarker.MarkerY, sliderMarker.MarkerZ);
                string comment = sliderMarker.Comment;
                AddMarker(markerPos, comment);
            }

            SetMode(false);
            OnShownGraphHasChanged();
            evolutionRenderer.Register(OnShownGraphHasChanged);
        }

        /// <summary>
        /// Waits until a camera becomes a available. When a camera is
        /// available, the world camera of the <see cref="AnimationCanvas"/>
        /// will be set to this camera.
        ///
        /// Intended to be run as a co-routine.
        /// </summary>
        /// <returns>whether to continue the co-routine</returns>
        private IEnumerator SetAnimationCanvasCamera()
        {
            Canvas canvas = AnimationCanvas.GetComponent<Canvas>();
            Camera camera = Camera.main;

            while (camera == null)
            {
                yield return new WaitForSeconds(0.5f);
                camera = Camera.main;
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
        /// Saves the marker data on application quit
        /// </summary>
        private void OnApplicationQuit()
        {
            foreach (KeyValuePair<Button, InputField> p in markerDictionary)
            {
                SliderMarker sliderMarker = sliderMarkerContainer.getSliderMarkerForLocation(p.Key.transform.position);
                sliderMarker.SetComment(p.Value.text);
            }

            sliderMarkerContainer?.Save(Path.Combine(Application.persistentDataPath, "sliderMarkers.xml"));
        }


        /// <summary>
        /// Handles actions for when the Play/Pause button has been clicked.
        /// </summary>
        private void TaskOnClickPlayButton()
        {
            if (!evolutionRenderer.IsAutoPlayReverse)
            {
                if (isFastBackward)
                {
                    animationTimeValue = 2;
                    evolutionRenderer.AnimationLag = animationTimeValue;
                    isFastBackward = false;
                    animationDataModel.FastBackwardButtonText.text = "◄◄";
                }
                if (!evolutionRenderer.IsAutoPlay)
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

        }

        /// <summary>
        /// Handles actions for when the Reverse/Pause button has been clicked.
        /// </summary>
        private void TaskOnClickReverseButton()
        {
            if (!evolutionRenderer.IsAutoPlay)
            {
                if (isFastForward)
                {
                    animationTimeValue = 2;
                    evolutionRenderer.AnimationLag = animationTimeValue;
                    isFastForward = false;
                    animationDataModel.FastFowardButtonText.text = "►►";
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

        }

        /// <summary>
        /// Handles actions for when the fast forward button has been clicked.
        /// Also resets the fast backward button.
        /// If the animation is playing backwards it does nothing.
        /// </summary>
        private void TaskOnClickFastForwardButton()
        {
            if (evolutionRenderer.IsAutoPlayReverse)
            {
                return;
            }
            if (isFastBackward)
            {
                animationTimeValue = 2;
                evolutionRenderer.AnimationLag = animationTimeValue;
                isFastBackward = false;
                animationDataModel.FastBackwardButtonText.text = "◄◄";
            }
            switch (animationTimeValue)
            {
                case 2:
                    isFastForward = true;
                    animationTimeValue = 1;
                    evolutionRenderer.AnimationLag = animationTimeValue;
                    animationDataModel.FastFowardButtonText.text = "►►2x";
                    break;
                case 1:
                    isFastForward = true;
                    animationTimeValue = 0.5f;
                    evolutionRenderer.AnimationLag = animationTimeValue;
                    animationDataModel.FastFowardButtonText.text = "►►4x";
                    break;
                case 0.5f:
                    isFastForward = false;
                    animationTimeValue = 2;
                    evolutionRenderer.AnimationLag = animationTimeValue;
                    animationDataModel.FastFowardButtonText.text = "►►";
                    break;
            }
        }

        /// <summary>
        /// Handles actions for when the fast forward button has been clicked.
        /// If the animation is playing forwards it does nothing.
        /// </summary>
        private void TaskOnClickFastBackwardButton()
        {
            if (evolutionRenderer.IsAutoPlay)
            {
                return;
            }
            if (isFastForward)
            {
                animationTimeValue = 2;
                evolutionRenderer.AnimationLag = animationTimeValue;
                isFastForward = false;
                animationDataModel.FastFowardButtonText.text = "►►";
            }
            switch (animationTimeValue)
            {
                case 2:
                    isFastBackward = true;
                    animationTimeValue = 1;
                    evolutionRenderer.AnimationLag = animationTimeValue;
                    animationDataModel.FastBackwardButtonText.text = "◄◄2x";
                    break;
                case 1:
                    isFastBackward = true;
                    animationTimeValue = 0.5f;
                    evolutionRenderer.AnimationLag = animationTimeValue;
                    animationDataModel.FastBackwardButtonText.text = "◄◄4x";
                    break;
                case 0.5f:
                    isFastBackward = false;
                    animationTimeValue = 2;
                    evolutionRenderer.AnimationLag = animationTimeValue;
                    animationDataModel.FastBackwardButtonText.text = "◄◄";
                    break;
            }
        }

        /// <summary>
        /// Handles actions for when a marker is clicked.
        /// </summary>
        /// <param name="clickedMarker"> Marker that has been clicked. </param>
        private void TaskOnClickMarker(Button clickedMarker)
        {
            selectedMarker = clickedMarker;
            string commentName = clickedMarker.GetHashCode() + "-comment";
            if (clickedMarker.transform.Find(commentName) != null)
            {
                GameObject comment = clickedMarker.transform.Find(commentName).gameObject;
                comment.SetActive(!comment.activeSelf);
            }
        }

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
            Vector3 commentPos = new Vector3(1500f, 0, 0);
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
        /// Adds a new marker at the specified position
        /// </summary>
        /// <param name="markerPos"> Position to add the marker at </param>
        /// <param name="comment"> Comment to be added to the marker, optional </param>
        private void AddMarker(Vector3 markerPos, string comment = null)
        {
            Button newMarker = Instantiate(animationDataModel.MarkerPrefab, animationDataModel.Slider.transform, false);
            newMarker.transform.position = markerPos;
            newMarker.onClick.AddListener(() => TaskOnClickMarker(newMarker));
            if (sliderMarkerContainer.getSliderMarkerForLocation(markerPos) == null)
            {
                SliderMarker newSliderMarker = new SliderMarker();
                newSliderMarker.MarkerX = markerPos.x;
                newSliderMarker.MarkerY = markerPos.y;
                newSliderMarker.MarkerZ = markerPos.z;
                sliderMarkerContainer.SliderMarkers.Add(newSliderMarker);
            }
            InputField commentField = AddCommentToMarker(newMarker, comment);
            commentField.gameObject.SetActive(false);
        }

        /// <summary>
        /// Removes the specified marker
        /// </summary>
        /// <param name="marker"> Marker to remove </param>
        private void RemoveMarker(Button marker)
        {
            SliderMarker sliderMarker = sliderMarkerContainer.getSliderMarkerForLocation(marker.transform.position);
            sliderMarkerContainer.SliderMarkers.Remove(sliderMarker);
            InputField comment = markerDictionary[marker];
            markerDictionary.Remove(marker);
            Destroy(comment.gameObject);
            Destroy(marker.gameObject);
        }

        /// <summary>
        /// Handles the user input as follows:
        ///   KeyBindings.PreviousRevision => previous graph revision is shown
        ///   KeyBindings.NextRevision     => next graph revision is shown
        ///   KeyBindings.SetMarker        => create new marker
        ///   KeyBindings.ToggleAutoPlay   => auto-play mode is toggled
        ///   KeyBindings.DeleteMarker     => delete selected marker
        ///   KeyBindings.IncreaseAnimationSpeed => double animation speed
        ///   KeyBindings.DecreaseAnimationSpeed => halve animation speed
        ///   ESC => toggle between the two canvases AnimationCanvas and RevisionSelectionCanvas
        /// </summary>
        private void Update()
        {
            if (!IsRevisionSelectionOpen)
            {
                if (SEEInput.PreviousRevision())
                {
                    evolutionRenderer.ShowPreviousGraph();
                }
                else if (SEEInput.NextRevision())
                {
                    evolutionRenderer.ShowNextGraph();
                }
                else if (SEEInput.ToggleAutoPlay())
                {
                    evolutionRenderer.ToggleAutoPlay();
                }
                else if (SEEInput.SetMarker())
                {
                    Vector3 handlePos = animationDataModel.Slider.handleRect.transform.position;
                    Vector3 markerPos = new Vector3(handlePos.x, handlePos.y + .08f, handlePos.z);
                    if (sliderMarkerContainer.getSliderMarkerForLocation(markerPos) == null)
                    {
                        AddMarker(markerPos, null);
                    }
                }
                else if (SEEInput.DeleteMarker())
                {
                    RemoveMarker(selectedMarker);
                }
                else if (SEEInput.IncreaseAnimationSpeed())
                {
                    evolutionRenderer.AnimationLag = Mathf.Max(0.25f, evolutionRenderer.AnimationLag / 2);
                    Debug.Log($"new animation lag is {evolutionRenderer.AnimationLag}\n");
                }
                else if (SEEInput.DecreaseAnimationSpeed())
                {
                    evolutionRenderer.AnimationLag = Mathf.Min(16.0f, evolutionRenderer.AnimationLag * 2);
                    Debug.Log($"new animation lag is {evolutionRenderer.AnimationLag}\n");
                }
            }
            if (SEEInput.ToggleEvolutionCanvases())
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

            AnimationCanvas.SetActive(!IsRevisionSelectionOpen);
            RevisionSelectionCanvas.SetActive(IsRevisionSelectionOpen);
            evolutionRenderer.SetAutoPlay(false);
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
        /// Returns commit id of the current graph <see cref="evolutionRenderer.GraphCurrent"/>.
        /// </summary>
        /// <returns>commit id</returns>
        private string CurrentCommitId()
        {
            return GetAttributeOfCurrentGraph(CommitIdAttributeName);
        }

        /// <summary>
        /// Returns the author of the commit of the current graph <see cref="evolutionRenderer.GraphCurrent"/>.
        /// </summary>
        /// <returns>commit author</returns>
        private string CurrentAuthor()
        {
            return GetAttributeOfCurrentGraph(CommitAuthorAttributeName);
        }

        /// <summary>
        /// Returns timestamp commit of the current graph <see cref="evolutionRenderer.GraphCurrent"/>.
        /// </summary>
        /// <returns>commit timestamp</returns>
        private string CurrentCommitTimestamp()
        {
            return GetAttributeOfCurrentGraph(CommitTimestampAttributeName);
        }

        /// <summary>
        /// Returns commit message of the current graph <see cref="evolutionRenderer.GraphCurrent"/>.
        /// </summary>
        /// <returns>commit message</returns>
        private string CurrentCommitMessage()
        {
            return GetAttributeOfCurrentGraph(CommitMessageAttributeName);
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
                + AllOrNothing("\nAuthor: ", CurrentAuthor())
                + AllOrNothing("\nTimestamp: ", CurrentCommitTimestamp())
                + AllOrNothing("\nMessage:\n", CurrentCommitMessage());
            animationDataModel.Slider.value = evolutionRenderer.CurrentGraphIndex;

            string AllOrNothing(string prefix, string postfix)
            {
                if (string.IsNullOrWhiteSpace(postfix))
                {
                    return "";
                }
                else
                {
                    return prefix + postfix;
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
