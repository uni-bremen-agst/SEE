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

                //EnclosingCircleIntersectingCircles(circles, out Vector3 center, out float radius);
                Vector3 center = CenterOfMass(circles);
                
                out_outer_radius = 0.0f;
                foreach (Circle c in circles)
                {
                    c.Transform.localPosition -= center;
                    out_outer_radius = Mathf.Max(out_outer_radius, c.Transform.localPosition.magnitude + c.Radius);
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
                EnclosingCircleIntersectingCircles(circles, new List<Circle>(), out Vector3 center, out float radius);
                out_center = center;
                out_radius = radius;
            }

            private static void EnclosingCircleIntersectingCircles(List<Circle> circles, List<Circle> borderCircles, out Vector3 out_center, out float out_radius)
            {
                out_center = Vector3.zero;
                out_radius = -1.0f;

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
                            CircleIntersectingTwoCircles(borderCircles[0], borderCircles[1], out Vector3 center, out float radius);
                            out_center = center;
                            out_radius = radius;
                            break;
                        }
                    case 3:
                        {
                            CircleIntersectingThreeCircles(borderCircles[0], borderCircles[1], borderCircles[2], out Vector3 center, out float radius);
                            out_center = center;
                            out_radius = radius;
                            break;
                        }
                }

                for (int i = 0; i < circles.Count; i++)
                {
                    if (out_radius == -1.0f || !CircleContainsCircle(circles[i], out_center, out_radius))
                    {
                        borderCircles.Add(circles[i]);
                        EnclosingCircleIntersectingCircles(circles, borderCircles, out Vector3 center, out float radius);
                        //borderCircles.Remove(circles[i]);
                    }
                }
            }

            private static bool CircleContainsCircle(Circle c1, Vector3 position, float radius)
            {
                return (c1.Transform.position - position).magnitude < c1.Radius - radius;
            }

            private static void CircleIntersectingTwoCircles(Circle c1, Circle c2, out Vector3 out_center, out float out_radius)
            {
                Vector3 c12 = c2.Transform.position - c1.Transform.position;
                float r12 = c2.Radius - c1.Radius;
                float l = c12.magnitude;
                out_center = (c1.Transform.position + c2.Transform.position + c12 / l * r12) / 2.0f;
                out_radius = (l + c1.Radius + c2.Radius) / 2.0f;
            }

            private static void CircleIntersectingThreeCircles(Circle c1, Circle c2, Circle c3, out Vector3 out_center, out float out_radius)
            {
                Vector3 a12 = 2.0f * (c1.Transform.position - c2.Transform.position);
                float b12 = 2.0f * (c2.Radius - c1.Radius);
                float c12 = c1.Transform.position.sqrMagnitude - c1.Radius * c1.Radius - c2.Transform.position.sqrMagnitude - c2.Radius * c2.Radius;

                Vector3 a13 = 2.0f * (c1.Transform.position - c3.Transform.position);
                float b13 = 2.0f * (c3.Radius - c1.Radius);
                float c13 = c1.Transform.position.sqrMagnitude - c1.Radius * c1.Radius - c3.Transform.position.sqrMagnitude - c3.Radius * c3.Radius;

                float ab = a13.x * a12.y - a12.x * a13.y;

                float xa = (a12.y * c13 - a13.y * c12) / ab - c1.Transform.position.x;
                float xb = (a13.y * b12 - a12.y * b13) / ab;
                float ya = (a13.x * c12 - a12.x * c13) / ab - c1.Transform.position.y;
                float yb = (a12.x * b13 - a13.x * b12) / ab;

                float A = xb * xb + yb * yb - 1.0f;
                float B = 2.0f * (xa * xb + ya * yb + c1.Radius);
                float C = xa * xa + ya * ya - c1.Radius * c1.Radius;

                float r = (-B - (float)Math.Sqrt(B * B - 4.0f * A * C)) / (2.0f * A);

                out_center = new Vector3(
                    xa + xb * r + c1.Transform.position.x,
                    0.0f,
                    ya + yb * r + c1.Transform.position.y
                );
                out_radius = r;
            }

            private static Vector3 CenterOfMass(List<Circle> circles)
            {
                Vector3 center = Vector3.zero;
                float totalMass = 0.0f;
                circles.ForEach(delegate (Circle c) { float mass = Mathf.PI * c.Radius * c.Radius; center += c.Transform.position * mass; totalMass += mass; });
                return center / (circles.Count * totalMass);
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