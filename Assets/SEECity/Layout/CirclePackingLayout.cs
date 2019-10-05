using System.Collections.Generic;
using UnityEngine;
using SEE.DataModel;
using System;

namespace SEE.Layout
{

    /// <summary>
    /// This layout packs circles closely together to decrease total area of city.
    /// </summary>
    public class CirclePackingLayout : ILayout
    {
        /// <summary>
        /// A Circle can be used by the <see cref="CirclePacker"/>.
        /// </summary>
        private struct Circle
        {
            // The position of the transform will be changed by the circle packer.
            public Transform Transform { get; set; }

            public float Radius { get; }

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
        private static class CirclePacker
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
                
                foreach (Circle c in circles)
                {
                    c.Transform.localPosition -= out_center;
                }
            }

            /// <summary>
            /// The original source can be found <see href="https://gist.github.com/mbostock/29c534ff0b270054a01c">HERE</see>/>.
            /// </summary>
            /// <param name="circles"></param>
            /// <param name="out_center"></param>
            /// <param name="out_radius"></param>
            private static void EnclosingCircleIntersectingCircles(List<Circle> circles, out Vector3 out_center, out float out_radius)
            {
                EnclosingCircleIntersectingCircles(new List<Circle>(circles), new List<Circle>(), out Vector3 center, out float radius);
                out_center = center;
                out_radius = radius;
            }

