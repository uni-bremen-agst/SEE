using SEE.DataModel;
using System;
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

        private Dictionary<Node, GameObject> GameObjects = new Dictionary<Node, GameObject>();

        private GameObject RootNodes;
        private GameObject RootEdges;

        private bool ShowErosions;
        private bool ShowDonuts;
        private readonly string[] InnerNodeMetrics;

        public static Vector3 LevelUnit;

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
            LevelUnit = Vector3.zero;
            graph.SortHierarchyByName();
            RootNodes = new GameObject("Nodes");
            RootNodes.tag = Tags.Node;
            List<Node> roots = graph.GetRoots();
            DrawNodes(RootNodes, roots, out float out_radius);
            DrawPlane(RootNodes, out_radius);
        }

        private void DrawNodes(GameObject parent, List<Node> nodes, out float out_radius)
        {
            List<Circle> circles = new List<Circle>(nodes.Count);

            for (int i = 0; i < nodes.Count; i++)
            {
                Node node = nodes[i];

                GameObject gameObject = new GameObject(node.LinkName);
                gameObject.tag = Tags.Node;
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
            {
                DrawOutline(parent, ref out_outer_radius);
            }
            out_radius = out_outer_radius;
        }

        private void DrawLeaf(GameObject leaf, out float out_leaf_radius)
        {
            Node node = leaf.GetComponent<NodeRef>().node;

            GameObject block = blockFactory.NewBlock();
            block.name = leaf.name + " Block";
            block.transform.parent = leaf.transform;
            blockFactory.ScaleBlock(block, GetScale(node));
            Vector3 size = blockFactory.GetSize(block);
            blockFactory.SetLocalPosition(block, new Vector3(0.0f, size.y / 2.0f, 0.0f));
            leaf.AddComponent<BlockRef>().Block = block;
            out_leaf_radius = Mathf.Sqrt(size.x * size.x + size.z * size.z);

            if (ShowErosions)
            {
                AddErosionIssues(leaf);
            }

            GameObjects[node] = leaf;
            LevelUnit.y = Mathf.Max(LevelUnit.y, size.y);
        }

        private void DrawOutline(GameObject parent, ref float radius)
        {
            if (ShowDonuts)
            {
                DrawDonut(parent, ref radius);
            }
            else
            {
                DrawCircle(parent, ref radius);
            }
        }

        private void DrawCircle(GameObject parent, ref float radius)
        {
            GameObject circle = new GameObject(parent.name + " Border");
            circle.tag = Tags.Node;
            circle.transform.parent = parent.transform;

            const int segments = 360;
            LineRenderer line = circle.AddComponent<LineRenderer>();
            LineFactory.SetDefaults(line);
            LineFactory.SetColor(line, Color.white);
            float lineWidth = radius / 100.0f;
            LineFactory.SetWidth(line, lineWidth);
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

        private void DrawDonut(GameObject parent, ref float radius)
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
            return new Vector3(scaler.GetNormalizedValue(node, widthMetric), scaler.GetNormalizedValue(node, heightMetric), scaler.GetNormalizedValue(node, breadthMetric)); ;
        }

        private void AddErosionIssues(GameObject node)
        {
            List<GameObject> sprites = new List<GameObject>();
            
            foreach (KeyValuePair<string, IconFactory.Erosion> issue in issueMap)
            {
                Node n = node.GetComponent<NodeRef>().node;
                if (n.TryGetNumeric(issue.Key, out float value))
                {
                    if (value > 0.0f)
                    {
                        GameObject sprite = IconFactory.Instance.GetIcon(Vector3.zero, issue.Value);
                        sprite.transform.parent = node.transform;
                        Vector3 spriteSize = GetSizeOfSprite(sprite);
                        float spriteScale = 1.0f / spriteSize.x;
                        float metricScale = scaler.GetNormalizedValue(n, issue.Key);
                        sprite.transform.localScale *= spriteScale * blockFactory.Unit();
                        sprite.transform.localScale *= metricScale;
                        sprite.name = sprite.name + " " + n.SourceName;
                        sprites.Add(sprite);
                    }
                }
            }
            
            Vector3 delta = Vector3.up / 100.0f;
            Vector3 currentRoof = blockFactory.Roof(node.GetComponent<BlockRef>().Block);
            sprites.Sort(Comparer<GameObject>.Create((left, right) => GetSizeOfSprite(left).x.CompareTo(GetSizeOfSprite(right).x)));
            for (int i = 0; i < sprites.Count; i++)
            {
                GameObject sprite = sprites[i];
                Vector3 size = GetSizeOfSprite(sprite);
                Vector3 halfHeight = (size.y / 2.0f) * Vector3.up;
                sprite.transform.position = currentRoof + delta + halfHeight;
                currentRoof = sprite.transform.position + halfHeight;
            }
        }

        private Vector3 GetSizeOfSprite(GameObject node)
        {
            return node.GetComponentInChildren<Renderer>().bounds.size;
        }

        private void DrawPlane(GameObject parent, float maxRadius)
        {
            const float enlargementFactor = 1.12f;

            GameObject plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            plane.name = "Plane";
            plane.tag = Tags.Decoration;
            plane.transform.parent = parent.transform;
            plane.transform.localScale = new Vector3(maxRadius * 0.2f, 1.0f / enlargementFactor, maxRadius * 0.2f) * enlargementFactor;

            Renderer planeRenderer = plane.GetComponent<Renderer>();
            planeRenderer.sharedMaterial.color = Color.gray;
            planeRenderer.sharedMaterial.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
            planeRenderer.sharedMaterial.EnableKeyword("_GLOSSYREFLECTIONS_OFF");
            planeRenderer.sharedMaterial.SetFloat("_SpecularHighlights", 1.0f);
        }

        /*
         * EDGES
         * ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
        */

        protected override void DrawEdges(Graph graph)
        {
            RootEdges = new GameObject("Edges");
            RootEdges.tag = Tags.Edge;

            List<Edge> edges = graph.Edges();

            Material edgeMaterial = new Material(defaultLineMaterial);
            if (edgeMaterial == null)
            {
                Debug.LogError("Could not find material " + materialPath + "\n");
                return;
            }

            for (int i = 0; i < edges.Count; i++)
            {
                Edge edge = edges[i];
                Node source = edge.Source;
                Node target = edge.Target;
                Vector3 sourcePosition = GameObjects[source].transform.position;
                Vector3 targetPosition = GameObjects[target].transform.position;

                GameObject gameObject = new GameObject(edge.Type + "(" + source.LinkName + ", " + target.LinkName + ")");
                gameObject.tag = Tags.Edge;
                gameObject.AddComponent<EdgeRef>().edge = edge;
                gameObject.transform.parent = RootEdges.transform;

                Vector3[] controlPoints = new Vector3[] {
                    sourcePosition,
                    Vector3.Lerp(sourcePosition, targetPosition, 0.3f) + LevelUnit,
                    Vector3.Lerp(sourcePosition, targetPosition, 0.7f) + LevelUnit,
                    targetPosition
                };
                BSplineFactory.Draw(gameObject, controlPoints, edgeWidth, edgeMaterial);
            }
        }

    }

}// namespace SEE.Layout