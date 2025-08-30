using UnityEngine;
using RootMotion.FinalIK;
using SEE.GO;

using Stopwatch = System.Diagnostics.Stopwatch;
using Mediapipe.Tasks.Vision.PoseLandmarker;
using Mediapipe.Tasks.Vision.HandLandmarker;
using Mediapipe.Unity.Experimental;
using Mediapipe.Tasks.Vision.GestureRecognizer;



namespace SEE.Game.Avatars
{
    /// <summary>
    /// Animates the hands movements of the avatar.
    /// </summary>
    /// <remarks>This component is assumed to be attached to the avatar's root object.</remarks>
    internal class BodyAnimator : MonoBehaviour
    {
        public HandsAnimator handsAnimator = new HandsAnimator();

        [SerializeField] private TextAsset poseLandmarkerModelAsset;
        [SerializeField] private TextAsset handLandmarkerModelAsset;
        [SerializeField] private TextAsset gestureRecognizerModelAsset;

        private WebCamTexture webCamTexture;

        Stopwatch stopwatch = new Stopwatch();
        TextureFrame textureFrame;
        PoseLandmarker poseLandmarker;
        HandLandmarker handLandmarker;
        GestureRecognizer gestureRecognizer;

        private FullBodyBipedIK ik;

        [Tooltip("If true, local interactions control where the avatar is pointing to.")]
        public bool IsLocallyControlled = true;

        private void Awake()
        {
            if(IsLocallyControlled)
            {
                WebcamManager.Initialize();
                webCamTexture = WebcamManager.WebCamTexture;
            }

            var poseLandmarkerOptions = new PoseLandmarkerOptions(
                baseOptions: new Mediapipe.Tasks.Core.BaseOptions(
                    Mediapipe.Tasks.Core.BaseOptions.Delegate.CPU,
                    modelAssetBuffer: poseLandmarkerModelAsset.bytes),
                runningMode: Mediapipe.Tasks.Vision.Core.RunningMode.VIDEO);

            poseLandmarker = PoseLandmarker.CreateFromOptions(poseLandmarkerOptions);

            var handLandmarkerOptions = new HandLandmarkerOptions(
                baseOptions: new Mediapipe.Tasks.Core.BaseOptions(
                    Mediapipe.Tasks.Core.BaseOptions.Delegate.CPU,
                    modelAssetBuffer: handLandmarkerModelAsset.bytes),
                runningMode: Mediapipe.Tasks.Vision.Core.RunningMode.VIDEO,
                numHands: 2);

            handLandmarker = HandLandmarker.CreateFromOptions(handLandmarkerOptions);

            var gestureRecognizerOptions = new GestureRecognizerOptions(
              baseOptions: new Mediapipe.Tasks.Core.BaseOptions(
                Mediapipe.Tasks.Core.BaseOptions.Delegate.CPU,
                modelAssetBuffer: gestureRecognizerModelAsset.bytes
              ),
              runningMode: Mediapipe.Tasks.Vision.Core.RunningMode.VIDEO,
              numHands: 2
            );

            gestureRecognizer = GestureRecognizer.CreateFromOptions(gestureRecognizerOptions);

            stopwatch.Start();
            textureFrame = new Mediapipe.Unity.Experimental.TextureFrame(webCamTexture.width, webCamTexture.height, TextureFormat.RGBA32);

            if (!gameObject.TryGetComponentOrLog(out ik))
            {
                enabled = false;
                return;
            }
            else if(IsLocallyControlled)
            {
                handsAnimator.initialize(transform, ik);
            }
        }



        private void LateUpdate()
        {
            /// <summary>
            /// Animating the movement of the hands with Full Body Biped IK and MediaPipe Landmarks 
            /// by rotating hands and changing positions coordinates.
            /// </summary>
            if (IsLocallyControlled)
            {
                if (handsAnimator.bringHandsToStartPositions())
                {
                    textureFrame.ReadTextureOnCPU(webCamTexture, flipHorizontally: true, flipVertically: false);
                    using var poseLandmarkerImage = textureFrame.BuildCPUImage();

                    var resultPoseLandmarker = poseLandmarker.DetectForVideo(poseLandmarkerImage, stopwatch.ElapsedMilliseconds);

                    if (resultPoseLandmarker.poseWorldLandmarks == null)
                    {
                        UnityEngine.Debug.Log("No Pose Landmarks found");
                    }
                    else
                    {
                        /// <summary>
                        /// Changing positions of the hands with MediaPipe Pose Landmarker model
                        /// and Full Body Biped IK.
                        /// </summary>
                        handsAnimator.solveHandsPositions(resultPoseLandmarker);

                        /// <summary>
                        /// Rotation of hands and fingers based on the
                        /// MediaPipe hands landmakrs and gestures detected with
                        /// MediaPipe Gesture Recognizer task.
                        /// </summary>
                        var imageForHandLandmarker = textureFrame.BuildCPUImage();
                        var resultHandLandmarker = handLandmarker.DetectForVideo(imageForHandLandmarker, stopwatch.ElapsedMilliseconds);

                        textureFrame.ReadTextureOnCPU(webCamTexture, flipHorizontally: false, flipVertically: true);
                        var imageForGestureRecognizer = textureFrame.BuildCPUImage();
                        var resultGestureRecognizer = gestureRecognizer.RecognizeForVideo(imageForGestureRecognizer, stopwatch.ElapsedMilliseconds);

                        if (resultHandLandmarker.handLandmarks?.Count > 0)
                        {
                            handsAnimator.solveLeftHand(resultHandLandmarker, resultGestureRecognizer, resultPoseLandmarker);
                            handsAnimator.solveRightHand(resultHandLandmarker, resultGestureRecognizer, resultPoseLandmarker);
                        }
                        else
                        {
                            Debug.Log("No hand landmarks found");
                        }
                    }
                }
            }
        }
    }
}
