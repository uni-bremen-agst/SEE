using RootMotion.FinalIK;
using SEE.Game.Avatars;
using SEE.GO;
using SEE.Utils;
using UMA.CharacterSystem;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Assertions;

namespace SEE.Net.Actions
{
    /// <summary>
    /// Updates values of <see cref="VRIK"/> on all remote clients.
    /// </summary>
    public class VRIKNetAction : AbstractNetAction
    {
        /// <summary>
        /// The network object ID of the spawned avatar. Not to be confused
        /// with a network client ID.
        /// </summary>
        public ulong NetworkObjectID;

        /// <summary>
        /// The remote position of the head.
        /// </summary>
        public Vector3 RemoteHeadPosition;

        /// <summary>
        /// The remote position of the left hand.
        /// </summary>
        public Vector3 RemoteLeftHandPosition;

        /// <summary>
        /// The remote position of the right hand.
        /// </summary>
        public Vector3 RemoteRightHandPosition;

        /// <summary>
        /// The remote rotation of the head.
        /// </summary>
        public Quaternion RemoteHeadRotation;

        /// <summary>
        /// The remote rotation of the left hand.
        /// </summary>
        public Quaternion RemoteLeftHandRotation;

        /// <summary>
        /// The remote rotation of the right hand.
        /// </summary>
        public Quaternion RemoteRightHandRotation;

        /// <summary>
        /// The path to the animator controller that should be used when the avatar
        /// is set up for VR. This controller will be assigned to the remote UMA avatar
        /// as the default race animation controller.
        /// </summary>
        private const string AnimatorForVrik = "Prefabs/Players/VRIKAnimatedLocomotion";

        /// <summary>
        /// Initializes all variables that should be transferred to the remote avatars.
        /// </summary>
        /// <param name="networkObjectID">NetworkObject ID of the spawned avatar game object.</param>
        /// <param name="vrik">VRIK component to be synchronized.</param>
        public VRIKNetAction(ulong networkObjectID, VRIK vrik)
        {
            GameObject remoteHeadTarget = vrik.solver.spine.headTarget.gameObject;
            GameObject remoteLeftArm = vrik.solver.leftArm.target.gameObject;
            GameObject remoteRightArm = vrik.solver.rightArm.target.gameObject;

            NetworkObjectID = networkObjectID;

            RemoteHeadPosition = remoteHeadTarget.transform.position;
            RemoteLeftHandPosition = remoteLeftArm.transform.position;
            RemoteRightHandPosition = remoteRightArm.transform.position;

            RemoteHeadRotation = remoteHeadTarget.transform.rotation;
            RemoteLeftHandRotation = remoteLeftArm.transform.rotation;
            RemoteRightHandRotation = remoteRightArm.transform.rotation;
        }

        /// <summary>
        /// If executed by the initiating client, nothing happens.
        /// If executed by the remote avatar, the usual positional and rotational model
        /// connections are established.
        /// </summary>
        protected override void ExecuteOnClient()
        {
            if (!IsRequester())
            {
                NetworkManager networkManager = NetworkManager.Singleton;

                if (networkManager != null)
                {
                    NetworkSpawnManager networkSpawnManager = networkManager.SpawnManager;
                    if (networkSpawnManager.SpawnedObjects.TryGetValue(NetworkObjectID,
                            out NetworkObject networkObject))
                    {
                        if (networkObject.gameObject.TryGetComponent(out VRIK vrik))
                        {
                            //Setup usual positional and rotational model connections.
                            GameObject headTarget = vrik.solver.spine.headTarget.gameObject;
                            GameObject rightArm = vrik.solver.rightArm.target.gameObject;
                            GameObject leftArm = vrik.solver.leftArm.target.gameObject;

                            headTarget.transform.position = RemoteHeadPosition;
                            rightArm.transform.position = RemoteRightHandPosition;
                            leftArm.transform.position = RemoteLeftHandPosition;

                            headTarget.gameObject.transform.rotation = RemoteHeadRotation;
                            leftArm.transform.rotation = RemoteLeftHandRotation;
                            rightArm.transform.rotation = RemoteRightHandRotation;
                        }
                        else
                        {
                            SetupRemotePlayer(networkObject);
                        }
                    }
                }
                else
                {
                    Debug.LogError($"There is no component {typeof(NetworkManager)} in the scene.\n");
                }
            }
        }

