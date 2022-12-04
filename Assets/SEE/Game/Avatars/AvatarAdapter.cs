using CrazyMinnow.SALSA;
using Dissonance;
using Dissonance.Audio.Playback;
using RootMotion.FinalIK;
using SEE.Controls;
using SEE.GO;
using SEE.Net;
using SEE.Utils;
using SEE.XR;
using System;
using System.Collections;
using UMA.CharacterSystem;
using Unity.Netcode;
using UnityEngine;
using Valve.VR.InteractionSystem;
using ViveSR.anipal.Lip;

namespace SEE.Game.Avatars
{
    /// <summary>
    /// This component is assumed to be attached to a game object representing
    /// an avatar (representing a local or remote player). It will adapt that
    /// avatar according to the platform we are currently running on
    /// (<see cref="SceneSettings.InputType"/>. In particular, it will
    /// enable SALSA's lip sync and Dissonance.
    /// </summary>
    internal class AvatarAdapter : NetworkBehaviour
    {
        /// <summary>
        /// If this code is executed for the local player, the necessary player type
        /// for the environment we are currently running on are added to this game object.
        /// </summary>
        private void Start()
        {
            if (IsLocalPlayer)
            {
                // I am the avatar of the local player.
                if (!gameObject.TryGetComponent(out CodeSpaceManager _))
                {
                    gameObject.AddComponent<CodeSpaceManager>();
                }

                switch (SceneSettings.InputType)
                {
                    case PlayerInputType.DesktopPlayer:
                    case PlayerInputType.TouchGamepadPlayer:
                        PrepareLocalPlayerForDesktop();
                        break;
                    case PlayerInputType.VRPlayer:
                        PrepareLocalPlayerForXR();
                        break;
                    default:
                        throw new NotImplementedException($"Unhandled case {SceneSettings.InputType}");
                }

                gameObject.name = "Local " + gameObject.name;
            }
            else
            {
                gameObject.name = "Remote " + gameObject.name;
                // Remote players need to be set up for Dissonance and SALSA lip sync.
                StartCoroutine(SetUpSALSA());
            }
            EnableLocalControl(IsLocalPlayer);
        }

        /// <summary>
        /// Enables/disables local control of the aiming system of the avatar.
        /// </summary>
        /// <param name="isLocalPlayer">if true, the aiming system will be enabled, otherwise disabled</param>
        private void EnableLocalControl(bool isLocalPlayer)
        {
            if (gameObject.TryGetComponentOrLog(out AvatarAimingSystem aimingSystem))
            {
                aimingSystem.IsLocallyControlled = isLocalPlayer;
            }
        }

        /// <summary>
        /// The dissonance communication. Its game object holds the remote players as its children.
        /// There can only be one such dissonance communication object for each client, that is, why
        /// this field can be static.
        /// </summary>
        private static DissonanceComms dissonanceComms;

        /// <summary>
        /// A coroutine setting up SALSA for lipsync.
        /// More specifically, the audio source of the <see cref="Salsa"/> component of this
        /// game object be the audio source of the remote player under the Dissonance communication
        /// associated with this avatar.
        /// </summary>
        /// <returns>as to whether to continue</returns>
        private IEnumerator SetUpSALSA()
        {
            if (gameObject.TryGetComponent(out IDissonancePlayer iDissonancePlayer))
            {
                string playerId = iDissonancePlayer.PlayerId;
                // Wait until the iDissonancePlayer is connected and has a valid player ID.
                // This may take some time.
                while (string.IsNullOrEmpty(playerId))
                {
                    playerId = iDissonancePlayer.PlayerId;
                    yield return null;
                }
                if (!string.IsNullOrEmpty(playerId))
                {
                    // Wait until we find a Dissonance player representation with the given playerId
                    GameObject dissonancePlayer = GetDissonancePlayer(playerId);
                    while (dissonancePlayer == null)
                    {
                        dissonancePlayer = GetDissonancePlayer(playerId);
                        yield return null;
                    }

                    if (dissonancePlayer.TryGetComponent(out AudioSource audioSource)
                        && gameObject.TryGetComponent(out Salsa salsa))
                    {
                        salsa.audioSrc = audioSource;
                    }
                    else
                    {
                        if (audioSource == null)
                        {
                            Debug.LogWarning($"{dissonancePlayer.name} has no {typeof(AudioSource)}.\n");
                        }
                        if (!gameObject.TryGetComponent(out Salsa _))
                        {
                            Debug.LogWarning($"{name} has no {typeof(Salsa)}.\n");
                        }
                    }
                }
                else
                {
                    Debug.LogWarning($"Game object {name} has a {typeof(IDissonancePlayer)} without ID.\n");
                }
            }
            else
            {
                Debug.LogWarning($"{name} has no {typeof(IDissonancePlayer)}.\n");
            }
        }

