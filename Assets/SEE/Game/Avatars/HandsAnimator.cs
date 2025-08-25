using System;
using UnityEngine;
using RootMotion.FinalIK;
using SEE.GO;
using Mediapipe.Tasks.Vision.PoseLandmarker;
using Mediapipe.Tasks.Vision.HandLandmarker;
using Mediapipe.Tasks.Vision.GestureRecognizer;


namespace SEE.Game.Avatars
{
    public class HandsAnimator
    {
        Transform transform; //main transform of the avatar

        private const string headName = "CC_Base_BoneRoot/CC_Base_Hip/CC_Base_Waist/CC_Base_Spine01/CC_Base_Spine02/CC_Base_NeckTwist01/CC_Base_NeckTwist02/CC_Base_Head";

        private const string leftHandName = "CC_Base_BoneRoot/CC_Base_Hip/CC_Base_Waist/CC_Base_Spine01/CC_Base_Spine02/CC_Base_L_Clavicle/CC_Base_L_Upperarm/CC_Base_L_Forearm/CC_Base_L_Hand";

        private const string leftMidFinger1Name = "CC_Base_BoneRoot/CC_Base_Hip/CC_Base_Waist/CC_Base_Spine01/CC_Base_Spine02/CC_Base_L_Clavicle/CC_Base_L_Upperarm/CC_Base_L_Forearm/CC_Base_L_Hand/CC_Base_L_Mid1";
        private const string leftMidFinger2Name = "CC_Base_BoneRoot/CC_Base_Hip/CC_Base_Waist/CC_Base_Spine01/CC_Base_Spine02/CC_Base_L_Clavicle/CC_Base_L_Upperarm/CC_Base_L_Forearm/CC_Base_L_Hand/CC_Base_L_Mid1/CC_Base_L_Mid2";
        private const string leftMidFinger3Name = "CC_Base_BoneRoot/CC_Base_Hip/CC_Base_Waist/CC_Base_Spine01/CC_Base_Spine02/CC_Base_L_Clavicle/CC_Base_L_Upperarm/CC_Base_L_Forearm/CC_Base_L_Hand/CC_Base_L_Mid1/CC_Base_L_Mid2/CC_Base_L_Mid3";

        private const string leftIndexFinger1Name = "CC_Base_BoneRoot/CC_Base_Hip/CC_Base_Waist/CC_Base_Spine01/CC_Base_Spine02/CC_Base_L_Clavicle/CC_Base_L_Upperarm/CC_Base_L_Forearm/CC_Base_L_Hand/CC_Base_L_Index1";
        private const string leftIndexFinger2Name = "CC_Base_BoneRoot/CC_Base_Hip/CC_Base_Waist/CC_Base_Spine01/CC_Base_Spine02/CC_Base_L_Clavicle/CC_Base_L_Upperarm/CC_Base_L_Forearm/CC_Base_L_Hand/CC_Base_L_Index1/CC_Base_L_Index2";
        private const string leftIndexFinger3Name = "CC_Base_BoneRoot/CC_Base_Hip/CC_Base_Waist/CC_Base_Spine01/CC_Base_Spine02/CC_Base_L_Clavicle/CC_Base_L_Upperarm/CC_Base_L_Forearm/CC_Base_L_Hand/CC_Base_L_Index1/CC_Base_L_Index2/CC_Base_L_Index3";

        private const string leftRingFinger1Name = "CC_Base_BoneRoot/CC_Base_Hip/CC_Base_Waist/CC_Base_Spine01/CC_Base_Spine02/CC_Base_L_Clavicle/CC_Base_L_Upperarm/CC_Base_L_Forearm/CC_Base_L_Hand/CC_Base_L_Ring1";
        private const string leftRingFinger2Name = "CC_Base_BoneRoot/CC_Base_Hip/CC_Base_Waist/CC_Base_Spine01/CC_Base_Spine02/CC_Base_L_Clavicle/CC_Base_L_Upperarm/CC_Base_L_Forearm/CC_Base_L_Hand/CC_Base_L_Ring1/CC_Base_L_Ring2";
        private const string leftRingFinger3Name = "CC_Base_BoneRoot/CC_Base_Hip/CC_Base_Waist/CC_Base_Spine01/CC_Base_Spine02/CC_Base_L_Clavicle/CC_Base_L_Upperarm/CC_Base_L_Forearm/CC_Base_L_Hand/CC_Base_L_Ring1/CC_Base_L_Ring2/CC_Base_L_Ring3";

        private const string leftPinkyFinger1Name = "CC_Base_BoneRoot/CC_Base_Hip/CC_Base_Waist/CC_Base_Spine01/CC_Base_Spine02/CC_Base_L_Clavicle/CC_Base_L_Upperarm/CC_Base_L_Forearm/CC_Base_L_Hand/CC_Base_L_Pinky1";
        private const string leftPinkyFinger2Name = "CC_Base_BoneRoot/CC_Base_Hip/CC_Base_Waist/CC_Base_Spine01/CC_Base_Spine02/CC_Base_L_Clavicle/CC_Base_L_Upperarm/CC_Base_L_Forearm/CC_Base_L_Hand/CC_Base_L_Pinky1/CC_Base_L_Pinky2";
        private const string leftPinkyFinger3Name = "CC_Base_BoneRoot/CC_Base_Hip/CC_Base_Waist/CC_Base_Spine01/CC_Base_Spine02/CC_Base_L_Clavicle/CC_Base_L_Upperarm/CC_Base_L_Forearm/CC_Base_L_Hand/CC_Base_L_Pinky1/CC_Base_L_Pinky2/CC_Base_L_Pinky3";

        private const string leftThumb1Name = "CC_Base_BoneRoot/CC_Base_Hip/CC_Base_Waist/CC_Base_Spine01/CC_Base_Spine02/CC_Base_L_Clavicle/CC_Base_L_Upperarm/CC_Base_L_Forearm/CC_Base_L_Hand/CC_Base_L_Thumb1";
        private const string leftThumb2Name = "CC_Base_BoneRoot/CC_Base_Hip/CC_Base_Waist/CC_Base_Spine01/CC_Base_Spine02/CC_Base_L_Clavicle/CC_Base_L_Upperarm/CC_Base_L_Forearm/CC_Base_L_Hand/CC_Base_L_Thumb1/CC_Base_L_Thumb2";
        private const string leftThumb3Name = "CC_Base_BoneRoot/CC_Base_Hip/CC_Base_Waist/CC_Base_Spine01/CC_Base_Spine02/CC_Base_L_Clavicle/CC_Base_L_Upperarm/CC_Base_L_Forearm/CC_Base_L_Hand/CC_Base_L_Thumb1/CC_Base_L_Thumb2/CC_Base_L_Thumb3";

        private const string rightHandName = "CC_Base_BoneRoot/CC_Base_Hip/CC_Base_Waist/CC_Base_Spine01/CC_Base_Spine02/CC_Base_R_Clavicle/CC_Base_R_Upperarm/CC_Base_R_Forearm/CC_Base_R_Hand";

