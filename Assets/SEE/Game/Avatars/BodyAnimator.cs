// This script uses MediaPipe Unity Plugin availible at
// https://github.com/homuler/MediaPipeUnityPlugin
//
// Copyright (c) 2021 homuler
//
// Use of the source code of the plugin is governed by an MIT-style
// license that can be found at
// https://github.com/homuler/MediaPipeUnityPlugin/blob/master/LICENSE
//
//
// This script also relies on the Task-API-Tutorial by homuler to use MediaPipe solutions in Unity scripts. The tutorial is available at the link:
// https://github.com/homuler/MediaPipeUnityPlugin/blob/master/docs/Tutorial-Task-API.md

using Mediapipe;
using Mediapipe.Tasks.Vision.GestureRecognizer;
using Mediapipe.Tasks.Vision.PoseLandmarker;
using Mediapipe.Unity.Experimental;
using RootMotion.FinalIK;
using SEE.Controls;
using SEE.GO;
using SEE.UI;
using SEE.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
/// <summary>
/// These namespaces are imported to be able to use MediaPipe solutions
/// </summary>
using Stopwatch = System.Diagnostics.Stopwatch;

namespace SEE.Game.Avatars
{
    /// <summary>
    /// Calls models from MediaPipe to further animate the hands and finger movements of the avatar.
    /// </summary>
    /// <remarks>This component is assumed to be attached to the avatar's root object.</remarks>
    /// <remarks>The animation itself occurs using the functions of the class <see cref="HandsAnimator"/>.</remarks>
    internal class BodyAnimator : MonoBehaviour
    {
        /// <summary>
        /// Instance of class <see cref="HandsAnimator"/> responsible for animation.
        /// </summary>
        public HandsAnimator HandsAnimator = new();

        /// <summary>
        /// Text assets that define configurations of MediaPipe models.
        /// </summary>
        [SerializeField] private TextAsset poseLandmarkerModelAsset;
        [SerializeField] private TextAsset gestureRecognizerModelAsset;

        /// <summary>
        /// Stores the texture from the device's webcam.
        /// </summary>
        private WebCamTexture webCamTexture;

        /// <summary>
        /// Used to calculate timestamps needed by MediaPipe calculators.
        /// </summary>
        private readonly Stopwatch stopwatch = new();

        /// <summary>
        /// TextureFrame object to hold a copy of the webcam texture on the CPU.
        /// </summary>
        private TextureFrame textureFrame;

        /// <summary>
        /// Solver from MediaPipe that is used to detect pose.
        /// </summary>
        private PoseLandmarker poseLandmarker;

        /// <summary>
        /// MediaPipe model used to classify detected gestures.
        /// </summary>
        private GestureRecognizer gestureRecognizer;

        /// <summary>
        /// The FullBodyBiped IK solver attached to the avatar.
        /// </summary>
        private FullBodyBipedIK ik;

        /// <summary>
        /// If true, local interactions control the avatar.
        /// <summary>
        public bool IsLocallyControlled = true;

        /// <summary>
        /// If true, hand animations with MediaPipe are to be used.
        /// </summary>
        public bool IsUsingHandAnimations = false;

        /// <summary>
        /// If true, the user enabled hand animations using MediaPipe for the first time.
        /// </summary>
        public bool IsFirstActivationOfHandAnimations = true;

        /// <summary>
        /// Time in seconds when the last error message indicating that no hand landmarks were found was shown.
        /// </summary>
        /// <remarks>Start negative so first error can appear immediatly.</remarks>
        private float lastHandLandmarksErrorTime = -handLandmarksErrorCooldown;

        /// <summary>
        /// Time interval (in seconds) between error messages.
        /// </summary>
        private const float handLandmarksErrorCooldown = 15f;

        /// <summary>
        /// Indicates whether the MediaPipe values are set.
        /// </summary>
        private bool isMediaPipeInitialized = false;

        /// <summary>
        /// Indicates whether the user's starting hand positions need to be recalibrated.
        /// </summary>
        public bool IsRecalibrationNeeded = false;

