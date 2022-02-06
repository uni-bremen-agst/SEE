using UnityEngine;
using System.Collections;

namespace UMA
{
	/// <summary>
	/// Auxillary slot which adds a TwistBone component for the forearms of a newly created character.
	/// </summary>
	public class O3nArmLowerLegTwistSlotScript : MonoBehaviour 
	{
		static int leftHandHash;
		static int rightHandHash;
		static int leftTwistHash;
		static int rightTwistHash;

        static int leftFootHash;
        static int rightFootHash;
        static int leftFootTwistHash;
        static int rightFootTwistHash;

        static int leftUpperArmTwistHash;
        static int rightUpperArmTwistHash;
        static int leftClavicleHash;
        static int rightClavicleHash;

        static int leftThighTwistHash;
        static int rightThighTwistHash;
        static int leftThighHash;
        static int rightThighHash;

        static bool hashesFound = false;

		public void OnDnaApplied(UMAData umaData)
		{
			if (!hashesFound)
			{
				leftHandHash = UMAUtils.StringToHash("hand_L");
				rightHandHash = UMAUtils.StringToHash("hand_R");
				leftTwistHash = UMAUtils.StringToHash("LowerarmAdjustTwist_L");
				rightTwistHash = UMAUtils.StringToHash("LowerarmAdjustTwist_R");
                leftFootHash = UMAUtils.StringToHash("Foot_L");
                rightFootHash = UMAUtils.StringToHash("Foot_R");
                leftFootTwistHash = UMAUtils.StringToHash("CalfAdjustTwist_L");
                rightFootTwistHash = UMAUtils.StringToHash("CalfAdjustTwist_R");

                leftUpperArmTwistHash = UMAUtils.StringToHash("UpperarmAdjustTwist_L");
                rightUpperArmTwistHash = UMAUtils.StringToHash("UpperarmAdjustTwist_R");
                leftClavicleHash = UMAUtils.StringToHash("Upperarm_L");
                rightClavicleHash = UMAUtils.StringToHash("Upperarm_R");

                leftThighTwistHash = UMAUtils.StringToHash("ThighAdjustTwist_L");
                rightThighTwistHash = UMAUtils.StringToHash("ThighAdjustTwist_R");
                leftThighHash = UMAUtils.StringToHash("Thigh_L");
                rightThighHash = UMAUtils.StringToHash("Thigh_R");

                hashesFound = true;
			}

			GameObject leftHand = umaData.GetBoneGameObject(leftHandHash);
			GameObject rightHand = umaData.GetBoneGameObject(rightHandHash);
			GameObject leftTwist = umaData.GetBoneGameObject(leftTwistHash);
			GameObject rightTwist = umaData.GetBoneGameObject(rightTwistHash);

            GameObject leftFoot = umaData.GetBoneGameObject(leftFootHash);
            GameObject rightFoot = umaData.GetBoneGameObject(rightFootHash);
            GameObject leftFootTwist = umaData.GetBoneGameObject(leftFootTwistHash);
            GameObject rightFootTwist = umaData.GetBoneGameObject(rightFootTwistHash);


            GameObject leftUpperArmTwist = umaData.GetBoneGameObject(leftUpperArmTwistHash);
            GameObject rightUpperArmTwist = umaData.GetBoneGameObject(rightUpperArmTwistHash);
            GameObject leftUpperArm = umaData.GetBoneGameObject(leftClavicleHash);
            GameObject rightUpperArm = umaData.GetBoneGameObject(rightClavicleHash);


            GameObject leftThighTwist = umaData.GetBoneGameObject(leftThighTwistHash);
            GameObject rightThighTwist = umaData.GetBoneGameObject(rightThighTwistHash);
            GameObject leftThigh = umaData.GetBoneGameObject(leftThighHash);
            GameObject rightThigh = umaData.GetBoneGameObject(rightThighHash);



            if ((leftHand == null) || (rightHand == null) || (leftTwist == null) || (rightTwist == null)
                || (leftFoot == null) || (rightFoot == null) || (leftFootTwist == null) || (rightFootTwist == null)
                || (leftUpperArm == null) || (rightUpperArm == null) || (leftUpperArmTwist == null) || (rightUpperArmTwist == null)
                || (leftThighTwist == null) || (rightThighTwist == null) || (leftThigh == null) || (rightThigh == null))
			{
				Debug.LogError("Failed to add o3n Twist to: " + umaData.name);
				return;
			}

            O3nTwistBone old = umaData.umaRoot.GetComponent<O3nTwistBone>();
            if (old != null)
            {
                DestroyImmediate(old);
            }

            var twist = umaData.umaRoot.AddComponent<O3nTwistBone>();
            twist.twistValue = 1.0f;
			twist.twistValues = new float[] { 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 0.5f, 0.5f };
			twist.twistBone = new Transform[] {leftTwist.transform, rightTwist.transform, leftFootTwist.transform, rightFootTwist.transform, leftUpperArmTwist.transform, rightUpperArmTwist.transform, leftThighTwist.transform, rightThighTwist.transform};
			twist.refBone = new Transform[] {leftHand.transform, rightHand.transform, leftFoot.transform, rightFoot.transform, leftUpperArm.transform, rightUpperArm.transform, leftThigh.transform, rightThigh.transform};
            twist.originalRefRotation = new Quaternion[] { Quaternion.Euler(-108f,0f,0f), Quaternion.Euler(-72f, 0f, 0f) , Quaternion.Euler(-181f, 0f, 0f) 
                , Quaternion.Euler(1f, 0f, 0f), Quaternion.Euler(-163f, 0f, 0f), Quaternion.Euler(-17f, 0f, 0f), Quaternion.Euler(-184f, 0f, 0f), Quaternion.Euler(4f, 0f, 0f)  };
            twist.axisVector = new Vector3[] { Vector3.down, Vector3.up, Vector3.down, Vector3.up, Vector3.down, Vector3.up, Vector3.down, Vector3.up };
            twist.shoulderTwist = new bool[] { false, false, false, false, true, true, false, false};
            twist.enabled = new bool[] { true, true, true, true, true, true, true, true};

        }
	}
}