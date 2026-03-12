using UnityEngine;

namespace SEE.Game.Drawable.ActionHelpers
{
    /// <summary>
    /// Provides shared constants and helper methods for calculating points of drawable shapes.
    /// </summary>
    internal static class PointsCalculator
    {
        /// <summary>
        /// The default number of vertices to use for circular shapes or polygons
        /// when generating points.
        /// </summary>
        internal const int DefaultVertices = 50;

        /// <summary>
        /// Converts a given local position into the drawable coordinate space by subtracting
        /// the global <see cref="ValueHolder.DistanceToDrawable"/> offset.
        /// This is useful for calculating positions relative to the drawable canvas or shape.
        /// </summary>
        /// <param name="x">The x-coordinate of the point in local space.</param>
        /// <param name="y">The y-coordinate of the point in local space.</param>
        /// <param name="z">The z-coordinate of the point in local space. Default is 0.</param>
        /// <returns>
        /// A <see cref="Vector3"/> representing the adjusted position in drawable coordinates.
        /// </returns>
        internal static Vector3 ToDrawable(float x, float y, float z = 0f)
        {
            return new Vector3(x, y, z) - ValueHolder.DistanceToDrawable;
        }
    }
}