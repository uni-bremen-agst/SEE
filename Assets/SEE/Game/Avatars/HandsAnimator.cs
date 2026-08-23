using Mediapipe.Tasks.Components.Containers;
using Mediapipe.Tasks.Vision.GestureRecognizer;
using Mediapipe.Tasks.Vision.PoseLandmarker;
using RootMotion.FinalIK;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.Game.Avatars
{
    /// <summary>
    /// Uses output from MediaPipe models to animate the avatar's hand and finger movements.
    /// </summary>
    internal class HandsAnimator
    {
        /// <summary>
        /// Main transform of the avatar.
        /// </summary>
        private Transform transform;

        /// <summary>
        /// The FullBodyBiped IK solver attached to the avatar.
        /// </summary>
        private FullBodyBipedIK ik;

        /// <summary>
        /// HandTransformState instances for left and right hand which are used to store
        /// the status of the current rotations and positions of the hands and fingers
        /// as well as other information required for animations.
        /// </summary>
        public HandTransformState LeftHandTransformState = new();
        public HandTransformState RightHandTransformState = new();

        /// <summary>
        /// Solver for calculating rotations.
        /// </summary>
        private readonly HandRotationsSolver rotationSolver = new();

        /// <summary>
        /// If true, the avatar's laser pointer is enabled.
        /// </summary>
        public bool IsPointing = true;

        /// <summary>
        /// If true, the owner of the avatar is using hand animations with MediaPipe.
        /// </summary>
        public bool IsUsingHandAnimations = false;

        /// <summary>
        /// If true, the HandsAnimator of the avatar is initialized.
        /// </summary>
        public bool IsHandsAnimatorInitialized = false;

        /// <summary>
        /// The weight that determines the level of influence of changes in the IK effectors of the hands on other bones in the chain.
        /// </summary>
        private const float weight = 1f;

        /// <summary>
        /// Animation speed of hand position changes and rotations.
        /// </summary>
        private const float moveSpeed = 2f;

        /// <summary>
        /// Threshold for considering the avatar's hands to have reached their start positions and are ready for live animation.
        /// </summary>
        private const float arrivalThreshold = 0.01f;

        /// <summary>
        /// The probability with which the presence of a hand in the camera can be considered acceptable for animation.
        /// </summary>
        private const float acceptableHandPresenceProbability = 0.7f;

        /// <summary>
        /// If true, the avatar's hands to have reached their start positions and are ready for live animation.
        /// </summary>
        public bool StartHandsPositionReached { get; private set; } = false;

        /// <summary>
        /// If true, no pose landmarks have been detected yet.
        /// </summary>
        private bool isFirstPoseLandmark = true;

        /// <summary>
        /// The position of the avatar's head in the scene relative to the main transform.
        /// </summary>
        private Vector3 headPosition = Vector3.one;

        /// <summary>
        /// Starting values ​​of positions and rotations of the hands before bringing them to the predefined start position.
        /// </summary>
        /// <remarks>By start position it is meant that the avatar's hands are in front of the avatar,
        /// bent at the elbows and the palms are facing forward.</remarks>
        private Vector3 leftHandStartPos = Vector3.zero;
        private Vector3 rightHandStartPos = Vector3.zero;
        private Quaternion startLeftHandRotation;
        private Quaternion startRightHandRotation;

        /// <summary>
        /// Rotations and positions that should be assigned to hands.
        /// </summary>
        private Quaternion leftHandTargetRotation;
        private Quaternion rightHandTargetRotation;
        private Vector3 leftHandTargetPos;
        private Vector3 rightHandTargetPos;

        /// <summary>
        /// The values by which the positions and rotations of the hands must be changed at the beginning to reach the start position.
        /// </summary>
        /// <remarks>By start position it is meant that the avatar's hands are in front of the avatar, bent at the elbows and the palms are facing forward.</remarks>
        private Quaternion leftHandRotationOffset = Quaternion.Euler(170, 100, 0);
        private Vector3 leftHandPositionOffset = new(-0.37f, 1.56f, 0.23f);
        private Quaternion rightHandRotationOffset = Quaternion.Euler(10, 70, 170);
        private Vector3 rightHandPositionOffset = new(0.37f, 1.56f, 0.23f);

        /// <summary>
        /// The interval at which the avatar's palm should face the camera (the values ​​are the difference in coordinates between the hand and the head).
        /// </summary>
        private readonly Tuple<float, float> handXCoordinatesDiffIntervalToFaceTheCamera = Tuple.Create(-0.47f, -0.15f);

        /// <summary>
        /// The interval at which the avatar's palm should be slightly rotated to avoid unnatural animations
        /// when moving the hand in front of the avatar's body (the values ​​are the difference in coordinates between the hand and the head).
        /// </summary>
        private readonly Tuple<float, float> handXCoordinatesDiffIntervalMovingInFront = Tuple.Create(-0.15f, 0.28f);

        /// <summary>
        /// The difference in the y-coordinate between the hand and the head,
        /// from which it can be assumed that the hand is moving downwards and therefore
        /// should be slightly rotated to avoid unnatural animations.
        /// </summary>
        private const float handYCoordinatesDiffToMoveDownFrom = -0.3f;

        /// <summary>
        /// List of hand landmarks from Mediapipe.
        /// </summary>
        private readonly MediaPipeHandLandmarks handLandmarks = new();

        /// <summary>
        /// Tracks the number of frames in which right hand is not detected by MediaPipe.
        /// Used to determine when a hand has been lost for too long and should be reset to a neutral position.
        /// </summary>
        private int rightHandLostFrames = 0;

        /// <summary>
        /// Number of frames the right hand has been continuously detected.
        /// Used to ensure that the presence of the hand in the camera picture is stable before animating the avatar.
        /// </summary>
        private int rightHandDetectedFrames = 0;

        /// <summary>
        /// Tracks the number of frames in which left hand is not detected by MediaPipe.
        /// Used to determine when a hand has been lost for too long and should be reset to a neutral position.
        private int leftHandLostFrames = 0;

        /// <summary>
        /// Number of frames the left hand has been continuously detected.
        /// Used to ensure that the presence of the hand in the camera picture is stable before animating the avatar.
        /// </summary>
        private int leftHandDetectedFrames = 0;

        /// <summary>
        /// Maximum number of lost frames allowed before assigning a neutral hand position
        /// to the avatar.
        /// </summary>
        private const int maxLostFrames = 100;

        /// <summary>
        /// Minimum number of consecutive detected frames required before a hand presence is considered valid
        /// and movement is animated.
        /// </summary>
        private const int minDetectedFrames = 50;

        /// <summary>
        /// Number of frames the left hand gesture has been continuously detected.
        /// Used to ensure the gesture is stable before it is applied to the avatar.
        /// </summary>
        private int leftHandGestureDetectedFrames = 0;

        /// <summary>
        /// Number of consecutive frames the left hand gesture has not been detected.
        /// Used to determine when to stop applying the gesture animation.
        /// </summary>
        private int leftHandGestureLostFrames = 0;

        /// <summary>
        /// Indicates whether a gesture was successfully recognized for the left hand by the MediaPipe <see cref="GestureRecognizer"/> model.
        /// If a gesture is recognized, the hand movement animation is applied and no neutral hand pose is assigned.
        /// This prevents the hand from being reset to a neutral position in cases where the <see cref="PoseLandmarker"/> model fails
        /// to correctly detect the hand landmarks during thumb up or thumb down gestures.
        /// </summary>
        private bool wasLeftHandGestureRecognized = false;

        /// <summary>
        /// Number of frames the right hand gesture has been continuously detected.
        /// Used to ensure the gesture is stable before it is applied to the avatar.
        /// </summary>
        private int rightHandGestureDetectedFrames = 0;

        /// <summary>
        /// Number of consecutive frames the right hand gesture has not been detected.
        /// Used to determine when to stop applying the gesture animation.
        /// </summary>
        private int rightHandGestureLostFrames = 0;

        /// <summary>
        /// Indicates whether a gesture was successfully recognized for the right hand by the MediaPipe <see cref="GestureRecognizer"/> model.
        /// If a gesture is recognized, the hand movement animation is applied and no neutral hand pose is assigned.
        /// This prevents the hand from being reset to a neutral position in cases where the <see cref="PoseLandmarker"/> model fails
        /// to correctly detect the hand landmarks during thumb up or thumb down gestures.
        /// </summary>
        private bool wasRightHandGestureRecognized = false;

        /// <summary>
        /// Minimum number of consecutive detected frames required before a gesture is considered valid
        /// and applied to the avatar.
        /// </summary>
        private const int minGestureDetectedFrames = 50;

        /// <summary>
        /// Maximum number of consecutive lost frames allowed before a gesture is considered stopped
        /// and the animation of the gesture is disabled.
        /// </summary>
        private const int maxGestureLostFames = 150;

        /// <summary>
        /// Indicates whether the first set of detected left hand landmark coordinates has been received.
        /// Used to initialize filters for stable tracking.
        /// </summary>
        private bool areFirstDetectedHandCoordinatesLeftHand = true;

        /// <summary>
        /// Indicates whether the first set of detected right hand landmark coordinates has been received.
        /// Used to initialize filters for stable tracking.
        /// </summary>
        private bool areFirstDetectedHandCoordinatesRightHand = true;

        /// <summary>
        /// One Euro Filters applied to positions of the left and right hand relative to the head.
        /// Used to smooth hands positions over time.
        /// </summary>
        private (OneEuroFilter leftHandPositionFilter, OneEuroFilter rightHandPositionFilter) handsPositionsFilters = (new OneEuroFilter(), new OneEuroFilter());

        /// <summary>
        /// One Euro Filters applied to each landmark of the left hand.
        /// Used to smooth raw MediaPipe landmarks over time.
        /// </summary>
        private List<OneEuroFilter> leftHandLandmarksFilters = new List<OneEuroFilter>();

        /// <summary>
        /// Smoothed (filtered) landmark positions of the left hand.
        /// </summary>
        private List<Vector3> filteredLeftHandLandmarks = new List<Vector3>();

        /// <summary>
        /// One Euro Filters applied to each landmark of the right hand.
        /// Used to smooth raw MediaPipe landmarks over time.
        /// </summary>
        private List<OneEuroFilter> rightHandLandmarksFilters = new List<OneEuroFilter>();

        /// <summary>
        /// Smoothed (filtered) landmark positions of the right hand.
        /// </summary>
        private List<Vector3> filteredRightHandLandmarks = new List<Vector3>();

        /// <summary>
        /// Initializes the initial positions of the hands and the head, the main avatar transform,
        /// the ik component, and also adds Bend Goals for the elbows.
        /// </summary>
        /// <param name="mainTrasform">The main transform of the avatar.</param>
        /// <param name="ikComponent">The FullBodyBiped IK solver attached to the avatar.</param>
        public void Initialize(Transform mainTrasform, FullBodyBipedIK ikComponent)
        {
            this.ik = ikComponent;
            this.transform = mainTrasform;

            Transform headBone = mainTrasform.Find(AvatarSceleton.Head);
            Transform leftHandBone = mainTrasform.Find(AvatarSceleton.LeftHand);
            Transform rightHandBone = mainTrasform.Find(AvatarSceleton.RightHand);
            if (headBone == null)
            {
                Debug.LogError($"Head bone not found: {AvatarSceleton.Head}\n");
                return;
            }
            else if (leftHandBone == null)
            {
                Debug.LogError($"Left hand bone not found: {AvatarSceleton.LeftHand}\n");
                return;
            }
            else if (rightHandBone == null)
            {
                Debug.LogError($"Right hand bone not found: {AvatarSceleton.RightHand}\n");
                return;
            }

            // Save information about the current position and rotation of the left hand.
            LeftHandTransformState.HandPosition = leftHandBone.position;
            LeftHandTransformState.HandRotation = leftHandBone.rotation;
            startLeftHandRotation = leftHandBone.localRotation;

            // Save information about the current position and rotation of the right hand.
            RightHandTransformState.HandPosition = rightHandBone.position;
            RightHandTransformState.HandRotation = rightHandBone.rotation;
            startRightHandRotation = rightHandBone.localRotation;

            headPosition = mainTrasform.InverseTransformPoint(headBone.position);

            ik.solver.leftHandEffector.positionWeight = weight;
            ik.solver.leftHandEffector.rotationWeight = weight;
            LeftHandTransformState.HandIKRotationWeight = ik.solver.leftHandEffector.rotationWeight;
            LeftHandTransformState.HandIKPositionWeight = ik.solver.leftHandEffector.positionWeight;

            ik.solver.rightHandEffector.positionWeight = weight;
            ik.solver.rightHandEffector.rotationWeight = weight;
            RightHandTransformState.HandIKRotationWeight = ik.solver.rightHandEffector.rotationWeight;
            RightHandTransformState.HandIKPositionWeight = ik.solver.rightHandEffector.positionWeight;

            // Add bend goals for the elbows so they bend downwards.
            GameObject leftElbowBendGoal = new("LeftElbowBendGoal");
            leftElbowBendGoal.transform.SetParent(this.transform, false);
            ik.solver.leftArmChain.bendConstraint.bendGoal = leftElbowBendGoal.transform;
            ik.solver.leftArmChain.bendConstraint.bendGoal.localPosition = new Vector3(-0.5f, 0.5f, 0);
            ik.solver.leftArmChain.bendConstraint.weight = 0.4f;
            LeftHandTransformState.BendGoalLocalPosition = ik.solver.leftArmChain.bendConstraint.bendGoal.localPosition;
            LeftHandTransformState.BendGoalConstraintWeight = ik.solver.leftArmChain.bendConstraint.weight;

            GameObject rightElbowBendGoal = new("RightElbowBendGoal");
            rightElbowBendGoal.transform.SetParent(this.transform, false);
            ik.solver.rightArmChain.bendConstraint.bendGoal = rightElbowBendGoal.transform;
            ik.solver.rightArmChain.bendConstraint.bendGoal.localPosition = new Vector3(0.5f, 0.5f, 0);
            ik.solver.rightArmChain.bendConstraint.weight = 0.4f;
            RightHandTransformState.BendGoalLocalPosition = ik.solver.rightArmChain.bendConstraint.bendGoal.localPosition;
            RightHandTransformState.BendGoalConstraintWeight = ik.solver.rightArmChain.bendConstraint.weight;

            areFirstDetectedHandCoordinatesLeftHand = true;
            areFirstDetectedHandCoordinatesRightHand = true;
            leftHandLandmarksFilters.Clear();
            rightHandLandmarksFilters.Clear();
            filteredLeftHandLandmarks.Clear();
            filteredRightHandLandmarks.Clear();

            IsHandsAnimatorInitialized = true;
        }

        /// <summary>
        /// Smoothly brings the avatar's hands to the start position.
        /// </summary>
        /// <returns>True, if the start position was reached.</returns>
        /// <remarks>By start position it is meant that the avatar's hands are in front of the avatar,
        /// bent at the elbows and the palms are facing forward.</remarks>
        public bool BringHandsToStartPositions()
        {
            Transform headBone = transform.Find(AvatarSceleton.Head);
            headPosition = transform.InverseTransformPoint(headBone.position);

            Transform leftHand = transform.Find(AvatarSceleton.LeftHand);
            Transform rightHand = transform.Find(AvatarSceleton.RightHand);

            leftHand.localRotation = startLeftHandRotation * leftHandRotationOffset;
            leftHandTargetRotation = leftHand.rotation;
            leftHand.localRotation = startLeftHandRotation;

            rightHand.localRotation = startRightHandRotation * rightHandRotationOffset;
            rightHandTargetRotation = rightHand.rotation;
            rightHand.localRotation = startRightHandRotation;

            leftHandTargetPos = transform.TransformPoint(leftHandPositionOffset);
            rightHandTargetPos = transform.TransformPoint(rightHandPositionOffset);

            // If the start position has not yet been reached.
            if (!StartHandsPositionReached
                && (Vector3.Distance(LeftHandTransformState.HandPosition, leftHandTargetPos) >= arrivalThreshold
                    || Vector3.Distance(RightHandTransformState.HandPosition, rightHandTargetPos) >= arrivalThreshold))
            {
                // Turn and move the hands slightly to get closer to the starting position.
                LeftHandTransformState.HandPosition = Vector3.Lerp(LeftHandTransformState.HandPosition, leftHandTargetPos, Time.deltaTime * moveSpeed);
                LeftHandTransformState.HandRotation = Quaternion.Slerp(LeftHandTransformState.HandRotation, leftHandTargetRotation, Time.deltaTime * moveSpeed);
                ik.solver.leftHandEffector.rotation = LeftHandTransformState.HandRotation;
                ik.solver.leftHandEffector.position = LeftHandTransformState.HandPosition;

                ik.solver.leftHandEffector.positionWeight = weight;
                ik.solver.leftHandEffector.rotationWeight = weight;
                LeftHandTransformState.HandIKRotationWeight = ik.solver.leftHandEffector.rotationWeight;
                LeftHandTransformState.HandIKPositionWeight = ik.solver.leftHandEffector.positionWeight;

                RightHandTransformState.HandPosition = Vector3.Lerp(RightHandTransformState.HandPosition, rightHandTargetPos, Time.deltaTime * moveSpeed);
                RightHandTransformState.HandRotation = Quaternion.Slerp(RightHandTransformState.HandRotation, rightHandTargetRotation, Time.deltaTime * moveSpeed);
                ik.solver.rightHandEffector.rotation = RightHandTransformState.HandRotation;
                ik.solver.rightHandEffector.position = RightHandTransformState.HandPosition;

                ik.solver.rightHandEffector.positionWeight = weight;
                ik.solver.rightHandEffector.rotationWeight = weight;
                RightHandTransformState.HandIKRotationWeight = ik.solver.rightHandEffector.rotationWeight;
                RightHandTransformState.HandIKPositionWeight = ik.solver.rightHandEffector.positionWeight;

                LeftHandTransformState.HandIKEffectorPosition = ik.solver.leftHandEffector.position;
                LeftHandTransformState.HandIKEffectorRotation = ik.solver.leftHandEffector.rotation;

                RightHandTransformState.HandIKEffectorPosition = ik.solver.rightHandEffector.position;
                RightHandTransformState.HandIKEffectorRotation = ik.solver.rightHandEffector.rotation;

                // Save the thumbs rotations values ​​to control their animation in the future.
                Transform leftThumb1Bone = transform.Find(AvatarSceleton.LeftThumb1);
                Transform leftThumb2Bone = transform.Find(AvatarSceleton.LeftThumb2);
                Transform leftThumb3Bone = transform.Find(AvatarSceleton.LeftThumb3);
                LeftHandTransformState.Thumb1Rotations = leftThumb1Bone.localRotation;
                LeftHandTransformState.Thumb2Rotations = leftThumb2Bone.localRotation;
                LeftHandTransformState.Thumb3Rotations = leftThumb3Bone.localRotation;

                Transform rightThumb1Bone = transform.Find(AvatarSceleton.RightThumb1);
                Transform rightThumb2Bone = transform.Find(AvatarSceleton.RightThumb2);
                Transform rightThumb3Bone = transform.Find(AvatarSceleton.RightThumb3);
                RightHandTransformState.Thumb1Rotations = rightThumb1Bone.localRotation;
                RightHandTransformState.Thumb2Rotations = rightThumb2Bone.localRotation;
                RightHandTransformState.Thumb3Rotations = rightThumb3Bone.localRotation;

                return false;
            }
            else
            {
                StartHandsPositionReached = true;
                return true;
            }
        }

        /// <summary>
        /// Changes the avatar's hand positions using the output from the MediaPipe <see cref="PoseLandmarker"/> model.
        /// </summary>
        /// <param name="resultPoseLandmarker">Output from the mediapipe <see cref="PoseLandmarker"/> model.</param>
        /// <param name="samplingTimes">List of timestamps from MediaPipe callbacks used to compute the sampling period for filtering.</param>
        public void SolveHandsPositions(PoseLandmarkerResult resultPoseLandmarker,
                                        List<float> samplingTimes)
        {
            Transform leftHand = transform.Find(AvatarSceleton.LeftHand);
            Transform rightHand = transform.Find(AvatarSceleton.RightHand);

            leftHand.localRotation = startLeftHandRotation * leftHandRotationOffset;
            leftHandTargetRotation = leftHand.rotation;
            leftHand.localRotation = startLeftHandRotation;

            rightHand.localRotation = startRightHandRotation * rightHandRotationOffset;
            rightHandTargetRotation = rightHand.rotation;
            rightHand.localRotation = startRightHandRotation;

            ik.solver.leftHandEffector.rotation = leftHandTargetRotation;
            ik.solver.rightHandEffector.rotation = rightHandTargetRotation;

            List<Landmark> poseLandmarks = resultPoseLandmarker.poseWorldLandmarks[0].landmarks;
            Landmark mediapipeLeftHandPosition = poseLandmarks[15];
            Landmark mediapipeRightHandPosition = poseLandmarks[16];

            // Save the first detected coordinates.
            if (isFirstPoseLandmark)
            {
                LeftHandTransformState.NewMediapipeCoordinates.x = mediapipeLeftHandPosition.x;
                LeftHandTransformState.NewMediapipeCoordinates.y = mediapipeLeftHandPosition.y;
                LeftHandTransformState.NewMediapipeCoordinates.z = mediapipeLeftHandPosition.z;

                RightHandTransformState.NewMediapipeCoordinates.x = mediapipeRightHandPosition.x;
                RightHandTransformState.NewMediapipeCoordinates.y = mediapipeRightHandPosition.y;
                RightHandTransformState.NewMediapipeCoordinates.z = mediapipeRightHandPosition.z;

                isFirstPoseLandmark = false;
            }

            // Set values ​​for hand rotations when moving in front of the avatar's body, when moving away from the avatar, and when moving downwards.
            LeftHandTransformState.HandRotationForMovementInFrontOfTheAvatar = leftHandTargetRotation * Quaternion.Euler(0, 55, 0);
            RightHandTransformState.HandRotationForMovementInFrontOfTheAvatar = rightHandTargetRotation * Quaternion.Euler(0, -55, 0);

            LeftHandTransformState.HandRotationForMovementToTheSide = leftHandTargetRotation * Quaternion.Euler(0, -50, 0);
            RightHandTransformState.HandRotationForMovementToTheSide = rightHandTargetRotation * Quaternion.Euler(0, 50, 0);

            LeftHandTransformState.HandRotationForMovementDown = leftHandTargetRotation * Quaternion.Euler(0, 0, 60);
            RightHandTransformState.HandRotationForMovementDown = rightHandTargetRotation * Quaternion.Euler(0, 0, -60);

            Landmark mediapipeHeadPosition = poseLandmarks[0];

            // Save the last detected coordinates and initialize new.
            LeftHandTransformState.PreviousMediapipeCoordinates = LeftHandTransformState.NewMediapipeCoordinates;
            LeftHandTransformState.NewMediapipeCoordinates.x = mediapipeLeftHandPosition.x;
            LeftHandTransformState.NewMediapipeCoordinates.y = mediapipeLeftHandPosition.y;
            LeftHandTransformState.NewMediapipeCoordinates.z = mediapipeLeftHandPosition.z;

            RightHandTransformState.PreviousMediapipeCoordinates = RightHandTransformState.NewMediapipeCoordinates;
            RightHandTransformState.NewMediapipeCoordinates.x = mediapipeRightHandPosition.x;
            RightHandTransformState.NewMediapipeCoordinates.y = mediapipeRightHandPosition.y;
            RightHandTransformState.NewMediapipeCoordinates.z = mediapipeRightHandPosition.z;

            // If the probability with which the left hand is in the picture is acceptable for animation.
            if (mediapipeLeftHandPosition.presence > acceptableHandPresenceProbability && mediapipeLeftHandPosition.visibility > acceptableHandPresenceProbability)
            {
                leftHandDetectedFrames++;
                leftHandLostFrames = Math.Max(leftHandLostFrames / 2, 1);

                // Animate the hand if it has been detected for a sufficient number of frames or if a gesture was recognized.
                if (leftHandDetectedFrames > minDetectedFrames || wasLeftHandGestureRecognized)
                {
                    if (ik.solver.leftHandEffector.positionWeight <= 0.95f || ik.solver.leftArmChain.bendConstraint.weight <= 0.37f)
                    {
                        ik.solver.leftHandEffector.positionWeight = Mathf.Lerp(ik.solver.leftHandEffector.positionWeight, weight, Time.deltaTime * moveSpeed * 2);
                        ik.solver.leftHandEffector.rotationWeight = Mathf.Lerp(ik.solver.leftHandEffector.rotationWeight, weight, Time.deltaTime * moveSpeed * 2);
                        ik.solver.leftArmChain.bendConstraint.weight = Mathf.Lerp(ik.solver.leftArmChain.bendConstraint.weight, 0.4f, Time.deltaTime * moveSpeed * 2);
                    }

                    LeftHandTransformState.HandToHeadCoordinateDifference
                        = new Vector3(mediapipeLeftHandPosition.x - mediapipeHeadPosition.x,
                        mediapipeLeftHandPosition.y - mediapipeHeadPosition.y,
                        transform.InverseTransformPoint(leftHandTargetPos).z - headPosition.z);

                    Vector3 newHandPosition = transform.TransformPoint(headPosition + LeftHandTransformState.HandToHeadCoordinateDifference);
                    newHandPosition = handsPositionsFilters.leftHandPositionFilter.ApplyFilterToHandPosition(samplingTimes, newHandPosition);

                    if (!wasLeftHandGestureRecognized)
                    {
                        ik.solver.leftHandEffector.position = Vector3.Lerp(
                                        ik.solver.leftHandEffector.position,
                                        newHandPosition,
                                        0.1f);
                    }
                    // If a gesture was recognized - move the hand slower.
                    else
                    {
                        ik.solver.leftHandEffector.position = Vector3.Lerp(
                                            ik.solver.leftHandEffector.position,
                                            newHandPosition,
                                            0.05f);
                    }

                    // Interval where palm should be facing the camera.
                    if (LeftHandTransformState.HandToHeadCoordinateDifference.x < handXCoordinatesDiffIntervalToFaceTheCamera.Item2
                        && LeftHandTransformState.HandToHeadCoordinateDifference.x > handXCoordinatesDiffIntervalToFaceTheCamera.Item1)
                    {
                        leftHand.localRotation = startLeftHandRotation * leftHandRotationOffset;
                        leftHandTargetRotation = leftHand.rotation;
                        leftHand.localRotation = startLeftHandRotation;
                        LeftHandTransformState.HandRotation = Quaternion.Slerp(LeftHandTransformState.HandRotation, leftHandTargetRotation, Time.deltaTime * moveSpeed * 3);
                        ik.solver.leftHandEffector.rotation = LeftHandTransformState.HandRotation;
                    }
                    // If the hand is moving in front of the character.
                    else if (LeftHandTransformState.HandToHeadCoordinateDifference.x >= handXCoordinatesDiffIntervalMovingInFront.Item1
                        && LeftHandTransformState.HandToHeadCoordinateDifference.x <= handXCoordinatesDiffIntervalMovingInFront.Item2
                        && !wasLeftHandGestureRecognized)
                    {
                        leftHandTargetRotation = LeftHandTransformState.HandRotationForMovementInFrontOfTheAvatar;
                        if (ik.solver.leftHandEffector.rotation.eulerAngles.y < LeftHandTransformState.HandRotationForMovementInFrontOfTheAvatar.eulerAngles.y)
                        {
                            LeftHandTransformState.HandRotation = Quaternion.Slerp(LeftHandTransformState.HandRotation, leftHandTargetRotation, Time.deltaTime * moveSpeed * 3);
                            ik.solver.leftHandEffector.rotation = LeftHandTransformState.HandRotation;
                            ik.solver.leftHandEffector.rotationWeight = weight;
                            ik.solver.leftArmChain.bendConstraint.bendGoal.localPosition = new Vector3(-0.5f, 0.5f, 0);
                            LeftHandTransformState.BendGoalLocalPosition = ik.solver.leftArmChain.bendConstraint.bendGoal.localPosition;
                        }
                    }
                    // If the hand is moving to the side, away from the character.
                    else if (LeftHandTransformState.PreviousMediapipeCoordinates.x > LeftHandTransformState.NewMediapipeCoordinates.x
                        && !wasLeftHandGestureRecognized)
                    {
                        leftHandTargetRotation = LeftHandTransformState.HandRotationForMovementToTheSide;
                        if (ik.solver.leftHandEffector.rotation.y > LeftHandTransformState.HandRotationForMovementToTheSide.y)
                        {
                            LeftHandTransformState.HandRotation = Quaternion.Slerp(LeftHandTransformState.HandRotation, leftHandTargetRotation, Time.deltaTime * moveSpeed);
                            ik.solver.leftHandEffector.rotation = LeftHandTransformState.HandRotation;
                            ik.solver.leftHandEffector.rotationWeight = weight;
                        }
                    }
                    // If the hand is moving downwards.
                    if (LeftHandTransformState.HandToHeadCoordinateDifference.y <= handYCoordinatesDiffToMoveDownFrom
                        && !wasLeftHandGestureRecognized)
                    {
                        leftHandTargetRotation = LeftHandTransformState.HandRotationForMovementDown;
                        if (ik.solver.leftHandEffector.rotation.z > LeftHandTransformState.HandRotationForMovementDown.z)
                        {
                            LeftHandTransformState.HandRotation = Quaternion.Slerp(LeftHandTransformState.HandRotation, leftHandTargetRotation, Time.deltaTime * moveSpeed * 3);
                            ik.solver.leftHandEffector.rotation = LeftHandTransformState.HandRotation;
                            ik.solver.leftHandEffector.rotationWeight = weight;
                        }
                    }
                }
            }
            // If the probability with which the left hand is in the picture is NOT acceptable for animation
            // and no gesture was recognized, assign the neutral position.
            else if (!wasLeftHandGestureRecognized)
            {
                leftHandLostFrames++;
                leftHandDetectedFrames = Mathf.Max(leftHandDetectedFrames - 1, 0);
                if (leftHandLostFrames >= maxLostFrames)
                {
                    if (ik.solver.leftHandEffector.positionWeight > 0.005f || ik.solver.leftArmChain.bendConstraint.weight > 0.005f)
                    {
                        StoreRotationsLeftHand();
                        ik.solver.leftHandEffector.positionWeight = Mathf.Lerp(ik.solver.leftHandEffector.positionWeight, 0f, Time.deltaTime * moveSpeed * 2);
                        ik.solver.leftHandEffector.rotationWeight = Mathf.Lerp(ik.solver.leftHandEffector.rotationWeight, 0f, Time.deltaTime * moveSpeed * 2);
                        ik.solver.leftArmChain.bendConstraint.weight = Mathf.Lerp(ik.solver.leftArmChain.bendConstraint.weight, 0f, Time.deltaTime * moveSpeed * 2);
                    }
                }
            }
            /// If the probability that the left hand is visible in the camera frame is insufficient for animation,
            /// but <see cref="wasLeftHandGestureRecognized"/> is true, the gesture detection counters are adjusted.
            /// This is required to reset <see cref="wasLeftHandGestureRecognized"/> to false when the last animated hand pose
            /// was a gesture (e.g., thumb up or thumb down) and the left hand is no longer visible in the camera frame.
            /// Without this reset, <see cref="wasLeftHandGestureRecognized"/> will remain true, causing the hand
            /// to incorrectly stay in a gesture state instead of returning to the neutral position.
            else
            {
                leftHandGestureDetectedFrames = Mathf.Max(leftHandGestureDetectedFrames - 1, 0);
                leftHandGestureLostFrames++;
            }

            if (leftHandGestureLostFrames >= maxGestureLostFames)
            {
                wasLeftHandGestureRecognized = false;
            }

            // If the probability with which the right hand is in the picture is acceptable for animation.
            if (mediapipeRightHandPosition.presence > acceptableHandPresenceProbability && mediapipeRightHandPosition.visibility > acceptableHandPresenceProbability)
            {
                rightHandDetectedFrames++;
                rightHandLostFrames = Math.Max(rightHandLostFrames / 2, 1);

                // Animate the hand if it has been detected for a sufficient number of frames or if a gesture was recognized.
                if (rightHandDetectedFrames > minDetectedFrames || wasRightHandGestureRecognized)
                {
                    if (ik.solver.rightHandEffector.positionWeight <= 0.95f || ik.solver.rightArmChain.bendConstraint.weight <= 0.37f)
                    {
                        ik.solver.rightHandEffector.positionWeight = Mathf.Lerp(ik.solver.rightHandEffector.positionWeight, weight, Time.deltaTime * moveSpeed * 2);
                        ik.solver.rightHandEffector.rotationWeight = Mathf.Lerp(ik.solver.rightHandEffector.rotationWeight, weight, Time.deltaTime * moveSpeed * 2);
                        ik.solver.rightArmChain.bendConstraint.weight = Mathf.Lerp(ik.solver.rightArmChain.bendConstraint.weight, 0.4f, Time.deltaTime * moveSpeed * 2);
                    }

                    RightHandTransformState.HandToHeadCoordinateDifference
                        = new Vector3(mediapipeRightHandPosition.x - mediapipeHeadPosition.x,
                                      mediapipeRightHandPosition.y - mediapipeHeadPosition.y,
                                      transform.InverseTransformPoint(rightHandTargetPos).z - headPosition.z);

                    Vector3 newHandPosition = transform.TransformPoint(headPosition + RightHandTransformState.HandToHeadCoordinateDifference);
                    newHandPosition = handsPositionsFilters.rightHandPositionFilter.ApplyFilterToHandPosition(samplingTimes, newHandPosition);

                    if (!wasRightHandGestureRecognized)
                    {
                        ik.solver.rightHandEffector.position = Vector3.Lerp(
                                            ik.solver.rightHandEffector.position,
                                            newHandPosition,
                                            0.1f);
                    }
                    // If a gesture was recognized - move the hand slower.
                    else
                    {
                        ik.solver.rightHandEffector.position = Vector3.Lerp(
                                                ik.solver.rightHandEffector.position,
                                                newHandPosition,
                                                0.05f);
                    }

                    // Interval where palm should be facing the camera.
                    if (RightHandTransformState.HandToHeadCoordinateDifference.x > -handXCoordinatesDiffIntervalToFaceTheCamera.Item2
                        && RightHandTransformState.HandToHeadCoordinateDifference.x < -handXCoordinatesDiffIntervalToFaceTheCamera.Item1)
                    {
                        rightHand.localRotation = startRightHandRotation * rightHandRotationOffset;
                        rightHandTargetRotation = rightHand.rotation;
                        rightHand.localRotation = startRightHandRotation;
                        RightHandTransformState.HandRotation = Quaternion.Slerp(RightHandTransformState.HandRotation, rightHandTargetRotation, Time.deltaTime * moveSpeed * 3);
                        ik.solver.rightHandEffector.rotation = RightHandTransformState.HandRotation;
                    }
                    // If the hand is moving in front of the character.
                    else if (RightHandTransformState.HandToHeadCoordinateDifference.x <= -handXCoordinatesDiffIntervalMovingInFront.Item1
                        && RightHandTransformState.HandToHeadCoordinateDifference.x >= -handXCoordinatesDiffIntervalMovingInFront.Item2
                        && !wasRightHandGestureRecognized)
                    {
                        rightHandTargetRotation = RightHandTransformState.HandRotationForMovementInFrontOfTheAvatar;
                        if (ik.solver.rightHandEffector.rotation.eulerAngles.y > RightHandTransformState.HandRotationForMovementInFrontOfTheAvatar.eulerAngles.y)
                        {
                            RightHandTransformState.HandRotation = Quaternion.Slerp(RightHandTransformState.HandRotation, rightHandTargetRotation, Time.deltaTime * moveSpeed * 3);
                            ik.solver.rightHandEffector.rotation = RightHandTransformState.HandRotation;
                            ik.solver.rightHandEffector.rotationWeight = weight;
                            ik.solver.rightArmChain.bendConstraint.bendGoal.localPosition = new Vector3(0.5f, 0.5f, 0);
                            RightHandTransformState.BendGoalLocalPosition = ik.solver.rightArmChain.bendConstraint.bendGoal.localPosition;
                        }
                    }
                    // If the hand is moving to the side, away from the character.
                    else if (RightHandTransformState.PreviousMediapipeCoordinates.x < RightHandTransformState.NewMediapipeCoordinates.x
                        && !wasRightHandGestureRecognized)
                    {
                        rightHandTargetRotation = RightHandTransformState.HandRotationForMovementToTheSide;
                        if (ik.solver.rightHandEffector.rotation.y < RightHandTransformState.HandRotationForMovementToTheSide.y)
                        {
                            RightHandTransformState.HandRotation = Quaternion.Slerp(RightHandTransformState.HandRotation, rightHandTargetRotation, Time.deltaTime * moveSpeed);
                            ik.solver.rightHandEffector.rotation = RightHandTransformState.HandRotation;
                            ik.solver.rightHandEffector.rotationWeight = weight;
                        }
                    }
                    // If the hand is moving downwards.
                    if (RightHandTransformState.HandToHeadCoordinateDifference.y <= handYCoordinatesDiffToMoveDownFrom
                        && !wasRightHandGestureRecognized)
                    {
                        rightHandTargetRotation = RightHandTransformState.HandRotationForMovementDown;
                        if (ik.solver.rightHandEffector.rotation.z > RightHandTransformState.HandRotationForMovementDown.z)
                        {
                            RightHandTransformState.HandRotation = Quaternion.Slerp(RightHandTransformState.HandRotation, rightHandTargetRotation, Time.deltaTime * moveSpeed * 3);
                            ik.solver.rightHandEffector.rotation = RightHandTransformState.HandRotation;
                            ik.solver.rightHandEffector.rotationWeight = weight;
                        }
                    }
                }
            }
            // If the probability with which the right hand is in the picture is NOT acceptable for animation
            // and no gesture was recognized, assign the neutral position.
            else if (!wasRightHandGestureRecognized)
            {
                rightHandLostFrames++;
                rightHandDetectedFrames = Mathf.Max(rightHandDetectedFrames - 1, 0);
                if (rightHandLostFrames >= maxLostFrames)
                {
                    if (ik.solver.rightHandEffector.positionWeight > 0.005f || ik.solver.rightArmChain.bendConstraint.weight > 0.005f)
                    {
                        StoreRotationsRightHand();
                        ik.solver.rightHandEffector.positionWeight = Mathf.Lerp(ik.solver.rightHandEffector.positionWeight, 0f, Time.deltaTime * moveSpeed * 2);
                        ik.solver.rightHandEffector.rotationWeight = Mathf.Lerp(ik.solver.rightHandEffector.rotationWeight, 0f, Time.deltaTime * moveSpeed * 2);
                        ik.solver.rightArmChain.bendConstraint.weight = Mathf.Lerp(ik.solver.rightArmChain.bendConstraint.weight, 0f, Time.deltaTime * moveSpeed * 2);
                    }
                }
            }
            /// If the probability that the right hand is visible in the camera frame is insufficient for animation,
            /// but <see cref="wasRightHandGestureRecognized"/> is true, the gesture detection counters are adjusted.
            /// This is required to reset <see cref="wasRightHandGestureRecognized"/> to false when the last animated hand pose
            /// was a gesture (e.g., thumb up or thumb down) and the right hand is no longer visible in the camera frame.
            /// Without this reset, <see cref="wasRightHandGestureRecognized"/> will remain true, causing the hand
            /// to incorrectly stay in a gesture state instead of returning to the neutral position.
            else
            {
                rightHandGestureDetectedFrames = Mathf.Max(rightHandGestureDetectedFrames - 1, 0);
                rightHandGestureLostFrames++;
            }

            if (rightHandGestureLostFrames >= maxGestureLostFames)
            {
                wasRightHandGestureRecognized = false;
            }

            if (IsPointing)
            {
                ik.solver.rightHandEffector.positionWeight = 0f;
                ik.solver.rightHandEffector.rotationWeight = 0f;
                ik.solver.rightArmChain.bendConstraint.weight = 0f;
            }

            // Save information about current hands positions and rotations.
            LeftHandTransformState.HandIKEffectorPosition = ik.solver.leftHandEffector.position;
            LeftHandTransformState.BendGoalConstraintWeight = ik.solver.leftArmChain.bendConstraint.weight;
            LeftHandTransformState.HandIKPositionWeight = ik.solver.leftHandEffector.positionWeight;
            LeftHandTransformState.HandIKRotationWeight = ik.solver.leftHandEffector.rotationWeight;

            RightHandTransformState.HandIKEffectorPosition = ik.solver.rightHandEffector.position;
            RightHandTransformState.BendGoalConstraintWeight = ik.solver.rightArmChain.bendConstraint.weight;
            RightHandTransformState.HandIKPositionWeight = ik.solver.rightHandEffector.positionWeight;
            RightHandTransformState.HandIKRotationWeight = ik.solver.rightHandEffector.rotationWeight;
        }

        /// <summary>
        /// Rotates the wrist and fingers of the left hand using the output from the MediaPipe <see cref="GestureRecognizer"/> model.
        /// </summary>
        /// <param name="resultGestureRecognizer">Output from the MediaPipe <see cref="GestureRecognizer"/> model.</param>
        /// <param name="samplingTimes">List of timestamps from MediaPipe callbacks used to compute the sampling period for filtering.</param>
        public void SolveLeftHand(GestureRecognizerResult resultGestureRecognizer,
                                  List<float> samplingTimes)
        {
            // Index of values ​​for the left hand in the list of coordinates from gesture recognizer model.
            int leftHandResultIndex = -1;

            if (resultGestureRecognizer.handedness != null)
            {
                leftHandResultIndex
                    = resultGestureRecognizer.handedness.IndexOf(resultGestureRecognizer.handedness.Find(x => x.categories[0].categoryName == "Left"));
            }
            string leftHandGesture = "None";
            if (leftHandResultIndex != -1)
            {
                leftHandGesture = resultGestureRecognizer.gestures[leftHandResultIndex].categories[0].categoryName;
            }

            // If the left hand was detected, get world coordinates of the keypoints.
            if (leftHandResultIndex >= 0 && leftHandLostFrames < maxLostFrames)
            {
                // Use filtered landmarks for animation.
                ApplyFiterToLeftHandLandmarks(resultGestureRecognizer, samplingTimes);
                List<Vector3> leftHandLandmarks = filteredLeftHandLandmarks;

                handLandmarks.LeftMiddleFinger3Position = leftHandLandmarks[11];
                handLandmarks.LeftMiddleFinger2Position = leftHandLandmarks[10];
                handLandmarks.LeftMiddleFinger1Position = leftHandLandmarks[9];

                handLandmarks.LeftIndexFinger3Position = leftHandLandmarks[7];
                handLandmarks.LeftIndexFinger2Position = leftHandLandmarks[6];
                handLandmarks.LeftIndexFinger1Position = leftHandLandmarks[5];

                handLandmarks.LeftRingFinger3Position = leftHandLandmarks[15];
                handLandmarks.LeftRingFinger2Position = leftHandLandmarks[14];
                handLandmarks.LeftRingFinger1Position = leftHandLandmarks[13];

                handLandmarks.LeftPinkyFinger3Position = leftHandLandmarks[19];
                handLandmarks.LeftPinkyFinger2Position = leftHandLandmarks[18];
                handLandmarks.LeftPinkyFinger1Position = leftHandLandmarks[17];

                handLandmarks.LeftThumb3Position = leftHandLandmarks[3];
                handLandmarks.LeftThumb2Position = leftHandLandmarks[2];

                handLandmarks.LeftHandPosition = leftHandLandmarks[0];

                // Get transform components of avatar fingers.
                Transform leftMidFinger3Bone = transform.Find(AvatarSceleton.LeftMidFinger3);
                Transform leftMidFinger2Bone = transform.Find(AvatarSceleton.LeftMidFinger2);
                Transform leftMidFinger1Bone = transform.Find(AvatarSceleton.LeftMidFinger1);

                Transform leftIndexFinger1Bone = transform.Find(AvatarSceleton.LeftIndexFinger1);
                Transform leftIndexFinger2Bone = transform.Find(AvatarSceleton.LeftIndexFinger2);
                Transform leftIndexFinger3Bone = transform.Find(AvatarSceleton.LeftIndexFinger3);

                Transform leftRingFinger1Bone = transform.Find(AvatarSceleton.LeftRingFinger1);
                Transform leftRingFinger2Bone = transform.Find(AvatarSceleton.LeftRingFinger2);
                Transform leftRingFinger3Bone = transform.Find(AvatarSceleton.LeftRingFinger3);

                Transform leftPinkyFinger1Bone = transform.Find(AvatarSceleton.LeftPinkyFinger1);
                Transform leftPinkyFinger2Bone = transform.Find(AvatarSceleton.LeftPinkyFinger2);
                Transform leftPinkyFinger3Bone = transform.Find(AvatarSceleton.LeftPinkyFinger3);

                Transform leftThumb1Bone = transform.Find(AvatarSceleton.LeftThumb1);
                Transform leftThumb2Bone = transform.Find(AvatarSceleton.LeftThumb2);
                Transform leftThumb3Bone = transform.Find(AvatarSceleton.LeftThumb3);

                // If these are the very first landmarks detected, save the starting positions of the bones (relative to their parent transforms)
                // so that these values can be used ​​to calculate rotations later.
                if (LeftHandTransformState.IsFirstHandLandmark)
                {
                    LeftHandTransformState.IndexFinger3StartPos = new Vector3(0, handLandmarks.LeftIndexFinger3Position.y - handLandmarks.LeftIndexFinger2Position.y, 0);
                    LeftHandTransformState.IndexFinger2StartPos = new Vector3(0, handLandmarks.LeftIndexFinger2Position.y - handLandmarks.LeftIndexFinger1Position.y, 0);
                    LeftHandTransformState.IndexFinger1StartPos = new Vector3(handLandmarks.LeftIndexFinger1Position.x - handLandmarks.LeftHandPosition.x, handLandmarks.LeftIndexFinger1Position.y - handLandmarks.LeftHandPosition.y, 0);

                    LeftHandTransformState.MidFinger3StartPos = new Vector3(handLandmarks.LeftMiddleFinger3Position.x - handLandmarks.LeftMiddleFinger2Position.x, handLandmarks.LeftMiddleFinger3Position.y - handLandmarks.LeftMiddleFinger2Position.y, 0);
                    LeftHandTransformState.MidFinger2StartPos = new Vector3(0, handLandmarks.LeftMiddleFinger2Position.y - handLandmarks.LeftMiddleFinger1Position.y, 0);

                    LeftHandTransformState.RingFinger3StartPos = new Vector3(0, handLandmarks.LeftRingFinger3Position.y - handLandmarks.LeftRingFinger2Position.y, 0);
                    LeftHandTransformState.RingFinger2StartPos = new Vector3(0, handLandmarks.LeftRingFinger2Position.y - handLandmarks.LeftRingFinger1Position.y, 0);

                    LeftHandTransformState.PinkyFinger3StartPos = new Vector3(0, handLandmarks.LeftPinkyFinger3Position.y - handLandmarks.LeftPinkyFinger2Position.y, 0);
                    LeftHandTransformState.PinkyFinger2StartPos = new Vector3(0, handLandmarks.LeftPinkyFinger2Position.y - handLandmarks.LeftPinkyFinger1Position.y, 0);

                    LeftHandTransformState.Thumb3StartPos = new Vector3(handLandmarks.LeftThumb3Position.x - handLandmarks.LeftThumb2Position.x, handLandmarks.LeftThumb3Position.y - handLandmarks.LeftThumb2Position.y, 0);

                    LeftHandTransformState.IsFirstHandLandmark = false;
                }

                // Rotating the wrist and fingers.
                // If the thumb up or thumb down gesture was recognized, animate accordingly.
                if (leftHandGesture == "Thumb_Up" && resultGestureRecognizer.gestures[leftHandResultIndex].categories[0].score > 0.6)
                {
                    leftHandGestureDetectedFrames++;
                    leftHandGestureLostFrames = 0;

                    if (leftHandGestureDetectedFrames >= minGestureDetectedFrames && leftHandGestureLostFrames <= maxGestureLostFames)
                    {
                        wasLeftHandGestureRecognized = true;

                        leftThumb1Bone.localRotation = Quaternion.Euler(57f, 35f, 30f);
                        leftThumb2Bone.localRotation = Quaternion.Euler(0, 0, 0);
                        leftThumb3Bone.localRotation = Quaternion.Euler(0, 0, 0);

                        leftMidFinger1Bone.localRotation *= Quaternion.Euler(0, 0, 60);
                        leftMidFinger2Bone.localRotation *= Quaternion.Euler(0, 0, 100);
                        leftMidFinger3Bone.localRotation *= Quaternion.Euler(0, 0, 50);

                        leftRingFinger1Bone.localRotation *= Quaternion.Euler(0, 0, 60);
                        leftRingFinger2Bone.localRotation *= Quaternion.Euler(0, 0, 100);
                        leftRingFinger3Bone.localRotation *= Quaternion.Euler(0, 0, 50);

                        leftIndexFinger1Bone.localRotation = Quaternion.Euler(0, 0, 60f);
                        leftIndexFinger2Bone.localRotation = Quaternion.Euler(0, 0, 120f);
                        leftIndexFinger3Bone.localRotation = Quaternion.Euler(0, 0, 60f);

                        leftPinkyFinger1Bone.localRotation *= Quaternion.Euler(0, 0, 60);
                        leftPinkyFinger2Bone.localRotation *= Quaternion.Euler(0, 0, 100);
                        leftPinkyFinger3Bone.localRotation *= Quaternion.Euler(0, 0, 50);

                        leftHandTargetRotation *= Quaternion.Euler(80, -60, -60);
                        Vector3 leftBendGoalTargetPosition = new Vector3(-1.5f, 0.5f, 0);

                        ik.solver.leftHandEffector.rotation = LeftHandTransformState.HandIKEffectorRotation;
                        if (Quaternion.Angle(ik.solver.leftHandEffector.rotation, leftHandTargetRotation) > 5f)
                        {
                            LeftHandTransformState.HandIKEffectorRotation = Quaternion.Slerp(
                                LeftHandTransformState.HandIKEffectorRotation,
                                leftHandTargetRotation,
                                Time.deltaTime * moveSpeed * 5);
                            ik.solver.leftHandEffector.rotation = LeftHandTransformState.HandIKEffectorRotation;
                        }
                        else
                        {
                            LeftHandTransformState.HandIKEffectorRotation = leftHandTargetRotation;
                        }

                        ik.solver.leftArmChain.bendConstraint.bendGoal.localPosition = LeftHandTransformState.BendGoalLocalPosition;
                        if (Vector3.Distance(LeftHandTransformState.BendGoalLocalPosition, leftBendGoalTargetPosition) > 0.03f)
                        {
                            LeftHandTransformState.BendGoalLocalPosition = Vector3.Lerp(
                                LeftHandTransformState.BendGoalLocalPosition,
                                leftBendGoalTargetPosition,
                                Time.deltaTime * moveSpeed);
                            ik.solver.leftArmChain.bendConstraint.bendGoal.localPosition = LeftHandTransformState.BendGoalLocalPosition;
                        }
                        else
                        {
                            LeftHandTransformState.BendGoalLocalPosition = leftBendGoalTargetPosition;
                        }

                        StoreRotationsLeftHand();
                    }
                    else
                    {
                        AnimateLastDetectedValuesLeftHand();
                    }
                }
                else if (leftHandGesture == "Thumb_Down" && resultGestureRecognizer.gestures[leftHandResultIndex].categories[0].score > 0.6)
                {
                    leftHandGestureDetectedFrames++;
                    leftHandGestureLostFrames = 0;

                    if (leftHandGestureDetectedFrames >= minGestureDetectedFrames && leftHandGestureLostFrames <= maxGestureLostFames)
                    {
                        wasLeftHandGestureRecognized = true;

                        leftThumb1Bone.localRotation = Quaternion.Euler(57f, 35f, 30f);
                        leftThumb2Bone.localRotation = Quaternion.Euler(0, 0, 0);
                        leftThumb3Bone.localRotation = Quaternion.Euler(0, 0, 0);

                        leftMidFinger1Bone.localRotation *= Quaternion.Euler(0, 0, 60);
                        leftMidFinger2Bone.localRotation *= Quaternion.Euler(0, 0, 100);
                        leftMidFinger3Bone.localRotation *= Quaternion.Euler(0, 0, 50);

                        leftRingFinger1Bone.localRotation *= Quaternion.Euler(0, 0, 60);
                        leftRingFinger2Bone.localRotation *= Quaternion.Euler(0, 0, 100);
                        leftRingFinger3Bone.localRotation *= Quaternion.Euler(0, 0, 50);

                        leftIndexFinger1Bone.localRotation = Quaternion.Euler(0, 0, 60f);
                        leftIndexFinger2Bone.localRotation = Quaternion.Euler(0, 0, 120f);
                        leftIndexFinger3Bone.localRotation = Quaternion.Euler(0, 0, 60f);

                        leftPinkyFinger1Bone.localRotation *= Quaternion.Euler(0, 0, 60);
                        leftPinkyFinger2Bone.localRotation *= Quaternion.Euler(0, 0, 100);
                        leftPinkyFinger3Bone.localRotation *= Quaternion.Euler(0, 0, 50);

                        leftHandTargetRotation *= Quaternion.Euler(-90, -80, -80);
                        Vector3 leftBendGoalTargetPosition = new Vector3(-1.5f, 0.5f, 0);

                        ik.solver.leftHandEffector.rotation = LeftHandTransformState.HandIKEffectorRotation;
                        if (Quaternion.Angle(ik.solver.leftHandEffector.rotation, leftHandTargetRotation) > 5f)
                        {
                            LeftHandTransformState.HandIKEffectorRotation = Quaternion.Slerp(
                                LeftHandTransformState.HandIKEffectorRotation,
                                leftHandTargetRotation,
                                Time.deltaTime * moveSpeed * 5);
                            ik.solver.leftHandEffector.rotation = LeftHandTransformState.HandIKEffectorRotation;
                        }
                        else
                        {
                            LeftHandTransformState.HandIKEffectorRotation = leftHandTargetRotation;
                        }

                        ik.solver.leftArmChain.bendConstraint.bendGoal.localPosition = LeftHandTransformState.BendGoalLocalPosition;
                        if (Vector3.Distance(LeftHandTransformState.BendGoalLocalPosition, leftBendGoalTargetPosition) > arrivalThreshold)
                        {
                            LeftHandTransformState.BendGoalLocalPosition = Vector3.Lerp(
                                LeftHandTransformState.BendGoalLocalPosition,
                                leftBendGoalTargetPosition,
                                Time.deltaTime * moveSpeed);
                            ik.solver.leftArmChain.bendConstraint.bendGoal.localPosition = LeftHandTransformState.BendGoalLocalPosition;
                        }
                        else
                        {
                            LeftHandTransformState.BendGoalLocalPosition = leftBendGoalTargetPosition;
                        }

                        StoreRotationsLeftHand();
                    }
                    else
                    {
                        AnimateLastDetectedValuesLeftHand();
                    }
                }
                // If neither gesture was recognized.
                else
                {
                    leftHandGestureDetectedFrames = Math.Max(leftHandGestureDetectedFrames - 1, 0);
                    leftHandGestureLostFrames++;

                    // If it is certain that no gesture was recognized, animate the fingers.
                    if (leftHandGestureDetectedFrames < minGestureDetectedFrames || leftHandGestureLostFrames > maxGestureLostFames)
                    {
                        wasLeftHandGestureRecognized = false;

                        // This rotation is mainly aimed at the "hello" gesture, it represents the bending of the hand from left to right and vice versa.
                        float newWristAngle
                            = rotationSolver.FindThumbAndWristXRotation(handLandmarks.LeftIndexFinger1Position, handLandmarks.LeftHandPosition, LeftHandTransformState.IndexFinger1StartPos);
                        leftHandTargetRotation = ik.solver.leftHandEffector.rotation * Quaternion.Euler(-newWristAngle, 0, 0);
                        Vector3 leftHandBendGoalTargetPosition = new Vector3(-0.5f, 0.5f, 0);

                        ik.solver.leftHandEffector.rotation = LeftHandTransformState.HandIKEffectorRotation;
                        if (Quaternion.Angle(ik.solver.leftHandEffector.rotation, leftHandTargetRotation) > 5f)
                        {
                            LeftHandTransformState.HandIKEffectorRotation = Quaternion.Slerp(
                                LeftHandTransformState.HandIKEffectorRotation,
                                leftHandTargetRotation,
                                Time.deltaTime * moveSpeed * 2);
                            ik.solver.leftHandEffector.rotation = LeftHandTransformState.HandIKEffectorRotation;
                        }
                        else
                        {
                            ik.solver.leftHandEffector.rotation = leftHandTargetRotation;
                        }

                        ik.solver.leftArmChain.bendConstraint.bendGoal.localPosition = LeftHandTransformState.BendGoalLocalPosition;
                        if (Vector3.Distance(ik.solver.leftArmChain.bendConstraint.bendGoal.localPosition, leftHandBendGoalTargetPosition) > arrivalThreshold)
                        {
                            LeftHandTransformState.BendGoalLocalPosition = Vector3.Lerp(
                                LeftHandTransformState.BendGoalLocalPosition,
                                leftHandBendGoalTargetPosition,
                                Time.deltaTime * moveSpeed);
                        }
                        else
                        {
                            ik.solver.leftArmChain.bendConstraint.bendGoal.localPosition = leftHandBendGoalTargetPosition;
                        }

                        // Middle Finger
                        float newAngle = rotationSolver.FindRotationForFlexionAndExtention(handLandmarks.LeftMiddleFinger3Position, handLandmarks.LeftMiddleFinger2Position, LeftHandTransformState.MidFinger3StartPos);
                        rotationSolver.SetFingertipRotation(newAngle, leftMidFinger3Bone, leftMidFinger2Bone);
                        float newAngleMiddleFinger = newAngle;

                        newAngle = rotationSolver.FindRotationForFlexionAndExtention(handLandmarks.LeftMiddleFinger2Position, handLandmarks.LeftMiddleFinger1Position, LeftHandTransformState.MidFinger2StartPos);
                        rotationSolver.SetBaseOfTheFingerRotation(newAngle, leftMidFinger1Bone);

                        // Index Finger
                        newAngle = rotationSolver.FindRotationForFlexionAndExtention(handLandmarks.LeftIndexFinger3Position, handLandmarks.LeftIndexFinger2Position, LeftHandTransformState.IndexFinger3StartPos);
                        rotationSolver.SetFingertipRotation(newAngle, leftIndexFinger3Bone, leftIndexFinger2Bone);
                        float newAngleIndexFinger = newAngle;

                        newAngle = rotationSolver.FindRotationForFlexionAndExtention(handLandmarks.LeftIndexFinger2Position, handLandmarks.LeftIndexFinger1Position, LeftHandTransformState.IndexFinger2StartPos);
                        rotationSolver.SetBaseOfTheFingerRotation(newAngle, leftIndexFinger1Bone);

                        // Ring Finger
                        newAngle = rotationSolver.FindRotationForFlexionAndExtention(handLandmarks.LeftRingFinger3Position, handLandmarks.LeftRingFinger2Position, LeftHandTransformState.RingFinger3StartPos);
                        rotationSolver.SetFingertipRotation(newAngle, leftRingFinger3Bone, leftRingFinger2Bone);
                        float newAngleRingFinger = newAngle;

                        newAngle = rotationSolver.FindRotationForFlexionAndExtention(handLandmarks.LeftRingFinger2Position, handLandmarks.LeftRingFinger1Position, LeftHandTransformState.RingFinger2StartPos);
                        rotationSolver.SetBaseOfTheFingerRotation(newAngle, leftRingFinger1Bone);

                        // Pinky
                        newAngle = rotationSolver.FindRotationForFlexionAndExtention(handLandmarks.LeftPinkyFinger3Position, handLandmarks.LeftPinkyFinger2Position, LeftHandTransformState.PinkyFinger3StartPos);
                        rotationSolver.SetFingertipRotation(newAngle, leftPinkyFinger3Bone, leftPinkyFinger2Bone);
                        float newAnglePinky = newAngle;

                        newAngle = rotationSolver.FindRotationForFlexionAndExtention(handLandmarks.LeftPinkyFinger2Position, handLandmarks.LeftPinkyFinger1Position, LeftHandTransformState.PinkyFinger2StartPos);
                        rotationSolver.SetBaseOfTheFingerRotation(newAngle, leftPinkyFinger1Bone);

                        // Thumb
                        float newAngleThumb = rotationSolver.FindThumbAndWristXRotation(handLandmarks.LeftThumb3Position, handLandmarks.LeftThumb2Position, LeftHandTransformState.Thumb3StartPos);
                        leftThumb2Bone.localRotation *= Quaternion.Euler(-newAngleThumb, 0, 0);

                        StoreRotationsLeftHand();
                    }
                    // Otherwise animate last detected values.
                    else
                    {
                        AnimateLastDetectedValuesLeftHand();
                    }
                }
            }
            // Otherwise animate last detected values.
            else
            {
                AnimateLastDetectedValuesLeftHand();
            }
        }

        /// <summary>
        /// Rotates the wrist and fingers of the right hand using the output from the MediaPipe <see cref="GestureRecognizer"/> model.
        /// </summary>
        /// <param name="resultGestureRecognizer">Output from the MediaPipe <see cref="GestureRecognizer"/> model.</param>
        /// <param name="samplingTimes">List of timestamps from MediaPipe callbacks used to compute the sampling period for filtering.</param>
        public void SolveRightHand
            (GestureRecognizerResult resultGestureRecognizer,
             List<float> samplingTimes)
        {
            // Index of values ​​for the right hand in the list of coordinates from gesture recognizer model.
            int rightHandResultIndex = -1;

            if (resultGestureRecognizer.handedness != null)
            {
                rightHandResultIndex = resultGestureRecognizer.handedness.IndexOf(resultGestureRecognizer.handedness.Find(x => x.categories[0].categoryName == "Right"));
            }
            String rightHandGesture = "None";
            if (rightHandResultIndex != -1)
            {
                rightHandGesture = resultGestureRecognizer.gestures[rightHandResultIndex].categories[0].categoryName;
            }

            // If the right hand was detected, get world coordinates of the keypoints.
            if (rightHandResultIndex >= 0 && rightHandLostFrames < maxLostFrames)
            {
                // Use filtered landmarks for animation.
                ApplyFiterToRightHandLandmarks(resultGestureRecognizer, samplingTimes);
                List<Vector3> rightHandLandmarks = filteredRightHandLandmarks;

                handLandmarks.RightMiddleFinger3Position = rightHandLandmarks[11];
                handLandmarks.RightMiddleFinger2Position = rightHandLandmarks[10];
                handLandmarks.RightMiddleFinger1Position = rightHandLandmarks[9];

                handLandmarks.RightIndexFinger3Position = rightHandLandmarks[7];
                handLandmarks.RightIndexFinger2Position = rightHandLandmarks[6];
                handLandmarks.RightIndexFinger1Position = rightHandLandmarks[5];

                handLandmarks.RightRingFinger3Position = rightHandLandmarks[15];
                handLandmarks.RightRingFinger2Position = rightHandLandmarks[14];
                handLandmarks.RightRingFinger1Position = rightHandLandmarks[13];

                handLandmarks.RightPinkyFinger3Position = rightHandLandmarks[19];
                handLandmarks.RightPinkyFinger2Position = rightHandLandmarks[18];
                handLandmarks.RightPinkyFinger1Position = rightHandLandmarks[17];

                handLandmarks.RightThumb3Position = rightHandLandmarks[3];
                handLandmarks.RightThumb2Position = rightHandLandmarks[2];

                handLandmarks.RightHandPosition = rightHandLandmarks[0];

                // Get transform components of avatar fingers.
                Transform rightMidFinger3Bone = transform.Find(AvatarSceleton.RightMidFinger3);
                Transform rightMidFinger2Bone = transform.Find(AvatarSceleton.RightMidFinger2);
                Transform rightMidFinger1Bone = transform.Find(AvatarSceleton.RightMidFinger1);

                Transform rightIndexFinger1Bone = transform.Find(AvatarSceleton.RightIndexFinger1);
                Transform rightIndexFinger2Bone = transform.Find(AvatarSceleton.RightIndexFinger2);
                Transform rightIndexFinger3Bone = transform.Find(AvatarSceleton.RightIndexFinger3);

                Transform rightRingFinger1Bone = transform.Find(AvatarSceleton.RightRingFinger1);
                Transform rightRingFinger2Bone = transform.Find(AvatarSceleton.RightRingFinger2);
                Transform rightRingFinger3Bone = transform.Find(AvatarSceleton.RightRingFinger3);

                Transform rightPinkyFinger1Bone = transform.Find(AvatarSceleton.RightPinkyFinger1);
                Transform rightPinkyFinger2Bone = transform.Find(AvatarSceleton.RightPinkyFinger2);
                Transform rightPinkyFinger3Bone = transform.Find(AvatarSceleton.RightPinkyFinger3);

                Transform rightThumb1Bone = transform.Find(AvatarSceleton.RightThumb1);
                Transform rightThumb2Bone = transform.Find(AvatarSceleton.RightThumb2);
                Transform rightThumb3Bone = transform.Find(AvatarSceleton.RightThumb3);

                // If these are the very first landmarks detected, save the starting positions of the bones (relative to their parent transforms)
                // so that these values can be used ​​to calculate rotations later.
                if (RightHandTransformState.IsFirstHandLandmark)
                {
                    RightHandTransformState.IndexFinger3StartPos = new Vector3(0, handLandmarks.RightIndexFinger3Position.y - handLandmarks.RightIndexFinger2Position.y, 0);
                    RightHandTransformState.IndexFinger2StartPos = new Vector3(0, handLandmarks.RightIndexFinger2Position.y - handLandmarks.RightIndexFinger1Position.y, 0);
                    RightHandTransformState.IndexFinger1StartPos = new Vector3(handLandmarks.RightIndexFinger1Position.x - handLandmarks.RightHandPosition.x, handLandmarks.RightIndexFinger1Position.y - handLandmarks.RightHandPosition.y, 0);

                    RightHandTransformState.MidFinger3StartPos = new Vector3(handLandmarks.RightMiddleFinger3Position.x - handLandmarks.RightMiddleFinger2Position.x, handLandmarks.RightMiddleFinger3Position.y - handLandmarks.RightMiddleFinger2Position.y, 0);
                    RightHandTransformState.MidFinger2StartPos = new Vector3(0, handLandmarks.RightMiddleFinger2Position.y - handLandmarks.RightMiddleFinger1Position.y, 0);

                    RightHandTransformState.RingFinger3StartPos = new Vector3(0, handLandmarks.RightRingFinger3Position.y - handLandmarks.RightRingFinger2Position.y, 0);
                    RightHandTransformState.RingFinger2StartPos = new Vector3(0, handLandmarks.RightRingFinger2Position.y - handLandmarks.RightRingFinger1Position.y, 0);

                    RightHandTransformState.PinkyFinger3StartPos = new Vector3(0, handLandmarks.RightPinkyFinger3Position.y - handLandmarks.RightPinkyFinger2Position.y, 0);
                    RightHandTransformState.PinkyFinger2StartPos = new Vector3(0, handLandmarks.RightPinkyFinger2Position.y - handLandmarks.RightPinkyFinger1Position.y, 0);

                    RightHandTransformState.Thumb3StartPos = new Vector3(handLandmarks.RightThumb3Position.x - handLandmarks.RightThumb2Position.x, handLandmarks.RightThumb3Position.y - handLandmarks.RightThumb2Position.y, 0);

                    RightHandTransformState.IsFirstHandLandmark = false;
                }

                // Rotating the wrist and fingers.
                // If the thumbs up or thumbs down gesture was recognized, animate accordingly.
                if (rightHandGesture == "Thumb_Up" && resultGestureRecognizer.gestures[rightHandResultIndex].categories[0].score > 0.6)
                {
                    rightHandGestureDetectedFrames++;
                    rightHandGestureLostFrames = 0;

                    if (rightHandGestureDetectedFrames >= minGestureDetectedFrames && rightHandGestureLostFrames <= maxGestureLostFames)
                    {
                        wasRightHandGestureRecognized = true;

                        rightThumb1Bone.localRotation = Quaternion.Euler(57f, 35f, 30f); ;
                        rightThumb2Bone.localRotation = Quaternion.Euler(0, 0, 0);
                        rightThumb3Bone.localRotation = Quaternion.Euler(0, 0, 0);

                        rightMidFinger1Bone.localRotation = Quaternion.Euler(5f, 0, -85f);
                        rightMidFinger2Bone.localRotation = Quaternion.Euler(0, 0, -85f);
                        rightMidFinger3Bone.localRotation = Quaternion.Euler(0, 0, -80f);

                        rightRingFinger1Bone.localRotation = Quaternion.Euler(5f, 0, -85f);
                        rightRingFinger2Bone.localRotation = Quaternion.Euler(0, 0, -85f);
                        rightRingFinger3Bone.localRotation = Quaternion.Euler(0, 0, -80f);

                        rightIndexFinger1Bone.localRotation = Quaternion.Euler(5f, 0, -85f);
                        rightIndexFinger2Bone.localRotation = Quaternion.Euler(0, 0, -85f);
                        rightIndexFinger3Bone.localRotation = Quaternion.Euler(0, 0, -80f);

                        rightPinkyFinger1Bone.localRotation = Quaternion.Euler(5f, 0, -85f);
                        rightPinkyFinger2Bone.localRotation = Quaternion.Euler(0, 0, -85f);
                        rightPinkyFinger3Bone.localRotation = Quaternion.Euler(0, 0, -80f);

                        rightHandTargetRotation *= Quaternion.Euler(80, -60, -60);
                        Vector3 rightBendGoalTargetPosition = new Vector3(1.5f, 1f, 0);

                        ik.solver.rightHandEffector.rotation = RightHandTransformState.HandIKEffectorRotation;
                        if (Quaternion.Angle(ik.solver.rightHandEffector.rotation, rightHandTargetRotation) > 5f)
                        {
                            RightHandTransformState.HandIKEffectorRotation = Quaternion.Slerp(
                                RightHandTransformState.HandIKEffectorRotation,
                                rightHandTargetRotation,
                                Time.deltaTime * moveSpeed * 5);
                            ik.solver.rightHandEffector.rotation = RightHandTransformState.HandIKEffectorRotation;
                        }
                        else
                        {
                            RightHandTransformState.HandIKEffectorRotation = rightHandTargetRotation;
                        }

                        ik.solver.rightArmChain.bendConstraint.bendGoal.localPosition = RightHandTransformState.BendGoalLocalPosition;
                        if (Vector3.Distance(RightHandTransformState.BendGoalLocalPosition, rightBendGoalTargetPosition) > 0.03f)
                        {
                            RightHandTransformState.BendGoalLocalPosition = Vector3.Lerp(
                                RightHandTransformState.BendGoalLocalPosition,
                                rightBendGoalTargetPosition,
                                Time.deltaTime * moveSpeed);
                            ik.solver.rightArmChain.bendConstraint.bendGoal.localPosition = RightHandTransformState.BendGoalLocalPosition;
                        }
                        else
                        {
                            RightHandTransformState.BendGoalLocalPosition = rightBendGoalTargetPosition;
                        }

                        StoreRotationsRightHand();
                    }
                    else
                    {
                        AnimateLastDetectedValuesRightHand();
                    }
                }
                else if (rightHandGesture == "Thumb_Down" && resultGestureRecognizer.gestures[rightHandResultIndex].categories[0].score > 0.6)
                {
                    rightHandGestureDetectedFrames++;
                    rightHandGestureLostFrames = 0;

                    if (rightHandGestureDetectedFrames >= minGestureDetectedFrames && rightHandGestureLostFrames <= maxGestureLostFames)
                    {
                        wasRightHandGestureRecognized = true;

                        rightThumb1Bone.localRotation = Quaternion.Euler(57f, 35f, 30f);
                        rightThumb2Bone.localRotation = Quaternion.Euler(0, 0, 0);
                        rightThumb3Bone.localRotation = Quaternion.Euler(0, 0, 0);

                        rightMidFinger1Bone.localRotation = Quaternion.Euler(5f, 0, -85f);
                        rightMidFinger2Bone.localRotation = Quaternion.Euler(0, 0, -85f);
                        rightMidFinger3Bone.localRotation = Quaternion.Euler(0, 0, -80f);

                        rightRingFinger1Bone.localRotation = Quaternion.Euler(5f, 0, -85f);
                        rightRingFinger2Bone.localRotation = Quaternion.Euler(0, 0, -85f);
                        rightRingFinger3Bone.localRotation = Quaternion.Euler(0, 0, -80f);

                        rightIndexFinger1Bone.localRotation = Quaternion.Euler(5f, 0, -85f);
                        rightIndexFinger2Bone.localRotation = Quaternion.Euler(0, 0, -85f);
                        rightIndexFinger3Bone.localRotation = Quaternion.Euler(0, 0, -80f);

                        rightPinkyFinger1Bone.localRotation = Quaternion.Euler(5f, 0, -85f);
                        rightPinkyFinger2Bone.localRotation = Quaternion.Euler(0, 0, -85f);
                        rightPinkyFinger3Bone.localRotation = Quaternion.Euler(0, 0, -80f);

                        rightHandTargetRotation *= Quaternion.Euler(-90, -80, -80);
                        Vector3 rightBendGoalTargetPosition = new Vector3(1.5f, 1f, 0);

                        ik.solver.rightHandEffector.rotation = RightHandTransformState.HandIKEffectorRotation;
                        if (Quaternion.Angle(ik.solver.rightHandEffector.rotation, rightHandTargetRotation) > 5f)
                        {
                            RightHandTransformState.HandIKEffectorRotation = Quaternion.Slerp(
                                RightHandTransformState.HandIKEffectorRotation,
                                rightHandTargetRotation,
                                Time.deltaTime * moveSpeed * 5);
                            ik.solver.rightHandEffector.rotation = RightHandTransformState.HandIKEffectorRotation;
                        }
                        else
                        {
                            RightHandTransformState.HandIKEffectorRotation = rightHandTargetRotation;
                        }

                        ik.solver.rightArmChain.bendConstraint.bendGoal.localPosition = RightHandTransformState.BendGoalLocalPosition;
                        if (Vector3.Distance(RightHandTransformState.BendGoalLocalPosition, rightBendGoalTargetPosition) > 0.03f)
                        {
                            RightHandTransformState.BendGoalLocalPosition = Vector3.Lerp(
                                RightHandTransformState.BendGoalLocalPosition,
                                rightBendGoalTargetPosition,
                                Time.deltaTime * moveSpeed);
                            ik.solver.rightArmChain.bendConstraint.bendGoal.localPosition = RightHandTransformState.BendGoalLocalPosition;
                        }
                        else
                        {
                            RightHandTransformState.BendGoalLocalPosition = rightBendGoalTargetPosition;
                        }

                        StoreRotationsRightHand();
                    }
                    else
                    {
                        AnimateLastDetectedValuesRightHand();
                    }
                }
                // If neither gesture was recognized.
                else
                {
                    rightHandGestureDetectedFrames = Mathf.Max(rightHandGestureDetectedFrames - 1, 0);
                    rightHandGestureLostFrames++;

                    // If it is certain that no gesture was recognized, animate the fingers.
                    if (rightHandGestureDetectedFrames < minGestureDetectedFrames || rightHandGestureLostFrames > maxGestureLostFames)
                    {
                        wasRightHandGestureRecognized = false;

                        // This rotation is mainly aimed at the "hello" gesture, it represents the bending of the hand from left to right and vice versa.
                        float newWristAngle
                            = rotationSolver.FindThumbAndWristXRotation(handLandmarks.RightIndexFinger1Position, handLandmarks.RightHandPosition, RightHandTransformState.IndexFinger1StartPos);
                        rightHandTargetRotation = ik.solver.rightHandEffector.rotation * Quaternion.Euler(newWristAngle, 0, 0);
                        Vector3 rightBendGoalTargetPosition = new Vector3(0.5f, 0.5f, 0);

                        ik.solver.rightHandEffector.rotation = RightHandTransformState.HandIKEffectorRotation;
                        if (Quaternion.Angle(ik.solver.rightHandEffector.rotation, rightHandTargetRotation) > 5f)
                        {
                            RightHandTransformState.HandIKEffectorRotation = Quaternion.Slerp(
                                RightHandTransformState.HandIKEffectorRotation,
                                rightHandTargetRotation,
                                Time.deltaTime * moveSpeed * 2);
                            ik.solver.rightHandEffector.rotation = RightHandTransformState.HandIKEffectorRotation;
                        }
                        else
                        {
                            ik.solver.rightHandEffector.rotation = rightHandTargetRotation;
                        }

                        ik.solver.rightArmChain.bendConstraint.bendGoal.localPosition = RightHandTransformState.BendGoalLocalPosition;
                        if (Vector3.Distance(ik.solver.rightArmChain.bendConstraint.bendGoal.localPosition, rightBendGoalTargetPosition) > arrivalThreshold)
                        {
                            RightHandTransformState.BendGoalLocalPosition = Vector3.Lerp(
                                RightHandTransformState.BendGoalLocalPosition,
                                rightBendGoalTargetPosition,
                                Time.deltaTime * moveSpeed);
                        }
                        else
                        {
                            ik.solver.rightArmChain.bendConstraint.bendGoal.localPosition = rightBendGoalTargetPosition;
                        }

                        // Middle Finger
                        float newAngle = rotationSolver.FindRotationForFlexionAndExtention(handLandmarks.RightMiddleFinger3Position, handLandmarks.RightMiddleFinger2Position, RightHandTransformState.MidFinger3StartPos);
                        rotationSolver.SetFingertipRotation(-newAngle, rightMidFinger3Bone, rightMidFinger2Bone);
                        float newAngleMiddleFinger = newAngle;

                        newAngle = rotationSolver.FindRotationForFlexionAndExtention(handLandmarks.RightMiddleFinger2Position, handLandmarks.RightMiddleFinger1Position, RightHandTransformState.MidFinger2StartPos);
                        rotationSolver.SetBaseOfTheFingerRotation(-newAngle, rightMidFinger1Bone);

                        // Index Finger
                        newAngle = rotationSolver.FindRotationForFlexionAndExtention(handLandmarks.RightIndexFinger3Position, handLandmarks.RightIndexFinger2Position, RightHandTransformState.IndexFinger3StartPos);
                        rotationSolver.SetFingertipRotation(-newAngle, rightIndexFinger3Bone, rightIndexFinger2Bone);
                        float newAngleIndexFinger = newAngle;

                        newAngle = rotationSolver.FindRotationForFlexionAndExtention(handLandmarks.RightIndexFinger2Position, handLandmarks.RightIndexFinger1Position, RightHandTransformState.IndexFinger2StartPos);
                        rotationSolver.SetBaseOfTheFingerRotation(-newAngle, rightIndexFinger1Bone);

                        // Ring Finger
                        newAngle = rotationSolver.FindRotationForFlexionAndExtention(handLandmarks.RightRingFinger3Position, handLandmarks.RightRingFinger2Position, RightHandTransformState.RingFinger3StartPos);
                        rotationSolver.SetFingertipRotation(-newAngle, rightRingFinger3Bone, rightRingFinger2Bone);
                        float newAngleRingFinger = newAngle;

                        newAngle = rotationSolver.FindRotationForFlexionAndExtention(handLandmarks.RightRingFinger2Position, handLandmarks.RightRingFinger1Position, RightHandTransformState.RingFinger2StartPos);
                        rotationSolver.SetBaseOfTheFingerRotation(-newAngle, rightRingFinger1Bone);

                        // Pinky
                        newAngle = rotationSolver.FindRotationForFlexionAndExtention(handLandmarks.RightPinkyFinger3Position, handLandmarks.RightPinkyFinger2Position, RightHandTransformState.PinkyFinger3StartPos);
                        rotationSolver.SetFingertipRotation(-newAngle, rightPinkyFinger3Bone, rightPinkyFinger2Bone);
                        float newAnglePinky = newAngle;

                        newAngle = rotationSolver.FindRotationForFlexionAndExtention(handLandmarks.RightPinkyFinger2Position, handLandmarks.RightPinkyFinger1Position, RightHandTransformState.PinkyFinger2StartPos);
                        rotationSolver.SetBaseOfTheFingerRotation(-newAngle, rightPinkyFinger1Bone);

                        // Thumb
                        float newAngleThumb = rotationSolver.FindThumbAndWristXRotation(handLandmarks.RightThumb3Position, handLandmarks.RightThumb2Position, RightHandTransformState.Thumb3StartPos);
                        rightThumb2Bone.localRotation *= Quaternion.Euler(newAngleThumb, 0, 0);

                        StoreRotationsRightHand();
                    }
                    // Otherwise animate last detected values.
                    else
                    {
                        AnimateLastDetectedValuesRightHand();
                    }
                }
            }
            // Otherwise animate last detected values.
            else
            {
                AnimateLastDetectedValuesRightHand();
            }
        }

        /// <summary>
        /// Stores values ​​for synchronizing the avatar's finger animations between clients.
        /// </summary>
        /// <remarks>
        /// This is necessary because when the user's left hand is not in the camera,
        /// the <see cref="SolveLeftHand(GestureRecognizerResult, List{float})"/>
        /// function is not called, causing the stored values ​​for the avatar's finger rotations to not be updated
        /// to properly synchronize animations between players.
        /// This is also necessary to avoid laggy movements if a gesture was falsely not recognized.
        /// </remarks>
        public void StoreRotationsLeftHand()
        {
            // Get transform components of avatar fingers.
            Transform leftMidFinger3Bone = transform.Find(AvatarSceleton.LeftMidFinger3);
            Transform leftMidFinger2Bone = transform.Find(AvatarSceleton.LeftMidFinger2);
            Transform leftMidFinger1Bone = transform.Find(AvatarSceleton.LeftMidFinger1);

            Transform leftIndexFinger1Bone = transform.Find(AvatarSceleton.LeftIndexFinger1);
            Transform leftIndexFinger2Bone = transform.Find(AvatarSceleton.LeftIndexFinger2);
            Transform leftIndexFinger3Bone = transform.Find(AvatarSceleton.LeftIndexFinger3);

            Transform leftRingFinger1Bone = transform.Find(AvatarSceleton.LeftRingFinger1);
            Transform leftRingFinger2Bone = transform.Find(AvatarSceleton.LeftRingFinger2);
            Transform leftRingFinger3Bone = transform.Find(AvatarSceleton.LeftRingFinger3);

            Transform leftPinkyFinger1Bone = transform.Find(AvatarSceleton.LeftPinkyFinger1);
            Transform leftPinkyFinger2Bone = transform.Find(AvatarSceleton.LeftPinkyFinger2);
            Transform leftPinkyFinger3Bone = transform.Find(AvatarSceleton.LeftPinkyFinger3);

            Transform leftThumb1Bone = transform.Find(AvatarSceleton.LeftThumb1);
            Transform leftThumb2Bone = transform.Find(AvatarSceleton.LeftThumb2);
            Transform leftThumb3Bone = transform.Find(AvatarSceleton.LeftThumb3);

            // Save information about current hand and finger rotations.
            LeftHandTransformState.HandIKEffectorPosition = ik.solver.leftHandEffector.position;
            LeftHandTransformState.HandIKEffectorRotation = ik.solver.leftHandEffector.rotation;
            LeftHandTransformState.IndexFingerRotations = new Vector3(leftIndexFinger1Bone.localRotation.eulerAngles.z,
                                                                        leftIndexFinger2Bone.localRotation.eulerAngles.z,
                                                                        leftIndexFinger3Bone.localRotation.eulerAngles.z);
            LeftHandTransformState.MiddleFingerRotations = new Vector3(leftMidFinger1Bone.localRotation.eulerAngles.z,
                                                                        leftMidFinger2Bone.localRotation.eulerAngles.z,
                                                                        leftMidFinger3Bone.localRotation.eulerAngles.z);
            LeftHandTransformState.RingFingerRotations = new Vector3(leftRingFinger1Bone.localRotation.eulerAngles.z,
                                                                     leftRingFinger2Bone.localRotation.eulerAngles.z,
                                                                     leftRingFinger3Bone.localRotation.eulerAngles.z);
            LeftHandTransformState.PinkyFingerRotations = new Vector3(leftPinkyFinger1Bone.localRotation.eulerAngles.z,
                                                                      leftPinkyFinger2Bone.localRotation.eulerAngles.z,
                                                                      leftPinkyFinger3Bone.localRotation.eulerAngles.z);
            LeftHandTransformState.Thumb1Rotations = leftThumb1Bone.localRotation;
            LeftHandTransformState.Thumb2Rotations = leftThumb2Bone.localRotation;
            LeftHandTransformState.Thumb3Rotations = leftThumb3Bone.localRotation;
        }

        /// <summary>
        /// Stores values ​​for synchronizing the avatar's finger animations between clients.
        /// </summary>
        /// <remarks>
        /// This is necessary because when the user's hands are not in the camera,
        /// the <see cref="SolveRightHand(GestureRecognizerResult, List{float})"/>
        /// function is not called, causing the stored values ​​for the avatar's finger rotations to not be updated
        /// to properly synchronize animations between players.
        /// This is also necessary to avoid laggy movements if a gesture was falsely not recognized.
        /// </remarks>
        public void StoreRotationsRightHand()
        {
            Transform rightMidFinger3Bone = transform.Find(AvatarSceleton.RightMidFinger3);
            Transform rightMidFinger2Bone = transform.Find(AvatarSceleton.RightMidFinger2);
            Transform rightMidFinger1Bone = transform.Find(AvatarSceleton.RightMidFinger1);

            Transform rightIndexFinger1Bone = transform.Find(AvatarSceleton.RightIndexFinger1);
            Transform rightIndexFinger2Bone = transform.Find(AvatarSceleton.RightIndexFinger2);
            Transform rightIndexFinger3Bone = transform.Find(AvatarSceleton.RightIndexFinger3);

            Transform rightRingFinger1Bone = transform.Find(AvatarSceleton.RightRingFinger1);
            Transform rightRingFinger2Bone = transform.Find(AvatarSceleton.RightRingFinger2);
            Transform rightRingFinger3Bone = transform.Find(AvatarSceleton.RightRingFinger3);

            Transform rightPinkyFinger1Bone = transform.Find(AvatarSceleton.RightPinkyFinger1);
            Transform rightPinkyFinger2Bone = transform.Find(AvatarSceleton.RightPinkyFinger2);
            Transform rightPinkyFinger3Bone = transform.Find(AvatarSceleton.RightPinkyFinger3);

            Transform rightThumb1Bone = transform.Find(AvatarSceleton.RightThumb1);
            Transform rightThumb2Bone = transform.Find(AvatarSceleton.RightThumb2);
            Transform rightThumb3Bone = transform.Find(AvatarSceleton.RightThumb3);

            // Save information about current hand and finger rotations.
            RightHandTransformState.HandIKEffectorPosition = ik.solver.rightHandEffector.position;
            RightHandTransformState.HandIKEffectorRotation = ik.solver.rightHandEffector.rotation;
            RightHandTransformState.IndexFingerRotations = new Vector3(rightIndexFinger1Bone.localRotation.eulerAngles.z,
                                                                       rightIndexFinger2Bone.localRotation.eulerAngles.z,
                                                                       rightIndexFinger3Bone.localRotation.eulerAngles.z);
            RightHandTransformState.MiddleFingerRotations = new Vector3(rightMidFinger1Bone.localRotation.eulerAngles.z,
                                                                        rightMidFinger2Bone.localRotation.eulerAngles.z,
                                                                        rightMidFinger3Bone.localRotation.eulerAngles.z);
            RightHandTransformState.RingFingerRotations = new Vector3(rightRingFinger1Bone.localRotation.eulerAngles.z,
                                                                      rightRingFinger2Bone.localRotation.eulerAngles.z,
                                                                      rightRingFinger3Bone.localRotation.eulerAngles.z);
            RightHandTransformState.PinkyFingerRotations = new Vector3(rightPinkyFinger1Bone.localRotation.eulerAngles.z,
                                                                       rightPinkyFinger2Bone.localRotation.eulerAngles.z,
                                                                       rightPinkyFinger3Bone.localRotation.eulerAngles.z);
            RightHandTransformState.Thumb1Rotations = rightThumb1Bone.localRotation;
            RightHandTransformState.Thumb2Rotations = rightThumb2Bone.localRotation;
            RightHandTransformState.Thumb3Rotations = rightThumb3Bone.localRotation;
        }

        /// <summary>
        /// Recalibrates the user's starting hand positions for better hand animations.
        /// </summary>
        public bool RecalibrateHandsStartPositions(GestureRecognizerResult resultGestureRecognizer)
        {
            // Index of values ​​for the right hand in the list of coordinates from gesture recognizer model.
            int rightHandResultIndex
                = resultGestureRecognizer.handedness.IndexOf(resultGestureRecognizer.handedness.Find(x => x.categories[0].categoryName == "Right"));

            // Index of values ​​for the left hand in the list of coordinates from gesture recognizer model.
            int leftHandResultIndex
                = resultGestureRecognizer.handedness.IndexOf(resultGestureRecognizer.handedness.Find(x => x.categories[0].categoryName == "Left"));

            if (leftHandResultIndex >= 0 && rightHandResultIndex >= 0)
            {
                LeftHandTransformState.IndexFinger3StartPos = new Vector3(0, handLandmarks.LeftIndexFinger3Position.y - handLandmarks.LeftIndexFinger2Position.y, 0);
                LeftHandTransformState.IndexFinger2StartPos = new Vector3(0, handLandmarks.LeftIndexFinger2Position.y - handLandmarks.LeftIndexFinger1Position.y, 0);
                LeftHandTransformState.IndexFinger1StartPos = new Vector3(handLandmarks.LeftIndexFinger1Position.x - handLandmarks.LeftHandPosition.x, handLandmarks.LeftIndexFinger1Position.y - handLandmarks.LeftHandPosition.y, 0);

                LeftHandTransformState.MidFinger3StartPos = new Vector3(handLandmarks.LeftMiddleFinger3Position.x - handLandmarks.LeftMiddleFinger2Position.x, handLandmarks.LeftMiddleFinger3Position.y - handLandmarks.LeftMiddleFinger2Position.y, 0);
                LeftHandTransformState.MidFinger2StartPos = new Vector3(0, handLandmarks.LeftMiddleFinger2Position.y - handLandmarks.LeftMiddleFinger1Position.y, 0);

                LeftHandTransformState.RingFinger3StartPos = new Vector3(0, handLandmarks.LeftRingFinger3Position.y - handLandmarks.LeftRingFinger2Position.y, 0);
                LeftHandTransformState.RingFinger2StartPos = new Vector3(0, handLandmarks.LeftRingFinger2Position.y - handLandmarks.LeftRingFinger1Position.y, 0);

                LeftHandTransformState.PinkyFinger3StartPos = new Vector3(0, handLandmarks.LeftPinkyFinger3Position.y - handLandmarks.LeftPinkyFinger2Position.y, 0);
                LeftHandTransformState.PinkyFinger2StartPos = new Vector3(0, handLandmarks.LeftPinkyFinger2Position.y - handLandmarks.LeftPinkyFinger1Position.y, 0);

                LeftHandTransformState.Thumb3StartPos = new Vector3(handLandmarks.LeftThumb3Position.x - handLandmarks.LeftThumb2Position.x, handLandmarks.LeftThumb3Position.y - handLandmarks.LeftThumb2Position.y, 0);


                RightHandTransformState.IndexFinger3StartPos = new Vector3(0, handLandmarks.RightIndexFinger3Position.y - handLandmarks.RightIndexFinger2Position.y, 0);
                RightHandTransformState.IndexFinger2StartPos = new Vector3(0, handLandmarks.RightIndexFinger2Position.y - handLandmarks.RightIndexFinger1Position.y, 0);
                RightHandTransformState.IndexFinger1StartPos = new Vector3(handLandmarks.RightIndexFinger1Position.x - handLandmarks.RightHandPosition.x, handLandmarks.RightIndexFinger1Position.y - handLandmarks.RightHandPosition.y, 0);

                RightHandTransformState.MidFinger3StartPos = new Vector3(handLandmarks.RightMiddleFinger3Position.x - handLandmarks.RightMiddleFinger2Position.x, handLandmarks.RightMiddleFinger3Position.y - handLandmarks.RightMiddleFinger2Position.y, 0);
                RightHandTransformState.MidFinger2StartPos = new Vector3(0, handLandmarks.RightMiddleFinger2Position.y - handLandmarks.RightMiddleFinger1Position.y, 0);

                RightHandTransformState.RingFinger3StartPos = new Vector3(0, handLandmarks.RightRingFinger3Position.y - handLandmarks.RightRingFinger2Position.y, 0);
                RightHandTransformState.RingFinger2StartPos = new Vector3(0, handLandmarks.RightRingFinger2Position.y - handLandmarks.RightRingFinger1Position.y, 0);

                RightHandTransformState.PinkyFinger3StartPos = new Vector3(0, handLandmarks.RightPinkyFinger3Position.y - handLandmarks.RightPinkyFinger2Position.y, 0);
                RightHandTransformState.PinkyFinger2StartPos = new Vector3(0, handLandmarks.RightPinkyFinger2Position.y - handLandmarks.RightPinkyFinger1Position.y, 0);

                RightHandTransformState.Thumb3StartPos = new Vector3(handLandmarks.RightThumb3Position.x - handLandmarks.RightThumb2Position.x, handLandmarks.RightThumb3Position.y - handLandmarks.RightThumb2Position.y, 0);

                return true;
            }
            return false;
        }

        /// <summary>
        /// Applies a One Euro Filter to all detected left hand landmarks from MediaPipe results.
        /// Filters are initialized on the first detection and then reused to smooth landmarks over time.
        /// </summary>
        /// <param name="gestureRecognizerResult">MediaPipe result containing raw landmarks.</param>
        /// <param name="samplingTimes">List of timestamps from MediaPipe callbacks used to compute the sampling period for filtering.</param>
        private void ApplyFiterToLeftHandLandmarks(GestureRecognizerResult gestureRecognizerResult, List<float> samplingTimes)
        {
            int leftHandResultIndex
                    = gestureRecognizerResult.handedness.IndexOf(gestureRecognizerResult.handedness.Find(x => x.categories[0].categoryName == "Left"));

            if (areFirstDetectedHandCoordinatesLeftHand)
            {
                foreach (Landmark landmark in gestureRecognizerResult.handWorldLandmarks[leftHandResultIndex].landmarks)
                {
                    leftHandLandmarksFilters.Add(new OneEuroFilter());
                    filteredLeftHandLandmarks.Add(new Vector3());
                }
                areFirstDetectedHandCoordinatesLeftHand = false;
            }

            List<Landmark> leftHandLandmarks = gestureRecognizerResult.handWorldLandmarks[leftHandResultIndex].landmarks;

            for (int i = 0; i < leftHandLandmarks.Count; i++)
            {
                filteredLeftHandLandmarks[i] = leftHandLandmarksFilters[i].ApplyFilterToHandLandmark(samplingTimes, leftHandLandmarks[i]);
            }
        }

        /// <summary>
        /// Applies a One Euro Filter to all detected right hand landmarks from MediaPipe results.
        /// Filters are initialized on the first detection and then reused to smooth landmarks over time.
        /// </summary>
        /// <param name="gestureRecognizerResult">MediaPipe result containing raw landmarks.</param>
        /// <param name="samplingTimes">List of timestamps from MediaPipe callbacks used to compute the sampling period for filtering.</param>
        private void ApplyFiterToRightHandLandmarks(GestureRecognizerResult gestureRecognizerResult, List<float> samplingTimes)
        {
            int rightHandResultIndex
                    = gestureRecognizerResult.handedness.IndexOf(gestureRecognizerResult.handedness.Find(x => x.categories[0].categoryName == "Right"));

            if (areFirstDetectedHandCoordinatesRightHand)
            {
                foreach (Landmark landmark in gestureRecognizerResult.handWorldLandmarks[rightHandResultIndex].landmarks)
                {
                    rightHandLandmarksFilters.Add(new OneEuroFilter());
                    filteredRightHandLandmarks.Add(new Vector3());
                }
                areFirstDetectedHandCoordinatesRightHand = false;
            }

            List<Landmark> rightHandLandmarks = gestureRecognizerResult.handWorldLandmarks[rightHandResultIndex].landmarks;

            for (int i = 0; i < rightHandLandmarks.Count; i++)
            {
                filteredRightHandLandmarks[i] = rightHandLandmarksFilters[i].ApplyFilterToHandLandmark(samplingTimes, rightHandLandmarks[i]);
            }
        }

        /// <summary>
        /// Applies the last detected values for the animation of the left hand to the avatar
        /// based on the values stored in the <see cref="LeftHandTransformState"/>.
        /// This is used when no new tracking data is available to keep the animation stable.
        /// </summary>
        public void AnimateLastDetectedValuesLeftHand()
        {
            ik.solver.leftHandEffector.rotation = LeftHandTransformState.HandIKEffectorRotation;

            Transform leftMidFinger3Bone = transform.Find(AvatarSceleton.LeftMidFinger3);
            Transform leftMidFinger2Bone = transform.Find(AvatarSceleton.LeftMidFinger2);
            Transform leftMidFinger1Bone = transform.Find(AvatarSceleton.LeftMidFinger1);
            leftMidFinger1Bone.localRotation = Quaternion.Euler(0, 0, LeftHandTransformState.MiddleFingerRotations.x);
            leftMidFinger2Bone.localRotation = Quaternion.Euler(0, 0, LeftHandTransformState.MiddleFingerRotations.y);
            leftMidFinger3Bone.localRotation = Quaternion.Euler(0, 0, LeftHandTransformState.MiddleFingerRotations.z);

            Transform leftIndexFinger1Bone = transform.Find(AvatarSceleton.LeftIndexFinger1);
            Transform leftIndexFinger2Bone = transform.Find(AvatarSceleton.LeftIndexFinger2);
            Transform leftIndexFinger3Bone = transform.Find(AvatarSceleton.LeftIndexFinger3);
            leftIndexFinger1Bone.localRotation = Quaternion.Euler(0, 0, LeftHandTransformState.IndexFingerRotations.x);
            leftIndexFinger2Bone.localRotation = Quaternion.Euler(0, 0, LeftHandTransformState.IndexFingerRotations.y);
            leftIndexFinger3Bone.localRotation = Quaternion.Euler(0, 0, LeftHandTransformState.IndexFingerRotations.z);

            Transform leftRingFinger1Bone = transform.Find(AvatarSceleton.LeftRingFinger1);
            Transform leftRingFinger2Bone = transform.Find(AvatarSceleton.LeftRingFinger2);
            Transform leftRingFinger3Bone = transform.Find(AvatarSceleton.LeftRingFinger3);
            leftRingFinger1Bone.localRotation = Quaternion.Euler(0, 0, LeftHandTransformState.RingFingerRotations.x);
            leftRingFinger2Bone.localRotation = Quaternion.Euler(0, 0, LeftHandTransformState.RingFingerRotations.y);
            leftRingFinger3Bone.localRotation = Quaternion.Euler(0, 0, LeftHandTransformState.RingFingerRotations.z);

            Transform leftPinkyFinger1Bone = transform.Find(AvatarSceleton.LeftPinkyFinger1);
            Transform leftPinkyFinger2Bone = transform.Find(AvatarSceleton.LeftPinkyFinger2);
            Transform leftPinkyFinger3Bone = transform.Find(AvatarSceleton.LeftPinkyFinger3);
            leftPinkyFinger1Bone.localRotation = Quaternion.Euler(0, 0, LeftHandTransformState.PinkyFingerRotations.x);
            leftPinkyFinger2Bone.localRotation = Quaternion.Euler(0, 0, LeftHandTransformState.PinkyFingerRotations.y);
            leftPinkyFinger3Bone.localRotation = Quaternion.Euler(0, 0, LeftHandTransformState.PinkyFingerRotations.z);

            Transform leftThumb1Bone = transform.Find(AvatarSceleton.LeftThumb1);
            Transform leftThumb2Bone = transform.Find(AvatarSceleton.LeftThumb2);
            Transform leftThumb3Bone = transform.Find(AvatarSceleton.LeftThumb3);
            leftThumb1Bone.localRotation = LeftHandTransformState.Thumb1Rotations;
            leftThumb2Bone.localRotation = LeftHandTransformState.Thumb2Rotations;
            leftThumb3Bone.localRotation = LeftHandTransformState.Thumb3Rotations;
        }

        /// <summary>
        /// Applies the last detected values for the animation of the right hand to the avatar
        /// based on the values stored in the <see cref="RightHandTransformState"/>.
        /// This is used when no new tracking data is available to keep the animation stable.
        /// </summary>
        public void AnimateLastDetectedValuesRightHand()
        {
            ik.solver.rightHandEffector.rotation = RightHandTransformState.HandIKEffectorRotation;

            Transform rightMidFinger3Bone = transform.Find(AvatarSceleton.RightMidFinger3);
            Transform rightMidFinger2Bone = transform.Find(AvatarSceleton.RightMidFinger2);
            Transform rightMidFinger1Bone = transform.Find(AvatarSceleton.RightMidFinger1);
            rightMidFinger1Bone.localRotation = Quaternion.Euler(0, 0, RightHandTransformState.MiddleFingerRotations.x);
            rightMidFinger2Bone.localRotation = Quaternion.Euler(0, 0, RightHandTransformState.MiddleFingerRotations.y);
            rightMidFinger3Bone.localRotation = Quaternion.Euler(0, 0, RightHandTransformState.MiddleFingerRotations.z);

            Transform rightIndexFinger1Bone = transform.Find(AvatarSceleton.RightIndexFinger1);
            Transform rightIndexFinger2Bone = transform.Find(AvatarSceleton.RightIndexFinger2);
            Transform rightIndexFinger3Bone = transform.Find(AvatarSceleton.RightIndexFinger3);
            rightIndexFinger1Bone.localRotation = Quaternion.Euler(0, 0, RightHandTransformState.IndexFingerRotations.x);
            rightIndexFinger2Bone.localRotation = Quaternion.Euler(0, 0, RightHandTransformState.IndexFingerRotations.y);
            rightIndexFinger3Bone.localRotation = Quaternion.Euler(0, 0, RightHandTransformState.IndexFingerRotations.z);

            Transform rightRingFinger1Bone = transform.Find(AvatarSceleton.RightRingFinger1);
            Transform rightRingFinger2Bone = transform.Find(AvatarSceleton.RightRingFinger2);
            Transform rightRingFinger3Bone = transform.Find(AvatarSceleton.RightRingFinger3);
            rightRingFinger1Bone.localRotation = Quaternion.Euler(0, 0, RightHandTransformState.RingFingerRotations.x);
            rightRingFinger2Bone.localRotation = Quaternion.Euler(0, 0, RightHandTransformState.RingFingerRotations.y);
            rightRingFinger3Bone.localRotation = Quaternion.Euler(0, 0, RightHandTransformState.RingFingerRotations.z);

            Transform rightPinkyFinger1Bone = transform.Find(AvatarSceleton.RightPinkyFinger1);
            Transform rightPinkyFinger2Bone = transform.Find(AvatarSceleton.RightPinkyFinger2);
            Transform rightPinkyFinger3Bone = transform.Find(AvatarSceleton.RightPinkyFinger3);
            rightPinkyFinger1Bone.localRotation = Quaternion.Euler(0, 0, RightHandTransformState.PinkyFingerRotations.x);
            rightPinkyFinger2Bone.localRotation = Quaternion.Euler(0, 0, RightHandTransformState.PinkyFingerRotations.y);
            rightPinkyFinger3Bone.localRotation = Quaternion.Euler(0, 0, RightHandTransformState.PinkyFingerRotations.z);

            Transform rightThumb1Bone = transform.Find(AvatarSceleton.RightThumb1);
            Transform rightThumb2Bone = transform.Find(AvatarSceleton.RightThumb2);
            Transform rightThumb3Bone = transform.Find(AvatarSceleton.RightThumb3);
            rightThumb1Bone.localRotation = RightHandTransformState.Thumb1Rotations;
            rightThumb2Bone.localRotation = RightHandTransformState.Thumb2Rotations;
            rightThumb3Bone.localRotation = RightHandTransformState.Thumb3Rotations;
        }
    }
}
