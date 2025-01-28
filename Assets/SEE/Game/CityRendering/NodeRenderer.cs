using System;
using System.Collections.Generic;
using System.Linq;
using SEE.Controls.Interactables;
using SEE.DataModel.DG;
using SEE.Game.City;
using SEE.Game.Operator;
using SEE.GO;
using SEE.GO.Decorators;
using SEE.GO.NodeFactories;
using SEE.Layout;
using SEE.Layout.NodeLayouts;
using SEE.Utils;
using UnityEngine;
using UnityEngine.Assertions;
using InvalidOperationException = System.InvalidOperationException;

namespace SEE.Game.CityRendering
{
    /// <summary>
    /// Implements the functions of the <see cref="GraphRenderer"/> related to nodes.
    /// </summary>
    public partial class GraphRenderer
    {
        /// <summary>
        /// Sets the name (<see cref="Node.ID"/>) and tag (<see cref="Tags.Node"/>)
        /// of given <paramref name="gameNode"/> and lets the node reference
        /// of it refer to <paramref name="node"/>.
        /// </summary>
        /// <param name="node">graph node represented by <paramref name="gameNode"/></param>
        /// <param name="gameNode">game node representing <paramref name="node"/></param>
        private static void SetGeneralNodeAttributes(Node node, GameObject gameNode)
        {
            gameNode.name = node.ID;
            gameNode.tag = Tags.Node;
            gameNode.AddComponent<NodeRef>().Value = node;
        }

        /// <summary>
        /// Creates and returns a new game object for representing the given <paramref name="node"/>
        /// as a leaf node. The exact kind of representation depends upon the leaf-node factory. The node is
        /// scaled according to the WidthMetric, HeightMetric, and DepthMetric of the current settings.
        /// Its style is determined by <see cref="SelectStyle(Node)"/>.
        /// The <paramref name="node"/> is attached to that new game object via a NodeRef component.
        ///
        /// Precondition: <paramref name="node"/> must be a leaf node in the node hierarchy.
        /// </summary>
        /// <param name="node">node for which to create a game object</param>
        /// <param name="addToGraphElementIDMap">if true, the resulting game object will be added to
        /// <see cref="GraphElementIDMap"/></param>
        /// <returns>game object representing given <paramref name="node"/></returns>
        private GameObject CreateGameNode(Node node, bool addToGraphElementIDMap = true)
        {
            if (!nodeTypeToFactory.TryGetValue(node.Type, out NodeFactory nodeFactory))
            {
                throw new InvalidOperationException($"No node type factory for node type {node.Type}.\n");
            }
            GameObject result = nodeFactory.NewBlock(SelectStyle(node), SelectMetrics(node));
            SetGeneralNodeAttributes(node, result);
            if (addToGraphElementIDMap)
            {
                GraphElementIDMap.Add(result);
            }
            return result;
        }

        /// <summary>
        /// Adds LOD to <paramref name="gameNode"/> and prepares it for interaction.
        /// If <paramref name="city"/> is different from null, <paramref name="gameNode"/>
        /// and all its descendants will respect the <paramref name="city"/> as portal.
        /// </summary>
        /// <param name="gameNode">game node to be finished</param>
        /// <param name="city">the game object representing the city in which to draw this node;
        /// it has the settings attached and the information about the scale, position, and
        /// portal of the city</param>
        private void FinishGameNode(GameObject gameNode, GameObject city = null)
        {
            AddLOD(gameNode);
            InteractionDecorator.PrepareForInteraction(gameNode);
            if (city != null)
            {
                Portal.SetPortal(city, gameNode, Portal.IncludeDescendants.AllDescendants);
            }
        }

