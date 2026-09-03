using SEE.Game;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SEE.Extensions
{
    /// <summary>
    /// Extension methods for code-city game objects. A code-city game object
    /// is a <see cref="UnityEngine.GameObject"/> under which game nodes and
    /// game edges are placed to render a code city.
    /// </summary>
    internal static class CodeCityExtensions
    {
        /// <summary>
        /// Returns true if a code city was drawn for this <paramref name="codeCity"/>.
        /// A code city is assumed to be drawn in there is at least one immediate child
        /// of this <paramref name="codeCity"/> that represents a graph node, i.e., has a <see cref="NodeRef"/>
        /// (checked by predicate <see cref="IsNode(GameObject)"/>.
        ///
        /// This predicate can be queried for game objects representing a code city,
        /// that is, game objects that have an <see cref="AbstractSEECity"/> attached to
        /// them.
        /// </summary>
        /// <param name="codeCity">The code city to checked.</param>
        /// <returns>True if a code city was drawn.</returns>
        /// <remarks>Applicable to a game object representing a code city.</remarks>
        public static bool IsCodeCityDrawn(this GameObject codeCity)
        {
            return codeCity.transform.Cast<Transform>().Any(child => child.gameObject.IsNode());
        }

        /// <summary>
        /// Returns true if a code city was drawn for this <paramref name="codeCity"/> and is active.
        /// A code city is assumed to be drawn in there is at least one immediate child
        /// of this game object that represents a graph node, i.e., has a <see cref="NodeRef"/>
        /// (checked by predicate <see cref="IsNode(GameObject)"/>.
        ///
        /// This predicate can be queried for game objects representing a code city,
        /// that is, game objects that have an <see cref="AbstractSEECity"/> attached to
        /// them.
        /// </summary>
        /// <param name="codeCity">The code city to checked.</param>
        /// <returns>True if a code city was drawn and is active.</returns>
        /// <remarks>Applicable to a game object representing a code city.</remarks>
        public static bool IsCodeCityDrawnAndActive(this GameObject codeCity)
        {
            return codeCity.transform.Cast<Transform>().Any(child => child.gameObject.IsNode()
                    && child.gameObject.activeInHierarchy);
        }

        /// <summary>
        /// Returns true if there is any edge in the given <paramref name="codeCity"/>.
        /// </summary>
        /// <param name="codeCity">The code city to checked.</param>
        /// <returns>True if there is any edge in the given <paramref name="codeCity"/>.</returns>
        /// <remarks>Applicable to a game object representing a code city.</remarks>
        public static bool CodeCityHasAnyEdges(this GameObject codeCity)
        {
            // Edges are immediate children of the code-city game object.
            return codeCity.transform.Cast<Transform>().Any(child => child.gameObject.IsEdge()
                     && child.gameObject.activeInHierarchy);
        }

        /// <summary>
        /// Returns first child of <paramref name="codeCity"/> tagged by <see cref="Tags.Node"/>
        /// or null if none can be found.
        /// </summary>
        /// <param name="codeCity">Object representing a code city (tagged by <see cref="Tags.CodeCity"/>).</param>
        /// <returns>Game object representing the root of the graph or null if there is none.</returns>
        /// <remarks>If <paramref name="codeCity"/> is a node representing a code city,
        /// the first child tagged as <see cref="Tags.Node"/> is considered the root of the graph.</remarks>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="codeCity"/> is null.
        /// </exception>
        /// <remarks>Applicable to a game object representing a code city.</remarks>
        public static GameObject GetCityRootNode(this GameObject codeCity)
        {
            if (codeCity == null)
            {
                throw new ArgumentNullException(nameof(codeCity));
            }
            foreach (Transform child in codeCity.transform)
            {
                if (child.CompareTag(Tags.Node))
                {
                    return child.transform.gameObject;
                }
            }
            return null;
        }

        /// <summary>
        /// Returns all game objects tagged as <see cref="Tags.Edge"/> that are descendants
        /// of <paramref name="codeCity"/>.
        /// </summary>
        /// <param name="codeCity">Root game object to be traversed.</param>
        /// <returns>All game objects tagged as <see cref="Tags.Edge"/>.</returns>
        /// <remarks>Applicable to a game object representing a code city.</remarks>
        internal static IEnumerable<GameObject> AllEdges(this GameObject codeCity)
        {
            return codeCity.AllDescendants(Tags.Edge);
        }

        /// <summary>
        /// Returns all transitive children of <paramref name="gameObject"/> tagged by
        /// given <paramref name="tag"/> (including <paramref name="gameObject"/> itself).
        /// </summary>
        /// <param name="gameObject">The game object whose children are requested.</param>
        /// <param name="tag">The tag the descendants must have.</param>
        /// <returns>All transitive children with <paramref name="tag"/>.</returns>
        /// <remarks>Although this method primarily intended for code cities, it is
        /// also applicable to a game node.</remarks>
        public static List<GameObject> AllDescendants(this GameObject gameObject, string tag)
        {
            List<GameObject> result = new();
            if (gameObject.CompareTag(tag))
            {
                result.Add(gameObject);
            }

            foreach (Transform child in gameObject.transform)
            {
                result.AddRange(child.gameObject.AllDescendants(tag));
            }

            return result;
        }

        /// <summary>
        /// Applies <paramref name="action"/> to all (transitive) descendants of <paramref name="root"/>
        /// (including <paramref name="root"/>) if they have the given <paramref name="tag"/>.
        /// </summary>
        /// <param name="gameObject">The game object on which to apply the <paramref name="action"/>.</param>
        /// <param name="tag">The tag the descendants must have.</param>
        /// <param name="action">The action to be applied.</param>
        /// <remarks>Although this method primarily intended for code cities, it is
        /// also applicable to a game node.</remarks>
        public static void ApplyToAllDescendants(this GameObject root, string tag, Action<GameObject> action)
        {
            if (root.CompareTag(tag))
            {
                action(root);
            }

            foreach (Transform child in root.transform)
            {
                child.gameObject.ApplyToAllDescendants(tag, action);
            }
        }

    }
}
