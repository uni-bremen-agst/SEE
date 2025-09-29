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
        public HandsAnimator HandsAnimator = new HandsAnimator();

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
        private Stopwatch stopwatch = new Stopwatch();

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
        /// Initializes the MediaPipe models and the instance of <see cref="HandsAnimator"/>.
        /// </summary>
        private void Awake()
        {
            //Use local WebCamTexture.
            if (IsLocallyControlled)
            {
                WebcamManager.Initialize();
                webCamTexture = WebcamManager.WebCamTexture;
            }

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

            //Start the stopwatch to later calculate timestamps needed by MediaPipe calculators.
            stopwatch.Start();
            textureFrame = new Mediapipe.Unity.Experimental.TextureFrame(webCamTexture.width, webCamTexture.height, TextureFormat.RGBA32);

            if (!gameObject.TryGetComponentOrLog(out ik))
            {
                enabled = false;
                return;
            }
            else if (IsLocallyControlled)
            {
                HandsAnimator.Initialize(transform, ik);
            }
        }

        /// <summary>
        /// Receives the results from the MediaPipe models and calls the <see cref="HandsAnimator"/> class functions for animation.
        /// </summary>
        private void LateUpdate()
        {
            //Animate with this script only if the avatar is locally controlled.
            if (IsLocallyControlled)
            {
                // If the avatar's hands are already in the starting position and ready for animation.
                if (HandsAnimator.BringHandsToStartPositions())
                {
                    // Needed to flip the image since MediaPipe and Unity handle pixel data differently.
                    textureFrame.ReadTextureOnCPU(webCamTexture, flipHorizontally: true, flipVertically: false);
                    Mediapipe.Image poseLandmarkerImage = textureFrame.BuildCPUImage();

                    PoseLandmarkerResult resultPoseLandmarker = poseLandmarker.DetectForVideo(poseLandmarkerImage, stopwatch.ElapsedMilliseconds);

                    if (resultPoseLandmarker.poseWorldLandmarks == null)
                    {
                        Debug.Log("No Pose Landmarks found" + "\n");
                    }
                    else
                    {
                        //Changing positions of the hands.
                        HandsAnimator.SolveHandsPositions(resultPoseLandmarker);

                        Mediapipe.Image imageForHandLandmarker = textureFrame.BuildCPUImage();
                        HandLandmarkerResult resultHandLandmarker = handLandmarker.DetectForVideo(imageForHandLandmarker, stopwatch.ElapsedMilliseconds);

                        textureFrame.ReadTextureOnCPU(webCamTexture, flipHorizontally: false, flipVertically: true);
                        Mediapipe.Image imageForGestureRecognizer = textureFrame.BuildCPUImage();
                        GestureRecognizerResult resultGestureRecognizer = gestureRecognizer.RecognizeForVideo(imageForGestureRecognizer, stopwatch.ElapsedMilliseconds);

                        if (resultHandLandmarker.handLandmarks?.Count > 0)
                        {
                            //Rotate hands and fingers.
                            HandsAnimator.SolveLeftHand(resultHandLandmarker, resultGestureRecognizer, resultPoseLandmarker);
                            HandsAnimator.SolveRightHand(resultHandLandmarker, resultGestureRecognizer, resultPoseLandmarker);
                        }
                        else
                        {
                            Debug.Log("No hand landmarks found" + "\n");
                        }
                    }
                }
            }
        }
    }
}
