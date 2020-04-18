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
        [Tooltip("Whether VR should be enabled.")]
        public bool EnableVR = true;

        [Tooltip("Whether the controllers should be hidden.")]
        public bool HideControllers = false;

        private void Start()
        {
            Debug.LogFormat("VR is enabled: {0}\n", EnableVR);
            // We have to explicitly disable VR if the user wants us to. Otherwise the
            // mouse positions will be wrong if VR is enabled and a head-mounted display (HMD)
            // is connected. That seems to be a bug.
            XRSettings.enabled = EnableVR;

            foreach (Transform child in transform)
            {
                if (child.name == "VRPlayer")
                {
                    child.gameObject.SetActive(EnableVR);
                }
                else if (child.name == "NonVRPlayer")
                {
                    child.gameObject.SetActive(!EnableVR);
                }
                else
                {
                    Debug.LogFormat("Do not know what to do with the child named '{0}'.\n", child.name);
                }
            }
        }

        /// <summary>
        /// If and only if HideControllers is true, the controllers will not be shown
        /// together with the hands. Apparently, this hiding/showing must be run at each frame
        /// and, hence, we need to put this code into an Update() method.
        /// </summary>
        private void Update()
        {
            if (EnableVR)
            {                
                foreach (var hand in Player.instance.hands)
                {
                    if (HideControllers)
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
