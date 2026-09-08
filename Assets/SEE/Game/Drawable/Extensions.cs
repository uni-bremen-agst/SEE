using UnityEngine;

namespace SEE.Game.Drawable
{
    /// <summary>
    /// Extension methods for <see cref="GameObject"/>s holding a <see cref="DrawableSurface"/>.
    /// </summary>
    internal static class Extensions
    {
        /// <summary>
        /// Returns true if <paramref name="gameObject"/> has a <see cref="DrawableSurfaceRef"/>
        /// component attached to it that is not null.
        /// </summary>
        /// <param name="gameObject">The game object whose DrawableSurfaceRef is checked.</param>
        /// <param name="surface">The surface referenced by the attached DrawableSurfaceRef; defined only if this method
        /// returns true.</param>
        /// <returns>True if <paramref name="gameObject"/> has a <see cref="DrawableSurfaceRef"/>
        /// component attached to it that is not null.</returns>
        public static bool TryGetDrawableSurface(this GameObject gameObject, out DrawableSurface surface)
        {
            surface = null;
            if (gameObject.TryGetComponent(out DrawableSurfaceRef surfaceRef))
            {
                surface = surfaceRef.Surface;
            }
            return surface != null;
        }
    }
}
