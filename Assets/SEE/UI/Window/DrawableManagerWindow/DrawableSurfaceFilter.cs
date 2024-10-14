using SEE.Game.Drawable;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SEE.UI.Window.DrawableManagerWindow
{
    /// <summary>
    /// A configurable filter for drawable surfaces.
    /// </summary>
    public class DrawableSurfaceFilter
    {
        /// <summary>
        /// Whether to include whiteboards.
        /// </summary>
        public bool IncludeWhiteboards = true;

        /// <summary>
        /// Whether to include sticky notes.
        /// </summary>
        public bool IncludeStickyNotes = true;

        /// <summary>
        /// Whether to include surfaces with a description.
        /// </summary>
        public bool IncludeHaveDescription = true;

        /// <summary>
        /// Whether to include surfaces without a description.
        /// </summary>
        public bool IncludeHaveNoDescription = true;

        /// <summary>
        /// Whether to include surfaces that have an active lighting.
        /// </summary>
        public bool IncludeHaveLighting = true;

        /// <summary>
        /// Whether to include surfaces that have an inactive lighting.
        /// </summary>
        public bool IncludeHaveNoLighting = true;

        /// <summary>
        /// Whether to include surfaces that are visible.
        /// </summary>
        public bool IncludeIsVisible = true;

        /// <summary>
        /// Whether to include surfaces that are invisible.
        /// </summary>
        public bool IncludeIsInvisibile = true;

        /// <summary>
        /// Resets the filter.
        /// </summary>
        public void Reset()
        {
            IncludeWhiteboards = true;
            IncludeStickyNotes = true;
            IncludeHaveDescription = true;
            IncludeHaveNoDescription = true;
            IncludeHaveLighting = true;
            IncludeHaveNoLighting = true;
            IncludeIsVisible = true;
            IncludeIsInvisibile = true;
        }

        /// <summary>
        /// Query if all filter includes are active.
        /// </summary>
        /// <returns>True if all filter includes are active.</returns>
        public bool AllActive()
        {
            return IncludeWhiteboards
                && IncludeStickyNotes
                && IncludeHaveDescription
                && IncludeHaveNoDescription
                && IncludeHaveLighting
                && IncludeHaveNoLighting
                && IncludeIsVisible
                && IncludeIsInvisibile;
        }

        /// <summary>
        /// Get the list of the filtered surfaces.
        /// </summary>
        /// <returns>The filtered surfaces.</returns>
        public List<GameObject> GetFilteredSurfaces()
        {
            List<GameObject> surfaces;
            if (AllActive())
            {
                surfaces = ValueHolder.DrawableSurfaces;
            }
            else
            {
                HashSet<GameObject> setSurface = new(ValueHolder.DrawableSurfaces);
                if (!IncludeWhiteboards)
                {
                    setSurface.ExceptWith(ValueHolder.DrawableSurfaces.FindAll(GameFinder.IsWhiteboard));
                }
                if (!IncludeStickyNotes)
                {
                    setSurface.ExceptWith(ValueHolder.DrawableSurfaces.FindAll(GameFinder.IsStickyNote));
                }
                if (!IncludeHaveDescription)
                {
                    setSurface.ExceptWith(ValueHolder.DrawableSurfaces.FindAll(GameDrawableManager.HasDescription));
                }
                if (!IncludeHaveNoDescription)
                {
                    setSurface.ExceptWith(ValueHolder.DrawableSurfaces.FindAll(x => !GameDrawableManager.HasDescription(x)));
                }
                if (!IncludeHaveLighting)
                {
                    setSurface.ExceptWith(ValueHolder.DrawableSurfaces.FindAll(GameDrawableManager.IsLighting));
                }
                if (!IncludeHaveNoLighting)
                {
                    setSurface.ExceptWith(ValueHolder.DrawableSurfaces.FindAll(x => !GameDrawableManager.IsLighting(x)));
                }
                if (!IncludeIsVisible)
                {
                    setSurface.ExceptWith(ValueHolder.DrawableSurfaces.FindAll(GameDrawableManager.IsVisible));
                }
                if (!IncludeIsInvisibile)
                {
                    setSurface.ExceptWith(ValueHolder.DrawableSurfaces.FindAll(x => !GameDrawableManager.IsVisible(x)));
                }
                surfaces = setSurface.ToList();
            }
            return surfaces;
        }
    }
}
