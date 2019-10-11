using System.Collections.Generic;
using UnityEngine;
using System;

namespace SEE.Layout
{

    /// <summary>
    /// A Circle can be used by the <see cref="CirclePacker"/>.
    /// </summary>
    public struct Circle
    {
        // The position of the transform will be changed by the circle packer.
        public Transform Transform;

        public float Radius;

        /// <summary>
        /// Creates a new circle at position within given transform and with given radius.
        /// </summary>
        /// <param name="transform">The transform with the position of the circle.</param>
        /// <param name="radius">The radius of the circle.</param>
        public Circle(Transform transform, float radius)
        {
            this.Transform = transform;
            this.Radius = radius;
        }
    }
    
    /// <summary>
    /// This class holds a list of <see cref="Circle"/>-Objects and can pack them closely.
    /// The original source can be found <see href="https://www.codeproject.com/Articles/42067/D-Circle-Packing-Algorithm-Ported-to-Csharp">HERE</see>.
    /// </summary>
    public static class CirclePacker
    {
        /// <summary>
        /// Packs the <paramref name="circles"/> as close together within reasonable time.
        /// </summary>
        /// <param name="circles">The circles to be packed.</param>
        /// <param name="out_outer_radius">The radius of the appoximated minimal enclosing circle.</param>
        public static void Pack(List<Circle> circles, out float out_outer_radius)
        {
            circles.Sort(Comparator);
            for (int i = 0; i < circles.Count - 1; i++)
            {
                for (int j = i + 1; j < circles.Count; j++)
                {
                    if (i == j)
                        continue;

                    Vector3 ab = circles[j].Transform.localPosition - circles[i].Transform.localPosition;
                    float r = circles[i].Radius + circles[j].Radius;

                    float d = Mathf.Max(0.0f, Vector3.SqrMagnitude(ab));

                    if (d < (r * r) - 0.01)
                    {
                        ab.Normalize();
                        ab *= (float)((r - Math.Sqrt(d)) * 0.5f);
                        circles[j].Transform.localPosition += ab;
                        circles[i].Transform.localPosition -= ab;
                    }
                }
            }

            EnclosingCircleIntersectingCircles(circles, out Vector3 out_center, out float out_radius);
            out_outer_radius = out_radius;

            for (int i = 0; i < circles.Count; i++)
            {
                circles[i].Transform.localPosition -= out_center;
            }
        }

        /// <summary>
        /// Finds smallest circle that encloses <paramref name="circles"/>. To improve
        /// performance, <paramref name="circles"/> should be already sorted by radius in
        /// descending order.
        /// 
        /// The original sources can be found
        /// <see href="https://gist.github.com/mbostock/29c534ff0b270054a01c">HERE</see>/> and
        /// <see href="http://www.sunshine2k.de/coding/java/Welzl/Welzl.html">HERE</see>/>.
        /// </summary>
        /// 
        /// <param name="circles">The circles to be enclosed.</param>
        /// 
        /// <param name="out_center">The center of <paramref name="circles"/> enclosing
        /// circle.</param>
        /// 
        /// <param name="out_radius">The radius of <paramref name="circles"/> enclosing
        /// circle.</param>
        private static void EnclosingCircleIntersectingCircles(List<Circle> circles, out Vector3 out_center, out float out_radius)
        {
            EnclosingCircleIntersectingCirclesImpl(new List<Circle>(circles), new List<Circle>(), out Vector3 center, out float radius);
            out_center = center;
            out_radius = radius;
        }

        /// <summary>
        /// Implementation of
        /// <see cref="EnclosingCircleIntersectingCircles(List{Circle}, out Vector3, out float)"/>
        /// .
        /// </summary>
        /// 
        /// <param name="circles">The circles to be enclosed.</param>
        /// 
        /// <param name="borderCircles">The circles that currently represent the border.
        /// <code>borderCircles.Count</code> is always less than or equal to 3.</param>
        /// 
        /// <param name="out_center">The center of <paramref name="borderCircles"/> enclosing
        /// circle.</param>
        /// 
        /// <param name="out_radius">The radius of <paramref name="borderCircles"/> enclosing
        /// circle.</param>
        private static void EnclosingCircleIntersectingCirclesImpl(List<Circle> circles, List<Circle> borderCircles, out Vector3 out_center, out float out_radius)
        {
            out_center = Vector3.zero;
            out_radius = 0.0f;

            if (circles.Count == 0 || borderCircles.Count > 0 && borderCircles.Count > 3)
            {
                switch (borderCircles.Count)
                {
                    case 1:
                        {
                            out_center = borderCircles[0].Transform.position;
                            out_radius = borderCircles[0].Radius;
                            break;
                        }
                    case 2:
                        {
                            CircleIntersectingTwoCircles(borderCircles[0], borderCircles[1], out Vector3 out_center_trivial, out float out_radius_trivial);
                            out_center = out_center_trivial;
                            out_radius = out_radius_trivial;
                            break;
                        }
                    case 3:
                        {
                            CircleIntersectingThreeCircles(borderCircles[0], borderCircles[1], borderCircles[2], out Vector3 out_center_trivial, out float out_radius_trivial);
                            out_center = out_center_trivial;
                            out_radius = out_radius_trivial;
                            break;
                        }
                }

                if (circles.Count == 0)
                {
                    return;
                }
            }

            // This is the smallest circle, if circles are sorted by descending radius
            int smallestCircleIndex = circles.Count - 1;
            Circle smallestCircle = circles[smallestCircleIndex];

            List<Circle> cmc = new List<Circle>(circles);
            cmc.RemoveAt(smallestCircleIndex);

            EnclosingCircleIntersectingCirclesImpl(cmc, borderCircles, out Vector3 out_center_cmc, out float out_radius_cmc);

            if (!CircleContainsCircle(out_center_cmc, out_radius_cmc, smallestCircle))
            {
                List<Circle> bcpc = new List<Circle>(borderCircles);
                bcpc.Add(smallestCircle);

                EnclosingCircleIntersectingCirclesImpl(cmc, bcpc, out Vector3 out_center_cmc_bcpc, out float out_radius_cmc_bcpc);

                out_center = out_center_cmc_bcpc;
                out_radius = out_radius_cmc_bcpc;
            }
            else
            {
                out_center = out_center_cmc;
                out_radius = out_radius_cmc;
            }
        }

