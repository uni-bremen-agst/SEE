using SEE.DataModel;
using SEE.DataModel.Drawable;

namespace SEE.Game
{
    /// <summary>
    /// Class used by the <see cref="DrawableManagerWindow"> to detect added or
    /// removed drawable surfaces.
    /// </summary>
    public class DrawableSurfaces : Observable<ChangeEvent>
    {
        /// <summary>
        /// Adds a <see cref="DrawableSurface"/> to the list.
        /// </summary>
        /// <param name="surface">The surface to be added.</param>
        public void Add(DrawableSurface surface)
        {
            Notify(new AddSurfaceEvent(surface.ID, surface));
        }

        /// <summary>
        /// Removes a <see cref="DrawableSurface"/> from the list.
        /// </summary>
        /// <param name="surface">The surface to be removed.</param>
        public void Remove(DrawableSurface surface)
        {
            Notify(new RemoveSurfaceEvent(surface.ID, surface));
        }
    }
}