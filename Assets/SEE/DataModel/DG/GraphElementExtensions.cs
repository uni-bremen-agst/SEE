using System.Collections.Generic;
using System.Linq;
using SEE.DataModel.DG;
using SEE.Game;
using SEE.Game.Operator;
using SEE.GO;
using UnityEngine;

namespace SEE.Utils
{
    /// <summary>
    /// Contains utility extension methods for graph elements.
    /// </summary>
    public static class GraphElementExtensions
    {
        /// <summary>
        /// Returns the <see cref="NodeOperator"/> for this <paramref name="node"/>.
        /// If no operator exists yet, it will be added.
        /// </summary>
        /// <param name="node">The node whose operator to retrieve.</param>
        /// <returns>The <see cref="NodeOperator"/> responsible for this <paramref name="node"/>.</returns>
        public static NodeOperator Operator(this Node node) => node.GameObject(true).NodeOperator();

        /// <summary>
        /// Returns the <see cref="EdgeOperator"/> for this <paramref name="edge"/>.
        /// If no operator exists yet, it will be added.
        /// </summary>
        /// <param name="edge">The edge whose operator to retrieve.</param>
        /// <returns>The <see cref="EdgeOperator"/> responsible for this <paramref name="edge"/>.</returns>
        public static EdgeOperator Operator(this Edge edge, bool mustFind = true) => edge.GameObject(mustFind).EdgeOperator();

        /// <summary>
        /// Returns the <see cref="GraphElementOperator"/> for this graph <paramref name="element"/>.
        /// If no operator exists yet, it will be added.
        /// </summary>
        /// <param name="element">The graph element whose operator to retrieve.</param>
        /// <returns>The <see cref="GraphElementOperator"/> responsible for this <paramref name="element"/>.</returns>
        public static GraphElementOperator Operator(this GraphElement element) => element.GameObject(true).Operator();

        /// <summary>
        /// Returns the <see cref="GameObject"/> for this graph <paramref name="element"/>.
        /// If no game object is associated to it, an exception will be thrown iff <paramref name="mustFind"/> is true,
        /// otherwise null will be returned.
        /// </summary>
        /// <param name="element">The graph element whose <see cref="GameObject"/> to retrieve.</param>
        /// <param name="mustFind">Whether to throw an exception if no corresponding game object was found.</param>
        /// <returns>The <see cref="GameObject"/> for this graph <paramref name="element"/>.</returns>
        public static GameObject GameObject(this GraphElement element, bool mustFind = false)
            => GraphElementIDMap.Find(element.ID, mustFind);

        /// <summary>
        /// Returns true iff the given <paramref name="element"/> has a game object associated to it.
        /// </summary>
        /// <param name="element">The element to check.</param>
        /// <returns>True iff the given <paramref name="element"/> has a game object associated to it.</returns>
        public static bool HasGameObject(this GraphElement element)
            => GraphElementIDMap.Has(element.ID);

        /// <summary>
        /// Returns all <see cref="GraphElement"/>s in the given <paramref name="elements"/> which have a
        /// game object associated to them.
        /// </summary>
        /// <param name="elements">The elements to filter.</param>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <returns>All <see cref="GraphElement"/>s in the given <paramref name="elements"/> which have a
        /// game object associated to them.</returns>
        public static IEnumerable<T> WithGameObject<T>(this IEnumerable<T> elements) where T : GraphElement
            => elements.Where(HasGameObject);
    }
}
