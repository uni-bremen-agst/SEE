using Sirenix.Serialization;
using SEE.Game;
using SEE.Game.Charts.VR;
using SEE.GO;
using SEE.Utils;
using System;
using UnityEngine;
using Valve.VR;
using Valve.VR.InteractionSystem;

namespace SEE.Controls
{
    public class SceneSettings : MonoBehaviour
    {
        //----------------------------------------------------------------------------------
        // Names of game objects present in the scene (possibly depending on the environment
        // we are running in).
        //----------------------------------------------------------------------------------

        /// <summary>
        /// The name of the game object where the ChartManager component and his friends are
        /// attached to. It is used for handling the metric charts.
        /// </summary>
        private const string ChartManagerName = "ChartManager";

        //-----------------------------------------------
        // Attributes that can be configured by the user.
        //-----------------------------------------------
        [Tooltip("The position at which to spawn the player initially.")]
        [OdinSerialize]
        public Vector3 PlayerOrigin = Vector3.one;

        [Tooltip("The rotation along the y axis at which to spawn the player initially (in degree).")]
        [OdinSerialize]
        [Range(0, 359)]
        public float PlayerYRotation = 0;

        [Tooltip("The plane the player is to focus initially in the desktop environment.")]
        [OdinSerialize]
        public GO.Plane FocusPlane;

        [Header("VR specific settings (relevant only for VR players)")]

        /// <summary>
        /// The game object representing the ground in the scene. A Unity plane
        /// should generally be attached to it. In the VR  environment, the TeleportArea will be attached to it.
        /// </summary>
        [Tooltip("The ground in the scene. This attribute must be set in VR for determining the teleporting area.")]
        [OdinSerialize]
        public GameObject Ground;

        [Tooltip("Whether the VR controllers should be hidden.")]
        public bool HideVRControllers = false;

        [Tooltip("Whether hints should be shown for controllers.")]
        public bool ShowControllerHints = false;

        [Tooltip("The factor by which code cities should be scaled on startup."), OdinSerialize, Min(0.01f)]
        public float CityScalingFactor = 1f;

        /// <summary>
        /// The name of the game object holding the unique <see cref="SceneSettings"/>
        /// component.
        /// </summary>
        private const string NameOfSceneSettingsGameObject = "Scene Settings";

        /// <summary>
        /// The single instance of <see cref="SceneSettings"/> derived from the
        /// current scene.
        /// </summary>
        private static SceneSettings instance;

        /// <summary>
        /// The scene settings within this local instance of Unity.
        /// </summary>
        /// <returns>scene settings</returns>
        public static SceneSettings Settings()
        {
            return instance;
        }

        /// <summary>
        /// Sets <see cref="instance"/> to the <see cref="SceneSettings"/> component
        /// retrieved from the current scene. Its game object is declared to be kept
        /// alive across scenes.
        ///
        /// Precondition: The scene must contain a game object named
        /// <see cref="NameOfPlayerSettingsGameObject"/> holding a
        /// <see cref="SceneSettings"/> component.
        /// </summary>
        private void SetInstance()
        {
            // Note: instance = FindObjectOfType<SceneSettings>() would not work
            // because FindObjectOfType does not work when changing scenes.

            GameObject seeSettings = GameObject.Find(NameOfSceneSettingsGameObject);
            if (seeSettings == null)
            {
                Debug.LogError($"There is no game object with name {NameOfSceneSettingsGameObject}. This is a fatal error.\n");
            }
            else if (!seeSettings.TryGetComponent(out instance))
            {
                Debug.LogError($"There is no game object with a {typeof(SceneSettings)} component. This is a fatal error.\n");
            }
        }

        private static GameObject localPlayer;
        /// <summary>
        /// The game object representing the active local player, that is, the player
        /// executing on this local instance of Unity.
        /// </summary>
        public static GameObject LocalPlayer
        {
            get
            {
                if (localPlayer == null)
                {
                    localPlayer = instance.CreatePlayer(PlayerSettings.GetInputType());
                }
                return localPlayer;
            }
            private set
            {
                if (LocalPlayer == null)
                {
                    localPlayer = value;
                }
            }
        }

        /// <summary>
        /// Depending on the user's selection, turns VR mode on or off and activates/deactivates
        /// the game objects representing the player in the scene.
        /// </summary>
        private void Awake()
        {
            playerInputType = PlayerSettings.GetInputType();
            Debug.Log($"Player input type: {playerInputType}.\n");
            VRStatus.Enable(playerInputType == PlayerInputType.VRPlayer);
        }

        /// <summary>
        /// The player input type derived from <see cref="PlayerSettings.GetInputType()"/>. This value
        /// is cached here because it will be used in <see cref="Update"/>.
        /// </summary>
        private PlayerInputType playerInputType;

        /// <summary>
        /// If we are running in a VR environment and the user wants us to hide the controllers,
        /// we do so. Caches <see cref="PlayerSettings.GetInputType()"/> in <see cref="playerInputType"/>.
        /// </summary>
        private void Start()
        {
            SetInstance();
            // Turn off VR controller hints if requested in the user settings.
            if (playerInputType == PlayerInputType.VRPlayer && !ShowControllerHints)
            {
                foreach (Hand hand in Player.instance.hands)
                {
                    ControllerButtonHints.HideAllButtonHints(hand);
                    ControllerButtonHints.HideAllTextHints(hand);
                }

                if (Teleport.instance != null)
                {
                    Teleport.instance.CancelTeleportHint();
                }
            }
#if UNITY_EDITOR
            KeyBindings.PrintBindings();
#endif
        }

