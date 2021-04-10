using SEE.DataModel.DG;
using SEE.GO;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.SEE.Game
{
    /// <summary>
    /// Creates new game objects representing graph edges or deleting these again,
    /// respectively.
    /// </summary>
    class GameEdgeAdder
    {
        /// <summary>
        /// Inverse operation of <see cref="Add(GameObject, Vector3, Vector3, string)"/>.
        /// Removes the given <paramref name="gameEdge"/> from the scene and its associated 
        /// graph edge from its graph. <paramref name="gameEdge"/> is destroyed afterwards.
        /// Precondition: <paramref name="gameEdge"/> must have a valid EdgeRef; otherwise
        /// an exception will be thrown.
        /// </summary>
        /// <param name="gameEdge">game edge to be removed</param>
        public static void Remove(GameObject gameEdge)
        {
            if (gameEdge.TryGetEdge(out Edge edge))
            {
                Graph graph = edge.ItsGraph;
                graph.RemoveEdge(edge);
                GameObject.Destroy(gameEdge);
            }
            else
            {
                throw new Exception($"Edge {gameEdge.name} has no valid edge reference.");
            }
        }
    }
}
