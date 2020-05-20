using UnityEngine;

namespace SEE.GO
{
    /// <summary>
    /// Rotates the game object this component is attachted to so that it always looks 
    /// to the main camera such that it can be seen from all angles.
    /// </summary>
    public class FacingCamera : MonoBehaviour
    {
        // The time in seconds until the game object is updated again.
        private const float updatePeriod = 0.5f;

        /// <summary>
        /// The minimal distance between the game object and the main camera to become visible.
        /// If the actual distance is below this value, the object will not be visible.
        /// If minimalDistance > maximalDistance, the object will not be visible.
        /// </summary>
        public float minimalDistance = 0.3f;

        /// <summary>
        /// The maximal distance between the game object and the main camera to become visible.
        /// If the actual distance is above this value, the object will not be visible.
        /// If minimalDistance > maximalDistance, the object will not be visible.
        /// </summary>
        public float maximalDistance = 5.0f;

        // Time since the start of the last update period.
        private float timer = 0;

        // The last known position of the main camera.
        private Vector3 lastCameraPosition = Vector3.zero;

        // Vector to describe the rotation of the game object. Needed to show the game object 
        // correctly.
        private static readonly Vector3 rotation = Vector3.up - new Vector3(0, 180, 0);

        // The renderer of the gameObject.
        private Renderer gameObjectRenderer;

        /// <summary>
        /// The camera the gameObject should be facing to.
        /// </summary>
        protected Camera mainCamera;

        protected virtual void Start()
        {
            gameObjectRenderer = gameObject.GetComponentInChildren<Renderer>();
            if (gameObjectRenderer == null)
            {
                Debug.LogErrorFormat("Game object {0} does not have a renderer.\n", gameObject.name);
            }
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                Debug.LogError("There is no camera tagged by MainCamera.\n");
            }
        }

        /// <summary>
        /// Updates the associated game object every update period if the
        /// camera has moved at all since the last update. The associated object will
        /// be rendered if its distance to the main camera does not exceed the
        /// minimalDistance. If it is rendered, it will be rotated to the face the
        /// camera so that it can always be seen.
        /// </summary>
        private void Update()
        {
            timer -= Time.deltaTime;
            if (timer < 0.0f)
            {
                timer = updatePeriod;                
                if (mainCamera.transform.position != lastCameraPosition)
                {
                    // Alternative calculation of difference, which does not quite work on updates, however.
                    // the object's heading vector
                    // Vector3 heading = transform.position - mainCamera.transform.position;
                    // mainCamera.transform.forward is the z axis of the camera
                    // float dist = Mathf.Abs(Vector3.Dot(heading, mainCamera.transform.forward));

                    float distance = Mathf.Abs(Vector3.Distance(transform.position, mainCamera.transform.position));
                    gameObjectRenderer.enabled = (minimalDistance <= distance && distance <= maximalDistance);
                    if (gameObjectRenderer.enabled)
                    {
                        lastCameraPosition = mainCamera.transform.position;
                        LookAtCamera();
                    }
                }
            }
        }

        protected virtual void LookAtCamera()
        {
            gameObject.transform.LookAt(mainCamera.transform);
            gameObject.transform.Rotate(rotation);
        }
    }
}