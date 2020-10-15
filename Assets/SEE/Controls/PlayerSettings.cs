using OdinSerializer;
using System;
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
        }

        [Tooltip("What kind of player type should be enabled.")]
        [OdinSerialize]
        public PlayerInputType playerInputType = PlayerInputType.Desktop;

        [Header("VR specific settings (relevant only for VR players)")]
        [Tooltip("Whether the VR controllers should be hidden.")]
        public bool HideVRControllers = false;

        [Tooltip("Whether hints should be shown for controllers.")]
        public bool ShowControllerHints = false;

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
                XRSettings.enabled = playerInputType == PlayerInputType.VR;
            }
            catch (Exception e)
            {
                Debug.LogWarningFormat("VR enabling/disabling issue: {0}", e);
            }

            Debug.LogFormat("Player input type: {0}\n", playerInputType.ToString());

            SetActive("DesktopPlayer", playerInputType == PlayerInputType.Desktop);
            SetActive("VRPlayer", playerInputType == PlayerInputType.VR);
            SetActive("InControl", playerInputType == PlayerInputType.TouchGamepad);

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