using System;
using SEE.DataModel.DG;
using SEE.Game.City;
using SEE.GO;
using UnityEngine;

namespace SEE.Game.SceneManipulation
{
    /// <summary>
    /// Creates new game objects representing graph edges or deletes these again,
    /// respectively.
    /// </summary>
    public class GameEdgeAdder
    {
        /// <summary>
        /// Creates and returns a new edge from <paramref name="source"/> to <paramref name="target"/>.
        /// A new graph edge will be added to the underlying graph as well and attached as an
        /// <see cref="EdgeRef"/> to that edge. The line of this edge is created by the
        /// <see cref="GraphRenderer"/> for the code city this edge belongs to. If <paramref name="edgeID"/>
        /// is neither null nor empty, that ID will be used for the name of the edge and the ID of the
        /// underlying graph edge. Otherwise the renderer will create a random unique ID for it.
        ///
        /// Precondition:
        /// (1) <paramref name="source"/> and <paramref name="target"/> must have a valid node reference
        ///      to nodes in the same graph and they both belong to the same code city.
        /// (2) if <paramref name="edgeID"/> is neither null nor empty, no edge with the same ID
        ///     may exist in the underlying graph.
        /// </summary>
        /// <param name="source">source of the edge</param>
        /// <param name="target">target of the edge</param>
        /// <param name="edgeType">the type of the edge to be added</param>
        /// <returns>the new game object representing the edge</returns>
        /// <exception cref="Exception">thrown if the edge could not be created; the message of the exception
        /// provides more details why</exception>
        public static GameObject Add(GameObject source, GameObject target, string edgeType)
        {
            AbstractSEECity city = source.ContainingCity();
            if (city == null)
            {
                throw new Exception($"The code city for the new edge from {source.name} to {target.name} cannot be determined.\n");
            }
            return city.Renderer.DrawEdge(source, target, edgeType);
        }

        /// <summary>
        /// Simply (re)draws an edge.
        /// </summary>
        /// <param name="edge">the edge that should be (re)drawn</param>
        /// <returns>the GameObject of the drawn edge</returns>
        public static GameObject Draw(Edge edge)
        {
            GameObject source = GraphElementIDMap.Find(edge.Source.ID);
            AbstractSEECity city = source.ContainingCity();
            if (city == null)
            {
                throw new Exception($"The code city for the edge {edge.Source.ID} cannot be determined.\n");
            }
            return city.Renderer.DrawEdge(edge, source: source);
        }

        /// <summary>
        /// Inverse operation of <see cref="Add(GameObject, GameObject, string)"/>.
        /// Removes the given <paramref name="gameEdge"/> from the scene and its associated
        /// graph edge from its graph.
        ///
        /// Note: <paramref name="gameEdge"/> is not actually destroyed.
        ///
        /// Precondition: <paramref name="gameEdge"/> must have a valid EdgeRef; otherwise
        /// an exception will be thrown.
        /// </summary>
        /// <param name="gameEdge">game edge to be removed</param>
        public static void Remove(GameObject gameEdge)
        {
            if (gameEdge.TryGetEdge(out Edge edge))
            {
                Graph graph = edge.ItsGraph;
                if (graph != null)
                {
                    graph.RemoveEdge(edge);
                }
                else
                {
                    throw new Exception($"Edge {gameEdge.name} to be removed is not contained in a graph.");
                }
            }
            else
            {
                throw new Exception($"Edge {gameEdge.name} to be removed has no valid edge reference.");
            }
        }
    }
}
