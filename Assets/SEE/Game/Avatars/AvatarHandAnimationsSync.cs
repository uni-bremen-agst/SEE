using UnityEngine;
using Unity.Netcode;
using RootMotion.FinalIK;
using SEE.GO;

namespace SEE.Game.Avatars
{
    /// <summary>
    /// Synchronizes hand and finger animations between players.
    /// </summary>
    internal class AvatarHandAnimationsSync : NetworkBehaviour
    {
        /// <summary>
        /// Hands Animator used by this avatar for animations.
        /// </summary>
        private HandsAnimator handsAnimator;

        /// <summary>
        /// The FullBodyBiped IK solver attached to the avatar.
        /// </summary>
        private FullBodyBipedIK ik;

        /// <summary>
        /// Position of the left hand.
        /// </summary>
        private readonly NetworkVariable<Vector3> leftHandPosition = new(writePerm: NetworkVariableWritePermission.Owner);

        /// <summary>
        /// Rotation of the left hand.
        /// </summary>
        private readonly NetworkVariable<Quaternion> leftHandRotation = new(writePerm: NetworkVariableWritePermission.Owner);

        /// <summary>
        /// Local position of the left bend goal for the elbow (relative to the main trasform).
        /// </summary>
        private readonly NetworkVariable<Vector3> leftBendGoalLocalPosition = new(writePerm: NetworkVariableWritePermission.Owner);

        /// <summary>
        /// Local position of the right bend goal for the elbow (relative to the main trasform).
        /// </summary>
        private readonly NetworkVariable<Vector3> rightBendGoalLocalPosition = new(writePerm: NetworkVariableWritePermission.Owner);

        /// <summary>
        /// The value for the weight of the left hand, that determines the level of influence of changes in the IK effector of the left hand on other bones in the chain.
        /// </summary>
        private readonly NetworkVariable<float> leftHandRotationWeight = new(writePerm: NetworkVariableWritePermission.Owner);

        /// <summary>
        /// The value for the weight of the right hand, that determines the level of influence of changes in the IK effector of the right hand on other bones in the chain.
        /// </summary>
        private readonly NetworkVariable<float> rightHandRotationWeight = new(writePerm: NetworkVariableWritePermission.Owner);

        /// <summary>
        /// Rotations of the joints of the index finger of the left hand.
        /// </summary>
        private readonly NetworkVariable<Vector3> leftIndexFingerRotations = new(writePerm: NetworkVariableWritePermission.Owner);

        /// <summary>
        /// Rotations of the joints of the middle finger of the left hand.
        /// </summary>
        private readonly NetworkVariable<Vector3> leftMiddleFingerRotations = new(writePerm: NetworkVariableWritePermission.Owner);

        /// <summary>
        /// Rotations of the joints of the ring finger of the left hand.
        /// </summary>
        private readonly NetworkVariable<Vector3> leftRingFingerRotations = new(writePerm: NetworkVariableWritePermission.Owner);

        /// <summary>
        /// Rotations of the joints of the little finger of the left hand.
        /// </summary>
        private readonly NetworkVariable<Vector3> leftPinkyFingerRotations = new(writePerm: NetworkVariableWritePermission.Owner);

        /// <summary>
        /// Rotations of the thumb bone of the left hand, which lies at the base of the thumb.
        /// </summary>
        private readonly NetworkVariable<Quaternion> leftThumb1Rotations = new(writePerm: NetworkVariableWritePermission.Owner);

        /// <summary>
        /// Rotations of the middle bone of the thumb of the left hand.
        /// </summary>
        private readonly NetworkVariable<Quaternion> leftThumb2Rotations = new(writePerm: NetworkVariableWritePermission.Owner);

        /// <summary>
        /// Rotations of the fingertip bone of the thumb of the left hand.
        /// </summary>
        private readonly NetworkVariable<Quaternion> leftThumb3Rotations = new(writePerm: NetworkVariableWritePermission.Owner);

        /// <summary>
        /// Position of the right hand.
        /// </summary>
        private readonly NetworkVariable<Vector3> rightHandPosition = new(writePerm: NetworkVariableWritePermission.Owner);

        /// <summary>
        /// Rotation of the right hand.
        /// </summary>
        private readonly NetworkVariable<Quaternion> rightHandRotation = new(writePerm: NetworkVariableWritePermission.Owner);

