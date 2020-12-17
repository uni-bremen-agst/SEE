using System;
using System.Linq;
using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Utilities;
using OdinSerializer;
using SEE.DataModel;
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
    /// or gamepads using InControl, or (3) virtual reality.
    /// </summary>
    public class PlayerSettings : MonoBehaviour
    {
        /// <summary>
        /// What kind of input devices the player uses.
        /// The order must be consistent with <see cref="PlayerName"/>.
        /// </summary>
        public enum PlayerInputType
        {
            Desktop = 0,      // player for desktop and mouse input
            TouchGamepad = 1, // player for touch devices or gamepads using InControl
            VR = 2,           // player for virtual reality devices
            HoloLens = 3,     // player for mixed reality devices
            None = 4,         // no player at all
        }

        /// <summary>
        /// A mapping from PlayerInputType onto the names of the player game objects.
        /// The order must be consistent with <see cref="PlayerInputType"/>.
        /// </summary>
        public static readonly string[] PlayerName = {
            "DesktopPlayer", // Desktop
            "InControl",     // TouchGamepad
            "VRPlayer",      // VR          
            "MRPlayer",      // HoloLens
            "No Player",     // None
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
                    if (cityCollection.TryGetComponentOrLog(out GridObjectCollection grid))
                    {
                        GameObject[] cities = GameObject.FindGameObjectsWithTag(Tags.CodeCity);
                        foreach (GameObject city in cities)
                        {
                            city.transform.localScale *= CityScalingFactor;
                            // City needs to be parented to collection to be organized by it
                            city.transform.parent = cityCollection.transform;
                        }

                        // To avoid overlaps, set cell width to maximum length of code cities
                        grid.CellWidth = cities.Select(x => x.transform.localScale.MaxComponent()).Max();
                        grid.UpdateCollection();
                    } 
                }
            }            
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