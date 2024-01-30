using SEE.Game.Drawable.ValueHolders;
using SEE.Utils;
using System;
using System.Collections.Generic;
using System.IO;
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
        public Color imageColor;

        /// <summary>
        /// The file data of the image.
        /// </summary>
        public byte[] fileData;

        /// <summary>
        /// The file path where the image is located.
        /// </summary>
        public string path;

        /// <summary>
        /// Label in the configurtation file for the color of the image.
        /// </summary>
        private const string ColorLabel = "ColorLabel";

        /// <summary>
        /// Label in the configuration file for the path of the image.
        /// </summary>
        private const string PathLabel = "PathLabel";

        /// <summary>
        /// Get a image configuration for the given game object.
        /// Only works if the game object is a drawable image.
        /// </summary>
        /// <param name="imageObject">The game object which contains the image, the canvas and a image tag.</param>
        /// <returns>A new created image configuration</returns>
        public static ImageConf GetImageConf(GameObject imageObject)
        {
            ImageConf conf = null;
            if (imageObject != null && imageObject.CompareTag(Tags.Image))
            {
                conf = new()
                {
                    id = imageObject.name,
                    position = imageObject.transform.localPosition,
                    eulerAngles = imageObject.transform.localEulerAngles,
                    scale = imageObject.transform.localScale,
                    orderInLayer = imageObject.GetComponent<OrderInLayerValueHolder>().GetOrderInLayer(),
                    imageColor = imageObject.GetComponent<Image>().color,
                    path = imageObject.GetComponent<ImageValueHolder>().GetPath(),
                    fileData = imageObject.GetComponent<ImageValueHolder>().GetFileData()
                };
            }
            return conf;
        }

        /// <summary>
        /// This method clons the <see cref="ImageConf"/> object.
        /// </summary>
        /// <returns>A new <see cref="ImageConf"/> with the values of this object.</returns>
        public object Clone()
        {
            return new ImageConf
            {
                id = this.id,
                position = this.position,
                eulerAngles = this.eulerAngles,
                scale = this.scale,
                orderInLayer = this.orderInLayer,
                imageColor = this.imageColor,
                path = this.path,
                fileData = this.fileData
            };
        }

        /// <summary>
        /// Writes this instances' attributes into the given <see cref="ConfigWriter"/>.
        /// </summary>
        /// <param name="writer">The <see cref="ConfigWriter"/> to write the attributes into.</param>
        override internal void Save(ConfigWriter writer)
        {
            writer.BeginGroup();
            base.Save(writer);
            writer.Save(imageColor, ColorLabel);
            writer.Save(path, PathLabel);
            writer.EndGroup();
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
            
            /// Try to restores the image color.
            Color loadedimageColor = Color.black;
            if (ConfigIO.Restore(attributes, ColorLabel, ref loadedimageColor))
            {
                imageColor = loadedimageColor;
            }
            else
            {
                imageColor = Color.black;
                errors = true;
            }

            /// Try to restores the image file.
            if (attributes.TryGetValue(PathLabel, out object p))
            {
                path = (string)p;
                if (File.Exists(path))
                {
                    fileData = File.ReadAllBytes(path);
                }
            }
            else
            {
                errors = true;
            }

            return !errors;
        }
    }
}