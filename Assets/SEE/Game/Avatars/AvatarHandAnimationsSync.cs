using UnityEngine;
using Unity.Netcode;
using RootMotion.FinalIK;
using SEE.GO;

namespace SEE.Game.Avatars
{
    public class AvatarHandAnimationsSync : NetworkBehaviour
    {
        private HandsAnimator handsAnimator;
        private FullBodyBipedIK ik;


        private NetworkVariable<Vector3> leftHandPosition = new(writePerm: NetworkVariableWritePermission.Owner);
        private NetworkVariable<Quaternion> leftHandRotation = new(writePerm: NetworkVariableWritePermission.Owner);

        private NetworkVariable<Vector3> leftBendGoalLocalPosition = new(writePerm: NetworkVariableWritePermission.Owner);
        private NetworkVariable<Vector3> rightBendGoalLocalPosition = new(writePerm: NetworkVariableWritePermission.Owner);

        private NetworkVariable<float> leftHandRotationWeight = new(writePerm: NetworkVariableWritePermission.Owner);
        private NetworkVariable<float> rightHandRotationWeight = new(writePerm: NetworkVariableWritePermission.Owner);

        private NetworkVariable<Vector3> leftIndexFingerRotations = new(writePerm: NetworkVariableWritePermission.Owner);
        private NetworkVariable<Vector3> leftMiddleFingerRotations = new(writePerm: NetworkVariableWritePermission.Owner);
        private NetworkVariable<Vector3> leftRingFingerRotations = new(writePerm: NetworkVariableWritePermission.Owner);
        private NetworkVariable<Vector3> leftPinkyFingerRotations = new(writePerm: NetworkVariableWritePermission.Owner);
        private NetworkVariable<Quaternion> leftThumb1Rotations = new(writePerm: NetworkVariableWritePermission.Owner);
        private NetworkVariable<Quaternion> leftThumb2Rotations = new(writePerm: NetworkVariableWritePermission.Owner);
        private NetworkVariable<Quaternion> leftThumb3Rotations = new(writePerm: NetworkVariableWritePermission.Owner); 

        private NetworkVariable<Vector3> rightHandPosition = new(writePerm: NetworkVariableWritePermission.Owner);
        private NetworkVariable<Quaternion> rightHandRotation = new(writePerm: NetworkVariableWritePermission.Owner);

        private NetworkVariable<Vector3> rightIndexFingerRotations = new(writePerm: NetworkVariableWritePermission.Owner);
        private NetworkVariable<Vector3> rightMiddleFingerRotations = new(writePerm: NetworkVariableWritePermission.Owner);
        private NetworkVariable<Vector3> rightRingFingerRotations = new(writePerm: NetworkVariableWritePermission.Owner);
        private NetworkVariable<Vector3> rightPinkyFingerRotations = new(writePerm: NetworkVariableWritePermission.Owner);
        private NetworkVariable<Quaternion> rightThumb1Rotations = new(writePerm: NetworkVariableWritePermission.Owner);
        private NetworkVariable<Quaternion> rightThumb2Rotations = new(writePerm: NetworkVariableWritePermission.Owner);
        private NetworkVariable<Quaternion> rightThumb3Rotations = new(writePerm: NetworkVariableWritePermission.Owner);

        private void Awake()
        {
            BodyAnimator bodyAnimator = GetComponent<BodyAnimator>();
            handsAnimator = bodyAnimator.handsAnimator;
            gameObject.TryGetComponentOrLog(out ik);
        }

        private void LateUpdate()
        {
            if(IsOwner)
            {
                CaptureFromHandsAnimator();
            }
            else
            {
                ApplyHandsAnimation();
            }
        }

