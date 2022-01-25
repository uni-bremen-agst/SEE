using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SEE.Controls.Actions;
using SEE.Controls.Interactables;
using SEE.DataModel;
using SEE.DataModel.DG;
using SEE.Game.City;
using SEE.GO;
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
            GameObject result = innerNodeFactory.NewBlock(SelectStyle(node), renderQueueOffset: node.Level);
            SetGeneralNodeAttributes(node, result);
            AdjustHeightOfInnerNode(result);
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
        /// The exact kind of representation depends upon the leaf-node factory. The node is
        /// scaled according to the WidthMetric, HeightMetric, and DepthMetric of the current settings.
        /// Its style is determined by LeafNodeStyleMetric (linear interpolation of a color gradient).
        /// The <paramref name="node"/> is attached to that new game object via a NodeRef component.
        /// LOD is added and the resulting node is prepared for interaction.
        /// Precondition: <paramref name="node"/> must be a leaf node in the node hierarchy.
        /// </summary>
        /// <param name="node">leaf node</param>
        /// <param name="city">the game object representing the city in which to draw this node;
        /// it has the settings attached and the information about the scale, position, and
        /// portal of the city</param>
        /// <returns>game object representing given <paramref name="node"/></returns>
        public GameObject DrawLeafNode(Node node, GameObject city = null)
        {
            Assert.IsTrue(node.ItsGraph.MaxDepth >= 0, $"Graph of node {node.ID} has negative depth");

            GameObject result = CreateLeafGameNode(node);
            FinishGameNode(result, city);
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
        /// <param name="city">the game object representing the city in which to draw this node;
        /// it has the settings attached and the information about the scale, position, and
        /// portal of the city</param>
        /// <returns>new game object for the inner node</returns>
        public GameObject DrawInnerNode(Node node, GameObject city = null)
        {
            GameObject result = CreateInnerGameNode(node);
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
            Object.Destroy(leafNode);
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
            bool isLeaf = node.IsLeaf();
            NodeFactory nodeFactory = isLeaf ? leafNodeFactory : innerNodeFactory;
            uint numberOfStyles = nodeFactory.NumberOfStyles();
            string colorMetric = isLeaf ? Settings.LeafNodeSettings.ColorMetric
                                        : Settings.InnerNodeSettings.ColorMetric;

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
                    Debug.LogWarning($"Value of color metric {colorMetric} for node {node.ID} is undefined.\n");
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
        private static bool TryGetFloat(string floatString, out float value)
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
                float value = GetMetricValueOfLeaf(nodeRef.Value, Settings.InnerNodeSettings.HeightMetric);
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
                    Vector3 scale = GetScaleOfLeaf(node);

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
                        Vector3 targetScale = new Vector3(widthOfSquare, scale.y, widthOfSquare) * NodeFactory.Unit;
                        leafNodeFactory.SetSize(gameNode, targetScale);
                    }
                    else
                    {
                        leafNodeFactory.SetSize(gameNode, scale);
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
        /// settings, i.e., what the use specified for the width, height, and depth of leaf nodes.
        ///
        /// Precondition: <paramref name="node"/> is a leaf.
        /// </summary>
        /// <param name="node">leaf node whose scale is requested</param>
        /// <returns>requested absolute scale in world space</returns>
        private Vector3 GetScaleOfLeaf(Node node)
        {
            Assert.IsTrue(node.IsLeaf());
            LeafNodeAttributes attribs = Settings.LeafNodeSettings;
            return new Vector3(GetMetricValueOfLeaf(node, attribs.WidthMetric),
                               GetMetricValueOfLeaf(node, attribs.HeightMetric),
                               GetMetricValueOfLeaf(node, attribs.DepthMetric));
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
        private float GetMetricValueOfLeaf(Node node, string metricName)
        {
            return Mathf.Clamp(GetMetricValue(node, metricName),
                        Settings.LeafNodeSettings.MinimalBlockLength,
                        Settings.LeafNodeSettings.MaximalBlockLength);
        }

        /// <summary>
        /// If <paramref name="metricName"/> is the name of a metric, the corresponding
        /// normalized value for <paramref name="node"/> is returned. If <paramref name="metricName"/>
        /// can be parsed as a number instead, the parsed number is returned.
        /// </summary>
        /// <param name="node">node whose metric is to be returned</param>
        /// <param name="metricName">the name of a node metric or a number</param>
        /// <returns>the value of <paramref name="node"/>'s metric <paramref name="metricName"/></returns>
        private float GetMetricValue(Node node, string metricName)
        {
            if (TryGetFloat(metricName, out float value))
            {
                return value;
            }
            else
            {
                return scaler.GetNormalizedValue(metricName, node);
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
            AddDecorations(gameNodes, Settings.InnerNodeSettings.Kind, Settings.NodeLayoutSettings.Kind);
        }

        /// <summary>
        /// Draws the decorations of the given game nodes.
        /// </summary>
        /// <param name="gameNodes">game nodes to be decorated</param>
        /// <param name="innerNodeKinds">the inner node kinds for the gameobject</param>
        /// <param name="nodeLayout">the nodeLayout used for this gameobject</param>
        private void AddDecorations(ICollection<GameObject> gameNodes, InnerNodeKinds innerNodeKinds,
                                    NodeLayoutKind nodeLayout)
        {
            ICollection<GameObject> leafNodes = FindLeafNodes(gameNodes);
            ICollection<GameObject> innerNodes = FindInnerNodes(gameNodes);

            // Add software erosion decorators for all nodes if requested.
            if (Settings.ErosionSettings.ShowInnerErosions)
            {
                //FIXME: This should instead check whether each node has non-aggregated metrics available,
                // and use those instead of the aggregated ones, because they are usually more accurate (see MetricImporter).
                ErosionIssues issueDecorator = new ErosionIssues(Settings.InnerIssueMap(), innerNodeFactory,
                                                                 scaler, Settings.ErosionSettings.ErosionScalingFactor);
                issueDecorator.Add(innerNodes);
            }
            if (Settings.ErosionSettings.ShowLeafErosions)
            {
                ErosionIssues issueDecorator = new ErosionIssues(Settings.LeafIssueMap(), leafNodeFactory,
                                                                 scaler, Settings.ErosionSettings.ErosionScalingFactor * 5);
                issueDecorator.Add(leafNodes);
            }

            // Add text labels for all inner nodes
            if (Settings.InnerNodeSettings.ShowNames)
            {
                AddLabels(innerNodes, innerNodeFactory);
            }

            foreach (GameObject node in leafNodes.Concat(innerNodes))
            {
                AddGeneralDecorations(node);
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
                        DonutDecorator decorator = new DonutDecorator(innerNodeFactory, scaler, Settings.InnerNodeSettings.InnerDonutMetric,
                                                                      Settings.AllInnerNodeIssues().ToArray());
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
                                                        + $"{Settings.InnerNodeSettings.Kind}");
            }
        }

        /// <summary>
        /// Adds decorations applicable to all node kinds to them.
        /// These general decorations currently consist of:
        /// <ul>
        /// <li>Outlines around the node</li>
        /// <li>An AlphaEnforcer ensuring the node always has the correct alpha value</li>
        /// </ul>
        /// </summary>
        protected virtual void AddGeneralDecorations(GameObject node)
        {
            // Add outline around nodes so they can be visually differentiated without needing the same color.
            //TODO: Make color of outline configurable (including total transparency) for inner/leaf node!
            // At the same time, we want to apply transparency to make it easier to tell which nodes are behind
            // other nodes, and to show when a node is being highlighted by making it opaque.
            //TODO: Make transparency value configurable
            Outline.Create(node, Color.black);
            node.AddComponent<AlphaEnforcer>().TargetAlpha = 0.9f;
        }

        /// <summary>
        /// Adds the source name as a label to the center of the given game nodes as a child
        /// for all given <paramref name="gameNodes"/> except for the root (its label would
        /// be too large and is not really neeed anyway).
        /// </summary>
        /// <param name="gameNodes">game nodes whose source name is to be added</param>
        /// <param name="innerNodeFactory">inner node factory</param>
        /// <returns>the game objects created for the text labels</returns>
        private static void AddLabels(IEnumerable<GameObject> gameNodes, NodeFactory innerNodeFactory)
        {
            GameObject codeCity = null;
            foreach (GameObject node in gameNodes)
            {
                Node theNode = node.GetNode();
                if (!theNode.IsRoot())
                {
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
}
