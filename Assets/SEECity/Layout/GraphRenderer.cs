using System;
using System.Collections.Generic;
using System.Linq;
using SEE.DataModel;
using UnityEngine;

namespace SEE.Layout
{
    /// <summary>
    /// A renderer for graphs. Encapsulates handling of block types, node and edge layouts,
    /// decorations and other visual attributes.
    /// </summary>
    public class GraphRenderer
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="graph">the graph to be drawn</param>
        /// <param name="settings">the settings for the visualization</param>
        public GraphRenderer(GraphSettings settings)
        {
            this.settings = settings;
            if (this.settings.CScapeBuildings)
            {
                blockFactory = new BuildingFactory();
            }
            else
            {
                blockFactory = new CubeFactory();
            }
        }

        /// <summary>
        /// Settings for the visualization.
        /// </summary>
        private readonly GraphSettings settings;

        /// <summary>
        /// The factory used to create blocks.
        /// </summary>
        private readonly BlockFactory blockFactory;

        /// <summary>
        /// The scale used to normalize the metrics determining the lengths of the blocks.
        /// </summary>
        private IScale scaler;

        /// <summary>
        /// Draws the graph.
        /// </summary>
        public void Draw(Graph graph)
        {
            SetScaler(graph);
            graph.SortHierarchyByName();

            if (settings.NodeLayout == GraphSettings.NodeLayouts.Manhattan
                || settings.NodeLayout == GraphSettings.NodeLayouts.Treemap
                || settings.NodeLayout == GraphSettings.NodeLayouts.BallonNode)
            {
                DrawCity(graph);
            }
            else
            {
                Dictionary<Node, GameObject> gameNodes = NodeLayout(graph, scaler);
                if (settings.ShowEdges)
                {
                    EdgeLayout(graph, gameNodes);
                }
            }
        }

        private void SetScaler(Graph graph)
        {
            List<string> nodeMetrics = new List<string>() { settings.WidthMetric, settings.HeightMetric, settings.DepthMetric };
            nodeMetrics.AddRange(settings.IssueMap().Keys);
            if (settings.ZScoreScale)
            {
                scaler = new ZScoreScale(graph, settings.MinimalBlockLength, settings.MaximalBlockLength, nodeMetrics);
            }
            else
            {
                scaler = new LinearScale(graph, settings.MinimalBlockLength, settings.MaximalBlockLength, nodeMetrics);
            }
        }

        private void EdgeLayout(Graph graph, Dictionary<Node, GameObject> gameNodes)
        {
            IEdgeLayout layout;
            switch (settings.EdgeLayout)
            {
                case GraphSettings.EdgeLayouts.Straight:
                    layout = new StraightEdgeLayout(blockFactory, settings.EdgeWidth, settings.EdgesAboveBlocks);
                    break;
                case GraphSettings.EdgeLayouts.Spline:
                    layout = new SplineEdgeLayout(blockFactory, settings.EdgeWidth, settings.EdgesAboveBlocks);
                    break;
                case GraphSettings.EdgeLayouts.Bundling:
                    layout = new BundledEdgeLayout(blockFactory, settings.EdgeWidth, settings.EdgesAboveBlocks);
                    break;
                default:
                    throw new Exception("Unhandled edge layout " + settings.EdgeLayout.ToString());
            }
            Performance p = Performance.Begin(layout.Name + " layout of edges");
            layout.DrawEdges(graph, gameNodes.Values.ToList());
            p.End();
        }

        private Dictionary<Node, GameObject> NodeLayout(Graph graph, IScale scaler)
        {
            INodeLayout layout;
            switch (settings.NodeLayout)
            {
                case GraphSettings.NodeLayouts.Balloon:
                    {
                        layout = new BalloonLayout(settings.WidthMetric, settings.HeightMetric, settings.DepthMetric,
                                                   settings.IssueMap(),
                                                   settings.InnerNodeMetrics,
                                                   blockFactory,
                                                   scaler,
                                                   settings.ShowErosions,
                                                   settings.ShowDonuts);
                        break;
                    }
                case GraphSettings.NodeLayouts.CirclePacking:
                    {
                        layout = new CirclePackingLayout(settings.WidthMetric, settings.HeightMetric, settings.DepthMetric,
                                                         settings.IssueMap(),
                                                         settings.InnerNodeMetrics,
                                                         blockFactory,
                                                         scaler,
                                                         settings.ShowErosions,
                                                         settings.ShowDonuts);
                        break;
                    }
                default:
                    throw new Exception("Unhandled node layout " + settings.NodeLayout.ToString());
            }
            Performance p = Performance.Begin(layout.Name + " layout of nodes");
            layout.Draw(graph);
            p.End();
            return layout.Nodes();
        }

        /// <summary>
        /// The y co-ordinate of the ground where blocks are placed.
        /// </summary>
        protected const float groundLevel = 0.0f;