        /// <summary>
        /// Yields the first child of <see cref="dissonanceComms"/> that has a <see cref="VoicePlayback"/>
        /// component whose <see cref="VoicePlayback.PlayerName"/> equals <paramref name="playerId"/>.
        /// If a <see cref="dissonanceComms"/> cannot be found or if there is no such child, an exception
        /// will be thrown.
        /// </summary>
        /// <param name="playerId">the searched player ID</param>
        /// <returns>child of <see cref="dissonanceComms"/> representing <paramref name="playerId"/></returns>
        private static GameObject GetDissonancePlayer(string playerId)
        {
            if (dissonanceComms == null)
            {
                dissonanceComms = FindObjectOfType<DissonanceComms>();
                if (dissonanceComms == null)
                {
                    throw new Exception($"A game object with a {typeof(DissonanceComms)} cannot be found.\n");
                }
            }
            foreach (Transform child in dissonanceComms.transform)
            {
                if (child.TryGetComponent(out VoicePlayback voicePlayback) && voicePlayback.PlayerName == playerId)
                {
                    return child.gameObject;
                }
            }
            return null;
        }

        /// <summary>
        /// Returns the height of a desktop avatar. Call this method only when running
        /// in a desktop environment.
        /// </summary>
        /// <returns>height of a desktop avatar</returns>
        private float DesktopAvatarHeight()
        {
            // If the avatar has a collider, we derive the height from the collider.
            if (gameObject.TryGetComponent(out Collider collider))
            {
                return collider.bounds.size.y;
            }
            else
            {
                // Fallback if we do not have a collider.
                return gameObject.transform.lossyScale.y;
            }
        }

        /// <summary>
        /// Prepares the avatar for a virtual reality environment by adding a VRPlayer prefab
        /// as a child and an <see cref="XRPlayerMovement"/> component.
        /// </summary>
        private void PrepareLocalPlayerForXR()
        {
            StartCoroutine(StartXRCoroutine());
        }

        /// <summary>
        /// The path to the animator controller that should be used when the avatar
        /// is set up for VR. This controller will be assigned to the UMA avatar
        /// as the default race animation controller.
        /// </summary>
        private const string AnimatorForVRIK = "Prefabs/Players/Locomotion"; // "Prefabs/Players/VRIKAnimatedLocomotion";

