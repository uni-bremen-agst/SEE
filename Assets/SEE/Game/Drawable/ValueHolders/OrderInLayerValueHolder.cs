using SEE.Game.Drawable.Configurations;
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
        /// The original position.
        /// Only used for sticky note.
        /// </summary>
        private Vector3 originPos;

        /// <summary>
        /// Sets the given order.
        /// </summary>
        /// <param name="order">The given order that should be set.</param>
        public void SetOrderInLayer(int order)
        {
            orderInLayer = order;
        }

        /// <summary>
        /// Gets the current order in layer.
        /// </summary>
        /// <returns>current order</returns>
        public int GetOrderInLayer()
        {
            return orderInLayer;
        }

        /// <summary>
        /// Sets the given position
        /// </summary>
        /// <param name="pos">the position</param>
        public void SetOriginPosition(Vector3 pos)
        {
            originPos = pos;
        }

        /// <summary>
        /// Gets the origin position.
        /// </summary>
        /// <returns>origin position</returns>
        public Vector3 GetOriginPosition()
        {
            return originPos;
        }
    }
}