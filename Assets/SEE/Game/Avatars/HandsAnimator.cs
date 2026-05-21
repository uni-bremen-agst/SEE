using System;
using UnityEngine;
using RootMotion.FinalIK;
using Mediapipe.Tasks.Vision.PoseLandmarker;
using Mediapipe.Tasks.Vision.HandLandmarker;
using Mediapipe.Tasks.Vision.GestureRecognizer;
using System.Collections.Generic;
using Mediapipe.Tasks.Components.Containers;

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
        private const float acceptableHandPresenceProbability = 0.5f;

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
        private Quaternion leftHandRotationOffset = Quaternion.Euler(170, 110, 0);
        private Vector3 leftHandPositionOffset = new(-0.37f, 1.56f, 0.23f);
        private Quaternion rightHandRotationOffset = Quaternion.Euler(-40, 15, 60);
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
        MediaPipeHandLandmarks handLandmarks = new();

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

            // Depending on whether the avatar's laser pointer is turned on or not, the animation needs to be adjusted slightly.
            if (IsPointing)
            {
                leftHand.localRotation = startLeftHandRotation * leftHandRotationOffset;
                leftHandTargetRotation = leftHand.rotation;
                leftHand.localRotation = startLeftHandRotation;

                rightHand.localRotation = startRightHandRotation * rightHandRotationOffset;
                rightHandTargetRotation = rightHand.rotation;
                rightHand.localRotation = startRightHandRotation;
            }
            else
            {
                leftHand.localRotation = startLeftHandRotation * leftHandRotationOffset * Quaternion.Euler(0, 15f, 0);
                leftHandTargetRotation = leftHand.rotation;
                leftHand.localRotation = startLeftHandRotation;

                rightHand.localRotation = startRightHandRotation * rightHandRotationOffset * Quaternion.Euler(70f, 0, 130f);
                rightHandTargetRotation = rightHand.rotation;
                rightHand.localRotation = startRightHandRotation;
            }

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
        /// Changes the avatar's hand positions using the output from the mediapipe pose landmarker model.
        /// </summary>
        /// <param name="resultPoseLandmarker">Output from the mediapipe pose landmarker model.</param>
        public void SolveHandsPositions(PoseLandmarkerResult resultPoseLandmarker)
        {
            Transform leftHand = transform.Find(AvatarSceleton.LeftHand);
            Transform rightHand = transform.Find(AvatarSceleton.RightHand);

            // Depending on whether the avatar's laser pointer is turned on or not, the animation needs to be adjusted slightly.
            if (IsPointing)
            {
                leftHand.localRotation = startLeftHandRotation * leftHandRotationOffset;
                leftHandTargetRotation = leftHand.rotation;
                leftHand.localRotation = startLeftHandRotation;

                rightHand.localRotation = startRightHandRotation * rightHandRotationOffset;
                rightHandTargetRotation = rightHand.rotation;
                rightHand.localRotation = startRightHandRotation;
            }
            else
            {
                leftHand.localRotation = startLeftHandRotation * leftHandRotationOffset * Quaternion.Euler(0, 15f, 0);
                leftHandTargetRotation = leftHand.rotation;
                leftHand.localRotation = startLeftHandRotation;

                rightHand.localRotation = startRightHandRotation * rightHandRotationOffset * Quaternion.Euler(70f, 0, 130f);
                rightHandTargetRotation = rightHand.rotation;
                rightHand.localRotation = startRightHandRotation;
            }

            ik.solver.leftHandEffector.position = leftHandTargetPos;
            ik.solver.rightHandEffector.position = rightHandTargetPos;
            ik.solver.leftHandEffector.rotation = leftHandTargetRotation;
            ik.solver.rightHandEffector.rotation = rightHandTargetRotation;

            List<Landmark> poseLandmarks = resultPoseLandmarker.poseWorldLandmarks[0].landmarks;
            Landmark mediapipeLeftHandPosition = poseLandmarks[15];
            Landmark mediapipeRightHandPosition = poseLandmarks[16];

            Landmark mediapipeLeftElbowPosition = poseLandmarks[13];
            Landmark mediapipeRightElbowPosition = poseLandmarks[14];

            // Save the last detected coordinates.
            if (isFirstPoseLandmark)
            {
                LeftHandTransformState.NewMediapipeCoordinates.x = mediapipeLeftHandPosition.x;
                LeftHandTransformState.NewMediapipeCoordinates.y = mediapipeLeftHandPosition.y;
                LeftHandTransformState.NewMediapipeCoordinates.z = mediapipeLeftHandPosition.z;

                leftHandStartPos = new Vector3(mediapipeLeftHandPosition.x - mediapipeLeftElbowPosition.x, mediapipeLeftHandPosition.y - mediapipeLeftElbowPosition.y, 0);
                rightHandStartPos = new Vector3(mediapipeRightHandPosition.x - mediapipeRightElbowPosition.x, mediapipeRightHandPosition.y - mediapipeRightElbowPosition.y, 0);

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
                if(ik.solver.leftHandEffector.positionWeight <= 0.95f || ik.solver.leftArmChain.bendConstraint.weight <= 0.37f)
                {
                    ik.solver.leftHandEffector.positionWeight = Mathf.Lerp(ik.solver.leftHandEffector.positionWeight, weight, Time.deltaTime * moveSpeed * 2);
                    ik.solver.leftHandEffector.rotationWeight = Mathf.Lerp(ik.solver.leftHandEffector.rotationWeight, weight, Time.deltaTime * moveSpeed * 2);
                    ik.solver.leftArmChain.bendConstraint.weight = Mathf.Lerp(ik.solver.leftArmChain.bendConstraint.weight, 0.4f, Time.deltaTime * moveSpeed * 2);
                }

                LeftHandTransformState.HandToHeadCoordinateDifference = new Vector3(mediapipeLeftHandPosition.x - mediapipeHeadPosition.x, mediapipeLeftHandPosition.y - mediapipeHeadPosition.y, transform.InverseTransformPoint(leftHandTargetPos).z - headPosition.z);
                Vector3 newHandPosition = headPosition + LeftHandTransformState.HandToHeadCoordinateDifference;
                ik.solver.leftHandEffector.position = transform.TransformPoint(newHandPosition);

                // Interval where palm should be facing the camera.
                if (LeftHandTransformState.HandToHeadCoordinateDifference.x < handXCoordinatesDiffIntervalToFaceTheCamera.Item2
                    && LeftHandTransformState.HandToHeadCoordinateDifference.x > handXCoordinatesDiffIntervalToFaceTheCamera.Item1)
                {
                    if (IsPointing)
                    {
                        leftHand.localRotation = startLeftHandRotation * leftHandRotationOffset;
                    }
                    else
                    {
                        leftHand.localRotation = startLeftHandRotation * leftHandRotationOffset * Quaternion.Euler(0, 15f, 0);
                    }
                    leftHandTargetRotation = leftHand.rotation;
                    leftHand.localRotation = startLeftHandRotation;
                    LeftHandTransformState.HandRotation = Quaternion.Slerp(LeftHandTransformState.HandRotation, leftHandTargetRotation, Time.deltaTime * moveSpeed * 3);
                    ik.solver.leftHandEffector.rotation = LeftHandTransformState.HandRotation;
                }
                // If the hand is moving in front of the character.
                else if (LeftHandTransformState.HandToHeadCoordinateDifference.x >= handXCoordinatesDiffIntervalMovingInFront.Item1
                    && LeftHandTransformState.HandToHeadCoordinateDifference.x <= handXCoordinatesDiffIntervalMovingInFront.Item2)
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
                else if (LeftHandTransformState.PreviousMediapipeCoordinates.x > LeftHandTransformState.NewMediapipeCoordinates.x)
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
                if (LeftHandTransformState.HandToHeadCoordinateDifference.y <= handYCoordinatesDiffToMoveDownFrom)
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
            // If the probability with which the left hand is in the picture is NOT acceptable for animation,
            // assign the neutral position.
            else
            {
                if(ik.solver.leftHandEffector.positionWeight > 0.005f || ik.solver.leftArmChain.bendConstraint.weight > 0.005f)
                {
                    ik.solver.leftHandEffector.positionWeight = Mathf.Lerp(ik.solver.leftHandEffector.positionWeight, 0f, Time.deltaTime * moveSpeed * 2);
                    ik.solver.leftHandEffector.rotationWeight = Mathf.Lerp(ik.solver.leftHandEffector.rotationWeight, 0f, Time.deltaTime * moveSpeed * 2);
                    ik.solver.leftArmChain.bendConstraint.weight = Mathf.Lerp(ik.solver.leftArmChain.bendConstraint.weight, 0f, Time.deltaTime * moveSpeed * 2);
                }
            }

            // If the probability with which the right hand is in the picture is acceptable for animation.
            if (mediapipeRightHandPosition.presence > acceptableHandPresenceProbability && mediapipeRightHandPosition.visibility > acceptableHandPresenceProbability)
            {
                if(ik.solver.rightHandEffector.positionWeight <= 0.95f || ik.solver.rightArmChain.bendConstraint.weight <= 0.37f)
                {
                    ik.solver.rightHandEffector.positionWeight = Mathf.Lerp(ik.solver.rightHandEffector.positionWeight, weight, Time.deltaTime * moveSpeed * 2);
                    ik.solver.rightHandEffector.rotationWeight = Mathf.Lerp(ik.solver.rightHandEffector.rotationWeight, weight, Time.deltaTime * moveSpeed * 2);
                    ik.solver.rightArmChain.bendConstraint.weight = Mathf.Lerp(ik.solver.rightArmChain.bendConstraint.weight, 0.4f, Time.deltaTime * moveSpeed * 2);
                }


                RightHandTransformState.HandToHeadCoordinateDifference
                    = new Vector3(mediapipeRightHandPosition.x - mediapipeHeadPosition.x,
                                  mediapipeRightHandPosition.y - mediapipeHeadPosition.y,
                                  transform.InverseTransformPoint(rightHandTargetPos).z - headPosition.z);
                Vector3 newHandPosition = headPosition + RightHandTransformState.HandToHeadCoordinateDifference;
                ik.solver.rightHandEffector.position = transform.TransformPoint(newHandPosition);

                // Interval where palm should be facing the camera.
                if (RightHandTransformState.HandToHeadCoordinateDifference.x > -handXCoordinatesDiffIntervalToFaceTheCamera.Item2
                    && RightHandTransformState.HandToHeadCoordinateDifference.x < -handXCoordinatesDiffIntervalToFaceTheCamera.Item1)
                {
                    if (IsPointing)
                    {
                        rightHand.localRotation = startRightHandRotation * rightHandRotationOffset;
                    }
                    else
                    {
                        rightHand.localRotation = startRightHandRotation * rightHandRotationOffset * Quaternion.Euler(70f, 0, 130f);
                    }
                    rightHandTargetRotation = rightHand.rotation;
                    rightHand.localRotation = startRightHandRotation;
                    RightHandTransformState.HandRotation = Quaternion.Slerp(RightHandTransformState.HandRotation, rightHandTargetRotation, Time.deltaTime * moveSpeed * 3);
                    ik.solver.rightHandEffector.rotation = RightHandTransformState.HandRotation;
                }
                // If the hand is moving in front of the character.
                else if (RightHandTransformState.HandToHeadCoordinateDifference.x <= -handXCoordinatesDiffIntervalMovingInFront.Item1)
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
                else if (RightHandTransformState.PreviousMediapipeCoordinates.x < RightHandTransformState.NewMediapipeCoordinates.x)
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
                if (RightHandTransformState.HandToHeadCoordinateDifference.y <= handYCoordinatesDiffToMoveDownFrom)
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
            // If the probability with which the right hand is in the picture is NOT acceptable for animation,
            // assign the neutral position.
            else
            {
                if(ik.solver.rightHandEffector.positionWeight > 0.005f || ik.solver.rightArmChain.bendConstraint.weight > 0.005f)
                {
                    ik.solver.rightHandEffector.positionWeight = Mathf.Lerp(ik.solver.rightHandEffector.positionWeight, 0f, Time.deltaTime * moveSpeed * 2);
                    ik.solver.rightHandEffector.rotationWeight = Mathf.Lerp(ik.solver.rightHandEffector.rotationWeight, 0f, Time.deltaTime * moveSpeed * 2);
                    ik.solver.rightArmChain.bendConstraint.weight = Mathf.Lerp(ik.solver.rightArmChain.bendConstraint.weight, 0f, Time.deltaTime * moveSpeed * 2);
                }
            }

            // Save information about current hand positions and rotations.
            LeftHandTransformState.HandIKEffectorPosition = ik.solver.leftHandEffector.position;
            LeftHandTransformState.HandIKEffectorRotation = ik.solver.leftHandEffector.rotation;
            LeftHandTransformState.BendGoalConstraintWeight = ik.solver.leftArmChain.bendConstraint.weight;
            LeftHandTransformState.HandIKPositionWeight = ik.solver.leftHandEffector.positionWeight;
            LeftHandTransformState.HandIKRotationWeight = ik.solver.leftHandEffector.rotationWeight;

            RightHandTransformState.HandIKEffectorPosition = ik.solver.rightHandEffector.position;
            RightHandTransformState.HandIKEffectorRotation = ik.solver.rightHandEffector.rotation;
            RightHandTransformState.BendGoalConstraintWeight = ik.solver.rightArmChain.bendConstraint.weight;
            RightHandTransformState.HandIKPositionWeight = ik.solver.rightHandEffector.positionWeight;
            RightHandTransformState.HandIKRotationWeight = ik.solver.rightHandEffector.rotationWeight;
        }

        /// <summary>
        /// Rotates the wrist and fingers of the left hand using the outputs from MediaPipe Models.
        /// </summary>
        /// <param name="resultHandLandmarker">Output from the mediapipe hand landmarker model.</param>
        /// <param name="resultGestureRecognizer">Output from the mediapipe gesture recognizer model.</param>
        /// <param name="resultPoseLandmarker">Output from the mediapipe pose landmarker model.</param>
        public void SolveLeftHand(HandLandmarkerResult resultHandLandmarker,
                                  GestureRecognizerResult resultGestureRecognizer,
                                  PoseLandmarkerResult resultPoseLandmarker)
        {
            // Index of values ​​for the left hand in the list of coordinates from hand landmarker model.
            int leftHandResultIndex
                = resultHandLandmarker.handedness.IndexOf(resultHandLandmarker.handedness.Find(x => x.categories[0].categoryName == "Left"));

            int leftHandInTheGesturesList = -1;
            if (resultGestureRecognizer.handedness != null)
            {
                //Index of values ​​for the left hand in the output from gesture recognizer model.
                leftHandInTheGesturesList
                    = resultGestureRecognizer.handedness.IndexOf(resultGestureRecognizer.handedness.Find(x => x.categories[0].categoryName == "Left"));
            }
            string leftHandGesture = "None";
            if (leftHandInTheGesturesList != -1)
            {
                leftHandGesture = resultGestureRecognizer.gestures[leftHandInTheGesturesList].categories[0].categoryName;
            }

            // If the left hand was detected, get world coordinates of the keypoints.
            if (leftHandResultIndex >= 0)
            {
                List<Landmark> leftHandLandmarks = resultHandLandmarker.handWorldLandmarks[leftHandResultIndex].landmarks;

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

                    LeftHandTransformState.MidFinger3StartPos = new Vector3(handLandmarks.LeftMiddleFinger3Position.x - handLandmarks.LeftMiddleFinger2Position.x, handLandmarks.LeftMiddleFinger3Position.y - handLandmarks.LeftMiddleFinger2Position.y, 0);
                    LeftHandTransformState.MidFinger2StartPos = new Vector3(0, handLandmarks.LeftMiddleFinger2Position.y - handLandmarks.LeftMiddleFinger1Position.y, 0);

                    LeftHandTransformState.RingFinger3StartPos = new Vector3(0, handLandmarks.LeftRingFinger3Position.y - handLandmarks.LeftRingFinger2Position.y, 0);
                    LeftHandTransformState.RingFinger2StartPos = new Vector3(0, handLandmarks.LeftRingFinger2Position.y - handLandmarks.LeftRingFinger1Position.y, 0);

                    LeftHandTransformState.PinkyFinger3StartPos = new Vector3(0, handLandmarks.LeftPinkyFinger3Position.y - handLandmarks.LeftPinkyFinger2Position.y, 0);
                    LeftHandTransformState.PinkyFinger2StartPos = new Vector3(0, handLandmarks.LeftPinkyFinger2Position.y - handLandmarks.LeftPinkyFinger1Position.y, 0);

                    LeftHandTransformState.Thumb3StartPos = new Vector3(handLandmarks.LeftThumb3Position.x - handLandmarks.LeftThumb2Position.x, handLandmarks.LeftThumb3Position.y - handLandmarks.LeftThumb2Position.y, 0);

                    LeftHandTransformState.IndexFinger1StartPos = new Vector3(handLandmarks.LeftIndexFinger1Position.x - handLandmarks.LeftHandPosition.x, handLandmarks.LeftIndexFinger1Position.y - handLandmarks.LeftHandPosition.y, 0);

                    LeftHandTransformState.IsFirstHandLandmark = false;
                }

                // Rotating the wrist.
                // Interval where palm should be facing the camera.
                if (LeftHandTransformState.HandToHeadCoordinateDifference.x < handXCoordinatesDiffIntervalToFaceTheCamera.Item2
                    && LeftHandTransformState.HandToHeadCoordinateDifference.x > handXCoordinatesDiffIntervalToFaceTheCamera.Item1)
                {
                    List<Landmark> poseLandmarks = resultPoseLandmarker.poseWorldLandmarks[0].landmarks;
                    Landmark mediapipeLeftHandPosition = poseLandmarks[15];
                    Landmark mediapipeLeftElbowPosition = poseLandmarks[13];

                    float newElbowRotation = rotationSolver.FindElbowRotation(mediapipeLeftHandPosition, mediapipeLeftElbowPosition, leftHandStartPos);

                    float widthDiffHandToElbow = mediapipeLeftHandPosition.x - mediapipeLeftElbowPosition.x;

                    // If the user has bent his arm at the elbow, animate the avatar accordingly.
                    if (mediapipeLeftElbowPosition.presence >= 0.4 && !float.IsNaN(newElbowRotation) && newElbowRotation >= 40 && widthDiffHandToElbow <= 0.08)
                    {
                        leftHandTargetRotation = startLeftHandRotation * leftHandRotationOffset;
                        ik.solver.leftHandEffector.rotation *= Quaternion.Euler(0, 0, newElbowRotation + leftHandTargetRotation.z - ik.solver.leftHandEffector.rotation.eulerAngles.z);
                        ik.solver.leftArmChain.bendConstraint.bendGoal.localPosition = new Vector3(-0.5f, 0.5f + newElbowRotation / 100, 0);
                        LeftHandTransformState.BendGoalLocalPosition = ik.solver.leftArmChain.bendConstraint.bendGoal.localPosition;

                        leftThumb1Bone.localRotation *= Quaternion.Euler(0, 0, 0);
                        leftThumb2Bone.localRotation *= Quaternion.Euler(-20, 0, 0);
                        leftThumb3Bone.localRotation *= Quaternion.Euler(-20, 0, 0);

                        leftMidFinger1Bone.localRotation = Quaternion.Euler(0, 0, 60);
                        leftMidFinger2Bone.localRotation = Quaternion.Euler(0, 0, 100);
                        leftMidFinger3Bone.localRotation = Quaternion.Euler(0, 0, 50);

                        leftRingFinger1Bone.localRotation = Quaternion.Euler(0, 0, 60);
                        leftRingFinger2Bone.localRotation = Quaternion.Euler(0, 0, 100);
                        leftRingFinger3Bone.localRotation = Quaternion.Euler(0, 0, 50);

                        leftIndexFinger1Bone.localRotation = Quaternion.Euler(0, 0, 20);
                        leftIndexFinger2Bone.localRotation = Quaternion.Euler(0, 0, 0);
                        leftIndexFinger3Bone.localRotation = Quaternion.Euler(0, 0, 0);

                        leftPinkyFinger1Bone.localRotation = Quaternion.Euler(0, 0, 60);
                        leftPinkyFinger2Bone.localRotation = Quaternion.Euler(0, 0, 100);
                        leftPinkyFinger3Bone.localRotation = Quaternion.Euler(0, 0, 50);
                    }
                    else
                    {
                        // This rotation is mainly aimed at the "hello" gesture, it represents the bending of the hand from left to right and vice versa.
                        float newWristAngle = rotationSolver.FindThumbAndWristXRotation(handLandmarks.LeftIndexFinger1Position, handLandmarks.LeftHandPosition, LeftHandTransformState.IndexFinger1StartPos);
                        ik.solver.leftHandEffector.rotation *= Quaternion.Euler(-newWristAngle, 0, 0);

                        // If the thumbs up or thumbs down gesture was recognized, animate accordingly.
                        if (leftHandGesture == "Thumb_Up")
                        {
                            ik.solver.leftHandEffector.rotation = leftHandTargetRotation * Quaternion.Euler(-90, -80, -80);
                            LeftHandTransformState.HandIKRotationWeight = ik.solver.leftHandEffector.rotationWeight;
                            LeftHandTransformState.HandIKPositionWeight = ik.solver.leftHandEffector.positionWeight;
                            ik.solver.leftArmChain.bendConstraint.bendGoal.localPosition = new Vector3(-1.5f, 0.5f, 0);
                            LeftHandTransformState.BendGoalLocalPosition = ik.solver.leftArmChain.bendConstraint.bendGoal.localPosition;
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
                        }
                        else if (leftHandGesture == "Thumb_Down")
                        {
                            ik.solver.leftHandEffector.rotation = leftHandTargetRotation * Quaternion.Euler(80, -60, -60);
                            LeftHandTransformState.HandIKRotationWeight = ik.solver.leftHandEffector.rotationWeight;
                            LeftHandTransformState.HandIKPositionWeight = ik.solver.leftHandEffector.positionWeight;
                            ik.solver.leftArmChain.bendConstraint.bendGoal.localPosition = new Vector3(-1.5f, 0.5f, 0);
                            LeftHandTransformState.BendGoalLocalPosition = ik.solver.leftArmChain.bendConstraint.bendGoal.localPosition;
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
                        }
                        // If neither gesture was recognized, animate fingers.
                        else
                        {
                            ik.solver.leftArmChain.bendConstraint.bendGoal.localPosition = new Vector3(-0.5f, 0.5f, 0);
                            LeftHandTransformState.BendGoalLocalPosition = ik.solver.leftArmChain.bendConstraint.bendGoal.localPosition;

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

                            // Animate the palm rotation from facing forward to the back of the palm facing forward.
                            // It's important to check whether the user has flexed their fingers, as flexing the fingers will
                            // sometimes be interpreted as a palm rotation due to the lack of Z-coordinate.
                            if (handLandmarks.LeftIndexFinger1Position.y - LeftHandTransformState.IndexFinger1StartPos.y <= 0.005f)
                            {
                                newWristAngle = rotationSolver.FindWristYRotation(handLandmarks.LeftIndexFinger1Position, handLandmarks.LeftHandPosition, LeftHandTransformState.IndexFinger1StartPos);
                                if (!float.IsNaN(newWristAngle) && newWristAngle <= 120f && !AreFingersBent(newAngleIndexFinger, newAngleMiddleFinger, newAngleRingFinger, newAnglePinky))
                                {
                                    ik.solver.leftHandEffector.rotation *= Quaternion.Euler(0, newWristAngle, 0);
                                }
                            }
                        }
                    }
                }
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
        }

        /// <summary>
        /// Rotates the wrist and fingers of the right hand using the outputs from MediaPipe Models.
        /// </summary>
        /// <param name="resultHandLandmarker">Output from the mediapipe hand landmarker model.</param>
        /// <param name="resultGestureRecognizer">Output from the mediapipe gesture recognizer model.</param>
        /// <param name="resultPoseLandmarker">Output from the mediapipe pose landmarker model.</param>
        public void SolveRightHand
            (HandLandmarkerResult resultHandLandmarker,
             GestureRecognizerResult resultGestureRecognizer,
             PoseLandmarkerResult resultPoseLandmarker)
        {
            // Index of values ​​for the right hand in the list of coordinates from hand landmarker model.
            int rightHandResultIndex = resultHandLandmarker.handedness.IndexOf(resultHandLandmarker.handedness.Find(x => x.categories[0].categoryName == "Right"));

            int rightHandInTheGesturesList = -1;
            if (resultGestureRecognizer.handedness != null)
            {
                // Index of values ​​for the right hand in the output from gesture recognizer model.
                rightHandInTheGesturesList = resultGestureRecognizer.handedness.IndexOf(resultGestureRecognizer.handedness.Find(x => x.categories[0].categoryName == "Right"));
            }
            String rightHandGesture = "None";
            if (rightHandInTheGesturesList != -1)
            {
                rightHandGesture = resultGestureRecognizer.gestures[rightHandInTheGesturesList].categories[0].categoryName;
            }

            // If the right hand was detected, get world coordinates of the keypoints.
            if (rightHandResultIndex >= 0)
            {
                List<Landmark> rightHandLandmarks = resultHandLandmarker.handWorldLandmarks[rightHandResultIndex].landmarks;

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

                // Rotating the wrist.
                // Interval where palm should be facing the camera.
                if (RightHandTransformState.HandToHeadCoordinateDifference.x > -handXCoordinatesDiffIntervalToFaceTheCamera.Item2
                    && RightHandTransformState.HandToHeadCoordinateDifference.x < -handXCoordinatesDiffIntervalToFaceTheCamera.Item1)
                {
                    List<Landmark> poseLandmarks = resultPoseLandmarker.poseWorldLandmarks[0].landmarks;
                    Landmark mediapipeRightHandPosition = poseLandmarks[16];
                    Landmark mediapipeRightElbowPosition = poseLandmarks[14];

                    float newElbowRotation = rotationSolver.FindElbowRotation(mediapipeRightHandPosition, mediapipeRightElbowPosition, rightHandStartPos);

                    float widthDiffHandToElbow = mediapipeRightHandPosition.x - mediapipeRightElbowPosition.x;

                    // If the user has bent his arm at the elbow, animate the avatar accordingly.
                    if (mediapipeRightElbowPosition.presence >= 0.4 && !float.IsNaN(newElbowRotation) && newElbowRotation >= 50 && widthDiffHandToElbow >= -0.08)
                    {
                        rightHandTargetRotation = startRightHandRotation * rightHandRotationOffset;
                        ik.solver.rightHandEffector.rotation *= Quaternion.Euler(0, 0, -newElbowRotation + rightHandTargetRotation.z - ik.solver.rightHandEffector.rotation.eulerAngles.z);

                        ik.solver.rightArmChain.bendConstraint.bendGoal.localPosition = new Vector3(0.5f, 0.5f + newElbowRotation / 100, 0);
                        RightHandTransformState.BendGoalLocalPosition = ik.solver.rightArmChain.bendConstraint.bendGoal.localPosition;

                        rightThumb1Bone.localRotation *= Quaternion.Euler(0, 0, 0);
                        rightThumb2Bone.localRotation *= Quaternion.Euler(20, 0, 0);
                        rightThumb3Bone.localRotation *= Quaternion.Euler(20, 0, 0);

                        rightMidFinger1Bone.localRotation = Quaternion.Euler(0, 0, -60);
                        rightMidFinger2Bone.localRotation = Quaternion.Euler(0, 0, -100);
                        rightMidFinger3Bone.localRotation = Quaternion.Euler(0, 0, -50);

                        rightRingFinger1Bone.localRotation = Quaternion.Euler(0, 0, -60);
                        rightRingFinger2Bone.localRotation = Quaternion.Euler(0, 0, -100);
                        rightRingFinger3Bone.localRotation = Quaternion.Euler(0, 0, -50);

                        rightIndexFinger1Bone.localRotation = Quaternion.Euler(0, 0, -20);
                        rightIndexFinger2Bone.localRotation = Quaternion.Euler(0, 0, 0);
                        rightIndexFinger3Bone.localRotation = Quaternion.Euler(0, 0, 0);

                        rightPinkyFinger1Bone.localRotation = Quaternion.Euler(0, 0, -60);
                        rightPinkyFinger2Bone.localRotation = Quaternion.Euler(0, 0, -100);
                        rightPinkyFinger3Bone.localRotation = Quaternion.Euler(0, 0, -50);
                    }
                    else
                    {
                        // This rotation is mainly aimed at the "hello" gesture, it represents the bending of the hand from left to right and vice versa.
                        float newWristAngle
                            = rotationSolver.FindThumbAndWristXRotation(handLandmarks.RightIndexFinger1Position, handLandmarks.RightHandPosition, RightHandTransformState.IndexFinger1StartPos);
                        ik.solver.rightHandEffector.rotation *= Quaternion.Euler(newWristAngle, 0, 0);

                        // If the thumbs up or thumbs down gesture was recognized, animate accordingly.
                        if (rightHandGesture == "Thumb_Up")
                        {
                            ik.solver.rightHandEffector.rotation = rightHandTargetRotation * Quaternion.Euler(-90, -80, -80);
                            RightHandTransformState.HandIKRotationWeight = ik.solver.rightHandEffector.rotationWeight;
                            RightHandTransformState.HandIKPositionWeight = ik.solver.rightHandEffector.positionWeight;
                            ik.solver.rightArmChain.bendConstraint.bendGoal.localPosition = new Vector3(1.5f, 1f, 0);
                            RightHandTransformState.BendGoalLocalPosition = ik.solver.rightArmChain.bendConstraint.bendGoal.localPosition;
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
                        }
                        else if (rightHandGesture == "Thumb_Down")
                        {
                            ik.solver.rightHandEffector.rotation = rightHandTargetRotation * Quaternion.Euler(80, -60, -60);
                            ik.solver.rightArmChain.bendConstraint.bendGoal.localPosition = new Vector3(1.5f, 1f, 0);
                            RightHandTransformState.HandIKRotationWeight = ik.solver.rightHandEffector.rotationWeight;
                            RightHandTransformState.HandIKPositionWeight = ik.solver.rightHandEffector.positionWeight;
                            RightHandTransformState.BendGoalLocalPosition = ik.solver.rightArmChain.bendConstraint.bendGoal.localPosition;
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
                        }
                        // If neither gesture was recognized, animate fingers.
                        else
                        {
                            ik.solver.rightArmChain.bendConstraint.bendGoal.localPosition = new Vector3(0.5f, 0.5f, 0);
                            RightHandTransformState.BendGoalLocalPosition = ik.solver.rightArmChain.bendConstraint.bendGoal.localPosition;

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
                            rightThumb1Bone.localRotation = Quaternion.Euler(50f, -20f, -15f);
                            rightThumb2Bone.localRotation = Quaternion.Euler(-20f, 0, 8f);
                            rightThumb3Bone.localRotation = Quaternion.Euler(0, 0, 5f);
                            rightThumb2Bone.localRotation *= Quaternion.Euler(newAngleThumb, 0, 0);
                            rightThumb1Bone.localRotation *= Quaternion.Euler(newAngleThumb / 4, 0, 0);

                            // Animate the palm rotation from facing forward to the back of the palm facing forward.
                            // It's important to check whether the user has flexed their fingers, as flexing the fingers will
                            // sometimes be interpreted as a palm rotation due to the lack of Z-coordinate.
                            if (handLandmarks.RightIndexFinger1Position.y - RightHandTransformState.IndexFinger1StartPos.y <= 0.005f)
                            {
                                newWristAngle = rotationSolver.FindWristYRotation(handLandmarks.RightIndexFinger1Position, handLandmarks.RightHandPosition, RightHandTransformState.IndexFinger1StartPos);
                                if (!float.IsNaN(newWristAngle) && newWristAngle <= 120f && !AreFingersBent(newAngleIndexFinger, newAngleMiddleFinger, newAngleRingFinger, newAnglePinky))
                                {
                                    ik.solver.rightHandEffector.rotation *= Quaternion.Euler(0, -newWristAngle, 0);
                                }
                            }
                        }
                    }
                }
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
        }

        /// <summary>
        /// Checks to see if at least one finger is bent (does not include the thumb).
        /// </summary>
        /// <param name="newAngleIndexFinger">The value to be assigned to the index finger for rotation.</param>
        /// <param name="newAngleMiddleFinger">The value to be assigned to the middle finger for rotation.</param>
        /// <param name="newAngleRingFinger">The value to be assigned to the ring finger for rotation.</param>
        /// <param name="newAnglePinky">The value to be assigned to the little finger for rotation.</param>
        /// <returns>True if at least one finger is bent.</returns>
        private bool AreFingersBent
            (float newAngleIndexFinger,
            float newAngleMiddleFinger,
            float newAngleRingFinger,
            float newAnglePinky)
        {
            return newAngleIndexFinger >= 50f
                || newAngleMiddleFinger >= 50f
                || newAngleRingFinger >= 50f
                || newAnglePinky >= 50f;
        }

        /// <summary>
        /// Stores values ​​for synchronizing the avatar's finger animations between clients.
        /// This function will be called if the user's hands are not in the camera view to store "standard" finger rotations
        /// (fully extended fingers, open palm).
        /// </summary>
        /// <remarks>
        /// This is necessary because when the user's hands are not in the camera, the SolveLeftHand() and SolveRightHand()
        /// functions are not called, causing the stored values ​​for the avatar's finger rotations to not be updated
        /// to properly synchronize animations between players.
        /// </remarks>
        public void StoreStandardFingerRotations()
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
            // Left hand
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

            // Right hand
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
        public bool RecalibrateHandsStartPositions(HandLandmarkerResult resultHandLandmarker)
        {
            // Index of values ​​for the right hand in the list of coordinates from hand landmarker model.
            int rightHandResultIndex
                = resultHandLandmarker.handedness.IndexOf(resultHandLandmarker.handedness.Find(x => x.categories[0].categoryName == "Right"));

            // Index of values ​​for the left hand in the list of coordinates from hand landmarker model.
            int leftHandResultIndex
                = resultHandLandmarker.handedness.IndexOf(resultHandLandmarker.handedness.Find(x => x.categories[0].categoryName == "Left"));

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
    }
}
