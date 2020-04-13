using UnityEngine;
using Valve.VR.InteractionSystem;

namespace SEE.Controls
{
    /// <summary>
    /// Allows a user to specify whether the controllers should be shown together
    /// with the hands. This script is assumed to be attached to a SteamVR
    /// Player rig.
    /// </summary>
    public class ShowControllers : MonoBehaviour
    {
        [Tooltip("Whether the controllers should be hidden.")]
        public bool HideControllers = false;

        /// <summary>
        /// If and only if HideControllers is true, the controllers will not be shown
        /// together with the hands.
        /// </summary>
        void Update()
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
