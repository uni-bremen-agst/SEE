using SEE.DataModel;
using SEE.GO;
using System;
using UnityEngine;

namespace SEE.Utils
{
    public static class MainCamera
    {
        /// <summary>
        /// The main camera in the scene. The main camera is the first enabled
        /// camera tagged "MainCamera". If there is no such camera at the moment,
        /// it will be null.
        ///
        /// Clients can register via <see cref="OnCameraAvailable"/> if they want
        /// to get notified that a camera is available.
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
                            return null;
                        case 1:
                            camera = Camera.main;
                            NotifyAll();
                            break;
                        default:
                            Debug.LogWarning($"There are {Camera.allCameras.Length} cameras in the scene. Expect unexpected visual results.\n");
                            foreach (Camera cam in Camera.allCameras)
                            {
                                // Analogous to Camera.main we are returning the first enabled camera tagged "MainCamera".
                                if (camera == null && cam.CompareTag(Tags.MainCamera))
                                {
                                    camera = cam;
                                }
                                Debug.LogWarning($"Camera: {cam.gameObject.FullName()}\n");
                            }
                            camera = Camera.main;
                            NotifyAll();
                            break;
                    }
                    // Debug.Log($"Selected main camera {camera.name} in game object {camera.gameObject.FullName()}.\n");
                }
                return camera;
            }
        }

        /// <summary>
        /// The main camera in the scene. May be null if there is no such camera.
        /// </summary>
        private static Camera camera;

        /// <summary>
        /// A delegate to be called when a camera is available.
        /// </summary>
        /// <param name="camera">the availabe camera</param>
        public delegate void OnCameraAvailableCallback(Camera camera);

        /// <summary>
        /// Event that is triggered when a camera is available.
        /// </summary>
        private static event OnCameraAvailableCallback onCameraAvailable;

        /// <summary>
        /// Registers <paramref name="callback"/> to be called when a camera becomes
        /// available. The argument of that callback will be the current camera.
        ///
        /// Note: The check whether a camera is available will be done only if a client
        /// asks for <see cref="MainCamera.Camera"/>. A client registered for this event
        /// will be notified only once. When the callback took place, it will be
        /// unregistered from this event.
        /// </summary>
        /// <param name="callback">callback to be called when the camera becomes available</param>
        public static void OnCameraAvailable(OnCameraAvailableCallback callback)
        {
            onCameraAvailable += callback;
        }

        /// <summary>
        /// Notifies all listeners of <see cref="onCameraAvailable"/>. After that all
        /// notified listeners will be unregistered from <see cref="onCameraAvailable"/>.
        /// </summary>
        private static void NotifyAll()
        {
            if (onCameraAvailable != null)
            {
                onCameraAvailable.Invoke(camera);

                foreach (Delegate callback in onCameraAvailable.GetInvocationList())
                {
                    onCameraAvailable -= (OnCameraAvailableCallback)callback;
                }
            }
        }

        /// <summary>
        /// If a camera is available, it will be returned. If no camera is available
        /// at the moment, the given <paramref name="callback"/> will be registered
        /// to be called when a camera becomes available (analogous to
        /// <see cref="OnCameraAvailable(OnCameraAvailableCallback)"/> and null
        /// will be returned.
        /// </summary>
        /// <param name="callback">callback to be called when the camera becomes available</param>
        /// <returns>the available camera if one exists or null otherwise</returns>
        public static Camera GetCameraNowOrLater(OnCameraAvailableCallback callback)
        {
            Camera result = Camera;
            if (result == null)
            {
                OnCameraAvailable(callback);
            }
            return result;
        }
    }
}