        private void CaptureFromHandsAnimator()
        {
            if(handsAnimator == null)
            {
                return;
            }

            leftHandPosition.Value = handsAnimator.leftHandEffectorPosition;
            leftHandRotation.Value = handsAnimator.leftHandEffectorRotation;

            leftBendGoalLocalPosition.Value = handsAnimator.leftBendGoalLocalPosition;
            rightBendGoalLocalPosition.Value = handsAnimator.rightBendGoalLocalPosition;

            leftHandRotationWeight.Value = handsAnimator.lefthandRotationWeight;
            rightHandRotationWeight.Value = handsAnimator.righthandRotationWeight;

            leftIndexFingerRotations.Value = handsAnimator.leftIndexFingerRotations;
            leftMiddleFingerRotations.Value = handsAnimator.leftMiddleFingerRotations;
            leftRingFingerRotations.Value = handsAnimator.leftRingFingerRotations;
            leftPinkyFingerRotations.Value = handsAnimator.leftPinkyFingerRotations;
            leftThumb1Rotations.Value = handsAnimator.leftThumb1Rotations;
            leftThumb2Rotations.Value = handsAnimator.leftThumb2Rotations;
            leftThumb3Rotations.Value = handsAnimator.leftThumb3Rotations; 

            rightHandPosition.Value = handsAnimator.rightHandEffectorPosition;
            rightHandRotation.Value = handsAnimator.rightHandEffectorRotation;

            rightIndexFingerRotations.Value = handsAnimator.rightIndexFingerRotations;
            rightMiddleFingerRotations.Value = handsAnimator.rightMiddleFingerRotations;
            rightRingFingerRotations.Value = handsAnimator.rightRingFingerRotations;
            rightPinkyFingerRotations.Value = handsAnimator.rightPinkyFingerRotations;
            rightThumb1Rotations.Value = handsAnimator.rightThumb1Rotations;
            rightThumb2Rotations.Value = handsAnimator.rightThumb2Rotations;
            rightThumb3Rotations.Value = handsAnimator.rightThumb3Rotations;
        }

