using UnityEngine;

namespace SEE.Game.Drawable.ValueHolders
{
    /// <summary>
    /// The value holder for images.
    /// </summary>
    public class ImageValueHolder : MonoBehaviour
    {
        /// <summary>
        /// Holds the file data as bytes.
        /// </summary>
        private byte[] fileData;

        /// <summary>
        /// The byte file data property.
        /// </summary>
        public byte[] FileData 
        { 
            get { return fileData; }
            set { fileData = value; }
        }

        /// <summary>
        /// The selected file path.
        /// </summary>
        private string path;

        /// <summary>
        /// The path property.
        /// </summary>
        public string Path 
        { 
            get { return path; } 
            set {  path = value; } 
        }

        /// <summary>
        /// The url of the image.
        /// </summary>
        private string url;

        /// <summary>
        /// The url property.
        /// </summary>
        public string URL
        {
            get { return url; }
            set { url = value; }
        }
    }
}