        protected void DrawCity(Graph graph)
        {
            Dictionary<Node, GameObject> nodeMap;
            Dictionary<GameObject, NodeTransform> layout;
            List<Node> nodes = graph.Nodes();
            switch (settings.NodeLayout)
            {
                case GraphSettings.NodeLayouts.Manhattan:
                    nodeMap = CreateBlocks(nodes); // only leaves
                    layout = new ManhattenLayout(groundLevel, blockFactory).Layout(nodeMap.Values);
                    break;
                case GraphSettings.NodeLayouts.Treemap:
                    nodeMap = CreateBlocks(nodes); // only leaves
                    layout = new TreemapLayout(groundLevel, blockFactory, 100.0f, 100.0f).Layout(nodeMap.Values);
                    break;
                case GraphSettings.NodeLayouts.BallonNode:
                    nodeMap = CreateBlocks(nodes); // leaves
                    AddContainers(nodeMap, nodes); // and inner nodes
                    layout = new BalloonNodeLayout(groundLevel, blockFactory).Layout(nodeMap.Values);
                    break;
                default:
                    throw new Exception("Unhandled node layout " + settings.NodeLayout.ToString());
            }
            
            Apply(layout);
            // Decorations must be applied after the blocks have been placed, so that
            // we also know their positions.
            if (settings.ShowErosions)
            {
                AddErosionIssues(nodeMap.Values);
            }
            BoundingBox(nodeMap.Values, out Vector2 leftFrontCorner, out Vector2 rightBackCorner);
            PlaneFactory.NewPlane(leftFrontCorner, rightBackCorner, groundLevel - 0.01f, Color.gray);
        }

        protected void AddErosionIssues(ICollection<GameObject> gameNodes)
        {
            foreach (GameObject block in gameNodes)
            {
                AddErosionIssues(block);
            }
        }

        public void Apply(Dictionary<GameObject, NodeTransform> layout)
        {
            foreach (var entry in layout)
            {
                GameObject block = entry.Key;
                NodeTransform transform = entry.Value;
                Node node = block.GetComponent<NodeRef>().node;

                if (node.IsLeaf())
                {
                    // Leave nodes were created as blocks by blockFactory.
                    // Note: We need to first scale a block and only then set its position
                    // because the scaling behavior differs between Cubes and CScape buildings.
                    // Cubes scale from its center up and downward, which CScape buildings
                    // scale only up.
                    blockFactory.ScaleBlock(block, transform.scale);
                    blockFactory.SetGroundPosition(block, transform.position);
                }
                else
                {
                    // Inner nodes were not created by blockFactory.
                    block.transform.position = transform.position;
                    block.transform.localScale = transform.scale;
                }
            }
        }

        /// <summary>
        /// Returns the unit of the world helpful for scaling. This unit depends upon the
        /// kind of blocks we are using to represent nodes.
        /// </summary>
        /// <returns>unit of the world</returns>
        public float Unit()
        {
            return blockFactory.Unit();
        }

        /// <summary>
        /// Adds a NodeRef component to given block referencing to given node.
        /// </summary>
        /// <param name="block"></param>
        /// <param name="node"></param>
        protected void AttachNode(GameObject block, Node node)
        {
            NodeRef nodeRef = block.AddComponent<NodeRef>();
            nodeRef.node = node;
        }

        /// <summary>
        /// Creates and scales blocks for all leaf nodes in given list of nodes.
        /// </summary>
        /// <param name="nodes">list of nodes for which to create blocks</param>
        /// <returns>blocks for all leaf nodes in given list of nodes</returns>
        private Dictionary<Node, GameObject> CreateBlocks(IList<Node> nodes)
        {
            Dictionary<Node, GameObject> result = new Dictionary<Node, GameObject>();

            foreach (Node node in nodes)
            {
                // We add only leaves.
                if (node.IsLeaf())
                {
                    GameObject block = blockFactory.NewBlock();
                    block.name = node.LinkName;

                    AttachNode(block, node);
                    // Scaled metric values for the dimensions.
                    Vector3 scale = new Vector3(scaler.GetNormalizedValue(node, settings.WidthMetric),
                                                scaler.GetNormalizedValue(node, settings.HeightMetric),
                                                scaler.GetNormalizedValue(node, settings.DepthMetric));

                    // Scale according to the metrics.
                    blockFactory.ScaleBlock(block, scale);

                    result[node] = block;
                }
            }
            return result;
        }

        /// <summary>
        /// Adds game objects for all inner nodes in given list of nodes to nodeMap.
        /// Note: added game objectsfor inner nodes are not scaled.
        /// </summary>
        /// <param name="nodeMap">nodeMap to which the game objects are to be added</param>
        /// <param name="nodes">list of nodes for which to create blocks</param>
        private void AddContainers(Dictionary<Node, GameObject> nodeMap, IList<Node> nodes)
        {
            foreach (Node node in nodes)
            {
                // We add only inner nodes.
                if (! node.IsLeaf())
                {
                    GameObject innerGameObject = new GameObject
                    {
                        name = node.LinkName,
                        tag = Tags.Node
                    };

                    AttachNode(innerGameObject, node);
                    AttachCircleLine(innerGameObject, 1.0f, 0.1f * blockFactory.Unit());
                    nodeMap[node] = innerGameObject;
                }
            }
        }

