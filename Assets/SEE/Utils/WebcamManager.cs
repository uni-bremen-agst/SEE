using UnityEngine;

namespace SEE.Utils
{
    /// <summary>
    /// Controls the initialization and use of WebCamTexture, since this texture cannot
    /// be initialized more than once.
    /// </summary>
    public static class WebcamManager
    {
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
            WebCamTexture = new WebCamTexture(device.name, 1240, 720, 30);
            WebCamTexture.Play();
            Debug.Log($"Webcam initialized: {device.name}\n");
        }
    }
}
