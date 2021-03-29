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

        private void Start()
        {
            mainCamera = MainCamera.Camera.transform;
        }

        private void LateUpdate()
        {
            Quaternion rotation = mainCamera.localRotation;
            transform.LookAt(transform.position + rotation * Vector3.forward, rotation * Vector3.up);
        }
    }
}