        /// <summary>
        /// Creates and returns a new game object for representing the given <paramref name="node"/>.
        /// The <paramref name="node"/> is attached to that new game object via a NodeRef component.
        /// LOD is added, its portal is set, and the resulting game node is prepared for interaction.
        /// </summary>
        /// <param name="node">graph node to be represented</param>
        /// <param name="city">the game object representing the city in which to draw this node;
        /// it has the information about how to draw the node and portal of the city</param>
        /// <returns>game object representing given <paramref name="node"/></returns>
        /// <remarks>Implements <see cref="IGraphRenderer.DrawNode(Node, GameObject)"/>.</remarks>
        public GameObject DrawNode(Node node, GameObject city = null)
        {
            Assert.IsTrue(node.ItsGraph.MaxDepth >= 0, $"Graph of node {node.ID} has negative depth");
            node.TryGetNumeric(Graph.MetricLevel, out float value);
            scaler.UpdateMetricLevel(value);
            GameObject result = CreateGameNode(node);
            FinishGameNode(result, city);
            return result;
        }

        /// <summary>
        /// Adds a LOD group to <paramref name="gameObject"/> with only a single LOD.
        /// This is used to cull the object if it gets too small. The percentage
        /// by which to cull is retrieved from <see cref="settings.LODCulling"/>
        /// </summary>
        /// <param name="gameObject">object where to add the LOD group</param>
        private void AddLOD(GameObject gameObject)
        {
            LODGroup lodGroup = gameObject.AddComponent<LODGroup>();
            // Only a single LOD: we either render or cull.
            LOD[] lods = new LOD[1];
            Renderer[] renderers = new Renderer[1];
            renderers[0] = gameObject.GetComponent<Renderer>();
            lods[0] = new LOD(Settings.LODCulling, renderers);
            lodGroup.SetLODs(lods);
            lodGroup.RecalculateBounds();
        }

        /// <summary>
        /// Applies AddLOD to every game object in <paramref name="gameObjects"/>.
        /// </summary>
        /// <param name="gameObjects">the list of game objects where AddLOD is to be applied</param>
        private void AddLOD(IEnumerable<GameObject> gameObjects)
        {
            foreach (GameObject go in gameObjects)
            {
                AddLOD(go);
            }
        }

        /// <summary>
        /// Returns a style index as a linear interpolation of X for range [0..M-1]
        /// where M is the number of available styles of the leafNodeFactory (if
        /// the node is a leaf) or innerNodeFactory (if it is an inner node)
        /// and X = C / metricMaximum and C is as follows:
        /// Let M be the metric selected for the style metric (the style metric
        /// for leaves if <paramref name="node"/> is a leaf or for an inner node
        /// otherwise). M can be either the name of a node metric or a number.
        /// If M is the name of a node metric, C is the normalized metric value of
        /// <paramref name="node"/> for the attribute chosen for the style
        /// and metricMaximum is the maximal value of the style metric. If M is
        /// instead a number, C is that value -- clamped into [0, S] where S is
        /// the maximal number of styles available) and metricMaximum is S.
        /// </summary>
        /// <param name="node">node for which to determine the style index</param>
        /// <returns>style index</returns>
        private int SelectStyle(Node node)
        {
            if (Settings.NodeTypes.TryGetValue(node.Type, out VisualNodeAttributes value))
            {
                return value.ColorProperty.Property switch
                {
                    PropertyKind.Metric => NodeMetricToColor(node, value.ColorProperty.ColorMetric),
                    PropertyKind.Type => NodeTypeToColor(node, value.ColorProperty.ByLevel),
                    _ => throw new NotImplementedException($"Unhandled {typeof(PropertyKind)} {value.ColorProperty.Property}")
                };
            }
            else
            {
                Debug.LogError($"No color specification for node {node.ID} of type {node.Type}.\n");
                return 0;
            }

            int NodeTypeToColor(Node node, bool byLevel)
            {
                if (byLevel)
                {
                    /// This color will be adjusted according to the level of <paramref name="node"/> in the
                    /// node hierarchy, i.e., the artificial Metric.Level.
                    return NodeMetricToColor(node, Graph.MetricLevel);
                }
                else
                {
                    return 0;
                }
            }

            int NodeMetricToColor(Node node, string colorMetric)
            {
                NodeFactory nodeFactory = nodeTypeToFactory[node.Type];
                uint numberOfStyles = nodeFactory.NumberOfStyles();

                float metricMaximum;
                if (FloatUtils.TryGetFloat(colorMetric, out float metricValue))
                {
                    // The colorMetric name is actually a constant number.
                    metricMaximum = numberOfStyles;
                    metricValue = Mathf.Clamp(metricValue, 0.0f, metricMaximum);
                }
                else
                {
                    if (!node.TryGetNumeric(colorMetric, out float _))
                    {
                        Debug.LogWarning($"Value of color metric {colorMetric} for node {node.ID} is undefined.\n");
                        return 0;
                    }

                    metricMaximum = scaler.GetNormalizedMaximum(colorMetric);
                    metricValue = scaler.GetNormalizedValue(colorMetric, node);
                    if (metricValue > metricMaximum)
                    {
                        Debug.LogError($"not true: {metricValue} <= {metricMaximum} for color metric {colorMetric} of node {node.ID}.\n");
                        return Mathf.RoundToInt(metricMaximum);
                    }
                }
                return Mathf.RoundToInt(Mathf.Lerp(0.0f, numberOfStyles - 1, metricValue / metricMaximum));
            }
        }

