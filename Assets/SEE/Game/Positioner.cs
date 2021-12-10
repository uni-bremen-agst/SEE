using System;
using UnityEngine;

namespace SEE.Game
{
    /// <summary>
    /// Provides positioning and rotation.
    /// </summary>
    internal static class Positioner
    {
        /// <summary>
        /// Sets the position (world space) and angle around the y axis of the given
        /// <paramref name="transform"/> according to <paramref name="position"/>
        /// and <paramref name="yAngle"/>.
        /// </summary>
        /// <param name="transform">transform to be set</param>
        /// <param name="position">new position in world space</param>
        /// <param name="yAngle">new angle w.r.t. y axis in degrees</param>
        internal static void Set(Transform transform, Vector3 position, float yAngle)
        {
            transform.position = position;
            transform.rotation = Quaternion.Euler(0.0f, yAngle, 0.0f);
        }

        /// <summary>
        /// Sets the position (world space) of the given <paramref name="transform"/> according
        /// to <paramref name="position"/>.
        /// </summary>
        /// <param name="transform">transform to be set</param>
        /// <param name="position">new position in world space</param>
        internal static void Set(Transform transform, Vector3 position)
        {
            transform.position = position;
        }

        /// <summary>
        /// Sets the position (world space) and local scale of the given <paramref name="transform"/>
        /// according to <paramref name="position"/> and <paramref name="localScale"/>.
        /// </summary>
        /// <param name="transform">transform to be set</param>
        /// <param name="position">new position in world space</param>
        /// <param name="localScale">new local scale</param>
        internal static void Set(Transform transform, Vector3 position, Vector3 localScale)
        {
            transform.position = position;
            transform.localScale = localScale;
        }
    }
}
