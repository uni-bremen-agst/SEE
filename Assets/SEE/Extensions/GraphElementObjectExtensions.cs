using SEE.Components.GameEdges;
using SEE.Game;
using SEE.Game.City;
using SEE.Game.Operator;
using SEE.GraphElementRefs;
using System;
using UnityEngine;

namespace SEE.Extensions
{
    /// <summary>
    /// Extension methods for game nodes and game edges alike. A game node is a <see cref="GameObject"/>
    /// representing a <see cref="SEE.DataModel.DG.Node"/>. A game edge is a <see cref="GameObject"/>
    /// representing a <see cref="SEE.DataModel.DG.Edge"/>
    /// </summary>
    internal static class GraphElementObjectExtensions
    {
        /// <summary>
        /// An extension of GameObjects to retrieve their IDs. If <paramref name="gameObject"/>
        /// has a <see cref="NodeRef"/> attached to it, the corresponding node's ID is returned.
        /// If <paramref name="gameObject"/> has an <see cref="EdgeRef"/> attached to it, the corresponding
        /// edge's ID is returned. Otherwise the name of <paramref name="gameObject"/> is
        /// returned.
        /// </summary>
        /// <returns>ID for <paramref name="gameObject"/>.</returns>
        /// <remarks>Applicable to game nodes and game edges.</remarks>
        public static string ID(this GameObject gameObject)
        {
            NodeRef nodeRef = gameObject.GetComponent<NodeRef>();
            if (nodeRef == null)
            {
                EdgeRef edgeRef = gameObject.GetComponent<EdgeRef>();
                if (edgeRef == null)
                {
                    return gameObject.name;
                }
                else
                {
                    return edgeRef.Value.ID;
                }
            }
            return nodeRef.Value.ID;
        }

        /// <summary>
        /// Returns the city this <paramref name="gameObject"/> is contained in.
        /// If <paramref name="gameObject"/> is null or if it is not contained in a city of type, null is returned.
        /// </summary>
        /// <param name="gameObject">Object whose containing city is requested.</param>
        /// <returns>The containing city of <paramref name="gameObject"/> or null.</returns>
        /// <remarks>Applicable to game nodes and game edges.</remarks>
        public static AbstractSEECity ContainingCity(this GameObject gameObject) => ContainingCity<AbstractSEECity>(gameObject);

        /// <summary>
        /// Returns the city of type <typeparamref name="T"/> this <paramref name="gameObject"/> is contained.
        /// If <paramref name="gameObject"/> is null or if it is not contained in a city of type, null is returned.
        /// </summary>
        /// <param name="gameObject">Object whose containing city of type <typeparamref name="T"/>
        /// is requested.</param>
        /// <returns>The containing city of type <typeparamref name="T"/> of <paramref name="gameObject"/>
        /// or null.</returns>
        /// <typeparam name="T">Type of the code city that shall be returned</typeparam>
        /// <remarks>Applicable to game nodes and game edges.</remarks>
        public static T ContainingCity<T>(this GameObject gameObject) where T : AbstractSEECity
        {
            if (gameObject == null)
            {
                return null;
            }
            else
            {
                GameObject codeCityObject = gameObject.GetCodeCity();
                if (codeCityObject != null && codeCityObject.TryGetComponent(out T city))
                {
                    return city;
                }
                else
                {
                    /// We do not log the fact that <see cref="codeCityObject"/> does not have the
                    /// expected type of city, as some clients are using this method just as a predicate.
                    return null;
                }
            }
        }

        /// <summary>
        /// Returns the closest ancestor of <paramref name="gameObject"/> that
        /// represents a code city, that is, is tagged by <see cref="Tags.CodeCity"/>.
        /// This ancestor is assumed to carry the settings (layout information etc.).
        /// If none can be found, null will be returned.
        /// If <paramref name="gameObject"/> is tagged by <see cref="Tags.CodeCity"/>,
        /// it will be returned.
        /// </summary>
        /// <param name="gameObject">Game object at which to start the search.</param>
        /// <returns>Closest ancestor game object in the game-object hierarchy tagged by
        /// <see cref="Tags.CodeCity"/> or null.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="gameObject"/> is null.
        /// </exception>
        /// <remarks>Applicable to game nodes and game edges.</remarks>
        public static GameObject GetCodeCity(this GameObject gameObject)
        {
            if (gameObject == null)
            {
                throw new ArgumentNullException(nameof(gameObject));
            }
            Transform result = gameObject.transform;
            while (result != null)
            {
                if (result.CompareTag(Tags.CodeCity))
                {
                    return result.gameObject;
                }
                result = result.parent;
            }
            return null;
        }

