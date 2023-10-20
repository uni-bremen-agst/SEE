using SEE.Game.UI.Notification;
using SEE.Utils;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace Assets.SEE.Game.Drawable.ActionHelpers
{
    /// <summary>
    /// Component to download an image from web.
    /// Idea by Marcus Ansley
    /// https://m-ansley.medium.com/unity-web-requests-downloading-an-image-e88d7389dd5a
    /// </summary>
    public class DownloadImage : MonoBehaviour
    {
        private Texture2D texture;
        private string imageUrl;

        public void Download(string url)
        {
            imageUrl = url;
            StartCoroutine(DownloadTexture());
        }

        private IEnumerator DownloadTexture()
        {
            UnityWebRequest request = UnityWebRequestTexture.GetTexture(imageUrl);

            yield return request.SendWebRequest();

            if (request.isHttpError || request.isNetworkError)
            {
                ShowNotification.Warn("Can't download", "The image of the http can't be downloaded.");
                Destroyer.Destroy(this);
            }
            else
            {
                texture = DownloadHandlerTexture.GetContent(request);
                if (texture == null)
                {
                    ShowNotification.Warn("Can't download", "The image of the http can't be downloaded.");
                    Destroyer.Destroy(this);
                }
            }
        }

        public Texture2D GetTexture()
        {
            if (texture != null)
            {
                return texture;
            } else
            {
                return null;
            }
        }
    }
}