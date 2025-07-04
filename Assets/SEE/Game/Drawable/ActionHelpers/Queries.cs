using UnityEngine;

namespace SEE.Game.Drawable.ActionHelpers
{
    /// <summary>
    /// This class provides various reused queries for the drawable actions.
    /// </summary>
    public static class Queries
    {
        /// <summary>
        /// Checks if the given drawable surface object is the same object as the other one.
        /// </summary>
        /// <param name="surface">The drawable surface to be checked.</param>
        /// <param name="other">>The other object.</param>
        /// <returns>True if the drawable surface is the same as the other object.</returns>
        public static bool SameDrawableSurface(GameObject surface, GameObject other)
        {
            return surface != null && GameFinder.GetDrawableSurface(other).Equals(surface);
        }

        /// <summary>
        /// Checks if the given drawable surface is null or the same object as the other <see cref="GameObject"/>.
        /// </summary>
        /// <param name="surface">The drawable surface to be checked.</param>
        /// <param name="other">The other object.</param>
        /// <returns>True if the drawable surface is null or the same as the other object.</returns>
        public static bool DrawableSurfaceNullOrSame(GameObject surface, GameObject other)
        {
            return surface == null || SameDrawableSurface(surface, other);
        }
    }
}
