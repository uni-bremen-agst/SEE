using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.UI;
using Microsoft.MixedReality.Toolkit.UI.BoundsControl;
using Microsoft.MixedReality.Toolkit.Utilities;
using OdinSerializer;
using SEE.DataModel;
using SEE.Game;
using SEE.Game.Charts.VR;
using SEE.GO;
using SEE.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
#if UNITY_ANDROID
#else
using Valve.VR;
using Valve.VR.InteractionSystem;
#endif

namespace SEE.Controls
{
    public class SceneSettings : MonoBehaviour
    {
        //----------------------------------------------------------------------------------
        // Names of game objects present in the scene (possibly depending on the environment
        // we are running in).
        //----------------------------------------------------------------------------------

        /// <summary>
        /// The name of the game object where the MixedRealityToolkit is attached to.
        /// Used only for the Hololens player.
        /// </summary>
        private const string MixedRealityToolkitName = "MixedRealityToolkit";
        /// <summary>
        /// The name of the game object where the GridObjectCollection is attached to.
        /// Used only for the Hololens player.
        /// </summary>
        private const string CityCollectionName = "CityCollection";
        /// <summary>
        /// The name of the game object which represents the app bar for a CodeCity.
        /// </summary>
        private const string AppBarName = "HoloLensAppBar";
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
        /// should generally be attached to it. Will be deactivated for the Hololens player. In the VR
        /// environment, the TeleportArea will be attached to it.
        /// </summary>
        [Tooltip("The ground in the scene. This attribute must be set in VR for determining the teleporting area.")]
        [OdinSerialize]
        public GameObject Ground;

        [Tooltip("Whether the VR controllers should be hidden.")]
        public bool HideVRControllers = false;

        [Tooltip("Whether hints should be shown for controllers.")]
        public bool ShowControllerHints = false;

        [Header("HoloLens specific settings (relevant only for HoloLens players)")]
        [Tooltip("Which scale shall be used for HoloLens players.")]
        public ExperienceScale ExperienceScale = ExperienceScale.Seated;

        [Tooltip("The factor by which code cities should be scaled on startup."), OdinSerialize, Min(0.01f)]
        public float CityScalingFactor = 1f;

        [Tooltip("Whether eye gaze should trigger hovering actions, such as node labels.")]
        public bool EyeGazeHover = true;

        [Range(0, 20)]
        [Tooltip("The time in seconds after which staring at an object triggers its hovering action.")]
        public float EyeStareDelay = 1;

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

        /// <summary>
        /// The game object representing the active local player, that is, the player
        /// executing on this local instance of Unity.
        /// </summary>
        public static GameObject LocalPlayer { get; private set; }

        /// <summary>
        /// Depending on the user's selection, turns VR mode on or off and activates/deactivates
        /// the game objects representing the player in the scene.
        /// </summary>
        private void Awake()
        {
            playerInputType = PlayerSettings.GetInputType();
            Debug.Log($"Player input type: {playerInputType}.\n");
            VRStatus.Enable(playerInputType == PlayerInputType.VRPlayer || playerInputType == PlayerInputType.HoloLensPlayer);
            LocalPlayer = CreatePlayer(playerInputType);
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
#if UNITY_ANDROID
#else
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
#endif
#if UNITY_EDITOR
            KeyBindings.PrintBindings();
#endif
        }

