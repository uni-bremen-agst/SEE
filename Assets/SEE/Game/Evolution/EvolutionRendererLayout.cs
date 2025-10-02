using SEE.DataModel.DG;
using SEE.Game.CityRendering;
using SEE.Layout;
using SEE.Layout.NodeLayouts;
using SEE.Utils;
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
        /// The last calculated <see cref="NodeLayout"/> (needed for incremental layouts).
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
            foreach (Graph graph in graphs)
            {
                NextLayout.Calculate(graph, GetNode, Renderer, edgesAreDrawn, gameObject,
                                     out Dictionary<string, ILayoutNode> layout,
                                     out Dictionary<string, ILayoutEdge<ILayoutNode>> edgeLayout,
                                     ref oldLayout);

                NodeLayouts.Add(layout);
                if (edgesAreDrawn)
                {
                    EdgeLayouts.Add(edgeLayout);
                }
            }
            objectManager.Clear();
            p.End(true);

            GameObject GetNode(Node node)
            {
                objectManager.GetNode(node, out GameObject gameNode);
                return gameNode;
            }
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