        /// <summary>
        /// The most recent <see cref="PoseLandmarkerResult"/> received from MediaPipe.
        /// </summary>
        /// <remarks>
        /// This object may be updated at any time by the MediaPipe processing thread,
        /// and therefore should not be accessed directly.
        /// </remarks>
        private PoseLandmarkerResult resultPoseLandmarker;

        /// <summary>
        /// The most recent <see cref="GestureRecognizerResult"/> received from MediaPipe.
        /// </summary>
        /// <remarks>
        /// This object may be updated at any time by the MediaPipe processing thread,
        /// and therefore should not be accessed directly.
        /// </remarks>
        private GestureRecognizerResult resultGestureRecognizer;

        /// <summary>
        /// A stable snapshot of the <see cref="PoseLandmarkerResult"/> at a specific point in time.
        /// This is a deep-copied version of the latest MediaPipe output,
        /// intended for use in animation without threading risks.
        /// </summary>
        private PoseLandmarkerResult snapshotResultPoseLandmarker = default;

        /// <summary>
        /// A stable snapshot of the <see cref="GestureRecognizerResult"/> at a specific point in time.
        /// This is a deep-copied version of the latest MediaPipe output,
        /// intended for use in animation without threading risks.
        /// </summary>
        private GestureRecognizerResult snapshotResultGestureRecognizer = default;

        /// <summary>
        /// Synchronization object used to ensure thread-safe access to MediaPipe results.
        /// All reads and writes to <see cref="resultPoseLandmarker"/> and <see cref="resultGestureRecognizer"/>
        /// must be protected using this lock to avoid race conditions.
        /// </summary>
        private readonly object _lock = new();

        /// <summary>
        /// A list of timestamps from MediaPipe callbacks used by One Euro Filter
        /// to compute sampling period of the signal.
        /// </summary>
        private readonly List<float> samplingTimesGestureRecognizer = new List<float>();

        /// <summary>
        /// A stable copy of the timestamps from MediaPipe callbacks at one specific moment in time.
        /// </summary>
        private readonly List<float> samplingTimesGestureRecognizerSnapshot = new List<float>();

        /// <summary>
        /// A list of timestamps from MediaPipe callbacks used by One Euro Filter
        /// to compute sampling period of the signal.
        /// </summary>
        private readonly List<float> samplingTimesPoseLandmarker = new List<float>();

        /// <summary>
        /// A stable copy of the timestamps from MediaPipe callbacks at one specific moment in time.
        /// </summary>
        private readonly List<float> samplingTimesPoseLandmarkerSnapshot = new List<float>();

        /// <summary>
        /// The timestamp of the first received MediaPipe <see cref="GestureRecognizer"/> callback.
        /// Used as a reference point to compute relative sampling times.
        /// </summary>
        private float firstTimestampGestureRecognizer;

        /// <summary>
        /// The timestamp of the first received MediaPipe <see cref="PoseLandmarker"/> callback.
        /// Used as a reference point to compute relative sampling times.
        /// </summary>
        private float firstTimestampPoseLandmarker;

        /// <summary>
        /// Indicates whether the current timestamp is the first one received from MediaPipe <see cref="GestureRecognizer"/> callbacks.
        /// </summary>
        private bool isFirstTimeStampGestureRecognizer = true;

        /// <summary>
        /// Indicates whether the current timestamp is the first one received from MediaPipe <see cref="PoseLandmarker"/> callbacks.
        /// </summary>
        private bool isFirstTimeStampPoseLandmarker = true;

        /// <summary>
        /// Indicates whether new hand landmarks have been received from the callback.
        /// </summary>
        private bool areNewHandLandmarks = false;

        /// <summary>
        /// Tracks the number of frames in which no pose landmarks are detected by MediaPipe.
        /// </summary>
        private int poseLandmarksLostFrames = 0;