        /// <summary>
        /// Adjusts the style of the given <paramref name="gameNode"/> according
        /// to the metric value of the graph node attached to <paramref name="gameNode"/>
        /// chosen to determine style.
        /// </summary>
        /// <param name="gameNode">a game node representing a leaf or inner graph node</param>
        public void AdjustStyle(GameObject gameNode)
        {
            if (gameNode.TryGetComponent(out NodeRef nodeRef))
            {
                Node node = nodeRef.Value;
                nodeTypeToFactory[node.Type].SetStyle(gameNode, SelectStyle(node));
            }
            else
            {
                throw new($"Game object {gameNode.name} does not have a graph node attached to it.");
            }
        }

        /// <summary>
        /// Adjusts the antenna of the given <paramref name="gameNode"/> according
        /// to the metric value of the graph node attached to <paramref name="gameNode"/>
        /// chosen to determine antenna segments.
        /// </summary>
        /// <param name="gameNode">a game node representing a leaf or inner graph node</param>
        public void AdjustAntenna(GameObject gameNode)
        {
            if (gameNode.TryGetComponent(out NodeRef nodeRef))
            {
                if (nodeTypeToAntennaDectorator.TryGetValue(nodeRef.Value.Type, out AntennaDecorator decorator))
                {
                    decorator.AddAntenna(gameNode);
                }
            }
            else
            {
                throw new($"Game object {gameNode.name} does not have a graph node attached to it.");
            }
        }

