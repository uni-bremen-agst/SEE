using SEE.Controls.Devices;
using UnityEngine;

namespace SEE.Controls
{
    public class Actor : MonoBehaviour
    {
        [Tooltip("The camera of this player.")]
        public Camera mainCamera;

        [Tooltip("The device from which to read the input for speed.")]
        public Throttle throttleDevice;

        [Tooltip("The device from which to retrieve the boost for movements. The boost amplifies speed.")]
        public Boost boostDevice;

        [Tooltip("The device from which to read the input for the direction of movements.")]
        public Direction directionDevice;

        [Tooltip("The device from which to read the viewpoint.")]
        public Viewpoint viewpointDevice;

        [Tooltip("The device from which to read selection input.")]
        public Selection selectionDevice;

        [Tooltip("The action applied to move the camera.")]
        public CameraAction cameraAction;

        [Tooltip("The action applied to select an object.")]
        public SelectionAction selectionAction;

        private void Start()
        {
            CameraSetup();
            SelectionSetup();
        }

        private void SelectionSetup()
        {
            if (selectionAction == null)
            {
                Debug.LogError("Selection action must be set.\n");
            }
            else
            {
                if (selectionDevice == null)
                {
                    Debug.LogWarning("Selection device not set.\n");
                    selectionDevice = gameObject.AddComponent<NullSelection>();
                }
                selectionAction.SelectionDevice = selectionDevice;
                selectionAction.MainCamera = mainCamera;
            }
        }

        private void CameraSetup()
        {
            if (cameraAction == null)
            {
                Debug.LogError("Camera action must be set.\n");
            }
            else
            {
                if (throttleDevice == null)
                {
                    Debug.LogWarning("Throttle device not set.\n");
                    cameraAction.ThrottleDevice = gameObject.AddComponent<NullThrottle>();
                }
                cameraAction.ThrottleDevice = throttleDevice;
                if (directionDevice == null)
                {
                    Debug.LogWarning("Direction device not set.\n");
                    cameraAction.DirectionDevice = gameObject.AddComponent<NullDirection>();
                }
                cameraAction.DirectionDevice = directionDevice;
                if (viewpointDevice == null)
                {
                    Debug.LogWarning("Viewpoint device not set.\n");
                    cameraAction.ViewpointDevice = gameObject.AddComponent<NullViewpoint>();
                }
                cameraAction.ViewpointDevice = viewpointDevice;
                if (boostDevice == null)
                {
                    Debug.LogWarning("Boost device not set.\n");
                    cameraAction.BoostDevice = gameObject.AddComponent<NullBoost>();
                }
                cameraAction.BoostDevice = boostDevice;
            }
        }
    }
}