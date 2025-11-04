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
        /// Gets the active <see cref="WebCamTexture"/> instance.
        /// Will be null until <see cref="Initialize"/> is successfully called.
        /// </summary>
        public static WebCamTexture WebCamTexture { get; private set; }

        /// <summary>
        /// Initializes the webcam manager.
        /// - If already initialized, this method does nothing.
        /// - If no webcams are found on the system, an error is logged and no texture is created.
        /// - If a webcam is available, the first detected device is selected, and a
        /// <see cref="webCamTexture"/> is created and started.
        /// </summary>
        public static void Initialize()
        {
            if (WebCamTexture != null)
            {
                return;
            }

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
