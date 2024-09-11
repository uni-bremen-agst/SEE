using RootMotion.FinalIK;
using SEE.GO;
using SEE.Utils;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Assertions;

namespace SEE.Game.Avatars
{
    /// <summary>
    /// This class is responsible for setting up the remote and local VRIK avatar.
    /// To make sure the avatar's movements and orientation are transferred correctly,
    /// necessary components are added and existing components are either removed or replaced.
    ///
    /// The animation control of the avatar is also altered.
    /// </summary>
    public static class VRIKActions
    {
        /// <summary>
        /// If executed by the initiating client, nothing happens.
        /// If executed by the remote avatar, the usual positional and rotational model
        /// connections are established.
        /// </summary>
        public static void ExecuteOnClient(ulong networkObjectID, string animatorForVrik,
            Vector3 remoteHeadPosition, Vector3 remoteRightHandPosition, Vector3 remoteLeftHandPosition,
            Quaternion remoteHeadRotation, Quaternion remoteRightHandRotation, Quaternion remoteLeftHandRotation)
        {
            NetworkManager networkManager = NetworkManager.Singleton;

            if (networkManager != null)
            {
                NetworkSpawnManager networkSpawnManager = networkManager.SpawnManager;
                if (networkSpawnManager.SpawnedObjects.TryGetValue(networkObjectID,
                        out NetworkObject networkObject))
                {
                    if (networkObject.gameObject.TryGetComponent(out VRIK vrik))
                    {
                        //Setup usual positional and rotational model connections.
                        GameObject headTarget = vrik.solver.spine.headTarget.gameObject;
                        GameObject rightArm = vrik.solver.rightArm.target.gameObject;
                        GameObject leftArm = vrik.solver.leftArm.target.gameObject;

                        headTarget.transform.position = remoteHeadPosition;
                        leftArm.transform.position = remoteLeftHandPosition;
                        rightArm.transform.position = remoteRightHandPosition;

                        headTarget.gameObject.transform.rotation = remoteHeadRotation;
                        leftArm.transform.rotation = remoteLeftHandRotation;
                        rightArm.transform.rotation = remoteRightHandRotation;
                    }
                    else
                    {
                        SetupRemotePlayer(networkObject, animatorForVrik,
                            remoteHeadPosition, remoteRightHandPosition, remoteLeftHandPosition,
                            remoteHeadRotation, remoteRightHandRotation, remoteLeftHandRotation);
                    }
                }
                else
                {
                    Debug.LogError($"There is no network object with ID {networkObjectID}.\n");
                }
            }
            else
            {
                Debug.LogError($"There is no component {typeof(NetworkManager)} in the scene.\n");
            }
        }


