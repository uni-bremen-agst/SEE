using System.Collections;
using UnityEngine;

namespace Assets.SEE.Game.Drawable
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
        /// The selected file path.
        /// </summary>
        private string path;

        /// <summary>
        /// Sets the file data.
        /// </summary>
        /// <param name="fileData">The bytes of the image</param>
        public void SetFileData(byte[] fileData)
        {
            this.fileData = fileData;
        }

        /// <summary>
        /// Gets the bytes of the image.
        /// </summary>
        /// <returns>the bytes of the image</returns>
        public byte[] GetFileData()
        {
            return fileData;
        }

        /// <summary>
        /// Sets the path value of the image.
        /// </summary>
        /// <param name="path">The path where the image is located</param>
        public void SetPath(string path)
        {
            this.path = path;
        }

        /// <summary>
        /// Gets the path of the file.
        /// </summary>
        /// <returns>the file path.</returns>
        public string GetPath()
        {
            return path;
        }
    }
}