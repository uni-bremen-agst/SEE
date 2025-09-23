using UnityEngine;


namespace SEE.Game.Avatars
{
    /// <summary>
    /// Controls the initialization and use of WebCamTexture, since this texture cannot be initialized more than once.
    /// </summary>
    public static class WebcamManager
    {
        private static WebCamTexture webCamTexture;

        public static WebCamTexture WebCamTexture => webCamTexture;

        public static void Initialize()
        {
            if (webCamTexture != null)
                return; // Already initialized

            var devices = WebCamTexture.devices;
            Debug.Log("Webcam devices count: " + devices.Length);

            if (devices.Length == 0)
            {
                Debug.LogError("No webcam found.");
                return;
            }

            var device = devices[0];
            webCamTexture = new WebCamTexture(device.name, 1240, 720, 30);
            webCamTexture.Play();

            Debug.Log("Webcam initialized: " + device.name);
        }
    }

}