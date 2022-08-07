using System;
using System.Collections.Generic;
using System.Linq;
using SEE.Controls.Interactables;
using SEE.DataModel;
using SEE.DataModel.DG;
using SEE.Game.City;
using SEE.GO;
using SEE.GO.NodeFactories;
using UnityEngine;
using UnityEngine.Assertions;
using Object = UnityEngine.Object;

namespace SEE.Game
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
        /// Its style is determined by LeafNodeStyleMetric (linear interpolation of a color gradient).
        /// The <paramref name="node"/> is attached to that new game object via a NodeRef component.
        ///
        /// Precondition: <paramref name="node"/> must be a leaf node in the node hierarchy.
        /// </summary>
        /// <param name="node">leaf node</param>
        /// <returns>game object representing given <paramref name="node"/></returns>
        private GameObject CreateGameNode(Node node)
        {
            NodeFactory nodeFactory = nodeTypeToFactory[node.Type];
            GameObject result = nodeFactory.NewBlock(SelectStyle(node), SelectMetrics(node));
            SetGeneralNodeAttributes(node, result);
            Debug.LogWarning("AddAntenna\n");
            // FIXME leafAntennaDecorator.AddAntenna(result);
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
                Portal.SetPortal(city, gameNode, Portal.IncludeDescendants.ALL_DESCENDANTS);
            }
        }

        /// <summary>
        /// Creates and returns a new game object for representing the given <paramref name="node"/>.
        /// The <paramref name="node"/> is attached to that new game object via a NodeRef component.
        /// LOD is added and the resulting node is prepared for interaction.
        /// </summary>
        /// <param name="node">graph node to be represented</param>
        /// <param name="city">the game object representing the city in which to draw this node;
        /// it has the information about how to draw the node and portal of the city</param>
        /// <returns>game object representing given <paramref name="node"/></returns>
        public GameObject DrawNode(Node node, GameObject city = null)
        {
            Assert.IsTrue(node.ItsGraph.MaxDepth >= 0, $"Graph of node {node.ID} has negative depth");
            GameObject result = CreateGameNode(node);
            FinishGameNode(result, city);
            return result;
        }

        /// <summary>
        /// Re-draws <paramref name="gameNode"/> as a leaf node.
        ///
        /// Precondition: <paramref name="gameNode"/> is an inner node.
        /// Postcondition: <paramref name="gameNode"/> is a leaf node.
        /// </summary>
        /// <param name="gameNode">node to be drawn as leaf node</param>
        [Obsolete]
        public void RedrawAsLeafNode(GameObject gameNode)
        {
            // We create a new leaf node and then "steal" its mesh.
            Node node = gameNode.GetNode();
            GameObject leafNode = CreateGameNode(node);
            gameNode.GetComponent<MeshFilter>().mesh = leafNode.GetComponent<MeshFilter>().mesh;

            // The original ground position of gameNode.
            Vector3 groundPosition = nodeTypeToFactory[node.Type].Ground(gameNode);

            // gameNode must be re-sized according to the metrics of the leaf node
            nodeTypeToFactory[node.Type].SetSize(gameNode, leafNode.transform.lossyScale);

            // Finally, because the height has changed, we need to adjust the position.
            nodeTypeToFactory[node.Type].SetGroundPosition(gameNode, groundPosition);

            // If newMesh is modified, call the following (just for the record, not necessary here).
            // Required by almost every shader to calculate brightness levels correctly:
            //   newMesh.RecalculateNormals();
            // Required for some shaders, especially those which use bump mapping.
            // This step depends on the normals, so it needs to be executed after
            // you recalculated the normals:
            //   newMesh.RecalculateTangents();
            // Required for reliably detecting if the mesh is off-screen (so it can
            // be culled) and for collision detection if you are using it as a
            // MeshCollider. This method actually gets called automatically when
            // you set the .triangles, but not when you set the .vertices:
            //   newMesh.RecalculateBounds();

            // leafNode is no longer needed; we have its mesh that is all we needed.
            // It can be dismissed.
            Object.Destroy(leafNode);
        }

        /// <summary>
        /// Re-draws <paramref name="gameNode"/> as an inner node.
        ///
        /// Precondition: <paramref name="gameNode"/> is a leaf node.
        /// Postcondition: <paramref name="gameNode"/> is an inner node.
        /// </summary>
        /// <param name="gameNode">node to be drawn as inner node</param>
        [Obsolete]
        public void RedrawAsInnerNode(GameObject gameNode)
        {
            // We create a new inner node and then "steal" its mesh.
            Node node = gameNode.GetNode();
            GameObject innerNode = CreateGameNode(node);
            gameNode.GetComponent<MeshFilter>().mesh = innerNode.GetComponent<MeshFilter>().mesh;

            // The original ground position of gameNode.
            Vector3 groundPosition = nodeTypeToFactory[node.Type].Ground(gameNode);
            /// We maintain the depth and width of gameNode, but adjust its height
            /// according to the metric that determines the height of inner nodes.
            /// The call to <see cref="CreateInnerGameNode"/> has set the height
            /// of <see cref="innerNode"/> accordingly.
            nodeTypeToFactory[node.Type].SetHeight(gameNode, innerNode.transform.lossyScale.y);

            // Finally, because the height has changed, we need to adjust the position.
            nodeTypeToFactory[node.Type].SetGroundPosition(gameNode, groundPosition);

            // innerNode is no longer needed; we have its mesh that is all we needed.
            // It can be dismissed.
            GameObject.Destroy(innerNode);
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
            // Only a single LOD: we either or cull.
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
        private void AddLOD(ICollection<GameObject> gameObjects)
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
                switch (value.ColorProperty.Property)
                {
                    case PropertyKind.Metric:
                        return NodeMetricToColor(node, value.ColorProperty.ColorMetric);
                    case PropertyKind.Type:
                        /// Node factories using the node type for determining the color have only one color.
                        /// <seealso cref="SetNodeFactories"/>
                        return 0;
                    default:
                        throw new NotImplementedException($"Unhandled {typeof(PropertyKind)} {value.ColorProperty.Property}");
                }
            }
            else
            {
                Debug.LogError($"No color specification for node {node.ID} of type {node.Type}.\n");
                return 0;
            }

            int NodeMetricToColor(Node node, string colorMetric)
            {
                NodeFactory nodeFactory = nodeTypeToFactory[node.Type];
                uint numberOfStyles = nodeFactory.NumberOfStyles();

                float metricMaximum;
                if (Utils.FloatUtils.TryGetFloat(colorMetric, out float metricValue))
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
        /// Returns the center of the roof of the given <paramref name="gameNode"/>.
        ///
        /// Precondition: <paramref name="gameNode"/> must have been created by this graph renderer.
        /// </summary>
        /// <param name="gameNode">game node for which to determine the roof position</param>
        /// <returns>roof position</returns>
        internal Vector3 GetRoof(GameObject gameNode)
        {
            if (gameNode.TryGetComponent<NodeRef>(out NodeRef nodeRef))
            {
                Node node = nodeRef.Value;
                return nodeTypeToFactory[node.Type].Roof(gameNode);
            }
            else
            {
                throw new Exception($"Game object {gameNode.name} does not have a graph node attached to it.");
            }
        }

        /// <summary>
        /// Returns the scale of the given <paramref name="gameNode"/>.
        ///
        /// Precondition: <paramref name="gameNode"/> must have been created by this graph renderer.
        /// </summary>
        /// <param name="gameNode"></param>
        /// <returns>scale of <paramref name="gameNode"/></returns>
        internal Vector3 GetSize(GameObject gameNode)
        {
            if (gameNode.TryGetComponent<NodeRef>(out NodeRef nodeRef))
            {
                Node node = nodeRef.Value;
                return nodeTypeToFactory[node.Type].GetSize(gameNode);
            }
            else
            {
                throw new Exception($"Game object {gameNode.name} does not have a graph node attached to it.");
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
            if (gameNode.TryGetComponent<NodeRef>(out NodeRef nodeRef))
            {
                Node node = nodeRef.Value;
                int style = SelectStyle(node);
                if (node.IsLeaf())
                {
                    nodeTypeToFactory[node.Type].SetStyle(gameNode, style);
                }
                else
                {
                    // TODO: for some reason, the material is selected twice. Once here and once
                    // somewhere earlier (I believe in NewBlock somewhere).
                    nodeTypeToFactory[node.Type].SetStyle(gameNode, style);
                }
            }
            else
            {
                throw new Exception($"Game object {gameNode.name} does not have a graph node attached to it.");
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
                Node node = nodeRef.Value;
                // FIXME: AddAnntenna
                //if (node.IsLeaf())
                //{
                //    leafAntennaDecorator.AddAntenna(gameNode);
                //}
                //else
                //{
                //    innerAntennaDecorator.AddAntenna(gameNode);
                //}
            }
            else
            {
                throw new Exception($"Game object {gameNode.name} does not have a graph node attached to it.");
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
            if (Settings.NodeTypes[node.Type].Shape == NodeShapes.Spiders
                || Settings.NodeTypes[node.Type].Shape == NodeShapes.Polygons
                || Settings.NodeTypes[node.Type].Shape == NodeShapes.Bars)
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
                for (int i = metrics.Count; i < 3; ++i)
                {
                    metrics.Add(0);
                }
                return metrics.ToArray();
            }
            Vector3 scale = GetScale(node);
            if (Settings.NodeLayoutSettings.Kind == NodeLayoutKind.Treemap)
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
            return new float[] { scale.x, scale.y, scale.z };

            void AddMetrics(Node node, IList<float> metrics, ICollection<string> metricNames)
            {
                HashSet<string> relevantMetrics = new HashSet<string>(metricNames);
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
                    if (Settings.NodeLayoutSettings.Kind == NodeLayoutKind.Treemap)
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
                        Vector3 targetScale = new Vector3(widthOfSquare, scale.y, widthOfSquare);
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
        /// Returns the scale of the given <paramref name="node"/> as requested by the user's
        /// settings, i.e., what the use specified for the width, height, and depth of a node.
        ///
        /// </summary>
        /// <param name="node">node whose scale is requested</param>
        /// <returns>requested absolute scale in world space</returns>
        private Vector3 GetScale(Node node)
        {
            VisualNodeAttributes attribs = Settings.NodeTypes[node.Type];
            return new Vector3(GetMetricValue(node, attribs.WidthMetric),
                               GetMetricValue(node, attribs.HeightMetric),
                               GetMetricValue(node, attribs.DepthMetric));
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
        protected void AddDecorations(GameObject gameNode)
        {
            AddDecorations(new List<GameObject> { gameNode });
        }

        /// <summary>
        /// Draws the decorations of the given game nodes.
        /// </summary>
        /// <param name="gameNodes">game nodes to be decorated</param>
        protected void AddDecorations(ICollection<GameObject> gameNodes)
        {
            ICollection<GameObject> leafNodes = FindLeafNodes(gameNodes);
            ICollection<GameObject> innerNodes = FindInnerNodes(gameNodes);

            // Add software erosion decorators for all nodes if requested.
            if (Settings.ErosionSettings.ShowInnerErosions)
            {
                //FIXME: This should instead check whether each node has non-aggregated metrics available,
                // and use those instead of the aggregated ones, because they are usually more accurate (see MetricImporter).
                ErosionIssues issueDecorator = new ErosionIssues(Settings.InnerIssueMap(),
                                                                 scaler, Settings.ErosionSettings.ErosionScalingFactor);
                issueDecorator.Add(innerNodes);
            }
            if (Settings.ErosionSettings.ShowLeafErosions)
            {
                ErosionIssues issueDecorator = new ErosionIssues(Settings.LeafIssueMap(),
                                                                 scaler, Settings.ErosionSettings.ErosionScalingFactor * 5);
                issueDecorator.Add(leafNodes);
            }

            AddLabels(innerNodes);

            foreach (GameObject node in leafNodes.Concat(innerNodes))
            {
                AddGeneralDecorations(node);
            }
        }

        /// <summary>
        /// Adds decorations applicable to all node kinds to them.
        /// These general decorations currently consist of:
        /// <ul>
        /// <li>Outlines around the node</li>
        /// </ul>
        /// </summary>
        protected virtual void AddGeneralDecorations(GameObject node)
        {
            // Add outline around nodes so they can be visually differentiated without needing the same color.
            VisualNodeAttributes attribs = Settings.NodeTypes[node.GetNode().Type];
            Outline.Create(node, Color.black, attribs.OutlineWidth);
        }

        /// <summary>
        /// Adds the source name as a label to the center of the given game nodes as a child
        /// for all given <paramref name="gameNodes"/> except for the root (its label would
        /// be too large and is not really neeed anyway).
        /// </summary>
        /// <param name="gameNodes">game nodes whose source name is to be added</param>
        /// <param name="innerNodeFactory">inner node factory</param>
        /// <returns>the game objects created for the text labels</returns>
        private void AddLabels(IEnumerable<GameObject> gameNodes)
        {
            GameObject codeCity = null;
            foreach (GameObject node in gameNodes)
            {
                Node theNode = node.GetNode();
                if (!theNode.IsRoot() && Settings.NodeTypes[theNode.Type].ShowNames)
                {
                    Vector3 size = node.transform.lossyScale;
                    float length = Mathf.Min(size.x, size.z);
                    // The text may occupy up to 30% of the length.
                    GameObject text = TextFactory.GetTextWithWidth(theNode.SourceName,
                                                                   node.transform.position, length * 0.3f);
                    text.transform.SetParent(node.transform);
                    codeCity ??= SceneQueries.GetCodeCity(node.transform).gameObject;
                    Portal.SetPortal(codeCity, text);
                }
            }
        }
    }
}
