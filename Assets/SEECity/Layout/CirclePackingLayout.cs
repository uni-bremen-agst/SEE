using SEE.DataModel;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.Layout
{

    /// <summary>
    /// This layout packs circles closely together to decrease total area of city.
    /// </summary>
    public class CirclePackingLayout : INodeLayout
    {
        private readonly string[] InnerNodeMetrics;

        public CirclePackingLayout(string widthMetric, string heightMetric, string breadthMetric,
                             SerializableDictionary<string, IconFactory.Erosion> issueMap,
                             string[] innerNodeMetrics,
                             BlockFactory blockFactory,
                             IScale scaler,
                             bool showErosions,
                             bool showDonuts)
        : base(widthMetric, heightMetric, breadthMetric, issueMap, blockFactory, scaler, showErosions)
        {
            name = "Circle Packing";
            ShowDonuts = showDonuts;
            InnerNodeMetrics = innerNodeMetrics;
        }

        private readonly bool ShowDonuts;

        public override void Draw(Graph graph)
        {
            GameObject artificialRootNode = new GameObject("Nodes");
            artificialRootNode.tag = Tags.Node;
            List<Node> roots = graph.GetRoots();
            DrawNodes(artificialRootNode, roots, out float out_radius);
            DrawPlane(artificialRootNode, out_radius);
        }

        private void DrawNodes(GameObject parent, List<Node> nodes, out float out_radius)
        {
            List<Circle> circles = new List<Circle>(nodes.Count);

            for (int i = 0; i < nodes.Count; i++)
            {
                Node node = nodes[i];

                GameObject gameObject;

                float radius;
                if (node.IsLeaf())
                {
                    gameObject = DrawLeaf(node, out float out_leaf_radius);
                    radius = out_leaf_radius;
                }
                else
                {
                    gameObject = new GameObject(node.LinkName);      
                    DrawNodes(gameObject, node.Children(), out float out_nodes_radius);
                    radius = out_nodes_radius;
                }
                gameObject.tag = Tags.Node;
                gameObject.AddComponent<NodeRef>().node = node;
                gameObject.transform.parent = parent.transform;

                float radians = ((float)i / (float)nodes.Count) * (2.0f * Mathf.PI);
                blockFactory.SetLocalGroundPosition(gameObject, 
                                                    new Vector3(Mathf.Cos(radians), 0.0f, Mathf.Sin(radians)) * radius
                                                    + new Vector3(0.0f, 0.1f, 0.0f));
                //gameObject.transform.localPosition = new Vector3(Mathf.Cos(radians), 0.0f, Mathf.Sin(radians)) * radius;
                circles.Add(new Circle(gameObject.transform, radius));
            }

            Vector3 position = parent.transform.position;
            parent.transform.position = position;

            CirclePacker.Pack(circles, out float out_outer_radius);
            if (circles.Count > 1)
            {
                DrawOutline(parent, ref out_outer_radius);
            }
            out_radius = out_outer_radius;
        }

        private GameObject DrawLeaf(Node node, out float out_leaf_radius)
        {
            GameObject block = blockFactory.NewBlock();
            gameNodes[node] = block;
            
            block.name = node.LinkName + " Block";
            blockFactory.ScaleBlock(block, GetScale(node));
            Vector3 size = blockFactory.GetSize(block);
            out_leaf_radius = Mathf.Sqrt(size.x * size.x + size.z * size.z);

            if (showErosions)
            {
                AddErosionIssues(node);
            }

            return block;
        }

        private void DrawOutline(GameObject parent, ref float radius)
        {
            if (ShowDonuts)
            {
                AddDonut(parent, ref radius);
            }
            else
            {
                AttachCircleLine(parent, ref radius);
            }
        }

        // FIXME: Unify with BallonLayout.AttachCircleLine
        private void AttachCircleLine(GameObject parent, ref float radius)
        {
            GameObject circle = new GameObject(parent.name + " Border");
            circle.tag = Tags.Node;
            circle.transform.parent = parent.transform;

            // Number of line segments constituting the circle
            const int segments = 360;

            LineRenderer line = circle.AddComponent<LineRenderer>();

            LineFactory.SetDefaults(line);
            LineFactory.SetColor(line, Color.white);

            // line width is relative to the radius
            float lineWidth = radius / 100.0f;
            LineFactory.SetWidth(line, lineWidth);

            // We want to set the points of the circle lines relative to the game object.
            line.useWorldSpace = false;

            // FIXME: We do not want to create a new material. The fewer materials, the lesser
            // drawing calls at run-time.
            line.sharedMaterial = new Material(LineFactory.DefaultLineMaterial);

            line.positionCount = segments + 1;
            const int pointCount = segments + 1; // add extra point to make startpoint and endpoint the same to close the circle
            Vector3[] points = new Vector3[pointCount];

            for (int i = 0; i < pointCount; i++)
            {
                float rad = Mathf.Deg2Rad * (i * 360f / segments);
                points[i] = new Vector3(Mathf.Sin(rad) * radius, 0, Mathf.Cos(rad) * radius);
            }
            line.SetPositions(points);
        }

        // FIXME: Unify with BallonLayout.AddDonut
        private void AddDonut(GameObject parent, ref float radius)
        {
            GameObject donut = new GameObject(parent.name + " Donut");
            donut.tag = Tags.Node;
            donut.transform.parent = parent.transform;

            float innerValue = UnityEngine.Random.Range(0.0f, 1.0f);
            float m1 = UnityEngine.Random.Range(0.0f, 50.0f);
            float m2 = UnityEngine.Random.Range(0.0f, 90.0f);
            float m3 = UnityEngine.Random.Range(0.0f, 150.0f);
            float m4 = UnityEngine.Random.Range(0.0f, 200.0f);

            const float innerScale = 0.95f;
            radius += (1.0f - innerScale) * radius;
            new DonutFactory(InnerNodeMetrics).DonutChart(donut, radius, innerValue, new float[] { m1, m2, m2, m3 }, innerScale);
        }

        private Vector3 GetScale(Node node)
        {
            return new Vector3(scaler.GetNormalizedValue(node, widthMetric), 
                               scaler.GetNormalizedValue(node, heightMetric), 
                               scaler.GetNormalizedValue(node, breadthMetric)); ;
        }

        private void DrawPlane(GameObject parent, float maxRadius)
        {
            const float enlargementFactor = 1.12f;

            // We put the circle into a square somewhat larger than what is necessary
            float widthAndDepth = 2.0f * maxRadius * enlargementFactor;

            GameObject plane = PlaneFactory.NewPlane(parent.transform.position, Color.gray, widthAndDepth, widthAndDepth);
            plane.transform.parent = parent.transform;
        }
    }

}// namespace SEE.Layout