        public IEnumerator StartXRCoroutine()
        {
            // Start XR manually.
            StartCoroutine(ManualXRControl.StartXRCoroutine());

            // Wait until XR is initialized.
            while (!ManualXRControl.IsInitialized())
            {
                yield return null;
            }

            Debug.Log($"[{nameof(AvatarAdapter)}] XR is initialized. Adding the necessary VR components.\n");

            // Now we can instantiate the prefabs for VR that require that SteamVR is up and running.
            GameObject rig = PrefabInstantiator.InstantiatePrefab("Prefabs/Players/VRUMACameraRig");
            rig.transform.position = gameObject.transform.position;
            rig.AddComponent<NetworkObject>().Spawn();
            rig.AddComponent<ClientNetworkTransform>();

            gameObject.transform.SetParent(rig.transform);
            gameObject.transform.position = Vector3.zero;

            TurnOffAvatarAimingSystem();
            ReplaceAnimator();

            SetupVRIK();

            //GameObject vrPlayer = PrefabInstantiator.InstantiatePrefab("Prefabs/Players/VRPlayer");
            //vrPlayer.name = PlayerInputType.VRPlayer.ToString();
            //gameObject.transform.position = vrPlayer.transform.position;
            //gameObject.transform.rotation = vrPlayer.transform.rotation;
            //vrPlayer.transform.SetParent(gameObject.transform);

            //XRPlayerMovement movement = gameObject.AddComponent<XRPlayerMovement>();
            //movement.DirectingHand = vrPlayer.transform.Find("SteamVRObjects/LeftHand").GetComponent<Hand>();
            //movement.characterController = gameObject.GetComponentInChildren<CharacterController>();

            // Turns off
            void TurnOffAvatarAimingSystem()
            {
                if (gameObject.TryGetComponentOrLog(out AvatarAimingSystem aimingSystem))
                {
                    Destroyer.DestroyComponent(aimingSystem);
                }
                if (gameObject.TryGetComponentOrLog(out AimIK aimIK))
                {
                    Destroyer.DestroyComponent(aimIK);
                }
                if (gameObject.TryGetComponentOrLog(out LookAtIK lookAtIK))
                {
                    Destroyer.DestroyComponent(lookAtIK);
                }
                // AvatarMovementAnimator is using animation parameters that are defined only
                // in our own AvatarAimingSystem animation controller. We will remove it
                // to avoid error messages.
                if (gameObject.TryGetComponentOrLog(out AvatarMovementAnimator avatarMovement))
                {
                    Destroyer.DestroyComponent(avatarMovement);
                }
            }

            // We need to replace the animator of the avatar.
            // The prefab has an aiming animation. We just want locomotion.
            void ReplaceAnimator()
            {
                if (gameObject.TryGetComponentOrLog(out DynamicCharacterAvatar avatar))
                {
                    RuntimeAnimatorController animationController = Resources.Load<RuntimeAnimatorController>(AnimatorForVRIK);
                    Debug.Log($"Loaded animation controller: {animationController != null}\n");
                    if (animationController != null)
                    {
                        avatar.raceAnimationControllers.defaultAnimationController = animationController;

                        if (gameObject.TryGetComponentOrLog(out Animator animator))
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

            // Set up FinalIK's VR IK on the avatar.
            void SetupVRIK()
            {
                VRIK vrIK = gameObject.AddOrGetComponent<VRIK>();
                vrIK.solver.spine.headTarget = rig.transform.Find("Camera/Head");
                UnityEngine.Assertions.Assert.IsNotNull(vrIK.solver.spine.headTarget);
                vrIK.solver.leftArm.target = rig.transform.Find("Controller (left)/LeftHand");
                UnityEngine.Assertions.Assert.IsNotNull(vrIK.solver.leftArm.target);
                vrIK.solver.rightArm.target = rig.transform.Find("Controller (right)/RightHand");
                UnityEngine.Assertions.Assert.IsNotNull(vrIK.solver.rightArm.target);
            }
        }

        /// <summary>
        /// Prepares the avatar for a desktop environment by adding a DesktopPlayer prefab
        /// as a child and a <see cref="DesktopPlayerMovement"/> component.
        /// </summary>
        private void PrepareLocalPlayerForDesktop()
        {
            // Set up the desktop player at the top of the player just in front of it.
            GameObject desktopPlayer = PrefabInstantiator.InstantiatePrefab("Prefabs/Players/DesktopPlayer");
            desktopPlayer.name = PlayerInputType.DesktopPlayer.ToString();
            desktopPlayer.transform.SetParent(gameObject.transform);
            desktopPlayer.transform.localPosition = new Vector3(0, DesktopAvatarHeight(), 0.3f);
            desktopPlayer.transform.localRotation = Quaternion.Euler(30, 0, 0);

            if (gameObject.TryGetComponentOrLog(out AvatarAimingSystem aaSystem))
            {
                // Note: the two components LookAtIK and AimIK are managed by AvatarAimingSystem;
                // we do not turn these on here.
                aaSystem.enabled = true;
            }
            gameObject.AddComponent<DesktopPlayerMovement>();
        }
    }
}