        /// <summary>
        /// Rotations of the joints of the index finger of the right hand.
        /// </summary>
        private readonly NetworkVariable<Vector3> rightIndexFingerRotations = new(writePerm: NetworkVariableWritePermission.Owner);

        /// <summary>
        /// Rotations of the joints of the middle finger of the right hand.
        /// </summary>
        private readonly NetworkVariable<Vector3> rightMiddleFingerRotations = new(writePerm: NetworkVariableWritePermission.Owner);

        /// <summary>
        /// Rotations of the joints of the ring finger of the right hand.
        /// </summary>
        private readonly NetworkVariable<Vector3> rightRingFingerRotations = new(writePerm: NetworkVariableWritePermission.Owner);

        /// <summary>
        /// Rotations of the joints of the little finger of the right hand.
        /// </summary>
        private readonly NetworkVariable<Vector3> rightPinkyFingerRotations = new(writePerm: NetworkVariableWritePermission.Owner);

        /// <summary>
        /// Rotations of the thumb bone of the right hand, which lies at the base of the thumb.
        /// </summary>
        private readonly NetworkVariable<Quaternion> rightThumb1Rotations = new(writePerm: NetworkVariableWritePermission.Owner);

        /// <summary>
        /// Rotations of the middle bone of the thumb of the right hand.
        /// </summary>
        private readonly NetworkVariable<Quaternion> rightThumb2Rotations = new(writePerm: NetworkVariableWritePermission.Owner);

        /// <summary>
        /// Rotations of the fingertip bone of the thumb of the right hand.
        /// </summary>
        private readonly NetworkVariable<Quaternion> rightThumb3Rotations = new(writePerm: NetworkVariableWritePermission.Owner);

        /// <summary>
        /// Initializes the BodyAnimator, HandsAnimator, and FullBodyBipedIK components that this avatar uses.
        /// </summary>
        private void Awake()
        {
            BodyAnimator bodyAnimator = GetComponent<BodyAnimator>();
            handsAnimator = bodyAnimator.HandsAnimator;
            gameObject.TryGetComponentOrLog(out ik);
        }

        /// <summary>
        /// If the player is the owner of the avatar, save animation values from the handsAnimator.
        /// If not, use the values saved by the owner, to animate the avatar locally.
        /// </summary>
        private void LateUpdate()
        {
            if (IsOwner)
            {
                CaptureFromHandsAnimator();
            }
            else
            {
                ApplyHandsAnimation();
            }
        }

        /// <summary>
        /// Stores the values ​​for animation in NetworkVariables from those stored in handsAnimator.
        /// </summary>
        private void CaptureFromHandsAnimator()
        {
            if (handsAnimator == null)
            {
                return;
            }

            leftHandPosition.Value = handsAnimator.LeftHandTransformState.HandIKEffectorPosition;
            leftHandRotation.Value = handsAnimator.LeftHandTransformState.HandIKEffectorRotation;

            leftBendGoalLocalPosition.Value = handsAnimator.LeftHandTransformState.BendGoalLocalPosition;
            rightBendGoalLocalPosition.Value = handsAnimator.RightHandTransformState.BendGoalLocalPosition;

            leftHandRotationWeight.Value = handsAnimator.LeftHandTransformState.HandIKRotationWeight;
            rightHandRotationWeight.Value = handsAnimator.RightHandTransformState.HandIKRotationWeight;

            leftIndexFingerRotations.Value = handsAnimator.LeftHandTransformState.IndexFingerRotations;
            leftMiddleFingerRotations.Value = handsAnimator.LeftHandTransformState.MiddleFingerRotations;
            leftRingFingerRotations.Value = handsAnimator.LeftHandTransformState.RingFingerRotations;
            leftPinkyFingerRotations.Value = handsAnimator.LeftHandTransformState.PinkyFingerRotations;
            leftThumb1Rotations.Value = handsAnimator.LeftHandTransformState.Thumb1Rotations;
            leftThumb2Rotations.Value = handsAnimator.LeftHandTransformState.Thumb2Rotations;
            leftThumb3Rotations.Value = handsAnimator.LeftHandTransformState.Thumb3Rotations;

            rightHandPosition.Value = handsAnimator.RightHandTransformState.HandIKEffectorPosition;
            rightHandRotation.Value = handsAnimator.RightHandTransformState.HandIKEffectorRotation;

            rightIndexFingerRotations.Value = handsAnimator.RightHandTransformState.IndexFingerRotations;
            rightMiddleFingerRotations.Value = handsAnimator.RightHandTransformState.MiddleFingerRotations;
            rightRingFingerRotations.Value = handsAnimator.RightHandTransformState.RingFingerRotations;
            rightPinkyFingerRotations.Value = handsAnimator.RightHandTransformState.PinkyFingerRotations;
            rightThumb1Rotations.Value = handsAnimator.RightHandTransformState.Thumb1Rotations;
            rightThumb2Rotations.Value = handsAnimator.RightHandTransformState.Thumb2Rotations;
            rightThumb3Rotations.Value = handsAnimator.RightHandTransformState.Thumb3Rotations;
        }

