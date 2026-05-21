using SEE.DataModel.DG;
using SEE.GO;
using SEE.Tools.ReflexionAnalysis;
using UnityEngine;

namespace SEE.Game.SceneManipulation
{
    /// <summary>
    /// Adds an edge to a reflexion graph allowing a currently divergent implementation
    /// dependency.
    /// </summary>
    public static class AcceptDivergence
    {
        /// <summary>
        /// Adds a new edge of given <paramref name="edgeType"/> from <paramref name="from"/>
        /// to <paramref name="to"/> to the graph <paramref name="from"/> is contained in
        /// via <see cref="ReflexionGraph.AddToArchitecture(Edge)"/>. Then the edge is
        /// drawn.
        ///
        /// Preconditions: <paramref name="from"/> and <paramref name="to"/> both have
        /// valid <see cref="NodeRef"/>s that are in the same graph and this graph is a
        /// <see cref="ReflexionGraph"/>. Given <paramref name="edgeId"/> must be a unique
        /// edge ID.
        /// </summary>
        /// <param name="from">The game object holding the source of the edge.</param>
        /// <param name="to">The game object holding the target of the edge.</param>
        /// <param name="edgeType">The type of the edge.</param>
        /// <param name="edgeId">The unique ID the edge should have.</param>
        /// <returns>The edge that was created and added to the graph.</returns>
        public static Edge Accept(GameObject from, GameObject to, string edgeType, string edgeId)
        {
            if (from.TryGetNode(out Node fromNode))
            {
                if (to.TryGetNode(out Node toNode))
                {
                    return Accept(fromNode, toNode, edgeType, edgeId);
                }
                else
                {
                    throw new System.Exception($"Game node {toNode.ID} has not graph node attached.\n");
                }
            }
            else
            {
                throw new System.Exception($"Game node {fromNode.ID} has not graph node attached.\n");
            }
        }

        /// <summary>
        /// Adds a new edge of given <paramref name="edgeType"/> from <paramref name="from"/>
        /// to <paramref name="to"/> to the graph <paramref name="from"/> is contained in
        /// via <see cref="ReflexionGraph.AddToArchitecture(Edge)"/>. Then the edge is
        /// drawn.
        ///
        /// Preconditions: <paramref name="from"/> and <paramref name="to"/> are in the
        /// same graph and this graph is a <see cref="ReflexionGraph"/>. If <paramref name="edgeId"/>
        /// is given, it must be a unique edge ID.
        /// </summary>
        /// <param name="from">The source of the edge.</param>
        /// <param name="to">The target of the edge.</param>
        /// <param name="edgeType">The type of the edge.</param>
        /// <param name="edgeId">The unique ID the edge should have; if <c>null</c> or empty
        /// a unique ID will be created.</param>
        /// <returns>The edge that was created and added to the graph.</returns>
        public static Edge Accept(Node from, Node to, string edgeType, string edgeId = null)
        {
            Edge result = new(from, to, edgeType);

            if (!string.IsNullOrEmpty(edgeId))
            {
                result.ID = edgeId;
            }

            if (from.ItsGraph is ReflexionGraph graph)
            {
                graph.AddToArchitecture(result);
            }

            GameEdgeAdder.Draw(result);
            return result;
        }
    }
}