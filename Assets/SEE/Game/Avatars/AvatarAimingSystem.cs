using UnityEngine;
using RootMotion.FinalIK;
using SEE.GO;
using SEE.Controls;
using SEE.Net.Actions;
using Unity.Netcode;

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
        [Tooltip("Reference to the AimIK component.")]
        public AimIK Aim;

        [Tooltip("Reference to the LookAt component (only used for the head in this instance).")]
        public LookAtIK LookAt;

        [Tooltip("Reference to the Animator component.")]
        public Animator Animator;

        [Tooltip("Time of cross-fading from pose to pose.")]
        public float CrossfadeTime = 0.2f;

        [Tooltip("Will keep the aim target at a distance.")]
        public float MinimalAimDistance = 0.1f;

        /// <summary>
        /// Whether the avatar is currently pointing, i.e., whether it has an aiming or looking target.
        /// </summary>
        [Tooltip("If true, the avatar is currently pointing. Its pose will be adjusted according to the aimed target.")]
        public bool IsPointing = true;

        [Tooltip("If true, local interactions control where the avatar is pointing to.")]
        public bool IsLocallyControlled = true;

        [Tooltip("The laser beam for pointing. If null, one will be created at run-time.")]
        public LaserPointer laser;

        /// <summary>
        /// AimPoser is a tool that returns an animation name based on direction.
        /// It will be searched in the scene by the name <see cref="AimPoserName"/>.
        /// There is only one <see cref="AimPoser"/> in the scene. That is why this
        /// field can be static.
        /// </summary>
        private static AimPoser AimPoser;

        /// <summary>
        /// The name of the <see cref="AimPoser"/> game object. Must be present in the scene.
        /// </summary>
        private const string AimPoserName = "Aim Poser";

        /// <summary>
        /// The pose of the <see cref="AimPoser"/> while aiming at the target.
        /// </summary>
        private AimPoser.Pose aimPose;

        /// <summary>
        /// The last <see cref="aimPose"/> stored so we know if <see cref="aimPose"/> changed.
        /// </summary>
        private AimPoser.Pose lastPose;

        /// <summary>
        /// The <see cref="AimIK"/> component attached to this avatar. It is used for aiming.
        /// The aiming target is <see cref="aimIK.solver.target"/>.
        /// </summary>
        private AimIK aimIK;

        /// <summary>
        /// The <see cref="NetworkObject"/> attached to the game object this <see cref="AvatarAimingSystem"/>
        /// is attached to. It will be used to retrieve the <see cref="NetworkObject.NetworkObjectId"/>.
        /// </summary>
        private NetworkObject networkObject;

        /// <summary>
        /// Toggles between pointing and not pointing.
        /// </summary>
        private void TogglePointing()
        {
            SetPointing(!IsPointing);
        }

        /// <summary>
        /// If <paramref name="activate"/> is true, the laser will be turned on;
        /// otherwise turned off.
        /// This parameter is also propagated to all clients.
        /// </summary>
        /// <param name="activate">whether pointing is to be activated</param>
        /// <remarks>This method is called either as a interaction request of the local
        /// player or from <see cref="TogglePointingNetAction"/> from a remote player via
        /// the network.</remarks>
        public void SetPointing(bool activate)
        {
            IsPointing = activate;
            Debug.Log($"[AAS] SetPointing({IsPointing}).\n");
            if (aimIK == null)
            {
                gameObject.TryGetComponentOrLog(out aimIK);
            }
            // Laser beam should be active only while we are pointing.
            if (laser == null)
            {
                Debug.Log($"[AAS] No laser yet. Adding one.\n");
                laser = gameObject.AddOrGetComponent<LaserPointer>();
                laser.Source = aimIK.solver.transform;
            }
            UnityEngine.Assertions.Assert.IsNotNull(laser);
            laser.SetActive(IsPointing);
            Debug.Log($"[AAS] Laser activated: {IsPointing}.\n");

            // Activate the aimed target. FIXME: What for?
            if (aimIK != null && aimIK.solver != null && aimIK.solver.target != null)
            {
                aimIK.solver.target.gameObject.SetActive(IsPointing);
            }
            if (IsLocallyControlled)
            {
                if (gameObject.TryGetComponentOrLog(out networkObject))
                {
                    Debug.Log($"[AAS] TogglePointingNetAction({networkObject.NetworkObjectId}, {IsPointing}).\n");
                    new TogglePointingNetAction(networkObject.NetworkObjectId, IsPointing).Execute();
                }
            }
            else
            {
                Debug.Log($"[AAS] Not locally controlled.\n");
            }
        }

        /// <summary>
        /// Retrieves the <see cref="AimPoser"/> from the scene when not already set.
        /// Disables the IK components <see cref="Aim"/> and <see cref="LookAt"/>
        /// so that we can manage their updating order by ourselves. Retrieves
        /// the <see cref="laser"/> from the aiming transform <see cref="aimIK.solver.transform"/>.
        /// </summary>
        private void Start()
        {
            // Retrieve the aim poser.
            if (AimPoser == null)
            {
                GameObject aimPoser = GameObject.Find(AimPoserName);
                if (aimPoser == null || !aimPoser.TryGetComponent(out AimPoser))
                {
                    Debug.LogError($"There is no game object named {AimPoserName} with a {typeof(AimPoser)} component in the scene.\n");
                    enabled = false;
                    return;
                }
            }

            /// We are disabling <see cref="Aim"/> and <see cref="LookAt"/> so that
            /// we can control their update cycle ourselves.
            Aim.enabled = false;
            LookAt.enabled = false;

            if (gameObject.TryGetComponent(out aimIK))
            {
                laser = gameObject.AddOrGetComponent<LaserPointer>();
                laser.Source = aimIK.solver.transform;
            }
            else
            {
                Debug.LogError($"There is no {typeof(AimIK)} component attached to the game object {gameObject.FullName()}.\n");
                enabled = false;
                return;
            }
            gameObject.TryGetComponentOrLog(out networkObject);
            MoveTarget();
            /// We start in the pointing state.
            SetPointing(true);
        }

        /// <summary>
        /// If <see cref="IsLocallyControlled"/>, moves the aimIK target
        /// and toggles pointing if <see cref="SEEInput.TogglePointing()"/>.
        /// </summary>
        private void Update()
        {
            if (IsLocallyControlled)
            {
                if (SEEInput.TogglePointing())
                {
                    TogglePointing();
                }
                MoveTarget();
            }
            if (IsPointing)
            {
                // This code will be run for a non-local player currently pointing.
                laser.Draw(aimIK.solver.target.position);
            }
        }

        /// <summary>
        /// Moves <see cref="aimIK.solver.target"/> to the end point of the laser beam,
        /// i.e., the point where the user is currently pointing to.
        /// </summary>
        private void MoveTarget()
        {
            if (IsPointing)
            {
                aimIK.solver.target.position = laser.Point();
            }
        }

        /// <summary>
        /// If <see cref="IsPointing"/>, adjusts the pose and updates the solver of <see cref="Aim"/>
        /// and <see cref="LookAt"/> to aim at the target; otherwise a neutral pose will be taken.
        /// </summary>
        private void LateUpdate()
        {
            if (IsPointing)
            {
                // Switch aim poses (Legacy animation)
                Pose();

                // Update IK solvers
                Aim.solver.Update();
                if (LookAt != null)
                {
                    LookAt.solver.Update();
                }
            }
            else
            {
                // Pointing down is our neutral pose.
                AimTowards(Vector3.down);
            }
        }

        /// <summary>
        /// Updates the pose of the avatar so that the avatar is pointing towards <see cref="Target"/>.
        /// </summary>
        private void Pose()
        {
            // Make sure aiming target is not too close (might make the solver instable when the
            // target is closer to the first bone than the last bone is).
            LimitAimTarget();

            // Get the aiming direction
            Vector3 direction = Aim.solver.IKPosition - Aim.solver.bones[0].transform.position;

            // Getting the direction relative to the root transform
            Vector3 localDirection = transform.InverseTransformDirection(direction);

            AimTowards(localDirection);
        }

        /// <summary>
        /// Takes a pose aiming towards <paramref name="localDirection"/> (cross-fading towards
        /// this direction).
        /// </summary>
        /// <param name="localDirection">direction to aim at relative to the avatar</param>
        private void AimTowards(Vector3 localDirection)
        {
            // Get the Pose from AimPoser
            aimPose = AimPoser.GetPose(localDirection);

            // If the Pose has changed
            if (aimPose != lastPose)
            {
                // Increase the angle buffer of the pose so we won't switch back too soon if
                // the direction changes a bit.
                AimPoser.SetPoseActive(aimPose);

                // Store the pose so we know if it changes.
                lastPose = aimPose;
            }

            // Direct blending
            foreach (AimPoser.Pose pose in AimPoser.poses)
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
            Vector3 aimFrom = Aim.solver.bones[0].transform.position;
            Vector3 direction = Aim.solver.IKPosition - aimFrom;
            direction = direction.normalized * Mathf.Max(direction.magnitude, MinimalAimDistance);
            Aim.solver.IKPosition = aimFrom + direction;
        }

        /// <summary>
        /// Uses Mecanim's Direct blend trees for cross-fading.
        /// </summary>
        /// <param name="pose">the pose parameter of the <see cref="Animator"/></param>
        /// <param name="target">target to be reached</param>
        private void DirectCrossFade(string pose, float target)
        {
            float newStateValue = Mathf.MoveTowards(Animator.GetFloat(pose), target, Time.deltaTime * (1f / CrossfadeTime));
            Animator.SetFloat(pose, newStateValue);
        }
    }
}