        /// <summary>
        /// Applies the values ​​stored in NetworkVariables to locally animate the avatar.
        /// </summary>
        private void ApplyHandsAnimation()
        {
            ik.solver.leftHandEffector.position = leftHandPosition.Value;
            ik.solver.leftHandEffector.rotation = leftHandRotation.Value;
            ik.solver.leftHandEffector.positionWeight = 1f;
            ik.solver.leftHandEffector.rotationWeight = leftHandRotationWeight.Value;

            Transform leftHandBendGoal = transform.Find("LeftElbowBendGoal");
            leftHandBendGoal.localPosition = leftBendGoalLocalPosition.Value;
            Transform rightHandBendGoal = transform.Find("RightElbowBendGoal");
            rightHandBendGoal.localPosition = rightBendGoalLocalPosition.Value;

            Transform leftMidFinger3Bone = transform.Find(HandsAnimator.LeftMidFinger3Name);
            Transform leftMidFinger2Bone = transform.Find(HandsAnimator.LeftMidFinger2Name);
            Transform leftMidFinger1Bone = transform.Find(HandsAnimator.LeftMidFinger1Name);
            leftMidFinger1Bone.localRotation = Quaternion.Euler(0,0,leftMiddleFingerRotations.Value.x);
            leftMidFinger2Bone.localRotation = Quaternion.Euler(0,0,leftMiddleFingerRotations.Value.y);
            leftMidFinger3Bone.localRotation = Quaternion.Euler(0,0,leftMiddleFingerRotations.Value.z);

            Transform leftIndexFinger1Bone = transform.Find(HandsAnimator.LeftIndexFinger1Name);
            Transform leftIndexFinger2Bone = transform.Find(HandsAnimator.LeftIndexFinger2Name);
            Transform leftIndexFinger3Bone = transform.Find(HandsAnimator.LeftIndexFinger3Name);
            leftIndexFinger1Bone.localRotation = Quaternion.Euler(0,0, leftIndexFingerRotations.Value.x);
            leftIndexFinger2Bone.localRotation = Quaternion.Euler(0,0, leftIndexFingerRotations.Value.y);
            leftIndexFinger3Bone.localRotation = Quaternion.Euler(0,0, leftIndexFingerRotations.Value.z);

            Transform leftRingFinger1Bone = transform.Find(HandsAnimator.LeftRingFinger1Name);
            Transform leftRingFinger2Bone = transform.Find(HandsAnimator.LeftRingFinger2Name);
            Transform leftRingFinger3Bone = transform.Find(HandsAnimator.LeftRingFinger3Name);
            leftRingFinger1Bone.localRotation = Quaternion.Euler(0, 0, leftRingFingerRotations.Value.x);
            leftRingFinger2Bone.localRotation = Quaternion.Euler(0, 0, leftRingFingerRotations.Value.y);
            leftRingFinger3Bone.localRotation = Quaternion.Euler(0, 0, leftRingFingerRotations.Value.z);

            Transform leftPinkyFinger1Bone = transform.Find(HandsAnimator.LeftPinkyFinger1Name);
            Transform leftPinkyFinger2Bone = transform.Find(HandsAnimator.LeftPinkyFinger2Name);
            Transform leftPinkyFinger3Bone = transform.Find(HandsAnimator.LeftPinkyFinger3Name);
            leftPinkyFinger1Bone.localRotation = Quaternion.Euler(0, 0, leftPinkyFingerRotations.Value.x);
            leftPinkyFinger2Bone.localRotation = Quaternion.Euler(0, 0, leftPinkyFingerRotations.Value.y);
            leftPinkyFinger3Bone.localRotation = Quaternion.Euler(0, 0, leftPinkyFingerRotations.Value.z);

            Transform leftThumb1Bone = transform.Find(HandsAnimator.LeftThumb1Name);
            Transform leftThumb2Bone = transform.Find(HandsAnimator.LeftThumb2Name);
            Transform leftThumb3Bone = transform.Find(HandsAnimator.LeftThumb3Name);
            leftThumb1Bone.localRotation = leftThumb1Rotations.Value;
            leftThumb2Bone.localRotation = leftThumb2Rotations.Value;
            leftThumb3Bone.localRotation = leftThumb3Rotations.Value;

            ik.solver.rightHandEffector.position = rightHandPosition.Value;
            ik.solver.rightHandEffector.rotation = rightHandRotation.Value;
            ik.solver.rightHandEffector.positionWeight = 1f;
            ik.solver.rightHandEffector.rotationWeight = rightHandRotationWeight.Value;

            Transform rightMidFinger3Bone = transform.Find(HandsAnimator.RightMidFinger3Name);
            Transform rightMidFinger2Bone = transform.Find(HandsAnimator.RightMidFinger2Name);
            Transform rightMidFinger1Bone = transform.Find(HandsAnimator.RightMidFinger1Name);
            rightMidFinger1Bone.localRotation = Quaternion.Euler(0,0,rightMiddleFingerRotations.Value.x);
            rightMidFinger2Bone.localRotation = Quaternion.Euler(0,0,rightMiddleFingerRotations.Value.y);
            rightMidFinger3Bone.localRotation = Quaternion.Euler(0,0,rightMiddleFingerRotations.Value.z);

            Transform rightIndexFinger1Bone = transform.Find(HandsAnimator.RightIndexFinger1Name);
            Transform rightIndexFinger2Bone = transform.Find(HandsAnimator.RightIndexFinger2Name);
            Transform rightIndexFinger3Bone = transform.Find(HandsAnimator.RightIndexFinger3Name);
            rightIndexFinger1Bone.localRotation = Quaternion.Euler(0,0, rightIndexFingerRotations.Value.x);
            rightIndexFinger2Bone.localRotation = Quaternion.Euler(0,0, rightIndexFingerRotations.Value.y);
            rightIndexFinger3Bone.localRotation = Quaternion.Euler(0,0, rightIndexFingerRotations.Value.z);

            Transform rightRingFinger1Bone = transform.Find(HandsAnimator.RightRingFinger1Name);
            Transform rightRingFinger2Bone = transform.Find(HandsAnimator.RightRingFinger2Name);
            Transform rightRingFinger3Bone = transform.Find(HandsAnimator.RightRingFinger3Name);
            rightRingFinger1Bone.localRotation = Quaternion.Euler(0, 0, rightRingFingerRotations.Value.x);
            rightRingFinger2Bone.localRotation = Quaternion.Euler(0, 0, rightRingFingerRotations.Value.y);
            rightRingFinger3Bone.localRotation = Quaternion.Euler(0, 0, rightRingFingerRotations.Value.z);

            Transform rightPinkyFinger1Bone = transform.Find(HandsAnimator.RightPinkyFinger1Name);
            Transform rightPinkyFinger2Bone = transform.Find(HandsAnimator.RightPinkyFinger2Name);
            Transform rightPinkyFinger3Bone = transform.Find(HandsAnimator.RightPinkyFinger3Name);
            rightPinkyFinger1Bone.localRotation = Quaternion.Euler(0, 0, rightPinkyFingerRotations.Value.x);
            rightPinkyFinger2Bone.localRotation = Quaternion.Euler(0, 0, rightPinkyFingerRotations.Value.y);
            rightPinkyFinger3Bone.localRotation = Quaternion.Euler(0, 0, rightPinkyFingerRotations.Value.z);

            Transform rightThumb1Bone = transform.Find(HandsAnimator.RightThumb1Name);
            Transform rightThumb2Bone = transform.Find(HandsAnimator.RightThumb2Name);
            Transform rightThumb3Bone = transform.Find(HandsAnimator.RightThumb3Name);
            rightThumb1Bone.localRotation = rightThumb1Rotations.Value;
            rightThumb2Bone.localRotation = rightThumb2Rotations.Value;
            rightThumb3Bone.localRotation = rightThumb3Rotations.Value;
        }
    }
}
