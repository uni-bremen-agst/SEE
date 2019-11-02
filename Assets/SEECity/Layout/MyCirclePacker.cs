using SEE.DataModel;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SEEC.Layout
{
    internal class MyCircle
    {
        public Vector2 center;
        public float radius;
        public GameObject gameObject;

        /// <summary>
        /// Creates a new circle for the game object at given center position with given radius.
        /// </summary>
        /// <param name="gameObject">the game object for which to determine position and radius</param>
        /// <param name="center">center position of the circle</param>
        /// <param name="radius">radius of the circle</param>
        public MyCircle(GameObject gameObject, Vector2 center, float radius)
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
    /// This class holds a list of <see cref="MyCircle"/>-Objects and can pack them closely.
    /// The original source can be found <see href="https://www.codeproject.com/Articles/42067/D-Circle-Packing-Algorithm-Ported-to-Csharp">HERE</see>.
    /// </summary>
    public static class MyCirclePacker
    {
        // FIXME: Should be relative to Unit()
        public static float mMinSeparation = 0.01f;

        public static void Test()
        {
            int pNumCircles = 10;
            float pMinRadius = 4.0f;
            float pMaxRadius = 20.0f;

            // List<Circle> mCircles = CreateRandomCircles(pNumCircles, pMinRadius, pMaxRadius);
            // List<Circle> mCircles = CreateCirclesInLine(pNumCircles, pMinRadius, pMaxRadius);
            List<MyCircle> mCircles = CreateCirclesInCircle(pNumCircles, pMinRadius, pMaxRadius);

            Pack(mCircles, out float out_outer_radius);
            Draw(mCircles);
        }

        private static GameObject NewCylinder()
        {
            GameObject cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cylinder.tag = Tags.Node;
            return cylinder;
        }

        private static List<MyCircle> CreateRandomCircles(int pNumCircles, double pMinRadius, double pMaxRadius)
        {
            // Create random circles
            List<MyCircle> mCircles = new List<MyCircle>();
            Vector2 mPackingCenter = Vector2.zero;
            System.Random Rnd = new System.Random(1); // System.DateTime.Now.Millisecond
            for (int i = 0; i < pNumCircles; i++)
            {
                Vector2 nCenter = new Vector2((float)(mPackingCenter.x +
                                                      Rnd.NextDouble() * pMinRadius),
                                              (float)(mPackingCenter.y +
                                                      Rnd.NextDouble() * pMinRadius));
                float nRadius = (float)(pMinRadius + Rnd.NextDouble() * (pMaxRadius - pMinRadius));
                mCircles.Add(new MyCircle(NewCylinder(), nCenter, nRadius));
            }
            return mCircles;
        }

        private static List<MyCircle> CreateCirclesInLine(int pNumCircles, double pMinRadius, double pMaxRadius)
        {
            // Create random circles
            List<MyCircle> mCircles = new List<MyCircle>();
            System.Random Rnd = new System.Random(1); 
            for (int i = 0; i < pNumCircles; i++)
            {
                Vector2 nCenter = i * Vector2.one;
                float nRadius = (float)(pMinRadius + Rnd.NextDouble() * (pMaxRadius - pMinRadius));
                mCircles.Add(new MyCircle(NewCylinder(), nCenter, nRadius));
            }
            return mCircles;
        }

        private static List<MyCircle> CreateCirclesInCircle(int pNumCircles, double pMinRadius, double pMaxRadius)
        {
            // Create random circles
            List<MyCircle> mCircles = new List<MyCircle>();
            System.Random Rnd = new System.Random(1);
            for (int i = 0; i < pNumCircles; i++)
            {
                float nRadius = (float)(pMinRadius + Rnd.NextDouble() * (pMaxRadius - pMinRadius));
                float radians = ((float)i / (float)pNumCircles) * (2.0f * Mathf.PI);
                Vector2 nCenter = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * nRadius;
                mCircles.Add(new MyCircle(NewCylinder(), nCenter, nRadius));
            }
            return mCircles;
        }

        /*
        /// <summary>
        /// Returns the square of the length of given circle center to the packing center.
        /// </summary>
        /// <param name="?"></param>
        /// <returns></returns>
        private static float DistanceToCenterSq(Circle pCircle)
        {
            return (pCircle.center - mPackingCenter).SqrMagnitude();
        }

        /// <summary>
        ///
        /// </summary>
        private static int DistanceToCenterComparer(Circle p1, Circle P2)
        {
            float d1 = DistanceToCenterSq(p1);
            float d2 = DistanceToCenterSq(P2);
            if (d1 < d2)
                return 1;
            else if (d1 > d2)
                return -1;
            else return 0;
        }
        */

        /// <summary>
        /// Compares <paramref name="c1"/> and <paramref name="c2"/> by radius (descending).
        /// </summary>
        /// <param name="c1">First circle.</param>
        /// <param name="c2">Second circle.</param>
        /// <returns></returns>
        private static int DescendingRadiusComparator(MyCircle c1, MyCircle c2)
        {
            float r1 = c1.radius;
            float r2 = c2.radius;
            if (r1 < r2)
                return 1;
            else if (r1 > r2)
                return -1;
            else return 0;
        }

        private static int AscendingRadiusComparator(MyCircle c1, MyCircle c2)
        {
            return -DescendingRadiusComparator(c1, c2);
        }

        /// <summary>
        /// Packs the <paramref name="circles"/> as close together within reasonable time.
        /// 
        /// Important note: the order of circles may have changed afterward.
        /// </summary>
        /// <param name="circles">The circles to be packed.</param>
        /// <param name="out_outer_radius">The radius of the appoximated minimal enclosing circle.</param>
        internal static void Pack(List<MyCircle> circles, out float out_outer_radius)
        {
            //if (circles.Count == 0)
            //{
            //    center = Vector2.zero;
            //    out_outer_radius = 0.0f;
            //}
            //else if (circles.Count == 1)
            //{
            //    center = Vector2.zero;
            //    circles[0].center = center;
            //    out_outer_radius = circles[0].radius;
            //}
            //else if (circles.Count == 2)
            //{
            //    float r0 = circles[0].radius;
            //    float r1 = circles[1].radius;
            //    Debug.Assert(r0 >= r1);
            //    circles[0].center = Vector2.zero;
            //    circles[1].center = circles[0].center + (r0 + r1) * Vector2.right;
            //    // distance between centers of both circles (they are to touch each other)
            //    SmallestEnclosingCircle(circles, out center, out out_outer_radius);

            //    //{
            //    //    List<MyCircle> debugCircles = new List<MyCircle>();
            //    //    debugCircles.AddRange(circles);
            //    //    debugCircles.Add(new MyCircle(null, center, out_outer_radius));
            //    //    DrawCircles(debugCircles);
            //    //}
            //}
            //else
            {
                out_outer_radius = 0.0f;
                // Sort circles descendingly based on radius
                //mCircles.Sort(DistanceToCenterComparer);
                circles.Sort(DescendingRadiusComparator);
                //mCircles.Sort(AscendingRadiusComparator);

                Vector2 center = Vector2.zero;
                float last_out_radius = Mathf.Infinity;
                float minSeparationSq = mMinSeparation * mMinSeparation;
                int max_iterations = circles.Count; // FIXME: What would be a suitable number of maximal iterations? mCircles.Count?
                for (int iterations = 1; iterations <= max_iterations; iterations++)
                {
                    // Each step draws all pairs of circles closer together.
                    for (int i = 0; i < circles.Count - 1; i++)
                    {
                        for (int j = i + 1; j < circles.Count; j++)
                        {
                            if (i == j)
                                continue;

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
                    SmallestEnclosingCircle(circles, out center, out out_outer_radius);

                    float improvement = out_outer_radius / last_out_radius;
                    if (last_out_radius != Mathf.Infinity && improvement < 1.01f)
                    {
                        // If the degree of improvement falls below 1%, we will stop.
                        Debug.LogFormat("Minor improvement of {0} after {1} iterations.\n", improvement, iterations);
                        //break;
                    }
                    //else
                    //{
                    //    Debug.LogFormat("Improvement: {0}\n", improvement);
                    //}
                    last_out_radius = out_outer_radius;
                }
                // Clients of CirclePacker assume that all co-ordinates of children are relative to Vector3.zero.
                // SmallestEnclosingCircle() may have given us a different center. That is why we need to make
                // adjustments here by subtracting center as delivered by SmallestEnclosingCircle().
                for (int i = 0; i < circles.Count; i++)
                {
                    circles[i].center -= center;
                }
            }
        }

        internal static void Draw(List<MyCircle> mCircles)
        {
            foreach(MyCircle c in mCircles)
            {
                GameObject gameObject = c.gameObject;
                gameObject.transform.position = new Vector3(c.center.x, 0.0f, c.center.y);
                gameObject.transform.localScale = new Vector3(c.radius, 1.0f, c.radius);
            }
        }

        private static void DrawCircles(List<MyCircle> mCircles)
        {
            int i = 0;
            foreach (MyCircle c in mCircles)
            {
                bool exists = c.gameObject != null;
                GameObject gameObject = exists ? GameObject.Instantiate(c.gameObject) : NewCylinder();
                gameObject.transform.position = new Vector3(c.center.x, 0.0f, c.center.y);
                if (! exists)
                {
                    gameObject.name = "circle " + i;
                    gameObject.transform.localScale = new Vector3(2 * c.radius, 0.1f, 2 * c.radius);
                }
                i++;
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
        private static void SmallestEnclosingCircle(List<MyCircle> circles, out Vector2 out_center, out float out_radius)
        {
            SmallestEnclosingCircleImpl(new List<MyCircle>(circles), new List<MyCircle>(), out Vector2 center, out float radius);
            out_center = center;
            out_radius = radius;
        }

        public static void TestCirclePacker()
        {
            TestPack();
            TestSmallestEnclosingCircle();
        }

        private static void TestPack()
        {
            TestPackSingle();
            TestPackPair();
        }

        private static void AssertRadius(float expected, float actual)
        {
            Debug.AssertFormat(expected == actual, "expected radius: {0} actual: {1}\n", expected, actual);
        }

        private static void AssertCenter(Vector2 expected, Vector2 actual)
        {
            Debug.AssertFormat(expected == actual, "expected center: {0} actual: {1}\n", expected, actual);
        }

        private static List<MyCircle> PrepareForPack(List<float> radii)
        {
            List<MyCircle> circles = new List<MyCircle>();
            int i = 0;
            foreach (float radius in radii)
            {
                // Position the children on a circle as required by CirclePacker.Pack.
                float radians = ((float)i / (float)radii.Count) * (2.0f * Mathf.PI);
                circles.Add(new MyCircle(null, new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * radius, radius));
                i++;
            }
            circles.Sort(DescendingRadiusComparator);
            return circles;
        }

        private static void TestPackSingle()
        {
            MyCircle myCircle = new MyCircle(null, Vector2.zero, 1.0f);
            List<MyCircle> circles = new List<MyCircle>() { myCircle };
            Pack(circles, out float radius);
            AssertRadius(myCircle.radius, radius);
        }

        private static void TestPackPair()
        {
            float r1 = 3.0f;
            float r2 = 1.0f;
            List<float> radii = new List<float>() { r1, r2 };
            List<MyCircle> circles = PrepareForPack(radii);
            float savedMinSeparation = mMinSeparation;
            mMinSeparation = 0.0f;
            Pack(circles, out float radius);
            mMinSeparation = savedMinSeparation;
            AssertRadius(r1 + r2, radius);
        }

        private static void TestSmallestEnclosingCircle()
        {
            TestSmallestEnclosingCircleSingle();
            TestSmallestEnclosingCirclePair();
            TestSmallestEnclosingCirclePairDifferentSizes();
            TestSmallestEnclosingCircleThreeDifferentSizes();
            TestSmallestEnclosingCircleMultiple();
        }

        private static void TestSmallestEnclosingCircleSingle()
        {
            MyCircle myCircle = new MyCircle(null, Vector2.zero, 1.0f);
            List<MyCircle> circles = new List<MyCircle>() { myCircle };
            SmallestEnclosingCircle(circles, out Vector2 center, out float radius);
            Debug.Assert(center == myCircle.center);
            Debug.Assert(radius == myCircle.radius);
        }

        private static void TestSmallestEnclosingCirclePair()
        {
            float r = 1.0f;
            MyCircle c1 = new MyCircle(null, Vector2.zero, r);
            MyCircle c2 = new MyCircle(null, Vector2.zero + 2 * r * Vector2.right, r);
            // Must be sorted by radius in descending order.
            // Must not overlap.
            List<MyCircle> circles = new List<MyCircle>() { c1, c2 };
            SmallestEnclosingCircle(circles, out Vector2 center, out float radius);
            Debug.Assert(center == Vector2.zero + r * Vector2.right);
            Debug.Assert(radius == 2 * r);
        }

        private static void TestSmallestEnclosingCirclePairDifferentSizes()
        {
            float r1 = 2.0f;
            float r2 = 0.1f;
            List<float> radii = new List<float>() { r1, r2 };
            List<MyCircle> circles = PrepareForPack(radii);
            SmallestEnclosingCircle(circles, out Vector2 center, out float radius);
            AssertCenter(new Vector2(1.9f, 0.0f), center);
            AssertRadius(2.1f, radius);
        }

        private static void TestSmallestEnclosingCircleThreeDifferentSizes()
        {
            float r1 = 2.0f;
            float r2 = 0.1f;
            float r3 = 0.1f;
            List<float> radii = new List<float>() { r1, r2, r3 };
            List<MyCircle> circles = PrepareForPack(radii);
            SmallestEnclosingCircle(circles, out Vector2 center, out float radius);
            AssertCenter(new Vector2(1.9f, 0.0f), center);
            AssertRadius(2.07595f, radius);
        }

        private static void TestSmallestEnclosingCircleMultiple()
        {
            float r = 1.0f;
            float d = 10.0f;
            MyCircle c1 = new MyCircle(null, Vector2.zero + d * Vector2.up, r);
            MyCircle c2 = new MyCircle(null, Vector2.zero + d * Vector2.down, r);
            MyCircle c3 = new MyCircle(null, Vector2.zero + 0.5f * d * Vector2.right, r);
            MyCircle c4 = new MyCircle(null, Vector2.zero + 0.75f * d * Vector2.left, r);
            List<MyCircle> circles = new List<MyCircle>() { c1, c2, c3, c4 };
            SmallestEnclosingCircle(circles, out Vector2 center, out float radius);
            Debug.Assert(center == Vector2.zero);
            Debug.AssertFormat(radius == d + r, "expected: {0} actual: {1}\n", d + r, radius);
        }

        /// <summary>
        /// Implementation of
        /// <see cref="SmallestEnclosingCircle(List{MyCircle}, out Vector2, out float)"/>
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
        private static void SmallestEnclosingCircleImpl(List<MyCircle> circles, List<MyCircle> borderCircles, out Vector2 out_center, out float out_radius)
        {
            out_center = Vector2.zero;
            out_radius = 0.0f;

            if (circles.Count == 0 || borderCircles.Count > 0 && borderCircles.Count > 3)
            {
                switch (borderCircles.Count)
                {
                    case 1:
                        {
                            out_center = borderCircles[0].center;
                            out_radius = borderCircles[0].radius;
                            break;
                        }
                    case 2:
                        {
                            CircleIntersectingTwoCircles(borderCircles[0], borderCircles[1], out Vector2 out_center_trivial, out float out_radius_trivial);
                            out_center = out_center_trivial;
                            out_radius = out_radius_trivial;
                            break;
                        }
                    case 3:
                        {
                            CircleIntersectingThreeCircles(borderCircles[0], borderCircles[1], borderCircles[2], out Vector2 out_center_trivial, out float out_radius_trivial);
                            out_center = out_center_trivial;
                            out_radius = out_radius_trivial;
                            break;
                        }
                }
                //out_center.y = 0.0f;

                if (circles.Count == 0)
                {
                    return;
                }
            }

            // This is the smallest circle, if circles are sorted by descending radius
            int smallestCircleIndex = circles.Count - 1;
            MyCircle smallestCircle = circles[smallestCircleIndex];

            List<MyCircle> cmc = new List<MyCircle>(circles);
            cmc.RemoveAt(smallestCircleIndex);

            SmallestEnclosingCircleImpl(cmc, borderCircles, out Vector2 out_center_cmc, out float out_radius_cmc);

            if (!CircleContainsCircle(out_center_cmc, out_radius_cmc, smallestCircle))
            {
                List<MyCircle> bcpc = new List<MyCircle>(borderCircles);
                bcpc.Add(smallestCircle);

                SmallestEnclosingCircleImpl(cmc, bcpc, out Vector2 out_center_cmc_bcpc, out float out_radius_cmc_bcpc);

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
        /// Returns <see langword="true"/> if circle with <paramref name="position"/> and
        /// <paramref name="radius"/> contains <paramref name="circle"/>.
        /// </summary>
        /// 
        /// <param name="position">Position of containing circle.</param>
        /// <param name="radius">Radius of containing circle.</param>
        /// <param name="circle">Contained circle.</param>
        /// <returns></returns>
        private static bool CircleContainsCircle(Vector2 position, float radius, MyCircle circle)
        {
            var xc0 = position.x - circle.center.x;
            var yc0 = position.y - circle.center.y;
            return Mathf.Sqrt(xc0 * xc0 + yc0 * yc0) < radius - circle.radius + float.Epsilon;
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
        private static void CircleIntersectingTwoCircles(MyCircle c1, MyCircle c2, out Vector2 out_center, out float out_radius)
        {
            Vector2 c12 = c2.center - c1.center;
            float r12 = c2.radius - c1.radius;
            float l = c12.magnitude;
            out_center = (c1.center + c2.center + c12 / l * r12) / 2.0f;
            out_radius = (l + c1.radius + c2.radius) / 2.0f;
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
        private static void CircleIntersectingThreeCircles(MyCircle c1, MyCircle c2, MyCircle c3, out Vector2 out_center, out float out_radius)
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

            out_radius = (-e2 - Mathf.Sqrt(e2 * e2 - 4.0f * e1 * e3)) / (2.0f * e1);
            out_center = new Vector2(cx + dx * out_radius + p1.x, cy + dy * out_radius + p1.y);
        }
    }
}
