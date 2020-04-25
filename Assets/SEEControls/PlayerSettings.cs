using System;
using UnityEngine;
using UnityEngine.XR;
using Valve.VR.InteractionSystem;

namespace SEE.Controls
{
    /// <summary>
    /// Allows a user to specify various settings of a player. 
    /// This script is assumed to be attached to a game object
    /// containing child objects named "VRPlayer" or "NonVRPlayer".
    /// The game objects named "VRPlayer" are assumed to be a SteamVR
    /// Player rig. Child objects named "NonVRPlayer" are camera rigs
    /// for non-VR environments (desktop with keyboard and mouse).
    /// </summary>
    public class PlayerSettings : MonoBehaviour
    {
        public enum PlayerInputType
        {
            Desktop,  // player for desktop and mouse input
            Touch,    // player for touch devices
            VR,       // player for virtual reality devices
        }

        [Tooltip("What kind of player type should be enabled.")]
        public PlayerInputType playerInputType = PlayerInputType.Desktop;

        [Tooltip("Whether the VR controllers should be hidden (relevant only for VR players).")]
        public bool HideVRControllers = false;

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
                Debug.LogWarningFormat("VR enabling/disabling issue: {0}\n", e);
            }
            SetActive("DesktopPlayer", playerInputType == PlayerInputType.Desktop);
            SetActive("VRPlayer",      playerInputType == PlayerInputType.VR);
            SetActive("TouchPlayer",   playerInputType == PlayerInputType.Touch);
            SetActive("InControl",     playerInputType == PlayerInputType.Touch);
        }

        /// <summary>
        /// Enables or disables a game object with the given <paramref name="name"/>.
        /// </summary>
        /// <param name="name">name of the object to be enabled/disabled</param>
        /// <param name="activate">whether to enable or disable the object</param>
        private void SetActive(string name, bool activate)
        {
            GameObject player = GameObject.Find(name);
            if (player != null)
            {
                player.SetActive(activate);
                Debug.LogFormat("Game object {0} {1}.\n", player.name, activate ? "enabled" : "disabled");
            }
            else
            {
                Debug.LogFormat("No game object named {0} found.\n", name);
            }
        }

        /// <summary>
        /// If and only if HideControllers is true, the controllers will not be shown
        /// together with the hands. Apparently, this hiding/showing must be run at each frame
        /// and, hence, we need to put this code into an Update() method.
        /// </summary>
        private void Update()
        {
            if (playerInputType == PlayerInputType.VR)
            {                
                foreach (var hand in Player.instance.hands)
                {
                    if (HideVRControllers)
                    {
                        hand.HideController();
                        hand.SetSkeletonRangeOfMotion(Valve.VR.EVRSkeletalMotionRange.WithoutController);
                    }
                    else
                    {
                        hand.ShowController();
                        hand.SetSkeletonRangeOfMotion(Valve.VR.EVRSkeletalMotionRange.WithController);
                    }
                }
            }
        }
    }
}