        private const string rightMidFinger1Name = "CC_Base_BoneRoot/CC_Base_Hip/CC_Base_Waist/CC_Base_Spine01/CC_Base_Spine02/CC_Base_R_Clavicle/CC_Base_R_Upperarm/CC_Base_R_Forearm/CC_Base_R_Hand/CC_Base_R_Mid1";
        private const string rightMidFinger2Name = "CC_Base_BoneRoot/CC_Base_Hip/CC_Base_Waist/CC_Base_Spine01/CC_Base_Spine02/CC_Base_R_Clavicle/CC_Base_R_Upperarm/CC_Base_R_Forearm/CC_Base_R_Hand/CC_Base_R_Mid1/CC_Base_R_Mid2";
        private const string rightMidFinger3Name = "CC_Base_BoneRoot/CC_Base_Hip/CC_Base_Waist/CC_Base_Spine01/CC_Base_Spine02/CC_Base_R_Clavicle/CC_Base_R_Upperarm/CC_Base_R_Forearm/CC_Base_R_Hand/CC_Base_R_Mid1/CC_Base_R_Mid2/CC_Base_R_Mid3";

        private const string rightIndexFinger1Name = "CC_Base_BoneRoot/CC_Base_Hip/CC_Base_Waist/CC_Base_Spine01/CC_Base_Spine02/CC_Base_R_Clavicle/CC_Base_R_Upperarm/CC_Base_R_Forearm/CC_Base_R_Hand/CC_Base_R_Index1";
        private const string rightIndexFinger2Name = "CC_Base_BoneRoot/CC_Base_Hip/CC_Base_Waist/CC_Base_Spine01/CC_Base_Spine02/CC_Base_R_Clavicle/CC_Base_R_Upperarm/CC_Base_R_Forearm/CC_Base_R_Hand/CC_Base_R_Index1/CC_Base_R_Index2";
        private const string rightIndexFinger3Name = "CC_Base_BoneRoot/CC_Base_Hip/CC_Base_Waist/CC_Base_Spine01/CC_Base_Spine02/CC_Base_R_Clavicle/CC_Base_R_Upperarm/CC_Base_R_Forearm/CC_Base_R_Hand/CC_Base_R_Index1/CC_Base_R_Index2/CC_Base_R_Index3";

        private const string rightRingFinger1Name = "CC_Base_BoneRoot/CC_Base_Hip/CC_Base_Waist/CC_Base_Spine01/CC_Base_Spine02/CC_Base_R_Clavicle/CC_Base_R_Upperarm/CC_Base_R_Forearm/CC_Base_R_Hand/CC_Base_R_Ring1";
        private const string rightRingFinger2Name = "CC_Base_BoneRoot/CC_Base_Hip/CC_Base_Waist/CC_Base_Spine01/CC_Base_Spine02/CC_Base_R_Clavicle/CC_Base_R_Upperarm/CC_Base_R_Forearm/CC_Base_R_Hand/CC_Base_R_Ring1/CC_Base_R_Ring2";
        private const string rightRingFinger3Name = "CC_Base_BoneRoot/CC_Base_Hip/CC_Base_Waist/CC_Base_Spine01/CC_Base_Spine02/CC_Base_R_Clavicle/CC_Base_R_Upperarm/CC_Base_R_Forearm/CC_Base_R_Hand/CC_Base_R_Ring1/CC_Base_R_Ring2/CC_Base_R_Ring3";

        private const string rightPinkyFinger1Name = "CC_Base_BoneRoot/CC_Base_Hip/CC_Base_Waist/CC_Base_Spine01/CC_Base_Spine02/CC_Base_R_Clavicle/CC_Base_R_Upperarm/CC_Base_R_Forearm/CC_Base_R_Hand/CC_Base_R_Pinky1";
        private const string rightPinkyFinger2Name = "CC_Base_BoneRoot/CC_Base_Hip/CC_Base_Waist/CC_Base_Spine01/CC_Base_Spine02/CC_Base_R_Clavicle/CC_Base_R_Upperarm/CC_Base_R_Forearm/CC_Base_R_Hand/CC_Base_R_Pinky1/CC_Base_R_Pinky2";
        private const string rightPinkyFinger3Name = "CC_Base_BoneRoot/CC_Base_Hip/CC_Base_Waist/CC_Base_Spine01/CC_Base_Spine02/CC_Base_R_Clavicle/CC_Base_R_Upperarm/CC_Base_R_Forearm/CC_Base_R_Hand/CC_Base_R_Pinky1/CC_Base_R_Pinky2/CC_Base_R_Pinky3";

        private const string rightThumb1Name = "CC_Base_BoneRoot/CC_Base_Hip/CC_Base_Waist/CC_Base_Spine01/CC_Base_Spine02/CC_Base_R_Clavicle/CC_Base_R_Upperarm/CC_Base_R_Forearm/CC_Base_R_Hand/CC_Base_R_Thumb1";
        private const string rightThumb2Name = "CC_Base_BoneRoot/CC_Base_Hip/CC_Base_Waist/CC_Base_Spine01/CC_Base_Spine02/CC_Base_R_Clavicle/CC_Base_R_Upperarm/CC_Base_R_Forearm/CC_Base_R_Hand/CC_Base_R_Thumb1/CC_Base_R_Thumb2";
        private const string rightThumb3Name = "CC_Base_BoneRoot/CC_Base_Hip/CC_Base_Waist/CC_Base_Spine01/CC_Base_Spine02/CC_Base_R_Clavicle/CC_Base_R_Upperarm/CC_Base_R_Forearm/CC_Base_R_Hand/CC_Base_R_Thumb1/CC_Base_R_Thumb2/CC_Base_R_Thumb3";

        private FullBodyBipedIK ik;

        public float weight = 1f;

        private Vector3 headPosition = Vector3.one;

        private Vector3 currentLeftHandPosition = Vector3.one;
        private Quaternion startLeftHandRotation;
        public Quaternion currentLeftHandRotation;

        private Vector3 currentRightHandPosition = Vector3.one;
        private Quaternion startRightHandRotation;
        public Quaternion currentRightHandRotation;

        private const float moveSpeed = 0.5f;
        private const float arrivalThreshold = 0.01f;
        private bool startHandsPositionReached = false;
        private bool isFirstPoseLandmark = true;

        Vector3 previousLeftHandMediapipeCoordinates = Vector3.zero;
        Vector3 newLeftHandMediapipeCoordinates = Vector3.zero;
        Vector3 previousRightHandMediapipeCoordinates = Vector3.zero;
        Vector3 newRightHandMediapipeCoordinates = Vector3.zero;

        Vector3 leftHandStartPos = Vector3.zero;
        Vector3 rightHandStartPos = Vector3.zero;