        private static void AttachCircleLine(GameObject circle, float radius, float lineWidth)
        {
            // Number of line segments constituting the circle
            const int segments = 360;

            LineRenderer line = circle.AddComponent<LineRenderer>();

            LineFactory.SetDefaults(line);
            LineFactory.SetColor(line, Color.white);
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

        /// <summary>
        /// Stacks sprites for software-erosion issues atop of the roof of the given node
        /// in ascending order in terms of the sprite width. The sprite width is proportional
        /// to the normalized metric value for the erosion issue.
        /// </summary>
        /// <param name="node"></param>
        protected void AddErosionIssues(GameObject gameNode)
        {
            Node node = gameNode.GetComponent<NodeRef>().node;

            // The list of sprites for the erosion issues.
            List<GameObject> sprites = new List<GameObject>();

            // Create and scale the sprites and add them to the list of sprites.
            foreach (KeyValuePair<string, IconFactory.Erosion> issue in settings.IssueMap())
            {
                if (node.TryGetNumeric(issue.Key, out float value))
                {
                    if (value > 0.0f)
                    {
                        GameObject sprite = IconFactory.Instance.GetIcon(Vector3.zero, issue.Value);
                        sprite.name = sprite.name + " " + node.SourceName;

                        Vector3 spriteSize = GetSizeOfSprite(sprite);
                        // Scale the sprite to one Unity unit.
                        float spriteScale = 1.0f / spriteSize.x;
                        // Scale the erosion issue by normalization.
                        float metricScale = scaler.GetNormalizedValue(node, issue.Key);
                        // First: scale its width to unit size 1.0 maintaining the aspect ratio
                        sprite.transform.localScale *= spriteScale * blockFactory.Unit();
                        // Now scale it by the normalized metric.
                        sprite.transform.localScale *= metricScale;
                        sprite.transform.position = blockFactory.Roof(gameNode);
                        sprites.Add(sprite);
                    }
                }
            }

            // Now we stack the sprites on top of the roof of the building in
            // ascending order of their widths.
            {
                // The space that we put in between two subsequent erosion issue sprites.
                Vector3 delta = Vector3.up / 100.0f;
                Vector3 currentRoof = blockFactory.Roof(gameNode);
                sprites.Sort(Comparer<GameObject>.Create((left, right) => GetSizeOfSprite(left).x.CompareTo(GetSizeOfSprite(right).x)));
                foreach (GameObject sprite in sprites)
                {
                    Vector3 size = GetSizeOfSprite(sprite);
                    // Note: Consider that the position of the sprite is its center.
                    Vector3 halfHeight = (size.y / 2.0f) * Vector3.up;
                    sprite.transform.position = currentRoof + delta + halfHeight;
                    currentRoof = sprite.transform.position + halfHeight;
                }
            }
        }

        protected static Vector3 GetSizeOfSprite(GameObject go)
        {
            // The game object representing an erosion is a composite of 
            // multiple LOD child objects to be drawn depending how close
            // the camera is. The container object 'go' itself does not
            // have a renderer. We need to obtain the renderer of the
            // first child hat represents the object at LOD 0 instead.
            Renderer renderer = go.GetComponentInChildren<Renderer>();
            // Note: renderer.sprite.bounds.size yields the original size
            // of the sprite of the prefab. It does not consider the scaling.
            // It depends only upon the imported graphic. That is why we
            // need to use renderer.bounds.size.
            return renderer.bounds.size;
        }

        private void BoundingBox(ICollection<GameObject> gameNodes, out Vector2 leftLowerCorner, out Vector2 rightUpperCorner)
        {
            if (gameNodes.Count == 0)
            {
                leftLowerCorner = Vector2.zero;
                rightUpperCorner = Vector2.zero;
            }
            else
            {
                leftLowerCorner = new Vector2(Mathf.Infinity, Mathf.Infinity);
                rightUpperCorner = new Vector2(Mathf.NegativeInfinity, Mathf.NegativeInfinity);

                foreach (GameObject go in gameNodes)
                {
                    Node node = go.GetComponent<NodeRef>().node;

                    // Note: go.transform.position denotes the center of the object
                    Vector3 extent = node.IsLeaf() ? blockFactory.GetSize(go) / 2.0f : go.GetComponent<Renderer>().bounds.extents;
                    Vector3 position = node.IsLeaf() ? blockFactory.GetCenterPosition(go) : go.transform.position;
                    {
                        // x co-ordinate of lower left corner
                        float x = position.x - extent.x;
                        if (x < leftLowerCorner.x)
                        {
                            leftLowerCorner.x = x;
                        }
                    }
                    {
                        // z co-ordinate of lower left corner
                        float z = position.z - extent.z;
                        if (z < leftLowerCorner.y)
                        {
                            leftLowerCorner.y = z;
                        }
                    }
                    {   // x co-ordinate of upper right corner
                        float x = position.x + extent.x;
                        if (x > rightUpperCorner.x)
                        {
                            rightUpperCorner.x = x;
                        }
                    }
                    {
                        // z co-ordinate of upper right corner
                        float z = position.z + extent.z;
                        if (z > rightUpperCorner.y)
                        {
                            rightUpperCorner.y = z;
                        }
                    }
                }
            }
        }
    }
}
