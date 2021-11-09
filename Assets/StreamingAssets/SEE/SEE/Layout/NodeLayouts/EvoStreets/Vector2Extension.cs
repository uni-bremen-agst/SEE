using UnityEngine;

namespace SEE.Layout.NodeLayouts.EvoStreets
{
    /// <summary>
    /// Provides extensions for Vector2.
    /// </summary>
    public static class Vector2Extension
    {
        public static Vector2 GetRotated(this Vector2 v, float angle)
        {
            float radian = angle * Mathf.Deg2Rad;
            float _x = v.x * Mathf.Cos(radian) - v.y * Mathf.Sin(radian);
            float _y = v.x * Mathf.Sin(radian) + v.y * Mathf.Cos(radian);
            return new Vector2(_x, _y);
        }
    }
}
