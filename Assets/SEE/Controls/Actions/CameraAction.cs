using SEE.Controls.Devices;
using UnityEngine;

namespace SEE.Controls
{
    /// <summary>
    /// An action that moves and rotates the camera. 
    /// The various devices can be set in the inspector by the user.
    /// </summary>
    public abstract class CameraAction : MonoBehaviour
    {
        private Throttle throttleDevice;
        /// <summary>
        /// The throttle device to speed up the movement.
        /// </summary>
        public Throttle ThrottleDevice
        {
            get => throttleDevice;
            set => throttleDevice = value;
        }

        private Direction directionDevice;
        /// <summary>
        /// The device that yields the direction of the movement.
        /// </summary>
        public Direction DirectionDevice
        {
            get => directionDevice;
            set => directionDevice = value;
        }

        private Viewpoint viewpointDevice;
        /// <summary>
        /// The device that yields the orientation for the viewpoint.
        /// </summary>
        public Viewpoint ViewpointDevice
        {
            get => viewpointDevice;
            set => viewpointDevice = value;
        }

        private Boost boostDevice;
        /// <summary>
        /// The device that provides a boost factor for the movement.
        /// </summary>
        public Boost BoostDevice
        {
            get => boostDevice;
            set => boostDevice = value;
        }
    }
}