        /// <summary>
        /// Returns the selected metrics for <paramref name="node"/> that are to be
        /// used to influence visual attributes.
        /// </summary>
        /// <param name="node">graph node whose metrics are to be selected</param>
        /// <returns>selected metrics of <paramref name="node"/></returns>
        private float[] SelectMetrics(Node node)
        {
            if (Settings.NodeTypes.TryGetValue(node.Type, out VisualNodeAttributes attributes)
                && attributes.Shape is NodeShapes.Spiders or NodeShapes.Polygons or NodeShapes.Bars)
            {
                // FIXME: Not all nodes have necessarily the same set of metrics.
                // If one does not have a particular numeric attributes, but others
                // have, that value should be 0. The metric vectors of all nodes
                // should have the same number of elements and same order so that
                // the length of the shapes are truly comparable.
                IList<float> metrics = new List<float>();
                // FIXME: There may be attributes that are not metrics, e.g., Source.Line.
                // We need a user setting that decides which attributes to use.
                AddMetrics(node, metrics, node.FloatAttributes.Keys);
                AddMetrics(node, metrics, node.IntAttributes.Keys);

                // There should be at least three values.
                if (metrics.Count < 3)
                {
                    Debug.LogWarning($"There should be at least three metrics for node {node.ID}. Adding zeros.");
                }
                for (int i = metrics.Count; i < 3; ++i)
                {
                    metrics.Add(0);
                }
                return metrics.ToArray();
            }
            else
            {
                Vector3 scale = GetScale(node);
                if (Settings.NodeLayoutSettings.Kind == NodeLayoutKind.Treemap
                    || Settings.NodeLayoutSettings.Kind == NodeLayoutKind.IncrementalTreeMap)
                {
                    // FIXME: This is ugly. The graph renderer should not need to care what
                    // kind of layout was applied.

                    // Treemaps can represent a metric by the area of a rectangle. Hence,
                    // they can represent only a single metric in the x/z plane.
                    // Let M be the metric selected to be represented by the treemap.
                    // Here, we choose the width metric (as selected by the user) to be M.
                    // That is, M mapped onto the rectangle area.
                    // The area of a rectangle is the product of the lengths of its two sides.
                    // That is why we need to take the square root of M for the lengths, because
                    // sqrt(M) * sqrt(M) = M. If we were instead using M as width and depth of the
                    // rectangle, the area would be M^2, which would skew the visual impression
                    // in the eye of the beholder. Nodes with larger values of M would have a
                    // disproportionally larger area.

                    // The input to the treemap layout are rectangles with equally sized lengths,
                    // in other words, squares. This determines only the ground area of the input
                    // blocks. The height of the blocks remains the original value of the metric
                    // chosen to determine the height, without any kind of transformation.
                    float widthOfSquare = Mathf.Sqrt(scale.x);
                    scale = new Vector3(widthOfSquare, scale.y, widthOfSquare);
                }
                return new[] { scale.x, scale.y, scale.z };
            }

            // Adds the values of the metrics of node listed in metricNames (excluding the
            // metric chosen to determine the color) to metrics.
            void AddMetrics(Node node, ICollection<float> metrics, IEnumerable<string> metricNames)
            {
                HashSet<string> relevantMetrics = new(metricNames);
                string colorMetric = Settings.NodeTypes[node.Type].ColorProperty.ColorMetric;
                relevantMetrics.Remove(colorMetric);
                foreach (string metricName in relevantMetrics)
                {
                    metrics.Add(scaler.GetMetricValue(node, metricName));
                }
            }
        }

        /// <summary>
        /// Adjusts the scale of the given leaf <paramref name="gameNode"/> according
        /// to the metric values of the <paramref name="node"/> attached to
        /// <paramref name="gameNode"/>.
        /// The scale of a leaf is determined by the node's width, height, and depth
        /// metrics (which are determined by the settings).
        /// Precondition: A graph node is attached to <paramref name="gameNode"/>
        /// and has the width, height, and depth metrics set and is a leaf.
        /// </summary>
        /// <param name="gameNode">the game object whose visual attributes are to be adjusted</param>
        [Obsolete]
        public void AdjustScaleOfLeaf(GameObject gameNode)
        {
            Assert.IsNull(gameNode.transform.parent);

            if (gameNode.TryGetComponent(out NodeRef nodeRef))
            {
                Node node = nodeRef.Value;
                if (node.IsLeaf())
                {
                    // Scaled metric values for the three dimensions.
                    Vector3 scale = GetScale(node);

                    // Scale according to the metrics.
                    if (Settings.NodeLayoutSettings.Kind == NodeLayoutKind.Treemap
                        || Settings.NodeLayoutSettings.Kind == NodeLayoutKind.IncrementalTreeMap)
                    {
                        // FIXME: This is ugly. The graph renderer should not need to care what
                        // kind of layout was applied.

                        // Treemaps can represent a metric by the area of a rectangle. Hence,
                        // they can represent only a single metric in the x/z plane.
                        // Let M be the metric selected to be represented by the treemap.
                        // Here, we choose the width metric (as selected by the user) to be M.
                        // That is, M mapped onto the rectangle area.
                        // The area of a rectangle is the product of the lengths of its two sides.
                        // That is why we need to take the square root of M for the lengths, because
                        // sqrt(M) * sqrt(M) = M. If we were instead using M as width and depth of the
                        // rectangle, the area would be M^2, which would skew the visual impression
                        // in the eye of the beholder. Nodes with larger values of M would have a
                        // disproportionally larger area.

                        // The input to the treemap layout are rectangles with equally sized lengths,
                        // in other words, squares. This determines only the ground area of the input
                        // blocks. The height of the blocks remains the original value of the metric
                        // chosen to determine the height, without any kind of transformation.
                        float widthOfSquare = Mathf.Sqrt(scale.x);
                        Vector3 targetScale = new(widthOfSquare, scale.y, widthOfSquare);
                        nodeTypeToFactory[node.Type].SetSize(gameNode, targetScale);
                    }
                    else
                    {
                        nodeTypeToFactory[node.Type].SetSize(gameNode, scale);
                    }
                }
                else
                {
                    throw new Exception($"Game object {gameNode.name} is not a leaf.");
                }
            }
            else
            {
                throw new Exception($"Game object {gameNode.name} does not have a graph node attached to it.");
            }
        }

