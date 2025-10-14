using SEE.DataModel.DG;
using SEE.Game.City;
using SEE.Layout;
using SEE.Layout.NodeLayouts;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.Game.CityRendering
{
    /// <summary>
    /// Interface of all graph renderers.
    /// </summary>
    public interface IGraphRenderer
    {
        /// <summary>
        /// The default edge layout if none was specified.
        /// </summary>
        const EdgeLayoutKind EdgeLayoutDefault = EdgeLayoutKind.Spline;

        /// <summary>
        /// Creates and returns a new game edge between <paramref name="source"/> and <paramref name="target"/>
        /// based on the current settings. A new graph edge will be added to the underlying graph, too.
        ///
        /// Note: The default edge layout <see cref="EdgeLayoutDefault"/> will be used if no edge layout,
        /// i.e., <see cref="EdgeLayoutKind.None>"/>, was chosen in the settings.
        ///
        /// Precondition: <paramref name="source"/> and <paramref name="target"/> must have a valid
        /// node reference. The corresponding graph nodes must be in the same graph.
        /// </summary>
        /// <param name="source">source of the new edge</param>
        /// <param name="target">target of the new edge</param>
        /// <param name="edgeType">the type of the edge to be created</param>
        /// <returns>The new game object representing the new edge from <paramref name="source"/> to <paramref name="target"/>.</returns>
        /// <exception cref="System.Exception">thrown if <paramref name="source"/> or <paramref name="target"/>
        /// are not contained in any graph or contained in different graphs</exception>
        GameObject DrawEdge(GameObject source, GameObject target, string edgeType);

        /// <summary>
        /// Draws and returns a new game edge <paramref name="edge"/>
        /// based on the current settings.
        ///
        /// Note: The default edge layout <see cref="IGraphRenderer.EdgeLayoutDefault"/> will be used if no edge layout,
        /// i.e., <see cref="EdgeLayoutKind.None>"/>, was chosen in the settings.
        ///
        /// Precondition: <paramref name="source"/> and <paramref name="target"/> must either have a valid
        /// node reference or be <c>null</c>. The corresponding graph nodes must be in the same graph.
        /// </summary>
        /// <param name="edge">the edge to be drawn</param>
        /// <param name="source">GameObject of source of the new edge</param>
        /// <param name="target">GameObject of target of the new edge</param>
        /// <returns>The new game object representing the given edge.</returns>
        GameObject DrawEdge(Edge edge, GameObject source = null, GameObject target = null);

        /// <summary>
        /// Returns an edge layout for the given <paramref name="gameEdges"/>.
        ///
        /// The result is a mapping of the names of the game objects in <paramref name="gameEdges"/>
        /// onto the layout for those edges.
        ///
        /// Precondition: The game objects in <paramref name="gameEdges"/> represent graph edges.
        ///
        /// Note: The default edge layout <see cref="EdgeLayoutDefault"/> will be used if no edge layout,
        /// i.e., <see cref="EdgeLayoutKind.None>"/>, was chosen in the settings.
        /// </summary>
        /// <param name="gameEdges">the edges for which to create a layout</param>
        /// <returns>mapping of the names of the game objects in <paramref name="gameEdges"/> onto
        /// their layout information</returns>
        IDictionary<string, ILayoutEdge<ILayoutNode>> LayoutEdges(ICollection<GameObject> gameEdges);

        /// <summary>
        /// Returns the connecting edges among <paramref name="layoutNodes"/> laid out by the
        /// selected edge layout.
        /// If <paramref name="layoutNodes"/> is null or empty or if no layout was selected
        /// by the user, the empty collection is returned.
        /// </summary>
        /// <param name="layoutNodes">nodes whose connecting edges are to be laid out</param>
        /// <returns>laid out edges</returns>
        public ICollection<LayoutGraphEdge<T>> LayoutEdges<T>(ICollection<T> layoutNodes)
            where T : AbstractLayoutNode;

        /// <summary>
        /// Creates and returns a new game object for representing the given <paramref name="node"/>.
        /// The <paramref name="node"/> is attached to that new game object via a <see cref="NodeRef"/>
        /// component. LOD is added and the resulting node is prepared for interaction.
        /// </summary>
        /// <param name="node">graph node to be represented</param>
        /// <param name="city">the game object representing the city in which to draw this node;
        /// it has the information about how to draw the node and portal of the city</param>
        /// <returns>game object representing given <paramref name="node"/></returns>
        GameObject DrawNode(Node node, GameObject city = null);

        /// <summary>
        /// Adjusts the style of the given <paramref name="gameNode"/> according
        /// to the metric value of the graph node attached to <paramref name="gameNode"/>
        /// chosen to determine style. A style may include color, material, and other visual
        /// properties.
        /// </summary>
        /// <param name="gameNode">a game node representing a leaf or inner graph node</param>
        public void AdjustStyle(GameObject gameNode);

        /// <summary>
        /// Returns the node layout.
        /// </summary>
        /// <returns>node layout</returns>
        NodeLayout GetLayout();

        /// <summary>
        /// Adjusts the scale of the given leaf <paramref name="gameNode"/> according
        /// to the metric values of the <see cref="Node"/> attached to <paramref name="gameNode"/>.
        /// </summary>
        /// <param name="gameNode">the game object whose visual attributes are to be adjusted</param>
        void AdjustScaleOfLeaf(GameObject gameNode);

        /// <summary>
        /// Adjusts the antenna of the given <paramref name="gameNode"/> according
        /// to the metric value of the graph node attached to <paramref name="gameNode"/>
        /// chosen to determine antenna segments.
        /// </summary>
        /// <param name="gameNode">a game node representing a leaf or inner graph node</param>
        void AdjustAntenna(GameObject gameNode);
    }
}
