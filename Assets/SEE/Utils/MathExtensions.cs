using UnityEngine;

namespace SEE.Utils
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
        public static float Angle180(this Vector2 a) => Mathf.Atan2(-a.y, a.x) / Mathf.PI * 180f;

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
        /// <paramref name="b"/> is <c>0</c>, the corresponding component of the resulting
        /// vector is set to <c>0</c> as well.
        /// </summary>
        /// <param name="a">The numerator.</param>
        /// <param name="b">The denominator.</param>
        /// <returns>The pairwise divided vector.</returns>
        public static Vector2 DividePairwise(this Vector2 a, Vector2 b) =>
            new Vector2(
                b.x == 0.0f ? 0.0f : a.x / b.x,
                b.y == 0.0f ? 0.0f : a.y / b.y
            );

        /// <summary>
        /// Returns the largest component of given vector.
        /// </summary>
        /// <param name="a">The vector.</param>
        /// <returns>The largest component of given vector.</returns>
        public static float MaxComponent(this Vector2 a) => Mathf.Max(a.x, a.y);

        /// <summary>
        /// Returns the smallest component of given vector.
        /// </summary>
        /// <param name="a">The vector.</param>
        /// <returns>The smallest component of given vector.</returns>
        public static float MinComponent(this Vector2 a) => Mathf.Min(a.x, a.y);

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
        /// <paramref name="b"/> is <c>0</c>, the corresponding component of the resulting
        /// vector is set to <c>0</c> as well.
        /// </summary>
        /// <param name="a">The numerator.</param>
        /// <param name="b">The denominator.</param>
        /// <returns>The pairwise divided vector.</returns>
        public static Vector3 DividePairwise(this Vector3 a, Vector3 b) =>
            new Vector3(
                b.x == 0.0f ? 0.0f : a.x / b.x,
                b.y == 0.0f ? 0.0f : a.y / b.y,
                b.z == 0.0f ? 0.0f : a.z / b.z
            );

        /// <summary>
        /// Floors the components of given vector to an integer vector and returns the
        /// result.
        /// </summary>
        /// <param name="a">The vector.</param>
        /// <returns>The floored vector.</returns>
        public static Vector3Int FloorToInt(this Vector3 a) =>
            new Vector3Int(Mathf.FloorToInt(a.x), Mathf.FloorToInt(a.y), Mathf.FloorToInt(a.z));

        /// <summary>
        /// Returns the largest component of given vector.
        /// </summary>
        /// <param name="a">The vector.</param>
        /// <returns>The largest component of given vector.</returns>
        public static float MaxComponent(this Vector3 a) => Mathf.Max(a.x, a.y, a.z);

        /// <summary>
        /// Returns the smallest component of given vector.
        /// </summary>
        /// <param name="a">The vector.</param>
        /// <returns>The smallest component of given vector.</returns>
        public static float MinComponent(this Vector3 a) => Mathf.Min(a.x, a.y, a.z);

        /// <summary>
        /// Performs a collision test between a circle and an axis-aligned bounding box.
        /// The distance and a normal pointing from the circle towards to the surface of
        /// the AABB are returned. If the circle lies inside of the AABB, the distance is
        /// negative. If the circle just touches the surface of the AABB, the distance
        /// and the magnitude of the normal are zero.
        ///
        /// This method uses the idea of the minkowski sum to convert a collision test
        /// between a circle and an AABB into a test with a single point against four
        /// circles and two rectangles.
        /// </summary>
        /// <param name="center">The center of the circle.</param>
        /// <param name="radius">The radius of the circle.</param>
        /// <param name="min">The min corner of the AABB.</param>
        /// <param name="max">The max corner of the AABB.</param>
        /// <param name="distance">The distance betweem the circle and the surface of the
        /// AABB.</param>
        /// <param name="normalizedToSurfaceDirection">The normalized direction from the
        /// circle towards the closest point on the AABB. If the circle just touches the
        /// surface, the magnitude is zero.
        /// </param>
        public static void TestCircleAABB(Vector2 center, float radius, Vector2 min, Vector2 max, out float distance, out Vector2 normalizedToSurfaceDirection)
        {
            Vector2 point = center;

            distance = float.MaxValue;
            normalizedToSurfaceDirection = Vector2.zero;

            Vector2[] centers = {
                min,
                new Vector2(max.x, min.y),
                new Vector2(min.x, max.y),
                max
            };

            for (int i = 0; i < 4; i++)
            {
                TestPointCircle(point, centers[i], radius, out float dist, out Vector2 dir);
                if (dist < distance)
                {
                    distance = dist;
                    normalizedToSurfaceDirection = dir;
                }
            }

            Vector2[] aabbMins = {
                new Vector2(min.x - radius, min.y),
                new Vector2(min.x, min.y - radius)
            };

            Vector2[] aabbMaxs = {
                new Vector2(max.x + radius, max.y),
                new Vector2(max.x, max.y + radius)
            };

            for (int i = 0; i < 2; i++)
            {
                TestPointAABB(point, aabbMins[i], aabbMaxs[i], out float dist, out Vector2 dir);
                if (dist < distance)
                {
                    distance = dist;
                    normalizedToSurfaceDirection = dir;
                }
            }
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
        /// Creates and returns a copy of the x, y- and z-components of given 4d-vector.
        /// </summary>
        /// <param name="a">The vector.</param>
        /// <returns>The copied components as a 3d-vector.</returns>
        public static Vector3 XYZ(this Vector4 a) => new Vector3(a.x, a.y, a.z);

        /// <summary>
        /// Creates and returns a copy of the y, z- and w-components of given 4d-vector.
        /// </summary>
        /// <param name="a">The vector.</param>
        /// <returns>The copied components as a 3d-vector.</returns>
        public static Vector3 YZW(this Vector4 a) => new Vector3(a.y, a.z, a.w);

        /// <summary>
        /// Creates and returns a copy of the x- and y-components of given 4d-vector.
        /// </summary>
        /// <param name="a">The vector.</param>
        /// <returns>The copied components as a 2d-vector.</returns>
        public static Vector2 XY(this Vector4 a) => new Vector2(a.x, a.y);

        /// <summary>
        /// Creates and returns a copy of the x- and z-components of given 4d-vector.
        /// </summary>
        /// <param name="a">The vector.</param>
        /// <returns>The copied components as a 2d-vector.</returns>
        public static Vector2 XZ(this Vector4 a) => new Vector2(a.x, a.z);

        /// <summary>
        /// Creates and returns a copy of the x- and w-components of given 4d-vector.
        /// </summary>
        /// <param name="a">The vector.</param>
        /// <returns>The copied components as a 2d-vector.</returns>
        public static Vector2 XW(this Vector4 a) => new Vector2(a.x, a.w);

        /// <summary>
        /// Creates and returns a copy of the y- and z-components of given 4d-vector.
        /// </summary>
        /// <param name="a">The vector.</param>
        /// <returns>The copied components as a 2d-vector.</returns>
        public static Vector2 YZ(this Vector4 a) => new Vector2(a.y, a.z);

        /// <summary>
        /// Creates and returns a copy of the y- and w-components of given 4d-vector.
        /// </summary>
        /// <param name="a">The vector.</param>
        /// <returns>The copied components as a 2d-vector.</returns>
        public static Vector2 YW(this Vector4 a) => new Vector2(a.y, a.w);

        /// <summary>
        /// Creates and returns a copy of the z- and w-components of given 4d-vector.
        /// </summary>
        /// <param name="a">The vector.</param>
        /// <returns>The copied components as a 2d-vector.</returns>
        public static Vector2 ZW(this Vector4 a) => new Vector2(a.z, a.w);

        /// <summary>
        /// Returns the given <paramref name="vector3"/>, replacing any of its components with
        /// <paramref name="x"/>, <paramref name="y"/>, and/or <paramref name="z"/>, respectively, if they were given.
        /// If no parameters are given, this method will be equivalent to the identity function.
        /// </summary>
        /// <param name="vector3">The vector whose components shall be replaced</param>
        /// <param name="x">New X component</param>
        /// <param name="y">New Y component</param>
        /// <param name="z">New Z component</param>
        /// <returns><paramref name="vector2"/> with its components replaced</returns>
        public static Vector3 WithXYZ(this Vector3 vector3, float? x = null, float? y = null, float? z = null)
        {
            return new Vector3(x ?? vector3.x, y ?? vector3.y, z ?? vector3.z);
        }

        /// <summary>
        /// Returns the given <paramref name="vector2"/>, replacing any of its components with
        /// <paramref name="x"/>, and/or <paramref name="y"/> respectively, if they were given.
        /// If no parameters are given, this method will be equivalent to the identity function.
        /// </summary>
        /// <param name="vector2">The vector whose components shall be replaced</param>
        /// <param name="x">New X component</param>
        /// <param name="y">New Y component</param>
        /// <returns><paramref name="vector2"/> with its components replaced</returns>
        public static Vector2 WithXY(this Vector2 vector2, float? x = null, float? y = null)
        {
            return new Vector2(x ?? vector2.x, y ?? vector2.y);
        }

        /// <summary>
        /// Performs a collision test between a point and a circle. The distance and a
        /// normal pointing from the point towards to the surface of the circle are
        /// returned. If the point lies inside of the AABB, the distance is negative. If
        /// the point lies exactly on the surface, the distance and the magnitude of the
        /// normal are zero.
        /// </summary>
        /// <param name="point">The position of the point.</param>
        /// <param name="center">The center of the circle.</param>
        /// <param name="radius">The radius of the circle.</param>
        /// <param name="distance">The distance between the point and the surface of the
        /// circle.</param>
        /// <param name="normalizedToSurfaceDirection">The normalized direction from the
        /// point towards the closest point on the circle. If the point lies exactly on
        /// the surface, the magnitude is zero.</param>
        public static void TestPointCircle(Vector2 point, Vector2 center, float radius, out float distance, out Vector2 normalizedToSurfaceDirection)
        {
            Vector2 pointToCenter = center - point;
            float magnitude = pointToCenter.magnitude;

            distance = magnitude - radius;
            normalizedToSurfaceDirection = magnitude == 0.0f ? Vector2.zero : pointToCenter / magnitude;

            if (distance < 0.0f)
            {
                normalizedToSurfaceDirection *= -1.0f;
            }
        }

        /// <summary>
        /// Performs a collision test between a point and an axis-aligned bounding box.
        /// The distance and a normal pointing from the point towards to the surface of
        /// the AABB are returned. If the point lies inside of the AABB, the distance is
        /// negative. If the point lies exactly on the surface, the distance and the
        /// magnitude of the normal are zero.
        /// </summary>
        /// <param name="point">The position of the point.</param>
        /// <param name="min">The min corner of the AABB.</param>
        /// <param name="max">The max corner of the AABB.</param>
        /// <param name="distance">The distance betweem the point and the surface of the
        /// AABB.</param>
        /// <param name="normalizedToSurfaceDirection">The normalized direction from the
        /// point towards the closest point on the AABB. If the point lies exactly on the
        /// surface, the magnitude is zero.
        /// </param>
        public static void TestPointAABB(Vector2 point, Vector2 min, Vector2 max, out float distance, out Vector2 normalizedToSurfaceDirection)
        {
            float x0 = min.x - point.x;
            float x1 = point.x - max.x;
            float y0 = min.y - point.y;
            float y1 = point.y - max.y;

            if (point.y > min.y && point.y < max.y)
            {
                if (x0 > x1)
                {
                    distance = x0;
                    normalizedToSurfaceDirection = new Vector2(x0 > 0.0f ? 1.0f : 0.0f, 0.0f);
                }
                else if (x1 > x0)
                {
                    distance = x1;
                    normalizedToSurfaceDirection = new Vector2(x1 > 0.0f ? -1.0f : 0.0f, 0.0f);
                }
                else
                {
                    distance = 0.0f;
                    normalizedToSurfaceDirection = Vector2.zero;
                }
            }
            else if (point.x > min.x && point.x < max.x)
            {
                if (y0 > y1)
                {
                    distance = y0;
                    normalizedToSurfaceDirection = new Vector2(0.0f, y0 > 0.0f ? 1.0f : -1.0f);
                }
                else if (y1 > y0)
                {
                    distance = y1;
                    normalizedToSurfaceDirection = new Vector2(0.0f, y1 > 0.0f ? -1.0f : 1.0f);
                }
                else
                {
                    distance = 0.0f;
                    normalizedToSurfaceDirection = Vector2.zero;
                }
            }
            else
            {
                normalizedToSurfaceDirection = new Vector2(x0 > x1 ? x0 : -x1, y0 > y1 ? y0 : -y1);
                distance = normalizedToSurfaceDirection.magnitude;
                normalizedToSurfaceDirection /= distance;
            }
        }
    }
}

