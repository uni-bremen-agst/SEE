using UnityEngine;
using System.Collections;

namespace UMA
{
	/// <summary>
	/// Auxillary slot which adds a TwistBone component for the forearms of a newly created character.
	/// </summary>
	public class O3nShoulderUpperLegTwistSlotScript : MonoBehaviour 
	{
		static int leftShoulderHash;
		static int rightShoulderHash;
		static int leftShoulderTwistHash;
		static int rightShoulerTwistHash;

        static int leftThighHash;
        static int rightThighHash;
        static int leftThighTwistHash;
        static int rightThighTwistHash;
        static bool hashesFound = false;

		public void OnDnaApplied(UMAData umaData)
		{
			if (!hashesFound)
			{
				leftShoulderHash = UMAUtils.StringToHash("Upperarm_L");
				rightShoulderHash = UMAUtils.StringToHash("Upperarm_R");
				leftShoulderTwistHash = UMAUtils.StringToHash("UpperarmAdjustTwist_L");
				rightShoulerTwistHash = UMAUtils.StringToHash("UpperarmAdjustTwist_R");
                leftThighHash = UMAUtils.StringToHash("Thigh_L");
                rightThighHash = UMAUtils.StringToHash("Thigh_R");
                leftThighTwistHash = UMAUtils.StringToHash("ThighAdjustTwist_L");
                rightThighTwistHash = UMAUtils.StringToHash("ThighAdjustTwist_R");
                hashesFound = true;
			}

			GameObject leftShoulder = umaData.GetBoneGameObject(leftShoulderHash);
			GameObject rightShoulder = umaData.GetBoneGameObject(rightShoulderHash);
			GameObject leftShoulderTwist = umaData.GetBoneGameObject(leftShoulderTwistHash);
			GameObject rightShoulderTwist = umaData.GetBoneGameObject(rightShoulerTwistHash);

            GameObject leftThigh = umaData.GetBoneGameObject(leftThighHash);
            GameObject rightThigh = umaData.GetBoneGameObject(rightThighHash);
            GameObject leftThighTwist = umaData.GetBoneGameObject(leftThighTwistHash);
            GameObject rightThighTwist = umaData.GetBoneGameObject(rightThighTwistHash);

            if ((leftShoulder == null) || (rightShoulder == null) || (leftShoulderTwist == null) || (rightShoulderTwist == null)
                || (leftThigh == null) || (rightThigh == null) || (leftThighTwist == null) || (rightThighTwist == null))
			{
				Debug.LogError("Failed to add o3n Forearm Twist to: " + umaData.name);
				return;
			}

			var twist = umaData.umaRoot.AddComponent<TwistBones>();
			twist.twistValue = -0.5f;
			twist.twistBone = new Transform[] {leftShoulderTwist.transform, rightShoulderTwist.transform, leftThighTwist.transform, rightThighTwist.transform};
			twist.refBone = new Transform[] {leftShoulder.transform, rightShoulder.transform, leftThigh.transform, rightThigh.transform};



		}
	}
}