        /// <summary>
        /// Sets the visibility and the collider of this <paramref name="gameObject"/> to <paramref name="show"/>.
        /// If <paramref name="show"/> is false, the object becomes invisible. If it is true
        /// instead, it becomes visible.
        ///
        /// If <paramref name="includingChildren"/> is false, only the renderer of <paramref name="gameObject"/>
        /// is turned on/off, which will not affect whether the <paramref name="gameObject"/>
        /// is active or inactive. If <paramref name="gameObject"/> has children, their
        /// renderers will not be changed.
        ///
        /// If <paramref name="includingChildren"/> is true, the operation applies to all descendants, too.
        ///
        /// Precondition: <paramref name="gameObject"/> must have a Renderer.
        /// </summary>
        /// <param name="gameObject">Object whose visibility is to be changed.</param>
        /// <param name="show">Whether or not to make the object visible.</param>
        /// <param name="includingChildren">If true, the operation applies to all descendants, too.</param>
        /// <remarks>Applicable to both game nodes and game edges.</remarks>
        public static void SetVisibility(this GameObject gameObject, bool show, bool includingChildren = true)
        {
            if (gameObject.TryGetComponent(out Renderer renderer))
            {
                renderer.enabled = show;
            }

            if (gameObject.TryGetComponent(out Collider collider))
            {
                collider.enabled = show;
            }

            if (includingChildren)
            {
                foreach (Transform child in gameObject.transform)
                {
                    child.gameObject.SetVisibility(show, includingChildren);
                }
            }
        }

        /// <summary>
        /// Returns the world-space center position of the roof of this <paramref name="gameObject"/>.
        /// </summary>
        /// <param name="gameObject">Game object whose roof has to be determined.</param>
        /// <returns>World-space center position of the roof of this <paramref name="gameObject"/>.</returns>
        /// <remarks>This does not consider the position of descendants if there are any.
        /// Consider <see cref="GetTop(GameObject, Func{Transform, bool})"/> if you want to
        /// take descendants into account, too. This method is applicable to game nodes
        /// and game edges having a <see cref="SEE.Components.GameEdges.SEESpline"/>.</remarks>
        public static Vector3 GetRoofCenter(this GameObject gameObject)
        {
            Vector3 result;
            if (gameObject.TryGetComponent(out SEESpline spline))
            {
                // Splines aren't actually positioned at their game object's position,
                // but their position can be determined by their middle control point.
                result = spline.GetMiddleControlPoint();
                result.y += spline.Radius;
            }
            else
            {
                result = gameObject.transform.position;
                result.y += gameObject.WorldSpaceSize().y / 2.0f;
            }
            return result;
        }

        /// <summary>
        /// Returns the <see cref="GraphElementOperator"/> for this <paramref name="gameObject"/>.
        /// If no operator exists yet, a fitting operator will be added.
        /// If the game object is neither a node nor an edge, an exception will be thrown.
        /// </summary>
        /// <param name="gameObject">The game object whose operator to retrieve.</param>
        /// <returns>The <see cref="GraphElementOperator"/> responsible for this <paramref name="gameObject"/>.</returns>
        /// <remarks>Applicable to both game nodes and game edges.</remarks>
        public static GraphElementOperator Operator(this GameObject gameObject)
        {
            if (gameObject.TryGetComponent(out GraphElementOperator elementOperator))
            {
                return elementOperator;
            }
            else
            {
                // We may need to add the appropriate operator first.
                if (gameObject.IsNode())
                {
                    return gameObject.AddComponent<NodeOperator>();
                }
                else if (gameObject.IsEdge())
                {
                    return gameObject.AddComponent<EdgeOperator>();
                }
                else
                {
                    throw new InvalidOperationException($"Cannot get {nameof(GraphElementOperator)} for game object "
                                                        + $"{gameObject.name} because it is neither a node nor an edge.");
                }
            }
        }
    }
}
