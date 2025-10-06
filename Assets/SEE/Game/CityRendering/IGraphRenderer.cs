using SEE.DataModel.DG;
using SEE.Game.City;
using SEE.Layout;
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
    /// <param name="sourceNode">GameObject of source of the new edge</param>
    /// <param name="targetNode">GameObject of target of the new edge</param>
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
    /// Creates and returns a new game object for representing the given <paramref name="node"/>.
    /// The <paramref name="node"/> is attached to that new game object via a <see cref="NodeRef"/>
    /// component. LOD is added and the resulting node is prepared for interaction.
    /// </summary>
    /// <param name="node">graph node to be represented</param>
    /// <param name="city">the game object representing the city in which to draw this node;
    /// it has the information about how to draw the node and portal of the city</param>
    /// <returns>game object representing given <paramref name="node"/></returns>
    GameObject DrawNode(Node node, GameObject city = null);
  }
}
