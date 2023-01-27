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
    public class VRIKNetAction : AbstractNetAction
    {
        /// <summary>
        /// The network object ID of the spawned avatar. Not to be confused
        /// with a network client ID.
        /// </summary>
        public ulong NetworkObjectID;



        public Vector3 RemoteHeadPosition;

        public Vector3 RemoteLeftHandPosition;

        public Vector3 RemoteRightHandPosition;
        
        
        public Quaternion RemoteHeadRotation;
        
        public Quaternion RemoteLeftHandRotation;

        public Quaternion RemoteRightHandRotation;
        
        
        /// <summary>
        /// The path to the animator controller that should be used when the avatar
        /// is set up for VR. This controller will be assigned to the UMA avatar
        /// as the default race animation controller.
        /// </summary>
        private const string AnimatorForVRIK = "Prefabs/Players/VRIKAnimatedLocomotion";
        
        
        
        
        public VRIKNetAction(ulong networkObjectID, VRIK vrik)
        {
            NetworkObjectID = networkObjectID;
            
            RemoteHeadRotation = vrik.solver.spine.headTarget.gameObject.transform.rotation;
            RemoteLeftHandRotation = vrik.solver.leftArm.target.gameObject.transform.rotation;
            RemoteRightHandRotation = vrik.solver.rightArm.target.gameObject.transform.rotation;
                
            RemoteHeadPosition = vrik.solver.spine.headTarget.gameObject.transform.position;
            RemoteLeftHandPosition = vrik.solver.leftArm.target.gameObject.transform.position;
            RemoteRightHandPosition = vrik.solver.rightArm.target.gameObject.transform.position;
            
        }
        
        
        /// <summary>
        /// If executed by the initiating client, nothing happens.
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
                            // daten übertragen

                            vrik.solver.spine.headTarget.gameObject.transform.position = RemoteHeadPosition;
                            vrik.solver.rightArm.target.gameObject.transform.position = RemoteRightHandPosition;
                            vrik.solver.leftArm.target.gameObject.transform.position = RemoteLeftHandPosition;
                            
                            vrik.solver.spine.headTarget.gameObject.transform.rotation = RemoteHeadRotation;
                            vrik.solver.leftArm.target.gameObject.transform.rotation = RemoteLeftHandRotation;
                            vrik.solver.rightArm.target.gameObject.transform.rotation = RemoteRightHandRotation;
                            
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


        private void SetupRemotePlayer(NetworkObject networkObject)
        {

            

            VRIK vrIK = networkObject.gameObject.AddOrGetComponent<VRIK>();

            
            TurnOffAvatarAimingSystem(networkObject);
            ReplaceAnimator(networkObject);

            
            GameObject remoteRig = new GameObject();
            GameObject remoteHead = new GameObject();
            GameObject remoteLeftHand = new GameObject();
            GameObject remoteRightHand = new GameObject();

            remoteRig.name = "RemoteRig";
            remoteHead.name = "RemoteHead";
            remoteLeftHand.name = "RemoteLeftHand";
            remoteRightHand.name = "RemoteRightHand";
            
            remoteRig.transform.SetParent(networkObject.gameObject.transform);
            remoteHead.transform.SetParent(remoteRig.transform);
            remoteLeftHand.transform.SetParent(remoteRig.transform);
            remoteRightHand.transform.SetParent(remoteRig.transform);


            remoteRig.transform.position = RemoteHeadPosition;
            remoteHead.transform.position = RemoteHeadPosition;
            remoteLeftHand.transform.position = RemoteLeftHandPosition;
            remoteRightHand.transform.position = RemoteRightHandPosition;
            
            remoteHead.transform.rotation = RemoteHeadRotation;
            remoteLeftHand.transform.rotation = RemoteLeftHandRotation;
            remoteRightHand.transform.rotation = RemoteRightHandRotation;
            
            vrIK.solver.spine.headTarget = remoteHead.transform;
            Assert.IsNotNull(vrIK.solver.spine.headTarget);
            vrIK.solver.leftArm.target = remoteLeftHand.transform;
            Assert.IsNotNull(vrIK.solver.leftArm.target);
            vrIK.solver.rightArm.target = remoteRightHand.transform;
            Assert.IsNotNull(vrIK.solver.rightArm.target);
            
            AddComponents(networkObject, remoteRightHand.transform);
            
        }

        private void AddComponents(NetworkObject networkObject, Transform remoteRightHand)
        {
            VRAvatarAimingSystem aiming = networkObject.gameObject.AddOrGetComponent<VRAvatarAimingSystem>();
            aiming.IsLocallyControlled = false;
            if (networkObject.gameObject.TryGetComponentOrLog(out AimIK aimIK))
            {
                aiming.Source = remoteRightHand;
                //aiming.Source = aimIK.solver.transform;
                aiming.Target = aimIK.solver.target;
            }
        }
        
        private void TurnOffAvatarAimingSystem(NetworkObject networkObject)
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
        
        
        private void ReplaceAnimator(NetworkObject networkObject)
        {
            if (networkObject.gameObject.TryGetComponentOrLog(out DynamicCharacterAvatar avatar))
            {
                RuntimeAnimatorController animationController = Resources.Load<RuntimeAnimatorController>(AnimatorForVRIK);
                Debug.Log($"Loaded animation controller: {animationController != null}\n");
                if (animationController != null)
                {
                    avatar.raceAnimationControllers.defaultAnimationController = animationController;

                    if (networkObject.gameObject.TryGetComponentOrLog(out Animator animator))
                    {
                        animator.runtimeAnimatorController = animationController;
                        Debug.Log($"Loaded animation controller {animator.name} is human: {animator.isHuman}\n");
                    }
                }
                else
                {
                    Debug.LogError($"Could not load the animation controller at '{AnimatorForVRIK}.'\n");
                }
            }
        }
        
        
        
        
        protected override void ExecuteOnServer()
        {
            // Intentionally left blank.
        }
        
    }
}