        /// <summary>
        /// Maximum number of pose landmarks lost frames allowed before assigning a neutral position
        /// to the avatar.
        /// If <see cref="poseLandmarksLostFrames"/> is smaller that this value, last detected values will be animated.
        /// </summary>
        private int maxPoseLandmarksLostFrames = 15;

        /// <summary>
        /// Subscribes to the <see cref="WebcamManager.OnActiveWebcamChanged"/> event.
        /// This ensures that the component reacts whenever the active webcam changes.
        /// Additionally, if a webcam is already active when this component is enabled,
        /// <see cref="HandleWebcamChanged"/> is called immediately to synchronize state.
        /// </summary>
        private void OnEnable()
        {
            WebcamManager.OnActiveWebcamChanged += HandleWebcamChanged;
            // Request current state once when enabling
            if (WebcamManager.ActiveWebcam != null)
            {
                HandleWebcamChanged(WebcamManager.ActiveWebcam);
            }
        }

        /// <summary>
        /// Closes the MediaPipe graphs and disposes of the pose landmarker and
        /// gesture recognizer ressources.
        /// </summary>
        private void OnDestroy()
        {
            poseLandmarker?.Close();
            if (poseLandmarker != null)
            {
                ((IDisposable)poseLandmarker).Dispose();
            }

            gestureRecognizer?.Close();
            if (gestureRecognizer != null)
            {
                ((IDisposable)gestureRecognizer).Dispose();
            }
        }

        /// <summary>
        /// Unsubscribes from the <see cref="WebcamManager.OnActiveWebcamChanged"/> event
        /// to prevent memory leaks or invalid callbacks when the component is disabled.
        /// </summary>
        private void OnDisable()
        {
            WebcamManager.OnActiveWebcamChanged -= HandleWebcamChanged;
        }

        /// <summary>
        /// Initializes the MediaPipe models.
        /// </summary>
        private void Awake()
        {
            //Use local WebCamTexture.
            if (IsLocallyControlled)
            {
                webCamTexture = WebcamManager.ActiveWebcam;
            }

            if (!gameObject.TryGetComponentOrLog(out ik))
            {
                enabled = false;
                return;
            }
        }

