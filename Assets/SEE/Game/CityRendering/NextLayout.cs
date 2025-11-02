using SEE.DataModel.DG;
using SEE.Layout;
using SEE.Layout.NodeLayouts;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SEE.Game.CityRendering
{
    /// <summary>
    /// Calculates the next layout for a given graph based on an already rendered code city.
    /// </summary>
    internal static class NextLayout
    {
        /// <summary>
        /// Creates new game nodes and edges for all nodes and edges in <paramref name="graph"/>.
        /// Calculates and applies the layout for <paramref name="graph"/> using the
        /// given <paramref name="renderer"/>.
        /// </summary>
        /// <param name="graph">graph for which the laid out nodes and edges are to be calculated</param>
        /// <param name="getNode">method to be called for getting a (new) <see cref="GameObject"/>
        /// for a given <see cref="Node"/>; it may yield an existing game node for a graph node
        /// if one was created before or a new one otherwise.</param>
        /// <param name="renderer">the graph renderer to obtain the layouts from</param>
        /// <param name="edgesAreDrawn">whether an edge layout is requested; if false,
        /// <paramref name="newEdgeLayout"/> will be null</param>
        /// <param name="city">the game object representing the code city; created game nodes
        /// and edges will be descendants of it</param>
        /// <param name="newNodelayout">the resulting node layout as a mapping of node Ids onto the layout information</param>
        /// <param name="newEdgeLayout">the resulting edge layout as a mapping of edge Ids onto
        /// the layout information if <paramref name="edgesAreDrawn"/> is true,
        /// otherwise null</param>
        /// <param name="oldNodeLayout">in case an incremental layout was used by the <paramref name="renderer"/>,
        /// this parameter is expected to contain the previous layout that should be taken into account
        /// by the incremental layout; the resulting layout applied to the nodes of <paramref name="graph"/>
        /// will be stored in this parameter for later use (independent of whether or not an incremental layout
        /// was used).</param>
        public static void Calculate
            (Graph graph,
             Func<Node, GameObject> getNode,
             IGraphRenderer renderer,
             bool edgesAreDrawn,
             GameObject city,
             out Dictionary<string, ILayoutNode> newNodelayout,
             out Dictionary<string, ILayoutEdge<ILayoutNode>> newEdgeLayout,
             ref NodeLayout oldNodeLayout)
        {
            // The following code assumes that a leaf node remains a leaf across all
            // graphs of the graph series and an inner node remains an inner node.
            // This may not necessarily be true. For instance, an empty directory could
            // get subdirectories in the course of the evolution.

            // Collecting all game objects corresponding to nodes of the given graph.
            List<GameObject> gameObjects = new();

            // Gather all nodes for the layout.
            foreach (Node node in graph.Nodes())
            {
                GameObject gameNode = getNode(node);
                // Now after having attached the new node to the game object,
                // we must adjust the scale of it according to the newly attached node so
                // that the layouter has these. We need to adjust the scale only for leaves,
                // however, because the layouter will select the scale for inner nodes.
                if (node.IsLeaf())
                {
                    renderer.AdjustScaleOfLeaf(gameNode);
                }
                gameObjects.Add(gameNode);
            }

            // The layout to be applied.
            NodeLayout nodeLayout = renderer.GetLayout();

            // Since incremental layouts must know the layout of the last revision
            // but are also bound to the function calls of NodeLayout,
            // we must hand over this argument here separately.
            if (nodeLayout is IIncrementalNodeLayout iNodeLayout
                && oldNodeLayout is IIncrementalNodeLayout iOldLayout)
            {
                iNodeLayout.OldLayout = iOldLayout;
            }

            // Calculate and apply the node layout.
            Debug.Log($"Calculating node layout {nodeLayout.GetType().Name} for {gameObjects.Count} nodes.\n");

            // ICollection<LayoutGraphNode> layoutNodes = ToLayoutNodes(gameObjects);
            ICollection<LayoutGraphNode> layoutNodes
                = GraphRenderer.ToLayoutNodes<LayoutGraphNode>
                    (gameObjects,
                     (Node node, GameObject gameNode) =>
                         new(node)
                         {
                            // We must transfer the scale from gameNode to layoutNode.
                            // Rotation and CenterPosition are all zero. They will be computed by the layout,
                            AbsoluteScale = gameNode.transform.lossyScale
                         }).Values;

            NodeLayout.Apply(nodeLayout.Create(layoutNodes, city.transform.position,
                                               new Vector2(city.transform.lossyScale.x,
                                                           city.transform.lossyScale.z)));
            oldNodeLayout = nodeLayout;

            if (edgesAreDrawn)
            {
                IList<LayoutGraphEdge<LayoutGraphNode>> layoutEdges = renderer.LayoutEdges(layoutNodes).ToList();
                newEdgeLayout = new(layoutEdges.Count);
                foreach (LayoutGraphEdge<LayoutGraphNode> le in layoutEdges)
                {
                    newEdgeLayout.Add(le.ItsEdge.ID, le);
                }
            }
            else
            {
                newEdgeLayout = null;
            }

            newNodelayout = ToNodeIDLayout(layoutNodes.ToList<ILayoutNode>());

            // Note: The game objects for leaf nodes are already properly scaled by the calls to
            // GetNode() and AdjustScaleOfLeaf() above. Yet, inner nodes are generally not scaled by
            // the layout and there may be layouts that may shrink leaf nodes. For instance,
            // TreeMap shrinks leaves so that they fit into the available space.
            // Anyhow, we do not need to apply the layout already now. That can be deferred
            // to the point in time when the city is actually visualized. Here, we just calculate
            // the layout for every graph in the graph series for later use.
        }

        /// <summary>
        /// Returns a mapping of graph-node IDs onto their corresponding <paramref name="layoutNodes"/>.
        /// </summary>
        /// <param name="layoutNodes">collection of layout nodes to be mapped</param>
        /// <returns>mapping indexed by the IDs of the nodes corresponding to the layout nodes</returns>
        private static Dictionary<string, T> ToNodeIDLayout<T>(ICollection<T> layoutNodes) where T : ILayoutNode
        {
            return layoutNodes.ToDictionary(layoutNode => layoutNode.ID);
        }
    }
}
