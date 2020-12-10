using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.UI;
using Microsoft.MixedReality.Toolkit.UI.BoundsControl;
using Microsoft.MixedReality.Toolkit.Utilities;
using OdinSerializer;
using SEE.DataModel;
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
    /// or gamepads using InControl, or (3) virtual reality.
    /// </summary>
    public class PlayerSettings : MonoBehaviour
    {
        /// <summary>
        /// What kind of input devices the player uses.
        /// </summary>
        public enum PlayerInputType
        {
            Desktop = 0,      // player for desktop and mouse input
            TouchGamepad, // player for touch devices or gamepads using InControl
            VR,           // player for virtual reality devices
            HoloLens,     // player for mixed reality devices
            None,         // no player at all
        }

        /// <summary>
        /// A mapping from PlayerInputType onto the names of the player game objects.
        /// </summary>
        static readonly string[] PlayerName = new[] {
            "DesktopPlayer",
            "VRPlayer",
            "InControl",
            "MRPlayer",
            "No Player",
            };

        [Tooltip("What kind of player type should be enabled.")]
        [OdinSerialize]
        public PlayerInputType playerInputType = PlayerInputType.Desktop;

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

        /// <summary>
        /// The game object representing the active local player, that is, the player 
        /// executing on this local instance of Unity.
        /// </summary>
        [HideInInspector]
        public static GameObject LocalPlayer
        {
            get;
            private set;
        }

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
                localPlayerInputType = FindObjectOfType<PlayerSettings>().playerInputType;
            }
            return localPlayerInputType;
        }

        /// <summary>
        /// Depending on the user's selection, turns VR mode on or off and activates/deactivates
        /// the game objects representing the player in the scene.
        /// </summary>
        private void Awake()
        {
            // We have to explicitly disable VR if the user wants us to. Otherwise the
            // mouse positions will be wrong if VR is enabled and a head-mounted display (HMD)
            // is connected. That seems to be a bug.
            try
            {
                XRSettings.enabled = playerInputType == PlayerInputType.VR || playerInputType == PlayerInputType.HoloLens;
            }
            catch (Exception e)
            {
                Debug.LogWarningFormat("VR enabling/disabling issue: {0}", e);
            }

            if (playerInputType != PlayerInputType.VR)
            {
                DisableSteamVRTeleporting();
            }

            Debug.LogFormat("Player input type: {0}\n", playerInputType.ToString());

            SetActive(PlayerName[(int)PlayerInputType.Desktop], playerInputType == PlayerInputType.Desktop);
            SetActive(PlayerName[(int)PlayerInputType.VR], playerInputType == PlayerInputType.VR);
            SetActive(PlayerName[(int)PlayerInputType.TouchGamepad], playerInputType == PlayerInputType.TouchGamepad);
            SetMixedReality(playerInputType == PlayerInputType.HoloLens);
            SetLocalPlayer(PlayerName[(int)playerInputType]);
        }

        /// <summary>
        /// Disabbles all TeleportAreas and Teleports (SteamVR).
        /// </summary>
        private void DisableSteamVRTeleporting()
        {
            foreach (TeleportArea area in UnityEngine.Object.FindObjectsOfType<TeleportArea>())
            {
                area.gameObject.SetActive(false);
            }
            foreach (Teleport port in UnityEngine.Object.FindObjectsOfType<Teleport>())
            {
                port.gameObject.SetActive(false);
            }
        }

        private void Start()
        {
            // Turn off controller hints if requested in the user settings.
            if (playerInputType == PlayerInputType.VR && !ShowControllerHints)
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
        /// Enables or disables mixed reality capabilities, including the Mixed Reality Toolkit.
        /// </summary>
        /// param name = "isActive" > If true, mixed reality capabilities are enabled, otherwise they will be disabled.</param>
        private void SetMixedReality(bool isActive)
        {
            SetActive(PlayerName[(int)PlayerInputType.HoloLens], isActive);
            SetActive("MixedRealityToolkit", isActive);
            SetActive("CityCollection", isActive);
            
            // Disable Teleporting to avoid conflict with MRTK
            SetActive("Teleporting", !isActive);
            SetActive("TeleportArea", !isActive);
            
            // Hide all decoration to improve performance
            GameObject.FindGameObjectsWithTag(Tags.Decoration).ForEach(go => go.SetActive(!isActive));

            if (!isActive)
            {
                MixedRealityToolkit.SetInstanceInactive(MixedRealityToolkit.Instance);
            }
            else
            {
                // Set selected experience scale 
                MixedRealityToolkit.Instance.ActiveProfile.TargetExperienceScale = experienceScale;
                
                if (experienceScale == ExperienceScale.Seated || experienceScale == ExperienceScale.OrientationOnly)
                {
                    // Position and scale planes and CodeCities accordingly using CityCollection grid
                    GameObject cityCollection = GameObject.Find("CityCollection").AssertNotNull("CityCollection");
                    UnityEngine.Assertions.Assert.IsTrue(cityCollection.TryGetComponent(out GridObjectCollection grid));

                    GameObject[] citiesWithoutShell = GameObject.FindGameObjectsWithTag(Tags.CodeCity);

                    GameObject[] cities = new GameObject[citiesWithoutShell.Length];


                    for (int i = 0; i< citiesWithoutShell.Length; i++)
                    {
                        cities[i] = AddCodeCityShell(citiesWithoutShell[i]);

                        GameObject city = cities[i];

                        SetCityScale(city, cityCollection.transform, CityScalingFactor);                     

                        AddMixedRealityGameObjectInteractions(city);
                        AppBarCityConfiguration(city);

                        PrepareBoundsControlForHololens2(city.GetComponent<BoundsControl>());
                    }

                    SetGridCellWitdth(grid, cities);
                }
            }

            #region Local Methods
            //Scales the city by factor and pretend it to collection 
            void SetCityScale(GameObject city, Transform cityCollectionTransform, float cityScaleFactor)
            {
                city.transform.localScale *= cityScaleFactor;
                // City needs to be parented to collection to be organized by it
                city.transform.parent = cityCollectionTransform;
            }

            //Sets the width of the Grid containing the cities
            void SetGridCellWitdth(GridObjectCollection grid, GameObject[] cities)
            {
                // To avoid overlaps, set cell width to maximum length of code cities
                grid.CellWidth = cities.Max(x => x.transform.localScale.MaxComponent());
                grid.UpdateCollection();
            }

            //Adds MixedRealityGameObjectInteraction and ObjectManipulator Script to City
            void AddMixedRealityGameObjectInteractions(GameObject city)
            {
                city.AddComponent<MixedRealityGameObjectInteractions>();
                city.AddComponent<ObjectManipulator>();
            }

            void AppBarCityConfiguration(GameObject city)
            {
                BoundsControl boundsControl = city.AddComponent<BoundsControl>();
                boundsControl.BoundsControlActivation = Microsoft.MixedReality.Toolkit.UI.BoundsControlTypes.BoundsControlActivationType.ActivateManually;
            }

            //Hololens2 BoundsControl
            void PrepareBoundsControlForHololens2(BoundsControl boundsControl)
            {
                //TODO: Figure out how to get materials like "BoundingBox.mat" without inspector

                /**
                BoxDisplayConfiguration boxConfiguration = boundsControl.BoxDisplayConfig;
                boxConfiguration.BoxMaterial = [Assign BoundingBox.mat];
                boxConfiguration.BoxGrabbedMaterial = [Assign BoundingBoxGrabbed.mat];
                ScaleHandlesConfiguration scaleHandleConfiguration = boundsControl.ScaleHandlesConfig;
                scaleHandleConfiguration.HandleMaterial = [Assign BoundingBoxHandleWhite.mat];
                scaleHandleConfiguration.HandleGrabbedMaterial = [Assign BoundingBoxHandleBlueGrabbed.mat];
                scaleHandleConfiguration.HandlePrefab = [Assign MRTK_BoundingBox_ScaleHandle.prefab];
                scaleHandleConfiguration.HandleSlatePrefab = [Assign MRTK_BoundingBox_ScaleHandle_Slate.prefab];
                scaleHandleConfiguration.HandleSize = 0.016f;
                scaleHandleConfiguration.ColliderPadding = 0.016f;
                RotationHandlesConfiguration rotationHandleConfiguration = boundsControl.RotationHandlesConfig;
                rotationHandleConfiguration.HandleMaterial = [Assign BoundingBoxHandleWhite.mat];
                rotationHandleConfiguration.HandleGrabbedMaterial = [Assign BoundingBoxHandleBlueGrabbed.mat];
                rotationHandleConfiguration.HandlePrefab = [Assign MRTK_BoundingBox_RotateHandle.prefab];
                rotationHandleConfiguration.HandleSize = 0.016f;
                rotationHandleConfiguration.ColliderPadding = 0.016f;
                **/
            }

            //Creats a Container GameObject for Cities
            GameObject AddCodeCityShell(GameObject city)
            {
                GameObject cityShell = new GameObject();
                Bounds bounds;

                cityShell.name = city.name + "Shell";
                cityShell.transform.position = city.transform.position;

                Vector3 citySize = city.Size();
                Vector3 cityCenter = city.GetComponent<Renderer>().bounds.center;

                if (citySize.y < 0.1f)
                {
                    bounds = new Bounds(cityCenter, new Vector3(citySize.x, 0.1f, citySize.z));
                }
                else
                {
                    bounds = new Bounds(cityCenter, citySize);
                }

                city.transform.SetParent(cityShell.transform);

                return cityShell;
            }

            #endregion
        }

        /// <summary>
        /// Enables or disables a game object with the given <paramref name="name" />.
        /// </summary>
        /// <param name="name">name of the object to be enabled/disabled</param>
        /// <param name="activate">whether to enable or disable the object</param>
        private void SetActive(string name, bool activate)
        {
            GameObject player = GameObject.Find(name);
            player?.SetActive(activate);           
        }

        /// <summary>
        /// Sets <see cref="LocalPlayer"/> by retrieving the game object with the given <paramref name="name"/>.
        /// </summary>
        /// <param name="name">name of the player game object</param>
        private static void SetLocalPlayer(string name)
        {
            GameObject player = GameObject.Find(name);
            if (player != null)
            {
                LocalPlayer = player;
            }
            else
            {
                Debug.LogErrorFormat("A player object named {0} to be activated could not be found.", name);
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
            if (playerInputType != PlayerInputType.VR)
            {
                return;
            }

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