        private float AcceptableHandPresenceProbability = 0.5f;
        private Quaternion LeftHandRotationInFrontOfTheCharacter;
        private Quaternion RightHandRotationInFrontOfTheCharacter;
        private Quaternion LeftHandYRotationMovementToTheSide;
        private Quaternion RightHandYRotationMovementToTheSide;
        private Quaternion LeftHandZRotationForMovementDown;
        private Quaternion RightHandZRotationForMovementDown;
        private Tuple<float, float> HandXCoordinatesDiffIntervalToFaceTheCamera = Tuple.Create(-0.47f, -0.15f);
        private Tuple<float, float> HandXCoordinatesDiffIntervalMovingInFront = Tuple.Create(-0.15f, 0.28f);
        private float HandYCoordinatesDiffToMoveDownFrom = -0.3f;


        public void initialize(Transform mainTrasform, FullBodyBipedIK ikComponent)
        {
            this.ik = ikComponent;
            this.transform = mainTrasform;

            Transform headBone = mainTrasform.Find(headName);
            Transform leftHandBone = mainTrasform.Find(leftHandName);
            Transform rightHandBone = mainTrasform.Find(rightHandName);
            if (headBone == null)
            {
                UnityEngine.Debug.LogError($"Head bone not found: {headName}");
                return;
            }
            else if (leftHandBone == null)
            {
                UnityEngine.Debug.LogError($"Left hand bone not found: {leftHandName}");
                return;
            }
            else if (rightHandBone == null)
            {
                UnityEngine.Debug.LogError($"Right hand bone not found: {rightHandName}");
                return;
            }

            currentLeftHandPosition = leftHandBone.position;
            startLeftHandRotation = leftHandBone.localRotation;
            currentLeftHandRotation = leftHandBone.rotation;

            currentRightHandPosition = rightHandBone.position;
            startRightHandRotation = rightHandBone.localRotation;
            currentRightHandRotation = rightHandBone.rotation;

            headPosition = mainTrasform.InverseTransformPoint(headBone.position);

            ik.solver.leftHandEffector.positionWeight = weight;
            ik.solver.leftHandEffector.rotationWeight = weight;

            GameObject leftElbowBendGoal = new GameObject("LeftElbowBendGoal");
            leftElbowBendGoal.transform.SetParent(mainTrasform, false);
            ik.solver.leftArmChain.bendConstraint.bendGoal = leftElbowBendGoal.transform;
            ik.solver.leftArmChain.bendConstraint.bendGoal.localPosition = new Vector3(-0.5f, 0.5f, 0);

            GameObject rightElbowBendGoal = new GameObject("RightElbowBendGoal");
            rightElbowBendGoal.transform.SetParent(mainTrasform, false);
            ik.solver.rightArmChain.bendConstraint.bendGoal = rightElbowBendGoal.transform;
            ik.solver.rightArmChain.bendConstraint.bendGoal.localPosition = new Vector3(0.5f, 0.5f, 0);
        }


        Quaternion leftHandTargetRotation;
        Quaternion rightHandTargetRotation;

        Quaternion leftHandRotationOffset = Quaternion.Euler(170, 110, 0);
        Vector3 leftHandPositionOffset = new Vector3(-0.37f, 1.56f, 0.23f);
        Vector3 leftHandTargetPos;

        Quaternion rightHandRotationOffset = Quaternion.Euler(-40, 15, 60);
        Vector3 rightHandPositionOffset = new Vector3(0.37f, 1.56f, 0.23f);
        Vector3 rightHandTargetPos;

        public Boolean bringHandsToStartPositions()
        {
            Debug.Log(transform);
            Transform headBone = transform.Find(headName);
            headPosition = transform.InverseTransformPoint(headBone.position);

            Transform leftHand = transform.Find(leftHandName);
            leftHand.localRotation = startLeftHandRotation * leftHandRotationOffset;
            leftHandTargetRotation = leftHand.rotation;
            leftHand.localRotation = startLeftHandRotation;

            leftHandTargetPos = transform.TransformPoint(leftHandPositionOffset);


            Transform rightHand = transform.Find(rightHandName);
            rightHand.localRotation = startRightHandRotation * rightHandRotationOffset;
            rightHandTargetRotation = rightHand.rotation;
            rightHand.localRotation = startRightHandRotation;

            rightHandTargetPos = transform.TransformPoint(rightHandPositionOffset);


            if (!startHandsPositionReached && (Vector3.Distance(currentLeftHandPosition, leftHandTargetPos) >= arrivalThreshold || Vector3.Distance(currentRightHandPosition, rightHandTargetPos) >= arrivalThreshold))
            {
                currentLeftHandPosition = Vector3.Lerp(currentLeftHandPosition, leftHandTargetPos, Time.deltaTime * moveSpeed * 4);
                currentLeftHandRotation = Quaternion.Slerp(currentLeftHandRotation, leftHandTargetRotation, Time.deltaTime * moveSpeed * 4);
                ik.solver.leftHandEffector.rotation = currentLeftHandRotation;
                ik.solver.leftHandEffector.position = currentLeftHandPosition;

                ik.solver.leftHandEffector.positionWeight = weight;
                ik.solver.leftHandEffector.rotationWeight = weight;

                currentRightHandPosition = Vector3.Lerp(currentRightHandPosition, rightHandTargetPos, Time.deltaTime * moveSpeed * 4);
                currentRightHandRotation = Quaternion.Slerp(currentRightHandRotation, rightHandTargetRotation, Time.deltaTime * moveSpeed * 4);
                ik.solver.rightHandEffector.rotation = currentRightHandRotation;
                ik.solver.rightHandEffector.position = currentRightHandPosition;

                ik.solver.rightHandEffector.positionWeight = weight;
                ik.solver.rightHandEffector.rotationWeight = weight;

                return false;
            }
            else {
                startHandsPositionReached = true;
                return true;
            }
        }

