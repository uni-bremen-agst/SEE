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

        /// <summary>
        /// The pose of the <see cref="aimPoser"/> while aiming at the target.
        /// </summary>
        private AimPoser.Pose aimPose;

        /// <summary>
        /// The last <see cref="aimPose"/> stored so we know if <see cref="aimPose"/> changed.
        /// </summary>
        private AimPoser.Pose lastPose;

        /// <summary>
        /// Disables the IK components <see cref="aim"/> and <see cref="lookAt"/>
        /// so that we can manage their updating order by ourselves.
        /// </summary>
        private void Start()
        {
            aim.enabled = false;
            lookAt.enabled = false;
        }

        /// <summary>
        /// Adjusts the pose and updates the solver of <see cref="aim"/> and
        /// <see cref="lookAt"/>.
        /// </summary>
        private void LateUpdate()
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
