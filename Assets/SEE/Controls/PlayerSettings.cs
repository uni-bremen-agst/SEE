
using System;
using System.Collections.Generic;
using System.Linq;
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
using UnityEngine;
using UnityEngine.XR;
using Valve.VR;
using Valve.VR.InteractionSystem;
using Hand = Valve.VR.InteractionSystem.Hand;

namespace SEE.Controls
{
    /// <summary>
    /// Allows a user to select the kind of environment in which the game
    /// runs: (1) desktop with keyboard and mouse input, (2) touch devices 
    /// or gamepads using InControl, (3) virtual reality, or (4) augmented
    /// reality.
    /// </summary>
    public class PlayerSettings : MonoBehaviour
    {
        [Tooltip("What kind of player type should be enabled.")]
        [OdinSerialize]
        public PlayerInputType playerInputType = PlayerInputType.DesktopPlayer;

        [Header("VR specific settings (relevant only for VR players)")]

        [Tooltip("Whether the VR controllers should be hidden.")]
        public bool HideVRControllers = false;

        [Tooltip("Whether hints should be shown for controllers.")]
        public bool ShowControllerHints = false;

        [Header("HoloLens specific settings (relevant only for HoloLens players)")] 
        [Tooltip("Which scale shall be used for HoloLens players.")]
        public ExperienceScale experienceScale = ExperienceScale.Seated;

        [Tooltip("The factor by which code cities should be scaled on startup."), OdinSerialize, Min(0.01f)]
        public float CityScalingFactor = 1f;

        [Tooltip("Whether eye gaze should trigger hovering actions, such as node labels.")]
        public bool EyeGazeHover = true;

        [Range(0, 20)]
        [Tooltip("The time in seconds after which staring at an object triggers its hovering action.")]
        public float EyeStareDelay = 1;

        /// <summary>
        /// The game object representing the active local player, that is, the player 
        /// executing on this local instance of Unity.
        /// </summary>
        public static GameObject LocalPlayer
        {
            get;
            private set;
        }

        /// <summary>
        /// The cached player settings within this local instance of Unity.
        /// Will be updated by <see cref="GetPlayerSettings"/> on its first call.
        /// </summary>
        private static PlayerSettings localPlayerSettings;

        /// <summary>
        /// The cached player input type within this local instance of Unity.
        /// Will be updated by <see cref="GetInputType"/> upon its first call.
        /// </summary>
        private static PlayerInputType localPlayerInputType = PlayerInputType.None;

        /// <summary>
        /// The player input type within this local instance of Unity.
        /// </summary>
        /// <returns>player input type</returns>
        public static PlayerInputType GetInputType()
        {
            if (localPlayerInputType == PlayerInputType.None)
            {
                localPlayerInputType = GetPlayerSettings().playerInputType;
            }
            return localPlayerInputType;
        }

        /// <summary>
        /// The player settings within this local instance of Unity.
        /// </summary>
        /// <returns>player settings</returns>
        public static PlayerSettings GetPlayerSettings()
        {
            if (localPlayerSettings == null)
            {
                localPlayerSettings = FindObjectOfType<PlayerSettings>();
            }
            return localPlayerSettings;
        }

        /// <summary>
        /// Depending on the user's selection, turns VR mode on or off and activates/deactivates
        /// the game objects representing the player in the scene.
        /// </summary>
        private void Awake()
        {
            Debug.LogFormat("Player input type: {0}\n", playerInputType.ToString());
            VRStatus.Enable(playerInputType == PlayerInputType.VRPlayer || playerInputType == PlayerInputType.HoloLensPlayer);
            LocalPlayer = CreatePlayer(playerInputType);
        }

        /// <summary>
        /// Creates the kind of player required for the given <paramref name="playerInputType"/>.
        /// For some players, additional game objects and/or components will be added to the
        /// scene required for the particular player to work correctly.
        /// </summary>
        /// <param name="playerInputType">the kind of environment the player is to run</param>
        /// <returns>new player for given <paramref name="playerInputType"/></returns>
        private GameObject CreatePlayer(PlayerInputType playerInputType)
        {
            GameObject player;

            switch (playerInputType)
            {
                case PlayerInputType.DesktopPlayer:
                    {
                        string TableName = "Table";

                        GameObject table = GameObject.Find(TableName);
                        if (table == null)
                        {
                            throw new Exception($"No game object named {TableName} found.");
                        }
                        else
                        {
                            player = PlayerFactory.CreateDesktopPlayer(table.GetComponent<SEE.GO.Plane>());
                        }
                    }
                    break;
                case PlayerInputType.VRPlayer:
                    {
                        player = PlayerFactory.CreateVRPlayer();
                        SetupVR(player);                        
                    }
                    break;
                case PlayerInputType.HoloLensPlayer:
                    {                        
                        SetupMixedReality();
                        player = PlayerFactory.CreateVRPlayer();
                    }
                    break;
                case PlayerInputType.TouchGamepadPlayer:
                    player = PlayerFactory.CreateTouchGamepadPlayer();
                    break;
                default:
                    throw new NotImplementedException($"Unhandled case {playerInputType}.");
            }
            player.transform.position = new Vector3(0, 1.26f, -2.75f);
            return player;
        }

