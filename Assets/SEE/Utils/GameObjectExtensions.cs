using System;
using SEE.Game;
using SEE.Game.Operator;
using SEE.GO;
using UnityEngine;

namespace SEE.Utils
{
    /// <summary>
    /// Contains utility extension methods for game objects.
    /// </summary>
    public static class GameObjectExtensions
    {
        /// <summary>
        /// Returns the <see cref="NodeOperator"/> for this <paramref name="gameObject"/>.
        /// If no operator exists yet, it will be added.
        /// If the game object is not a node, an exception will be thrown.
        /// </summary>
        /// <param name="gameObject">The game object whose operator to retrieve.</param>
        /// <returns>The <see cref="NodeOperator"/> responsible for this <paramref name="gameObject"/>.</returns>
        public static NodeOperator NodeOperator(this GameObject gameObject)
        {
            if (gameObject.CompareTag(Tags.Node))
            {
                return gameObject.AddOrGetComponent<NodeOperator>();
            }
            else
            {
                throw new InvalidOperationException($"Cannot get NodeOperator for game object {gameObject.name} because it is not a node.");
            }
        }

        /// <summary>
        /// Returns the <see cref="EdgeOperator"/> for this <paramref name="gameObject"/>.
        /// If no operator exists yet, it will be added.
        /// If the game object is not an edge, an exception will be thrown.
        /// </summary>
        /// <param name="gameObject">The game object whose operator to retrieve.</param>
        /// <returns>The <see cref="EdgeOperator"/> responsible for this <paramref name="gameObject"/>.</returns>
        public static EdgeOperator EdgeOperator(this GameObject gameObject)
        {
            if (gameObject.CompareTag(Tags.Edge))
            {
                return gameObject.AddOrGetComponent<EdgeOperator>();
            }
            else
            {
                throw new InvalidOperationException($"Cannot get EdgeOperator for game object {gameObject.name} because it is not an edge.");
            }
        }

        /// <summary>
        /// Returns the <see cref="GraphElementOperator"/> for this <paramref name="gameObject"/>.
        /// If no operator exists yet, a fitting operator will be added.
        /// If the game object is neither a node nor an edge, an exception will be thrown.
        /// </summary>
        /// <param name="gameObject">The game object whose operator to retrieve.</param>
        /// <returns>The <see cref="GraphElementOperator"/> responsible for this <paramref name="gameObject"/>.</returns>
        public static GraphElementOperator Operator(this GameObject gameObject)
        {
            if (gameObject.TryGetComponent(out GraphElementOperator elementOperator))
            {
                return elementOperator;
            }
            else
            {
                // We may need to add the appropriate operator first.
                if (gameObject.CompareTag(Tags.Node))
                {
                    return gameObject.AddComponent<NodeOperator>();
                }
                else if (gameObject.CompareTag(Tags.Edge))
                {
                    return gameObject.AddComponent<EdgeOperator>();
                }
                else
                {
                    throw new InvalidOperationException("Cannot get GraphElementOperator for game object "
                                                        + $"{gameObject.name} because it is neither a node nor an edge.");
                }
            }
        }
    }
}
