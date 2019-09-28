using System.Collections.Generic;
using UnityEngine;
using SEE.DataModel;
using System;

namespace SEE.Layout
{

    public class CirclePackingLayout : ILayout
    {
        private const float minimal_radius = 0.5f;
        private static readonly float maximal_length = (float)Math.Sqrt(2.0) * minimal_radius;

        private struct Circle
        {
            public Transform Transform { get; set; }
            public float Radius { get; }

            public Circle(Transform transform, float radius)
            {
                this.Transform = transform;
                this.Radius = radius;
            }
        }

        /// <summary>
        /// <see href="https://www.codeproject.com/Articles/42067/D-Circle-Packing-Algorithm-Ported-to-Csharp">Source</see>
        /// </summary>
        private class CirclePacker
        {
            public List<Circle> Circles { get; set; }
            public float MinSeparation { get; set; }

            public CirclePacker(List<Circle> circles = null)
            {
                this.Circles = circles == null ? new List<Circle>() : circles;
                this.MinSeparation = 1.0f;
            }

            public void Pack(out float out_outer_radius)
            {
                Circles.Sort(Comparator);

                float minSeparationSq = MinSeparation * MinSeparation;
                for (int i = 0; i < Circles.Count - 1; i++)
                {
                    for (int j = i + 1; j < Circles.Count; j++)
                    {
                        if (i == j)
                            continue;

                        Vector3 ab = Circles[j].Transform.localPosition - Circles[i].Transform.localPosition;
                        float r = Circles[i].Radius + Circles[j].Radius;

                        float d = Vector3.SqrMagnitude(ab) - minSeparationSq;
                        float minSepSq = Math.Min(d, minSeparationSq);
                        d -= minSepSq;

                        if (d < (r * r) - 0.01)
                        {
                            ab.Normalize();
                            ab *= (float)((r - Math.Sqrt(d)) * 0.5f);
                            Circles[j].Transform.localPosition += ab;
                            Circles[i].Transform.localPosition -= ab;
                        }
                    }
                }

                long iterationCounter = 1L;

                float damping = 0.1f / (float)(iterationCounter);
                for (int i = 0; i < Circles.Count; i++)
                {
                    Vector3 v = Circles[i].Transform.localPosition * damping;
                    Circles[i].Transform.localPosition -= v;
                }

                out_outer_radius = 0.0f;
                foreach (Circle c in Circles)
                {
                    out_outer_radius = Mathf.Max(out_outer_radius, c.Transform.localPosition.magnitude + c.Radius);
                }
            }

            private int Comparator(Circle p1, Circle p2)
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
            CirclePacker cp = new CirclePacker();
            for (int i = 0; i < nodes.Count; i++)
            {
                nodes[i].transform.position = new Vector3(
                    Mathf.Sin((float)i / (float)nodes.Count),
                    0.0f,
                    Mathf.Cos((float)i / (float)nodes.Count)
                );

                float radius = 0;

                if (nodes[i].IsLeaf())
                {
                    Vector3 scale = nodes[i].transform.GetChild(0).lossyScale;
                    radius = scale.magnitude;
                }
                else
                {
                    DrawNodes(nodes[i].Children(), scaler, out float out_radius_of_child);
                    radius = out_radius_of_child;
                }
                Circle c = new Circle(nodes[i].transform, radius);
                cp.Circles.Add(c);
            }
            cp.Pack(out float out_outer_radius);

            foreach (Node node in nodes)
            {
                DrawCircle(node, node.transform.GetChild(0).lossyScale.magnitude);
            }

            out_radius = out_outer_radius;
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
            LineFactory.SetWidth(line, 0.1f);
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
                GameObject cube = new GameObject();
                cube.name = node.gameObject.name + " house";
                MeshFactory.AddCube(cube);
                cube.transform.parent = node.gameObject.transform;
                cube.transform.localScale = new Vector3(
                    scaler.GetNormalizedValue(node, widthMetric),
                    scaler.GetNormalizedValue(node, heightMetric),
                    scaler.GetNormalizedValue(node, breadthMetric)
                );
                InitializeNodes(node.Children(), scaler, node.gameObject);
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