        /// <summary>
        /// Loads and applies a layout stored in <see cref="Settings.NodeLayoutSettings.LayoutPath.Path"/>.
        /// </summary>
        /// <param name="gameNodes">the nodes to be laid out</param>
        /// <param name="groundLevel">ground level where the lowest node will be placed (y axis)</param>
        internal void LoadLayout(ICollection<GameObject> gameNodes, float groundLevel)
        {
            if (Application.isPlaying)
            {
                // The LayoutGraphNode, unlike LayoutGameNode, does not change the underlying game object
                // representing a node, hence, we have no unwanted side effects when calling the Layout.
                ICollection<LayoutGraphNode> layoutNodes = ToAbstractLayoutNodes(gameNodes);
                LoadedNodeLayout layout = new(groundLevel, Settings.NodeLayoutSettings.LayoutPath.Path);
                foreach (KeyValuePair<ILayoutNode, NodeTransform> item in layout.Layout(layoutNodes))
                {
                    GameObject node = GraphElementIDMap.Find(item.Key.ID);
                    if (node != null)
                    {
                        NodeTransform nodeTransform = item.Value;
                        NodeOperator nodeOperator = node.NodeOperator();
                        // nodeTransform.position.y relates to the ground of the node;
                        // the node operator's y co-ordinate is meant to be the center
                        nodeTransform.Position.y += nodeTransform.Scale.y / 2;
                        //Debug.Log($"{node.name} [{node.transform.position}, {node.transform.lossyScale}] => [{nodeTransform.position}), ({nodeTransform.scale}]\n");
                        nodeOperator.MoveTo(nodeTransform.Position);
                        // FIXME: Scaling doesn't work yet; likely because nodeTransform.scale is world space
                        // but the node operator expects local scale.
                        //nodeOperator.ScaleTo(nodeTransform.scale, animationDuration);
                    }
                }
            }
            else
            {
                throw new NotImplementedException("Loading a layout is currently supported only in play mode."
                    + " In the editor, use 'Draw Data' instead");
            }
        }

