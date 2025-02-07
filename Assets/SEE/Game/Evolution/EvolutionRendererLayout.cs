using SEE.DataModel.DG;
using SEE.Game.CityRendering;
using SEE.Layout;
using SEE.Layout.NodeLayouts;
using SEE.Utils;
using Sirenix.Utilities;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SEE.Game.Evolution
{
    /// <summary>
    /// Part of the <see cref="EvolutionRenderer"/> taking care of the graph layouts.
    /// </summary>
    public partial class EvolutionRenderer
    {
        /// <summary>
        /// The layout of <see cref="nextCity"/>. The layout is a mapping of the graph
        /// nodes' IDs onto their <see cref="ILayoutNode"/>.
        /// </summary>
        private Dictionary<string, ILayoutNode> NextLayoutToBeShown => nextCity?.Layout;

        /// <summary>
        /// All pre-computed layouts for the whole graph series. The order of those layouts
        /// corresponds to the order of <see cref="graphs"/>, that is, graphs[i] has
        /// NodeLayouts[i].
        /// </summary>
        private IList<Dictionary<string, ILayoutNode>> NodeLayouts { get; }
             = new List<Dictionary<string, ILayoutNode>>();

        /// <summary>
        /// All pre-computed edge layouts for the whole graph series.  The order of those layouts
        /// corresponds to the order of <see cref="graphs"/>, that is, graphs[i] has
        /// EdgeLayouts[i].
        /// </summary>
        private IList<Dictionary<string, ILayoutEdge<ILayoutNode>>> EdgeLayouts { get; }
            = new List<Dictionary<string, ILayoutEdge<ILayoutNode>>>();

        /// <summary>
        /// The last calculated <see cref="NodeLayout"/> used for <see cref="IncrementalTreeMapLayout"/>.
        /// </summary>
        private NodeLayout oldLayout = null;

        /// <summary>
        /// Creates and saves the node and edge layouts for all given <paramref name="graphs"/>. This will
        /// also create all necessary game nodes and game edges-- even those game nodes and game edges
        /// that are not present in the first graph in <see cref="graphs"/>.
        /// </summary>
        private void CalculateAllGraphLayouts(IList<Graph> graphs)
        {
            // Determines the layouts of all loaded graphs upfront.
            Performance p = Performance.Begin($"Layouting all {graphs.Count} graphs");
            graphs.ForEach(CalculateLayout);
            objectManager.Clear();
            p.End(true);
        }

        /// <summary>
        /// Calculates the node and edge layout data for <paramref name="graph"/> using the <see cref="graphRenderer"/>.
        /// All the game objects created for the nodes and edges of <paramref name="graph"/> will
        /// be created by the <see cref="objectManager"/>, thus, be available for later use. The layout
        /// is not actually applied.
        ///
        /// The edge layout will be added to <see cref="EdgeLayouts"/>. The node layout will
        /// be added to <see cref="NodeLayouts"/>.
        ///
        /// Note: This method assumes that it is called in the order of <see cref="graphs"/>,
        /// that is, the i'th call is assumed to calculate the edge layout for
        /// <paramref name="graph"/>[i].
        /// </summary>
        /// <param name="graph">graph for which the layout is to be calculated</param>
        /// <returns>the node layout for all nodes in <paramref name="graph"/></returns>
        private void CalculateLayout(Graph graph)
        {
            // The following code assumes that a leaf node remains a leaf across all
            // graphs of the graph series and an inner node remains an inner node.
            // This may not necessarily be true. For instance, an empty directory could
            // get subdirectories in the course of the evolution.

            // Collecting all game objects corresponding to nodes of the given graph.
            // If the node existed in a previous graph, we will re-use its corresponding
            // game object created earlier.
            List<GameObject> gameObjects = new();

            // The layout to be applied.
            NodeLayout nodeLayout = Renderer.GetLayout();

            // Gather all nodes for the layout.
            foreach (Node node in graph.Nodes())
            {
                // All layouts (flat and hierarchical ones) must be able to handle leaves;
                // hence, leaves can be added at any rate. For a hierarchical layout, we
                // need to add the game objects for inner nodes, too. To put it differently,
                // inner nodes are added only if we apply a hierarchical layout.
                objectManager.GetNode(node, out GameObject gameNode);
                // Now after having attached the new node to the game object,
                // we must adjust the scale of it according to the newly attached node so
                // that the layouter has these. We need to adjust the scale only for leaves,
                // however, because the layouter will select the scale for inner nodes.
                if (node.IsLeaf())
                {
                    Renderer.AdjustScaleOfLeaf(gameNode);
                }
                gameObjects.Add(gameNode);
            }

            // Since incremental layouts must know the layout of the last revision
            // but are also bound to the function calls of NodeLayout
            // we must hand over this argument here separately
            if (nodeLayout is IIncrementalNodeLayout iNodeLayout && oldLayout is IIncrementalNodeLayout iOldLayout)
            {
                iNodeLayout.OldLayout = iOldLayout;
            }

            // Calculate and apply the node layout.
            ICollection<LayoutGraphNode> layoutNodes = GraphRenderer.ToAbstractLayoutNodes(gameObjects);
            NodeLayout.Apply(nodeLayout.Layout(layoutNodes, gameObject.transform.position,
                                               new Vector2(gameObject.transform.lossyScale.x, gameObject.transform.lossyScale.z)));
            oldLayout = nodeLayout;

            if (edgesAreDrawn)
            {
                IList<LayoutGraphEdge<LayoutGraphNode>> layoutEdges = Renderer.LayoutEdges(layoutNodes).ToList();
                Dictionary<string, ILayoutEdge<ILayoutNode>> edgeLayout = new(layoutEdges.Count);
                foreach (LayoutGraphEdge<LayoutGraphNode> le in layoutEdges)
                {
                    edgeLayout.Add(le.ItsEdge.ID, le);
                }
                EdgeLayouts.Add(edgeLayout);
            }
            NodeLayouts.Add(ToNodeIDLayout(layoutNodes.ToList<ILayoutNode>()));

            // Note: The game objects for leaf nodes are already properly scaled by the call to
            // objectManager.GetNode() above. Yet, inner nodes are generally not scaled by
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

        /// <summary>
        /// Returns true and a <see cref="LaidOutGraph"/> if there is one for <see cref="CurrentGraphIndex"/>
        /// in <see cref="graphs"/>.
        /// </summary>
        /// <param name="laidOutGraph">the <see cref="LaidOutGraph"/> at the <see cref="CurrentGraphIndex"/>
        /// or undefined if there is none (that is, defined only if this method returns true)</param>
        /// <returns>true if there is graph at <see cref="CurrentGraphIndex"/></returns>
        private bool HasCurrentLaidOutGraph(out LaidOutGraph laidOutGraph)
        {
            return HasLaidOutGraph(CurrentGraphIndex, out laidOutGraph);
        }

        /// <summary>
        /// Returns true and a LaidOutGraph if there is a LaidOutGraph for the given graph index.
        /// </summary>
        /// <param name="index">index of the requested graph</param>
        /// <param name="laidOutGraph">the resulting graph with given index; defined only if this method returns true</param>
        /// <returns>true iff there is a graph at the given index</returns>
        private bool HasLaidOutGraph(int index, out LaidOutGraph laidOutGraph)
        {
            laidOutGraph = null;
            Graph graph = graphs[index];
            if (graph == null)
            {
                Debug.LogError($"There is no graph available for graph with index {index}.\n");
                return false;
            }
            Dictionary<string, ILayoutNode> nodeLayout = NodeLayouts[index];

            if (edgesAreDrawn)
            {
                Dictionary<string, ILayoutEdge<ILayoutNode>> edgeLayout = EdgeLayouts[index];
                laidOutGraph = new LaidOutGraph(graph, nodeLayout, edgeLayout);
            }
            else
            {
                laidOutGraph = new LaidOutGraph(graph, nodeLayout, null);
            }
            return true;
        }
    }
}
