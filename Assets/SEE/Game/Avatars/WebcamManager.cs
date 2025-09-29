using UnityEngine;

namespace SEE.Game.Avatars
{
    /// <summary>
    /// Controls the initialization and use of WebCamTexture, since this texture cannot be initialized more than once.
    /// </summary>
    public static class WebcamManager
    {
        /// <summary>
        /// The active webcam texture instance.
        /// </summary>
        private static WebCamTexture webCamTexture;

        /// <summary>
        /// Gets the active <see cref="WebCamTexture"/> instance.
        /// Will be null until <see cref="Initialize"/> is successfully called.
        /// </summary>
        public static WebCamTexture WebCamTexture => webCamTexture;

        /// <summary>
        /// Initializes the webcam manager.
        /// - If already initialized, this method does nothing.
        /// - If no webcams are found on the system, an error is logged and no texture is created.
        /// - If a webcam is available, the first detected device is selected, and a <see cref="webCamTexture"/> is created and started.
        /// </summary>
        public static void Initialize()
        {
            if (webCamTexture != null)
            {
                return;
            }

            WebCamDevice[] devices = WebCamTexture.devices;
            Debug.Log("Webcam devices count: " + devices.Length + "\n");

            if (devices.Length == 0)
            {
                Debug.LogError("No webcam found." + "\n");
                return;
            }

            WebCamDevice device = devices[0];
            webCamTexture = new WebCamTexture(device.name, 1240, 720, 30);
            webCamTexture.Play();
            Debug.Log("Webcam initialized: " + device.name + "\n");
        }
    }
}
