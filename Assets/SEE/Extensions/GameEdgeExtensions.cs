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
        /// Returns the graph edge represented by this <paramref name="gameEdge"/>.
        ///
        /// Precondition: <paramref name="gameEdge"/> must have an <see cref="EdgeRef"/>
        /// attached to it referring to a valid edge; if not, an exception is raised.
        /// </summary>
        /// <param name="gameEdge">The game object whose <see cref="Edge"/> is requested.</param>
        /// <returns>The corresponding graph edge (will never be null).</returns>
        /// <exception cref="NullReferenceException">Thrown if <paramref name="gameEdge"/> has
        /// no valid <see cref="EdgeRef"/> or <see cref="Edge"/>.</exception>
        /// <remarks>This method is similar to <see cref="TryGetEdge"/>, but throws an exception
        /// if the edge is not found. It is analogous to <see cref="GetNode(GameObject)"/>.</remarks>
        public static Edge GetEdge(this GameObject gameEdge)
        {
            if (gameEdge.TryGetComponent(out EdgeRef edgeRef))
            {
                if (edgeRef != null)
                {
                    if (edgeRef.Value != null)
                    {
                        return edgeRef.Value;
                    }
                    else
                    {
                        throw new NullReferenceException($"Edge referenced by game object {gameEdge.name} is null.");
                    }
                }
                else
                {
                    throw new NullReferenceException($"Edge reference of game object {gameEdge.name} is null.");
                }
            }
            else
            {
                throw new NullReferenceException($"Game object {gameEdge.name} has no {nameof(EdgeRef)}.");
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
