using Cysharp.Threading.Tasks;
using SEE.Controls.Actions.Drawable;
using SEE.Game.Drawable;
using SEE.Game.Drawable.ActionHelpers;
using SEE.Game.Drawable.Configurations;
using SEE.Utils;
using System.IO;
using UnityEngine;

namespace SEE.Net.Actions.Drawable
{
    /// <summary>
    /// This class is reponsible for add <see cref="AddImageAction"/> an image to the given drawable on all clients.
    /// </summary>
    public class AddImageNetAction : DrawableNetAction
    {
        /// <summary>
        /// The image that should be added as <see cref="ImageConf"/> object.
        /// </summary>
        public ImageConf Conf;

        /// <summary>
        /// Whether if the image is a web image.
        /// </summary>
        public bool Web;

        /// <summary>
        /// The size of the file data.
        /// </summary>
        public long Size;

        /// <summary>
        /// The constructor of this action. All it does is assign the value you pass it to a field.
        /// </summary>
        /// <param name="drawableID">The id of the drawable on which the object should be placed.</param>
        /// <param name="parentDrawableID">The id of the drawable parent.</param>
        /// <param name="imageConf">The image configuration of the image that should be added.</param>
        public AddImageNetAction(string drawableID, string parentDrawableID, ImageConf imageConf)
            : base(drawableID, parentDrawableID)
        {
            Conf = (ImageConf)imageConf.Clone();
            if (!string.IsNullOrEmpty(Conf.URL))
            {
                Size = new FileInfo(Conf.Path).Length;
                Conf.FileData = new byte[0];
                Web = true;
            }
            else
            {
                Web = false;
            }
        }

        /// <summary>
        /// Adds the image on each client.
        /// </summary>
        /// <exception cref="System.Exception">Will be thrown, if the <see cref="DrawableID"/> or <see cref="Conf.id"/> don't exists.</exception>
        public override void ExecuteOnClient()
        {
            base.ExecuteOnClient();
            if (Conf != null && Conf.ID != "")
            {
                if (!Web)
                {
                    GameImage.RePlaceImage(Surface, Conf);
                }
                else
                {
                    if (File.Exists(Conf.Path) && new FileInfo(Conf.Path).Length == Size)
                    {
                        Conf.FileData = File.ReadAllBytes(Conf.Path);
                        GameImage.RePlaceImage(Surface, Conf);
                    } else
                    {
                        DownloadAndAdd().Forget();
                    }
                }
            }
            else
            {
                throw new System.Exception($"There is no image to add.");
            }
            return;

            async UniTask DownloadAndAdd()
            {
                DownloadImage download = Surface.AddComponent<DownloadImage>();
                download.Download(Conf.URL);

                while (download != null && download.GetTexture() == null)
                {
                    await UniTask.Yield();
                }

                if (download != null && download.GetTexture() != null)
                {
                    Conf.FileData = download.GetTexture().EncodeToPNG();
                    GameImage.RePlaceImage(Surface, Conf);
                    Destroyer.Destroy(download);
                }
            }
        }
    }
}