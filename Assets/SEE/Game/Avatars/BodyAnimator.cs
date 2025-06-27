using UnityEngine;
using RootMotion.FinalIK;
using SEE.GO;

using Stopwatch = System.Diagnostics.Stopwatch;
using Mediapipe.Tasks.Vision.PoseLandmarker;
using Mediapipe.Unity;
using Mediapipe;
using Mediapipe.Unity.Experimental;
using System;

namespace SEE.Game.Avatars
{
    /// <summary>
    /// Animates the hands of the avatar.
    /// </summary>
    /// <remarks>This component is assumed to be attached to the avatar's root object.</remarks>
    internal class BodyAnimator : MonoBehaviour
    {
        [SerializeField] private TextAsset poseLandmarkerModelAsset;

        private WebCamTexture webCamTexture;

        Stopwatch stopwatch = new Stopwatch();
        TextureFrame textureFrame;
        PoseLandmarker poseLandmarker;

        private FullBodyBipedIK ik;

        private const string leftHandName = "CC_Base_BoneRoot/CC_Base_Hip/CC_Base_Waist/CC_Base_Spine01/CC_Base_Spine02/CC_Base_L_Clavicle/CC_Base_L_Upperarm/CC_Base_L_Forearm/CC_Base_L_Hand";
        private const string leftShoulderName = "CC_Base_BoneRoot/CC_Base_Hip/CC_Base_Waist/CC_Base_Spine01/CC_Base_Spine02/CC_Base_L_Clavicle/CC_Base_L_Upperarm";
        private const string headName = "CC_Base_BoneRoot/CC_Base_Hip/CC_Base_Waist/CC_Base_Spine01/CC_Base_Spine02/CC_Base_NeckTwist01/CC_Base_NeckTwist02/CC_Base_Head";

        public float weight = 1f;

        private Vector3 currentLeftHandPosition = Vector3.one;
        private Vector3 headPosition = Vector3.one;
        private Vector3 leftShoulderPosition = Vector3.one;

        private Quaternion startLeftHandRotation;
        public Quaternion currentLeftHandRotation;

        private const float moveSpeed = 0.5f;
        private const float arrivalThreshold = 0.005f;
        private bool firstPoseLandmark = true;
        Vector3 previousHandMediapipeCoordinates = new Vector3(0f, 0f, 0f);
        Vector3 newHandMediapipeCoordinates = new Vector3(0f, 0f, 0f);

        private float AcceptableHandPresenceProbability = 0.5f;
        private float HandYRotationInFronOfTheCharacter = 235f;
        private float HandYRotationMovementToTheSide = 70f;
        private Tuple<float, float> HandZRotationIntervalMovementDown = Tuple.Create(350f, 60f);
        private Tuple<float, float> HandXCoordinatesDiffIntervalToFaceTheCamera = Tuple.Create(-0.47f, -0.25f);
        private Tuple<float, float> HandXCoordinatesDiffIntervalMovingInFront = Tuple.Create(-0.25f, 0.28f);
        private float HandYCoordinatesDiffToMoveDownFrom = -0.25f;