        private void ApplyHandsAnimation()
        {
            ik.solver.leftHandEffector.position = leftHandPosition.Value;
            ik.solver.leftHandEffector.rotation = leftHandRotation.Value;
            ik.solver.leftHandEffector.positionWeight = 1f;
            ik.solver.leftHandEffector.rotationWeight = leftHandRotationWeight.Value;

            Transform leftHandBendGoal = handsAnimator.transform.Find("LeftElbowBendGoal");
            leftHandBendGoal.localPosition = leftBendGoalLocalPosition.Value;
            Transform rightHandBendGoal = handsAnimator.transform.Find("RightElbowBendGoal");
            rightHandBendGoal.localPosition = rightBendGoalLocalPosition.Value;

            Transform leftMidFinger3Bone = transform.Find(HandsAnimator.leftMidFinger3Name);
            Transform leftMidFinger2Bone = transform.Find(HandsAnimator.leftMidFinger2Name);
            Transform leftMidFinger1Bone = transform.Find(HandsAnimator.leftMidFinger1Name);
            leftMidFinger1Bone.localRotation = Quaternion.Euler(0,0,leftMiddleFingerRotations.Value.x);
            leftMidFinger2Bone.localRotation = Quaternion.Euler(0,0,leftMiddleFingerRotations.Value.y);
            leftMidFinger3Bone.localRotation = Quaternion.Euler(0,0,leftMiddleFingerRotations.Value.z);

            Transform leftIndexFinger1Bone = transform.Find(HandsAnimator.leftIndexFinger1Name);
            Transform leftIndexFinger2Bone = transform.Find(HandsAnimator.leftIndexFinger2Name);
            Transform leftIndexFinger3Bone = transform.Find(HandsAnimator.leftIndexFinger3Name);
            leftIndexFinger1Bone.localRotation = Quaternion.Euler(0,0, leftIndexFingerRotations.Value.x);
            leftIndexFinger2Bone.localRotation = Quaternion.Euler(0,0, leftIndexFingerRotations.Value.y);
            leftIndexFinger3Bone.localRotation = Quaternion.Euler(0,0, leftIndexFingerRotations.Value.z);

            Transform leftRingFinger1Bone = transform.Find(HandsAnimator.leftRingFinger1Name);
            Transform leftRingFinger2Bone = transform.Find(HandsAnimator.leftRingFinger2Name);
            Transform leftRingFinger3Bone = transform.Find(HandsAnimator.leftRingFinger3Name);
            leftRingFinger1Bone.localRotation = Quaternion.Euler(0, 0, leftRingFingerRotations.Value.x);
            leftRingFinger2Bone.localRotation = Quaternion.Euler(0, 0, leftRingFingerRotations.Value.y);
            leftRingFinger3Bone.localRotation = Quaternion.Euler(0, 0, leftRingFingerRotations.Value.z);

            Transform leftPinkyFinger1Bone = transform.Find(HandsAnimator.leftPinkyFinger1Name);
            Transform leftPinkyFinger2Bone = transform.Find(HandsAnimator.leftPinkyFinger2Name);
            Transform leftPinkyFinger3Bone = transform.Find(HandsAnimator.leftPinkyFinger3Name);
            leftPinkyFinger1Bone.localRotation = Quaternion.Euler(0, 0, leftPinkyFingerRotations.Value.x);
            leftPinkyFinger2Bone.localRotation = Quaternion.Euler(0, 0, leftPinkyFingerRotations.Value.y);
            leftPinkyFinger3Bone.localRotation = Quaternion.Euler(0, 0, leftPinkyFingerRotations.Value.z);

            Transform leftThumb1Bone = handsAnimator.transform.Find(HandsAnimator.leftThumb1Name);
            Transform leftThumb2Bone = handsAnimator.transform.Find(HandsAnimator.leftThumb2Name);
            Transform leftThumb3Bone = handsAnimator.transform.Find(HandsAnimator.leftThumb3Name);
            leftThumb1Bone.localRotation = leftThumb1Rotations.Value;
            leftThumb2Bone.localRotation = leftThumb2Rotations.Value;
            leftThumb3Bone.localRotation = leftThumb3Rotations.Value; 


            ik.solver.rightHandEffector.position = rightHandPosition.Value;
            ik.solver.rightHandEffector.rotation = rightHandRotation.Value;
            ik.solver.rightHandEffector.positionWeight = 1f;
            ik.solver.rightHandEffector.rotationWeight = rightHandRotationWeight.Value;

            Transform rightMidFinger3Bone = transform.Find(HandsAnimator.rightMidFinger3Name);
            Transform rightMidFinger2Bone = transform.Find(HandsAnimator.rightMidFinger2Name);
            Transform rightMidFinger1Bone = transform.Find(HandsAnimator.rightMidFinger1Name);
            rightMidFinger1Bone.localRotation = Quaternion.Euler(0,0,rightMiddleFingerRotations.Value.x);
            rightMidFinger2Bone.localRotation = Quaternion.Euler(0,0,rightMiddleFingerRotations.Value.y);
            rightMidFinger3Bone.localRotation = Quaternion.Euler(0,0,rightMiddleFingerRotations.Value.z);

            Transform rightIndexFinger1Bone = transform.Find(HandsAnimator.rightIndexFinger1Name);
            Transform rightIndexFinger2Bone = transform.Find(HandsAnimator.rightIndexFinger2Name);
            Transform rightIndexFinger3Bone = transform.Find(HandsAnimator.rightIndexFinger3Name);
            rightIndexFinger1Bone.localRotation = Quaternion.Euler(0,0, rightIndexFingerRotations.Value.x);
            rightIndexFinger2Bone.localRotation = Quaternion.Euler(0,0, rightIndexFingerRotations.Value.y);
            rightIndexFinger3Bone.localRotation = Quaternion.Euler(0,0, rightIndexFingerRotations.Value.z);

            Transform rightRingFinger1Bone = transform.Find(HandsAnimator.rightRingFinger1Name);
            Transform rightRingFinger2Bone = transform.Find(HandsAnimator.rightRingFinger2Name);
            Transform rightRingFinger3Bone = transform.Find(HandsAnimator.rightRingFinger3Name);
            rightRingFinger1Bone.localRotation = Quaternion.Euler(0, 0, rightRingFingerRotations.Value.x);
            rightRingFinger2Bone.localRotation = Quaternion.Euler(0, 0, rightRingFingerRotations.Value.y);
            rightRingFinger3Bone.localRotation = Quaternion.Euler(0, 0, rightRingFingerRotations.Value.z);

            Transform rightPinkyFinger1Bone = transform.Find(HandsAnimator.rightPinkyFinger1Name);
            Transform rightPinkyFinger2Bone = transform.Find(HandsAnimator.rightPinkyFinger2Name);
            Transform rightPinkyFinger3Bone = transform.Find(HandsAnimator.rightPinkyFinger3Name);
            rightPinkyFinger1Bone.localRotation = Quaternion.Euler(0, 0, rightPinkyFingerRotations.Value.x);
            rightPinkyFinger2Bone.localRotation = Quaternion.Euler(0, 0, rightPinkyFingerRotations.Value.y);
            rightPinkyFinger3Bone.localRotation = Quaternion.Euler(0, 0, rightPinkyFingerRotations.Value.z);

            Transform rightThumb1Bone = transform.Find(HandsAnimator.rightThumb1Name);
            Transform rightThumb2Bone = transform.Find(HandsAnimator.rightThumb2Name);
            Transform rightThumb3Bone = transform.Find(HandsAnimator.rightThumb3Name);
            rightThumb1Bone.localRotation = rightThumb1Rotations.Value;
            rightThumb2Bone.localRotation = rightThumb2Rotations.Value;
            rightThumb3Bone.localRotation = rightThumb3Rotations.Value; 
        }

    }
}