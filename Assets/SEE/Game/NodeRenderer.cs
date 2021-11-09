using SEE.DataModel;
using SEE.DataModel.DG;
using SEE.GO;
using SEE.Layout;
using SEE.Utils;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;

namespace SEE.Game
{
    public partial class GraphRenderer
    {
        /// <summary>
        /// Applies the given <paramref name="layout"/> to the given <paramref name="gameNode"/>,
        /// i.e., sets its size and position according to the <paramref name="layout"/> and
        /// possibly rotates it. The game node can represent a leaf or inner node of the node
        /// hierarchy.
        ///
        /// Precondition: <paramref name="gameNode"/> must have NodeRef component referencing a
        /// graph node.
        /// </summary>
        /// <param name="gameNode">the game node the layout should be applied to</param>
        /// <param name="layout">layout to be applied to the game node</param>
        public void Apply(GameObject gameNode, GameObject itsParent, ILayoutNode layout)
        {
            Node node = gameNode.GetComponent<NodeRef>().Value;

            if (node.IsLeaf())
            {
                // Leaf nodes were created as blocks by leaveNodeFactory.
                // We need to first scale the game node and only afterwards set its
                // position because transform.scale refers to the center position.
                leafNodeFactory.SetSize(gameNode, layout.LocalScale);
                // FIXME: Must adjust layout.CenterPosition.y
                leafNodeFactory.SetGroundPosition(gameNode, layout.CenterPosition);
            }
            else
            {
                // Inner nodes were created by innerNodeFactory.
                innerNodeFactory.SetSize(gameNode, layout.LocalScale);
                // FIXME: Must adjust layout.CenterPosition.y
                innerNodeFactory.SetGroundPosition(gameNode, layout.CenterPosition);
            }
            // Rotate the game object.
            if (node.IsLeaf())
            {
                leafNodeFactory.Rotate(gameNode, layout.Rotation);
            }
            else
            {
                innerNodeFactory.Rotate(gameNode, layout.Rotation);
            }

            // fit layoutNodes into parent
            //Fit(itsParent, layoutNodes); // FIXME

            // Stack the node onto its parent (maintaining its x and z co-ordinates)
            Vector3 levelIncrease = gameNode.transform.position;
            levelIncrease.y = itsParent.transform.position.y + itsParent.transform.lossyScale.y / 2.0f + LevelDistance;
            gameNode.transform.position = levelIncrease;

            // Add the node to the node hierarchy
            gameNode.transform.SetParent(itsParent.transform);

            // Prepare the node for interactions
            InteractionDecorator.PrepareForInteraction(gameNode);

            // Decorations must be applied after the blocks have been placed, so that
            // we also know their positions.
            AddDecorations(gameNode);
        }

        /// <summary>
        /// Sets the name (<see cref="Node.ID"/>) and tag (<see cref="Tags.Node"/>
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
        /// Create and returns a new game object for representing the given <paramref name="node"/>
        /// as a leaf node. The exact kind of representation depends upon the leaf-node factory. The node is
        /// scaled according to the WidthMetric, HeightMetric, and DepthMetric of the current settings.
        /// Its style is determined by LeafNodeStyleMetric (linerar interpolation of a color gradient).
        /// The <paramref name="node"/> is attached to that new game object via a NodeRef component.
        ///
        /// Precondition: <paramref name="node"/> must be a leaf node in the node hierarchy.
        /// </summary>
        /// <param name="node">leaf node</param>
        /// <returns>game object representing given <paramref name="node"/></returns>
        private GameObject CreateLeafGameNode(Node node)
        {
            // The deeper the node in the node hierarchy (quantified by a node's level), the
            // later it should be drawn, or in other words, the higher its offset in the
            // render queue should be. We are assuming that the nodes are stacked on each
            // other according to the node hierarchy. Leaves are on top of all other nodes.
            // That is why we put them at the highest necessary rendering queue offset.
            GameObject result = leafNodeFactory.NewBlock(SelectStyle(node), node.ItsGraph.MaxDepth);
            SetGeneralNodeAttributes(node, result);
            AdjustScaleOfLeaf(result);
            return result;
        }