        private void Awake()
        {
            if (webCamTexture == null)
            {
                UnityEngine.Debug.Log("WebCamTexture is not initialized yet.");
                WebcamManager.Initialize();
            }

            webCamTexture = WebcamManager.SharedWebCamTexture;

            var poseLandmarkerOptions = new PoseLandmarkerOptions(
                baseOptions: new Mediapipe.Tasks.Core.BaseOptions(
                    Mediapipe.Tasks.Core.BaseOptions.Delegate.CPU,
                    modelAssetBuffer: poseLandmarkerModelAsset.bytes),
                runningMode: Mediapipe.Tasks.Vision.Core.RunningMode.VIDEO);

            poseLandmarker = PoseLandmarker.CreateFromOptions(poseLandmarkerOptions);
            stopwatch.Start();
            textureFrame = new Mediapipe.Unity.Experimental.TextureFrame(webCamTexture.width, webCamTexture.height, TextureFormat.RGBA32);

            if (!gameObject.TryGetComponentOrLog(out ik))
            {
                enabled = false;
                return;
            }
            {
                Transform leftHandBone = transform.Find(leftHandName);
                Transform leftShoulderBone = transform.Find(leftShoulderName);
                Transform headBone = transform.Find(headName);

                if (leftHandBone == null)
                {
                    UnityEngine.Debug.LogError($"Left hand bone not found: {leftHandName}");
                    enabled = false;
                    return;
                }
                leftShoulderPosition = leftShoulderBone.position;
                currentLeftHandPosition = leftHandBone.position;
                startLeftHandRotation = leftHandBone.rotation;
                currentLeftHandRotation = leftHandBone.rotation;

                headPosition = headBone.position;
            }
            ik.solver.leftHandEffector.positionWeight = weight;
            ik.solver.leftHandEffector.rotationWeight = weight;
        }


