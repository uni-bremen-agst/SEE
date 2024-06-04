using SEE.UI.Notification;
using SEE.Utils;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace SEE.Game.Drawable.ActionHelpers
{
    /// <summary>
    /// Component to download an image from the web.
    ///
    /// Idea by Marcus Ansley
    /// https://m-ansley.medium.com/unity-web-requests-downloading-an-image-e88d7389dd5a
    /// last visite: 12.12.2023
    /// </summary>
    public class DownloadImage : MonoBehaviour
    {
        /// <summary>
        /// The downloaded texture.
        /// </summary>
        private Texture2D texture;

        /// <summary>
        /// The url of the image.
        /// </summary>
        private string imageUrl;

        /// <summary>
        /// Initializes the download of the image.
        /// </summary>
        /// <param name="url">The url from which the image should be downloaded.</param>
        public void Download(string url)
        {
            imageUrl = url;
            StartCoroutine(DownloadTexture());
        }

        /// <summary>
        /// Coroutine that handles the download.
        /// Displays a warning if an error occurs during the download.
        /// </summary>
        /// <returns>enumerator to continue</returns>
        private IEnumerator DownloadTexture()
        {
            UnityWebRequest request = UnityWebRequestTexture.GetTexture(imageUrl);

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.ProtocolError
                ||  request.result == UnityWebRequest.Result.ConnectionError)
            {
                Warn(imageUrl);
                Destroyer.Destroy(this);
            }
            else
            {
                texture = DownloadHandlerTexture.GetContent(request);
                if (texture == null)
                {
                    Warn(imageUrl);
                    Destroyer.Destroy(this);
                }
            }

            static void Warn(string imageUrl)
            {
                ShowNotification.Warn("Can't download", $"The image can't be downloaded from {imageUrl}.");
            }
        }

        /// <summary>
        /// Gets the downloaded texture.
        /// Note: Can be null.
        /// </summary>
        /// <returns>the downloaded texture</returns>
        public Texture2D GetTexture()
        {
            if (texture != null)
            {
                return texture;
            }
            else
            {
                return null;
            }
        }
    }
}