        public void solveHandsPositions(PoseLandmarkerResult resultPoseLandmarker)
        {
            ik.solver.leftHandEffector.position = leftHandTargetPos;
            ik.solver.rightHandEffector.position = rightHandTargetPos;
            ik.solver.leftHandEffector.rotation = leftHandTargetRotation;
            ik.solver.rightHandEffector.rotation = rightHandTargetRotation;

            Transform leftHand = transform.Find(leftHandName);
            Transform rightHand = transform.Find(rightHandName);

            var poseLandmarks = resultPoseLandmarker.poseWorldLandmarks[0].landmarks;
            var mediapipeLeftHandPosition = poseLandmarks[15];
            var mediapipeRightHandPosition = poseLandmarks[16];

            var mediapipeLeftElbowPosition = poseLandmarks[13];
            var mediapipeRightElbowPosition = poseLandmarks[14];

            if (isFirstPoseLandmark)
            {
                newLeftHandMediapipeCoordinates.x = mediapipeLeftHandPosition.x;
                newLeftHandMediapipeCoordinates.y = mediapipeLeftHandPosition.y;
                newLeftHandMediapipeCoordinates.z = mediapipeLeftHandPosition.z;

                leftHandStartPos = new Vector3(mediapipeLeftHandPosition.x - mediapipeLeftElbowPosition.x, mediapipeLeftHandPosition.y - mediapipeLeftElbowPosition.y, 0);
                rightHandStartPos = new Vector3(mediapipeRightHandPosition.x - mediapipeRightElbowPosition.x, mediapipeRightHandPosition.y - mediapipeRightElbowPosition.y, 0);

                newRightHandMediapipeCoordinates.x = mediapipeRightHandPosition.x;
                newRightHandMediapipeCoordinates.y = mediapipeRightHandPosition.y;
                newRightHandMediapipeCoordinates.z = mediapipeRightHandPosition.z;

                isFirstPoseLandmark = false;
            }


            LeftHandRotationInFrontOfTheCharacter = leftHandTargetRotation * Quaternion.Euler(0, 55, 0);
            RightHandRotationInFrontOfTheCharacter = rightHandTargetRotation * Quaternion.Euler(0, -55, 0);

            LeftHandYRotationMovementToTheSide = leftHandTargetRotation * Quaternion.Euler(0, -50, 0);
            RightHandYRotationMovementToTheSide = rightHandTargetRotation * Quaternion.Euler(0, 50, 0);

            LeftHandZRotationForMovementDown = leftHandTargetRotation * Quaternion.Euler(0, 0, 60);
            RightHandZRotationForMovementDown = rightHandTargetRotation * Quaternion.Euler(0, 0, -60);




            var mediapipeHeadPosition = poseLandmarks[0];

            previousLeftHandMediapipeCoordinates = newLeftHandMediapipeCoordinates;
            newLeftHandMediapipeCoordinates.x = mediapipeLeftHandPosition.x;
            newLeftHandMediapipeCoordinates.y = mediapipeLeftHandPosition.y;
            newLeftHandMediapipeCoordinates.z = mediapipeLeftHandPosition.z;


            previousRightHandMediapipeCoordinates = newLeftHandMediapipeCoordinates;
            newRightHandMediapipeCoordinates.x = mediapipeRightHandPosition.x;
            newRightHandMediapipeCoordinates.y = mediapipeRightHandPosition.y;
            newRightHandMediapipeCoordinates.z = mediapipeRightHandPosition.z;

            if (mediapipeLeftHandPosition.presence > AcceptableHandPresenceProbability)
            {
                leftHandToHeadCoordinateDifference = new Vector3(mediapipeLeftHandPosition.x - mediapipeHeadPosition.x, mediapipeLeftHandPosition.y - mediapipeHeadPosition.y, transform.InverseTransformPoint(leftHandTargetPos).z - headPosition.z);
                var newHandPosition = headPosition + leftHandToHeadCoordinateDifference;//transform.InverseTransformPoint(headPosition) + leftHandToHeadCoordinateDifference;
                ik.solver.leftHandEffector.position = transform.TransformPoint(newHandPosition);

                //interval where palm should be facing the camera
                if (leftHandToHeadCoordinateDifference.x < HandXCoordinatesDiffIntervalToFaceTheCamera.Item2 && leftHandToHeadCoordinateDifference.x > HandXCoordinatesDiffIntervalToFaceTheCamera.Item1)
                {
                    leftHand.localRotation = startLeftHandRotation * leftHandRotationOffset;
                    leftHandTargetRotation = leftHand.rotation;
                    leftHand.localRotation = startLeftHandRotation;
                    currentLeftHandRotation = Quaternion.Slerp(currentLeftHandRotation, leftHandTargetRotation, Time.deltaTime * moveSpeed * 10);
                    ik.solver.leftHandEffector.rotation = currentLeftHandRotation;
                }
                //there if the hand is moving to the right, in front of the character
                else if (leftHandToHeadCoordinateDifference.x >= HandXCoordinatesDiffIntervalMovingInFront.Item1
                    && leftHandToHeadCoordinateDifference.x <= HandXCoordinatesDiffIntervalMovingInFront.Item2)
                {
                    leftHandTargetRotation = LeftHandRotationInFrontOfTheCharacter;
                    if (ik.solver.leftHandEffector.rotation.eulerAngles.y < LeftHandRotationInFrontOfTheCharacter.eulerAngles.y)
                    {
                        currentLeftHandRotation = Quaternion.Slerp(currentLeftHandRotation, leftHandTargetRotation, Time.deltaTime * moveSpeed * 10);
                        ik.solver.leftHandEffector.rotation = currentLeftHandRotation;
                        ik.solver.leftHandEffector.rotationWeight = weight;
                        ik.solver.leftArmChain.bendConstraint.bendGoal.localPosition = new Vector3(-0.5f, 0.5f, 0);
                    }
                }
                //moving to the side, not in front of the character
                else if (previousLeftHandMediapipeCoordinates.x > newLeftHandMediapipeCoordinates.x)
                {
                    leftHandTargetRotation = LeftHandYRotationMovementToTheSide;
                    if (ik.solver.leftHandEffector.rotation.y > LeftHandYRotationMovementToTheSide.y)
                    {
                        currentLeftHandRotation = Quaternion.Slerp(currentLeftHandRotation, leftHandTargetRotation, Time.deltaTime * moveSpeed * 5);
                        ik.solver.leftHandEffector.rotation = currentLeftHandRotation;
                        ik.solver.leftHandEffector.rotationWeight = weight;
                    }
                }
                //moving the hand down
                if (leftHandToHeadCoordinateDifference.y <= HandYCoordinatesDiffToMoveDownFrom)
                {
                    leftHandTargetRotation = LeftHandZRotationForMovementDown;
                    if (ik.solver.leftHandEffector.rotation.z > LeftHandZRotationForMovementDown.z)
                    {
                        currentLeftHandRotation = Quaternion.Slerp(currentLeftHandRotation, leftHandTargetRotation, Time.deltaTime * moveSpeed * 10);
                        ik.solver.leftHandEffector.rotation = currentLeftHandRotation;
                        ik.solver.leftHandEffector.rotationWeight = weight;
                    }
                }
            }
            else
            {
                var newHandPosition = headPosition + leftHandToHeadCoordinateDifference;
                if (leftHandToHeadCoordinateDifference != Vector3.zero)
                {
                    ik.solver.leftHandEffector.position = transform.TransformPoint(newHandPosition);
                }
            }

            /// <summary>
            /// Animating the movement of the right hand with Full Body Biped IK and Mediapipe Landmarks 
            /// by rotating the hand and changing the position coordinates
            /// </summary>
            if (mediapipeRightHandPosition.presence > AcceptableHandPresenceProbability)
            {

                rightHandToHeadCoordinateDifference = new Vector3(mediapipeRightHandPosition.x - mediapipeHeadPosition.x, mediapipeRightHandPosition.y - mediapipeHeadPosition.y, transform.InverseTransformPoint(rightHandTargetPos).z - headPosition.z);
                var newHandPosition = headPosition + rightHandToHeadCoordinateDifference;//transform.InverseTransformPoint(headPosition) + rightHandToHeadCoordinateDifference;
                ik.solver.rightHandEffector.position = transform.TransformPoint(newHandPosition);


                //interval where palm should be facing the camera
                if (rightHandToHeadCoordinateDifference.x > -HandXCoordinatesDiffIntervalToFaceTheCamera.Item2 && rightHandToHeadCoordinateDifference.x < -HandXCoordinatesDiffIntervalToFaceTheCamera.Item1)
                {
                    rightHand.localRotation = startRightHandRotation * rightHandRotationOffset;
                    rightHandTargetRotation = rightHand.rotation;
                    rightHand.localRotation = startRightHandRotation;
                    currentRightHandRotation = Quaternion.Slerp(currentRightHandRotation, rightHandTargetRotation, Time.deltaTime * moveSpeed * 10);
                    ik.solver.rightHandEffector.rotation = currentRightHandRotation;
                }
                //there if the hand is moving to the left, in front of the character
                else if (/**rightHandToHeadCoordinateDifference.x >= HandXCoordinatesDiffIntervalMovingInFront.Item1
                            &&*/ rightHandToHeadCoordinateDifference.x <= -HandXCoordinatesDiffIntervalMovingInFront.Item1)
                {
                    rightHandTargetRotation = RightHandRotationInFrontOfTheCharacter;
                    if (ik.solver.rightHandEffector.rotation.eulerAngles.y > RightHandRotationInFrontOfTheCharacter.eulerAngles.y)
                    {
                        currentRightHandRotation = Quaternion.Slerp(currentRightHandRotation, rightHandTargetRotation, Time.deltaTime * moveSpeed * 10);
                        ik.solver.rightHandEffector.rotation = currentRightHandRotation;
                        ik.solver.rightHandEffector.rotationWeight = weight;
                        ik.solver.rightArmChain.bendConstraint.bendGoal.localPosition = new Vector3(0.5f, 0.5f, 0);//new Vector3(-3.85f, 0.48f, -6.3f);
                    }
                }
                //moving to the side, not in front of the character
                else if (previousRightHandMediapipeCoordinates.x < newRightHandMediapipeCoordinates.x)
                {
                    rightHandTargetRotation = RightHandYRotationMovementToTheSide;
                    if (ik.solver.rightHandEffector.rotation.y < RightHandYRotationMovementToTheSide.y)
                    {
                        currentRightHandRotation = Quaternion.Slerp(currentRightHandRotation, rightHandTargetRotation, Time.deltaTime * moveSpeed * 5);
                        ik.solver.rightHandEffector.rotation = currentRightHandRotation;
                        ik.solver.rightHandEffector.rotationWeight = weight;
                    }
                }
                //moving the hand down
                if (rightHandToHeadCoordinateDifference.y <= HandYCoordinatesDiffToMoveDownFrom)
                {
                    rightHandTargetRotation = RightHandZRotationForMovementDown;
                    if (ik.solver.rightHandEffector.rotation.z > RightHandZRotationForMovementDown.z)
                    {
                        currentRightHandRotation = Quaternion.Slerp(currentRightHandRotation, rightHandTargetRotation, Time.deltaTime * moveSpeed * 10);
                        ik.solver.rightHandEffector.rotation = currentRightHandRotation;
                        ik.solver.rightHandEffector.rotationWeight = weight;
                    }
                }
            }
            else
            {
                var newHandPosition = headPosition + rightHandToHeadCoordinateDifference;
                if (rightHandToHeadCoordinateDifference != Vector3.zero)
                {
                    ik.solver.rightHandEffector.position = transform.TransformPoint(newHandPosition);
                }
            }

        }

        