        private static void SetupVR(GameObject player)
        {
            string FloorName = "Floor";

            GameObject floor = GameObject.Find(FloorName);
            if (floor == null)
            {
                throw new Exception($"No game object named {FloorName} found.");
            }
            else
            {
                {
                    // Create Teleporting game object
                    UnityEngine.Object teleportingPrefab = Resources.Load<GameObject>("Prefabs/Players/Teleporting");
                    UnityEngine.Assertions.Assert.IsNotNull(teleportingPrefab);
                    GameObject teleporting = GameObject.Instantiate(teleportingPrefab) as GameObject;
                    UnityEngine.Assertions.Assert.IsNotNull(teleporting);
                    teleporting.name = "Teleporting";
                }
                {
                    // Attach TeleportArea to floor
                    // The TeleportArea replaces the material of the game object it is attached to
                    // it into a transparent material. This way the game object becomes invisible.
                    // For this reason, we will clone the floor move the cloned floor slightly above 
                    // its origin and then attached the TeleportArea to the cloned floor.
                    Vector3 position = floor.transform.position;
                    position.y += 0.01f;
                    GameObject clonedFloor = Instantiate(floor, position, floor.transform.rotation);
                    clonedFloor.AddComponent<TeleportArea>();
                }
                {
                    // Assign the VR camera to the chart manager so that charts can move along with the camera.
                    string chartManagerName = "ChartManager";
                    GameObject chartManager = GameObject.Find(chartManagerName);
                    if (chartManager)
                    {
                        ChartPositionVr chartPosition = chartManager.GetComponentInChildren<ChartPositionVr>();
                        if (chartPosition)
                        {
                            chartPosition.CameraTransform = player.GetComponentInChildren<Camera>().transform;
                            Debug.Log($"VR camera of {player.name} successfully assigned to {chartManagerName}.\n");
                        }
                        else
                        {
                            Debug.Log($"{chartManagerName} has no component ChartPositionVr.\n");
                        }
                    }
                    else
                    {
                        Debug.Log($"No {chartManagerName} found.\n");
                    }
                }
            }
        }

        private void Start()
        {
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
        }

        /// <summary>
        /// Enables mixed reality capabilities in the scene, including the Mixed Reality Toolkit.
        /// </summary>
        private void SetupMixedReality()
        {
            {
                // Add a MixedRealityToolkit to the scene
                GameObject mrtk = new GameObject("MixedRealityToolkit");
                mrtk.AddComponent<MixedRealityToolkit>();
            }
            {
                // Create HoloLensAppBar from prefab
                UnityEngine.Object appBarPrefab = Resources.Load<GameObject>("Prefabs/HoloLensAppBar.prefab");
                GameObject appBar = Instantiate(appBarPrefab) as GameObject;
                UnityEngine.Assertions.Assert.IsNotNull(appBar);
            }
            {
                // Add a city collection
                GameObject cityCollection = new GameObject("CityCollection");
                cityCollection.AddComponent<GridObjectCollection>();
            }
             
            // Hide all decoration to improve performance
            GameObject.FindGameObjectsWithTag(Tags.Decoration).ForEach(go => go.SetActive(false));

            {
                // Set selected experience scale 
                MixedRealityToolkit.Instance.ActiveProfile.TargetExperienceScale = experienceScale;
                
                Debug.Log($"Current HoloLens scale: {experienceScale.ToString()}\n");
                if (experienceScale == ExperienceScale.Seated || experienceScale == ExperienceScale.OrientationOnly)
                {
                    // Position and scale planes and CodeCities accordingly using CityCollection grid
                    GameObject cityCollection = GameObject.Find("CityCollection").AssertNotNull("CityCollection");

                    if (cityCollection.TryGetComponentOrLog(out GridObjectCollection grid))
                    {

                        GameObject[] cities = GameObject.FindGameObjectsWithTag(Tags.CodeCity);

                        foreach (GameObject city in cities)
                        {
                            // Scale city by given factor, and reset position to origin
                            city.transform.localScale *= CityScalingFactor;
                            // City needs to be parented to collection to be organized by it
                            city.transform.parent = cityCollection.transform;
                            
                            AddInteractions(city);
                            AppBarCityConfiguration(city);
                        }

                        SetGridCellWidth(grid, cities);
                    }
                }
            }

            #region Local Methods

            //Sets the width of the Grid containing the cities
            void SetGridCellWidth(GridObjectCollection grid, IEnumerable<GameObject> cities)
            {
                // To avoid overlaps, set cell width to maximum length of code cities
                grid.CellWidth = cities.Max(x => x.transform.localScale.MaxComponent());
                grid.UpdateCollection();
            }

            // Adds AppBar and ObjectManipulator components to City
            void AddInteractions(GameObject city)
            {
                city.AddComponent<AppBarInteractableObject>();
                city.AddComponent<ObjectManipulator>();
            }

            void AppBarCityConfiguration(GameObject city)
            {
                BoundsControl boundsControl = city.AddComponent<BoundsControl>();
                boundsControl.BoundsControlActivation = Microsoft.MixedReality.Toolkit.UI.BoundsControlTypes
                                                                 .BoundsControlActivationType.ActivateManually;
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
