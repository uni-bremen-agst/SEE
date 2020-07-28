namespace UnityEngine
{
    /// <summary>
    /// Contains extensions for math operations.
    /// </summary>
    public static class MathExtensions
    {
        /// <summary>
        /// Divides the components of <paramref name="a"/> by the components of
        /// <paramref name="b"/> and returns the result.
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
    }
}