        /// <summary>
        /// Setup for the remote player VRIK model.
        /// </summary>
        /// <param name="networkObject">The existing network object</param>
        /// <param name="remoteHeadPosition">The remote head</param>
        /// <param name="remoteRightHandPosition">The remote right hand position</param>
        /// <param name="remoteLeftHandPosition">The remote left hand position</param>
        /// <param name="remoteHeadRotation">The remote head rotation</param>
        /// <param name="remoteRightHandRotation">The remote right hand rotation</param>
        /// <param name="remoteLeftHandRotation">The remote left hand rotation</param>
        private static void SetupRemotePlayer(NetworkObject networkObject, string animatorForVrik,
            Vector3 remoteHeadPosition, Vector3 remoteRightHandPosition, Vector3 remoteLeftHandPosition,
            Quaternion remoteHeadRotation, Quaternion remoteRightHandRotation, Quaternion remoteLeftHandRotation)
        {
            VRIK vrIK = networkObject.gameObject.AddOrGetComponent<VRIK>();

            // Note: AddComponents() must be run before TurnOffAvatarAimingSystem() because the latter
            // will remove components, the former must query.
            AddComponents(networkObject.gameObject, false);
            TurnOffAvatarAimingSystem(networkObject.gameObject);
            ReplaceAnimator(networkObject.gameObject, animatorForVrik);

            GameObject remoteRig = new()
            {
                name = "RemoteRig",
                transform =
                {
                    position = remoteHeadPosition,
                    parent = networkObject.gameObject.transform
                }
            };

            GameObject remoteHead = new()
            {
                name = "RemoteHead",
                transform =
                {
                    position = remoteHeadPosition,
                    rotation = remoteHeadRotation,
                    parent = remoteRig.transform
                }
            };

            GameObject remoteLeftHand = new()
            {
                name = "RemoteLeftHand",
                transform =
                {
                    position = remoteLeftHandPosition,
                    rotation = remoteLeftHandRotation,
                    parent = remoteRig.transform
                }
            };

            GameObject remoteRightHand = new()
            {
                name = "RemoteRightHand",
                transform =
                {
                    position = remoteRightHandPosition,
                    rotation = remoteRightHandRotation,
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
        /// <param name="isLocallyControlled">Boolean value to determine the locally controlled status, set to false when a network object is provided.</param>
        public static void AddComponents(GameObject gameObject, bool isLocallyControlled)
        {
            VRAvatarAimingSystem aiming = gameObject.AddOrGetComponent<VRAvatarAimingSystem>();
            aiming.IsLocallyControlled = isLocallyControlled;
            if (gameObject.TryGetComponentOrLog(out AimIK aimIK))
            {
                aiming.Source = aimIK.solver.transform;
                aiming.Target = aimIK.solver.target;
            }
            //GameObject vrPlayer = PrefabInstantiator.InstantiatePrefab("Prefabs/Players/VRPlayer");
            //gameObject.transform.position = vrPlayer.transform.position;
            //gameObject.transform.rotation = vrPlayer.transform.rotation;
            //vrPlayer.transform.SetParent(gameObject.transform);

            //XRPlayerMovement movement = gameObject.AddOrGetComponent<XRPlayerMovement>();
            //movement.DirectingHand = rig.
            //movement.DirectingHand = vrPlayer.transform.Find("SteamVRObjects/LeftHand").GetComponent<Hand>();
            //movement.characterController = gameObject.GetComponentInChildren<CharacterController>();
        }

        /// <summary>
        /// Removes AvatarAimingSystem and its associated AimIK and LookAtIK
        /// because our remote VR avatar is controlled by VRIK instead.
        /// Removes AvatarMovementAnimator from remote gameObject, too, because
        /// it is using animation parameters that are defined only
        /// in our own AvatarAimingSystem animation controller.
        /// </summary>
        /// <param name="gameObject">The existing nesting object</param>
        public static void TurnOffAvatarAimingSystem(GameObject gameObject)
        {
            if (gameObject.TryGetComponentOrLog(out AvatarAimingSystem aimingSystem))
            {
                Destroyer.Destroy(aimingSystem);
            }

            if (gameObject.TryGetComponentOrLog(out AimIK aimIK))
            {
                Destroyer.Destroy(aimIK);
            }

            if (gameObject.TryGetComponentOrLog(out LookAtIK lookAtIK))
            {
                Destroyer.Destroy(lookAtIK);
            }

            // AvatarMovementAnimator is using animation parameters that are defined only
            // in our own AvatarAimingSystem animation controller. We will remove it
            // to avoid error messages.
            if (gameObject.TryGetComponentOrLog(out AvatarMovementAnimator avatarMovement))
            {
                Destroyer.Destroy(avatarMovement);
            }
        }

        /// <summary>
        /// We need to replace the animator of the avatar.
        /// The prefab has an aiming animation. We just want locomotion.
        /// </summary>
        /// <param name="gameObject">The existing to be modified game object</param>
        /// <param name="animatorForVrik">The to be replaced Animator</param>
        public static void ReplaceAnimator(GameObject gameObject, string animatorForVrik)
        {
            RuntimeAnimatorController animationController = Resources.Load<RuntimeAnimatorController>(animatorForVrik);

            if (gameObject.TryGetComponentOrLog(out Animator animator))
            {
                animator.runtimeAnimatorController = animationController;
                Debug.Log($"Loaded animation controller {animator.name} is human: {animator.isHuman}\n");
            }
            else
            {
                Debug.LogError($"Could not load the animation controller at '{animatorForVrik}.'\n");
            }
        }
    }
}