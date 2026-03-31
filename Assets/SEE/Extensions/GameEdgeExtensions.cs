using SEE.DataModel.DG;
using SEE.Game;
using SEE.Game.Operator;
using SEE.GraphElementRefs;
using System;
using UnityEngine;

namespace SEE.Extensions
{
    /// <summary>
    /// Extension methods for game edges. A game edge is a <see cref="GameObject"/>
    /// representing a <see cref="SEE.DataModel.DG.Edge"/>.
    /// </summary>
    internal static class GameEdgeExtensions
    {
        /// <summary>
        /// Returns true if <paramref name="gameEdge"/> has an <see cref="EdgeRef"/>
        /// component attached to it whose edge is not null.
        /// </summary>
        /// <param name="gameEdge">The game object whose EdgeRef is checked.</param>
        /// <returns>True if <paramref name="gameEdge"/> has an <see cref="EdgeRef"/>
        /// component attached to it whose edge is not null.</returns>
        public static bool HasEdgeRef(this GameObject gameEdge)
        {
            return gameEdge.TryGetComponent(out EdgeRef edgeRef) && edgeRef.Value != null;
        }

        /// <summary>
        /// Returns true if <paramref name="gameEdge"/> is tagged by <see cref="Tags.Edge"/>.
        /// </summary>
        /// <param name="gameEdge">The game object to check.</param>
        /// <returns>True if <paramref name="gameEdge"/> is tagged by <see cref="Tags.Edge"/>.</returns>
        public static bool IsEdge(this GameObject gameEdge)
        {
            return gameEdge.CompareTag(Tags.Edge);
        }

        /// <summary>
        /// Returns true if <paramref name="gameEdge"/> has an <see cref="EdgeRef"/>
        /// component attached to it that is not null.
        /// </summary>
        /// <param name="gameEdge">The game object whose EdgeRef is checked.</param>
        /// <param name="edge">The edge referenced by the attached EdgeRef; defined only if this method
        /// returns true.</param>
        /// <returns>True if <paramref name="gameEdge"/> has an <see cref="EdgeRef"/>
        /// component attached to it that is not null.</returns>
        public static bool TryGetEdge(this GameObject gameEdge, out Edge edge)
        {
            edge = null;
            if (gameEdge.TryGetComponent(out EdgeRef edgeRef))
            {
                edge = edgeRef.Value;
            }
            return edge != null;
        }

        /// <summary>
        /// Returns the source node of the given <paramref name="gameEdge"/>.
        /// The <paramref name="gameEdge"/> is assumed to represent an edge, that is,
        /// is tagged by <see cref="Tags.Edge"/> and has an <see cref="EdgeRef"/>.
        /// If this is not the case, an exception is thrown. If the source node
        /// of this edge does not exist, an exception is thrown, too.
        /// </summary>
        /// <param name="gameEdge">Game object representing an edge.</param>
        /// <returns>The game object representing the source of this edge.</returns>
        public static GameObject Source(this GameObject gameEdge)
        {
            if (gameEdge.CompareTag(Tags.Edge) && gameEdge.TryGetComponent(out EdgeRef edgeRef))
            {
                return GraphElementIDMap.Find(edgeRef.SourceNodeID, mustFindElement: true);
            }
            else
            {
                throw new Exception($"Game object {gameEdge.name} is not an edge. It has no source node.");
            }
        }

        /// <summary>
        /// Returns the target node of the given <paramref name="gameEdge"/>.
        /// The <paramref name="gameEdge"/> is assumed to represent an edge, that is,
        /// is tagged by <see cref="Tags.Edge"/> and has an <see cref="EdgeRef"/>.
        /// If this is not the case, an exception is thrown. If the target node
        /// of this edge does not exist, an exception is thrown, too.
        /// </summary>
        /// <param name="gameEdge">Game object representing an edge.</param>
        /// <returns>The game object representing the target of this edge.</returns>
        public static GameObject Target(this GameObject gameEdge)
        {
            if (gameEdge.CompareTag(Tags.Edge) && gameEdge.TryGetComponent(out EdgeRef edgeRef))
            {
                return GraphElementIDMap.Find(edgeRef.SourceNodeID, mustFindElement: true);
            }
            else
            {
                throw new Exception($"Game object {gameEdge.name} is not an edge. It has no target node.");
            }
        }

        /// <summary>
        /// Returns the <see cref="EdgeOperator"/> for this <paramref name="gameEdge"/>.
        /// If no operator exists yet, it will be added.
        /// If the game object is not an edge, an exception will be thrown.
        /// </summary>
        /// <param name="gameEdge">The game object whose operator to retrieve.</param>
        /// <returns>The <see cref="EdgeOperator"/> responsible for this <paramref name="gameEdge"/>.</returns>
        public static EdgeOperator EdgeOperator(this GameObject gameEdge)
        {
            if (gameEdge.CompareTag(Tags.Edge))
            {
                return gameEdge.AddOrGetComponent<EdgeOperator>();
            }
            else
            {
                throw new InvalidOperationException($"Cannot get {nameof(EdgeOperator)} for game object {gameEdge.name} because it is not an edge.");
            }
        }
    }
}
