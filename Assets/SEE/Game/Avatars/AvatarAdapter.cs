using CrazyMinnow.SALSA;
using Dissonance;
using Dissonance.Audio.Playback;
using RootMotion.FinalIK;
using SEE.Controls;
using SEE.GO;
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
        [Header("VR specific settings (relevant only for VR players)")]

        [Tooltip("Whether the VR controllers should be hidden.")]
        public bool HideVRControllers = false;

        [Tooltip("Whether hints should be shown for controllers.")]
        public bool ShowControllerHints = false;

        /// <summary>
        /// The distance from the top of the player's height to his eyes.
        /// </summary>
        private const float PlayerTopToEyeDistance = 0.14f;

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
        /// <param name="isLocalPlayer">if true, the aiming system will be locally controlled
        /// (i.e., by the local player) or remotely controlled (i.e., the aiming action
        /// of a remote player are to be replicated on its local representation)</param>
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
        private const string AnimatorForVRIK = "Prefabs/Players/VRIKAnimatedLocomotion"; // "Prefabs/Players/Locomotion";

        /// <summary>
        /// The path of the prefab for the VR camera rig.
        /// </summary>
        private const string VRPlayerRigPrefab = "Prefabs/Players/VRUMACameraRig"; // "Prefabs/Players/VRPlayerRig";
        /// <summary>
        /// The composite name of the child within <see cref="VRPlayerRigPrefab"/>
        /// representing the head for VRIK.
        /// </summary>
        private const string VRPlayerHeadForVRIK = "Camera/Head"; //"SteamVRObjects/VRCamera/HeadForVRIK";
        /// <summary>
        /// The composite name of the child within <see cref="VRPlayerRigPrefab"/>
        /// representing the left hand for VRIK.
        /// </summary>
        private const string VRPLayerLeftHandForVRIK = "Controller (left)/LeftHand"; // "SteamVRObjects/LeftHand/LeftHandForVRIK";
        /// <summary>
        /// The composite name of the child within <see cref="VRPlayerRigPrefab"/>
        /// representing the right hand for VRIK.
        /// </summary>
        private const string VRPlayerRightHandForVRIK = "Controller (right)/RightHand";  // "SteamVRObjects/RightHand/RightHandForVRIK";

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

            // Now we can instantiate the prefabs for VR that requires that SteamVR is up and running.
            GameObject rig = PrefabInstantiator.InstantiatePrefab(VRPlayerRigPrefab);
            rig.transform.position = gameObject.transform.position;
            // FIXME: Only the server is allowed to spawn objects.
            //rig.AddComponent<NetworkObject>().Spawn();
            //rig.AddComponent<ClientNetworkTransform>();

            //gameObject.transform.SetParent(rig.transform);
            //gameObject.transform.position = Vector3.zero;

            PrepareScene();
            // Note: AddComponents() must be run before TurnOffAvatarAimingSystem() because the latter
            // will remove components, the former must query.
            AddComponents();
            TurnOffAvatarAimingSystem();
            ReplaceAnimator();
            SetupVRIK();


            /// <summary>
            /// Sets up the scene for playing in an VR environment. This means to instantiate the
            /// Teleporting object and to attach a TeleportArea to the ground plane named <see cref="FloorName"/>.
            /// In addition, the VR camera is assigned to the ChartManager if it exists.
            ///
            /// Precondition: There must be a game object named <see cref="FloorName"/> in the scene, representing
            /// the ground (a Unity Plane would be attached to it).
            /// </summary>
            void PrepareScene()
            {
                const string GroundName = "Ground";

                GameObject ground = GameObject.Find(GroundName);
                if (ground == null)
                {
                    Debug.LogError($"There is no ground object named {GroundName}. Teleporting cannot be set up.\n");
                }
                else
                {
                    // Create Teleporting game object
                    PrefabInstantiator.InstantiatePrefab("Prefabs/Players/Teleporting").name = "Teleporting";
                    {
                        // Attach TeleportArea to floor
                        // The TeleportArea replaces the material of the game object it is attached to
                        // into a transparent material. This way the game object becomes invisible.
                        // For this reason, we will clone the floor and move the cloned floor slightly above
                        // its origin and then attach the TeleportArea to the cloned floor.
                        Vector3 position = ground.transform.position;
                        position.y += 0.01f;
                        GameObject clonedFloor = Instantiate(ground, position, ground.transform.rotation);
                        clonedFloor.AddComponent<TeleportArea>();
                    }
                    // FIXME: This needs to work again for our metric charts.
                    //{
                    //    // Assign the VR camera to the chart manager so that charts can move along with the camera.
                    //    GameObject chartManager = GameObject.Find(ChartManagerName);
                    //    if (chartManager)
                    //    {
                    //        ChartPositionVr chartPosition = chartManager.GetComponentInChildren<ChartPositionVr>();
                    //        if (chartPosition)
                    //        {
                    //            chartPosition.enabled = true;
                    //            chartPosition.CameraTransform = player.GetComponentInChildren<Camera>().transform;
                    //            Debug.Log($"VR camera of {player.name} successfully assigned to {ChartManagerName}.\n");
                    //        }
                    //        else
                    //        {
                    //            Debug.LogError($"{ChartManagerName} has no component {nameof(ChartPositionVr)}.\n");
                    //        }
                    //    }
                    //    else
                    //    {
                    //        Debug.LogError($"No {ChartManagerName} found.\n");
                    //    }
                    //}
                }
            }

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
                vrIK.solver.spine.headTarget = rig.transform.Find(VRPlayerHeadForVRIK);
                UnityEngine.Assertions.Assert.IsNotNull(vrIK.solver.spine.headTarget);
                vrIK.solver.leftArm.target = rig.transform.Find(VRPLayerLeftHandForVRIK);
                UnityEngine.Assertions.Assert.IsNotNull(vrIK.solver.leftArm.target);
                vrIK.solver.rightArm.target = rig.transform.Find(VRPlayerRightHandForVRIK);
                UnityEngine.Assertions.Assert.IsNotNull(vrIK.solver.rightArm.target);
            }

            // Adds required components.
            void AddComponents()
            {
                VRAvatarAimingSystem aiming = gameObject.AddOrGetComponent<VRAvatarAimingSystem>();
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
        }

        /// <summary>
        /// Turns off VR controller hints if <see cref="ShowControllerHints"/> is <c>false</c>.
        /// </summary>
        private void TurnOffControllerHintsIfRequested()
        {
            if (SceneSettings.InputType == PlayerInputType.VRPlayer && !ShowControllerHints)
            {
                if (Player.instance != null)
                {
                    foreach (Hand hand in Player.instance.hands)
                    {
                        ControllerButtonHints.HideAllButtonHints(hand);
                        ControllerButtonHints.HideAllTextHints(hand);
                    }
                }
                else
                {
                    Debug.LogError($"{nameof(Player)}.instance is null. Is VR running?\n");
                }

                if (Teleport.instance != null)
                {
                    Teleport.instance.CancelTeleportHint();
                }
                else
                {
                    Debug.LogWarning($"{nameof(Teleport)}.instance is null. Is there no teleport area in the scene?\n");
                }
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
            desktopPlayer.transform.localPosition = new Vector3(0, DesktopAvatarHeight() - PlayerTopToEyeDistance, 0.3f);
            desktopPlayer.transform.localRotation = Quaternion.Euler(30, 0, 0);

            if (gameObject.TryGetComponentOrLog(out AvatarAimingSystem aaSystem))
            {
                // Note: the two components LookAtIK and AimIK are managed by AvatarAimingSystem;
                // we do not turn these on here.
                aaSystem.enabled = true;
            }
            gameObject.AddComponent<DesktopPlayerMovement>();
        }

        /// <summary>
        /// If and only if HideControllers is true (when a VR player is playing), the VR controllers
        /// will not be visualized together with the hands of the player. Apparently, this
        /// hiding/showing must be run at each frame and, hence, we need to put this code into
        /// an Update() method.
        /// </summary>
        //private void Update()
        //{
        //    if (SceneSettings.InputType == PlayerInputType.VRPlayer && Player.instance != null)
        //    {
        //        foreach (Hand hand in Player.instance.hands)
        //        {
        //            if (HideVRControllers)
        //            {
        //                hand.HideController();
        //                hand.SetSkeletonRangeOfMotion(EVRSkeletalMotionRange.WithoutController);
        //            }
        //            else
        //            {
        //                hand.ShowController();
        //                hand.SetSkeletonRangeOfMotion(EVRSkeletalMotionRange.WithController);
        //            }
        //        }
        //    }
        //}
    }
}
