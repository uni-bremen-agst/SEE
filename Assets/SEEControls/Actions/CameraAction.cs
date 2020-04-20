using SEE.Controls.Devices;
using UnityEngine;

namespace SEE.Controls
{
    public abstract class CameraAction : MonoBehaviour
    {
        protected Throttle throttleDevice;
        public Throttle ThrottleDevice
        {
            get => throttleDevice;
            set => throttleDevice = value;
        }

        protected Direction directionDevice;
        public Direction DirectionDevice
        {
            get => directionDevice;
            set => directionDevice = value;
        }

        protected Viewpoint viewpointDevice;
        public Viewpoint ViewpointDevice
        {
            get => viewpointDevice;
            set => viewpointDevice = value;
        }

        protected Boost boostDevice;
        public Boost BoostDevice
        {
            get => boostDevice;
            set => boostDevice = value;
        }
    }
}