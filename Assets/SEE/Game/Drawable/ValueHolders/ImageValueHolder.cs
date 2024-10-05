using UnityEngine;

namespace SEE.Game.Drawable.ValueHolders
{
    /// <summary>
    /// The value holder for images.
    /// </summary>
    public class ImageValueHolder : MonoBehaviour
    {
        /// <summary>
        /// The byte file data property.
        /// </summary>
        public byte[] FileData { get; set; }

        /// <summary>
        /// The path property.
        /// </summary>
        public string Path { get; set; } = string.Empty;

        /// <summary>
        /// The url property.
        /// </summary>
        public string URL { get; set; } = string.Empty;
    }
}