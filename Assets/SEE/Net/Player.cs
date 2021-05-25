using SEE.Utils;
using UnityEngine;

namespace SEE.Net
{
    /// <summary>
    /// The player synchronizes the player transform with the camera transform.
    /// </summary>
    public class Player : MonoBehaviour
    {
        /// <summary>
        /// The transform of the main camera.
        /// </summary>
        private Transform cameraTransform;

        /// <summary>
        /// Initializes the player prefab or destroys this script, if this client is not
        /// the owner, so that the transform is not synchronized with the main camera of
        /// the other client.
        /// </summary>
        private void Start()
        {
            if (GetComponent<ViewContainer>().IsOwner())
            {
                if (MainCamera.Camera.transform == null)
                {
                    throw new System.NullReferenceException("Main camera must not be null!");
                }
                cameraTransform = MainCamera.Camera.transform;
                foreach (Transform child in transform)
                {
                    child.gameObject.SetActive(false);
                }
            }
            else
            {
                Destroy(this);
            }
        }

        /// <summary>
        /// Synchronizes transform with camera transform.
        /// </summary>
        private void Update()
        {
            transform.position = cameraTransform.position;
            transform.rotation = cameraTransform.rotation;
        }
    }
}
