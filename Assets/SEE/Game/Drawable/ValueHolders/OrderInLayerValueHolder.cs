using UnityEngine;

namespace SEE.Game.Drawable.ValueHolders
{
    /// <summary>
    /// This class represents a value holder component for the
    /// order in layer value.
    /// </summary>
    public class OrderInLayerValueHolder : MonoBehaviour
    {
        /// <summary>
        /// The order in layer property.
        /// </summary>
        public int OrderInLayer { get; set; }

        /// <summary>
        /// The origin position property.
        /// Only used for sticky note.
        /// </summary>
        public Vector3 OriginPosition { get; set; }
    }
}