        /// <summary>
        /// Initializes the the instance of <see cref="HandsAnimator"/>,
        /// receives the results from the MediaPipe models and calls the <see cref="HandsAnimator"/> class functions for animation.
        /// </summary>
        private void LateUpdate()
        {
            // Animate only if the avatar is locally controlled.
            if (IsLocallyControlled)
            {
                if (SEEInput.TogglePointing())
                {
                    HandsAnimator.IsPointing = !HandsAnimator.IsPointing;
                }

                // Animate only if the user wishes to use hand animations.
                if (IsUsingHandAnimations)
                {
                    // If it's the first time the user enabled the animations, initialize the <see cref="HandsAnimator"/>.
                    if (IsFirstActivationOfHandAnimations)
                    {
                        HandsAnimator.Initialize(transform, ik);
                        IsFirstActivationOfHandAnimations = false;
                    }

                    // If the avatar's hands are already in the starting position and ready for animation.
                    if (HandsAnimator.BringHandsToStartPositions())
                    {
                        // Needed to flip the image since MediaPipe and Unity handle pixel data differently.
                        textureFrame.ReadTextureOnCPU(webCamTexture, flipHorizontally: true, flipVertically: false);
                        Mediapipe.Image poseLandmarkerImage = textureFrame.BuildCPUImage();

                        poseLandmarker.DetectAsync(poseLandmarkerImage, stopwatch.ElapsedMilliseconds);

                        // Create a stable copy of the MediaPipe result data at one specific moment in time.
                        lock (_lock)
                        {
                            resultPoseLandmarker.CloneTo(ref snapshotResultPoseLandmarker);
                            if (samplingTimesPoseLandmarker.Count > 0)
                            {
                                samplingTimesPoseLandmarkerSnapshot.Add(samplingTimesPoseLandmarker.Last() / 100); // Scale the sampling time for the One Euro Filter.
                                if (samplingTimesPoseLandmarkerSnapshot.Count > 2)
                                {
                                    samplingTimesPoseLandmarkerSnapshot.RemoveAt(0);
                                }
                            }
                        }

                        if (snapshotResultPoseLandmarker.poseWorldLandmarks == null)
                        {
                            poseLandmarksLostFrames++;

                            if (poseLandmarksLostFrames < maxPoseLandmarksLostFrames)
                            {
                                // MediaPipe may occasionally fail to detect pose landmarks even though the user is still visible
                                // in the camera frame. In this case, continue animating using the last detected landmark values
                                // to avoid visual lag or jitter.
                                HandsAnimator.AnimateLastDetectedValuesLeftHand();
                                HandsAnimator.AnimateLastDetectedValuesRightHand();
                            }
                            // Smoothly bring hands to neutral position if there certainly are no pose landmarks to detect
                            // (the user is not in the camera picture).
                            else
                            {
                                if (ik.solver.leftHandEffector.positionWeight > 0.005f || ik.solver.leftArmChain.bendConstraint.weight > 0.005f
                                    || ik.solver.rightHandEffector.positionWeight > 0.005f || ik.solver.rightArmChain.bendConstraint.weight > 0.005f)
                                {
                                    ik.solver.leftHandEffector.positionWeight = Mathf.Lerp(ik.solver.leftHandEffector.positionWeight, 0f, Time.deltaTime * 4);
                                    ik.solver.leftHandEffector.rotationWeight = Mathf.Lerp(ik.solver.leftHandEffector.rotationWeight, 0f, Time.deltaTime * 4);
                                    ik.solver.leftArmChain.bendConstraint.weight = Mathf.Lerp(ik.solver.leftArmChain.bendConstraint.weight, 0f, Time.deltaTime * 4);
                                    ik.solver.rightHandEffector.positionWeight = Mathf.Lerp(ik.solver.rightHandEffector.positionWeight, 0f, Time.deltaTime * 4);
                                    ik.solver.rightHandEffector.rotationWeight = Mathf.Lerp(ik.solver.rightHandEffector.rotationWeight, 0f, Time.deltaTime * 4);
                                    ik.solver.rightArmChain.bendConstraint.weight = Mathf.Lerp(ik.solver.rightArmChain.bendConstraint.weight, 0f, Time.deltaTime * 4);
                                }
                                else
                                {
                                    ik.solver.leftHandEffector.positionWeight = 0f;
                                    ik.solver.leftHandEffector.rotationWeight = 0f;
                                    ik.solver.leftArmChain.bendConstraint.weight = 0f;
                                    ik.solver.rightHandEffector.positionWeight = 0f;
                                    ik.solver.rightHandEffector.rotationWeight = 0f;
                                    ik.solver.rightArmChain.bendConstraint.weight = 0f;
                                }
                            }
                            HandsAnimator.LeftHandTransformState.HandIKPositionWeight = ik.solver.leftHandEffector.positionWeight;
                            HandsAnimator.LeftHandTransformState.HandIKRotationWeight = ik.solver.leftHandEffector.rotationWeight;
                            HandsAnimator.LeftHandTransformState.BendGoalConstraintWeight = ik.solver.leftArmChain.bendConstraint.weight;
                            HandsAnimator.RightHandTransformState.HandIKPositionWeight = ik.solver.rightHandEffector.positionWeight;
                            HandsAnimator.RightHandTransformState.HandIKRotationWeight = ik.solver.rightHandEffector.rotationWeight;
                            HandsAnimator.RightHandTransformState.BendGoalConstraintWeight = ik.solver.rightArmChain.bendConstraint.weight;

                            HandsAnimator.StoreRotationsLeftHand();
                            HandsAnimator.StoreRotationsRightHand();
                            Debug.Log("No pose landmarks found.\n");
                        }
                        else
                        {
                            poseLandmarksLostFrames = 0;

                            // Changing positions of the hands.
                            HandsAnimator.SolveHandsPositions(snapshotResultPoseLandmarker, samplingTimesPoseLandmarkerSnapshot);

                            Mediapipe.Image imageForGestureRecognizer = textureFrame.BuildCPUImage();
                            gestureRecognizer.RecognizeAsync(imageForGestureRecognizer, stopwatch.ElapsedMilliseconds);

                            // Create a stable copy of the MediaPipe result data at one specific moment in time.
                            lock (_lock)
                            {
                                resultGestureRecognizer.CloneTo(ref snapshotResultGestureRecognizer);
                                if (samplingTimesGestureRecognizer.Count > 0)
                                {
                                    samplingTimesGestureRecognizerSnapshot.Add(samplingTimesGestureRecognizer.Last() / 100); // Scale the sampling time for the One Euro Filter.
                                    if (samplingTimesGestureRecognizerSnapshot.Count > 2)
                                    {
                                        samplingTimesGestureRecognizerSnapshot.RemoveAt(0);
                                    }
                                }
                            }

                            if (snapshotResultGestureRecognizer.handLandmarks?.Count > 0)
                            {
                                if (IsRecalibrationNeeded)
                                {
                                    if (areNewHandLandmarks)
                                    {
                                        RecalibrateHandsStartPositions(snapshotResultGestureRecognizer);
                                        areNewHandLandmarks = false;
                                    }
                                }

                                // Rotate hands and fingers.
                                HandsAnimator.SolveLeftHand(snapshotResultGestureRecognizer, samplingTimesGestureRecognizerSnapshot);
                                if (!HandsAnimator.IsPointing)
                                {
                                    HandsAnimator.SolveRightHand(snapshotResultGestureRecognizer, samplingTimesGestureRecognizerSnapshot);
                                }
                                else
                                {
                                    HandsAnimator.StoreRotationsRightHand();
                                    ik.solver.rightHandEffector.positionWeight = 0f;
                                    ik.solver.rightHandEffector.rotationWeight = 0f;
                                    ik.solver.rightArmChain.bendConstraint.weight = 0f;
                                }
                            }
                            else
                            {
                                // Animate the last detected values ​​to avoid lag caused by erroneously undetected landmarks.
                                HandsAnimator.AnimateLastDetectedValuesLeftHand();
                                HandsAnimator.AnimateLastDetectedValuesRightHand();
                                if (Time.time - lastHandLandmarksErrorTime >= handLandmarksErrorCooldown)
                                {
                                    Debug.Log("No hand landmarks found.\n");
                                    lastHandLandmarksErrorTime = Time.time;
                                }
                            }
                        }
                    }
                }
                else
                {
                    ik.solver.leftHandEffector.positionWeight = 0f;
                    ik.solver.rightHandEffector.positionWeight = 0f;
                    ik.solver.leftHandEffector.rotationWeight = 0f;
                    ik.solver.rightHandEffector.rotationWeight = 0f;
                    ik.solver.leftArmChain.bendConstraint.weight = 0f;
                    ik.solver.rightArmChain.bendConstraint.weight = 0f;
                }
            }
        }

