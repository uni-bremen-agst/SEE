// Code inspired by https://github.com/Firnox/Billboarding/blob/main/Billboard.cs

using SEE.Utils;
using UnityEngine;

namespace SEE.GO
{
    /// <summary>
    /// This script can be added to any game object to make it always face
    /// the main camera. It supports locking of all three axes.
    /// </summary>
    public class FaceCamera : MonoBehaviour
    {
        /// <summary>
        /// Main camera of the scene.
        /// </summary>
        private Transform mainCamera;

        /// <summary>
        /// The original rotation the game object had at <see cref="Awake"/>.
        /// This will be used for locking.
        /// </summary>
        private Vector3 originalRotation;

        /// <summary>
        /// Whether to lock rotation around the x axis.
        /// </summary>
        [Header("Lock Rotation")]
        [SerializeField]
        private bool lockX;
        /// <summary>
        /// Whether to lock rotation around the y axis.
        /// </summary>
        [SerializeField]
        private bool lockY;

        /// <summary>
        /// Whether to lock rotation around the z axis.
        /// </summary>
        [SerializeField]
        private bool lockZ;

        /// <summary>
        /// Sets <see cref="originalRotation"/> to the current rotation
        /// of the game object.
        /// </summary>
        private void Awake()
        {
            originalRotation = transform.rotation.eulerAngles;
        }

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
        /// Sets <see cref="mainCamera"/> and enables this component.
        /// </summary>
        /// <param name="camera">The availabe camera.</param>
        private void OnCameraAvailable(Camera camera)
        {
            if (camera == null)
            {
                Debug.LogError($"No main camera found. {name}.{nameof(FaceCamera)} remains disabled.");
                return;
            }
            mainCamera = camera.transform;
            enabled = true;
        }

        /// <summary>
        /// Rotates this game object towards <see cref="mainCamera"/>.
        /// </summary>
        private void LateUpdate()
        {
            if (mainCamera == null)
            {
                // Although our code makes sure the camera is available, during shutdown
                // the camera might have been destroyed already while this component
                // is still active.
                return;
            }

            {
                // Rotate such that the front of game objects looks at the camera.
                Quaternion rotation = mainCamera.rotation;
                transform.LookAt(transform.position + rotation * Vector3.forward, rotation * Vector3.up);
            }

            {
                // Modify the rotation in Euler space to lock certain dimensions.
                Vector3 rotation = transform.rotation.eulerAngles;
                if (lockX) { rotation.x = originalRotation.x; }
                if (lockY) { rotation.y = originalRotation.y; }
                if (lockZ) { rotation.z = originalRotation.z; }
                transform.rotation = Quaternion.Euler(rotation);
            }
        }
    }
}
