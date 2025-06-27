using UnityEngine;


namespace SEE.Game.Avatars
{
    public static class WebcamManager
    {
        private static WebCamTexture sharedWebCamTexture;

        public static WebCamTexture SharedWebCamTexture => sharedWebCamTexture;

        public static void Initialize()
        {
            if (sharedWebCamTexture != null)
                return; // Already initialized

            var devices = WebCamTexture.devices;
            Debug.Log("Webcam devices count: " + devices.Length);

            if (devices.Length == 0)
            {
                Debug.LogError("No webcam found.");
                return;
            }

            var device = devices[0];
            sharedWebCamTexture = new WebCamTexture(device.name, 1240, 720, 30);
            sharedWebCamTexture.Play();

            Debug.Log("Webcam initialized: " + device.name);
        }
    }

}