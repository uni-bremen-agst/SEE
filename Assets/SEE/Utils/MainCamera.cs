using SEE.Game;
using SEE.GO;
using System;
using UnityEngine;

namespace SEE.Utils
{
    /// <summary>
    /// Allows to retrieve the main camera, that is, the camera attached to the
    /// local player.
    /// </summary>
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
                            // We have a single enabled camera, but that camera is
                            // not necessarily tagged as the "MainCamera".
                            if (camera != null)
                            {
                                NotifyAll();
                            }
                            break;
                        default:
                            // There is more than one camera, but there should be only one that is tagged as the "MainCamera".
                            foreach (Camera cam in Camera.allCameras)
                            {
                                // Analogous to Camera.main we are returning the first enabled camera tagged "MainCamera".
                                if (cam.CompareTag(Tags.MainCamera))
                                {
                                    if (camera == null)
                                    {
                                        camera = cam;
                                    }
                                    else
                                    {
                                        Debug.LogError($"Multiple cameras tagged by {Tags.MainCamera}: {cam.gameObject.FullName()}\n");
                                    }
                                }
                            }
                            NotifyAll();
                            break;
                    }
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
        /// <param name="camera">The availabe camera.</param>
        public delegate void OnCameraAvailableCallback(Camera camera);

        /// <summary>
        /// Event that is triggered when a camera is available.
        /// </summary>
        private static event OnCameraAvailableCallback OnCameraAvailableCallBack;

        /// <summary>
        /// Registers <paramref name="callback"/> to be called when a camera becomes
        /// available. The argument of that callback will be the current camera.
        ///
        /// Note: The check whether a camera is available will be done only if a client
        /// asks for <see cref="MainCamera.Camera"/>. A client registered for this event
        /// will be notified only once. When the callback took place, it will be
        /// unregistered from this event.
        /// </summary>
        /// <param name="callback">Callback to be called when the camera becomes available.</param>
        public static void OnCameraAvailable(OnCameraAvailableCallback callback)
        {
            OnCameraAvailableCallBack += callback;
        }

        /// <summary>
        /// Notifies all listeners of <see cref="OnCameraAvailableCallBack"/>. After that all
        /// notified listeners will be unregistered from <see cref="OnCameraAvailableCallBack"/>.
        /// </summary>
        private static void NotifyAll()
        {
            if (OnCameraAvailableCallBack != null)
            {
                OnCameraAvailableCallBack.Invoke(camera);

                foreach (Delegate callback in OnCameraAvailableCallBack.GetInvocationList())
                {
                    OnCameraAvailableCallBack -= (OnCameraAvailableCallback)callback;
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
        /// <param name="callback">Callback to be called when the camera becomes available.</param>
        /// <returns>The available camera if one exists or null otherwise.</returns>
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