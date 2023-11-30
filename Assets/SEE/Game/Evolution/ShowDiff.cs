using SEE.DataModel.DG;
using SEE.Game.CityRendering;
using SEE.Layout;
using SEE.Utils;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace SEE.Game.Evolution{
public class ShowDiff : MonoBehaviour, IGraphRenderer
{


        /// <summary>
        /// Set of added nodes from the current to the next graph.
        /// They are contained in the next graph.
        /// </summary>
        private ISet<Node> addedNodes;
        /// <summary>
        /// Set of removed nodes from the current to the next graph.
        /// They are contained in the current graph.
        /// </summary>
        private ISet<Node> removedNodes;
        /// <summary>
        /// Set of changed nodes from the current to the next graph.
        /// They are contained in the next graph (and logically also
        /// in the current graph, that is, there is a node in the
        /// current graph that has the same ID).
        /// </summary>
        private ISet<Node> changedNodes;
        /// <summary>
        /// Set of equal nodes (i.e., nodes without any changes) from
        /// the current to the next graph.
        /// They are contained in the next graph (and logically also
        /// in the current graph, that is, there is a node in the
        /// current graph that has the same ID).
        /// </summary>
        private ISet<Node> equalNodes;

        /// <summary>
        /// Set of added edges from the current to the next graph.
        /// They are contained in the next graph.
        /// </summary>
        private ISet<Edge> addedEdges;
        /// <summary>
        /// Set of removed edges from the current to the next graph.
        /// They are contained in the current graph.
        /// </summary>
        private ISet<Edge> removedEdges;
        /// <summary>
        /// Set of changed edges from the current to the next graph.
        /// They are contained in the next graph (and logically also
        /// in the current graph, that is, there is an edge in the
        /// current graph that has the same ID).
        /// </summary>
        private ISet<Edge> changedEdges;
        /// <summary>
        /// Set of equal edges (i.e., edges without any changes) from
        /// the current to the next graph.
        /// They are contained in the next graph (and logically also
        /// in the current graph, that is, there is an edge in the
        /// current graph that has the same ID).
        /// </summary>
        private ISet<Edge> equalEdges;

        /// <summary>
        /// The city (graph + layout) currently shown.
        /// </summary>
        private LaidOutGraph currentCity;  // not serialized by Unity

        /// <summary>
        /// Allows the comparison of two instances of <see cref="Node"/> from different graphs.
        /// </summary>
        private static readonly NodeEqualityComparer nodeEqualityComparer = new();

        /// <summary>
        /// Allows the comparison of two instances of <see cref="Edge"/> from different graphs.
        /// </summary>
        private static readonly EdgeEqualityComparer edgeEqualityComparer = new();

        /// <summary>
        /// The manager of the game objects created for the city.
        /// This attribute will be set in the setter of the attribute CityEvolution because it
        /// depends upon the graphRenderer, which in turn depends upon the city, which is set by
        /// this setter.
        /// </summary>
        private ObjectManager objectManager;


        /// <summary>
        /// Renders the animation from <paramref name="current"/> to <paramref name="next"/>.
        /// </summary>
        /// <param name="current">the graph currently shown that is to be migrated into the next graph; may be null</param>
        /// <param name="next">the new graph to be shown, in which to migrate the current graph; must not be null</param>
        private void RenderGraph(LaidOutGraph current, LaidOutGraph next)
        {
            next.AssertNotNull("next");

            Graph oldGraph = current?.Graph;
            Graph newGraph = next?.Graph;

            // Node comparison.
            newGraph.Diff(oldGraph,
                          g => g.Nodes(),
                          (g, id) => g.GetNode(id),
                          GraphExtensions.AttributeDiff(newGraph, oldGraph),
                          nodeEqualityComparer,
                          out addedNodes,
                          out removedNodes,
                          out changedNodes,
                          out equalNodes);

            // Edge comparison.
            newGraph.Diff(oldGraph,
                          g => g.Edges(),
                          (g, id) => g.GetEdge(id),
                          GraphExtensions.AttributeDiff(newGraph, oldGraph),
                          edgeEqualityComparer,
                          out addedEdges,
                          out removedEdges,
                          out changedEdges,
                          out equalEdges);

            OnlyShowAdded(next);
        }

        private void OnlyShowAdded(LaidOutGraph next)
        {

        }

        /// <summary>
        /// Displays the given graph instantly if all animations are finished. This is
        /// called when we jump directly to a specific graph in the graph series: when
        /// we start the visualization of the graph evolution initially and when the user
        /// selects a specific graph revision.
        /// The graph is drawn from scratch.
        /// </summary>
        /// <param name="graph">graph to be drawn initially</param>
        private void DisplayGraphAsNew(LaidOutGraph graph)
        {
            graph.AssertNotNull("graph");

            // The upfront calculation of the node layout for all graphs has filled
            // objectManager with game objects for those nodes. Likewise, when we jump
            // to a graph directly in the version history, the nodes of its predecessors
            // may still be contained in the scene and objectManager. We need to clean up
            // first.
            objectManager?.Clear();
            RenderGraph(currentCity, graph);
        }



        /// <summary>
        /// Yields a graph renderer that can draw code cities for the each graph of
        /// the graph series.
        /// </summary>
        /// <remarks>Implements <see cref="AbstractSEECity.Renderer"/>.</remarks>
        public GraphRenderer Renderer { get; private set; }


        public GameObject DrawEdge(GameObject source, GameObject target, string edgeType)
        {
            return Renderer.DrawEdge(source, target, edgeType);
        }

        public GameObject DrawEdge(Edge edge, GameObject source = null, GameObject target = null)
        {
            return Renderer.DrawEdge(edge);
        }

        public GameObject DrawNode(Node node, GameObject city = null)
        {
            return Renderer.DrawNode(node);
        }

        public IDictionary<string, ILayoutEdge<ILayoutNode>> LayoutEdges(ICollection<GameObject> gameEdges)
        {
            return Renderer.LayoutEdges(gameEdges);
        }
    }
}