        /// <summary>
        /// Returns <see langword="true"/> if circle with <paramref name="position1"/> and
        /// <paramref name="radius1"/> contains <paramref name="c1"/>.
        /// </summary>
        /// 
        /// <param name="position1">Position of containing circle.</param>
        /// <param name="radius1">Radius of containing circle.</param>
        /// <param name="c1">Contained circle.</param>
        /// <returns></returns>
        private static bool CircleContainsCircle(Vector3 position1, float radius1, Circle c1)
        {
            var xc0 = position1.x - c1.Transform.position.x;
            var yc0 = position1.z - c1.Transform.position.z;
            return Mathf.Sqrt(xc0 * xc0 + yc0 * yc0) < radius1 - c1.Radius + float.Epsilon;
        }

        /// <summary>
        /// Calculates smallest enclosing circle of <paramref name="c1"/> and
        /// <paramref name="c2"/>.
        /// </summary>
        /// 
        /// <param name="c1">First circle.</param>
        /// <param name="c2">Second circle.</param>
        /// <param name="out_center">Center of smallest enclosing circle.</param>
        /// <param name="out_radius">Radius of smallest enclosing circle.</param>
        private static void CircleIntersectingTwoCircles(Circle c1, Circle c2, out Vector3 out_center, out float out_radius)
        {
            Vector3 c12 = c2.Transform.position - c1.Transform.position;
            float r12 = c2.Radius - c1.Radius;
            float l = c12.magnitude;
            out_center = (c1.Transform.position + c2.Transform.position + c12 / l * r12) / 2.0f;
            out_radius = (l + c1.Radius + c2.Radius) / 2.0f;
        }

        /// <summary>
        /// Calculates smallest enclosing circle of <paramref name="c1"/>,
        /// <paramref name="c2"/> and <paramref name="c3"/>.
        /// </summary>
        /// 
        /// <param name="c1">First circle.</param>
        /// <param name="c2">Second circle.</param>
        /// <param name="c3">Third circle.</param>
        /// <param name="out_center">Center of smallest enclosing circle.</param>
        /// <param name="out_radius">Radius of smallest enclosing circle.</param>
        private static void CircleIntersectingThreeCircles(Circle c1, Circle c2, Circle c3, out Vector3 out_center, out float out_radius)
        {
            Vector2 p0 = new Vector2(c1.Transform.position.x, c1.Transform.position.z);
            Vector2 p1 = new Vector2(c2.Transform.position.x, c2.Transform.position.z);
            Vector2 p2 = new Vector2(c3.Transform.position.x, c3.Transform.position.z);

            float r0 = c1.Radius;
            float r1 = c2.Radius;
            float r2 = c3.Radius;

            Vector2 a0 = 2.0f * (p0 - p1);
            float a1 = 2.0f * (r1 - r0);
            float a2 = p0.SqrMagnitude() - r0 * r0 - p1.SqrMagnitude() + r1 * r1;

            Vector2 b0 = 2.0f * (p0 - p2);
            float b1 = 2.0f * (r2 - r0);
            float b2 = p0.SqrMagnitude() - r0 * r0 - p2.SqrMagnitude() + r2 * r2;

            float det = b0.x * a0.y - a0.x * b0.y;

            float cx = (a0.y * b2 - b0.y * a2) / det - p1.x;
            float cy = -(a0.x * b2 - b0.x * a2) / det - p1.y;
            float dx = (b0.y * a1 - a0.y * b1) / det;
            float dy = -(b0.x * a1 - a0.x * b1) / det;

            float e1 = dx * dx + dy * dy - 1.0f;
            float e2 = 2.0f * (cx * dx + cy * dy + r1);
            float e3 = cx * cx + cy * cy - r1 * r1;

            out_radius = (-e2 - Mathf.Sqrt(e2 * e2 - 4.0f * e1 * e3)) / (2.0f * e1);
            out_center = new Vector3(cx + dx * out_radius + p1.x, 0.0f, cy + dy * out_radius + p1.y);
        }

        /// <summary>
        /// Compares <paramref name="p1"/> and <paramref name="p2"/> by radius (descending).
        /// </summary>
        /// <param name="p1">First circle.</param>
        /// <param name="p2">Second circle.</param>
        /// <returns></returns>
        private static int Comparator(Circle p1, Circle p2)
        {
            float d1 = p1.Radius;
            float d2 = p2.Radius;
            if (d1 < d2)
                return 1;
            else if (d1 > d2)
                return -1;
            else return 0;
        }
    }

}// namespace SEE.Layout