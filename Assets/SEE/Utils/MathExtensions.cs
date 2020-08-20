namespace UnityEngine
{
    /// <summary>
    /// Contains extensions for math operations.
    /// </summary>
    public static class MathExtensions
    {
        /// <summary>
        /// The golden ratio.
        /// </summary>
        public const float GoldenRatio = 1.618034f;

        /// <summary>
        /// Divides the components of <paramref name="a"/> by <paramref name="f"/> and
        /// returns the result. If <paramref name="f"/> is <code>0</code>,
        /// <see cref="Vector2.zero"/> is returned.
        /// </summary>
        /// <param name="a">The numerator.</param>
        /// <param name="f">The denominator.</param>
        /// <returns>The pairwise divided vector.</returns>
        public static Vector2 Divide(this Vector2 a, float f)
        {
            if (f == 0)
            {
                return Vector2.zero;
            }
            else
            {
                return new Vector2(a.x / f, a.y / f);
            }
        }

        /// <summary>
        /// Divides the components of <paramref name="a"/> by the components of
        /// <paramref name="b"/> and returns the result. If one component of
        /// <paramref name="b"/> is <code>0</code>, the corresponding component of the resulting
        /// vector is set to <code>0</code> as well.
        /// </summary>
        /// <param name="a">The numerator.</param>
        /// <param name="b">The denominator.</param>
        /// <returns>The pairwise divided vector.</returns>
        public static Vector2 DividePairwise(this Vector2 a, Vector2 b)
        {
            return new Vector2(
                b.x == 0.0f ? 0.0f : a.x / b.x,
                b.y == 0.0f ? 0.0f : a.y / b.y
            );
        }

        /// <summary>
        /// Creates and returns a copy of the x- and y-components of given 3d-vector.
        /// </summary>
        /// <param name="a">The vector.</param>
        /// <returns>The copied components as a 2d-vector.</returns>
        public static Vector2 XY(this Vector3 a) => new Vector2(a.x, a.y);

        /// <summary>
        /// Creates and returns a copy of the x- and z-components of given 3d-vector.
        /// </summary>
        /// <param name="a">The vector.</param>
        /// <returns>The copied components as a 2d-vector.</returns>
        public static Vector2 XZ(this Vector3 a) => new Vector2(a.x, a.z);

        /// <summary>
        /// Creates and returns a copy of the y- and z-components of given 3d-vector.
        /// </summary>
        /// <param name="a">The vector.</param>
        /// <returns>The copied components as a 2d-vector.</returns>
        public static Vector2 YZ(this Vector3 a) => new Vector2(a.y, a.z);

        /// <summary>
        /// Floors the components of given vector to an integer vector and returns the
        /// result.
        /// </summary>
        /// <param name="a">The vector.</param>
        /// <returns>The floored vector.</returns>
        public static Vector3Int FloorToInt(this Vector3 a) => new Vector3Int(Mathf.FloorToInt(a.x), Mathf.FloorToInt(a.y), Mathf.FloorToInt(a.z));

        /// <summary>
        /// Divides the components of <paramref name="a"/> by <paramref name="f"/> and
        /// returns the result. If <paramref name="f"/> is <code>0</code>,
        /// <see cref="Vector3.zero"/> is returned.
        /// </summary>
        /// <param name="a">The numerator.</param>
        /// <param name="f">The denominator.</param>
        /// <returns>The pairwise divided vector.</returns>
        public static Vector3 Divide(this Vector3 a, float f)
        {
            if (f == 0)
            {
                return Vector3.zero;
            }
            else
            {
                return new Vector3(a.x / f, a.y / f, a.z / f);
            }
        }

        /// <summary>
        /// Divides the components of <paramref name="a"/> by the components of
        /// <paramref name="b"/> and returns the result. If one component of
        /// <paramref name="b"/> is <code>0</code>, the corresponding component of the resulting
        /// vector is set to <code>0</code> as well.
        /// </summary>
        /// <param name="a">The numerator.</param>
        /// <param name="b">The denominator.</param>
        /// <returns>The pairwise divided vector.</returns>
        public static Vector3 DividePairwise(this Vector3 a, Vector3 b)
        {
            return new Vector3(
                b.x == 0.0f ? 0.0f : a.x / b.x,
                b.y == 0.0f ? 0.0f : a.y / b.y,
                b.z == 0.0f ? 0.0f : a.z / b.z
            );
        }

        /// <summary>
        /// Returns the angle of <paramref name="a"/> in range [0, 360) in degrees. The
        /// rotation starts with vector (1.0f, 0.0f) at an angle of zero and is assumed
        /// to be clockwise.
        /// 
        ///              270
        ///            _  _  _
        ///         -'    |    '-
        ///       '       |       '
        ///      '        |        '
        ///  180 +--------+--------+ 0
        ///      .        |        .
        ///       .       |       .
        ///  y      -. _  |  _ .-
        ///  ^           90
        ///  |
        ///  +---> x
        /// 
        /// </summary>
        /// <param name="a">The vector of which the angle is to be determined.</param>
        /// <returns>The angle of given vector.</returns>
        public static float Angle360(this Vector2 a)
        {
            float result = Mathf.Atan2(-a.y, a.x) / Mathf.PI * 180f;
            if (result < 0.0f)
            {
                result += 360.0f;
            }
            return result;
        }

        /// <summary>
        /// Returns the angle of <paramref name="a"/> in range [-180, 180) in degrees. The
        /// rotation starts with vector (1.0f, 0.0f) at an angle of zero and is assumed
        /// to be clockwise.
        /// 
        ///              -90
        ///            _  _  _
        ///         -'    |    '-
        ///       '       |       '
        ///      '        |        '
        /// -180 +--------+--------+ 0
        ///      .        |        .
        ///       .       |       .
        ///  y      -. _  |  _ .-
        ///  ^           90
        ///  |
        ///  +---> x
        /// 
        /// </summary>
        /// <param name="a">The vector of which the angle is to be determined.</param>
        /// <returns>The angle of given vector.</returns>
        public static float Angle180(this Vector2 a)
        {
            float result = Mathf.Atan2(-a.y, a.x) / Mathf.PI * 180f;
            return result;
        }

        /// <summary>
        /// Tests collision between a circle of given center and radius and a rectangle
        /// of given min and max. If they do not collide, <paramref name="sqrDistance"/>
        /// is set to the squared distance between the two shapes. Otherwise, it is set
        /// to zero. The return value states whether the two shapes collide.
        /// </summary>
        /// <param name="center">The center of the circle.</param>
        /// <param name="radius">The radius of the circle.</param>
        /// <param name="min">The min corner of the rectangle.</param>
        /// <param name="max">The max corner of the rectangle.</param>
        /// <param name="sqrDistance">The squared distance between the two shapes or
        /// zero, if they collide.</param>
        /// <returns></returns>
        public static bool TestCircleRect(Vector2 center, float radius, Vector2 min, Vector2 max, out float sqrDistance)
        {
            sqrDistance = 0.0f;
            for (int i = 0; i < 2; i++)
            {
                float v = center[i];
                if (v < min[i]) sqrDistance += (min[i] - v) * (min[i] - v);
                if (v > max[i]) sqrDistance += (v - max[i]) * (v - max[i]);
            }
            bool result = sqrDistance <= radius * radius;
            return result;
        }
    }
}

