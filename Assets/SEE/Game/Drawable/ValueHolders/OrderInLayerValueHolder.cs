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
        /// The holded order in layer.
        /// </summary>
        private int orderInLayer;

        /// <summary>
        /// The order in layer property.
        /// </summary>
        public int OrderInLayer
        {
            get { return orderInLayer; }
            set { orderInLayer = value; }
        }

        /// <summary>
        /// The original position.
        /// Only used for sticky note.
        /// </summary>
        private Vector3 originPos;

        /// <summary>
        /// The origin position property.
        /// </summary>
        public Vector3 OriginPosition
        {
            get { return originPos; }
            set { originPos = value; }
        }
    }
}