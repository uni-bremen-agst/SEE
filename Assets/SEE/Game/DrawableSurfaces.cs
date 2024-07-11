using SEE.DataModel;
using SEE.DataModel.Drawable;
using Assets.SEE.UI.Window.DrawableManagerWindow;
using System.Collections.Generic;

namespace SEE.Game
{
    /// <summary>
    /// Class used by the <see cref="DrawableManagerWindow"> to detect added or removed drawable surfaces.
    /// </summary>
    public class DrawableSurfaces : Observable<ChangeEvent>
    {
        /// <summary>
        /// The observable list of <see cref="DrawableSurface"/>s.
        /// </summary>
        private List<DrawableSurface> surfaces = new();

        /// <summary>
        /// Property for the surface list.
        /// </summary>
        public List<DrawableSurface> Surfaces { get { return surfaces; } }

        /// <summary>
        /// Adds a <see cref="DrawableSurface"/> to the list.
        /// </summary>
        /// <param name="surface">The surface to be added.</param>
        public void Add(DrawableSurface surface)
        {
            surfaces.Add(surface);
            Notify(new AddSurfaceEvent(surface.ID, surface));
        }

        /// <summary>
        /// Removes a <see cref="DrawableSurface"/> from the list.
        /// </summary>
        /// <param name="surface">The surface to be removed.</param>
        public void Remove(DrawableSurface surface)
        {
            surfaces.Remove(surface);
            Notify(new RemoveSurfaceEvent(surface.ID, surface));
        }

        /// <summary>
        /// Query, if the list contains the <paramref name="surface"/>.
        /// </summary>
        /// <param name="surface">The surface to be checked.</param>
        /// <returns>whether the list contains the surface or not.</returns>
        public bool Contains(DrawableSurface surface)
        {
            return surfaces.Contains(surface);
        }
    }
}