        private void LateUpdate()
        {
            /// <summary>
            /// Animating the movement of the left hand with Full Body Biped IK and Mediapipe Landmarks 
            /// by rotating the hand and changing the position coordinates
            /// </summary>
            /// 
            Quaternion leftHandRotationOffset = Quaternion.Euler(90, 90, 0);
            Quaternion leftHandTargetRotation = startLeftHandRotation * leftHandRotationOffset;

            var leftHandPositionOffset = new Vector3(0.45f, 0.05f, 0.2f);
            Vector3 leftHandTargetPos = leftShoulderPosition + leftHandPositionOffset;


            if (Vector3.Distance(currentLeftHandPosition, leftHandTargetPos) >= arrivalThreshold)
            {
                currentLeftHandPosition = Vector3.Lerp(currentLeftHandPosition, leftHandTargetPos, Time.deltaTime * moveSpeed * 4);
                currentLeftHandRotation = Quaternion.Slerp(currentLeftHandRotation, leftHandTargetRotation, Time.deltaTime * moveSpeed * 4);
                ik.solver.leftHandEffector.rotation = currentLeftHandRotation;
                ik.solver.leftHandEffector.position = currentLeftHandPosition;

                ik.solver.leftHandEffector.positionWeight = weight;
                ik.solver.leftHandEffector.rotationWeight = weight;
            }
            else
            {

                Transform leftHandBone = transform.Find(leftHandName);

                textureFrame.ReadTextureOnCPU(webCamTexture, flipHorizontally: true, flipVertically: false);
                using var poseLandmarkerImage = textureFrame.BuildCPUImage();

                var resultPoseLandmarker = poseLandmarker.DetectForVideo(poseLandmarkerImage, stopwatch.ElapsedMilliseconds);
                if (resultPoseLandmarker.poseWorldLandmarks?.Count <= 0)
                {
                    UnityEngine.Debug.Log("No Pose Landmarks found");
                }
                else
                {
                    var poseLandmarks = resultPoseLandmarker.poseWorldLandmarks[0].landmarks;
                    var mediapipeLeftHandPosition = poseLandmarks[15];

                    if (firstPoseLandmark)
                    {
                        newHandMediapipeCoordinates.x = mediapipeLeftHandPosition.x;
                        newHandMediapipeCoordinates.y = mediapipeLeftHandPosition.y;
                        newHandMediapipeCoordinates.z = mediapipeLeftHandPosition.z;
                        firstPoseLandmark = false;
                    }

                    var mediapipeHeadPosition = poseLandmarks[0];

                    previousHandMediapipeCoordinates = newHandMediapipeCoordinates;
                    newHandMediapipeCoordinates.x = mediapipeLeftHandPosition.x;
                    newHandMediapipeCoordinates.y = mediapipeLeftHandPosition.y;
                    newHandMediapipeCoordinates.z = mediapipeLeftHandPosition.z;

                    var leftHandToHeadCoordinateDifference = new Vector3(mediapipeLeftHandPosition.x - mediapipeHeadPosition.x, mediapipeLeftHandPosition.y - mediapipeHeadPosition.y, currentLeftHandPosition.x - headPosition.x);

                    if (mediapipeLeftHandPosition.presence > AcceptableHandPresenceProbability)
                    {
                        var newHandPosition = transform.InverseTransformPoint(headPosition) + leftHandToHeadCoordinateDifference;
                        ik.solver.leftHandEffector.position = transform.TransformPoint(newHandPosition);

                        //interval where palm should be facing the camera
                        if (leftHandToHeadCoordinateDifference.x < HandXCoordinatesDiffIntervalToFaceTheCamera.Item2 && leftHandToHeadCoordinateDifference.x > HandXCoordinatesDiffIntervalToFaceTheCamera.Item1)
                        {
                            leftHandTargetRotation = startLeftHandRotation * leftHandRotationOffset;
                            currentLeftHandRotation = Quaternion.Slerp(currentLeftHandRotation, leftHandTargetRotation, Time.deltaTime * moveSpeed * 10);
                            ik.solver.leftHandEffector.rotation = currentLeftHandRotation;
                        }
                        //there if the hand is moving to the right, in front of the character
                        else if (leftHandToHeadCoordinateDifference.x >= HandXCoordinatesDiffIntervalMovingInFront.Item1 
                            && leftHandToHeadCoordinateDifference.x <= HandXCoordinatesDiffIntervalMovingInFront.Item2)
                        {
                            leftHandTargetRotation = currentLeftHandRotation * Quaternion.Euler(0, HandYRotationInFronOfTheCharacter - currentLeftHandRotation.eulerAngles.y, 0);
                            if (ik.solver.leftHandEffector.rotation.eulerAngles.y < HandYRotationInFronOfTheCharacter)
                            {
                                currentLeftHandRotation = Quaternion.Slerp(currentLeftHandRotation, leftHandTargetRotation, Time.deltaTime * moveSpeed * 10);
                                ik.solver.leftHandEffector.rotation = currentLeftHandRotation;
                                ik.solver.leftHandEffector.rotationWeight = weight;
                            }
                        }
                        //moving to the side, not in front of the character
                        else if (previousHandMediapipeCoordinates.x > newHandMediapipeCoordinates.x)
                        {
                            leftHandTargetRotation = currentLeftHandRotation * Quaternion.Euler(0, HandYRotationMovementToTheSide - currentLeftHandRotation.eulerAngles.y, 0);
                            if (ik.solver.leftHandEffector.rotation.eulerAngles.y >= HandYRotationMovementToTheSide)
                            {
                                currentLeftHandRotation = Quaternion.Slerp(currentLeftHandRotation, leftHandTargetRotation, Time.deltaTime * moveSpeed * 5);
                                ik.solver.leftHandEffector.rotation = currentLeftHandRotation;
                                ik.solver.leftHandEffector.rotationWeight = weight;
                            }
                        }
                        //moving the hand down
                        if (leftHandToHeadCoordinateDifference.y <= HandYCoordinatesDiffToMoveDownFrom)
                        {
                            leftHandTargetRotation = currentLeftHandRotation * Quaternion.Euler(0, 0, HandZRotationIntervalMovementDown.Item2 - currentLeftHandRotation.eulerAngles.z);
                            if (ik.solver.leftHandEffector.rotation.eulerAngles.z >= HandZRotationIntervalMovementDown.Item1 
                                || ik.solver.leftHandEffector.rotation.eulerAngles.z <= HandZRotationIntervalMovementDown.Item2)
                            {
                                currentLeftHandRotation = Quaternion.Slerp(currentLeftHandRotation, leftHandTargetRotation, Time.deltaTime * moveSpeed * 10);
                                ik.solver.leftHandEffector.rotation = currentLeftHandRotation;
                                ik.solver.leftHandEffector.rotationWeight = weight;
                            }
                        }
                    }
                }
            }
        }
    }
}
