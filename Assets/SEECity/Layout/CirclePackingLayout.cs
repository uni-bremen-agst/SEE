using System.Collections.Generic;
using UnityEngine;
using SEE.DataModel;

namespace SEE.Layout
{

    /// <summary>
    /// This layout packs circles closely together to decrease total area of city.
    /// </summary>
    public class CirclePackingLayout : ILayout
    {
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
            for (int i = 0; i < nodes.Count; i++)
            {
                Node node = nodes[i];
                node.transform.parent = parent.transform;
                if (node.IsLeaf())
                {
                    GameObject cube = new GameObject();
                    cube.transform.position = new Vector3(0.0f, scaler.GetNormalizedValue(node, heightMetric) / 2.0f, 0.0f);
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
            List<Edge> edges = graph.Edges();
            for (int i = 0; i < edges.Count; i++)
            {
                edges[i].transform.parent = Edges.transform;
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