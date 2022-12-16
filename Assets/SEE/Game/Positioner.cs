using System;
using UnityEngine;

namespace SEE.Game
{
    /// <summary>
    /// Provides positioning and rotation.
    /// </summary>
    internal static class Positioner
    {
        
        // TODO: Ultimately, all calls in here should be replaced with NodeOperator calls.
        
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
    }
}