        /// <summary>
        /// Setup for the remote player VRIK model.
        /// </summary>
        /// <param name="networkObject">The existing network object</param>
        private void SetupRemotePlayer(NetworkObject networkObject)
        {
            VRIK vrIK = networkObject.gameObject.AddOrGetComponent<VRIK>();

            // Note: AddComponents() must be run before TurnOffAvatarAimingSystem() because the latter
            // will remove components, the former must query.
            AddComponents(networkObject);
            TurnOffAvatarAimingSystem(networkObject);
            ReplaceAnimator(networkObject);

            GameObject remoteRig = new()
            {
                name = "RemoteRig",
                transform =
                {
                    position = RemoteHeadPosition,
                    parent = networkObject.gameObject.transform
                }
            };

            GameObject remoteHead = new()
            {
                name = "RemoteHead",
                transform =
                {
                    position = RemoteHeadPosition,
                    rotation = RemoteHeadRotation,
                    parent = remoteRig.transform
                }
            };

            GameObject remoteLeftHand = new()
            {
                name = "RemoteLeftHand",
                transform =
                {
                    position = RemoteLeftHandPosition,
                    rotation = RemoteLeftHandRotation,
                    parent = remoteRig.transform
                }
            };

            GameObject remoteRightHand = new()
            {
                name = "RemoteRightHand",
                transform =
                {
                    position = RemoteRightHandPosition,
                    rotation = RemoteRightHandRotation,
                    parent = remoteRig.transform
                }
            };

            vrIK.solver.spine.headTarget = remoteHead.transform;
            Assert.IsNotNull(vrIK.solver.spine.headTarget);
            vrIK.solver.leftArm.target = remoteLeftHand.transform;
            Assert.IsNotNull(vrIK.solver.leftArm.target);
            vrIK.solver.rightArm.target = remoteRightHand.transform;
            Assert.IsNotNull(vrIK.solver.rightArm.target);
        }

        /// <summary>
        /// Adds required components.
        /// </summary>
        /// <param name="networkObject">The existing network object</param>
        private static void AddComponents(NetworkObject networkObject)
        {
            VRAvatarAimingSystem aiming = networkObject.gameObject.AddOrGetComponent<VRAvatarAimingSystem>();
            aiming.IsLocallyControlled = false;
            if (networkObject.gameObject.TryGetComponentOrLog(out AimIK aimIK))
            {
                aiming.Source = aimIK.solver.transform;
                aiming.Target = aimIK.solver.target;
            }
        }

        /// <summary>
        /// Removes AvatarAimingSystem and its associated AimIK and LookAtIK
        /// because our remote VR avatar is controlled by VRIK instead.
        /// Removes AvatarMovementAnimator from remote gameObject, too, because
        /// it is using animation parameters that are defined only
        /// in our own AvatarAimingSystem animation controller.
        /// </summary>
        /// <param name="networkObject">The existing network object</param>
        private static void TurnOffAvatarAimingSystem(NetworkObject networkObject)
        {
            if (networkObject.gameObject.TryGetComponentOrLog(out AvatarAimingSystem aimingSystem))
            {
                Destroyer.Destroy(aimingSystem);
            }

            if (networkObject.gameObject.TryGetComponentOrLog(out AimIK aimIK))
            {
                Destroyer.Destroy(aimIK);
            }

            if (networkObject.gameObject.TryGetComponentOrLog(out LookAtIK lookAtIK))
            {
                Destroyer.Destroy(lookAtIK);
            }

            // AvatarMovementAnimator is using animation parameters that are defined only
            // in our own AvatarAimingSystem animation controller. We will remove it
            // to avoid error messages.
            if (networkObject.gameObject.TryGetComponentOrLog(out AvatarMovementAnimator avatarMovement))
            {
                Destroyer.Destroy(avatarMovement);
            }
        }

        /// <summary>
        /// We need to replace the animator of the avatar.
        /// The prefab has an aiming animation. We just want locomotion.
        /// </summary>
        /// <param name="networkObject">The existing network object</param>
        private static void ReplaceAnimator(NetworkObject networkObject)
        {
            if (networkObject.gameObject.TryGetComponentOrLog(out DynamicCharacterAvatar avatar))
            {
                RuntimeAnimatorController animationController =
                    Resources.Load<RuntimeAnimatorController>(AnimatorForVrik);
                if (animationController != null)
                {
                    avatar.raceAnimationControllers.defaultAnimationController = animationController;

                    if (networkObject.gameObject.TryGetComponentOrLog(out Animator animator))
                    {
                        animator.runtimeAnimatorController = animationController;
                    }
                }
                else
                {
                    Debug.LogError($"Could not load the animation controller at '{AnimatorForVrik}.'\n");
                }
            }
        }

        /// <summary>
        /// Does not do anything.
        /// </summary>
        protected override void ExecuteOnServer()
        {
            // Intentionally left blank.
        }
    }
}