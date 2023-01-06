using SEE.DataModel.DG;
using SEE.Layout;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.Game
{
    /// <summary>
    /// Interface of all graph renderers.
    /// </summary>
    public interface IGraphRenderer
    {
        /// <summary>
        /// Creates and returns a new game edge between <paramref name="from"/> and <paramref name="to"/>
        /// based on the current settings. A new graph edge will be added to the underlying graph, too.
        ///
        /// Note: A default edge layout will be used if no edge layout was chosen.
        ///
        /// Precondition: <paramref name="from"/> and <paramref name="to"/> must have a valid
        /// node reference. The corresponding graph nodes must be in the same graph.
        /// </summary>
        /// <param name="source">source of the new edge</param>
        /// <param name="target">target of the new edge</param>
        /// <param name="edgeType">the type of the edge to be created</param>
        /// <param name="existingEdge">If non-null, we'll use this as the edge in the underlying graph
        /// instead of creating a new one</param>
        /// <exception cref="Exception">thrown if <paramref name="from"/> or <paramref name="to"/>
        /// are not contained in any graph or contained in different graphs</exception>
        GameObject DrawEdge(GameObject source, GameObject target, string edgeType, Edge existingEdge);

        /// <summary>
        /// Returns an edge layout for the given <paramref name="gameEdges"/>.
        ///
        /// The result is a mapping of the names of the game objects in <paramref name="gameEdges"/>
        /// onto the layout for those edges.
        ///
        /// Precondition: The game objects in <paramref name="gameEdges"/> represent graph edges.
        /// </summary>
        /// <param name="gameEdges">the edges for which to create a layout</param>
        /// <returns>mapping of the names of the game objects in <paramref name="gameEdges"/> onto
        /// their layout information</returns>
        public IDictionary<string, ILayoutEdge<ILayoutNode>> LayoutEdges(ICollection<GameObject> gameEdges);

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