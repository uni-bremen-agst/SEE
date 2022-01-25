// Code from http://wiki.unity3d.com/index.php?title=CameraFacingBillboard
// Credits go to Neil Carter (NCarter)

using SEE.Utils;
using UnityEngine;

namespace SEE.GameObjects
{
    /// <summary>
    /// This script can be added to a Canvas GameObject to make it always face the main camera.
    /// It is a component of the prefab ScrollableTextWindow.
    /// </summary>
    public class CanvasFaceCamera : MonoBehaviour
    {
        /// <summary>
        /// Main camera of the scene.
        /// </summary>
        private Transform mainCamera;

        /// <summary>
        /// Sets <see cref="mainCamera"/> to the transform of <see cref="MainCamera.Camera"/>
        /// if there is such a camera. Otherwise registers this component to be informed
        /// when the camera becomes available (via <see cref="OnCameraAvailable(Camera)"/>
        /// and disables itself until then.
        /// </summary>
        private void Start()
        {
            Camera camera = MainCamera.GetCameraNowOrLater(OnCameraAvailable);
            if (camera)
            {
                mainCamera = camera.transform;
            }
            else
            {
                // Disable until we have a camera.
                enabled = false;
            }
        }

        /// <summary>
        /// A delegate to be called when a camera is available.
        /// </summary>
        /// <param name="camera">the availabe camera</param>
        private void OnCameraAvailable(Camera camera)
        {
            mainCamera = camera.transform;
            enabled = true;
        }

        /// <summary>
        /// Rotates this game object towards <see cref="mainCamera"/>.
        /// </summary>
        private void LateUpdate()
        {
            Quaternion rotation = mainCamera.localRotation;
            transform.LookAt(transform.position + rotation * Vector3.forward, rotation * Vector3.up);
        }
    }
}
