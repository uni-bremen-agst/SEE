using System;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.Layout.NodeLayouts.CirclePacking
{
    /// <summary>
    /// Represents a circle by its center and radius for a given game object to be laid out.
    /// </summary>
    internal class Circle
    {
        /// <summary>
        /// Center of the circle.
        /// </summary>
        public Vector2 center;
        /// <summary>
        /// Radius of the circle
        /// </summary>
        public float radius;
        /// <summary>
        /// The game object represented by this circle.
        /// </summary>
        public ILayoutNode gameObject;

        /// <summary>
        /// Creates a new circle for the game object at given center position with given radius.
        /// </summary>
        /// <param name="gameObject">the game object for which to determine position and radius</param>
        /// <param name="center">center position of the circle</param>
        /// <param name="radius">radius of the circle</param>
        public Circle(ILayoutNode gameObject, Vector2 center, float radius)
        {
            this.gameObject = gameObject;
            this.center = center;
            this.radius = radius;
        }
        /// <summary>
        /// For debugging.
        /// </summary>
        /// <returns>string representation of the circle</returns>
        public override string ToString()
        {
            return "(center= " + center.ToString() + ", radius=" + radius + ")";
        }
    }

    /// <summary>
    /// This class holds a list of <see cref="Circle"/> objects and packs them closely.
    /// The original source can be found 
    /// <see href="https://www.codeproject.com/Articles/42067/D-Circle-Packing-Algorithm-Ported-to-Csharp">HERE</see>.
    /// </summary>
    public static class CirclePacker
    {
        /// <summary>
        /// The default minimal separation between two circles to be placed next to each other.
        /// </summary>
        public const float DefaultMinimalSeparation = 0.1f;

        /// <summary>
        /// The minimal separation between two circles to be placed next to each other,
        /// initially DefaultMinimalSeparation but possibly later adjusted by the world unit.
        /// </summary>
        private static readonly float MinimalSeparation = DefaultMinimalSeparation;

        /// <summary>
        /// Compares <paramref name="c1"/> and <paramref name="c2"/> by radius (descending).
        /// </summary>
        /// <param name="c1">First circle.</param>
        /// <param name="c2">Second circle.</param>
        /// <returns></returns>
        private static int DescendingRadiusComparator(Circle c1, Circle c2)
        {
            float r1 = c1.radius;
            float r2 = c2.radius;
            if (r1 < r2)
            {
                return 1;
            }
            else if (r1 > r2)
            {
                return -1;
            }
            else
            {
                return 0;
            }
        }

        /// <summary>
        /// Packs the <paramref name="circles"/> as close together within reasonable time.
        /// 
        /// Important note: the order of circles may have changed afterward.
        /// </summary>
        /// <param name="relMinDist">The minimal relative distance between circles as a percentage of the radius of the circles, respecively.</param>
        /// <param name="circles">The circles to be packed.</param>
        /// <param name="outOuterRadius">The radius of the appoximated minimal enclosing circle.</param>
        internal static void Pack(float relMinDist, List<Circle> circles, out float outOuterRadius)
        {
            outOuterRadius = 0.0f;

#if UNITY_EDITOR
            if (relMinDist < 0.0f)
            {
                Debug.LogWarning("Relative min distance is negative and will be treated as zero!");
            }
#endif
            if (relMinDist > 0.0f)
            {
                for (int i = 0; i < circles.Count; i++)
                {
                    circles[i].radius *= 1.0f + relMinDist;
                }
            }

            // Sort circles descendingly based on radius
            circles.Sort(DescendingRadiusComparator);

            Vector2 center = Vector2.zero;
            float lastOutRadius = Mathf.Infinity;
            float minSeparationSq = MinimalSeparation * MinimalSeparation;
            int maxIterations = circles.Count; // FIXME: What would be a suitable number of maximal iterations? mCircles.Count?
            for (int iterations = 1; iterations <= maxIterations; iterations++)
            {
                // Each step draws all pairs of circles closer together.
                for (int i = 0; i < circles.Count - 1; i++)
                {
                    for (int j = i + 1; j < circles.Count; j++)
                    {
                        if (i == j)
                        {
                            continue;
                        }

                        // vector between the two centers
                        Vector2 AB = circles[j].center - circles[i].center;
                        // the minimal distance between the two centers so 
                        // that the circles don't overlap
                        float r = circles[i].radius + circles[j].radius;

                        // Length squared = (dx * dx) + (dy * dy);
                        float d = AB.SqrMagnitude() - minSeparationSq;
                        float minSepSq = Math.Min(d, minSeparationSq);
                        d -= minSepSq;

                        if (d < (r * r) - 0.01)
                        {
                            AB.Normalize();

                            AB *= (float)((r - Math.Sqrt(d)) * 0.5f);

                            circles[j].center += AB;
                            circles[i].center -= AB;
                        }
                    }
                }
                SmallestEnclosingCircle(circles, out center, out outOuterRadius);

                // This check and the early termination of the loop may result in fewer
                // iterations. However, it must not create layouts in which the circles
                // overlap. The reason is that initially the circles may overlap 
                // and then the layout is actually expanding to create non-overlapping
                // circles. The phase of expansion is characterized by 
                // outOuterRadius > lastOutRadius. In case of outOuterRadius < lastOutRadius,
                // the shrinking phase has begun. In the shrinking phase, we will terminate early
                // when the ratio of the new and old radius drops below the threshold.
                float ratio = outOuterRadius / lastOutRadius;
                if (lastOutRadius != Mathf.Infinity && !(outOuterRadius > lastOutRadius || ratio < 0.99f))
                {
                    // If the degree of improvement falls below 1%, we will stop.
                    //Debug.LogFormat("Minor improvement of {0} after {1} iterations out of {2}.\n", ratio.ToString("0.0000"), iterations, maxIterations);
                    break;
                }
                lastOutRadius = outOuterRadius;
            }

            if (relMinDist > 0.0f)
            {
                for (int i = 0; i < circles.Count; i++)
                {
                    circles[i].radius *= 1.0f / (1.0f + relMinDist);
                }
                SmallestEnclosingCircle(circles, out center, out outOuterRadius);
                outOuterRadius *= 1.0f + relMinDist;
            }

            // Clients of CirclePacker assume that all co-ordinates of children are relative to Vector3.zero.
            // SmallestEnclosingCircle() may have given us a different center. That is why we need to make
            // adjustments here by subtracting center as delivered by SmallestEnclosingCircle().
            for (int i = 0; i < circles.Count; i++)
            {
                circles[i].center -= center;
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
        /// <param name="outCenter">The center of <paramref name="circles"/> enclosing
        /// circle.</param>
        /// 
        /// <param name="outRadius">The radius of <paramref name="circles"/> enclosing
        /// circle.</param>
        private static void SmallestEnclosingCircle(List<Circle> circles, out Vector2 outCenter, out float outRadius)
        {
            SmallestEnclosingCircleImpl(new List<Circle>(circles), new List<Circle>(), out Vector2 center, out float radius);
            outCenter = center;
            outRadius = radius;
        }

        /// <summary>
        /// Implementation of
        /// <see cref="SmallestEnclosingCircle(List{Circle}, out Vector2, out float)"/>
        /// .
        /// </summary>
        /// 
        /// <param name="circles">The circles to be enclosed.</param>
        /// 
        /// <param name="borderCircles">The circles that currently represent the border.
        /// <code>borderCircles.Count</code> is always less than or equal to 3.</param>
        /// 
        /// <param name="outCenter">The center of <paramref name="borderCircles"/> enclosing
        /// circle.</param>
        /// 
        /// <param name="outRadius">The radius of <paramref name="borderCircles"/> enclosing
        /// circle.</param>
        private static void SmallestEnclosingCircleImpl(List<Circle> circles, List<Circle> borderCircles, out Vector2 outCenter, out float outRadius)
        {
            outCenter = Vector2.zero;
            outRadius = 0.0f;

            if (circles.Count == 0 || borderCircles.Count > 0 && borderCircles.Count > 3)
            {
                switch (borderCircles.Count)
                {
                    case 1:
                        {
                            outCenter = borderCircles[0].center;
                            outRadius = borderCircles[0].radius;
                            break;
                        }
                    case 2:
                        {
                            CircleIntersectingTwoCircles(borderCircles[0], borderCircles[1], out Vector2 outCenterTrivial, out float outRadiusTrivial);
                            outCenter = outCenterTrivial;
                            outRadius = outRadiusTrivial;
                            break;
                        }
                    case 3:
                        {
                            CircleIntersectingThreeCircles(borderCircles[0], borderCircles[1], borderCircles[2], out Vector2 outCenterTrivial, out float outRadiusTrivial);
                            outCenter = outCenterTrivial;
                            outRadius = outRadiusTrivial;
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

            SmallestEnclosingCircleImpl(cmc, borderCircles, out Vector2 outCenterCmc, out float outRadiusCmc);

            if (!CircleContainsCircle(outCenterCmc, outRadiusCmc, smallestCircle))
            {
                List<Circle> bcpc = new List<Circle>(borderCircles);
                bcpc.Add(smallestCircle);

                SmallestEnclosingCircleImpl(cmc, bcpc, out Vector2 outCenterCmcBcpc, out float outRadiusCmcBcpc);

                outCenter = outCenterCmcBcpc;
                outRadius = outRadiusCmcBcpc;
            }
            else
            {
                outCenter = outCenterCmc;
                outRadius = outRadiusCmc;
            }
        }

        /// <summary>
        /// Returns <see langword="true"/> if circle with <paramref name="position"/> and
        /// <paramref name="radius"/> contains <paramref name="circle"/>.
        /// </summary>
        /// 
        /// <param name="position">Position of containing circle.</param>
        /// <param name="radius">Radius of containing circle.</param>
        /// <param name="circle">Contained circle.</param>
        /// <returns></returns>
        private static bool CircleContainsCircle(Vector2 position, float radius, Circle circle)
        {
            float xc0 = position.x - circle.center.x;
            float yc0 = position.y - circle.center.y;
            return Mathf.Sqrt(xc0 * xc0 + yc0 * yc0) < radius - circle.radius + float.Epsilon;
        }

        /// <summary>
        /// Calculates smallest enclosing circle of <paramref name="c1"/> and
        /// <paramref name="c2"/>.
        /// </summary>
        /// 
        /// <param name="c1">First circle.</param>
        /// <param name="c2">Second circle.</param>
        /// <param name="outCenter">Center of smallest enclosing circle.</param>
        /// <param name="outRadius">Radius of smallest enclosing circle.</param>
        private static void CircleIntersectingTwoCircles(Circle c1, Circle c2, out Vector2 outCenter, out float outRadius)
        {
            Vector2 c12 = c2.center - c1.center;
            float r12 = c2.radius - c1.radius;
            float l = c12.magnitude;
            outCenter = (c1.center + c2.center + c12 / l * r12) / 2.0f;
            outRadius = (l + c1.radius + c2.radius) / 2.0f;
        }

        /// <summary>
        /// Calculates smallest enclosing circle of <paramref name="c1"/>,
        /// <paramref name="c2"/> and <paramref name="c3"/>.
        /// </summary>
        /// 
        /// <param name="c1">First circle.</param>
        /// <param name="c2">Second circle.</param>
        /// <param name="c3">Third circle.</param>
        /// <param name="outCenter">Center of smallest enclosing circle.</param>
        /// <param name="outRadius">Radius of smallest enclosing circle.</param>
        private static void CircleIntersectingThreeCircles(Circle c1, Circle c2, Circle c3, out Vector2 outCenter, out float outRadius)
        {
            Vector2 p0 = new Vector2(c1.center.x, c1.center.y);  // FIXME: Can be simplified to p0 = c1.center
            Vector2 p1 = new Vector2(c2.center.x, c2.center.y);
            Vector2 p2 = new Vector2(c3.center.x, c3.center.y);

            float r0 = c1.radius;
            float r1 = c2.radius;
            float r2 = c3.radius;

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

            outRadius = (-e2 - Mathf.Sqrt(e2 * e2 - 4.0f * e1 * e3)) / (2.0f * e1);
            outCenter = new Vector2(cx + dx * outRadius + p1.x, cy + dy * outRadius + p1.y);
        }
    }
}
