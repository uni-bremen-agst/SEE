// Code from http://wiki.unity3d.com/index.php?title=CameraFacingBillboard
// Credits go to Neil Carter (NCarter)

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
            if (Camera.allCamerasCount > 1)
            {
                Debug.LogWarning("There is more than one camera in the scene.\n");
            }
            mainCamera = Camera.main.transform;
        }

        private void LateUpdate()
        {
            Quaternion rotation = mainCamera.localRotation;
            transform.LookAt(transform.position + rotation * Vector3.forward, rotation * Vector3.up);
        }
    }
}
