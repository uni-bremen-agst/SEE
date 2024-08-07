using SEE.Game.Drawable;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.UI.Window.DrawableManagerWindow
{
    /// <summary>
    /// Manages the grouping of drawable surface elements in the drawable manager window.
    /// </summary>
    public class DrawableWindowGrouper
    {
        /// <summary>
        /// Whether the group is active or not.
        /// </summary>
        public bool IsActive = false;

        /// <summary>
        /// Resets the grouper to inactive.
        /// </summary>
        public void Reset()
        {
            IsActive = false;
        }

        /// <summary>
        /// Gets all whiteboards depending on the current filter.
        /// </summary>
        /// <param name="filter">The filter of the drawable manager window.</param>
        /// <returns>A list of the whiteboards depending on the filter.</returns>
        public List<GameObject> GetWhiteboards(DrawableSurfaceFilter filter)
        {
            return filter.GetFilteredSurfaces().FindAll(GameFinder.IsWhiteboard);
        }

        /// <summary>
        /// Gets all sticky notes depending on the current filter.
        /// </summary>
        /// <param name="filter">The filter of the drawable manger window.</param>
        /// <returns>A list of the sticky notes depending on the filter.</returns>
        public List<GameObject> GetStickyNotes(DrawableSurfaceFilter filter)
        {
            return filter.GetFilteredSurfaces().FindAll(GameFinder.IsStickyNote);
        }
    }
}
