using SEE.DataModel;
using SEE.GO;
using UnityEngine;

namespace SEE.Utils
{
    public static class MainCamera
    {
        /// <summary>
        /// The main camera in the scene. The main camera is the first enabled
        /// camera tagged "MainCamera". An exception will be thrown when there
        /// is no such camera.
        ///
        /// Unlike the equivalent Camera.main property, the main camera will be
        /// cached and re-used upon every other access. This helps to avoid the
        /// relatively expensive lookup of Camera.main.
        ///
        /// If there are multiple cameras in the scene, a warning will be logged
        /// and all cameras will be enumerated in the log.
        /// </summary>
        public static Camera Camera
        {
            get
            {
                if (camera == null)
                {
                    switch (Camera.allCameras.Length)
                    {
                        case 0:
                            Debug.LogError("There is no camera in the scene.\n");
                            throw new System.Exception("There is no camera in the scene");
                        case 1:
                            camera = Camera.main;
                            break;
                        default:
                            Debug.LogWarning($"There are {Camera.allCameras.Length} cameras in the scene. Expect unexpected visual results.\n");
                            foreach (Camera c in Camera.allCameras)
                            {
                                // Analogous to Camera.main we are returning the first enabled camera tagged "MainCamera".
                                if (camera == null && c.CompareTag(Tags.MainCamera))
                                {
                                    camera = c;
                                }
                                Debug.LogWarning($"Camera: {c.gameObject.GetFullName()}\n");
                            }
                            camera = Camera.main;
                            break;
                    }
                    Debug.Log($"Selected main camera {camera.name} in game object {camera.gameObject.FullName()}.\n");
                }
                return camera;
            }
        }

        /// <summary>
        /// The main camera in the scene. May be null if there is no such camera.
        /// </summary>
        private static Camera camera;
    }
}