        /// <summary>
        /// Toggles between using hand animations with MediaPipe and not using them.
        /// </summary>
        public void ToggleHandAnimations()
        {
            IsUsingHandAnimations = !IsUsingHandAnimations;
            HandsAnimator.IsUsingHandAnimations = !HandsAnimator.IsUsingHandAnimations;

            if (IsUsingHandAnimations)
            {
                WebcamManager.Acquire();
                UIOverlay.ToggleBodyAnimator();
                if (!isMediaPipeInitialized)
                {
                    SetupMediaPipe();
                }
            }
            else
            {
                WebcamManager.Release();
                UIOverlay.ToggleBodyAnimator();
            }

            void SetupMediaPipe()
            {
                // Generate the MediaPipe Tasks by setting options.
                PoseLandmarkerOptions poseLandmarkerOptions = new PoseLandmarkerOptions(
                    baseOptions: new Mediapipe.Tasks.Core.BaseOptions(
                        Mediapipe.Tasks.Core.BaseOptions.Delegate.CPU,
                        modelAssetBuffer: poseLandmarkerModelAsset.bytes),
                    runningMode: Mediapipe.Tasks.Vision.Core.RunningMode.LIVE_STREAM,
                    resultCallback: (PoseLandmarkerResult result, Image image, long timestamp) =>
                    {
                        lock (_lock)
                        {
                            result.CloneTo(ref resultPoseLandmarker);
                            if (isFirstTimeStampPoseLandmarker)
                            {
                                firstTimestampPoseLandmarker = timestamp;
                                isFirstTimeStampPoseLandmarker = false;
                            }
                            samplingTimesPoseLandmarker.Add(timestamp - firstTimestampPoseLandmarker);
                            if (samplingTimesPoseLandmarker.Count > 2)
                            {
                                samplingTimesPoseLandmarker.RemoveAt(0);
                            }
                        }
                    });

                poseLandmarker = PoseLandmarker.CreateFromOptions(poseLandmarkerOptions);

                GestureRecognizerOptions gestureRecognizerOptions = new GestureRecognizerOptions(
                  baseOptions: new Mediapipe.Tasks.Core.BaseOptions(
                    Mediapipe.Tasks.Core.BaseOptions.Delegate.CPU,
                    modelAssetBuffer: gestureRecognizerModelAsset.bytes
                  ),
                  runningMode: Mediapipe.Tasks.Vision.Core.RunningMode.LIVE_STREAM,
                  resultCallback: (GestureRecognizerResult result, Image image, long timestamp) =>
                  {
                      areNewHandLandmarks = true;
                      lock (_lock)
                      {
                          result.CloneTo(ref resultGestureRecognizer);
                          if (isFirstTimeStampGestureRecognizer)
                          {
                             firstTimestampGestureRecognizer = timestamp;
                             isFirstTimeStampGestureRecognizer = false;
                          }
                          samplingTimesGestureRecognizer.Add(timestamp - firstTimestampGestureRecognizer);
                          if (samplingTimesGestureRecognizer.Count > 2)
                          {
                              samplingTimesGestureRecognizer.RemoveAt(0);
                          }
                      }
                  },
                  numHands: 2);

                gestureRecognizer = GestureRecognizer.CreateFromOptions(gestureRecognizerOptions);

                // Start the stopwatch to later calculate timestamps needed by MediaPipe calculators.
                stopwatch.Start();
                textureFrame = new TextureFrame(webCamTexture.width, webCamTexture.height, TextureFormat.RGBA32);
                isMediaPipeInitialized = true;
            }
        }

