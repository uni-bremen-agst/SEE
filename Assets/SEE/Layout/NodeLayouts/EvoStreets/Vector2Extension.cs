using UnityEngine;

namespace SEE.Layout.NodeLayouts.EvoStreets
{
    /// <summary>
    /// Provides extensions for Vector2.
    /// </summary>
    public static class Vector2Extension
    {
        /// <summary>
        /// Rotates the vector by given <paramref name="angle"/> in degrees.
        /// </summary>
        /// <param name="v">vector to be rotated</param>
        /// <param name="angle">angle of the rotation in degree</param>
        /// <returns></returns>
        public static Vector2 GetRotated(this Vector2 v, float angle)
        {
            float radian = angle * Mathf.Deg2Rad;
            return new Vector2(v.x * Mathf.Cos(radian) - v.y * Mathf.Sin(radian),
                               v.x * Mathf.Sin(radian) + v.y * Mathf.Cos(radian));
        }
    }
}