        /// <summary>
        /// Creates the kind of player required for the given <paramref name="inputType"/>.
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
                /// FIXME: This could should be moved to <see cref="SEE.Game.Avatars.AvatarAdapter"/>.
                case PlayerInputType.DesktopPlayer:
                    /// The code original executed here has now be moved to
                    /// <see cref="SEE.Game.Avatars.AvatarAdapter"/>. The other
                    /// code must be moved there, too.
                    return null;
                case PlayerInputType.VRPlayer:
                    {
                        player = PlayerFactory.CreateVRPlayer();
                        SetupVR(player);
                    }
                    break;
                case PlayerInputType.HoloLensPlayer:
                    {
                        player = PlayerFactory.CreateHololensPlayer();
                        SetupMixedReality();
                    }
                    break;
                case PlayerInputType.TouchGamepadPlayer:
                    player = PlayerFactory.CreateTouchGamepadPlayer();
                    break;
                case PlayerInputType.None: return null; // No player needs to be created
                default:
                    throw new NotImplementedException($"Unhandled case {inputType}.");
            }
            player.transform.position = PlayerOrigin;
            player.transform.rotation = Quaternion.Euler(0, PlayerYRotation, 0);
            return player;
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
        /// <exception cref="Exception">thrown if there is no plane named <see cref="FloorName"/> in the scene</exception>
        private void SetupVR(GameObject player)
        {
            if (Ground == null)
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
                    Vector3 position = Ground.transform.position;
                    position.y += 0.01f;
                    GameObject clonedFloor = Instantiate(Ground, position, Ground.transform.rotation);
#if UNITY_ANDROID
#else
                    clonedFloor.AddComponent<TeleportArea>();
#endif
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
        /// Enables mixed reality capabilities in the scene, including the Mixed Reality Toolkit.
        /// </summary>
        private void SetupMixedReality()
        {
            Destroy(UnityEngine.SceneManagement.SceneManager.GetActiveScene()
                               .GetRootGameObjects().SingleOrDefault(x => x.name == "MixedRealityPlayspace"));

            // Add a MixedRealityToolkit to the scene
            PrefabInstantiator.InstantiatePrefab("Prefabs/MixedRealityToolkit").name = MixedRealityToolkitName;

            // Create HoloLensAppBar from prefab
            PrefabInstantiator.InstantiatePrefab("Prefabs/HoloLensAppBar").name = AppBarName;

            // Add a city collection
            GameObject cityCollection = PrefabInstantiator.InstantiatePrefab("Prefabs/CityCollection");
            cityCollection.name = CityCollectionName;
#if UNITY_ANDROID
#else
            // Hide all decoration to improve performance.
            GameObject.FindGameObjectsWithTag(Tags.Decoration).ForEach(go => go.SetActive(false));
#endif
            {
                // Set selected experience scale.
                MixedRealityToolkit.Instance.ActiveProfile.TargetExperienceScale = ExperienceScale;

                Debug.Log($"Current HoloLens scale: {ExperienceScale}\n");
                if (ExperienceScale == ExperienceScale.Seated || ExperienceScale == ExperienceScale.OrientationOnly)
                {
                    // Position and scale planes and CodeCities accordingly using CityCollection grid
                    if (cityCollection.TryGetComponentOrLog(out GridObjectCollection grid))
                    {
                        GameObject[] cities = GameObject.FindGameObjectsWithTag(Tags.CodeCity);

                        GameObject[] citiesWithContainer = cities.Select(AddCodeCityContainer).ToArray();

                        foreach (GameObject city in citiesWithContainer)
                        {
                            SetCityScale(city, cityCollection.transform, CityScalingFactor);
                            AddInteractions(city);
                            AppBarCityConfiguration(city);
                        }

                        SetGridCellWidth(grid, citiesWithContainer);
                    }
                }
            }

#region Local Methods
            //Scales the city by factor and pretend it to collection
            static void SetCityScale(GameObject cityWitchContainer, Transform cityCollectionTransform, float cityScaleFactor)
            {
                cityWitchContainer.transform.localScale *= cityScaleFactor;
                // City needs to be parented to collection to be organized by it
                cityWitchContainer.transform.parent = cityCollectionTransform;
            }

            //Sets the width of the Grid containing the cities
            static void SetGridCellWidth(GridObjectCollection grid, IEnumerable<GameObject> cities)
            {
                // To avoid overlaps, set cell width to maximum length of code cities
                grid.CellWidth = cities.Max(x => x.transform.localScale.MaxComponent());
                grid.UpdateCollection();
            }

            // Adds AppBar and ObjectManipulator components to City
            static void AddInteractions(GameObject city)
            {
                city.AddComponent<AppBarInteractableObject>();
                city.AddComponent<ObjectManipulator>();
            }

            static void AppBarCityConfiguration(GameObject city)
            {
                BoundsControl boundsControl = city.AddComponent<BoundsControl>();
                boundsControl.BoundsControlActivation = Microsoft.MixedReality.Toolkit.UI.BoundsControlTypes
                                                                 .BoundsControlActivationType.ActivateManually;
            }


            // Creates a Container GameObject for Cities
            static GameObject AddCodeCityContainer(GameObject city)
            {
                GameObject cityContainer = new GameObject { name = city.name + "Container" };
                cityContainer.transform.position = city.transform.position;
                city.transform.SetParent(cityContainer.transform);
                return cityContainer;
            }

#endregion
        }

        /// <summary>
        /// If and only if HideControllers is true (when a VR player is playing), the VR controllers
        /// will not be visualized together with the hands of the player. Apparently, this
        /// hiding/showing must be run at each frame and, hence, we need to put this code into
        /// an Update() method.
        /// </summary>
        private void Update()
        {
#if UNITY_ANDROID
#else
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
#endif
            if (SEEInput.Help())
            {
                KeyBindings.PrintBindings();
            }
        }
    }
}