        /// <summary>
        /// Handles the event of switching to a new webcam.
        /// Resets all MediaPipe and hand animation-related states to ensure
        /// a fresh start for the newly selected camera.
        /// </summary>
        /// <param name="newWebcam">The new <see cref="WebCamTexture"/> that has been selected.
        /// If null or the same as the currently active webcam, no changes are made.
        /// </param>
        private void HandleWebcamChanged(WebCamTexture newWebcam)
        {
            if (newWebcam == null || webCamTexture == newWebcam)
            {
                return;
            }
            IsUsingHandAnimations = false;
            IsFirstActivationOfHandAnimations = true;
            isMediaPipeInitialized = false;
            if (stopwatch.IsRunning)
            {
                stopwatch.Stop();
            }
            webCamTexture = newWebcam;
        }

        /// <summary>
        /// Recalibrates the user's starting hand positions for better hand animations.
        /// </summary>
        /// <param name="gestureRecognizerResult">MediaPipe landmarks being used to set fresh start values for animations.</param>
        public void RecalibrateHandsStartPositions(GestureRecognizerResult gestureRecognizerResult)
        {
            if (HandsAnimator.RecalibrateHandsStartPositions(gestureRecognizerResult))
            {
                IsRecalibrationNeeded = false;
            }
        }
    }
}
