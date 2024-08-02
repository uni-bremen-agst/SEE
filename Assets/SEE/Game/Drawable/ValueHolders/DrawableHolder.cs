using Unity.Collections;
using UnityEngine;

namespace SEE.Game.Drawable.ValueHolders
{
    /// <summary>
    /// This class represents a value holder component for the drawable surface.
    /// </summary>
    /// <remarks>This component is meant to be attached to a drawable surface.</remarks>
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
        /// The current description for this drawable surface.
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

        /// <summary>
        /// The current selected page of this drawable surface.
        /// </summary>
        private int currentPage = 0;

        /// <summary>
        /// The current page property.
        /// </summary>
        public int CurrentPage 
        {
            get { return currentPage; } 
            set { currentPage = value; } 
        }

        /// <summary>
        /// The current max page size for this drawable surface.
        /// </summary>
        private int maxPageSize = 1;

        /// <summary>
        /// The maximum page size property.
        /// </summary>
        public int MaxPageSize 
        { 
            get { return maxPageSize; } 
            set { maxPageSize = value; } 
        }
    }
}