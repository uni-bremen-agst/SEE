using SEE.Controls;
using SEE.DataModel.DG;
using SEE.DataModel.Drawable;
using SEE.Game;
using SEE.Game.City;
using SEE.Game.Operator;
using SEE.Utils;
using Sirenix.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static SEE.Game.Portal.IncludeDescendants;

namespace SEE.GO
{
    /// <summary>
    /// Provides extensions for GameObjects.
    /// </summary>
    public static class GameObjectExtensions
    {
        /// <summary>
        /// An extension of GameObjects to retrieve their IDs. If <paramref name="gameObject"/>
        /// has a NodeRef attached to it, the corresponding node's ID is returned.
        /// If <paramref name="gameObject"/> has an EdgeRef attached to it, the corresponding
        /// edge's ID is returned. Otherwise the name of <paramref name="gameObject"/> is
        /// returned.
        /// </summary>
        /// <returns>ID for <paramref name="gameObject"/>.</returns>
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
        /// Returns the first immediate child of <paramref name="gameObject"/> that
        /// is a graph node, i.e., has a <see cref="NodeRef"/> attached to it
        /// (checked by predicate <see cref="IsNode(GameObject)"/>) or null if there
        /// is none.
        /// </summary>
        /// <param name="gameObject">The game object whose child is to be retrieved.</param>
        /// <returns>First immediate child representing a node or null if there is none.</returns>
        public static GameObject FirstChildNode(this GameObject gameObject)
        {
            foreach (Transform child in gameObject.transform)
            {
                if (child.gameObject.IsNode())
                {
                    return child.gameObject;
                }
            }
            return null;
        }

        /// <summary>
        /// Returns true if a code city was drawn for this <paramref name="gameObject"/>.
        /// A code city is assumed to be drawn in there is at least one immediate child
        /// of this game object that represents a graph node, i.e., has a <see cref="NodeRef"/>
        /// (checked by predicate <see cref="IsNode(GameObject)"/>.
        ///
        /// This predicate can be queried for game objects representing a code city,
        /// that is, game objects that have an <see cref="AbstractSEECity"/> attached to
        /// them.
        /// </summary>
        /// <returns>True if a code city was drawn.</returns>
        public static bool IsCodeCityDrawn(this GameObject gameObject)
        {
            return gameObject.transform.Cast<Transform>().Any(child => child.gameObject.IsNode());
        }

        /// <summary>
        /// Returns true if a code city was drawn for this <paramref name="gameObject"/> and is active.
        /// A code city is assumed to be drawn in there is at least one immediate child
        /// of this game object that represents a graph node, i.e., has a <see cref="NodeRef"/>
        /// (checked by predicate <see cref="IsNode(GameObject)"/>.
        ///
        /// This predicate can be queried for game objects representing a code city,
        /// that is, game objects that have an <see cref="AbstractSEECity"/> attached to
        /// them.
        /// </summary>
        /// <returns>True if a code city was drawn and is active.</returns>
        public static bool IsCodeCityDrawnAndActive(this GameObject gameObject)
        {
            return gameObject.transform.Cast<Transform>().Any(child => child.gameObject.IsNode()
                    && child.gameObject.activeInHierarchy);
        }

        /// <summary>
        /// Returns the city this <paramref name="gameObject"/> is contained in.
        /// If <paramref name="gameObject"/> is null or if it is not contained in a city of type, null is returned.
        /// </summary>
        /// <param name="gameObject">Object whose containing city is requested.</param>
        /// <returns>The containing city of <paramref name="gameObject"/> or null.</returns>
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
        /// True if <paramref name="gameNode"/> represents a leaf in the graph.
        ///
        /// Precondition: <paramref name="gameNode"/> has a <see cref="NodeRef"/> component
        /// attached to it that is a valid graph node reference.
        /// </summary>
        /// <param name="gameNode">Game object representing a Node to be queried whether it is a leaf.</param>
        /// <returns>True if <paramref name="gameNode"/> represents a leaf in the graph.</returns>
        public static bool IsLeaf(this GameObject gameNode)
        {
            return gameNode.GetComponent<NodeRef>()?.Value?.IsLeaf() ?? false;
        }

        /// <summary>
        /// True if <paramref name="gameNode"/> represents the root of the graph.
        ///
        /// Precondition: <paramref name="gameNode"/> has a <see cref="NodeRef"/> component
        /// attached to it that is a valid graph node reference.
        /// </summary>
        /// <param name="gameNode">Game object representing a Node to be queried whether it is a root node.</param>
        /// <returns>True if <paramref name="gameNode"/> represents a root in the graph.</returns>
        public static bool IsRoot(this GameObject gameNode)
        {
            return gameNode.GetComponent<NodeRef>()?.Value?.IsRoot() ?? false;
        }

        /// <summary>
        /// True if <paramref name="gameNode"/> represents the implementation or architecture root of
        /// the graph.
        ///
        /// Precondition: <paramref name="gameNode"/> has a <see cref="NodeRef"/> component
        /// attached to it that is a valid graph node reference.
        /// </summary>
        /// <param name="gameNode">Game object representing a Node to be queried whether it is an implementation or architecture root.</param>
        /// <returns>True if <paramref name="gameNode"/> represents an implementation or architecture root in the graph.</returns>
        public static bool IsArchitectureOrImplementationRoot(this GameObject gameNode)
        {
            return gameNode.GetComponent<NodeRef>()?.Value?.IsArchitectureOrImplementationRoot() ?? false;
        }

        /// <summary>
        /// Returns all game objects tagged as <see cref="Tags.Edge"/> that are descendants
        /// of <paramref name="gameObject"/>.
        /// </summary>
        /// <param name="gameObject">Root game object to be traversed.</param>
        /// <returns>All game objects tagged as <see cref="Tags.Edge"/>.</returns>
        internal static IEnumerable<GameObject> AllEdges(this GameObject gameObject)
        {
            return gameObject.AllDescendants(Tags.Edge);
        }

        /// <summary>
        /// Returns all transitive children of <paramref name="gameObject"/> tagged by
        /// given <paramref name="tag"/> (including <paramref name="gameObject"/> itself).
        /// </summary>
        /// <param name="gameObject">The game object whose children are requested.</param>
        /// <param name="tag">The tag the descendants must have.</param>
        /// <returns>All transitive children with <paramref name="tag"/>.</returns>
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