        /// <summary>
        /// Yields the collection of LayoutNodes corresponding to the given <paramref name="gameNodes"/>.
        /// Each LayoutNode has the position, scale, and rotation of the game node. The graph node
        /// attached to the game node is passed on to the LayoutNode so that the graph node data is
        /// available to the node layout (e.g., Parent or Children).
        /// Sets also the node levels of all resulting LayoutNodes.
        /// </summary>
        /// <param name="gameNodes">collection of game objects created to represent inner nodes or leaf nodes of a graph</param>
        /// <returns>collection of LayoutNodes representing the information of <paramref name="gameNodes"/> for layouting</returns>
        public static ICollection<LayoutGraphNode> ToAbstractLayoutNodes(ICollection<GameObject> gameNodes)
        {
            IList<LayoutGraphNode> result = new List<LayoutGraphNode>();
            Dictionary<Node, ILayoutNode> toLayoutNode = new();

            foreach (GameObject gameObject in gameNodes)
            {
                Node node = gameObject.GetComponent<NodeRef>().Value;
                LayoutGraphNode layoutNode = new(node, toLayoutNode)
                {
                    // We must transfer the scale from gameObject to layoutNode.
                    // but the layout needs the game object's scale.
                    // Rotation and CenterPosition are all zero. They will be computed by the layout,
                    // Note: LayoutGraphNode does not make a distinction between local and absolute scale.
                    LocalScale = gameObject.transform.lossyScale
                };
                result.Add(layoutNode);
            }
            LayoutNodes.SetLevels(result);
            return result;
        }

        /// <summary>
        /// Returns the scale of the given <paramref name="node"/> as requested by the user's
        /// settings, i.e., what the use specified for the width, height, and depth of a node.
        ///
        /// </summary>
        /// <param name="node">node whose scale is requested</param>
        /// <returns>requested absolute scale in world space</returns>
        private Vector3 GetScale(Node node)
        {
            if (Settings.NodeTypes.TryGetValue(node.Type, out VisualNodeAttributes attribs))
            {
                return new Vector3(GetMetricValue(node, attribs.WidthMetric),
                                   GetMetricValue(node, attribs.HeightMetric),
                                   GetMetricValue(node, attribs.DepthMetric));
            }
            else
            {
                Debug.LogWarning($"No metric specifiction (width, height, depth) for node type {node.Type}.\n");
                return Vector3.zero;
            }
        }

        /// <summary>
        /// If <paramref name="metricName"/> is the name of a metric, the corresponding
        /// normalized value for <paramref name="node"/> is returned. If <paramref name="metricName"/>
        /// can be parsed as a number instead, the parsed number is returned.
        /// The result is clamped into [MinimalBlockLength, MaximalBlockLength] where
        /// MinimalBlockLength is the minimal length for leaf nodes specified by the user and
        /// MaximalBlockLength is the maximal length for leaf nodes specified by the user.
        ///
        /// Precondition: <paramref name="node"/> is a leaf.
        /// </summary>
        /// <param name="node">leaf node whose metric is to be returned</param>
        /// <param name="metricName">the name of a leaf node metric or a number</param>
        /// <returns>the value of <paramref name="node"/>'s metric <paramref name="metricName"/></returns>
        private float GetMetricValue(Node node, string metricName)
        {
            VisualNodeAttributes attribs = Settings.NodeTypes[node.Type];
            return Mathf.Clamp(scaler.GetMetricValue(node, metricName),
                               attribs.MinimalBlockLength,
                               attribs.MaximalBlockLength);
        }

        /// <summary>
        /// Adds decoration for the given <paramref name="gameNode"/> with the global settings
        /// for inner node kinds and nodelayout.
        /// </summary>
        /// <param name="gameNode"></param>
        public void AddDecorations(GameObject gameNode)
        {
            AddDecorations(new List<GameObject> { gameNode });
        }

