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
        /// <param name="mustFindGameObject">Whether to throw an exception
        /// if no corresponding game object was found.</param>
        /// <returns>The <see cref="NodeOperator"/> responsible for this <paramref name="node"/>.</returns>
        public static NodeOperator Operator(this Node node, bool mustFindGameObject = true)
            => node.GameObject(mustFindGameObject).AddOrGetComponent<NodeOperator>();

        /// <summary>
        /// Returns the <see cref="EdgeOperator"/> for this <paramref name="edge"/>.
        /// If no operator exists yet, it will be added.
        /// </summary>
        /// <param name="edge">The edge whose operator to retrieve.</param>
        /// <param name="mustFindGameObject">Whether to throw an exception
        /// if no corresponding game object was found.</param>
        /// <returns>The <see cref="EdgeOperator"/> responsible for this <paramref name="edge"/>.</returns>
        public static EdgeOperator Operator(this Edge edge, bool mustFindGameObject = true)
            => edge.GameObject(mustFindGameObject).AddOrGetComponent<EdgeOperator>();

        /// <summary>
        /// Returns the <see cref="GameObject"/> for this graph <paramref name="element"/>.
        /// If no game object is associated to it, an exception will be thrown iff <paramref name="mustFind"/> is true,
        /// otherwise <c>null</c> will be returned.
        /// </summary>
        /// <param name="element">The graph element whose <see cref="GameObject"/> to retrieve.</param>
        /// <param name="mustFind">Whether to throw an exception if no corresponding game object was found.</param>
        /// <returns>The <see cref="GameObject"/> for this graph <paramref name="element"/>.</returns>
        public static GameObject GameObject(this GraphElement element, bool mustFind = true)
            => GraphElementIDMap.Find(element.ID, mustFind);
    }
}