        /// <summary>
        /// Creates a new game object for an inner node using innerNodeFactory.
        /// The inner <paramref name="node"/> is attached to that new game object
        /// via a NodeRef component. The style and height of the resulting game
        /// object are adjusted according to the selected InnerNodeStyleMetric
        /// and InnerNodeHeightMetric, respectively. The other scale dimensions
        /// are not changed.
        ///
        /// Precondition: <paramref name="node"/> must be an inner node of the node
        /// hierarchy.
        /// </summary>
        /// <param name="node">graph node for which to create the game node</param>
        /// <returns>new game object for the inner node</returns>
        private GameObject CreateInnerGameNode(Node node)
        {
            // The deeper the node in the node hierarchy (quantified by a node's level), the
            // later it should be drawn, or in other words, the higher its offset in the
            // render queue should be. We are assuming that the nodes are stacked on each
            // other according to the node hierarchy. Leaves are on top of all other nodes.
            GameObject result = innerNodeFactory.NewBlock(style: SelectStyle(node), renderQueueOffset: node.Level);
            SetGeneralNodeAttributes(node, result);
            AdjustHeightOfInnerNode(result);
            return result;
        }

        /// <summary>
        /// Adds LOD to <paramref name="gameNode"/> and prepares it for interaction.
        /// </summary>
        /// <param name="gameNode">game node to be finished</param>
        private void FinishGameNode(GameObject gameNode)
        {
            AddLOD(gameNode);
            InteractionDecorator.PrepareForInteraction(gameNode);
        }

        /// <summary>
        /// Create and returns a new game object for representing the given <paramref name="node"/>.
        /// The exact kind of representation depends upon the leaf-node factory. The node is
        /// scaled according to the WidthMetric, HeightMetric, and DepthMetric of the current settings.
        /// Its style is determined by LeafNodeStyleMetric (linerar interpolation of a color gradient).
        /// The <paramref name="node"/> is attached to that new game object via a NodeRef component.
        /// LOD is added and the resulting node is prepared for interaction.
        /// Precondition: <paramref name="node"/> must be a leaf node in the node hierarchy.
        /// </summary>
        /// <param name="node">leaf node</param>
        /// <returns>game object representing given <paramref name="node"/></returns>
        public GameObject DrawLeafNode(Node node)
        {
            Assert.IsTrue(node.ItsGraph.MaxDepth >= 0, $"Graph of node {node.ID} has negative depth");

            GameObject result = CreateLeafGameNode(node);
            FinishGameNode(result);
            return result;
        }

        /// <summary>
        /// Creates a new game object for an inner node using innerNodeFactory.
        /// The inner <paramref name="node"/> is attached to that new game object
        /// via a NodeRef component. The style and height of the resulting game
        /// object are adjusted according to the selected InnerNodeStyleMetric
        /// and InnerNodeHeightMetric, respectively. The other scale dimensions
        /// are not changed. In addition, level of details are added as well
        /// as all components needed for interaction with this game object.
        /// LOD is added and the resulting node is prepared for interaction.
        ///
        /// Precondition: <paramref name="node"/> must be an inner node of the node
        /// hierarchy.
        /// </summary>
        /// <param name="node">graph node for which to create the game node</param>
        /// <returns>new game object for the inner node</returns>
        public GameObject DrawInnerNode(Node node)
        {
            GameObject result = CreateInnerGameNode(node);
            FinishGameNode(result);
            return result;
        }

        /// <summary>
        /// Re-draws <paramref name="gameNode"/> as a leaf node.
        ///
        /// Precondition: <paramref name="gameNode"/> is an inner node.
        /// Postcondition: <paramref name="gameNode"/> is a leaf node.
        /// </summary>
        /// <param name="gameNode">node to be drawn as leaf node</param>
        public void RedrawAsLeafNode(GameObject gameNode)
        {
            // We create a new leaf node and then "steal" its mesh.
            Node node = gameNode.GetNode();
            GameObject leafNode = CreateLeafGameNode(node);
            gameNode.GetComponent<MeshFilter>().mesh = leafNode.GetComponent<MeshFilter>().mesh;

            // The original ground position of gameNode.
            Vector3 groundPosition = innerNodeFactory.Ground(gameNode);

            // gameNode must be re-sized according to the metrics of the leaf node
            innerNodeFactory.SetSize(gameNode, leafNode.transform.lossyScale);

            // Finally, because the height has changed, we need to adjust the position.
            innerNodeFactory.SetGroundPosition(gameNode, groundPosition);

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
            GameObject.Destroy(leafNode);
        }

