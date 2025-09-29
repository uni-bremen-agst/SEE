using System;
using UnityEngine;
using RootMotion.FinalIK;
using SEE.GO;
using SEE.Controls;
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
        /// Name of the head bone in the hierarchy (relative to the root of the avatar).
        /// </summary>
        public const string HeadName = "CC_Base_BoneRoot/CC_Base_Hip/CC_Base_Waist/CC_Base_Spine01/CC_Base_Spine02/CC_Base_NeckTwist01/CC_Base_NeckTwist02/CC_Base_Head";

        /// <summary>
        /// Name of the left hand bone in the hierarchy (relative to the root of the avatar).
        /// </summary>
        public const string LeftHandName = "CC_Base_BoneRoot/CC_Base_Hip/CC_Base_Waist/CC_Base_Spine01/CC_Base_Spine02/CC_Base_L_Clavicle/CC_Base_L_Upperarm/CC_Base_L_Forearm/CC_Base_L_Hand";

        /// <summary>
        /// Names of the bones of the left middle finger in the hierarchy (relative to the root of the avatar).
        /// </summary>
        public const string LeftMidFinger1Name = "CC_Base_BoneRoot/CC_Base_Hip/CC_Base_Waist/CC_Base_Spine01/CC_Base_Spine02/CC_Base_L_Clavicle/CC_Base_L_Upperarm/CC_Base_L_Forearm/CC_Base_L_Hand/CC_Base_L_Mid1";
        public const string LeftMidFinger2Name = "CC_Base_BoneRoot/CC_Base_Hip/CC_Base_Waist/CC_Base_Spine01/CC_Base_Spine02/CC_Base_L_Clavicle/CC_Base_L_Upperarm/CC_Base_L_Forearm/CC_Base_L_Hand/CC_Base_L_Mid1/CC_Base_L_Mid2";
        public const string LeftMidFinger3Name = "CC_Base_BoneRoot/CC_Base_Hip/CC_Base_Waist/CC_Base_Spine01/CC_Base_Spine02/CC_Base_L_Clavicle/CC_Base_L_Upperarm/CC_Base_L_Forearm/CC_Base_L_Hand/CC_Base_L_Mid1/CC_Base_L_Mid2/CC_Base_L_Mid3";

        /// <summary>
        /// Names of the bones of the left index finger in the hierarchy (relative to the root of the avatar).
        /// </summary>
        public const string LeftIndexFinger1Name = "CC_Base_BoneRoot/CC_Base_Hip/CC_Base_Waist/CC_Base_Spine01/CC_Base_Spine02/CC_Base_L_Clavicle/CC_Base_L_Upperarm/CC_Base_L_Forearm/CC_Base_L_Hand/CC_Base_L_Index1";
        public const string LeftIndexFinger2Name = "CC_Base_BoneRoot/CC_Base_Hip/CC_Base_Waist/CC_Base_Spine01/CC_Base_Spine02/CC_Base_L_Clavicle/CC_Base_L_Upperarm/CC_Base_L_Forearm/CC_Base_L_Hand/CC_Base_L_Index1/CC_Base_L_Index2";
        public const string LeftIndexFinger3Name = "CC_Base_BoneRoot/CC_Base_Hip/CC_Base_Waist/CC_Base_Spine01/CC_Base_Spine02/CC_Base_L_Clavicle/CC_Base_L_Upperarm/CC_Base_L_Forearm/CC_Base_L_Hand/CC_Base_L_Index1/CC_Base_L_Index2/CC_Base_L_Index3";

        /// <summary>
        /// Names of the bones of the left ring finger in the hierarchy (relative to the root of the avatar).
        /// </summary>
        public const string LeftRingFinger1Name = "CC_Base_BoneRoot/CC_Base_Hip/CC_Base_Waist/CC_Base_Spine01/CC_Base_Spine02/CC_Base_L_Clavicle/CC_Base_L_Upperarm/CC_Base_L_Forearm/CC_Base_L_Hand/CC_Base_L_Ring1";
        public const string LeftRingFinger2Name = "CC_Base_BoneRoot/CC_Base_Hip/CC_Base_Waist/CC_Base_Spine01/CC_Base_Spine02/CC_Base_L_Clavicle/CC_Base_L_Upperarm/CC_Base_L_Forearm/CC_Base_L_Hand/CC_Base_L_Ring1/CC_Base_L_Ring2";
        public const string LeftRingFinger3Name = "CC_Base_BoneRoot/CC_Base_Hip/CC_Base_Waist/CC_Base_Spine01/CC_Base_Spine02/CC_Base_L_Clavicle/CC_Base_L_Upperarm/CC_Base_L_Forearm/CC_Base_L_Hand/CC_Base_L_Ring1/CC_Base_L_Ring2/CC_Base_L_Ring3";

        /// <summary>
        /// Names of the bones of the left little finger in the hierarchy (relative to the root of the avatar).
        /// </summary>
        public const string LeftPinkyFinger1Name = "CC_Base_BoneRoot/CC_Base_Hip/CC_Base_Waist/CC_Base_Spine01/CC_Base_Spine02/CC_Base_L_Clavicle/CC_Base_L_Upperarm/CC_Base_L_Forearm/CC_Base_L_Hand/CC_Base_L_Pinky1";
        public const string LeftPinkyFinger2Name = "CC_Base_BoneRoot/CC_Base_Hip/CC_Base_Waist/CC_Base_Spine01/CC_Base_Spine02/CC_Base_L_Clavicle/CC_Base_L_Upperarm/CC_Base_L_Forearm/CC_Base_L_Hand/CC_Base_L_Pinky1/CC_Base_L_Pinky2";
        public const string LeftPinkyFinger3Name = "CC_Base_BoneRoot/CC_Base_Hip/CC_Base_Waist/CC_Base_Spine01/CC_Base_Spine02/CC_Base_L_Clavicle/CC_Base_L_Upperarm/CC_Base_L_Forearm/CC_Base_L_Hand/CC_Base_L_Pinky1/CC_Base_L_Pinky2/CC_Base_L_Pinky3";

        /// <summary>
        /// Names of the bones of the left thumb in the hierarchy (relative to the root of the avatar).
        /// </summary>
        public const string LeftThumb1Name = "CC_Base_BoneRoot/CC_Base_Hip/CC_Base_Waist/CC_Base_Spine01/CC_Base_Spine02/CC_Base_L_Clavicle/CC_Base_L_Upperarm/CC_Base_L_Forearm/CC_Base_L_Hand/CC_Base_L_Thumb1";
        public const string LeftThumb2Name = "CC_Base_BoneRoot/CC_Base_Hip/CC_Base_Waist/CC_Base_Spine01/CC_Base_Spine02/CC_Base_L_Clavicle/CC_Base_L_Upperarm/CC_Base_L_Forearm/CC_Base_L_Hand/CC_Base_L_Thumb1/CC_Base_L_Thumb2";
        public const string LeftThumb3Name = "CC_Base_BoneRoot/CC_Base_Hip/CC_Base_Waist/CC_Base_Spine01/CC_Base_Spine02/CC_Base_L_Clavicle/CC_Base_L_Upperarm/CC_Base_L_Forearm/CC_Base_L_Hand/CC_Base_L_Thumb1/CC_Base_L_Thumb2/CC_Base_L_Thumb3";

        /// <summary>
        /// Name of the right hand bone in the hierarchy (relative to the root of the avatar).
        /// </summary>
        public const string RightHandName = "CC_Base_BoneRoot/CC_Base_Hip/CC_Base_Waist/CC_Base_Spine01/CC_Base_Spine02/CC_Base_R_Clavicle/CC_Base_R_Upperarm/CC_Base_R_Forearm/CC_Base_R_Hand";

        /// <summary>
        /// Names of the bones of the right middle finger in the hierarchy (relative to the root of the avatar).
        /// </summary>
        public const string RightMidFinger1Name = "CC_Base_BoneRoot/CC_Base_Hip/CC_Base_Waist/CC_Base_Spine01/CC_Base_Spine02/CC_Base_R_Clavicle/CC_Base_R_Upperarm/CC_Base_R_Forearm/CC_Base_R_Hand/CC_Base_R_Mid1";
        public const string RightMidFinger2Name = "CC_Base_BoneRoot/CC_Base_Hip/CC_Base_Waist/CC_Base_Spine01/CC_Base_Spine02/CC_Base_R_Clavicle/CC_Base_R_Upperarm/CC_Base_R_Forearm/CC_Base_R_Hand/CC_Base_R_Mid1/CC_Base_R_Mid2";
        public const string RightMidFinger3Name = "CC_Base_BoneRoot/CC_Base_Hip/CC_Base_Waist/CC_Base_Spine01/CC_Base_Spine02/CC_Base_R_Clavicle/CC_Base_R_Upperarm/CC_Base_R_Forearm/CC_Base_R_Hand/CC_Base_R_Mid1/CC_Base_R_Mid2/CC_Base_R_Mid3";

        /// <summary>
        /// Names of the bones of the right index finger in the hierarchy (relative to the root of the avatar).
        /// </summary>
        public const string RightIndexFinger1Name = "CC_Base_BoneRoot/CC_Base_Hip/CC_Base_Waist/CC_Base_Spine01/CC_Base_Spine02/CC_Base_R_Clavicle/CC_Base_R_Upperarm/CC_Base_R_Forearm/CC_Base_R_Hand/CC_Base_R_Index1";
        public const string RightIndexFinger2Name = "CC_Base_BoneRoot/CC_Base_Hip/CC_Base_Waist/CC_Base_Spine01/CC_Base_Spine02/CC_Base_R_Clavicle/CC_Base_R_Upperarm/CC_Base_R_Forearm/CC_Base_R_Hand/CC_Base_R_Index1/CC_Base_R_Index2";
        public const string RightIndexFinger3Name = "CC_Base_BoneRoot/CC_Base_Hip/CC_Base_Waist/CC_Base_Spine01/CC_Base_Spine02/CC_Base_R_Clavicle/CC_Base_R_Upperarm/CC_Base_R_Forearm/CC_Base_R_Hand/CC_Base_R_Index1/CC_Base_R_Index2/CC_Base_R_Index3";

        /// <summary>
        /// Names of the bones of the right ring finger in the hierarchy (relative to the root of the avatar).
        /// </summary>
        public const string RightRingFinger1Name = "CC_Base_BoneRoot/CC_Base_Hip/CC_Base_Waist/CC_Base_Spine01/CC_Base_Spine02/CC_Base_R_Clavicle/CC_Base_R_Upperarm/CC_Base_R_Forearm/CC_Base_R_Hand/CC_Base_R_Ring1";
        public const string RightRingFinger2Name = "CC_Base_BoneRoot/CC_Base_Hip/CC_Base_Waist/CC_Base_Spine01/CC_Base_Spine02/CC_Base_R_Clavicle/CC_Base_R_Upperarm/CC_Base_R_Forearm/CC_Base_R_Hand/CC_Base_R_Ring1/CC_Base_R_Ring2";
        public const string RightRingFinger3Name = "CC_Base_BoneRoot/CC_Base_Hip/CC_Base_Waist/CC_Base_Spine01/CC_Base_Spine02/CC_Base_R_Clavicle/CC_Base_R_Upperarm/CC_Base_R_Forearm/CC_Base_R_Hand/CC_Base_R_Ring1/CC_Base_R_Ring2/CC_Base_R_Ring3";

        /// <summary>
        /// Names of the bones of the right little finger in the hierarchy (relative to the root of the avatar).
        /// </summary>
        public const string RightPinkyFinger1Name = "CC_Base_BoneRoot/CC_Base_Hip/CC_Base_Waist/CC_Base_Spine01/CC_Base_Spine02/CC_Base_R_Clavicle/CC_Base_R_Upperarm/CC_Base_R_Forearm/CC_Base_R_Hand/CC_Base_R_Pinky1";
        public const string RightPinkyFinger2Name = "CC_Base_BoneRoot/CC_Base_Hip/CC_Base_Waist/CC_Base_Spine01/CC_Base_Spine02/CC_Base_R_Clavicle/CC_Base_R_Upperarm/CC_Base_R_Forearm/CC_Base_R_Hand/CC_Base_R_Pinky1/CC_Base_R_Pinky2";
        public const string RightPinkyFinger3Name = "CC_Base_BoneRoot/CC_Base_Hip/CC_Base_Waist/CC_Base_Spine01/CC_Base_Spine02/CC_Base_R_Clavicle/CC_Base_R_Upperarm/CC_Base_R_Forearm/CC_Base_R_Hand/CC_Base_R_Pinky1/CC_Base_R_Pinky2/CC_Base_R_Pinky3";

        /// <summary>
        /// Names of the bones of the right thumb in the hierarchy (relative to the root of the avatar).
        /// </summary>
        public const string RightThumb1Name = "CC_Base_BoneRoot/CC_Base_Hip/CC_Base_Waist/CC_Base_Spine01/CC_Base_Spine02/CC_Base_R_Clavicle/CC_Base_R_Upperarm/CC_Base_R_Forearm/CC_Base_R_Hand/CC_Base_R_Thumb1";
        public const string RightThumb2Name = "CC_Base_BoneRoot/CC_Base_Hip/CC_Base_Waist/CC_Base_Spine01/CC_Base_Spine02/CC_Base_R_Clavicle/CC_Base_R_Upperarm/CC_Base_R_Forearm/CC_Base_R_Hand/CC_Base_R_Thumb1/CC_Base_R_Thumb2";
        public const string RightThumb3Name = "CC_Base_BoneRoot/CC_Base_Hip/CC_Base_Waist/CC_Base_Spine01/CC_Base_Spine02/CC_Base_R_Clavicle/CC_Base_R_Upperarm/CC_Base_R_Forearm/CC_Base_R_Hand/CC_Base_R_Thumb1/CC_Base_R_Thumb2/CC_Base_R_Thumb3";

        /// <summary>
        /// The FullBodyBiped IK solver attached to the avatar.
        /// </summary>
        private FullBodyBipedIK ik;

        /// <summary>
        /// HandTransformState instances for left and right hand which are used to store 
        /// the status of the current rotations and positions of the hands and fingers 
        /// as well as other information required for animations.
        /// </summary>
        public HandTransformState LeftHandTransformState = new HandTransformState();
        public HandTransformState RightHandTransformState = new HandTransformState();

        /// <summary>
        /// Solver for calculating rotations.
        /// </summary>
        private HandRotationsSolver rotationSolver = new HandRotationsSolver();

        /// <summary>
        /// If true, the avatar's laser pointer is enabled.
        /// </summary>
        private bool IsPointing = true;

        /// <summary>
        /// The weight that determines the level of influence of changes in the IK effectors of the hands on other bones in the chain.
        /// </summary>
        private float weight = 1f;

        /// <summary>
        /// Фnimation speed of hand position changes and rotations.
        /// </summary>
        private const float moveSpeed = 0.5f;

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
        private bool startHandsPositionReached = false;

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
        /// <remarks>By start position is meant that the avatar's hands are in front of the avatar, bent at the elbows and the palms are facing forward.</remarks>
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
        /// <remarks>By start position is meant that the avatar's hands are in front of the avatar, bent at the elbows and the palms are facing forward.</remarks>
        private Quaternion leftHandRotationOffset = Quaternion.Euler(170, 110, 0);
        private Vector3 leftHandPositionOffset = new Vector3(-0.37f, 1.56f, 0.23f);
        private Quaternion rightHandRotationOffset = Quaternion.Euler(-40, 15, 60);
        private Vector3 rightHandPositionOffset = new Vector3(0.37f, 1.56f, 0.23f);

        /// <summary>
        /// The interval at which the avatar's palm should face the camera (the values ​​are the difference in coordinates between the hand and the head).
        /// </summary>
        private Tuple<float, float> HandXCoordinatesDiffIntervalToFaceTheCamera = Tuple.Create(-0.47f, -0.15f);

        /// <summary>
        /// The interval at which the avatar's palm should be slightly rotated to avoid unnatural animations 
        /// when moving the hand in front of the avatar's body (the values ​​are the difference in coordinates between the hand and the head).
        /// </summary>
        private Tuple<float, float> HandXCoordinatesDiffIntervalMovingInFront = Tuple.Create(-0.15f, 0.28f);

        /// <summary>
        /// The difference in the y-coordinate between the hand and the head, 
        /// from which it can be assumed that the hand is moving downwards and therefore 
        /// should be slightly rotated to avoid unnatural animations.
        /// </summary>
        private const float HandYCoordinatesDiffToMoveDownFrom = -0.3f;

        /// <summary>
        /// Initializes the initial positions of the hands and the head, the main avatar transform, the ik component, and also adds Bend Goals for the elbows.
        /// </summary>
        /// <param name="mainTrasform">The main transform of the avatar.</param>
        /// <param name="ikComponent">The FullBodyBiped IK solver attached to the avatar.</param>
        public void Initialize(Transform mainTrasform, FullBodyBipedIK ikComponent)
        {
            this.ik = ikComponent;
            this.transform = mainTrasform;

            Transform headBone = mainTrasform.Find(HeadName);
            Transform leftHandBone = mainTrasform.Find(LeftHandName);
            Transform rightHandBone = mainTrasform.Find(RightHandName);
            if (headBone == null)
            {
                UnityEngine.Debug.LogError($"Head bone not found: {HeadName}" + "\n");
                return;
            }
            else if (leftHandBone == null)
            {
                UnityEngine.Debug.LogError($"Left hand bone not found: {LeftHandName}" + "\n");
                return;
            }
            else if (rightHandBone == null)
            {
                UnityEngine.Debug.LogError($"Right hand bone not found: {RightHandName}" + "\n");
                return;
            }

            //Save information about the current position and rotation of the hand.
            LeftHandTransformState.HandPosition = leftHandBone.position;
            LeftHandTransformState.HandRotation = leftHandBone.rotation;
            startLeftHandRotation = leftHandBone.localRotation;

            //Save information about the current position and rotation of the hand.
            RightHandTransformState.HandPosition = rightHandBone.position;
            RightHandTransformState.HandRotation = rightHandBone.rotation;
            startRightHandRotation = rightHandBone.localRotation;

            headPosition = mainTrasform.InverseTransformPoint(headBone.position);

            ik.solver.leftHandEffector.positionWeight = weight;
            ik.solver.leftHandEffector.rotationWeight = weight;
            LeftHandTransformState.HandIKRotationWeight = ik.solver.leftHandEffector.rotationWeight; 

            ik.solver.rightHandEffector.positionWeight = weight;
            ik.solver.rightHandEffector.rotationWeight = weight;
            RightHandTransformState.HandIKRotationWeight = ik.solver.rightHandEffector.rotationWeight;

            //Add bend goals for the elbows so they bend downwards.
            GameObject leftElbowBendGoal = new GameObject("LeftElbowBendGoal");
            leftElbowBendGoal.transform.SetParent(mainTrasform, false);
            ik.solver.leftArmChain.bendConstraint.bendGoal = leftElbowBendGoal.transform;
            ik.solver.leftArmChain.bendConstraint.bendGoal.localPosition = new Vector3(-0.5f, 0.5f, 0);
            LeftHandTransformState.BendGoalLocalPosition = ik.solver.leftArmChain.bendConstraint.bendGoal.localPosition;

            GameObject rightElbowBendGoal = new GameObject("RightElbowBendGoal");
            rightElbowBendGoal.transform.SetParent(mainTrasform, false);
            ik.solver.rightArmChain.bendConstraint.bendGoal = rightElbowBendGoal.transform;
            ik.solver.rightArmChain.bendConstraint.bendGoal.localPosition = new Vector3(0.5f, 0.5f, 0);
            RightHandTransformState.BendGoalLocalPosition = ik.solver.rightArmChain.bendConstraint.bendGoal.localPosition;
        }

        /// <summary>
        /// Smoothly brings the avatar's hands to the start position.
        /// </summary>
        /// <returns>True, if the start position was reached.</returns>
        /// <remarks>By start position is meant that the avatar's hands are in front of the avatar, bent at the elbows and the palms are facing forward.</remarks>
        public bool BringHandsToStartPositions()
        {
            Transform headBone = transform.Find(HeadName);
            headPosition = transform.InverseTransformPoint(headBone.position);

            Transform leftHand = transform.Find(LeftHandName);
            leftHand.localRotation = startLeftHandRotation * leftHandRotationOffset;
            leftHandTargetRotation = leftHand.rotation;
            leftHand.localRotation = startLeftHandRotation;

            leftHandTargetPos = transform.TransformPoint(leftHandPositionOffset);

            Transform rightHand = transform.Find(RightHandName);
            rightHand.localRotation = startRightHandRotation * rightHandRotationOffset;
            rightHandTargetRotation = rightHand.rotation;
            rightHand.localRotation = startRightHandRotation;

            rightHandTargetPos = transform.TransformPoint(rightHandPositionOffset);

            //If the start position has not yet been reached.
            if (!startHandsPositionReached && (Vector3.Distance(LeftHandTransformState.HandPosition, leftHandTargetPos) >= arrivalThreshold || Vector3.Distance(RightHandTransformState.HandPosition, rightHandTargetPos) >= arrivalThreshold))
            {
                //Turn and move the hands slightly to get closer to the starting position.
                LeftHandTransformState.HandPosition = Vector3.Lerp(LeftHandTransformState.HandPosition, leftHandTargetPos, Time.deltaTime * moveSpeed * 4);
                LeftHandTransformState.HandRotation = Quaternion.Slerp(LeftHandTransformState.HandRotation, leftHandTargetRotation, Time.deltaTime * moveSpeed * 4);
                ik.solver.leftHandEffector.rotation = LeftHandTransformState.HandRotation;
                ik.solver.leftHandEffector.position = LeftHandTransformState.HandPosition;

                ik.solver.leftHandEffector.positionWeight = weight;
                ik.solver.leftHandEffector.rotationWeight = weight;
                LeftHandTransformState.HandIKRotationWeight = ik.solver.leftHandEffector.rotationWeight;

                RightHandTransformState.HandPosition = Vector3.Lerp(RightHandTransformState.HandPosition, rightHandTargetPos, Time.deltaTime * moveSpeed * 4);
                RightHandTransformState.HandRotation = Quaternion.Slerp(RightHandTransformState.HandRotation, rightHandTargetRotation, Time.deltaTime * moveSpeed * 4);
                ik.solver.rightHandEffector.rotation = RightHandTransformState.HandRotation;
                ik.solver.rightHandEffector.position = RightHandTransformState.HandPosition;

                ik.solver.rightHandEffector.positionWeight = weight;
                ik.solver.rightHandEffector.rotationWeight = weight;
                RightHandTransformState.HandIKRotationWeight = ik.solver.rightHandEffector.rotationWeight;

                LeftHandTransformState.HandIKEffectorPosition = ik.solver.leftHandEffector.position;
                LeftHandTransformState.HandIKEffectorRotation = ik.solver.leftHandEffector.rotation;

                RightHandTransformState.HandIKEffectorPosition = ik.solver.rightHandEffector.position;
                RightHandTransformState.HandIKEffectorRotation = ik.solver.rightHandEffector.rotation;

                //Save the thumbs rotations values ​​to control their animation in the future.
                Transform leftThumb1Bone = transform.Find(LeftThumb1Name);
                Transform leftThumb2Bone = transform.Find(LeftThumb2Name);
                Transform leftThumb3Bone = transform.Find(LeftThumb3Name);
                LeftHandTransformState.Thumb1Rotations = leftThumb1Bone.localRotation;
                LeftHandTransformState.Thumb2Rotations = leftThumb2Bone.localRotation;
                LeftHandTransformState.Thumb3Rotations = leftThumb3Bone.localRotation;

                Transform rightThumb1Bone = transform.Find(RightThumb1Name);
                Transform rightThumb2Bone = transform.Find(RightThumb2Name);
                Transform rightThumb3Bone = transform.Find(RightThumb3Name);
                RightHandTransformState.Thumb1Rotations = rightThumb1Bone.localRotation;
                RightHandTransformState.Thumb2Rotations = rightThumb2Bone.localRotation;
                RightHandTransformState.Thumb3Rotations = rightThumb3Bone.localRotation;

                return false;
            }
            else {
                startHandsPositionReached = true;
                return true;
            }
        }

        /// <summary>
        /// Changes the avatar's hand positions using the output from the mediapipe pose landmarker model.
        /// </summary>
        /// <param name="resultPoseLandmarker">Output from the mediapipe pose landmarker model.</param>
        public void SolveHandsPositions(PoseLandmarkerResult resultPoseLandmarker)
        {
            Transform leftHand = transform.Find(LeftHandName);
            Transform rightHand = transform.Find(RightHandName);

            // Whether the laser pointer was toggled
            if (SEEInput.TogglePointing())
            {
                if (IsPointing == true)
                {
                    IsPointing = false;
                }
                else 
                {
                    IsPointing = true;
                }
            }

            // Depending on whether the avatar's laser pointer is turned on or not, the animation needs to be adjusted slightly.
            if (IsPointing == false)
            {
                leftHand.localRotation = startLeftHandRotation * leftHandRotationOffset * Quaternion.Euler(0, 15f, 0);
                leftHandTargetRotation = leftHand.rotation;
                leftHand.localRotation = startLeftHandRotation;

                rightHand.localRotation = startRightHandRotation * rightHandRotationOffset * Quaternion.Euler(70f, 0, 130f);
                rightHandTargetRotation = rightHand.rotation;
                rightHand.localRotation = startRightHandRotation;
            }
            else
            {
                leftHand.localRotation = startLeftHandRotation * leftHandRotationOffset;
                leftHandTargetRotation = leftHand.rotation;
                leftHand.localRotation = startLeftHandRotation;

                rightHand.localRotation = startRightHandRotation * rightHandRotationOffset;
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

            //Save the last detected coordinates.
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

            //Set values ​​for hand rotations when moving in front of the avatar's body, when moving away from the avatar, and when moving downwards.
            LeftHandTransformState.HandRotationForMovementInFrontOfTheAvatar = leftHandTargetRotation * Quaternion.Euler(0, 55, 0);
            RightHandTransformState.HandRotationForMovementInFrontOfTheAvatar = rightHandTargetRotation * Quaternion.Euler(0, -55, 0);

            LeftHandTransformState.HandRotationForMovementToTheSide = leftHandTargetRotation * Quaternion.Euler(0, -50, 0);
            RightHandTransformState.HandRotationForMovementToTheSide = rightHandTargetRotation * Quaternion.Euler(0, 50, 0);

            LeftHandTransformState.HandRotationForMovementDown = leftHandTargetRotation * Quaternion.Euler(0, 0, 60);
            RightHandTransformState.HandRotationForMovementDown = rightHandTargetRotation * Quaternion.Euler(0, 0, -60);

            Landmark mediapipeHeadPosition = poseLandmarks[0];

            //Save the last detected coordinates snd initialize new.
            LeftHandTransformState.PreviousMediapipeCoordinates = LeftHandTransformState.NewMediapipeCoordinates;
            LeftHandTransformState.NewMediapipeCoordinates.x = mediapipeLeftHandPosition.x;
            LeftHandTransformState.NewMediapipeCoordinates.y = mediapipeLeftHandPosition.y;
            LeftHandTransformState.NewMediapipeCoordinates.z = mediapipeLeftHandPosition.z;

            RightHandTransformState.PreviousMediapipeCoordinates = RightHandTransformState.NewMediapipeCoordinates;
            RightHandTransformState.NewMediapipeCoordinates.x = mediapipeRightHandPosition.x;
            RightHandTransformState.NewMediapipeCoordinates.y = mediapipeRightHandPosition.y;
            RightHandTransformState.NewMediapipeCoordinates.z = mediapipeRightHandPosition.z;

            //If the probability with which the left hand is in the picture is acceptable for animation.
            if (mediapipeLeftHandPosition.presence > acceptableHandPresenceProbability)
            {
                LeftHandTransformState.HandToHeadCoordinateDifference = new Vector3(mediapipeLeftHandPosition.x - mediapipeHeadPosition.x, mediapipeLeftHandPosition.y - mediapipeHeadPosition.y, transform.InverseTransformPoint(leftHandTargetPos).z - headPosition.z);
                Vector3 newHandPosition = headPosition + LeftHandTransformState.HandToHeadCoordinateDifference;
                ik.solver.leftHandEffector.position = transform.TransformPoint(newHandPosition);

                // Interval where palm should be facing the camera.
                if (LeftHandTransformState.HandToHeadCoordinateDifference.x < HandXCoordinatesDiffIntervalToFaceTheCamera.Item2 && LeftHandTransformState.HandToHeadCoordinateDifference.x > HandXCoordinatesDiffIntervalToFaceTheCamera.Item1)
                {
                    if (IsPointing == true)
                    {
                        leftHand.localRotation = startLeftHandRotation * leftHandRotationOffset;
                    }
                    else 
                    {
                        leftHand.localRotation = startLeftHandRotation * leftHandRotationOffset * Quaternion.Euler(0, 15f, 0);
                    }
                    leftHandTargetRotation = leftHand.rotation;
                    leftHand.localRotation = startLeftHandRotation;
                    LeftHandTransformState.HandRotation = Quaternion.Slerp(LeftHandTransformState.HandRotation, leftHandTargetRotation, Time.deltaTime * moveSpeed * 10);
                    ik.solver.leftHandEffector.rotation = LeftHandTransformState.HandRotation;
                }
                // If the hand is moving in front of the character.
                else if (LeftHandTransformState.HandToHeadCoordinateDifference.x >= HandXCoordinatesDiffIntervalMovingInFront.Item1
                    && LeftHandTransformState.HandToHeadCoordinateDifference.x <= HandXCoordinatesDiffIntervalMovingInFront.Item2)
                {
                    leftHandTargetRotation = LeftHandTransformState.HandRotationForMovementInFrontOfTheAvatar;
                    if (ik.solver.leftHandEffector.rotation.eulerAngles.y < LeftHandTransformState.HandRotationForMovementInFrontOfTheAvatar.eulerAngles.y)
                    {
                        LeftHandTransformState.HandRotation = Quaternion.Slerp(LeftHandTransformState.HandRotation, leftHandTargetRotation, Time.deltaTime * moveSpeed * 10);
                        ik.solver.leftHandEffector.rotation = LeftHandTransformState.HandRotation;
                        ik.solver.leftHandEffector.rotationWeight = weight;
                        LeftHandTransformState.HandIKRotationWeight = ik.solver.leftHandEffector.rotationWeight;
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
                        LeftHandTransformState.HandRotation = Quaternion.Slerp(LeftHandTransformState.HandRotation, leftHandTargetRotation, Time.deltaTime * moveSpeed * 5);
                        ik.solver.leftHandEffector.rotation = LeftHandTransformState.HandRotation;
                        ik.solver.leftHandEffector.rotationWeight = weight;
                        LeftHandTransformState.HandIKRotationWeight = ik.solver.leftHandEffector.rotationWeight;
                    }
                }
                // If the hand is moving downwards.
                if (LeftHandTransformState.HandToHeadCoordinateDifference.y <= HandYCoordinatesDiffToMoveDownFrom)
                {
                    leftHandTargetRotation = LeftHandTransformState.HandRotationForMovementDown;
                    if (ik.solver.leftHandEffector.rotation.z > LeftHandTransformState.HandRotationForMovementDown.z)
                    {
                        LeftHandTransformState.HandRotation = Quaternion.Slerp(LeftHandTransformState.HandRotation, leftHandTargetRotation, Time.deltaTime * moveSpeed * 10);
                        ik.solver.leftHandEffector.rotation = LeftHandTransformState.HandRotation;
                        ik.solver.leftHandEffector.rotationWeight = weight;
                        LeftHandTransformState.HandIKRotationWeight = ik.solver.leftHandEffector.rotationWeight;
                    }
                }
            }
            // If the probability with which the left hand is in the picture is NOT acceptable for animation,
            // assign the value of last tracked coordinates.
            else
            {
                Vector3 newHandPosition = headPosition + LeftHandTransformState.HandToHeadCoordinateDifference;
                if (LeftHandTransformState.HandToHeadCoordinateDifference != Vector3.zero)
                {
                    ik.solver.leftHandEffector.position = transform.TransformPoint(newHandPosition);
                }
            }

            // If the probability with which the right hand is in the picture is acceptable for animation.
            if (mediapipeRightHandPosition.presence > acceptableHandPresenceProbability)
            {
                RightHandTransformState.HandToHeadCoordinateDifference = new Vector3(mediapipeRightHandPosition.x - mediapipeHeadPosition.x, mediapipeRightHandPosition.y - mediapipeHeadPosition.y, transform.InverseTransformPoint(rightHandTargetPos).z - headPosition.z);
                Vector3 newHandPosition = headPosition + RightHandTransformState.HandToHeadCoordinateDifference;
                ik.solver.rightHandEffector.position = transform.TransformPoint(newHandPosition);

                // Interval where palm should be facing the camera.
                if (RightHandTransformState.HandToHeadCoordinateDifference.x > -HandXCoordinatesDiffIntervalToFaceTheCamera.Item2 && RightHandTransformState.HandToHeadCoordinateDifference.x < -HandXCoordinatesDiffIntervalToFaceTheCamera.Item1)
                {
                    if (IsPointing == true)
                    {
                        rightHand.localRotation = startRightHandRotation * rightHandRotationOffset;
                    }
                    else
                    {
                        rightHand.localRotation = startRightHandRotation * rightHandRotationOffset * Quaternion.Euler(70f, 0, 130f);
                    }
                    rightHandTargetRotation = rightHand.rotation;
                    rightHand.localRotation = startRightHandRotation;
                    RightHandTransformState.HandRotation = Quaternion.Slerp(RightHandTransformState.HandRotation, rightHandTargetRotation, Time.deltaTime * moveSpeed * 10);
                    ik.solver.rightHandEffector.rotation = RightHandTransformState.HandRotation;
                }
                // If the hand is moving in front of the character.
                else if (RightHandTransformState.HandToHeadCoordinateDifference.x <= -HandXCoordinatesDiffIntervalMovingInFront.Item1)
                {
                    rightHandTargetRotation = RightHandTransformState.HandRotationForMovementInFrontOfTheAvatar;
                    if (ik.solver.rightHandEffector.rotation.eulerAngles.y > RightHandTransformState.HandRotationForMovementInFrontOfTheAvatar.eulerAngles.y)
                    {
                        RightHandTransformState.HandRotation = Quaternion.Slerp(RightHandTransformState.HandRotation, rightHandTargetRotation, Time.deltaTime * moveSpeed * 10);
                        ik.solver.rightHandEffector.rotation = RightHandTransformState.HandRotation;
                        ik.solver.rightHandEffector.rotationWeight = weight;
                        RightHandTransformState.HandIKRotationWeight = ik.solver.rightHandEffector.rotationWeight;
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
                        RightHandTransformState.HandRotation = Quaternion.Slerp(RightHandTransformState.HandRotation, rightHandTargetRotation, Time.deltaTime * moveSpeed * 5);
                        ik.solver.rightHandEffector.rotation = RightHandTransformState.HandRotation;
                        ik.solver.rightHandEffector.rotationWeight = weight;
                        RightHandTransformState.HandIKRotationWeight = ik.solver.rightHandEffector.rotationWeight;
                    }
                }
                // If the hand is moving downwards.
                if (RightHandTransformState.HandToHeadCoordinateDifference.y <= HandYCoordinatesDiffToMoveDownFrom)
                {
                    rightHandTargetRotation = RightHandTransformState.HandRotationForMovementDown;
                    if (ik.solver.rightHandEffector.rotation.z > RightHandTransformState.HandRotationForMovementDown.z)
                    {
                        RightHandTransformState.HandRotation = Quaternion.Slerp(RightHandTransformState.HandRotation, rightHandTargetRotation, Time.deltaTime * moveSpeed * 10);
                        ik.solver.rightHandEffector.rotation = RightHandTransformState.HandRotation;
                        ik.solver.rightHandEffector.rotationWeight = weight;
                        RightHandTransformState.HandIKRotationWeight = ik.solver.rightHandEffector.rotationWeight;
                    }
                }
            }
            // If the probability with which the right hand is in the picture is NOT acceptable for animation,
            // assign the value of last tracked coordinates.
            else
            {
                Vector3 newHandPosition = headPosition + RightHandTransformState.HandToHeadCoordinateDifference;
                if (RightHandTransformState.HandToHeadCoordinateDifference != Vector3.zero)
                {
                    ik.solver.rightHandEffector.position = transform.TransformPoint(newHandPosition);
                }
            }

            //Save information about current hand positions and rotations.
            LeftHandTransformState.HandIKEffectorPosition = ik.solver.leftHandEffector.position;
            LeftHandTransformState.HandIKEffectorRotation = ik.solver.leftHandEffector.rotation;

            RightHandTransformState.HandIKEffectorPosition = ik.solver.rightHandEffector.position;
            RightHandTransformState.HandIKEffectorRotation = ik.solver.rightHandEffector.rotation;

        }

        /// <summary>
        /// Rotates the wrist and fingers of the left hand using the outputs from MediaPipe Models.
        /// </summary>
        /// <param name="resultHandLandmarker">Output from the mediapipe hand landmarker model.</param>
        /// <param name="resultGestureRecognizer">Output from the mediapipe gesture recognizer model.</param>
        /// <param name="resultPoseLandmarker">Output from the mediapipe pose landmarker model.</param>
        public void SolveLeftHand(HandLandmarkerResult resultHandLandmarker, GestureRecognizerResult resultGestureRecognizer, PoseLandmarkerResult resultPoseLandmarker)
        {
            //Index of values ​​for the left hand in the list of coordinates from hand landmarker model.
            int leftHandResultIndex = resultHandLandmarker.handedness.IndexOf(resultHandLandmarker.handedness.Find(x => x.categories[0].categoryName == "Left"));

            int leftHandInTheGesturesList = -1;
            if (resultGestureRecognizer.handedness != null)
            {
                //Index of values ​​for the left hand in the output from gesture recognizer model.
                leftHandInTheGesturesList = resultGestureRecognizer.handedness.IndexOf(resultGestureRecognizer.handedness.Find(x => x.categories[0].categoryName == "Left"));
            }
            String leftHandGesture = "None";
            if (leftHandInTheGesturesList != -1)
            {
                leftHandGesture = resultGestureRecognizer.gestures[leftHandInTheGesturesList].categories[0].categoryName;
            }

            //If the left hand was detected, get world coordinates of the keypoints.
            if (leftHandResultIndex >= 0)
            {
                List<Landmark> leftHandLandmarks = resultHandLandmarker.handWorldLandmarks[leftHandResultIndex].landmarks;

                Landmark leftMiddleFinger3Position = leftHandLandmarks[11];
                Landmark leftMiddleFinger2Position = leftHandLandmarks[10];
                Landmark leftMiddleFinger1Position = leftHandLandmarks[9];

                Landmark leftIndexFinger3Position = leftHandLandmarks[7];
                Landmark leftIndexFinger2Position = leftHandLandmarks[6];
                Landmark leftIndexFinger1Position = leftHandLandmarks[5];

                Landmark leftRingFinger3Position = leftHandLandmarks[15];
                Landmark leftRingFinger2Position = leftHandLandmarks[14];
                Landmark leftRingFinger1Position = leftHandLandmarks[13];

                Landmark leftPinkyFinger3Position = leftHandLandmarks[19];
                Landmark leftPinkyFinger2Position = leftHandLandmarks[18];
                Landmark leftPinkyFinger1Position = leftHandLandmarks[17];

                Landmark leftThumb3Position = leftHandLandmarks[3];
                Landmark leftThumb2Position = leftHandLandmarks[2];
                Landmark leftThumb1Position = leftHandLandmarks[1];

                Landmark leftHandPosition = leftHandLandmarks[0];

                //Get transform components of avatar fingers.
                Transform leftMidFinger3Bone = transform.Find(LeftMidFinger3Name);
                Transform leftMidFinger2Bone = transform.Find(LeftMidFinger2Name);
                Transform leftMidFinger1Bone = transform.Find(LeftMidFinger1Name);

                Transform leftIndexFinger1Bone = transform.Find(LeftIndexFinger1Name);
                Transform leftIndexFinger2Bone = transform.Find(LeftIndexFinger2Name);
                Transform leftIndexFinger3Bone = transform.Find(LeftIndexFinger3Name);

                Transform leftRingFinger1Bone = transform.Find(LeftRingFinger1Name);
                Transform leftRingFinger2Bone = transform.Find(LeftRingFinger2Name);
                Transform leftRingFinger3Bone = transform.Find(LeftRingFinger3Name);

                Transform leftPinkyFinger1Bone = transform.Find(LeftPinkyFinger1Name);
                Transform leftPinkyFinger2Bone = transform.Find(LeftPinkyFinger2Name);
                Transform leftPinkyFinger3Bone = transform.Find(LeftPinkyFinger3Name);

                Transform leftThumb1Bone = transform.Find(LeftThumb1Name);
                Transform leftThumb2Bone = transform.Find(LeftThumb2Name);
                Transform leftThumb3Bone = transform.Find(LeftThumb3Name);

                // If these are the very first landmarks detected, save the starting positions of the bones (relative to their parent transforms)
                // so that these values can be used ​​to calculate rotations later.
                if (LeftHandTransformState.IsFirstHandLandmark)
                {
                    LeftHandTransformState.IndexFinger3StartPos = new Vector3(0, leftIndexFinger3Position.y - leftIndexFinger2Position.y, 0);
                    LeftHandTransformState.IndexFinger2StartPos = new Vector3(0, leftIndexFinger2Position.y - leftIndexFinger1Position.y, 0);

                    LeftHandTransformState.MidFinger3StartPos = new Vector3(leftMiddleFinger3Position.x - leftMiddleFinger2Position.x, leftMiddleFinger3Position.y - leftMiddleFinger2Position.y, 0);
                    LeftHandTransformState.MidFinger2StartPos = new Vector3(0, leftMiddleFinger2Position.y - leftMiddleFinger1Position.y, 0);

                    LeftHandTransformState.RingFinger3StartPos = new Vector3(0, leftRingFinger3Position.y - leftRingFinger2Position.y, 0);
                    LeftHandTransformState.RingFinger2StartPos = new Vector3(0, leftRingFinger2Position.y - leftRingFinger1Position.y, 0);

                    LeftHandTransformState.PinkyFinger3StartPos = new Vector3(0, leftPinkyFinger3Position.y - leftPinkyFinger2Position.y, 0);
                    LeftHandTransformState.PinkyFinger2StartPos = new Vector3(0, leftPinkyFinger2Position.y - leftPinkyFinger1Position.y, 0);

                    LeftHandTransformState.Thumb3StartPos = new Vector3(leftThumb3Position.x - leftThumb2Position.x, leftThumb3Position.y - leftThumb2Position.y, 0);

                    LeftHandTransformState.IndexFinger1StartPos = new Vector3(leftIndexFinger1Position.x - leftHandPosition.x, leftIndexFinger1Position.y - leftHandPosition.y, 0);

                    LeftHandTransformState.IsFirstHandLandmark = false;
                }

                // Rotating the wrist.
                // Interval where palm should be facing the camera.
                if (LeftHandTransformState.HandToHeadCoordinateDifference.x < HandXCoordinatesDiffIntervalToFaceTheCamera.Item2 && LeftHandTransformState.HandToHeadCoordinateDifference.x > HandXCoordinatesDiffIntervalToFaceTheCamera.Item1)
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
                        float newWristAngle = rotationSolver.FindThumbAndWristXRotation(leftIndexFinger1Position, leftHandPosition, LeftHandTransformState.IndexFinger1StartPos);
                        ik.solver.leftHandEffector.rotation *= Quaternion.Euler(-newWristAngle, 0, 0);

                        // If the thumbs up or thumbs down gesture was recognized, animate accordingly.
                        if (leftHandGesture == "Thumb_Up")
                        {
                            ik.solver.leftHandEffector.rotation = leftHandTargetRotation * Quaternion.Euler(-90, -80, -80);
                            ik.solver.leftHandEffector.rotationWeight = 0.8f;
                            LeftHandTransformState.HandIKRotationWeight = ik.solver.leftHandEffector.rotationWeight;
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
                            float newAngle = rotationSolver.FindRotationForFlexionAndExtention(leftMiddleFinger3Position, leftMiddleFinger2Position, LeftHandTransformState.MidFinger3StartPos);
                            rotationSolver.SetFingertipRotation(newAngle, leftMidFinger3Bone, leftMidFinger2Bone);
                            float newAngleMiddleFinger = newAngle;


                            newAngle = rotationSolver.FindRotationForFlexionAndExtention(leftMiddleFinger2Position, leftMiddleFinger1Position, LeftHandTransformState.MidFinger2StartPos);
                            rotationSolver.SetBaseOfTheFingerRotation(newAngle, leftMidFinger1Bone);

                            // Index Finger
                            newAngle = rotationSolver.FindRotationForFlexionAndExtention(leftIndexFinger3Position, leftIndexFinger2Position, LeftHandTransformState.IndexFinger3StartPos);
                            rotationSolver.SetFingertipRotation(newAngle, leftIndexFinger3Bone, leftIndexFinger2Bone);
                            float newAngleIndexFinger = newAngle;

                            newAngle = rotationSolver.FindRotationForFlexionAndExtention(leftIndexFinger2Position, leftIndexFinger1Position, LeftHandTransformState.IndexFinger2StartPos);
                            rotationSolver.SetBaseOfTheFingerRotation(newAngle, leftIndexFinger1Bone);

                            // Ring Finger
                            newAngle = rotationSolver.FindRotationForFlexionAndExtention(leftRingFinger3Position, leftRingFinger2Position, LeftHandTransformState.RingFinger3StartPos);
                            rotationSolver.SetFingertipRotation(newAngle, leftRingFinger3Bone, leftRingFinger2Bone);
                            float newAngleRingFinger = newAngle;

                            newAngle = rotationSolver.FindRotationForFlexionAndExtention(leftRingFinger2Position, leftRingFinger1Position, LeftHandTransformState.RingFinger2StartPos);
                            rotationSolver.SetBaseOfTheFingerRotation(newAngle, leftRingFinger1Bone);

                            // Pinky
                            newAngle = rotationSolver.FindRotationForFlexionAndExtention(leftPinkyFinger3Position, leftPinkyFinger2Position, LeftHandTransformState.PinkyFinger3StartPos);
                            rotationSolver.SetFingertipRotation(newAngle, leftPinkyFinger3Bone, leftPinkyFinger2Bone);
                            float newAnglePinky = newAngle;

                            newAngle = rotationSolver.FindRotationForFlexionAndExtention(leftPinkyFinger2Position, leftPinkyFinger1Position, LeftHandTransformState.PinkyFinger2StartPos);
                            rotationSolver.SetBaseOfTheFingerRotation(newAngle, leftPinkyFinger1Bone);

                            // Thumb
                            float newAngleThumb = rotationSolver.FindThumbAndWristXRotation(leftThumb3Position, leftThumb2Position, LeftHandTransformState.Thumb3StartPos);
                            leftThumb2Bone.localRotation *= Quaternion.Euler(-newAngleThumb, 0, 0);

                            // Animate the palm rotation from facing forward to the back of the palm facing forward.
                            // It's important to check whether the user has flexed their fingers, as flexing the fingers will
                            // sometimes be interpreted as a palm rotation due to the lack of Z-coordinate.
                            if (leftIndexFinger1Position.y - LeftHandTransformState.IndexFinger1StartPos.y <= 0.005f)
                            {
                                newWristAngle = rotationSolver.FindWristYRotation(leftIndexFinger1Position, leftHandPosition, LeftHandTransformState.IndexFinger1StartPos);
                                if (!float.IsNaN(newWristAngle) && newWristAngle <= 120f && !AreFingersBent(newAngleIndexFinger, newAngleMiddleFinger, newAngleRingFinger, newAnglePinky))
                                {
                                    ik.solver.leftHandEffector.rotation *= Quaternion.Euler(0, newWristAngle, 0);
                                }
                            }
                        }
                    }
                }
                //Save information about current hand and finger rotations.
                LeftHandTransformState.HandIKEffectorPosition = ik.solver.leftHandEffector.position;
                LeftHandTransformState.HandIKEffectorRotation = ik.solver.leftHandEffector.rotation;
                LeftHandTransformState.IndexFingerRotations = new Vector3(leftIndexFinger1Bone.localRotation.eulerAngles.z, leftIndexFinger2Bone.localRotation.eulerAngles.z, leftIndexFinger3Bone.localRotation.eulerAngles.z);
                LeftHandTransformState.MiddleFingerRotations = new Vector3(leftMidFinger1Bone.localRotation.eulerAngles.z, leftMidFinger2Bone.localRotation.eulerAngles.z, leftMidFinger3Bone.localRotation.eulerAngles.z);
                LeftHandTransformState.RingFingerRotations = new Vector3(leftRingFinger1Bone.localRotation.eulerAngles.z, leftRingFinger2Bone.localRotation.eulerAngles.z, leftRingFinger3Bone.localRotation.eulerAngles.z);
                LeftHandTransformState.PinkyFingerRotations = new Vector3(leftPinkyFinger1Bone.localRotation.eulerAngles.z, leftPinkyFinger2Bone.localRotation.eulerAngles.z, leftPinkyFinger3Bone.localRotation.eulerAngles.z);
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
        public void SolveRightHand(HandLandmarkerResult resultHandLandmarker, GestureRecognizerResult resultGestureRecognizer, PoseLandmarkerResult resultPoseLandmarker)
        {
            //Index of values ​​for the right hand in the list of coordinates from hand landmarker model.
            int rightHandResultIndex = resultHandLandmarker.handedness.IndexOf(resultHandLandmarker.handedness.Find(x => x.categories[0].categoryName == "Right"));

            int rightHandInTheGesturesList = -1;
            if (resultGestureRecognizer.handedness != null)
            {
                //Index of values ​​for the right hand in the output from gesture recognizer model.
                rightHandInTheGesturesList = resultGestureRecognizer.handedness.IndexOf(resultGestureRecognizer.handedness.Find(x => x.categories[0].categoryName == "Right"));
            }
            String rightHandGesture = "None";
            if (rightHandInTheGesturesList != -1)
            {
                rightHandGesture = resultGestureRecognizer.gestures[rightHandInTheGesturesList].categories[0].categoryName;
            }

            //If the right hand was detected, get world coordinates of the keypoints.
            if (rightHandResultIndex >= 0)
            {
                List<Landmark> rightHandLandmarks = resultHandLandmarker.handWorldLandmarks[rightHandResultIndex].landmarks;

                Landmark rightMiddleFinger3Position = rightHandLandmarks[11];
                Landmark rightMiddleFinger2Position = rightHandLandmarks[10];
                Landmark rightMiddleFinger1Position = rightHandLandmarks[9];

                Landmark rightIndexFinger3Position = rightHandLandmarks[7];
                Landmark rightIndexFinger2Position = rightHandLandmarks[6];
                Landmark rightIndexFinger1Position = rightHandLandmarks[5];

                Landmark rightRingFinger3Position = rightHandLandmarks[15];
                Landmark rightRingFinger2Position = rightHandLandmarks[14];
                Landmark rightRingFinger1Position = rightHandLandmarks[13];

                Landmark rightPinkyFinger3Position = rightHandLandmarks[19];
                Landmark rightPinkyFinger2Position = rightHandLandmarks[18];
                Landmark rightPinkyFinger1Position = rightHandLandmarks[17];

                Landmark rightThumb3Position = rightHandLandmarks[3];
                Landmark rightThumb2Position = rightHandLandmarks[2];
                Landmark rightThumb1Position = rightHandLandmarks[1];

                Landmark rightHandPosition = rightHandLandmarks[0];

                //Get transform components of avatar fingers.
                Transform rightMidFinger3Bone = transform.Find(RightMidFinger3Name);
                Transform rightMidFinger2Bone = transform.Find(RightMidFinger2Name);
                Transform rightMidFinger1Bone = transform.Find(RightMidFinger1Name);

                Transform rightIndexFinger1Bone = transform.Find(RightIndexFinger1Name);
                Transform rightIndexFinger2Bone = transform.Find(RightIndexFinger2Name);
                Transform rightIndexFinger3Bone = transform.Find(RightIndexFinger3Name);

                Transform rightRingFinger1Bone = transform.Find(RightRingFinger1Name);
                Transform rightRingFinger2Bone = transform.Find(RightRingFinger2Name);
                Transform rightRingFinger3Bone = transform.Find(RightRingFinger3Name);

                Transform rightPinkyFinger1Bone = transform.Find(RightPinkyFinger1Name);
                Transform rightPinkyFinger2Bone = transform.Find(RightPinkyFinger2Name);
                Transform rightPinkyFinger3Bone = transform.Find(RightPinkyFinger3Name);

                Transform rightThumb1Bone = transform.Find(RightThumb1Name);
                Transform rightThumb2Bone = transform.Find(RightThumb2Name);
                Transform rightThumb3Bone = transform.Find(RightThumb3Name);

                // If these are the very first landmarks detected, save the starting positions of the bones (relative to their parent transforms)
                // so that these values can be used ​​to calculate rotations later.
                if (RightHandTransformState.IsFirstHandLandmark)
                {
                    RightHandTransformState.IndexFinger3StartPos = new Vector3(0, rightIndexFinger3Position.y - rightIndexFinger2Position.y, 0);
                    RightHandTransformState.IndexFinger2StartPos = new Vector3(0, rightIndexFinger2Position.y - rightIndexFinger1Position.y, 0);

                    RightHandTransformState.MidFinger3StartPos = new Vector3(rightMiddleFinger3Position.x - rightMiddleFinger2Position.x, rightMiddleFinger3Position.y - rightMiddleFinger2Position.y, 0);
                    RightHandTransformState.MidFinger2StartPos = new Vector3(0, rightMiddleFinger2Position.y - rightMiddleFinger1Position.y, 0);

                    RightHandTransformState.RingFinger3StartPos = new Vector3(0, rightRingFinger3Position.y - rightRingFinger2Position.y, 0);
                    RightHandTransformState.RingFinger2StartPos = new Vector3(0, rightRingFinger2Position.y - rightRingFinger1Position.y, 0);

                    RightHandTransformState.PinkyFinger3StartPos = new Vector3(0, rightPinkyFinger3Position.y - rightPinkyFinger2Position.y, 0);
                    RightHandTransformState.PinkyFinger2StartPos = new Vector3(0, rightPinkyFinger2Position.y - rightPinkyFinger1Position.y, 0);

                    RightHandTransformState.Thumb3StartPos = new Vector3(rightThumb3Position.x - rightThumb2Position.x, rightThumb3Position.y - rightThumb2Position.y, 0);

                    RightHandTransformState.IndexFinger1StartPos = new Vector3(rightIndexFinger1Position.x - rightHandPosition.x, rightIndexFinger1Position.y - rightHandPosition.y, 0);

                    RightHandTransformState.IsFirstHandLandmark = false;
                }

                // Rotating the wrist.
                // Interval where palm should be facing the camera.
                if (RightHandTransformState.HandToHeadCoordinateDifference.x > -HandXCoordinatesDiffIntervalToFaceTheCamera.Item2 && RightHandTransformState.HandToHeadCoordinateDifference.x < -HandXCoordinatesDiffIntervalToFaceTheCamera.Item1)
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
                        float newWristAngle = rotationSolver.FindThumbAndWristXRotation(rightIndexFinger1Position, rightHandPosition, RightHandTransformState.IndexFinger1StartPos);
                        ik.solver.rightHandEffector.rotation *= Quaternion.Euler(newWristAngle, 0, 0);

                        // If the thumbs up or thumbs down gesture was recognized, animate accordingly.
                        if (rightHandGesture == "Thumb_Up")
                        {
                            ik.solver.rightHandEffector.rotation = rightHandTargetRotation * Quaternion.Euler(-90, -80, -80);
                            ik.solver.rightHandEffector.rotationWeight = 0.8f;
                            RightHandTransformState.HandIKRotationWeight = ik.solver.rightHandEffector.rotationWeight;
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
                            float newAngle = rotationSolver.FindRotationForFlexionAndExtention(rightMiddleFinger3Position, rightMiddleFinger2Position, RightHandTransformState.MidFinger3StartPos);
                            rotationSolver.SetFingertipRotation(-newAngle, rightMidFinger3Bone, rightMidFinger2Bone);
                            float newAngleMiddleFinger = newAngle;

                            newAngle = rotationSolver.FindRotationForFlexionAndExtention(rightMiddleFinger2Position, rightMiddleFinger1Position, RightHandTransformState.MidFinger2StartPos);
                            rotationSolver.SetBaseOfTheFingerRotation(-newAngle, rightMidFinger1Bone);

                            // Index Finger
                            newAngle = rotationSolver.FindRotationForFlexionAndExtention(rightIndexFinger3Position, rightIndexFinger2Position, RightHandTransformState.IndexFinger3StartPos);
                            rotationSolver.SetFingertipRotation(-newAngle, rightIndexFinger3Bone, rightIndexFinger2Bone);
                            float newAngleIndexFinger = newAngle;

                            newAngle = rotationSolver.FindRotationForFlexionAndExtention(rightIndexFinger2Position, rightIndexFinger1Position, RightHandTransformState.IndexFinger2StartPos);
                            rotationSolver.SetBaseOfTheFingerRotation(-newAngle, rightIndexFinger1Bone);

                            // Ring Finger
                            newAngle = rotationSolver.FindRotationForFlexionAndExtention(rightRingFinger3Position, rightRingFinger2Position, RightHandTransformState.RingFinger3StartPos);
                            rotationSolver.SetFingertipRotation(-newAngle, rightRingFinger3Bone, rightRingFinger2Bone);
                            float newAngleRingFinger = newAngle;

                            newAngle = rotationSolver.FindRotationForFlexionAndExtention(rightRingFinger2Position, rightRingFinger1Position, RightHandTransformState.RingFinger2StartPos);
                            rotationSolver.SetBaseOfTheFingerRotation(-newAngle, rightRingFinger1Bone);

                            // Pinky
                            newAngle = rotationSolver.FindRotationForFlexionAndExtention(rightPinkyFinger3Position, rightPinkyFinger2Position, RightHandTransformState.PinkyFinger3StartPos);
                            rotationSolver.SetFingertipRotation(-newAngle, rightPinkyFinger3Bone, rightPinkyFinger2Bone);
                            float newAnglePinky = newAngle;

                            newAngle = rotationSolver.FindRotationForFlexionAndExtention(rightPinkyFinger2Position, rightPinkyFinger1Position, RightHandTransformState.PinkyFinger2StartPos);
                            rotationSolver.SetBaseOfTheFingerRotation(-newAngle, rightPinkyFinger1Bone);

                            // Thumb
                            float newAngleThumb = rotationSolver.FindThumbAndWristXRotation(rightThumb3Position, rightThumb2Position, RightHandTransformState.Thumb3StartPos);
                            rightThumb1Bone.localRotation = Quaternion.Euler(50f, -20f, -15f);
                            rightThumb2Bone.localRotation = Quaternion.Euler(-20f, 0, 8f);
                            rightThumb3Bone.localRotation = Quaternion.Euler(0, 0, 5f);
                            rightThumb2Bone.localRotation *= Quaternion.Euler(newAngleThumb, 0, 0);
                            rightThumb1Bone.localRotation *= Quaternion.Euler(newAngleThumb / 4, 0, 0);

                            // Animate the palm rotation from facing forward to the back of the palm facing forward.
                            // It's important to check whether the user has flexed their fingers, as flexing the fingers will
                            // sometimes be interpreted as a palm rotation due to the lack of Z-coordinate.
                            if (rightIndexFinger1Position.y - RightHandTransformState.IndexFinger1StartPos.y <= 0.005f)
                            {
                                newWristAngle = rotationSolver.FindWristYRotation(rightIndexFinger1Position, rightHandPosition, RightHandTransformState.IndexFinger1StartPos);
                                if (!float.IsNaN(newWristAngle) && newWristAngle <= 120f && !AreFingersBent(newAngleIndexFinger, newAngleMiddleFinger, newAngleRingFinger, newAnglePinky))
                                {
                                    ik.solver.rightHandEffector.rotation *= Quaternion.Euler(0, -newWristAngle, 0);
                                }
                            }
                        }
                    }
                }
                //Save information about current hand and finger rotations.
                RightHandTransformState.HandIKEffectorPosition = ik.solver.rightHandEffector.position;
                RightHandTransformState.HandIKEffectorRotation = ik.solver.rightHandEffector.rotation;
                RightHandTransformState.IndexFingerRotations = new Vector3(rightIndexFinger1Bone.localRotation.eulerAngles.z, rightIndexFinger2Bone.localRotation.eulerAngles.z, rightIndexFinger3Bone.localRotation.eulerAngles.z);
                RightHandTransformState.MiddleFingerRotations = new Vector3(rightMidFinger1Bone.localRotation.eulerAngles.z, rightMidFinger2Bone.localRotation.eulerAngles.z, rightMidFinger3Bone.localRotation.eulerAngles.z);
                RightHandTransformState.RingFingerRotations = new Vector3(rightRingFinger1Bone.localRotation.eulerAngles.z, rightRingFinger2Bone.localRotation.eulerAngles.z, rightRingFinger3Bone.localRotation.eulerAngles.z);
                RightHandTransformState.PinkyFingerRotations = new Vector3(rightPinkyFinger1Bone.localRotation.eulerAngles.z, rightPinkyFinger2Bone.localRotation.eulerAngles.z, rightPinkyFinger3Bone.localRotation.eulerAngles.z);
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
        /// <returns></returns>
        public bool AreFingersBent( float newAngleIndexFinger, float newAngleMiddleFinger, float newAngleRingFinger, float newAnglePinky)
        {
            return newAngleIndexFinger >= 50f || newAngleMiddleFinger >= 50f || newAngleRingFinger >= 50f || newAnglePinky >= 50f;
        }
    }
}