        /// <summary>
        /// Returns the first descendant of the given <paramref name="gameObject"/> with the given <paramref name="name"/>
        /// (attribute 'name' of a GameObject).
        /// Will also return inactive game objects. If no such descendant exists, null will be returned.
        /// Unlike <see cref="Transform.Find(string)"/>, this method will descend into the game-object hierarchy.
        /// </summary>
        /// <param name="gameObject">Root object.</param>
        /// <param name="name">Name of the descendant to be found.</param>
        /// <returns>Found game object or null.</returns>
        public static GameObject Descendant(this GameObject gameObject, string name)
        {
            foreach (Transform child in gameObject.transform)
            {
                if (child.name == name)
                {
                    return child.gameObject;
                }
                else
                {
                    GameObject ancestor = child.gameObject.Descendant(name);
                    if (ancestor != null)
                    {
                        return ancestor;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Returns all descendants of <paramref name="gameObject"/> having a name contained in <paramref name="gameObjectIDs"/>.
        /// The result will also include inactive game objects, but does not contain <paramref name="gameObject"/> itself.
        /// This method will descend into the game-object hierarchy rooted by <paramref name="gameObject"/>.
        ///
        /// Precondition: <paramref name="gameObjectIDs"/> is not null.
        /// </summary>
        /// <param name="gameObject">Root of the game-object hierarchy to be searched.</param>
        /// <param name="gameObjectIDs">List of names any of the game objects to be retrieved should have.</param>
        /// <returns>Found game objects.</returns>
        public static IList<GameObject> Descendants(this GameObject gameObject, ISet<string> gameObjectIDs)
        {
            List<GameObject> result = new();
            foreach (Transform child in gameObject.transform)
            {
                if (gameObjectIDs.Contains(child.name))
                {
                    result.Add(child.gameObject);
                }

                result.AddRange(child.gameObject.Descendants(gameObjectIDs));
            }

            return result;
        }

        /// <summary>
        /// Sets the color for this <paramref name="gameObject"/> to given <paramref name="color"/>.
        ///
        /// Precondition: <paramref name="gameObject"/> has a renderer whose material has a color attribute.
        /// </summary>
        /// <seealso cref="Material.color"/>
        /// <param name="gameObject">Object whose color is to be set.</param>
        /// <param name="color">The new color to be set.</param>
        public static void SetColor(this GameObject gameObject, Color color)
        {
            if (gameObject.TryGetComponent(out Renderer renderer))
            {
                renderer.sharedMaterial.color = color;
            }
        }

        /// <summary>
        /// Retrieves the color from this <paramref name="gameObject"/>.
        ///
        /// Precondition: <paramref name="gameObject"/> has a renderer whose material has a color attribute.
        /// </summary>
        /// <param name="gameObject">Object whose color is to be returned.</param>
        /// <returns>Color of this <paramref name="gameObject"/>.</returns>
        /// <exception cref="InvalidOperationException">
        /// If this <paramref name="gameObject"/> has no renderer attached to it.
        /// </exception>
        public static Color GetColor(this GameObject gameObject)
        {
            return gameObject.MustGetComponent<Renderer>().sharedMaterial.color;
        }

        /// <summary>
        /// Sets the alpha value (transparency) of the given <paramref name="gameObject"/>
        /// to <paramref name="alpha"/>.
        /// </summary>
        /// <param name="gameObject">Game objects whose transparency is to be set.</param>
        /// <param name="alpha">A value in between 0 and 1 for transparency.</param>
        public static void SetTransparency(this GameObject gameObject, float alpha)
        {
            if (gameObject.TryGetComponent(out Renderer renderer))
            {
                Color oldColor = renderer.material.color;
                renderer.material.color = oldColor.WithAlpha(alpha);
            }
        }

        /// <summary>
        /// Sets the start and end line color of <paramref name="gameObject"/>.
        ///
        /// Precondition: <paramref name="gameObject"/> must have a line renderer.
        /// </summary>
        /// <param name="gameObject">Object holding a line renderer whose start and end color is to be set.</param>
        /// <param name="startColor">Start color of the line.</param>
        /// <param name="endColor">End color of the line.</param>
        public static void SetLineColor(this GameObject gameObject, Color startColor, Color endColor)
        {
            if (gameObject.TryGetComponent(out LineRenderer renderer))
            {
                renderer.startColor = startColor;
                renderer.endColor = endColor;
            }
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
        /// Sets the scale of this <paramref name="gameObject"/> to <paramref name="worldScale"/> independent from
        /// the local scale of its parent.
        /// </summary>
        /// <param name="gameObject">Object whose scale should be set.</param>
        /// <param name="worldScale">The new scale in world space.</param>
        /// <param name="animate">If true and <paramref name="gameObject"/> is a graph node,
        /// a <see cref="Game.Operator.NodeOperator"/> will be used to animate the scaling; otherwise the
        /// scale of <paramref name="gameObject"/> is set immediately without any animation.</param>
        public static void SetAbsoluteScale(this GameObject gameObject, Vector3 worldScale, bool animate = true)
        {
            Transform parent = gameObject.transform.parent;
            gameObject.transform.parent = null;
            if (animate && gameObject.HasNodeRef())
            {
                NodeOperator @operator = gameObject.NodeOperator();
                @operator.ScaleTo(worldScale, 0f);
            }
            else
            {
                gameObject.transform.localScale = worldScale;
            }
            gameObject.transform.parent = parent;
        }

        /// <summary>
        /// Returns the world-space y position of the roof of this <paramref name="gameObject"/>.
        /// </summary>
        /// <param name="gameObject">Game object whose roof has to be determined.</param>
        /// <returns>World-space y position of the roof of this <paramref name="gameObject"/>.</returns>
        /// <remarks>This does not consider the position of descendants if there are any.
        /// Consider <see cref="GetTop(GameObject, Func{Transform, bool})"/> if you want to
        /// take descendants into account, too.</remarks>
        public static float GetRoof(this GameObject gameObject)
        {
            return gameObject.transform.position.y + gameObject.WorldSpaceSize().y / 2.0f;
        }

        /// <summary>
        /// Returns the world-space center position of the roof of this <paramref name="gameObject"/>.
        /// </summary>
        /// <param name="gameObject">Game object whose roof has to be determined.</param>
        /// <returns>World-space center position of the roof of this <paramref name="gameObject"/>.</returns>
        /// <remarks>This does not consider the position of descendants if there are any.
        /// Consider <see cref="GetTop(GameObject, Func{Transform, bool})"/> if you want to
        /// take descendants into account, too.</remarks>
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
        /// Returns the world-space center position of the ground of this <paramref name="gameObject"/>.
        /// </summary>
        /// <param name="gameObject">Game object whose ground has to be determined.</param>
        /// <returns>World-space center position of the ground of this <paramref name="gameObject"/>.</returns>
        public static Vector3 GetGroundCenter(this GameObject gameObject)
        {
            Vector3 result = gameObject.transform.position;
            result.y -= gameObject.WorldSpaceSize().y / 2.0f;
            return result;
        }

        /// <summary>
        /// Returns the maximal world-space position (y co-ordinate) of the roof of
        /// this <paramref name="gameObject"/> or any of its active descendants.
        /// Unlike <see cref="GetRoof(GameObject)"/>, this method recurses into
        /// the game-object hierarchy rooted by <paramref name="gameObject"/>.
        ///
        /// Note: only descendants that are currently active in the scene are considered.
        /// </summary>
        /// <param name="gameObject">Game object whose height has to be determined.</param>
        /// <param name="filterTransform">Function returning true for descendant transforms that shall be taken into
        /// account. By default, this is a constant function which always returns true.</param>
        /// <returns>World-space position of the roof of this <paramref name="gameObject"/>
        /// or any of its active descendants.</returns>
        public static float GetMaxY(this GameObject gameObject, Func<Transform, bool> filterTransform = null)
        {
            float result = float.NegativeInfinity;
            filterTransform ??= _ => true;
            Recurse(gameObject, ref result);
            return result;

            void Recurse(GameObject root, ref float max)
            {
                float roof = root.GetRoof();
                if (max < roof)
                {
                    max = roof;
                }

                foreach (Transform child in root.transform)
                {
                    if (child.gameObject.activeInHierarchy && filterTransform(child))
                    {
                        Recurse(child.gameObject, ref max);
                    }
                }
            }
        }

        /// <summary>
        /// Returns the maximal world-space center position of the hull of
        /// this <paramref name="gameObject"/>. The hull includes <paramref name="gameObject"/>
        /// and any of its active descendants.
        /// Unlike <see cref="GetRoof(GameObject)"/>, this method recurses into
        /// the game-object hierarchy rooted by <paramref name="gameObject"/>.
        ///
        /// Note: only descendants that are currently active in the scene are considered.
        /// </summary>
        /// <param name="gameObject">Game object whose center top has to be determined.</param>
        /// <param name="filterTransform">Function returning true for descendant transforms that shall be taken into
        /// account. By default, this is a constant function which always returns true.</param>
        /// <returns>World-space position of the center top of the hull of this <paramref name="gameObject"/>.
        /// </returns>
        /// <remarks>The result is in world space of <see cref="gameObject"/>. If your are interested
        /// in local space, use <see cref="GetRelativeTop(GameObject, Func{Transform, bool})"/> instead.</remarks>
        public static Vector3 GetTop(this GameObject gameObject, Func<Transform, bool> filterTransform = null)
        {
            Vector3 result = gameObject.transform.position;
            result.y = gameObject.GetMaxY(filterTransform);
            return result;
        }

        /// <summary>
        /// Returns the maximal local-space center position of the hull of
        /// this <paramref name="gameObject"/>. The hull includes <paramref name="gameObject"/>
        /// and any of its active descendants.
        /// Note: only descendants that are currently active in the scene are considered.
        /// </summary>
        /// <param name="gameObject">Game object whose center top has to be determined.</param>
        /// <param name="filterTransform">Function returning true for descendant transforms that shall be taken into
        /// account. By default, this is a constant function which always returns true.</param>
        /// <returns>Local-space position of the center top of the hull of this <paramref name="gameObject"/>.
        /// </returns>
        /// <remarks>The result is in local space of <see cref="gameObject"/>. If your are interested
        /// in world space, use <see cref="GetTop(GameObject, Func{Transform, bool})"/> instead.</remarks>
        public static float GetRelativeTop(this GameObject gameObject, Func<Transform, bool> filterTransform = null)
        {
            float top = gameObject.GetMaxY(filterTransform);
            return top - gameObject.transform.position.y;
        }
        /// <summary>
        /// Provides the size and the mesh offset of the given <paramref name="gameObject"/> in world space.
        /// This does not include the size of descendants if there are any.
        /// <para>
        /// This value reflects the actual world-space bounds of the axis-aligned cuboid that contains the rendered
        /// object.
        /// Please note that the <see cref="Transform.lossyScale"/> is only the scale factor and not the actual size of
        /// a rendered object.
        /// Similarly, <see cref="Transform.position"/> is not necessarily the center point of the rendered object.
        /// </para><para>
        /// <list type="bullet">
        /// <item>
        /// If a <see cref="Collider"/> is attached, the <see cref="Collider.bounds"/> will be used.
        /// </item><item>
        /// If a <see cref="LineRenderer"/> is attached, the bounds will be calculated based on its positions with a
        /// performance penalty (see <see cref="GeometryUtils.CalculateLineBounds"/>).
        /// </item><item>
        /// If a <see cref="Renderer"/> is attached, the <see cref="Renderer.bounds"/> will be used.
        /// </item><item>
        /// Else, <see cref="Transform.lossyScale"/> and <see cref="Transform.position"/> are provided and a warning
        /// is logged.
        /// It means that either the object is not rendered at all or this method needs to be extended.
        /// </item>
        /// </list>
        /// </para><para>
        /// Local-space counterpart: <see cref="LocalSize(GameObject, out Vector3, out Vector3)"/>
        /// </para>
        /// </summary>
        /// <param name="gameObject">Object whose scale is requested.</param>
        /// <param name="position">Out parameter for the world-space position of the object.</param>
        /// <param name="size">Out parameter for the world-space size of the object.</param>
        /// <returns>True if the size was successfully retrieved, false if the fallback was used.</returns>
        public static bool WorldSpaceSize(this GameObject gameObject, out Vector3 size, out Vector3 position)
        {
            // Rely on collider bounds if available.
            if (gameObject.TryGetComponent(out Collider collider))
            {
                size = collider.bounds.size;
                position = collider.bounds.center;
                return true;
            }

            // For objects with a LineRenderer, we can use its positions to determine its bounds.
            // Otherwise Unity will return overly large bounds.
            if (gameObject.TryGetComponent(out LineRenderer lineRenderer))
            {
                Bounds lineBounds = GeometryUtils.CalculateLineBounds(lineRenderer, true);
                size = lineBounds.size;
                position = lineBounds.center;
                return true;
            }

            // For some objects, such as capsules or custom meshes, lossyScale gives wrong results.
            // The more reliable option to determine the size is using the
            // object's renderer if it has one.
            if (gameObject.TryGetComponent(out Renderer renderer))
            {
                size = renderer.bounds.size;
                position = renderer.bounds.center;
                return true;
            }

            // No renderer, so we use lossyScale as a fallback.
            // Note: This may happen for container objects that have no mesh.
            size = gameObject.transform.lossyScale;
            position = gameObject.transform.position;
            return false;
        }

        /// <summary>
        /// Returns the size of the given <paramref name="gameObject"/> in world space.
        /// This does not include the size of descendants if there are any.
        /// <para>
        /// This is a shorthand method for <see cref="WorldSpaceSize(GameObject, out Vector3, out Vector3)"/> that only returns the size.
        /// See there for additional documentation.
        /// </para><para>
        /// Use <see cref="WorldSpaceSize(GameObject, out Vector3, out Vector3)"/> directly if you need both position and size.
        /// </para><para>
        /// Local-space counterpart: <see cref="LocalSize(GameObject)"/>
        /// </para>
        /// </summary>
        /// <param name="gameObject">Object whose size is requested.</param>
        /// <returns>Size of given <paramref name="gameObject"/>.</returns>
        public static Vector3 WorldSpaceSize(this GameObject gameObject)
        {
            WorldSpaceSize(gameObject, out Vector3 size, out Vector3 _);
            return size;
        }

        /// <summary>
        /// Provides the size and the mesh offset of the given <paramref name="gameObject"/> in local space,
        /// i.e., in relation to its parent.
        /// This does not include the size of descendants if there are any.
        /// <para>
        /// This value should often be used instead of the <see cref="Transform.localScale"/> because the scale only
        /// reflects the size for objects with a standardized size like cube primitives. Similarly, the
        /// <see cref="Transform.localPosition"/> can be significantly off the object's center.
        /// </para><para>
        /// <list type="bullet">
        /// <item>
        /// If a <see cref="Collider"/> is attached, the <see cref="Collider.bounds"/> will be used and converted into
        /// local space.
        /// </item><item>
        /// If a <see cref="LineRenderer"/> is attached, the bounds will be calculated based on its positions with a
        /// performance penalty (see <see cref="GeometryUtils.CalculateLineBounds"/>).
        /// </item><item>
        /// If a <see cref="MeshFilter"/> is attached, the <see cref="MeshFilter.sharedMesh.bounds"/> will be used.
        /// </item><item>
        /// Else, <see cref="Transform.localScale"/> and <see cref="Transform.localPosition"/> are provided and a
        /// warning is logged.
        /// It means that either the object is not rendered at all or this method needs to be extended.
        /// </item>
        /// </list>
        /// </para><para>
        /// World-space counterpart: <see cref="WorldSpaceSize(GameObject, out Vector3, out Vector3)"/>
        /// </para>
        /// </summary>
        /// <param name="gameObject">Object whose scale is requested.</param>
        /// <param name="size">Out parameter for the local size of the object.</param>
        /// <param name="position">Out parameter for the local position of the object.</param>
        /// <returns>True if the <paramref name="gameObject"/> has a size, false if the fallback was used.</returns>
        public static bool LocalSize(this GameObject gameObject, out Vector3 size, out Vector3 position)
        {
            // Rely on collider bounds if available.
            if (gameObject.TryGetComponent(out Collider collider))
            {
                size = getLocalColliderSize(collider);
                position = collider.transform.InverseTransformPoint(collider.bounds.center) + gameObject.transform.localPosition;
                return true;
            }

            // For objects with a LineRenderer, we can use its positions to determine its bounds.
            // Otherwise Unity will return overly large bounds.
            if (gameObject.TryGetComponent(out LineRenderer lineRenderer))
            {
                Bounds lineBounds = GeometryUtils.CalculateLineBounds(lineRenderer, false);
                size = lineBounds.size;
                position = lineBounds.center;
                return true;
            }

            // For some objects, such as capsules or custom meshes, localScale gives wrong results.
            // The more reliable option to determine the size is using the object's mesh if it has one.
            Mesh sharedMesh;
            if (gameObject.TryGetComponent(out MeshFilter meshFilter) && (sharedMesh = meshFilter.sharedMesh) != null)
            {
                size = Vector3.Scale(sharedMesh.bounds.size, gameObject.transform.localScale);
                position = sharedMesh.bounds.center + gameObject.transform.localPosition;
                return true;
            }

            // No mesh, so we use localScale as a fallback.
            // Note: This should not happen. If the object has no mesh, it has no size at all.
            Debug.LogWarning($"GameObject has no mesh or LineRenderer, using localScale as fallback: {gameObject.name}");
            size = gameObject.transform.localScale;
            position = gameObject.transform.localPosition;
            return false;

            Vector3 getLocalColliderSize(Collider collider)
            {
                Vector3 localScale = collider.transform.localScale;

                if (collider is BoxCollider box)
                {
                    return Vector3.Scale(box.size, localScale);
                }
                else if (collider is SphereCollider sphere)
                {
                    float diameter = sphere.radius * 2f;
                    // Sphere scales uniformly in all axes
                    return new Vector3(diameter, diameter, diameter) * Mathf.Max(localScale.x, Mathf.Max(localScale.y, localScale.z));
                }
                else if (collider is CapsuleCollider capsule)
                {
                    float diameter = capsule.radius * 2f;
                    Vector3 size = Vector3.zero;
                    switch (capsule.direction)
                    {
                        case 0: // X axis
                            size = new Vector3(capsule.height, diameter, diameter);
                            break;
                        case 1: // Y axis
                            size = new Vector3(diameter, capsule.height, diameter);
                            break;
                        case 2: // Z axis
                            size = new Vector3(diameter, diameter, capsule.height);
                            break;
                        default:
                            // This should never happen
                            throw new NotImplementedException();
                    }
                    size.x *= localScale.x;
                    size.y *= localScale.y;
                    size.z *= localScale.z;
                    return size;
                }
                else if (collider is MeshCollider meshCollider)
                {
                    Mesh mesh = meshCollider.sharedMesh;
                    if (mesh != null)
                    {
                        return Vector3.Scale(mesh.bounds.size, localScale);
                    }
                    else
                    {
                        return Vector3.zero;
                    }
                }
                else
                {
                    // Fallback: bounds.size is in world space, convert to local by dividing by scale
                    Debug.LogWarning($"GameObject has unknown collider type, using localScale as fallback: {gameObject.name}");
                    Bounds worldBounds = collider.bounds;
                    Vector3 worldSize = worldBounds.size;
                    return new Vector3(
                        localScale.x != 0 ? worldSize.x / localScale.x : 0,
                        localScale.y != 0 ? worldSize.y / localScale.y : 0,
                        localScale.z != 0 ? worldSize.z / localScale.z : 0);
                }
            }
        }

        /// <summary>
        /// Returns the size of the given <paramref name="gameObject"/> in local space,
        /// i.e., in relation to its parent.
        /// <para>
        /// This is a shorthand method for <see cref="LocalSize(GameObject, out Vector3, out Vector3)"/> that only returns the size.
        /// See there for additional documentation.
        /// </para><para>
        /// Use <see cref="LocalSize(GameObject, out Vector3, out Vector3)"/> directly if you need both position and size.
        /// </para><para>
        /// World-space counterpart: <see cref="WorldSpaceSize(GameObject)"/>
        /// </para>
        /// </summary>
        /// <param name="gameObject">Object whose size is requested.</param>
        /// <returns>Size of given <paramref name="gameObject"/>.</returns>
        public static Vector3 LocalSize(this GameObject gameObject)
        {
            LocalSize(gameObject, out Vector3 size, out Vector3 _);
            return size;
        }

        /// <summary>
        /// Returns the bounds of the given <paramref name="gameObject"/> in its own
        /// local coordinate system.
        /// <para>
        /// Note: A primitive cube has a size of (1,1,1), and a coordinate center (pivot)
        /// of (0,0,0).
        /// However, that does not apply for all primitives or models in general.
        /// </para>
        /// </summary>
        /// <param name="gameObject">The game object.</param>
        /// <returns>Local-space bounds of <paramref name="gameObject"/>.</returns>
        public static Bounds LocalBounds(this GameObject gameObject)
        {
            // For objects with a LineRenderer, we can use its positions to determine its bounds.
            // Otherwise Unity will return overly large bounds.
            if (gameObject.TryGetComponent(out LineRenderer lineRenderer))
            {
                return GeometryUtils.CalculateLineBounds(lineRenderer, false);
            }
            if (gameObject.TryGetComponent(out MeshFilter meshFilter))
            {
                return meshFilter.sharedMesh.bounds;
            }
            if (gameObject.TryGetComponent(out Renderer renderer))
            {
                return new(
                        gameObject.transform.InverseTransformPoint(renderer.bounds.center),
                        gameObject.transform.InverseTransformVector(renderer.bounds.size));
            }
            // This fallback works for uniform primitives like cubes, but not for non-uniforms like cylinders.
            return new(Vector3.zero, Vector3.one);
        }

        /// <summary>
        /// Returns true if this <paramref name="block"/> is within the spatial area of <paramref name="parentBlock"/>,
        /// that is, if the bounding box of <paramref name="block"/> plus the extra padding <paramref name="outerEdgeMargin"/>
        /// is fully contained in the bounding box of <paramref name="parentBlock"/>.
        ///
        /// Note that this only checks on the XZ-plane, and ignores any height difference between the two blocks.
        /// </summary>
        /// <param name="block">We check whether this block is included in <paramref name="parentBlock"/>'s area.
        /// </param>
        /// <param name="parentBlock">The block whose area shall be checked.</param>
        /// <param name="outerEdgeMargin">Additional margins that should be added inward the area.</param>
        /// <returns>True if this <paramref name="block"/> is within the area of <paramref name="parentBlock"/>.
        /// </returns>
        public static bool IsInArea(this GameObject block, GameObject parentBlock, float outerEdgeMargin)
        {
            // FIXME: Support node types other than cubes
            Collider collider = block.MustGetComponent<Collider>();
            Vector3 blockCenter = collider.bounds.center;
            // We only care about the XZ-plane. Setting z to zero here makes it consistent with the bounds setup below.
            Bounds blockBounds = new(new Vector3(blockCenter.x, blockCenter.z, 0), collider.bounds.extents);
            Collider parentCollider = parentBlock.MustGetComponent<Collider>();
            Bounds parentBlockBounds = parentCollider.bounds;

            Vector2 topRight = parentBlockBounds.max.XZ();
            Vector2 bottomLeft = parentBlockBounds.min.XZ();
            Vector2 topLeft = topRight.WithXY(x: bottomLeft.x);
            Vector2 bottomRight = topRight.WithXY(y: bottomLeft.y);

            // These represent the outer edge regions of the parent block with the margins applied.
            Bounds left = new(bottomLeft, Vector3.zero);
            left.Encapsulate(topLeft.WithXY(x: topLeft.x + outerEdgeMargin));
            if (left.Intersects(blockBounds))
            {
                return true;
            }

            Bounds right = new(bottomRight, Vector3.zero);
            right.Encapsulate(topRight.WithXY(x: topRight.x - outerEdgeMargin));
            if (right.Intersects(blockBounds))
            {
                return true;
            }

            Bounds bottom = new(bottomLeft, Vector3.zero);
            bottom.Encapsulate(bottomRight.WithXY(y: bottomRight.y + outerEdgeMargin));
            if (bottom.Intersects(blockBounds))
            {
                return true;
            }

            Bounds top = new(topLeft, Vector3.zero);
            top.Encapsulate(topRight.WithXY(y: topRight.y - outerEdgeMargin));
            return top.Intersects(blockBounds);
        }


        /// <summary>
        /// Tries to get the component of the given type <typeparamref name="T"/> of this <paramref name="gameObject"/>.
        /// If the component was found, it will be stored in <paramref name="component"/> and true will be returned.
        /// If it wasn't found, <paramref name="component"/> will be null, false will be returned,
        /// and an error message will be logged indicating that the component type wasn't present on the GameObject.
        /// </summary>
        /// <param name="gameObject">The game object the component should be gotten from. Must not be null.</param>
        /// <param name="component">The variable in which to save the component.</param>
        /// <typeparam name="T">The type of the component.</typeparam>
        /// <returns>True if the component was present on the <paramref name="gameObject"/>, false otherwise.</returns>
        public static bool TryGetComponentOrLog<T>(this GameObject gameObject, out T component)
        {
            if (!gameObject.TryGetComponent(out component))
            {
                Debug.LogError($"Couldn't find component '{typeof(T).GetNiceName()}' "
                               + $"on game object '{gameObject.FullName()}'.\n");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Tries to get the component of the given type <typeparamref name="T"/> of this <paramref name="gameObject"/>.
        /// If a component of the type was found, it will be returned, otherwise a new component of the type
        /// will be added and returned.
        /// </summary>
        /// <param name="gameObject">The gameobject whose component of type <typeparamref name="T"/>
        /// we wish to return.</param>
        /// <typeparam name="T">The component to get / add</typeparam>
        /// <returns>The existing or newly created component.</returns>
        public static T AddOrGetComponent<T>(this GameObject gameObject) where T : Component
        {
            return gameObject.TryGetComponent(out T component) ? component : gameObject.AddComponent<T>();
        }

        /// <summary>
        /// Tries to get the component of the given type <typeparamref name="T"/> of this <paramref name="gameObject"/>.
        /// If the component was found, it will be returned.
        /// If it wasn't found, <see cref="InvalidOperationException"/> will be thrown.
        /// </summary>
        /// <param name="gameObject">The game object the component should be gotten from. Must not be null.</param>
        /// <typeparam name="T">The type of the component.</typeparam>
        /// <exception cref="InvalidOperationException">Thrown if <paramref name="gameObject"/> has no
        /// component of type <typeparamref name="T"/>.</exception>
        public static T MustGetComponent<T>(this GameObject gameObject)
        {
            if (!gameObject.TryGetComponent(out T component))
            {
                throw new InvalidOperationException($"Couldn't find component '{typeof(T).GetNiceName()}' on game object '{gameObject.FullName()}'");
            }
            return component;
        }

        /// <summary>
        /// Returns true if <paramref name="gameObject"/> has a <see cref="NodeRef"/>
        /// component attached to it that is actually referring to a valid node
        /// (i.e., its Value is not null).
        /// </summary>
        /// <param name="gameObject">The game object whose NodeRef is checked.</param>
        /// <returns>True if <paramref name="gameObject"/> has a <see cref="NodeRef"/>
        /// component attached to it whose node is non-null.</returns>
        public static bool HasNodeRef(this GameObject gameObject)
        {
            return gameObject.TryGetComponent(out NodeRef nodeRef) && nodeRef.Value != null;
        }

        /// <summary>
        /// Returns true if <paramref name="gameObject"/> is tagged by <see cref="Tags.Node"/>.
        /// </summary>
        /// <param name="gameObject">The game object to check.</param>
        /// <returns>True if <paramref name="gameObject"/> is tagged by <see cref="Tags.Node"/>.</returns>
        public static bool IsNode(this GameObject gameObject)
        {
            return gameObject.CompareTag(Tags.Node);
        }

        /// <summary>
        /// Returns true if <paramref name="gameObject"/>'s <see cref="GameObject.activeSelf"/>
        /// is true and it is tagged by <see cref="Tags.Node"/>.
        /// </summary>
        /// <param name="gameObject">The game object to check.</param>
        /// <returns>True if <paramref name="gameObject"/> is an active node.</returns>
        public static bool IsNodeAndActiveSelf(this GameObject gameObject)
        {
            return gameObject.activeSelf && gameObject.CompareTag(Tags.Node);
        }

        /// <summary>
        /// Returns true if <paramref name="gameObject"/>'s <see cref="GameObject.activeInHierarchy"/>
        /// is true and it is tagged by <see cref="Tags.Node"/>.
        /// </summary>
        /// <param name="gameObject">The game object to check.</param>
        /// <returns>True if <paramref name="gameObject"/> is an active node.</returns>
        public static bool IsNodeAndActiveInHierarchy(this GameObject gameObject)
        {
            return gameObject.CompareTag(Tags.Node) && gameObject.activeInHierarchy;
        }

        /// <summary>
        /// Retrieves the node reference component, if possible.
        /// </summary>
        /// <param name="gameObject">The game object whose NodeRef is checked.</param>
        /// <param name="nodeRef">The attached NodeRef; defined only if this method
        /// returns true.</param>
        /// <returns>True if <paramref name="gameObject"/> has a <see cref="NodeRef"/>
        /// component attached to it.</returns>
        public static bool TryGetNodeRef(this GameObject gameObject, out NodeRef nodeRef)
        {
            return gameObject.TryGetComponent(out nodeRef);
        }

        /// <summary>
        /// Returns true if <paramref name="gameObject"/> has a <see cref="NodeRef"/>
        /// component attached to it that is not null.
        /// </summary>
        /// <param name="gameObject">The game object whose NodeRef is checked.</param>
        /// <param name="node">The node referenced by the attached NodeRef; defined only if this method
        /// returns true.</param>
        /// <returns>True if <paramref name="gameObject"/> has a <see cref="NodeRef"/>
        /// component attached to it that is not null.</returns>
        public static bool TryGetNode(this GameObject gameObject, out Node node)
        {
            node = null;
            if (gameObject.TryGetComponent(out NodeRef nodeRef))
            {
                node = nodeRef.Value;
            }
            return node != null;
        }

        /// <summary>
        /// Returns true if <paramref name="gameObject"/> has a <see cref="DrawableSurfaceRef"/>
        /// component attached to it that is not null.
        /// </summary>
        /// <param name="gameObject">The game object whose DrawableSurfaceRef is checked.</param>
        /// <param name="surface">The surface referenced by the attached DrawableSurfaceRef; defined only if this method
        /// returns true.</param>
        /// <returns>True if <paramref name="gameObject"/> has a <see cref="DrawableSurfaceRef"/>
        /// component attached to it that is not null.</returns>
        public static bool TryGetDrawableSurface(this GameObject gameObject, out DrawableSurface surface)
        {
            surface = null;
            if (gameObject.TryGetComponent(out DrawableSurfaceRef surfaceRef))
            {
                surface = surfaceRef.Surface;
            }
            return surface != null;
        }

        /// <summary>
        /// Returns the graph node represented by this <paramref name="gameObject"/>.
        ///
        /// Precondition: <paramref name="gameObject"/> must have a <see cref="NodeRef"/>
        /// attached to it referring to a valid node; if not, an exception is raised.
        /// </summary>
        /// <param name="gameObject">The game object whose Node is requested.</param>
        /// <returns>The correponding graph node (will never be null).</returns>
        /// <exception cref="NullReferenceException">Thrown if <paramref name="gameObject"/> has
        /// no valid <see cref="NodeRef"/> or <see cref="Node"/>.</exception>
        public static Node GetNode(this GameObject gameObject)
        {
            if (gameObject.TryGetComponent(out NodeRef nodeRef))
            {
                if (nodeRef != null)
                {
                    if (nodeRef.Value != null)
                    {
                        return nodeRef.Value;
                    }
                    else
                    {
                        throw new NullReferenceException($"Node referenced by game object {gameObject.name} is null.");
                    }
                }
                else
                {
                    throw new NullReferenceException($"Node reference of game object {gameObject.name} is null.");
                }
            }
            else
            {
                throw new NullReferenceException($"Game object {gameObject.name} has no NodeRef.");
            }
        }

        /// <summary>
        /// Returns true if <paramref name="gameObject"/> has an <see cref="EdgeRef"/>
        /// component attached to it whose edge is not null.
        /// </summary>
        /// <param name="gameObject">The game object whose EdgeRef is checked.</param>
        /// <returns>True if <paramref name="gameObject"/> has an <see cref="EdgeRef"/>
        /// component attached to it whose edge is not null.</returns>
        public static bool HasEdgeRef(this GameObject gameObject)
        {
            return gameObject.TryGetComponent(out EdgeRef edgeRef) && edgeRef.Value != null;
        }

        /// <summary>
        /// Returns true if <paramref name="gameObject"/> is tagged by <see cref="Tags.Edge"/>.
        /// </summary>
        /// <param name="gameObject">The game object to check.</param>
        /// <returns>True if <paramref name="gameObject"/> is tagged by <see cref="Tags.Edge"/>.</returns>
        public static bool IsEdge(this GameObject gameObject)
        {
            return gameObject.CompareTag(Tags.Edge);
        }

        /// <summary>
        /// Returns true if <paramref name="gameObject"/> has an <see cref="EdgeRef"/>
        /// component attached to it that is not null.
        /// </summary>
        /// <param name="gameObject">The game object whose EdgeRef is checked.</param>
        /// <param name="edge">The edge referenced by the attached EdgeRef; defined only if this method
        /// returns true.</param>
        /// <returns>True if <paramref name="gameObject"/> has an <see cref="EdgeRef"/>
        /// component attached to it that is not null.</returns>
        public static bool TryGetEdge(this GameObject gameObject, out Edge edge)
        {
            edge = null;
            if (gameObject.TryGetComponent(out EdgeRef edgeRef))
            {
                edge = edgeRef.Value;
            }

            return edge != null;
        }

        /// <summary>
        /// Returns the graph containing the node represented by this <paramref name="gameObject"/>.
        ///
        /// Precondition: <paramref name="gameObject"/> must have a <see cref="NodeRef"/>
        /// attached to it referring to a valid node; if not, an exception is raised.
        /// </summary>
        /// <param name="gameObject">The game object whose graph is requested.</param>
        /// <returns>The correponding graph.</returns>
        public static Graph ItsGraph(this GameObject gameObject)
        {
            return gameObject.GetNode().ItsGraph;
        }

        /// <summary>
        /// Enables/disables the renderers of <paramref name="gameObject"/> and all its
        /// descendants so that they become visible/invisible.
        /// </summary>
        /// <param name="gameObject">Objects whose renderer (and those of its children) is to be enabled/disabled.</param>
        /// <param name="isVisible">Iff true, the renderers will be enabled.</param>
        private static void SetVisible(this GameObject gameObject, bool isVisible)
        {
            gameObject.GetComponent<Renderer>().enabled = isVisible;
            foreach (Transform child in gameObject.transform)
            {
                SetVisible(child.gameObject, isVisible);
            }
        }

        /// <summary>
        /// Returns the full name of the game object, that is, its name and the
        /// names of its ancestors in the game-object hierarchy separated by /.
        /// </summary>
        /// <param name="gameObject">Game object for which to retrieve the full name.</param>
        public static string FullName(this GameObject gameObject)
        {
            string result = gameObject.name;
            while (gameObject.transform.parent != null)
            {
                gameObject = gameObject.transform.parent.gameObject;
                result = gameObject.name + "/" + result;
            }

            return result;
        }

        /// <summary>
        /// Returns all active descendants of given <paramref name="rootNode"/> tagged by <see cref="Tags.Node"/>
        /// including <paramref name="rootNode"/> itself.
        /// </summary>
        /// <param name="rootNode">The root of the node hierarchy to be collected.</param>
        /// <returns>All descendants of <paramref name="rootNode"/> including <paramref name="rootNode"/>.</returns>
        public static IList<GameObject> AllDescendants(this GameObject rootNode)
        {
            IList<GameObject> result = new List<GameObject>() { rootNode };
            AllDescendants(rootNode, result);
            return result;
        }

        /// <summary>
        /// Adds all active descendants of <paramref name="root"/> to <paramref name="result"/>
        /// (only if tagged by <see cref="Tags.Node"/>).
        ///
        /// Note: <paramref name="root"/> is assumed to be contained in <paramref name="result"/>
        /// already.
        /// </summary>
        /// <param name="root">The root of the game-object hierarchy to be collected.</param>
        /// <param name="result">Where to add the descendants.</param>
        private static void AllDescendants(GameObject root, IList<GameObject> result)
        {
            foreach (Transform child in root.transform)
            {
                if (child.gameObject.activeInHierarchy && child.gameObject.CompareTag(Tags.Node))
                {
                    result.Add(child.gameObject);
                    AllDescendants(child.gameObject, result);
                }
            }
        }

        /// <summary>
        /// Returns the source node of the given <paramref name="gameObject"/>.
        /// The <paramref name="gameObject"/> is assumed to represent an edge, that is,
        /// is tagged by <see cref="Tags.Edge"/> and has an <see cref="EdgeRef"/>.
        /// If this is not the case, an exception is thrown. If the source node
        /// of this edge does not exist, an exception is thrown, too.
        /// </summary>
        /// <param name="gameObject">Game object representing an edge.</param>
        /// <returns>The game object representing the source of this edge.</returns>
        public static GameObject Source(this GameObject gameObject)
        {
            if (gameObject.CompareTag(Tags.Edge) && gameObject.TryGetComponent(out EdgeRef edgeRef))
            {
                return GraphElementIDMap.Find(edgeRef.SourceNodeID, mustFindElement: true);
            }
            else
            {
                throw new Exception($"Game object {gameObject.name} is not an edge. It has no source node.");
            }
        }

        /// <summary>
        /// Returns the target node of the given <paramref name="gameObject"/>.
        /// The <paramref name="gameObject"/> is assumed to represent an edge, that is,
        /// is tagged by <see cref="Tags.Edge"/> and has an <see cref="EdgeRef"/>.
        /// If this is not the case, an exception is thrown. If the target node
        /// of this edge does not exist, an exception is thrown, too.
        /// </summary>
        /// <param name="gameObject">Game object representing an edge.</param>
        /// <returns>The game object representing the target of this edge.</returns>
        public static GameObject Target(this GameObject gameObject)
        {
            if (gameObject.CompareTag(Tags.Edge) && gameObject.TryGetComponent(out EdgeRef edgeRef))
            {
                return GraphElementIDMap.Find(edgeRef.SourceNodeID, mustFindElement: true);
            }
            else
            {
                throw new Exception($"Game object {gameObject.name} is not an edge. It has no target node.");
            }
        }

        /// <summary>
        /// Updates the portal of this game object by setting the boundaries of itself
        /// (and its descendants, depending on <paramref name="includeDescendants"/>)
        /// to the code city they're contained in.
        /// If they're not contained in a code city and <paramref name="warnOnFailure"/> is true,
        /// a warning log message will be emitted, otherwise nothing will happen.
        /// </summary>
        /// <param name="gameObject">The game object whose portal shall be updated.</param>
        /// <param name="warnOnFailure">
        /// Whether a warning log message shall be emitted if the <paramref name="gameObject"/>
        /// is not attached to any code city.
        /// </param>
        /// <param name="includeDescendants">
        /// Whether the portal of the descendants of this <paramref name="gameObject"/> shall be updated too.
        /// </param>
        public static void UpdatePortal(this GameObject gameObject, bool warnOnFailure = false,
                                        Portal.IncludeDescendants includeDescendants = OnlySelf)
        {
            GameObject rootCity = gameObject.GetCodeCity();
            if (rootCity != null)
            {
                Portal.SetPortal(rootCity, gameObject, includeDescendants);
            }
            else if (warnOnFailure)
            {
                Debug.LogWarning("Couldn't update portal: No code city has been found"
                                 + $" attached to game object {gameObject.FullName()}.\n");
            }
        }

        /// <summary>
        /// Enables/disables the child of <paramref name="gameObject"/> with <paramref name="childName"/>.
        /// </summary>
        /// <param name="gameObject">Object whose child is to be enabled/disabled.</param>
        /// <param name="childName">The name of the child; may be a composite name.</param>
        /// <param name="active">Whether to enable it.</param>
        public static void SetChildActive(this GameObject gameObject, string childName, bool active)
        {
            Transform child = gameObject.transform.Find(childName);
            if (child)
            {
                child.gameObject.SetActive(active);
            }
            else
            {
                Debug.LogError($"Game object '{gameObject.FullName()}' does not have child with name '{childName}'.\n");
            }
        }

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
                throw new InvalidOperationException($"Cannot get {nameof(NodeOperator)} for game object {gameObject.name} because it is not a node.");
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
                throw new InvalidOperationException($"Cannot get {nameof(EdgeOperator)} for game object {gameObject.name} because it is not an edge.");
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

        /// <summary>
        /// Checks if <paramref name="gameObject"/> overlaps with any other active direct child node of its parent.
        /// <para>
        /// Overlap is checked based on the <see cref="Collider"/> components. Objects with no <see cref="Collider"/>
        /// component and inactive nodes are ignored.
        /// </para>
        /// </summary>
        /// <remarks>
        /// The <paramref name="gameObject"/> must be a node, i.e., coantain a NodeRef component.
        /// </remarks>
        /// <param name="gameObject">The game object whose operator to retrieve.</param>
        /// <returns>False if <paramref name="gameObject"/> does not have a <see cref="Collider"/> component,
        /// or does not overlap with its siblings.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the object the method is called on is not a node, i.e., has no <see cref="NodeRef"/>
        /// component.
        /// </exception>
        public static bool OverlapsWithSiblings(this GameObject gameObject)
        {
            if (!gameObject.HasNodeRef())
            {
                throw new InvalidOperationException("GameObject must be a node!");
            }
            if (!gameObject.TryGetComponent(out Collider collider))
            {
                return false;
            }
            foreach (Transform sibling in gameObject.transform.parent)
            {
                if (sibling.gameObject == gameObject || !sibling.gameObject.IsNodeAndActiveSelf()
                    || !sibling.gameObject.TryGetComponent(out Collider siblingCollider))
                {
                    continue;
                }

                if (collider.bounds.Intersects(siblingCollider.bounds))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Searches for the first child that starts with the <paramref name="prefix"/>.
        /// </summary>
        /// <param name="gameObject">The game object whose children should be examined.</param>
        /// <param name="prefix">The prefix to search for.</param>
        /// <returns>The found child or null.</returns>
        public static GameObject FindChildWithPrefix(this GameObject gameObject, string prefix)
        {
            foreach (Transform child in gameObject.transform)
            {
                if (child.name.StartsWith(prefix))
                {
                    return child.gameObject;
                }
            }
            return null;
        }

        /// <summary>
        /// Searches for the first descendant <see cref="GameObject"/> with the specified <paramref name="descendantName"/>
        /// within the hierarchy of the given <paramref name="gameObject"/>.
        /// </summary>
        /// <param name="gameObject">The game object whose descendants will be searched.</param>
        /// <param name="descendantName">The name of the descendant to search for.</param>
        /// <param name="includeInactive">If set to true, the search wil include inactive <see cref="GameObject"/>s.
        /// Otherwise, only active ones will be considered.</param>
        /// <returns>The frist matching descendant <see cref="GameObject"/> with the specified <paramref name="descendantName"/>,
        /// or null if none is found.</returns>
        public static GameObject FindDescendant(this GameObject gameObject, string descendantName, bool includeInactive = true)
        {
            return gameObject
                    .GetComponentsInChildren<Transform>(includeInactive)
                    .FirstOrDefault(t => t.gameObject.name == descendantName)?
                    .gameObject;
        }

        /// <summary>
        /// Searches for the first descendant <see cref="GameObject"/> with the specified <paramref name="tag"/>
        /// within the hierarchy of the given <paramref name="gameObject"/>.
        /// </summary>
        /// <param name="gameObject">The game object whose descendants will be searched.</param>
        /// <param name="tag">The tag to search for.</param>
        /// <param name="includeInactive">If set to true, the search will include inactive <see cref="GameObject"/>s.
        /// Otherwise, only active ones will be considered.</param>
        /// <returns>The first matching descendant <see cref="GameObject"/> with the specified tag, or null if none is found.</returns>
        public static GameObject FindDescendantWithTag(this GameObject gameObject, string tag, bool includeInactive = true)
        {
            return gameObject
                .GetComponentsInChildren<Transform>(includeInactive)
                .FirstOrDefault(t => t.gameObject.CompareTag(tag))?
                .gameObject;
        }

        /// <summary>
        /// QDetermines whether the <paramref name="gameObject"/> has any descendant
        /// with the specified <paramref name="tag"/>.
        /// </summary>
        /// <param name="gameObject">The root <see cref="GameObject"/> to search from.</param>
        /// <param name="tag">The tag to search for.</param>
        /// <returns>True if a descendant with the specified tag is found; otherwise, false.</returns>
        public static bool HasDescendantWithTag(this GameObject gameObject, string tag)
        {
            return gameObject.FindDescendantWithTag(tag) != null;
        }

        /// <summary>
        /// Finds all descendant <see cref="GameObject"/>s of the given <paramref name="gameObject"/>
        /// that have the specified tag.
        /// </summary>
        /// <param name="gameObject">The root <see cref="GameObject"/> to start the search from.</param>
        /// <param name="tag">The tag that matching descendants must have.</param>
        /// <param name="includeInactive">Whether to include inactive <see cref="GameObject"/>s in the search.</param>
        /// <returns>A list of all descendant <see cref="GameObject"/>s with the specified tag.</returns>
        public static IList<GameObject> FindAllDescendantsWithTag(this GameObject gameObject, string tag, bool includeInactive = true)
        {
            return gameObject
                .GetComponentsInChildren<Transform>(includeInactive)
                .Where(t => t.CompareTag(tag))
                .Select(t => t.gameObject)
                .ToList();
        }
        /// <summary>
        /// Finds all descendant <see cref="GameObject"/>s with the specified <paramref name="descendantTag"/>,
        /// exluding those whose immediate parent has the specified <paramref name="immediateParentTag"/>.
        /// </summary>
        /// <param name="gameObject">The root <see cref="GameObject"/> to search from.</param>
        /// <param name="descendantTag">The tag that matching descendants must have.</param>
        /// <param name="immediateParentTag">If the immediate parent has this tag, the child will be excluded from the result.</param>
        /// <param name="includeInactive">Whether inactive <see cref="GameObject"/>s should be included in the search.</param>
        /// <returns>A list of matching descendant <see cref="GameObject"/>s, excluding those whose parent has the specified tag.</returns>
        public static List<GameObject> FindAllDescendantsWithTagExcludingSpecificParentTag(this GameObject gameObject,
            string descendantTag, string immediateParentTag, bool includeInactive = true)
        {
            return gameObject
                .GetComponentsInChildren<Transform>(includeInactive)
                .Where(t => t.CompareTag(descendantTag) &&
                            t.parent != null &&
                            !t.parent.CompareTag(immediateParentTag))
                .Select(t => t.gameObject)
                .ToList();
        }

        /// <summary>
        /// Determines whether the specified <paramref name="gameObject"/> has any ancestor
        /// with the given <paramref name="tag"/>.
        /// </summary>
        /// <param name="gameObject">The starting <see cref="GameObject"/> whose parent hierarhcy will be searched.</param>
        /// <param name="tag">The tag to search for.</param>
        /// <returns>True if a parent or ancestor with the specified tag is found; otherwise, false.</returns>
        public static bool HasParentWithTag(this GameObject gameObject, string tag)
        {
            Transform transform = gameObject.transform;
            while (transform.parent != null)
            {
                if (transform.parent.gameObject.CompareTag(tag))
                {
                    return true;
                }
                transform = transform.parent;
            }
            return false;
        }

        /// <summary>
        /// Searches upward through the transform hierarchy to find the first parent GameObject
        /// with the specified name.
        /// </summary>
        /// <param name="gameObject">The starting GameObject from which the search begins.</param>
        /// <param name="name">The exact name of the parent GameObject to look for.</param>
        /// <returns>
        /// The first matching parent GameObject, or null if no parent with the given name is found.
        /// </returns>
        public static GameObject FindParentWithName(this GameObject gameObject, string name)
        {
            if (gameObject.transform.parent == null)
            {
                return null;
            }
            else
            {
                return gameObject.transform.parent.name == name ?
                          gameObject.transform.parent.gameObject
                        : FindParentWithName(gameObject.transform.parent.gameObject, name);
            }
        }

        /// <summary>
        /// Checks recursively whether the specified GameObject has any parent
        /// with the given layer.
        /// </summary>
        /// <param name="gameObject">The starting GameObject from which the search begins.</param>
        /// <param name="layer">The layer number to check against.</param>
        /// <returns>
        /// True if any parent GameObject has the specified layer;
        /// otherwise, false.
        /// </returns>
        public static bool HasParentWithLayer(this GameObject gameObject, uint layer)
        {
            if (gameObject.transform.parent == null)
            {
                return false;
            }
            else
            {
                return gameObject.transform.parent.gameObject.layer == layer
                       || HasParentWithLayer(gameObject.transform.parent.gameObject, layer);
            }
        }

        /// <summary>
        /// Traverses up the hierachy from the given <paramref name="gameObject"/>
        /// and returns the highest parent.
        /// </summary>
        /// <param name="gameObject">The starting <see cref="GameObject"/> in the hierarchy.</param>
        /// <returns>The root <see cref="GameObject"/> at the top of the hierarchy.
        /// If the given object has no parent, it is returned itself.</returns>
        public static GameObject GetRootParent(this GameObject gameObject)
        {
            Transform parent = gameObject.transform.parent;
            return parent != null ? GetRootParent(parent.gameObject) : gameObject;
        }

        /// <summary>
        /// Updates the interaction layer of the game object, and optionally its children.
        /// </summary>
        /// <param name="gameObject">The affected game object.</param>
        /// <param name="recurse">Should children be updated as well?.</param>
        public static void UpdateInteractableLayers(this GameObject gameObject, bool recurse = true)
        {
            if (gameObject.TryGetComponent(out InteractableObjectBase io))
            {
                io.UpdateLayer();
            }
            else
            {
                Debug.LogWarning($"GameObject {gameObject.name} is not an interactable object!");
            }

            if (recurse)
            {
                InteractableObjectBase[] children = gameObject.transform.GetComponentsInChildren<InteractableObjectBase>();
                foreach (InteractableObjectBase child in children)
                {
                    child.UpdateLayer();
                }
            }
        }
    }
}