            private static void EnclosingCircleIntersectingCircles(List<Circle> circles, List<Circle> borderCircles, out Vector3 out_center, out float out_radius)
            {
                out_center = Vector3.zero;
                out_radius = 0.0f;

                if (circles.Count == 0 || borderCircles.Count == 3)
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


                Circle c = circles[0];
                List<Circle> cmc = new List<Circle>(circles);
                cmc.RemoveAt(0);
                EnclosingCircleIntersectingCircles(cmc, borderCircles, out Vector3 out_center_cmc, out float out_radius_cmc);
                if (!CircleContainsCircle(out_center_cmc, out_radius_cmc, c))
                {
                    List<Circle> bcpc = new List<Circle>(borderCircles);
                    bcpc.Add(c);
                    EnclosingCircleIntersectingCircles(cmc, bcpc, out Vector3 out_center_cmc_bcpc, out float out_radius_cmc_bcpc);
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
            /// Returns <see langword="true"/> if circle with <paramref name="position1"/> and <paramref name="radius1"/> contains <paramref name="c1"/>.
            /// </summary>
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

            private static void CircleIntersectingTwoCircles(Circle c1, Circle c2, out Vector3 out_center, out float out_radius)
            {
                Vector3 c12 = c2.Transform.position - c1.Transform.position;
                float r12 = c2.Radius - c1.Radius;
                float l = c12.magnitude;
                out_center = (c1.Transform.position + c2.Transform.position + c12 / l * r12) / 2.0f;
                out_radius = (l + c1.Radius + c2.Radius) / 2.0f;
            }

            private static void CircleIntersectingThreeCircles(Circle circle1, Circle circle2, Circle circle3, out Vector3 out_center, out float out_radius)
            {
                var x1 = circle1.Transform.position.x;
                var y1 = circle1.Transform.position.z;
                var r1 = circle1.Radius;

                var x2 = circle2.Transform.position.x;
                var y2 = circle2.Transform.position.z;
                var r2 = circle2.Radius;

                var x3 = circle3.Transform.position.x;
                var y3 = circle3.Transform.position.z;
                var r3 = circle3.Radius;

                var a2 = 2.0f * (x1 - x2);
                var b2 = 2.0f * (y1 - y2);
                var c2 = 2.0f * (r2 - r1);
                var d2 = x1 * x1 + y1 * y1 - r1 * r1 - x2 * x2 - y2 * y2 + r2 * r2;

                var a3 = 2.0f * (x1 - x3);
                var b3 = 2.0f * (y1 - y3);
                var c3 = 2.0f * (r3 - r1);
                var d3 = x1 * x1 + y1 * y1 - r1 * r1 - x3 * x3 - y3 * y3 + r3 * r3;

                var ab = a3 * b2 - a2 * b3;

                var xa = (b2 * d3 - b3 * d2) / ab - x1;
                var xb = (b3 * c2 - b2 * c3) / ab;
                var ya = (a3 * d2 - a2 * d3) / ab - y1;
                var yb = (a2 * c3 - a3 * c2) / ab;

                var A = xb * xb + yb * yb - 1;
                var B = 2 * (xa * xb + ya * yb + r1);
                var C = xa * xa + ya * ya - r1 * r1;

                out_radius = (-B - Mathf.Sqrt(B * B - 4.0f * A * C)) / (2.0f * A);
                out_center = new Vector3(xa + xb * out_radius + x1, 0.0f, ya + yb * out_radius + y1);
            }

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

        /*
         * CIRCLE PACKING LAYOUT
         * ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
        */

        private GameObject Nodes;
        private GameObject Edges;

        public CirclePackingLayout(string widthMetric, string heightMetric, string breadthMetric, SerializableDictionary<string, IconFactory.Erosion> issueMap)
        : base(widthMetric, heightMetric, breadthMetric, issueMap)
        {
            name = "Circle Packing";
        }

        /*
         * NODES
         * ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
        */

        protected override void DrawNodes(Graph graph)
        {
            IScale scaler = new ZScoreScale(graph, minimal_length, new List<string>() { widthMetric, heightMetric, breadthMetric });
            Nodes = new GameObject();
            Nodes.name = "Nodes";
            InitializeNodes(graph.GetRoots(), scaler, Nodes);
            DrawNodes(graph.GetRoots(), scaler, out float out_radius);
        }

        private void DrawNodes(List<Node> nodes, IScale scaler, out float out_radius)
        {
            List<Circle> circles = new List<Circle>();
            for (int i = 0; i < nodes.Count; i++)
            {
                float radius = 0;
                
                if (nodes[i].IsLeaf())
                {
                    Transform cubeTransform = nodes[i].transform.GetChild(0);
                    Vector3 cubeScale = cubeTransform.lossyScale;
                    float cubeXZPlaneRadius = Mathf.Sqrt(cubeScale.x * cubeScale.x + cubeScale.z * cubeScale.z);
                    radius = cubeXZPlaneRadius;
                }
                else
                {
                    DrawNodes(nodes[i].Children(), scaler, out float out_radius_of_child);
                    radius = out_radius_of_child;
                }

                nodes[i].transform.position = new Vector3(
                    Mathf.Sin((float)i / (float)nodes.Count) * radius,
                    0.0f,
                    Mathf.Cos((float)i / (float)nodes.Count) * radius
                );

                DrawCircle(nodes[i], radius);
                circles.Add(new Circle(nodes[i].gameObject.transform, radius));
            }

            out_radius = 0;
            for (int i = 0; i < circles.Count; i++) // cp.Circles.Count as heuristic for decent result
            {
                CirclePacker.Pack(circles, out float out_outer_radius);
                out_radius = out_outer_radius;
            }
        }

        private void DrawCircle(Node node, float radius)
        {
            GameObject parent = node.gameObject;
            GameObject circle = new GameObject();
            circle.name = node.name + " border";
            circle.transform.parent = parent.transform;
            circle.transform.localPosition = Vector3.zero;

            const int segments = 360;
            LineRenderer line = circle.AddComponent<LineRenderer>();
            LineFactory.SetDefaults(line);
            LineFactory.SetColor(line, Color.red);
            LineFactory.SetWidth(line, radius / 100.0f);
            line.useWorldSpace = false;
            line.sharedMaterial = new Material(defaultLineMaterial);
            line.positionCount = segments + 1;
            const int pointCount = segments + 1;
            Vector3[] points = new Vector3[pointCount];
            for (int i = 0; i < pointCount; i++)
            {
                float rad = Mathf.Deg2Rad * (i * 360f / segments);
                points[i] = new Vector3(Mathf.Sin(rad) * radius, 0, Mathf.Cos(rad) * radius);
            }
            line.SetPositions(points);
        }

        private void InitializeNodes(List<Node> nodes, IScale scaler, GameObject parent)
        {
            foreach (Node node in nodes)
            {
                node.transform.parent = parent.transform;
                if (node.IsLeaf())
                {
                    GameObject cube = new GameObject();
                    cube.name = node.gameObject.name + " house";
                    MeshFactory.AddCube(cube);
                    cube.transform.parent = node.gameObject.transform;
                    cube.transform.localScale = new Vector3(
                        scaler.GetNormalizedValue(node, widthMetric),
                        scaler.GetNormalizedValue(node, heightMetric),
                        scaler.GetNormalizedValue(node, breadthMetric)
                    );
                }
                else
                {
                    InitializeNodes(node.Children(), scaler, node.gameObject);
                }
            }
        }

        /*
         * EDGES
         * ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
        */

        protected override void DrawEdges(Graph graph)
        {
            Edges = new GameObject();
            Edges.name = "Edges";
            foreach (Edge edge in graph.Edges())
            {
                edge.transform.parent = Edges.transform;
            }
        }

        public override void Reset()
        {
            if (Nodes != null)
            {
                Destroyer.DestroyGameObject(this.Nodes);
                Nodes = null;
            }
            if (Edges != null)
            {
                Destroyer.DestroyGameObject(this.Edges);
                Edges = null;
            }
        }
    }

}// namespace SEE.Layout