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
        }

        /*
         * NODES
         * ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
        */

        protected override void DrawNodes(Graph graph)
        {
            graph.SortHierarchyByName();
            RootNodes = new GameObject();
            RootNodes.name = "Nodes";
            RootNodes.tag = Tags.Node;
            DrawNodes(RootNodes, graph.GetRoots(), out float out_radius);
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
                    Vector3 scale = GetScale(node);
                    DrawLeaf(gameObject, out float out_leaf_radius);
                    radius = out_leaf_radius;
                }
                else
                {
                    DrawNodes(gameObject, node.Children(), out float out_child_radius);
                    radius = out_child_radius;
                }

                float radians = ((float)i / (float)nodes.Count) * (2.0f * Mathf.PI);
                gameObject.transform.position = new Vector3(Mathf.Cos(radians), 0.0f, Mathf.Sin(radians)) * radius;
                circles.Add(new Circle(gameObject.transform, radius));
            }

            CirclePacker.Pack(circles, out float out_outer_radius);
            DrawCircle(parent, out_outer_radius);
            out_radius = out_outer_radius;
        }

        private void DrawLeaf(GameObject leaf, out float out_leaf_radius)
        {
            Node node = leaf.GetComponent<NodeRef>().node;

            GameObject block = blockFactory.NewBlock();
            blockFactory.ScaleBlock(block, GetScale(node));
            block.transform.parent = leaf.transform;

            Vector3 size = blockFactory.GetSize(block);
            out_leaf_radius = Mathf.Sqrt(size.x * size.x + size.z * size.z);
        }

        private void DrawCircle(GameObject node, float radius)
        {
            GameObject circle = new GameObject();
            circle.name = node.name + " border";
            circle.tag = Tags.Node;
            circle.transform.parent = node.transform;

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