        /// <summary>
        /// Re-draws <paramref name="gameNode"/> as an inner node.
        ///
        /// Precondition: <paramref name="gameNode"/> is a leaf node.
        /// Postcondition: <paramref name="gameNode"/> is an inner node.
        /// </summary>
        /// <param name="gameNode">node to be drawn as inner node</param>
        public void RedrawAsInnerNode(GameObject gameNode)
        {
            // We create a new inner node and then "steal" its mesh.
            Node node = gameNode.GetNode();
            GameObject innerNode = CreateInnerGameNode(node);
            gameNode.GetComponent<MeshFilter>().mesh = innerNode.GetComponent<MeshFilter>().mesh;

            // The original ground position of gameNode.
            Vector3 groundPosition = leafNodeFactory.Ground(gameNode);
            /// We maintain the depth and width of gameNode, but adjust its height
            /// according to the metric that determines the height of inner nodes.
            /// The call to <see cref="CreateInnerGameNode"/> has set the height
            /// of <see cref="innerNode"/> accordingly.
            leafNodeFactory.SetHeight(gameNode, innerNode.transform.lossyScale.y);

            // Finally, because the height has changed, we need to adjust the position.
            leafNodeFactory.SetGroundPosition(gameNode, groundPosition);

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
            lods[0] = new LOD(settings.LODCulling, renderers);
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
            bool isLeaf = node.IsLeaf();
            NodeFactory nodeFactory = isLeaf ? leafNodeFactory : innerNodeFactory;
            uint numberOfStyles = nodeFactory.NumberOfStyles();
            string colorMetric = isLeaf ? settings.LeafNodeSettings.ColorMetric
                                        : settings.InnerNodeSettings.ColorMetric;
            float metricMaximum;
            if (TryGetFloat(colorMetric, out float metricValue))
            {
                // The colorMetric name is actually a constant number.
                metricMaximum = numberOfStyles;
                metricValue = Mathf.Clamp(metricValue, 0.0f, metricMaximum);
            }
            else
            {
                if (!node.TryGetNumeric(colorMetric, out float _))
                {
                    Debug.LogWarning($"value of color metric {colorMetric} for node {node.ID} is undefined.\n");
                    return 0;
                }

                metricMaximum = scaler.GetNormalizedMaximum(colorMetric);
                metricValue = scaler.GetNormalizedValue(colorMetric, node);
                Assert.IsTrue(metricValue <= metricMaximum);
            }
            return Mathf.RoundToInt(Mathf.Lerp(0.0f, numberOfStyles - 1, metricValue / metricMaximum));
        }

        /// <summary>
        /// Tries to parse <paramref name="floatString"/> as a floating point number.
        /// Upon success, its value is return in <paramref name="value"/> and true
        /// is returned. Otherwise false is returned and <paramref name="value"/>
        /// is undefined.
        /// </summary>
        /// <param name="floatString">string to be parsed for a floating point number</param>
        /// <param name="value">parsed floating point value; defined only if this method returns true</param>
        /// <returns>true if a floating point number could be parsed successfully</returns>
        private bool TryGetFloat(string floatString, out float value)
        {
            try
            {
                value = float.Parse(floatString, CultureInfo.InvariantCulture.NumberFormat);
                return true;
            }
            catch (FormatException)
            {
                value = 0.0f;
                return false;
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
                if (node.IsLeaf())
                {
                    return leafNodeFactory.Roof(gameNode);
                }
                else
                {
                    return innerNodeFactory.Roof(gameNode);
                }
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
                if (node.IsLeaf())
                {
                    return leafNodeFactory.GetSize(gameNode);
                }
                else
                {
                    return innerNodeFactory.GetSize(gameNode);
                }
            }
            else
            {
                throw new Exception($"Game object {gameNode.name} does not have a graph node attached to it.");
            }
        }

