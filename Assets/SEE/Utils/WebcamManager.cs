using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace SEE.Utils
{
    /// <summary>
    /// Manages multiple webcams with lazy initialization.
    /// Only the active webcam is played to save resources.
    ///
    /// Note: A WebCamTexture cannot be initialized more than once per device.
    /// Attempting to create multiple WebCamTextures for the same camera will fail,
    /// so this manager ensures that each device has at most one WebCamTexture instance.
    /// </summary>
    public static class WebcamManager
    {
        /// <summary>
        /// List of all available <see cref="WebCamTexture"/> instances in the order
        /// they were detected on the system.
        /// </summary>
        private static readonly List<WebCamTexture> webcams = new();

        /// <summary>
        /// Index of the currently active webcam in the <see cref="webcams"/> list.
        /// </summary>
        private static int activeIndex = 0;

        /// <summary>
        /// Occurs when the active webcam is changed via <see cref="SwitchCamera(int)"/>.
        /// </summary>
        public static event Action<WebCamTexture> OnActiveWebcamChanged;

        /// <summary>
        /// Gets the currently active <see cref="WebCamTexture"/>.
        /// If no webcam has been initialized yet, this property automatically calls
        /// <see cref="Initialize"/> to create and start the first available webcam device.
        /// If initialization fails (e.g., because no camera is available), this property will return null.
        /// </summary>
        public static WebCamTexture WebCamTexture
        {
            get
            {
                if (webcams.Count == 0)
                {
                    Initialize();
                }
                return webcams.Count > 0 ? webcams[activeIndex] : null;
            }
        }

        /// <summary>
        /// Initializes the webcam manager and registers all available webcams.
        /// - If no webcams are found on the system, an error is logged and no <see cref="WebCamTexture"/> is created.
        /// - The first detected device is selected as the active webcam and started automatically.
        /// - All other webcams are registered in the list but not started, to save resources.
        /// - Each <see cref="WebCamTexture"/> is created exactly once per device.
        /// </summary>
        private static void Initialize()
        {
            WebCamDevice[] devices = WebCamTexture.devices;

            if (devices.Length == 0)
            {
                Debug.LogError("No webcam found.\n");
                return;
            }

            // Initialize the remaining webcams (not played yet)
            for (int i = 0; i < devices.Length; i++)
            {
                WebCamDevice device = devices[i];
                webcams.Add(new(device.name));
                if (i == 0)
                {
                    activeIndex = i;
                    webcams[i].Play(); // TODO: Check if Play() is required for BodyAnimator?
                    Debug.Log($"[WebcamManager] Active webcam initialized: {device.name}\n");
                }
            }
        }

        /// <summary>
        /// Switches the active webcam by index.
        /// Stops the previously active webcam and starts the newly selected one.
        /// </summary>
        /// <param name="index">Index of the webcam in the <see cref="webcams"/> list.</param>
        public static void SwitchCamera(int index)
        {
            if (index < 0 || index >= webcams.Count)
            {
                Debug.LogWarning($"[WebcamManager] Webcam index {index} is out of range.");
                return;
            }

            if (activeIndex != index)
            {
                StopWebcamAsync(activeIndex).Forget();
                activeIndex = index;
                OnActiveWebcamChanged?.Invoke(webcams[activeIndex]);
                webcams[activeIndex].Play(); // TODO: Check if Play() is required for BodyAnimator?
            }

            static async UniTask StopWebcamAsync(int index)
            {
                await UniTask.Yield();
                webcams[index].Stop();
            }
        }
    }
}
