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

using UnityEngine;
using RootMotion.FinalIK;
using SEE.GO;
using SEE.Controls;
using SEE.Utils;

/// <summary>
/// These namespaces are imported to be able to use MediaPipe solutions
/// </summary>
using Stopwatch = System.Diagnostics.Stopwatch;
using Mediapipe.Tasks.Vision.PoseLandmarker;
using Mediapipe.Tasks.Vision.HandLandmarker;
using Mediapipe.Unity.Experimental;
using Mediapipe.Tasks.Vision.GestureRecognizer;

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
        [SerializeField] private TextAsset handLandmarkerModelAsset;
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
        /// Solvers from MediaPipe that are used to detect pose and hand landmarks.
        /// </summary>
        private PoseLandmarker poseLandmarker;
        private HandLandmarker handLandmarker;

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
        private bool isUsingHandAnimations = false;

        /// <summary>
        /// If true, the user enabled hand animations using MediaPipe for the first time.
        /// </summary>
        public bool IsFirstActivationOfHandHanimations = true;

        /// <summary>
        /// Time in seconds when the last error message indicating that no hand landmarks were found was shown.
        /// </summary>
        /// <remarks>Start negative so first error can appear immediatly.</remarks>
        private float lastHandLandmarksErrorTime = -5f;

        /// <summary>
        /// Time interval (in seconds) between error messages.
        /// </summary>
        private const float handLandmarksErrorCooldown = 5f;

        /// <summary>
        /// Indicates whether the MediaPipe values are setted.
        /// </summary>
        private bool isMediaPipeInitialized = false;

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
            if (WebcamManager.WebCamTexture != null)
            {
                HandleWebcamChanged(WebcamManager.WebCamTexture);
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
                webCamTexture = WebcamManager.WebCamTexture;
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
            if (SEEInput.ToggleHandAnimations())
            {
                ToggleHandAnimations();
            }

            if (SEEInput.TogglePointing())
            {
                HandsAnimator.IsPointing = !HandsAnimator.IsPointing;
            }

            // Animate only if the avatar is locally controlled.
            if (IsLocallyControlled)
            {
                // Animate only if the user wishes to use hand animations.
                if (isUsingHandAnimations)
                {
                    // If it's the first time the user enabled the animations, initialize the HandsAnimator.
                    if (IsFirstActivationOfHandHanimations)
                    {
                        HandsAnimator.Initialize(transform, ik);
                        IsFirstActivationOfHandHanimations = false;
                    }

                    // If the avatar's hands are already in the starting position and ready for animation.
                    if (HandsAnimator.BringHandsToStartPositions())
                    {
                        // Needed to flip the image since MediaPipe and Unity handle pixel data differently.
                        textureFrame.ReadTextureOnCPU(webCamTexture, flipHorizontally: true, flipVertically: false);
                        Mediapipe.Image poseLandmarkerImage = textureFrame.BuildCPUImage();

                        PoseLandmarkerResult resultPoseLandmarker = poseLandmarker.DetectForVideo(poseLandmarkerImage, stopwatch.ElapsedMilliseconds);

                        if (resultPoseLandmarker.poseWorldLandmarks == null)
                        {
                            Debug.Log("No pose landmarks found.\n");
                        }
                        else
                        {
                            // Changing positions of the hands.
                            HandsAnimator.SolveHandsPositions(resultPoseLandmarker);

                            Mediapipe.Image imageForHandLandmarker = textureFrame.BuildCPUImage();
                            HandLandmarkerResult resultHandLandmarker = handLandmarker.DetectForVideo(imageForHandLandmarker, stopwatch.ElapsedMilliseconds);

                            textureFrame.ReadTextureOnCPU(webCamTexture, flipHorizontally: false, flipVertically: true);
                            Mediapipe.Image imageForGestureRecognizer = textureFrame.BuildCPUImage();
                            GestureRecognizerResult resultGestureRecognizer = gestureRecognizer.RecognizeForVideo(imageForGestureRecognizer, stopwatch.ElapsedMilliseconds);

                            if (resultHandLandmarker.handLandmarks?.Count > 0)
                            {
                                // Rotate hands and fingers.
                                HandsAnimator.SolveLeftHand(resultHandLandmarker, resultGestureRecognizer, resultPoseLandmarker);
                                HandsAnimator.SolveRightHand(resultHandLandmarker, resultGestureRecognizer, resultPoseLandmarker);
                            }
                            else
                            {
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
                }
            }
        }

        /// <summary>
        /// Toggles between using hand animations with MediaPipe and not using them.
        /// </summary>
        private void ToggleHandAnimations()
        {
            isUsingHandAnimations = !isUsingHandAnimations;
            if (isUsingHandAnimations)
            {
                WebcamManager.Acquire();
                if (!isMediaPipeInitialized)
                {
                    SetupMediaPipe();
                }
            }
            else
            {
                WebcamManager.Release();
            }

            void SetupMediaPipe()
            {
                // Generate the MediaPipe Tasks by setting options.
                PoseLandmarkerOptions poseLandmarkerOptions = new PoseLandmarkerOptions(
                    baseOptions: new Mediapipe.Tasks.Core.BaseOptions(
                        Mediapipe.Tasks.Core.BaseOptions.Delegate.CPU,
                        modelAssetBuffer: poseLandmarkerModelAsset.bytes),
                    runningMode: Mediapipe.Tasks.Vision.Core.RunningMode.VIDEO);

                poseLandmarker = PoseLandmarker.CreateFromOptions(poseLandmarkerOptions);

                HandLandmarkerOptions handLandmarkerOptions = new HandLandmarkerOptions(
                    baseOptions: new Mediapipe.Tasks.Core.BaseOptions(
                        Mediapipe.Tasks.Core.BaseOptions.Delegate.CPU,
                        modelAssetBuffer: handLandmarkerModelAsset.bytes),
                    runningMode: Mediapipe.Tasks.Vision.Core.RunningMode.VIDEO,
                    numHands: 2);

                handLandmarker = HandLandmarker.CreateFromOptions(handLandmarkerOptions);

                GestureRecognizerOptions gestureRecognizerOptions = new GestureRecognizerOptions(
                  baseOptions: new Mediapipe.Tasks.Core.BaseOptions(
                    Mediapipe.Tasks.Core.BaseOptions.Delegate.CPU,
                    modelAssetBuffer: gestureRecognizerModelAsset.bytes
                  ),
                  runningMode: Mediapipe.Tasks.Vision.Core.RunningMode.VIDEO,
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
        /// If <c>null</c> or the same as the currently active webcam, no changes are made.
        /// </param>
        private void HandleWebcamChanged(WebCamTexture newWebcam)
        {
            if (newWebcam == null || webCamTexture == newWebcam)
            {
                return;
            }
            isUsingHandAnimations = false;
            IsFirstActivationOfHandHanimations = true;
            isMediaPipeInitialized = false;
            if (stopwatch.IsRunning)
            {
                stopwatch.Stop();
            }
            webCamTexture = newWebcam;
        }
    }
}
