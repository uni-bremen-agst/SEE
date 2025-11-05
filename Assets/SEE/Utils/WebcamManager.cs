using System.Collections.Generic;
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
        ///
        /// </summary>
        private static Dictionary<string, WebCamTexture> webcams = new();

        /// <summary>
        ///
        /// </summary>
        private static WebCamDevice[] devices;

        /// <summary>
        /// The currently active <see cref="WebCamTexture"/> instance.
        /// </summary>
        private static WebCamTexture activeWebcam;

        /// <summary>
        /// Gets the active <see cref="WebCamTexture"/> instance.
        /// If no webcam has been initialized yet, this property automatically calls
        /// <see cref="Initialize"/> to create and start the first available webcam device.
        /// If initialization fails (e.g., because no camera is available), this property will return null.
        /// </summary>
        public static WebCamTexture WebCamTexture
        {
            get
            {
                if (activeWebcam == null)
                {
                    Initialize();
                }
                return activeWebcam;
            }
            private set => activeWebcam = value;
        }

        /// <summary>
        /// Initializes the webcam manager.
        /// - If already initialized, this method does nothing.
        /// - If no webcams are found on the system, an error is logged and no texture is created.
        /// - If a webcam is available, the first detected device is selected, and a
        /// <see cref="webCamTexture"/> is created and started.
        /// </summary>
        private static void Initialize()
        {
            WebCamDevice[] devices = WebCamTexture.devices;
            Debug.Log($"Webcam devices count: {devices.Length}\n");

            if (devices.Length == 0)
            {
                Debug.LogError("No webcam found.\n");
                return;
            }

            WebCamDevice device = devices[0];
            WebCamTexture = new WebCamTexture(device.name);
            WebCamTexture.Play();
            Debug.Log($"Webcam initialized: {device.name}\n");
        }
    }
}