        private bool firstLeftHandLandmark = true;
        Vector3 leftMidFinger3StartPos = Vector3.zero;
        Vector3 leftMidFinger2StartPos = Vector3.zero;
        Vector3 leftIndexFinger3StartPos = Vector3.zero;
        Vector3 leftIndexFinger2StartPos = Vector3.zero;
        Vector3 leftIndexFinger1StartPos = Vector3.zero;
        Vector3 leftRingFinger3StartPos = Vector3.zero;
        Vector3 leftRingFinger2StartPos = Vector3.zero;
        Vector3 leftPinkyFinger3StartPos = Vector3.zero;
        Vector3 leftPinkyFinger2StartPos = Vector3.zero;
        Vector3 leftThumb3StartPos = Vector3.zero;
        Quaternion leftThumb2BoneStartRotation = Quaternion.Euler(0, 0, 0);
        Vector3 leftHandToHeadCoordinateDifference = Vector3.zero;

        private bool firstRightHandLandmark = true;
        Vector3 rightMidFinger3StartPos = Vector3.zero;
        Vector3 rightMidFinger2StartPos = Vector3.zero;
        Vector3 rightIndexFinger3StartPos = Vector3.zero;
        Vector3 rightIndexFinger2StartPos = Vector3.zero;
        Vector3 rightIndexFinger1StartPos = Vector3.zero;
        Vector3 rightRingFinger3StartPos = Vector3.zero;
        Vector3 rightRingFinger2StartPos = Vector3.zero;
        Vector3 rightPinkyFinger3StartPos = Vector3.zero;
        Vector3 rightPinkyFinger2StartPos = Vector3.zero;
        Vector3 rightThumb3StartPos = Vector3.zero;
        Vector3 rightHandToHeadCoordinateDifference = Vector3.zero;


        FingerRotationSolver rotationSolver = new FingerRotationSolver();

