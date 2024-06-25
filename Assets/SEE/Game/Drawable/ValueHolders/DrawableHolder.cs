using UnityEngine;

namespace Assets.SEE.Game.Drawable.ValueHolders
{
    /// <summary>
    /// This class represents a value holder component for the drawable surface.
    /// </summary>
    public class DrawableHolder : MonoBehaviour
    {
        /// <summary>
        /// The current order in layer for this drawable surface.
        /// </summary>
        private int orderInLayer = 1;

        /// <summary>
        /// The order in layer property.
        /// </summary>
        public int OrderInLayer
        {
            get { return orderInLayer; }
            set { orderInLayer = value; }
        }

        /// <summary>
        /// Increases the order in layer by one.
        /// </summary>
        public void Inc()
        {
            orderInLayer++;
        }

        /// <summary>
        /// Decreases the order in layer by one.
        /// </summary>
        public void Dec()
        {
            if (orderInLayer > 0)
            {
                orderInLayer--;
            }
        }

        /// <summary>
        /// The description for this drawable surface.
        /// </summary>
        private string description = "";

        /// <summary>
        /// The description property.
        /// </summary>
        public string Description
        {
            get { return description; }
            set { description = value; }
        }
    }
}