        /// <summary>
        /// Finds or creates the kind of player required for the given <paramref name="inputType"/>.
        /// For some players, additional game objects and/or components will be added to the
        /// scene required for the particular player to work correctly.
        /// </summary>
        /// <param name="inputType">the kind of environment the player is to run</param>
        /// <returns>new player for given <paramref name="inputType"/>, <c>null</c>
        /// if <paramref name="inputType"/> equals <see cref="PlayerInputType.DesktopPlayer"/></returns>
        [Obsolete("Do not use. This method will be superseded by SEE.Game.Avatars.AvatarAdapter.")]
        private GameObject CreatePlayer(PlayerInputType inputType)
        {
            GameObject player;

            switch (inputType)
            {
                case PlayerInputType.DesktopPlayer:
                    // position and rotation of the local desktop player are set
                    // elsewhere by the player spawner. That is why we can return
                    // this local player immediately.
                    return LocalPlayerForDesktop();
                case PlayerInputType.VRPlayer:
                    {
                        /// FIXME: Move this code to <see cref="SEE.Game.Avatars.AvatarAdapter"/>.
                        player = PlayerFactory.CreateVRPlayer();
                        SetupVR(player, Ground);
                    }
                    break;
                case PlayerInputType.TouchGamepadPlayer:
                    /// FIXME: Move this code to <see cref="SEE.Game.Avatars.AvatarAdapter"/>.
                    player = PlayerFactory.CreateTouchGamepadPlayer();
                    break;
                case PlayerInputType.None: return null; // No player needs to be created
                default:
                    throw new NotImplementedException($"Unhandled case {inputType}.");
            }
            player.transform.position = PlayerOrigin;
            player.transform.rotation = Quaternion.Euler(0, PlayerYRotation, 0);
            return player;

            static GameObject LocalPlayerForDesktop()
            {
                // The local player is holding the main camera. Remote players do not have
                // a camera attached. Hence, we only need to retrieve that camera.

                /// FIXME: This should be the case for all environments as soon as we
                /// migrated <see cref="CreatePlayer(PlayerInputType)"/> to
                /// <see cref="Game.Avatars.AvatarAdapter"/>
                return PlayerSettings.GetInputType() == PlayerInputType.DesktopPlayer ?
                    MainCamera.Camera.gameObject : null;
            }
        }

        /// <summary>
        /// Sets up the scene for playing in an VR environment. This means to instantiate the
        /// Teleporting object and to attach a TeleportArea to the ground plane named <see cref="FloorName"/>.
        /// In addition, the VR camera is assigned to the ChartManager if it exists.
        ///
        /// Precondition: There must be a game object named <see cref="FloorName"/> in the scene, representing
        /// the ground (a Unity Plane would be attached to it).
        /// </summary>
        /// <param name="player">the current VR player</param>
        /// <param name="ground"></param>
        /// <exception cref="Exception">thrown if there is no plane named <see cref="FloorName"/> in the scene</exception>
        private static void SetupVR(GameObject player, GameObject ground)
        {
            if (ground == null)
            {
                throw new InvalidOperationException("A Ground must be assigned in the PlayerSettings. Use the Inspector.");
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
                {
                    // Assign the VR camera to the chart manager so that charts can move along with the camera.
                    GameObject chartManager = GameObject.Find(ChartManagerName);
                    if (chartManager)
                    {
                        ChartPositionVr chartPosition = chartManager.GetComponentInChildren<ChartPositionVr>();
                        if (chartPosition)
                        {
                            chartPosition.enabled = true;
                            chartPosition.CameraTransform = player.GetComponentInChildren<Camera>().transform;
                            Debug.Log($"VR camera of {player.name} successfully assigned to {ChartManagerName}.\n");
                        }
                        else
                        {
                            Debug.LogError($"{ChartManagerName} has no component {nameof(ChartPositionVr)}.\n");
                        }
                    }
                    else
                    {
                        Debug.LogError($"No {ChartManagerName} found.\n");
                    }
                }
            }
        }

        /// <summary>
        /// If and only if HideControllers is true (when a VR player is playing), the VR controllers
        /// will not be visualized together with the hands of the player. Apparently, this
        /// hiding/showing must be run at each frame and, hence, we need to put this code into
        /// an Update() method.
        /// </summary>
        private void Update()
        {
            if (playerInputType == PlayerInputType.VRPlayer)
            {
                foreach (Hand hand in Player.instance.hands)
                {
                    if (HideVRControllers)
                    {
                        hand.HideController();
                        hand.SetSkeletonRangeOfMotion(EVRSkeletalMotionRange.WithoutController);
                    }
                    else
                    {
                        hand.ShowController();
                        hand.SetSkeletonRangeOfMotion(EVRSkeletalMotionRange.WithController);
                    }
                }
            }
        }
    }
}
