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
        /// The order in layer property.
        /// </summary>
        public int OrderInLayer { get; set; } = 1;

        /// <summary>
        /// Increases the order in layer by one.
        /// </summary>
        public void Inc()
        {
            OrderInLayer++;
        }

        /// <summary>
        /// The description property.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// The current page property.
        /// </summary>
        public int CurrentPage { get; set; }

        /// <summary>
        /// The maximum page size property.
        /// </summary>
        public int MaxPageSize { get; set; } = 1;
    }
}