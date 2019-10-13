using SEE.DataModel;
using System.Collections.Generic;
using UnityEngine;

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
        
        private GameObject RootNodes;
        private GameObject RootEdges;

        private bool ShowErosions;
        private bool ShowDonuts;
        private readonly string[] InnerNodeMetrics;

        public CirclePackingLayout(bool showEdges,
                             string widthMetric, string heightMetric, string breadthMetric,
                             SerializableDictionary<string, IconFactory.Erosion> issueMap,
                             string[] innerNodeMetrics,
                             BlockFactory blockFactory,
                             IScale scaler,
                             float edgeWidth,
                             bool showErosions,
                             bool showDonuts)
        : base(showEdges, widthMetric, heightMetric, breadthMetric, issueMap, blockFactory, scaler, edgeWidth)
        {
            name = "Circle Packing";

            ShowErosions = showErosions;
            ShowDonuts = showDonuts;
            InnerNodeMetrics = innerNodeMetrics;
        }

        /*
         * NODES
         * ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
        */

        protected override void DrawNodes(Graph graph)
        {
            graph.SortHierarchyByName();
            RootNodes = new GameObject("Nodes");
            RootNodes.tag = Tags.Node;
            List<Node> roots = graph.GetRoots();
            DrawNodes(RootNodes, roots, out float out_radius);
        }

        private void DrawNodes(GameObject parent, List<Node> nodes, out float out_radius)
        {
            List<Circle> circles = new List<Circle>(nodes.Count);

            for (int i = 0; i < nodes.Count; i++)
            {
                Node node = nodes[i];

                GameObject gameObject = new GameObject(node.LinkName);
                gameObject.AddComponent<NodeRef>().node = node;
                gameObject.transform.parent = parent.transform;

                float radius;
                if (node.IsLeaf())
                {
                    DrawLeaf(gameObject, out float out_leaf_radius);
                    radius = out_leaf_radius;
                }
                else
                {
                    DrawNodes(gameObject, node.Children(), out float out_nodes_radius);
                    radius = out_nodes_radius;
                }

                float radians = ((float)i / (float)nodes.Count) * (2.0f * Mathf.PI);
                gameObject.transform.localPosition = new Vector3(Mathf.Cos(radians), 0.0f, Mathf.Sin(radians)) * radius;
                gameObject.transform.position = gameObject.transform.position + new Vector3(0.0f, 0.1f, 0.0f);
                circles.Add(new Circle(gameObject.transform, radius));
            }

            Vector3 position = parent.transform.position;
            parent.transform.position = position;

            CirclePacker.Pack(circles, out float out_outer_radius);
            if (circles.Count > 1)
                DrawOutline(parent, out_outer_radius);
            out_radius = out_outer_radius;
        }

        private void DrawLeaf(GameObject leaf, out float out_leaf_radius)
        {
            Node node = leaf.GetComponent<NodeRef>().node;

            GameObject block = blockFactory.NewBlock();
            block.name = leaf.name + " Block";
            block.transform.parent = leaf.transform;
            block.transform.localPosition = Vector3.zero;
            blockFactory.ScaleBlock(block, GetScale(node));

            Vector3 size = blockFactory.GetSize(block);
            out_leaf_radius = Mathf.Sqrt(size.x * size.x + size.z * size.z);
        }

        private void DrawOutline(GameObject parent, float radius)
        {
            if (ShowDonuts)
            {
                DrawDonut(parent, radius);
            }
            else
            {
                DrawCircle(parent, radius);
            }
        }

        private void DrawCircle(GameObject parent, float radius)
        {
            GameObject circle = new GameObject(parent.name + " Border");
            circle.tag = Tags.Node;
            circle.transform.parent = parent.transform;
            circle.transform.localPosition = Vector3.zero;

            const int segments = 360;
            LineRenderer line = circle.AddComponent<LineRenderer>();
            LineFactory.SetDefaults(line);
            LineFactory.SetColor(line, Color.white);
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

        private void DrawDonut(GameObject parent, float radius)
        {
            // TODO: move to ILayout (change in other layouts, too)

            // FIXME: Derive real metrics from nodes.

            GameObject donut = new GameObject(parent.name + " Donut");
            donut.tag = Tags.Node;
            donut.transform.parent = parent.transform;

            float innerValue = UnityEngine.Random.Range(0.0f, 1.0f);
            float m1 = UnityEngine.Random.Range(0.0f, 50.0f);
            float m2 = UnityEngine.Random.Range(0.0f, 90.0f);
            float m3 = UnityEngine.Random.Range(0.0f, 150.0f);
            float m4 = UnityEngine.Random.Range(0.0f, 200.0f);
            new DonutFactory(InnerNodeMetrics).DonutChart(donut, radius, innerValue, new float[] { m1, m2, m2, m3 }, 0.95f);
        }

        private Vector3 GetScale(Node node)
        {
            return new Vector3(scaler.GetNormalizedValue(node, widthMetric), scaler.GetNormalizedValue(node, heightMetric), scaler.GetNormalizedValue(node, breadthMetric)); ;
        }

        /*
         * EDGES
         * ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
        */

        // TODO: it's very empty here

    }

}// namespace SEE.Layout