        /// <summary>
        /// Adjusts the height (y axis) of the given <paramref name="gameNode"/> according
        /// to the InnerNodeHeightMetric.
        ///
        /// Precondition: <paramref name="gameNode"/> must denote an inner node created
        /// by <see cref="innerNodeFactory"/> and must have a <see cref="NodeRef"/>
        /// attached to it.
        /// </summary>
        /// <param name="gameNode">inner node whose height is to be set</param>
        /// <exception cref="Exception">thrown if <paramref name="gameNode"/> has no
        /// <see cref="NodeRef"/> attached to it</exception>
        private void AdjustHeightOfInnerNode(GameObject gameNode)
        {
            if (gameNode.TryGetComponent<NodeRef>(out NodeRef nodeRef))
            {
                Node node = nodeRef.Value;
                float value = GetMetricValue(nodeRef.Value, settings.InnerNodeSettings.HeightMetric);
                innerNodeFactory.SetHeight(gameNode, value);
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
                    leafNodeFactory.SetStyle(gameNode, style);
                }
                else
                {
                    // TODO: for some reason, the material is selected twice. Once here and once
                    // somewhere earlier (I believe in NewBlock somewhere).
                    innerNodeFactory.SetStyle(gameNode, style);
                }
            }
            else
            {
                throw new Exception($"Game object {gameNode.name} does not have a graph node attached to it.");
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
                    if (settings.NodeLayoutSettings.Kind == NodeLayoutKind.Treemap)
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
                        // sqrt(M) * sqrt(M) = M. If were instead using M as width and depth of the
                        // rectangle, the area would be M^2, which would skew the visual impression
                        // in the eye of the beholder. Nodes with larger values of M would have a
                        // disproportionally larger area.

                        // The input to the treemap layout are rectangles with equally sized lengths,
                        // in other words, squares. This determines only the ground area of the input
                        // blocks. The height of the blocks remains the original value of the metric
                        // chosen to determine the height, without any kind of transformation.
                        float widthOfSquare = Mathf.Sqrt(scale.x);
                        Vector3 targetScale = new Vector3(widthOfSquare, scale.y, widthOfSquare) * NodeFactory.Unit;
                        leafNodeFactory.SetSize(gameNode, targetScale);
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
        /// settings, i.e., what the use specified for the width, height, and depth of nodes.
        /// </summary>
        /// <param name="node"></param>
        /// <returns>requested absolute scale in world space</returns>
        private Vector3 GetScale(Node node)
        {
            LeafNodeAttributes attribs = settings.LeafNodeSettings;
            return new Vector3(GetMetricValue(node, attribs.WidthMetric),
                               GetMetricValue(node, attribs.HeightMetric),
                               GetMetricValue(node, attribs.DepthMetric));
        }

        /// <summary>
        /// If <paramref name="metricName"/> is the name of a metric, the corresponding
        /// normalized value for <paramref name="node"/> is returned. If <paramref name="metricName"/>
        /// can be parsed as a number instead, the parsed number is returned.
        /// The result is clamped into [MinimalBlockLength, MaximalBlockLength].
        /// </summary>
        /// <param name="node">node whose metric is to be returned</param>
        /// <param name="metricName">the name of a node metric or a number</param>
        /// <returns>the value of <paramref name="node"/>'s metric <paramref name="metricName"/></returns>
        private float GetMetricValue(Node node, string metricName)
        {
            float result;
            if (TryGetFloat(metricName, out float value))
            {
                result = value;
            }
            else
            {
                result = scaler.GetNormalizedValue(metricName, node);
            }
            return Mathf.Clamp(result,
                        settings.LeafNodeSettings.MinimalBlockLength,
                        settings.LeafNodeSettings.MaximalBlockLength);
        }

        /// <summary>
        /// Adjusts the scale of every node such that the maximal extent of each node is one.
        /// </summary>
        /// <param name="nodeMap">The nodes to scale.</param>
        private static void AdjustScaleBetweenNodeKinds(Dictionary<Node, GameObject> nodeMap)
        {
            Vector3 denominator = Vector3.negativeInfinity;
            IList<KeyValuePair<Node, GameObject>> nodeMapMatchingDomain = nodeMap.ToList();
            denominator = nodeMapMatchingDomain.Aggregate(denominator, (current, pair) => Vector3.Max(current, pair.Value.transform.localScale));
            foreach (KeyValuePair<Node, GameObject> pair in nodeMapMatchingDomain)
            {
                pair.Value.transform.localScale = pair.Value.transform.localScale.DividePairwise(denominator);
            }
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
        /// Adds decoration for the given list of <paramref name="gameNodes"/> with the global settings
        /// for inner node kinds and nodelayout.
        /// </summary>
        /// <param name="gameNodes">a list with gamenode objects</param>
        protected void AddDecorations(ICollection<GameObject> gameNodes)
        {
            AddDecorations(gameNodes, settings.InnerNodeSettings.Kind, settings.NodeLayoutSettings.Kind);
        }

        /// <summary>
        /// Draws the decorations of the given game nodes.
        /// </summary>
        /// <param name="gameNodes">game nodes to be decorated</param>
        /// <param name="innerNodeKinds">the inner node kinds for the gameobject</param>
        /// <param name="nodeLayout">the nodeLayout used for this gameobject</param>
        /// <returns>the game objects added for the decorations; may be an empty collection</returns>
        private void AddDecorations(ICollection<GameObject> gameNodes, InnerNodeKinds innerNodeKinds,
                                    NodeLayoutKind nodeLayout)
        {
            ICollection<GameObject> leafNodes = FindLeafNodes(gameNodes);
            ICollection<GameObject> innerNodes = FindInnerNodes(gameNodes);

            // Add software erosion decorators for all nodes if requested.
            if (settings.ErosionSettings.ShowInnerErosions)
            {
                //FIXME: This should instead check whether each node has non-aggregated metrics available,
                // and use those instead of the aggregated ones, because they are usually more accurate (see MetricImporter).
                ErosionIssues issueDecorator = new ErosionIssues(settings.InnerIssueMap(), innerNodeFactory,
                                                                 scaler, settings.ErosionSettings.ErosionScalingFactor);
                issueDecorator.Add(innerNodes);
            }
            if (settings.ErosionSettings.ShowLeafErosions)
            {
                ErosionIssues issueDecorator = new ErosionIssues(settings.LeafIssueMap(), leafNodeFactory,
                                                                 scaler, settings.ErosionSettings.ErosionScalingFactor * 5);
                issueDecorator.Add(leafNodes);
            }

            // Add text labels for all inner nodes
            if (nodeLayout == NodeLayoutKind.Balloon
                || nodeLayout == NodeLayoutKind.EvoStreets
                || nodeLayout == NodeLayoutKind.CirclePacking)
            {
                AddLabels(innerNodes, innerNodeFactory);
            }

            // Add decorators specific to the shape of inner nodes (circle decorators for circles
            // and donut decorators for donuts).

            switch (innerNodeKinds)
            {
                case InnerNodeKinds.Empty:
                    // do nothing
                    break;
                case InnerNodeKinds.Circles:
                    {
                        // We want to adjust the size and the line width of the circle line created by the CircleFactory.
                        CircleDecorator decorator = new CircleDecorator(innerNodeFactory, Color.white);
                        decorator.Add(innerNodes);
                    }
                    break;
                case InnerNodeKinds.Rectangles:
                    {
                        // We want to adjust the line width of the rectangle line created by the RectangleFactory.
                        RectangleDecorator decorator = new RectangleDecorator(innerNodeFactory, Color.white);
                        decorator.Add(innerNodes);
                    }
                    break;
                case InnerNodeKinds.Donuts:
                    {
                        DonutDecorator decorator = new DonutDecorator(innerNodeFactory, scaler, settings.InnerNodeSettings.InnerDonutMetric,
                                                                      settings.AllInnerNodeIssues().ToArray());
                        // the circle segments and the inner circle for the donut are added as children by Add();
                        // that is why we do not add the result to decorations.
                        decorator.Add(innerNodes);
                    }
                    break;
                case InnerNodeKinds.Cylinders:
                    break;
                case InnerNodeKinds.Blocks:
                    // TODO
                    break;
                default:
                    throw new InvalidOperationException("Unhandled GraphSettings.InnerNodeKinds "
                                                        + $"{settings.InnerNodeSettings.Kind}");
            }
        }

        /// <summary>
        /// Adds the source name as a label to the center of the given game nodes as a child.
        /// </summary>
        /// <param name="gameNodes">game nodes whose source name is to be added</param>
        /// <param name="innerNodeFactory">inner node factory</param>
        /// <returns>the game objects created for the text labels</returns>
        /// <returns>the game objects created for the text labels</returns>
        private static void AddLabels(IEnumerable<GameObject> gameNodes, NodeFactory innerNodeFactory)
        {
            GameObject codeCity = null;
            foreach (GameObject node in gameNodes)
            {
                Node theNode = node.GetComponent<NodeRef>().Value;
                Vector3 size = innerNodeFactory.GetSize(node);
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
