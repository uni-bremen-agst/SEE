using UnityEngine;
using RootMotion.FinalIK;
using SEE.GO;

namespace SEE.Game.Avatars
{
    /// <summary>
    /// Animates the body of the avatar.
    /// </summary>
    /// <remarks>This component is assumed to be attached to the avatar's root object.</remarks>
    internal class BodyAnimator : MonoBehaviour
    {
        /// <summary>
        /// The FullBodyBiped IK solver attached to the avatar.
        /// </summary>
        private FullBodyBipedIK ik;

        /// <summary>
        /// Name of the left hand bone in the hierarchy (relative to the root of the avatar).
        /// </summary>
        private const string leftHandName = "CC_Base_BoneRoot/CC_Base_Hip/CC_Base_Waist/CC_Base_Spine01/CC_Base_Spine02/CC_Base_L_Clavicle/CC_Base_L_Upperarm/CC_Base_L_Forearm/CC_Base_L_Hand";
        /// <summary>
        /// The target position of the left hand effector in world space.
        /// </summary>
        public Vector3 LeftHandPosition = Vector3.one;

        private void Awake()
        {
            if (!gameObject.TryGetComponentOrLog(out ik))
            {
                enabled = false;
                return;
            }
            {
                Transform leftHandBone = transform.Find(leftHandName);
                if (leftHandBone == null)
                {
                    Debug.LogError($"Left hand bone not found: {leftHandName}");
                    enabled = false;
                    return;
                }
                LeftHandPosition = leftHandBone.position;
            }
            ik.solver.leftHandEffector.positionWeight = 1f;
            ik.solver.leftHandEffector.rotationWeight = 1f;
        }

        /// <summary>
        /// The time passed since the last cycle started. Range: [0, cycleTime].
        /// </summary>
        private float timePast = 0f;

        /// <summary>
        /// Time for one full cycle (forth and back).
        /// </summary>
        private const float cycleTime = 4f;

        /// <summary>
        /// Y-coordinate of the left hand effector in world space for maximal lower reach.
        /// </summary>
        private const float maximalLowerReach = 1f;

        /// <summary>
        /// Y-coordinate of the left hand effector in world space for maximal upper reach.
        /// </summary>
        private const float maximalUpperReach = 1.8f;

        /// <summary>
        /// Moves the left hand up and down in a cycle.
        /// </summary>
        private void LateUpdate()
        {
            timePast += Time.deltaTime;
            if (timePast < cycleTime / 2)
            {
                // Move the left hand up.
                LeftHandPosition.y = Mathf.Lerp(maximalLowerReach, maximalUpperReach, timePast / (cycleTime / 2));
            }
            else if (timePast < cycleTime)
            {
                LeftHandPosition.y = Mathf.Lerp(maximalUpperReach, maximalLowerReach, (timePast - cycleTime / 2) / (cycleTime / 2));
            }
            else
            {
                timePast = 0; // Reset the timer for the next cycle.
            }

            // Effector position in world space.
            ik.solver.leftHandEffector.position = LeftHandPosition;
        }
    }
}
