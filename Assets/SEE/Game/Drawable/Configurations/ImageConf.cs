using SEE.Game.Drawable.ValueHolders;
using SEE.Utils.Config;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace SEE.Game.Drawable.Configurations
{
    /// <summary>
    /// The configuration class for a drawable image.
    /// </summary>
    [Serializable]
    public class ImageConf : DrawableType, ICloneable
    {
        /// <summary>
        /// The color of the image.
        /// </summary>
        public Color ImageColor;

        /// <summary>
        /// The file data of the image.
        /// </summary>
        public byte[] FileData;

        /// <summary>
        /// The file path where the image is located.
        /// </summary>
        public string Path;

        /// <summary>
        /// The url of the image.
        /// </summary>
        public string URL;

        /// <summary>
        /// Returns an image configuration for the given game object.
        /// Only works if the game object is a drawable image.
        /// If not, <c>null</c> is returned.
        /// </summary>
        /// <param name="imageObject">The game object which contains the image, the canvas and an image tag.</param>
        /// <returns>A newly created image configuration.</returns>
        public static ImageConf GetImageConf(GameObject imageObject)
        {
            ImageConf conf = null;
            if (imageObject != null && imageObject.CompareTag(Tags.Image))
            {
                conf = new()
                {
                    ID = imageObject.name,
                    AssociatedPage = imageObject.GetComponent<AssociatedPageHolder>().AssociatedPage,
                    Position = imageObject.transform.localPosition,
                    EulerAngles = imageObject.transform.localEulerAngles,
                    Scale = imageObject.transform.localScale,
                    OrderInLayer = imageObject.GetComponent<OrderInLayerValueHolder>().OrderInLayer,
                    ImageColor = imageObject.GetComponent<Image>().color,
                    Path = imageObject.GetComponent<ImageValueHolder>().Path,
                    FileData = imageObject.GetComponent<ImageValueHolder>().FileData,
                    URL = imageObject.GetComponent<ImageValueHolder>().URL
                };
            }
            return conf;
        }

        /// <summary>
        /// Returns a clone of this <see cref="ImageConf"/> object.
        /// </summary>
        /// <returns>A new <see cref="ImageConf"/> with the values of this object.</returns>
        public object Clone()
        {
            return new ImageConf
            {
                ID = this.ID,
                AssociatedPage = this.AssociatedPage,
                Position = this.Position,
                EulerAngles = this.EulerAngles,
                Scale = this.Scale,
                OrderInLayer = this.OrderInLayer,
                ImageColor = this.ImageColor,
                Path = this.Path,
                FileData = this.FileData.ToArray(),
                URL = this.URL
            };
        }

        #region Config I/O

        /// <summary>
        /// Label in the configuration file for the color of the image.
        /// </summary>
        private const string colorLabel = "ColorLabel";

        /// <summary>
        /// Label in the configuration file for the path of the image.
        /// </summary>
        private const string pathLabel = "PathLabel";

        /// <summary>
        /// Label in the configuration file for the url of the image.
        /// </summary>
        private const string urlLabel = "UrlLabel";

        /// <summary>
        /// Saves this instance's attributes using <see cref="ConfigWriter"/>.
        /// </summary>
        /// <param name="writer">The <see cref="ConfigWriter"/> to write the attributes.</param>
        protected override void SaveAttributes(ConfigWriter writer)
        {
            writer.Save(ImageColor, colorLabel);
            writer.Save(Path, pathLabel);
            writer.Save(URL, urlLabel);
        }

        /// <summary>
        /// Given the representation of a <see cref="ImageConf"/> as created by the <see cref="ConfigWriter"/>,
        /// this method parses the attributes from that representation and
        /// puts them into this <see cref="ImageConf"/> instance.
        /// </summary>
        /// <param name="attributes">A list of labels (strings) of attributes and their values (objects). This
        /// has to be the representation of a <see cref="ImageConf"/> as created by
        /// <see cref="ConfigWriter"/>.</param>
        /// <returns>Whether or not the <see cref="ImageConf"/> was loaded without errors.</returns>
        override internal bool Restore(Dictionary<string, object> attributes)
        {
            bool errors = base.Restore(attributes);

            /// Try to restore the image color.
            Color loadedimageColor = Color.black;
            if (ConfigIO.Restore(attributes, colorLabel, ref loadedimageColor))
            {
                ImageColor = loadedimageColor;
            }
            else
            {
                ImageColor = Color.black;
                errors = true;
            }

            /// Try to restore the image file.
            if (attributes.TryGetValue(pathLabel, out object p))
            {
                Path = (string)p;
                if (File.Exists(Path))
                {
                    FileData = File.ReadAllBytes(Path);
                }
            }
            else
            {
                errors = true;
            }

            /// Try to restore the image url.
            if (attributes.TryGetValue(pathLabel, out object url))
            {
                URL = (string)url;
            }
            else
            {
                errors = true;
            }

            return !errors;
        }

        #endregion
    }
}