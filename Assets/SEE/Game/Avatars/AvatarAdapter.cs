using System;
using System.Collections;
using CrazyMinnow.SALSA;
using Dissonance;
using Dissonance.Audio.Playback;
using SEE.Controls;
using SEE.GO;
using SEE.GO.Menu;
using SEE.Tools.OpenTelemetry;
using SEE.Utils;
using Unity.Netcode;
using UnityEngine;
#if ENABLE_VR
using UnityEngine.Assertions;
using SEE.XR;
using ViveSR.anipal;
using ViveSR.anipal.Lip;
using RootMotion.FinalIK;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;
#endif

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
        /// The transform of the player's head that will be periodically tracked for position and rotation data.
        /// This value is set during VR initialization and used for telemetry tracking.
        /// </summary>
        private Transform headTransformToTrack;

        /// <summary>
        /// The distance from the top of the player's height to his eyes.
        /// </summary>
        private const float playerTopToEyeDistance = 0.14f;

        /// <summary>
        /// If this code is executed for the local player, the necessary player type
        /// for the environment we are currently running on are added to this game object.
        /// </summary>
        private void Start()
        {
            if (IsLocalPlayer)
            {
                // I am the avatar of the local player.
                LocalPlayer.Instance = gameObject;

                if (!gameObject.TryGetComponent(out WindowSpaceManager _))
                {
                    gameObject.AddComponent<WindowSpaceManager>();
                }

                if (User.UserSettings.IsVR)
                {
                    gameObject.AddOrGetComponent<PlayerMenu>();
                    gameObject.AddOrGetComponent<DrawableSurfacesRef>();
                }

                switch (User.UserSettings.Instance.InputType)
                {
                    case PlayerInputType.DesktopPlayer:
                    case PlayerInputType.TouchGamepadPlayer:
                        PrepareLocalPlayerForDesktop();
                        break;
                    case PlayerInputType.VRPlayer:
                        PrepareLocalPlayerForXR();
                        break;
                    default:
                        throw new NotImplementedException($"Unhandled case {User.UserSettings.Instance.InputType}");
                }

                gameObject.name = "Local " + gameObject.name;
            }
            else
            {
                gameObject.name = "Remote " + gameObject.name;
                // Remote players need to be set up for Dissonance and SALSA lip sync.
                StartCoroutine(SetUpSALSA());
            }

            if (gameObject.TryGetComponentOrLog(out NetworkObject net))
            {
                Debug.Log(
                    $"Avatar {gameObject.name} is local player: {net.IsLocalPlayer}. Owner of avatar is server: {net.IsOwnedByServer} or is local client: {net.IsOwner}\n");
            }

            EnableLocalControl(IsLocalPlayer);
        }

        /// <summary>
        /// Enables/disables local control of the aiming system of the avatar.
        /// </summary>
        /// <param name="isLocalPlayer">If true, the aiming system will be locally controlled
        /// (i.e., by the local player) or remotely controlled (i.e., the aiming action
        /// of a remote player are to be replicated on its local representation).</param>
        private void EnableLocalControl(bool isLocalPlayer)
        {
            if (gameObject.TryGetComponentOrLog(out AvatarAimingSystem aimingSystem))
            {
                aimingSystem.IsLocallyControlled = isLocalPlayer;
            }
            if (gameObject.TryGetComponentOrLog(out BodyAnimator bodyAnimator))
            {
                bodyAnimator.IsLocallyControlled = isLocalPlayer;
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
        /// <returns>As to whether to continue.</returns>
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
        /// <param name="playerId">The searched player ID.</param>
        /// <returns>Child of <see cref="dissonanceComms"/> representing <paramref name="playerId"/>.</returns>
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
        /// <returns>Height of a desktop avatar.</returns>
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
        /// Prepares the avatar for a desktop environment by adding a DesktopPlayer prefab
        /// as a child and a <see cref="DesktopPlayerMovement"/> component.
        /// </summary>
        private void PrepareLocalPlayerForDesktop()
        {
            // Set up the desktop player at the top of the player just in front of it.
            GameObject desktopPlayer = PrefabInstantiator.InstantiatePrefab("Prefabs/Players/DesktopPlayer");
            desktopPlayer.name = PlayerInputType.DesktopPlayer.ToString();
            desktopPlayer.transform.SetParent(gameObject.transform);
            desktopPlayer.transform.localPosition =
                new Vector3(0, DesktopAvatarHeight() - playerTopToEyeDistance, 0.3f);
            desktopPlayer.transform.localRotation = Quaternion.Euler(30, 0, 0);

            if (gameObject.TryGetComponentOrLog(out AvatarAimingSystem aaSystem))
            {
                // Note: the two components LookAtIK and AimIK are managed by AvatarAimingSystem;
                // we do not turn these on here.
                aaSystem.enabled = true;
            }

            gameObject.AddComponent<DesktopPlayerMovement>();
        }

        #region ENABLE_VR

#if !ENABLE_VR
        private void PrepareLocalPlayerForXR()
        {
            // intentionally left blank
        }
#else
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
        private const string animatorForVRIKPrefab = "Prefabs/Players/VRIKAnimatedLocomotion";

        /// <summary>
        /// The path of the prefab for the VR camera rig.
        /// </summary>
        private const string vrPlayerRigPrefab = "Prefabs/Players/XRRig";

        /// <summary>
        /// The composite name of the child within <see cref="vrPlayerRigPrefab"/>
        /// representing the head for VRIK.
        /// </summary>
        private const string vrPlayerHeadForVRIK = "Camera Offset/Main Camera/Head";

        /// <summary>
        /// The composite name of the child within <see cref="vrPlayerRigPrefab"/>
        /// representing the left hand for VRIK.
        /// </summary>
        private const string vrPLayerLeftHandForVRIK = XRCameraRigManager.LeftControllerName + "/LeftHand";

        /// <summary>
        /// The composite name of the child within <see cref="vrPlayerRigPrefab"/>
        /// representing the right hand for VRIK.
        /// </summary>
        private const string vrPlayerRightHandForVRIK = XRCameraRigManager.RightControllerName + "/RightHand";

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

            // Now we can instantiate the prefabs for VR that requires that XR is up and running.

            GameObject rig = PrefabInstantiator.InstantiatePrefab(vrPlayerRigPrefab);
            rig.transform.position = gameObject.transform.position;
            VRIKActions.TurnOffAvatarAimingSystem(gameObject);
            VRIKActions.ReplaceAnimator(gameObject, animatorForVRIKPrefab);

            SetupVRIK();

            PrepareLipTracker();
            InitializeVrikRemote();

            PrepareScene();

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
                const string groundName = "Floor";

                GameObject ground = GameObject.Find(groundName);
                if (ground == null)
                {
                    Debug.LogError($"There is no ground object named {groundName}. Teleporting cannot be set up.\n");
                }
                else
                {
                    AddTeleportArea(ground);
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

                PrefabInstantiator.InstantiatePrefab("Prefabs/UI/XRTabletCanvas");
            }

            /// <summary>
            /// Adds a teleport area to the ground plane.
            /// </summary>
            void AddTeleportArea(GameObject ground)
            {
                GameObject teleportArea = PrefabInstantiator.InstantiatePrefab("Prefabs/Players/TeleportArea");
                teleportArea.name = "TeleportArea";
                if (teleportArea.TryGetComponentOrLog(out TeleportationArea teleportationArea)
                    && ground.TryGetComponentOrLog(out Collider collider))
                {
                    if (teleportationArea.colliders.Count == 0)
                    {
                        teleportationArea.colliders.Add(collider);
                    }
                    else
                    {
                        if (teleportationArea.colliders[0] != null)
                        {
                            Debug.LogWarning($"The teleport area {teleportArea.name} already has a collider assigned.\n");
                        }

                        teleportationArea.colliders[0] = collider;
                    }

                    // The teleport area is initially disabled in the prefab.
                    // If it were enabled, the assigned collider would be ignored.
                    // We need to first assign the collider and then enable the teleport area.
                    teleportArea.SetActive(true);
                    teleportationArea.enabled = true;
                }
            }

            // Adds component to VR-Player to sent data from VRIK to all remote clients.
            void InitializeVrikRemote()
            {
                gameObject.AddComponent<VRIKSynchronizer>();
            }

            // Set up FinalIK's VR IK on the avatar.
            void SetupVRIK()
            {
                VRIK vrIK = gameObject.AddOrGetComponent<VRIK>();

                vrIK.solver.spine.headTarget = rig.transform.Find(vrPlayerHeadForVRIK);
                Assert.IsNotNull(vrIK.solver.spine.headTarget);
                vrIK.solver.leftArm.target = rig.transform.Find(vrPLayerLeftHandForVRIK);
                Assert.IsNotNull(vrIK.solver.leftArm.target);
                vrIK.solver.rightArm.target = rig.transform.Find(vrPlayerRightHandForVRIK);
                Assert.IsNotNull(vrIK.solver.rightArm.target);

                headTransformToTrack = vrIK.solver.spine.headTarget;
            }

            // Prepare HTC Facial Tracker
            void PrepareLipTracker()
            {
                Debug.Log("[HTC Facial Tracker] Initialize HTC Facial Tracker...\n");
                SRanipal_API.GetStatus(SRanipal_Lip_v2.ANIPAL_TYPE_LIP_V2, out AnipalStatus status);

                if (status == AnipalStatus.ERROR)
                {
                    Debug.LogWarning("[HTC Facial Tracker] Did you start sr_runtime before?\n");
                    Debug.LogWarning(
                        "[HTC Facial Tracker] Trying to start sr_runtime... This may take some time and can freeze your game!\n");
                    StartCoroutine(PrepareLipFramework());
                }
                // SR_runtime Status is IDLE or WORKING
                else
                {
                    gameObject.AddComponent<SRanipal_Lip_Framework>().EnableLipVersion =
                        SRanipal_Lip_Framework.SupportedLipVersion.version2;
                    AddComponentsForFacialTracker();
                }
            }

            // Coroutine for starting the sr_runtime and preparing the Lip Framework. By default the Lip Framework has the status stop.
            // After initialization the status is either working or error. If the status is working, then scripts are
            // added for the Lip Framework and multiplayer functionality. Otherwise the Lip Framework will be deactivated.
            IEnumerator PrepareLipFramework()
            {
                // Waiting for sr_runtime and lip_framework initialisation.
                gameObject.AddComponent<SRanipal_Lip_Framework>().EnableLipVersion =
                    SRanipal_Lip_Framework.SupportedLipVersion.version2;
                yield return new WaitUntil(() =>
                    SRanipal_Lip_Framework.Status != SRanipal_Lip_Framework.FrameworkStatus.STOP);

                if (SRanipal_Lip_Framework.Status == SRanipal_Lip_Framework.FrameworkStatus.WORKING)
                {
                    AddComponentsForFacialTracker();
                }
                else if (SRanipal_Lip_Framework.Status == SRanipal_Lip_Framework.FrameworkStatus.ERROR)
                {
                    Debug.LogError(
                        "[HTC Facial Tracker] Please check or install SR_Runtime. Maybe your HMD does not support HTC Facial Tracker.\n");
                    gameObject.GetComponent<SRanipal_Lip_Framework>().enabled = false;
                }
            }

            // Adds required components for HTC Facial Tracker
            void AddComponentsForFacialTracker()
            {
                Debug.Log("[HTC Facial Tracker] SR_Runtime found. Adding components...\n");
                gameObject.AddComponent<AvatarBlendshapeExpressions>();

                // Multiplayer functionality for Facialtracker.
                gameObject.AddComponent<BlendshapeExpressionsSynchronizer>();
                Debug.Log("[HTC Facial Tracker] Initialisation complete.\n");
            }
        }

        /// <summary>
        /// Periodically tracks the head transform for local VR players by invoking telemetry logging.
        /// This method ensures that head position and rotation are logged over time during gameplay.
        /// </summary>
        private void Update()
        {
            if (IsLocalPlayer && headTransformToTrack != null)
            {
                TracingHelperService.Instance?.TrackHeadTransformPeriodically(headTransformToTrack, Time.time);
            }
        }
    }
}

#endif

#endregion ENABLE_VR


