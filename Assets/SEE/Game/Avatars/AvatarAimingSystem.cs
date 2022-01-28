using UnityEngine;
using RootMotion.FinalIK;

namespace SEE.Game.Avatars
{
    /// <summary>
    /// A 360-degree aiming system based on Final IK and built with six static aiming poses and
    /// AimIK.
    ///
    /// This code is based on <see cref="RootMotion.Demos.SimpleAimingSystem"/> and adapted
    /// to our needs. Unlike <see cref="RootMotion.Demos.SimpleAimingSystem"/>, this code
    /// provides a means to unaim.
    /// </summary>
    public class AvatarAimingSystem : MonoBehaviour
    {
        [Tooltip("AimPoser is a tool that returns an animation name based on direction.")]
        public AimPoser aimPoser;

        [Tooltip("Reference to the AimIK component.")]
        public AimIK aim;

        [Tooltip("Reference to the LookAt component (only used for the head in this instance).")]
        public LookAtIK lookAt;

        [Tooltip("Reference to the Animator component.")]
        public Animator animator;

        [Tooltip("Time of cross-fading from pose to pose.")]
        public float crossfadeTime = 0.2f;

        [Tooltip("Will keep the aim target at a distance.")]
        public float minAimDistance = 0.5f;

        [Tooltip("The target to point to when a neutral pose is to be taken (not aiming).")]
        public Transform NeutralAimTarget;

        [Tooltip("The target to look to when a neutral pose is to be taken (not aiming).")]
        public Transform NeutralLookTarget;

        /// <summary>
        /// The pose of the <see cref="aimPoser"/> while aiming at the target.
        /// </summary>
        private AimPoser.Pose aimPose;

        /// <summary>
        /// The last <see cref="aimPose"/> stored so we know if <see cref="aimPose"/> changed.
        /// </summary>
        private AimPoser.Pose lastPose;

        /// <summary>
        /// Whether the avatar is currently pointing, i.e., whether it has an aiming or looking target.
        /// </summary>
        private bool isPointing = true;

        /// <summary>
        /// The <see cref="AimIK"/> component attached to this avatar. It is used for aiming.
        /// We are using it to switch between the original aiming target (retrieved from
        /// <see cref="aimIK.solver.target"/> and <see cref="NeutralAimTarget"/>.
        /// </summary>
        private AimIK aimIK;

        /// <summary>
        /// The <see cref="LookAtIK"/> component attached to this avatar. It is used for looking
        /// at a particular target.
        /// We are using it to switch between the original looking target (retrieved from
        /// <see cref="lookAtIK.solver.target"/> and <see cref="NeutralLookTarget"/>.
        /// </summary>
        private LookAtIK lookAtIK;

        /// <summary>
        /// The original aimed target of the avatar as retrieved via <see cref="aimIK.solver.target"/>.
        /// It is used to switch back from non-pointing to pointing. May be null.
        /// </summary>
        private Transform originalAimTarget;

        /// <summary>
        /// The original aimed target of the avatar as retrieved via <see cref="lookAtIK.solver.target"/>.
        /// It is used to switch back from non-pointing to pointing. May be null.
        /// </summary>
        private Transform originalLookTarget;

        /// <summary>
        /// Toggles between pointing and not pointing.
        /// </summary>
        public void TogglePointing()
        {
            isPointing = !isPointing;
            if (isPointing)
            {
                aimIK.solver.target = originalAimTarget;
                lookAtIK.solver.target = originalLookTarget;
            }
            else
            {
                originalAimTarget = aimIK.solver.target;
                originalLookTarget = lookAtIK.solver.target;
                aimIK.solver.target = NeutralAimTarget;
                lookAtIK.solver.target = NeutralLookTarget;
            }
        }

        /// <summary>
        /// Disables the IK components <see cref="aim"/> and <see cref="lookAt"/>
        /// so that we can manage their updating order by ourselves. Sets
        /// <see cref="aimIK"/>, <see cref="originalAimTarget"/>, <see cref="lookAtIK"/>,
        /// <see cref="originalLookTarget"/>, and <see cref="isPointing"/>.
        /// </summary>
        private void Start()
        {
            aim.enabled = false;
            lookAt.enabled = false;

            if (TryGetComponent(out aimIK))
            {
                originalAimTarget = aimIK.solver.target;
            }
            if (TryGetComponent(out lookAtIK))
            {
                originalLookTarget = lookAtIK.solver.target;
            }
            isPointing = originalLookTarget != null || originalAimTarget != null;
        }

        /// <summary>
        /// If <see cref="KeyCode.P"/> is entered, toggles pointing.
        /// </summary>
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.P))
            {
                TogglePointing();
            }
        }

        /// <summary>
        /// Adjusts the pose and updates the solver of <see cref="aim"/> and
        /// <see cref="lookAt"/>.
        /// </summary>
        private void LateUpdate()
        {
            if (isPointing)
            {
                // Switch aim poses (Legacy animation)
                Pose();

                // Update IK solvers
                aim.solver.Update();
                if (lookAt != null)
                {
                    lookAt.solver.Update();
                }
            }
            else
            {
                AimTowards(Vector3.down);
            }
        }

        /// <summary>
        /// Updates the pose.
        /// </summary>
        private void Pose()
        {
            // Make sure aiming target is not too close (might make the solver instable when the
            // target is closer to the first bone than the last bone is).
            LimitAimTarget();

            // Get the aiming direction
            Vector3 direction = aim.solver.IKPosition - aim.solver.bones[0].transform.position;

            // Getting the direction relative to the root transform
            Vector3 localDirection = transform.InverseTransformDirection(direction);

            AimTowards(localDirection);
        }

        private void AimTowards(Vector3 localDirection)
        {
            // Get the Pose from AimPoser
            aimPose = aimPoser.GetPose(localDirection);

            // If the Pose has changed
            if (aimPose != lastPose)
            {
                // Increase the angle buffer of the pose so we won't switch back too soon if
                // the direction changes a bit.
                aimPoser.SetPoseActive(aimPose);

                // Store the pose so we know if it changes.
                lastPose = aimPose;
            }

            // Direct blending
            foreach (AimPoser.Pose pose in aimPoser.poses)
            {
                if (pose == aimPose)
                {
                    DirectCrossFade(pose.name, 1f);
                }
                else
                {
                    DirectCrossFade(pose.name, 0f);
                }
            }
        }

        /// <summary>
        /// Makes sure the aiming target is not too close (might make the solver instable when
        /// the target is closer to the first bone than the last bone is).
        /// </summary>
        private void LimitAimTarget()
        {
            Vector3 aimFrom = aim.solver.bones[0].transform.position;
            Vector3 direction = aim.solver.IKPosition - aimFrom;
            direction = direction.normalized * Mathf.Max(direction.magnitude, minAimDistance);
            aim.solver.IKPosition = aimFrom + direction;
        }

        /// <summary>
        /// Uses Mecanim's Direct blend trees for cross-fading.
        /// </summary>
        /// <param name="pose">the pose parameter of the <see cref="animator"/></param>
        /// <param name="target">target to be reached</param>
        private void DirectCrossFade(string pose, float target)
        {
            float newStateValue = Mathf.MoveTowards(animator.GetFloat(pose), target, Time.deltaTime * (1f / crossfadeTime));
            animator.SetFloat(pose, newStateValue);
        }
    }
}