        public void solveLeftHand(HandLandmarkerResult resultHandLandmarker, GestureRecognizerResult resultGestureRecognizer, PoseLandmarkerResult resultPoseLandmarker)
        {
            var leftHandResultIndex = resultHandLandmarker.handedness.IndexOf(resultHandLandmarker.handedness.Find(x => x.categories[0].categoryName == "Left"));

            var leftHandInTheGesturesList = resultGestureRecognizer.handedness.IndexOf(resultGestureRecognizer.handedness.Find(x => x.categories[0].categoryName == "Left"));
            String leftHandGesture = "None";
            if (leftHandInTheGesturesList != -1)
            {
                leftHandGesture = resultGestureRecognizer.gestures[leftHandInTheGesturesList].categories[0].categoryName;
                //Debug.Log("what gesture leftHand: " + resultGestureRecognizer.gestures[leftHandInTheGesturesList].categories[0].categoryName);
            }

            if (leftHandResultIndex >= 0)
            {
                var leftHandLandmarks = resultHandLandmarker.handWorldLandmarks[leftHandResultIndex].landmarks;

                var leftMiddleFinger3Position = leftHandLandmarks[11];
                var leftMiddleFinger2Position = leftHandLandmarks[10];
                var leftMiddleFinger1Position = leftHandLandmarks[9];

                var leftIndexFinger3Position = leftHandLandmarks[7];
                var leftIndexFinger2Position = leftHandLandmarks[6];
                var leftIndexFinger1Position = leftHandLandmarks[5];

                var leftRingFinger3Position = leftHandLandmarks[15];
                var leftRingFinger2Position = leftHandLandmarks[14];
                var leftRingFinger1Position = leftHandLandmarks[13];

                var leftPinkyFinger3Position = leftHandLandmarks[19];
                var leftPinkyFinger2Position = leftHandLandmarks[18];
                var leftPinkyFinger1Position = leftHandLandmarks[17];

                var leftThumb3Position = leftHandLandmarks[3];
                var leftThumb2Position = leftHandLandmarks[2];
                var leftThumb1Position = leftHandLandmarks[1];

                var leftHandPosition = leftHandLandmarks[0];


                Transform leftMidFinger3Bone = transform.Find(leftMidFinger3Name);
                Transform leftMidFinger2Bone = transform.Find(leftMidFinger2Name);
                Transform leftMidFinger1Bone = transform.Find(leftMidFinger1Name);

                Transform leftIndexFinger1Bone = transform.Find(leftIndexFinger1Name);
                Transform leftIndexFinger2Bone = transform.Find(leftIndexFinger2Name);
                Transform leftIndexFinger3Bone = transform.Find(leftIndexFinger3Name);

                Transform leftRingFinger1Bone = transform.Find(leftRingFinger1Name);
                Transform leftRingFinger2Bone = transform.Find(leftRingFinger2Name);
                Transform leftRingFinger3Bone = transform.Find(leftRingFinger3Name);

                Transform leftPinkyFinger1Bone = transform.Find(leftPinkyFinger1Name);
                Transform leftPinkyFinger2Bone = transform.Find(leftPinkyFinger2Name);
                Transform leftPinkyFinger3Bone = transform.Find(leftPinkyFinger3Name);

                Transform leftThumb1Bone = transform.Find(leftThumb1Name);
                Transform leftThumb2Bone = transform.Find(leftThumb2Name);
                Transform leftThumb3Bone = transform.Find(leftThumb3Name);


                if (firstLeftHandLandmark)
                {
                    leftIndexFinger3StartPos = new Vector3(0, leftIndexFinger3Position.y - leftIndexFinger2Position.y, 0);
                    leftIndexFinger2StartPos = new Vector3(0, leftIndexFinger2Position.y - leftIndexFinger1Position.y, 0);

                    leftMidFinger3StartPos = new Vector3(leftMiddleFinger3Position.x - leftMiddleFinger2Position.x, leftMiddleFinger3Position.y - leftMiddleFinger2Position.y, 0);
                    leftMidFinger2StartPos = new Vector3(0, leftMiddleFinger2Position.y - leftMiddleFinger1Position.y, 0);

                    leftRingFinger3StartPos = new Vector3(0, leftRingFinger3Position.y - leftRingFinger2Position.y, 0);
                    leftRingFinger2StartPos = new Vector3(0, leftRingFinger2Position.y - leftRingFinger1Position.y, 0);

                    leftPinkyFinger3StartPos = new Vector3(0, leftPinkyFinger3Position.y - leftPinkyFinger2Position.y, 0);
                    leftPinkyFinger2StartPos = new Vector3(0, leftPinkyFinger2Position.y - leftPinkyFinger1Position.y, 0);

                    leftThumb3StartPos = new Vector3(leftThumb3Position.x - leftThumb2Position.x, leftThumb3Position.y - leftThumb2Position.y, 0);

                    leftIndexFinger1StartPos = new Vector3(leftIndexFinger1Position.x - leftHandPosition.x, leftIndexFinger1Position.y - leftHandPosition.y, 0);

                    leftThumb2BoneStartRotation = leftThumb2Bone.localRotation;

                    firstLeftHandLandmark = false;
                }

                /// <summary>
                /// Rtating the wrist
                /// </summary>
                //interval where palm should be facing the camera
                if (leftHandToHeadCoordinateDifference.x < HandXCoordinatesDiffIntervalToFaceTheCamera.Item2 && leftHandToHeadCoordinateDifference.x > HandXCoordinatesDiffIntervalToFaceTheCamera.Item1)
                {
                    var poseLandmarks = resultPoseLandmarker.poseWorldLandmarks[0].landmarks;
                    var mediapipeLeftHandPosition = poseLandmarks[15];
                    var mediapipeLeftElbowPosition = poseLandmarks[13];

                    var newElbowRotation = rotationSolver.findElbowRotation(mediapipeLeftHandPosition, mediapipeLeftElbowPosition, leftHandStartPos);

                    var widthDiffHandToElbow = mediapipeLeftHandPosition.x - mediapipeLeftElbowPosition.x;

                    if (mediapipeLeftElbowPosition.presence >= 0.4 && !float.IsNaN(newElbowRotation) && newElbowRotation >= 40 && widthDiffHandToElbow <= 0.08)
                    {
                        leftHandTargetRotation = startLeftHandRotation * leftHandRotationOffset;
                        ik.solver.leftHandEffector.rotation *= Quaternion.Euler(0, 0, newElbowRotation + leftHandTargetRotation.z - ik.solver.leftHandEffector.rotation.eulerAngles.z);
                        ik.solver.leftArmChain.bendConstraint.bendGoal.localPosition = new Vector3(-0.5f, 0.5f + newElbowRotation / 100, 0);

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
                        float newWristAngle = rotationSolver.findXRotation(leftIndexFinger1Position, leftHandPosition, leftIndexFinger1StartPos);
                        ik.solver.leftHandEffector.rotation *= Quaternion.Euler(-newWristAngle, 0, 0);//hello

                        /**if (leftIndexFinger1Position.y - leftIndexFinger1StartPos.y <= 0.005f)
                        {
                            newWristAngle = rotationSolver.findWristYRotation(leftIndexFinger1Position, leftHandPosition, leftIndexFinger1StartPos);
                            if (!float.IsNaN(newWristAngle))
                            {
                                ik.solver.leftHandEffector.rotation *= Quaternion.Euler(0, newWristAngle, 0);
                            }
                        }*/


                        if (leftHandGesture == "Thumb_Up")
                        {
                            ik.solver.leftHandEffector.rotation = leftHandTargetRotation * Quaternion.Euler(-90, -80, -80);
                            ik.solver.leftHandEffector.rotationWeight = 0.8f;
                            ik.solver.leftArmChain.bendConstraint.bendGoal.localPosition = new Vector3(-1.5f, 0.5f, 0);
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
                        else
                        {
                            ik.solver.leftArmChain.bendConstraint.bendGoal.localPosition = new Vector3(-0.5f, 0.5f, 0);

                            /// <summary>
                            /// Middle Finger
                            /// </summary>
                            float newAngle = rotationSolver.findTopDownRotation(leftMiddleFinger3Position, leftMiddleFinger2Position, leftMidFinger3StartPos);
                            rotationSolver.setFingertipRotation(newAngle, leftMidFinger3Bone, leftMidFinger2Bone);


                            newAngle = rotationSolver.findTopDownRotation(leftMiddleFinger2Position, leftMiddleFinger1Position, leftMidFinger2StartPos);
                            rotationSolver.setBaseOfTheFingerRotation(newAngle, leftMidFinger1Bone);

                            /// <summary>
                            /// Index Finger
                            /// </summary>
                            newAngle = rotationSolver.findTopDownRotation(leftIndexFinger3Position, leftIndexFinger2Position, leftIndexFinger3StartPos);
                            rotationSolver.setFingertipRotation(newAngle, leftIndexFinger3Bone, leftIndexFinger2Bone);

                            newAngle = rotationSolver.findTopDownRotation(leftIndexFinger2Position, leftIndexFinger1Position, leftIndexFinger2StartPos);
                            rotationSolver.setBaseOfTheFingerRotation(newAngle, leftIndexFinger1Bone);


                            /// <summary>
                            /// Ring Finger
                            /// </summary>
                            newAngle = rotationSolver.findTopDownRotation(leftRingFinger3Position, leftRingFinger2Position, leftRingFinger3StartPos);
                            rotationSolver.setFingertipRotation(newAngle, leftRingFinger3Bone, leftRingFinger2Bone);

                            newAngle = rotationSolver.findTopDownRotation(leftRingFinger2Position, leftRingFinger1Position, leftRingFinger2StartPos);
                            rotationSolver.setBaseOfTheFingerRotation(newAngle, leftRingFinger1Bone);

                            /// <summary>
                            /// Pinky
                            /// </summary>
                            newAngle = rotationSolver.findTopDownRotation(leftPinkyFinger3Position, leftPinkyFinger2Position, leftPinkyFinger3StartPos);
                            rotationSolver.setFingertipRotation(newAngle, leftPinkyFinger3Bone, leftPinkyFinger2Bone);

                            newAngle = rotationSolver.findTopDownRotation(leftPinkyFinger2Position, leftPinkyFinger1Position, leftPinkyFinger2StartPos);
                            rotationSolver.setBaseOfTheFingerRotation(newAngle, leftPinkyFinger1Bone);

                            /// <summary>
                            /// Thumb
                            /// </summary>
                            float newAngleThumb = rotationSolver.findXRotation(leftThumb3Position, leftThumb2Position, leftThumb3StartPos);
                            leftThumb2Bone.localRotation = leftThumb2BoneStartRotation * Quaternion.Euler(-newAngleThumb, 0, 0);
                        }

                    }

                }


            }
        }

        public void solveRightHand(HandLandmarkerResult resultHandLandmarker, GestureRecognizerResult resultGestureRecognizer, PoseLandmarkerResult resultPoseLandmarker)
        {
            var rightHandResultIndex = resultHandLandmarker.handedness.IndexOf(resultHandLandmarker.handedness.Find(x => x.categories[0].categoryName == "Right"));

            var rightHandInTheGesturesList = resultGestureRecognizer.handedness.IndexOf(resultGestureRecognizer.handedness.Find(x => x.categories[0].categoryName == "Right"));
            String rightHandGesture = "None";
            if (rightHandInTheGesturesList != -1)
            {
                rightHandGesture = resultGestureRecognizer.gestures[rightHandInTheGesturesList].categories[0].categoryName;
                //Debug.Log("what gesture RIGHT Hand: " + resultGestureRecognizer.gestures[rightHandInTheGesturesList].categories[0].categoryName);
            }

            if (rightHandResultIndex >= 0)
            {
                var rightHandLandmarks = resultHandLandmarker.handWorldLandmarks[rightHandResultIndex].landmarks;

                var rightMiddleFinger3Position = rightHandLandmarks[11];
                var rightMiddleFinger2Position = rightHandLandmarks[10];
                var rightMiddleFinger1Position = rightHandLandmarks[9];

                var rightIndexFinger3Position = rightHandLandmarks[7];
                var rightIndexFinger2Position = rightHandLandmarks[6];
                var rightIndexFinger1Position = rightHandLandmarks[5];

                var rightRingFinger3Position = rightHandLandmarks[15];
                var rightRingFinger2Position = rightHandLandmarks[14];
                var rightRingFinger1Position = rightHandLandmarks[13];

                var rightPinkyFinger3Position = rightHandLandmarks[19];
                var rightPinkyFinger2Position = rightHandLandmarks[18];
                var rightPinkyFinger1Position = rightHandLandmarks[17];

                var rightThumb3Position = rightHandLandmarks[3];
                var rightThumb2Position = rightHandLandmarks[2];
                var rightThumb1Position = rightHandLandmarks[1];

                var rightHandPosition = rightHandLandmarks[0];


                Transform rightMidFinger3Bone = transform.Find(rightMidFinger3Name);
                Transform rightMidFinger2Bone = transform.Find(rightMidFinger2Name);
                Transform rightMidFinger1Bone = transform.Find(rightMidFinger1Name);

                Transform rightIndexFinger1Bone = transform.Find(rightIndexFinger1Name);
                Transform rightIndexFinger2Bone = transform.Find(rightIndexFinger2Name);
                Transform rightIndexFinger3Bone = transform.Find(rightIndexFinger3Name);

                Transform rightRingFinger1Bone = transform.Find(rightRingFinger1Name);
                Transform rightRingFinger2Bone = transform.Find(rightRingFinger2Name);
                Transform rightRingFinger3Bone = transform.Find(rightRingFinger3Name);

                Transform rightPinkyFinger1Bone = transform.Find(rightPinkyFinger1Name);
                Transform rightPinkyFinger2Bone = transform.Find(rightPinkyFinger2Name);
                Transform rightPinkyFinger3Bone = transform.Find(rightPinkyFinger3Name);

                Transform rightThumb1Bone = transform.Find(rightThumb1Name);
                Transform rightThumb2Bone = transform.Find(rightThumb2Name);
                Transform rightThumb3Bone = transform.Find(rightThumb3Name);

                if (firstRightHandLandmark)
                {
                    rightIndexFinger3StartPos = new Vector3(0, rightIndexFinger3Position.y - rightIndexFinger2Position.y, 0);
                    rightIndexFinger2StartPos = new Vector3(0, rightIndexFinger2Position.y - rightIndexFinger1Position.y, 0);

                    rightMidFinger3StartPos = new Vector3(rightMiddleFinger3Position.x - rightMiddleFinger2Position.x, rightMiddleFinger3Position.y - rightMiddleFinger2Position.y, 0);
                    rightMidFinger2StartPos = new Vector3(0, rightMiddleFinger2Position.y - rightMiddleFinger1Position.y, 0);

                    rightRingFinger3StartPos = new Vector3(0, rightRingFinger3Position.y - rightRingFinger2Position.y, 0);
                    rightRingFinger2StartPos = new Vector3(0, rightRingFinger2Position.y - rightRingFinger1Position.y, 0);

                    rightPinkyFinger3StartPos = new Vector3(0, rightPinkyFinger3Position.y - rightPinkyFinger2Position.y, 0);
                    rightPinkyFinger2StartPos = new Vector3(0, rightPinkyFinger2Position.y - rightPinkyFinger1Position.y, 0);

                    rightThumb3StartPos = new Vector3(rightThumb3Position.x - rightThumb2Position.x, rightThumb3Position.y - rightThumb2Position.y, 0);

                    rightIndexFinger1StartPos = new Vector3(rightIndexFinger1Position.x - rightHandPosition.x, rightIndexFinger1Position.y - rightHandPosition.y, 0);

                    firstRightHandLandmark = false;
                }

                /// <summary>
                /// Rotation the wrist
                /// </summary>
                //interval where palm should be facing the camera
                if (rightHandToHeadCoordinateDifference.x > -HandXCoordinatesDiffIntervalToFaceTheCamera.Item2 && rightHandToHeadCoordinateDifference.x < -HandXCoordinatesDiffIntervalToFaceTheCamera.Item1)
                {
                    var poseLandmarks = resultPoseLandmarker.poseWorldLandmarks[0].landmarks;
                    var mediapipeRightHandPosition = poseLandmarks[16];
                    var mediapipeRightElbowPosition = poseLandmarks[14];

                    var newElbowRotation = rotationSolver.findElbowRotation(mediapipeRightHandPosition, mediapipeRightElbowPosition, rightHandStartPos);

                    var widthDiffHandToElbow = mediapipeRightHandPosition.x - mediapipeRightElbowPosition.x;

                    if (mediapipeRightElbowPosition.presence >= 0.4 && !float.IsNaN(newElbowRotation) && newElbowRotation >= 50 && widthDiffHandToElbow >= -0.08)
                    {
                        rightHandTargetRotation = startRightHandRotation * rightHandRotationOffset;
                        ik.solver.rightHandEffector.rotation *= Quaternion.Euler(0, 0, -newElbowRotation + rightHandTargetRotation.z - ik.solver.rightHandEffector.rotation.eulerAngles.z);

                        ik.solver.rightArmChain.bendConstraint.bendGoal.localPosition = new Vector3(0.5f, 0.5f + newElbowRotation / 100, 0);

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
                        float newWristAngle = rotationSolver.findXRotation(rightIndexFinger1Position, rightHandPosition, rightIndexFinger1StartPos);
                        ik.solver.rightHandEffector.rotation *= Quaternion.Euler(newWristAngle, 0, 0);//hello

                        /**newWristAngle = rotationSolver.findWristYRotation(rightIndexFinger1Position, rightHandPosition, rightIndexFinger1StartPos);
                        if (rightIndexFinger1Position.y - rightIndexFinger1StartPos.y <= 0.005f)
                        {
                            if (!float.IsNaN(newWristAngle))
                            {
                                ik.solver.rightHandEffector.rotation *= Quaternion.Euler(0, -newWristAngle, 0);
                            }
                        }*/

                        if (rightHandGesture == "Thumb_Up")
                        {
                            ik.solver.rightHandEffector.rotation = rightHandTargetRotation * Quaternion.Euler(-90, -80, -80);
                            ik.solver.rightHandEffector.rotationWeight = 0.8f;
                            ik.solver.rightArmChain.bendConstraint.bendGoal.localPosition = new Vector3(1.5f, 1f, 0);
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
                        else
                        {
                            ik.solver.rightArmChain.bendConstraint.bendGoal.localPosition = new Vector3(0.5f, 0.5f, 0);

                            /// <summary>
                            /// Middle Finger
                            /// </summary>
                            float newAngle = rotationSolver.findTopDownRotation(rightMiddleFinger3Position, rightMiddleFinger2Position, rightMidFinger3StartPos);
                            rotationSolver.setFingertipRotation(-newAngle, rightMidFinger3Bone, rightMidFinger2Bone);

                            newAngle = rotationSolver.findTopDownRotation(rightMiddleFinger2Position, rightMiddleFinger1Position, rightMidFinger2StartPos);
                            rotationSolver.setBaseOfTheFingerRotation(-newAngle, rightMidFinger1Bone);

                            /// <summary>
                            /// Index Finger
                            /// </summary>
                            newAngle = rotationSolver.findTopDownRotation(rightIndexFinger3Position, rightIndexFinger2Position, rightIndexFinger3StartPos);
                            rotationSolver.setFingertipRotation(-newAngle, rightIndexFinger3Bone, rightIndexFinger2Bone);

                            newAngle = rotationSolver.findTopDownRotation(rightIndexFinger2Position, rightIndexFinger1Position, rightIndexFinger2StartPos);
                            rotationSolver.setBaseOfTheFingerRotation(-newAngle, rightIndexFinger1Bone);


                            /// <summary>
                            /// Ring Finger
                            /// </summary>
                            newAngle = rotationSolver.findTopDownRotation(rightRingFinger3Position, rightRingFinger2Position, rightRingFinger3StartPos);
                            rotationSolver.setFingertipRotation(-newAngle, rightRingFinger3Bone, rightRingFinger2Bone);

                            newAngle = rotationSolver.findTopDownRotation(rightRingFinger2Position, rightRingFinger1Position, rightRingFinger2StartPos);
                            rotationSolver.setBaseOfTheFingerRotation(-newAngle, rightRingFinger1Bone);

                            /// <summary>
                            /// Pinky
                            /// </summary>
                            newAngle = rotationSolver.findTopDownRotation(rightPinkyFinger3Position, rightPinkyFinger2Position, rightPinkyFinger3StartPos);
                            rotationSolver.setFingertipRotation(-newAngle, rightPinkyFinger3Bone, rightPinkyFinger2Bone);

                            newAngle = rotationSolver.findTopDownRotation(rightPinkyFinger2Position, rightPinkyFinger1Position, rightPinkyFinger2StartPos);
                            rotationSolver.setBaseOfTheFingerRotation(-newAngle, rightPinkyFinger1Bone);

                            /// <summary>
                            /// Thumb
                            /// </summary>
                            float newAngleThumb = rotationSolver.findXRotation(rightThumb3Position, rightThumb2Position, rightThumb3StartPos);
                            rightThumb1Bone.localRotation = Quaternion.Euler(50f, -20f, -15f);
                            rightThumb2Bone.localRotation = Quaternion.Euler(-20f, 0, 8f);
                            rightThumb3Bone.localRotation = Quaternion.Euler(0, 0, 5f);
                            rightThumb2Bone.localRotation *= Quaternion.Euler(newAngleThumb, 0, 0);
                            rightThumb1Bone.localRotation *= Quaternion.Euler(newAngleThumb / 4, 0, 0);
                        }

                    }

                }
            }

        }

    }
}
