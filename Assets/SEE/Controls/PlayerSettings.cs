using System;
using System.Linq;
using Microsoft.MixedReality.Toolkit;
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
            Desktop,      // player for desktop and mouse input
            TouchGamepad, // player for touch devices or gamepads using InControl
            VR,           // player for virtual reality devices
            HoloLens      // player for mixed reality devices
        }

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

        public static PlayerInputType GetInputType()
        {
            PlayerInputType result = FindObjectOfType<PlayerSettings>().playerInputType;
            return result;
        }

        /// <summary>
        /// Depending on the user's selection, turns VR mode on or off and activates/deactivates
        /// the game objects representing the player in the scene.
        /// </summary>
        private void Start()
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

            Debug.LogFormat("Player input type: {0}\n", playerInputType.ToString());

            SetActive("DesktopPlayer", playerInputType == PlayerInputType.Desktop);
            SetActive("VRPlayer", playerInputType == PlayerInputType.VR);
            SetActive("InControl", playerInputType == PlayerInputType.TouchGamepad);
            SetMixedReality(playerInputType == PlayerInputType.HoloLens);

            // Turn off controller hints if requested in the user settings.
            if (!ShowControllerHints)
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

        /**
         * Enables or disables mixed reality capabilities, including the Mixed Reality Toolkit.
         * <param name="isActive">If true, mixed reality capabilities are enabled, otherwise they will be disabled.</param>
         */
        private void SetMixedReality(bool isActive)
        {
            SetActive("MRPlayer", isActive);
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
                // MRTK also changes camera attributes for a better mixed reality experience, so we revert that here
                GameObject.FindWithTag("MainCamera").GetComponent<Camera>()?.Reset();
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

        /// <summary>
        /// Enables or disables a game object with the given <paramref name="name" />.
        /// </summary>
        /// <param name="name">name of the object to be enabled/disabled</param>
        /// <param name="activate">whether to enable or disable the object</param>
        private void SetActive(string name, bool activate)
        {
            GameObject.Find(name)?.SetActive(activate);
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