        /// <summary>
        /// Draws the decorations of the given game nodes.
        /// </summary>
        /// <param name="gameNodes">game nodes to be decorated</param>
        protected void AddDecorations(ICollection<GameObject> gameNodes)
        {
            AddNames(gameNodes);

            foreach (GameObject node in gameNodes)
            {
                AddGeneralDecorations(node);
            }

            AddMarkers(gameNodes);

            // Add software erosion decorators for all nodes if requested.
            if (Settings.ErosionSettings.ShowLeafErosions)
            {
                ErosionIssues issueDecorator = new(Settings.IssueMap(), scaler,
                                                   Settings.ErosionSettings.ErosionScalingFactor * 5);
                // Leaf erosions can even be present on inner nodes, hence, we add all nodes.
                // "Leaf" just refers to the lowest level the erosion type can be present on, which may not be
                // the lowest level of the graph.
                issueDecorator.Add(gameNodes);
            }
            if (Settings.ErosionSettings.ShowInnerErosions)
            {
                ErosionIssues issueDecorator = new(Settings.IssueMap(), scaler,
                                                   Settings.ErosionSettings.ErosionScalingFactor, aggregated: true);
                issueDecorator.Add(FindInnerNodes(gameNodes));
            }
        }

        /// <summary>
        /// Adds markers to all <paramref name="gameNodes"/>.
        /// </summary>
        /// <param name="gameNodes">List of gamenodes to be marked</param>
        private void AddMarkers(ICollection<GameObject> gameNodes)
        {
            MarkerFactory markerFactory = new(Settings.MarkerAttributes);

            foreach (GameObject gameNode in gameNodes)
            {
                Node node = gameNode.GetNode();
                if (node.HasToggle(ChangeMarkers.IsNew))
                {
                    markerFactory.MarkBorn(gameNode);
                }
                else if (node.HasToggle(ChangeMarkers.IsDeleted))
                {
                    markerFactory.MarkDead(gameNode);
                }
                else if (node.HasToggle(ChangeMarkers.IsChanged))
                {
                    markerFactory.MarkChanged(gameNode);
                }
            }
        }

        /// <summary>
        /// Adds decorations applicable to all node kinds to them.
        /// These general decorations currently consist of:
        /// <ul>
        /// <li>Outlines around the node</li>
        /// <li>Antennas</li>
        /// </ul>
        /// </summary>
        /// <param name="gameNode">game object representing a node to be decorated</param>
        protected virtual void AddGeneralDecorations(GameObject gameNode)
        {
            // Add outline around nodes so they can be visually differentiated without needing the same color.
            Node node = gameNode.GetNode();
            VisualNodeAttributes attribs = Settings.NodeTypes[node.Type];
            Outline.Create(gameNode, Color.black, attribs.OutlineWidth);
            nodeTypeToAntennaDectorator[node.Type]?.AddAntenna(gameNode);
        }

        /// <summary>
        /// Adds the source name to the center of the given game nodes as a child
        /// for all given <paramref name="gameNodes"/> except for the root (its name would
        /// be too large and is not really neeed anyway).
        /// </summary>
        /// <param name="gameNodes">game nodes whose source name is to be added</param>
        /// <remarks>This name addition is not be confused with ShowLabel. The latter is
        /// popping up while a node is hovered. This name here is shown all the time.</remarks>
        private void AddNames(IEnumerable<GameObject> gameNodes)
        {
            const float relativeLabelSize = 0.8f;
            GameObject codeCity = null;
            AbstractSEECity city = null;
            foreach (GameObject node in gameNodes)
            {
                Node theNode = node.GetNode();
                if (!theNode.IsRoot() && Settings.NodeTypes[theNode.Type].ShowNames)
                {
                    Vector3 size = node.transform.lossyScale;
                    float length = Mathf.Min(size.x, size.z);
                    // The text may occupy up to RelativeLabelSize of the length.
                    Vector3 position = node.GetRoofCenter();
                    codeCity ??= SceneQueries.GetCodeCity(node.transform).gameObject;
                    city ??= codeCity.GetComponent<AbstractSEECity>();
                    GameObject text = TextFactory.GetTextWithWidth(city: city,
                                                                   text: theNode.SourceName,
                                                                   position: position,
                                                                   width: length * relativeLabelSize,
                                                                   lift: true,
                                                                   textColor: node.GetColor().Invert());
                    text.transform.SetParent(node.transform);
                    AddLOD(text);
                    Portal.SetPortal(codeCity, text);
                }
            }
        }
    }
}
