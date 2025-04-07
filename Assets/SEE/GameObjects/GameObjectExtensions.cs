using System;
using System.Collections.Generic;
using System.Linq;
using SEE.DataModel.DG;
using SEE.Game;
using SEE.Game.City;
using SEE.Game.Operator;
using SEE.Utils;
using UnityEngine;
using static SEE.Game.Portal.IncludeDescendants;
using Sirenix.Utilities;
using SEE.DataModel.Drawable;

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
        /// <returns>ID for <paramref name="gameObject"/></returns>
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
        /// Returns true if a code city was drawn for this <paramref name="gameObject"/>.
        /// A code city is assumed to be drawn in there is at least one immediate child
        /// of this game object that represents a graph node, i.e., has a <see cref="NodeRef"/>
        /// (checked by predicate <see cref="IsNode(GameObject)"/>.
        ///
        /// This predicate can be queried for game objects representing a code city,
        /// that is, game objects that have an <see cref="AbstractSEECity"/> attached to
        /// them.
        /// </summary>
        /// <returns>true if a code city was drawn</returns>
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
        /// <returns>true if a code city was drawn and is active.</returns>
        public static bool IsCodeCityDrawnAndActive(this GameObject gameObject)
        {
            return gameObject.transform.Cast<Transform>().Any(child => child.gameObject.IsNode()
                    && child.gameObject.activeInHierarchy);
        }

        /// <summary>
        /// If <paramref name="gameObject"/> represents a graph node or edge, the city this
        /// object is contained in will be returned. Otherwise null is returned.
        /// </summary>
        /// <param name="gameObject">graph node or edge whose containing city is requested</param>
        /// <returns>the containing city of <paramref name="gameObject"/> or null</returns>
        public static AbstractSEECity ContainingCity(this GameObject gameObject) => ContainingCity<AbstractSEECity>(gameObject);

        /// <summary>
        /// If <paramref name="gameObject"/> represents a graph node or edge, the city of type <typeparamref name="T"/>
        /// this object is contained in will be returned. Otherwise null is returned.
        /// </summary>
        /// <param name="gameObject">graph node or edge whose containing city of type <typeparamref name="T"/>
        /// is requested</param>
        /// <returns>the containing city of type <typeparamref name="T"/> of <paramref name="gameObject"/>
        /// or null</returns>
        /// <typeparam name="T">Type of the code city that shall be returned</typeparam>
        public static T ContainingCity<T>(this GameObject gameObject) where T : AbstractSEECity
        {
            if (gameObject == null || (!gameObject.HasNodeRef() && !gameObject.HasEdgeRef()))
            {
                return null;
            }
            else
            {
                Transform codeCityObject = SceneQueries.GetCodeCity(gameObject.transform);
                if (codeCityObject != null && codeCityObject.gameObject.TryGetComponent(out T city))
                {
                    return city;
                }
                else
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// True if <paramref name="gameNode"/> represents a leaf in the graph.
        ///
        /// Precondition: <paramref name="gameNode"/> has a <see cref="NodeRef"/> component
        /// attached to it that is a valid graph node reference.
        /// </summary>
        /// <param name="gameNode">game object representing a Node to be queried whether it is a leaf</param>
        /// <returns>true if <paramref name="gameNode"/> represents a leaf in the graph</returns>
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
        /// <param name="gameNode">game object representing a Node to be queried whether it is a root node</param>
        /// <returns>true if <paramref name="gameNode"/> represents a root in the graph</returns>
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
        /// <param name="gameNode">game object representing a Node to be queried whether it is an implementation or architecture root</param>
        /// <returns>true if <paramref name="gameNode"/> represents an implementation or architecture root in the graph</returns>
        public static bool IsArchitectureOrImplementationRoot(this GameObject gameNode)
        {
            return gameNode.GetComponent<NodeRef>()?.Value?.IsArchitectureOrImplementationRoot() ?? false;
        }

        /// <summary>
        /// Returns all game objects tagged as <see cref="Tags.Edge"/> that are descendants
        /// of <paramref name="gameObject"/>.
        /// </summary>
        /// <param name="gameObject">root game object to be traversed</param>
        /// <returns>all game objects tagged as <see cref="Tags.Edge"/></returns>
        internal static IEnumerable<GameObject> AllEdges(this GameObject gameObject)
        {
            return gameObject.AllDescendants(Tags.Edge);
        }

        /// <summary>
        /// Returns all transitive children of <paramref name="gameObject"/> tagged by
        /// given <paramref name="tag"/> (including <paramref name="gameObject"/> itself).
        /// </summary>
        /// <param name="tag">tag the descendants must have</param>
        /// <returns>all transitive children with <paramref name="tag"/></returns>
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
        /// Returns the first descendant of the given <paramref name="gameObject"/> with the given <paramref name="name"/>
        /// (attribute 'name' of a GameObject).
        /// Will also return inactive game objects. If no such descendant exists, null will be returned.
        /// Unlike <see cref="Transform.Find(string)"/>, this method will descend into the game-object hierarchy.
        /// </summary>
        /// <param name="gameObject">root object</param>
        /// <param name="name">name of the descendant to be found</param>
        /// <returns>found game object or null</returns>
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
        /// <param name="gameObject">root of the game-object hierarchy to be searched</param>
        /// <param name="gameObjectIDs">list of names any of the game objects to be retrieved should have</param>
        /// <returns>found game objects</returns>
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
        /// <param name="gameObject">object whose color is to be set</param>
        /// <param name="color">the new color to be set</param>
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
        /// <param name="gameObject">object whose color is to be returned</param>
        /// <returns>Color of this <paramref name="gameObject"/></returns>
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
        /// <param name="gameObject">game objects whose transparency is to be set</param>
        /// <param name="alpha">a value in between 0 and 1 for transparency</param>
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
        /// <param name="gameObject">object holding a line renderer whose start and end color is to be set</param>
        /// <param name="startColor">start color of the line</param>
        /// <param name="endColor">end color of the line</param>
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
        /// <param name="gameObject">object whose visibility is to be changed</param>
        /// <param name="show">whether or not to make the object visible</param>
        /// <param name="includingChildren">if true, the operation applies to all descendants, too</param>
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
        /// <param name="gameObject">object whose scale should be set</param>
        /// <param name="worldScale">the new scale in world space</param>
        /// <param name="animate">if true and <paramref name="gameObject"/> is a graph node,
        /// a <see cref="Game.Operator.NodeOperator"/> will be used to animate the scaling; otherwise the
        /// scale of <paramref name="gameObject"/> is set immediately without any animation</param>
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
        /// <param name="gameObject">game object whose roof has to be determined</param>
        /// <returns>world-space y position of the roof of this <paramref name="gameObject"/></returns>
        public static float GetRoof(this GameObject gameObject)
        {
            return gameObject.transform.position.y + gameObject.WorldSpaceSize().y / 2.0f;
        }

        /// <summary>
        /// Returns the world-space center position of the roof of this <paramref name="gameObject"/>.
        /// </summary>
        /// <param name="gameObject">game object whose roof has to be determined</param>
        /// <returns>world-space center position of the roof of this <paramref name="gameObject"/></returns>
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
        /// <param name="gameObject">game object whose ground has to be determined</param>
        /// <returns>world-space center position of the ground of this <paramref name="gameObject"/></returns>
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
        /// Note: only descendants that are currently active in the scene are
        /// considered.
        /// </summary>
        /// <param name="gameObject">game object whose height has to be determined</param>
        /// <param name="filterTransform">Function returning true for descendant transforms that shall be taken into
        /// account. By default, this is a constant function which always returns true.</param>
        /// <returns>world-space position of the roof of this <paramref name="gameObject"/>
        /// or any of its active descendants</returns>
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
        /// Returns the maximal world-space center position of hull of
        /// this <paramref name="gameObject"/>. The hull includes <paramref name="gameObject"/>
        /// and any of its active descendants.
        /// Unlike <see cref="GetRoof(GameObject)"/>, this method recurses into
        /// the game-object hierarchy rooted by <paramref name="gameObject"/>.
        ///
        /// Note: only descendants that are currently active in the scene are
        /// considered.
        /// </summary>
        /// <param name="gameObject">game object whose center top has to be determined</param>
        /// <param name="filterTransform">Function returning true for descendant transforms that shall be taken into
        /// account. By default, this is a constant function which always returns true.</param>
        /// <returns>world-space position of the center top of the hull of this <paramref name="gameObject"/>
        /// </returns>
        public static Vector3 GetTop(this GameObject gameObject, Func<Transform, bool> filterTransform = null)
        {
            Vector3 result = gameObject.transform.position;
            result.y = gameObject.GetMaxY(filterTransform);
            return result;
        }

        /// <summary>
        /// Returns the size of the given <paramref name="gameObject"/> in world space.
        /// <para>
        /// This is a shorthand to get the <c>bounds.size</c> of the <see cref="Renderer"/> component, if present.
        /// This value reflects the actual world-space bounds of the cuboid that contains the rendered object.
        /// </para><para>
        /// This value should often be used instead of the <c>transform.lossyScale</c> because the scale only reflects
        /// the size for objects with a standardized size like cube primitives.
        /// </para><para>
        /// Local-space counterpart: <see cref="LocalSize"/>
        /// </para>
        /// </summary>
        /// <remarks>
        /// If the game object has no renderer, its <c>lossyScale</c> is returned.
        /// </remarks>
        /// <param name="gameObject">object whose scale is requested</param>
        /// <returns>size of given <paramref name="gameObject"/></returns>
        public static Vector3 WorldSpaceSize(this GameObject gameObject)
        {
            // For some objects, such as capsules, lossyScale gives wrong results.
            // The more reliable option to determine the scale is using the
            // object's renderer if it has one.
            if (gameObject.TryGetComponent(out Renderer renderer))
            {
                return renderer.bounds.size;
            }
            else
            {
                // No renderer, so we use lossyScale as a fallback.
                return gameObject.transform.lossyScale;
            }
        }

        /// <summary>
        /// Returns the size of the given <paramref name="gameObject"/> in local space,
        /// i.e., in relation to its parent.
        /// <para>
        /// This value should often be used instead of the <c>transform.localScale</c> because the scale only reflects
        /// the size for objects with a standardized size like cube primitives.
        /// </para><para>
        /// World-space counterpart: <see cref="WorldSpaceSize"/>
        /// </para>
        /// </summary>
        /// <remarks>
        /// If the game object has no renderer, its <c>localScale</c> is returned.
        /// </remarks>
        /// <param name="gameObject">object whose scale is requested</param>
        /// <returns>size of given <paramref name="gameObject"/></returns>
        public static Vector3 LocalSize(this GameObject gameObject)
        {
            // For some objects, such as capsules, localScale gives wrong results.
            // The more reliable option to determine the scale is using the
            // object's renderer if it has one.
            if (gameObject.TryGetComponent(out Renderer renderer))
            {
                return Vector3.Scale(renderer.localBounds.size, gameObject.transform.localScale);
            }
            else
            {
                // No renderer, so we use localScale as a fallback.
                return gameObject.transform.localScale;
            }
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
        /// <param name="block">We check whether this block is included in <paramref name="parentBlock"/>'s area
        /// </param>
        /// <param name="parentBlock">The block whose area shall be checked</param>
        /// <param name="outerEdgeMargin">Additional margins that should be added inward the area </param>
        /// <returns>True if this <paramref name="block"/> is within the area of <paramref name="parentBlock"/>
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
        /// If it wasn't found, <paramref name="component"/> will be <code>null</code>, false will be returned,
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
        /// we wish to return</param>
        /// <typeparam name="T">The component to get / add</typeparam>
        /// <returns>The existing or newly created component</returns>
        public static T AddOrGetComponent<T>(this GameObject gameObject) where T: Component
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
        /// <exception cref="InvalidOperationException">thrown if <paramref name="gameObject"/> has no
        /// component of type <typeparamref name="T"/></exception>
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
        /// <param name="gameObject">the game object whose NodeRef is checked</param>
        /// <returns>true if <paramref name="gameObject"/> has a <see cref="NodeRef"/>
        /// component attached to it whose node is non-null</returns>
        public static bool HasNodeRef(this GameObject gameObject)
        {
            return gameObject.TryGetComponent(out NodeRef nodeRef) && nodeRef.Value != null;
        }

        /// <summary>
        /// Returns true if <paramref name="gameObject"/> is tagged by <see cref="Tags.Node"/>.
        /// </summary>
        /// <param name="gameObject">the game object to check</param>
        /// <returns>true if <paramref name="gameObject"/> is tagged by <see cref="Tags.Node"/></returns>
        public static bool IsNode(this GameObject gameObject)
        {
            return gameObject.CompareTag(Tags.Node);
        }

        /// <summary>
        /// Returns true if <paramref name="gameObject"/>'s <see cref="GameObject.activeSelf"/>
        /// is true and it is tagged by <see cref="Tags.Node"/>.
        /// </summary>
        /// <param name="gameObject">the game object to check</param>
        /// <returns>true if <paramref name="gameObject"/> is an active node</returns>
        public static bool IsNodeAndActiveSelf(this GameObject gameObject)
        {
            return gameObject.activeSelf && gameObject.CompareTag(Tags.Node);
        }

        /// <summary>
        /// Returns true if <paramref name="gameObject"/>'s <see cref="GameObject.activeInHierarchy"/>
        /// is true and it is tagged by <see cref="Tags.Node"/>.
        /// </summary>
        /// <param name="gameObject">the game object to check</param>
        /// <returns>true if <paramref name="gameObject"/> is an active node</returns>
        public static bool IsNodeAndActiveInHierarchy(this GameObject gameObject)
        {
            return gameObject.CompareTag(Tags.Node) && gameObject.activeInHierarchy;
        }

        /// <summary>
        /// Retrieves the node reference component, if possible.
        /// </summary>
        /// <param name="gameObject">the game object whose NodeRef is checked</param>
        /// <param name="nodeRef">the attached NodeRef; defined only if this method
        /// returns true</param>
        /// <returns>true if <paramref name="gameObject"/> has a <see cref="NodeRef"/>
        /// component attached to it</returns>
        public static bool TryGetNodeRef(this GameObject gameObject, out NodeRef nodeRef)
        {
            return gameObject.TryGetComponent(out nodeRef);
        }

        /// <summary>
        /// Returns true if <paramref name="gameObject"/> has a <see cref="NodeRef"/>
        /// component attached to it that is not null.
        /// </summary>
        /// <param name="gameObject">the game object whose NodeRef is checked</param>
        /// <param name="node">the node referenced by the attached NodeRef; defined only if this method
        /// returns true</param>
        /// <returns>true if <paramref name="gameObject"/> has a <see cref="NodeRef"/>
        /// component attached to it that is not null</returns>
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
        /// <param name="gameObject">the game object whose DrawableSurfaceRef is checked</param>
        /// <param name="surface">the surface referenced by the attached DrawableSurfaceRef; defined only if this method
        /// returns true.</param>
        /// <returns>true if <paramref name="gameObject"/> has a <see cref="DrawableSurfaceRef"/>
        /// component attached to it that is not null</returns>
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
        /// <param name="gameObject">the game object whose Node is requested</param>
        /// <returns>the correponding graph node (will never be null)</returns>
        /// <exception cref="NullReferenceException">thrown if <paramref name="gameObject"/> has
        /// no valid <see cref="NodeRef"/> or <see cref="Node"/></exception>
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
        /// <param name="gameObject">the game object whose EdgeRef is checked</param>
        /// <returns>true if <paramref name="gameObject"/> has an <see cref="EdgeRef"/>
        /// component attached to it whose edge is not null</returns>
        public static bool HasEdgeRef(this GameObject gameObject)
        {
            return gameObject.TryGetComponent(out EdgeRef edgeRef) && edgeRef.Value != null;
        }

        /// <summary>
        /// Returns true if <paramref name="gameObject"/> is tagged by <see cref="Tags.Edge"/>.
        /// </summary>
        /// <param name="gameObject">the game object to check</param>
        /// <returns>true if <paramref name="gameObject"/> is tagged by <see cref="Tags.Edge"/></returns>
        public static bool IsEdge(this GameObject gameObject)
        {
            return gameObject.CompareTag(Tags.Edge);
        }

        /// <summary>
        /// Returns true if <paramref name="gameObject"/> has an <see cref="EdgeRef"/>
        /// component attached to it that is not null.
        /// </summary>
        /// <param name="gameObject">the game object whose EdgeRef is checked</param>
        /// <param name="edge">the edge referenced by the attached EdgeRef; defined only if this method
        /// returns true</param>
        /// <returns>true if <paramref name="gameObject"/> has an <see cref="EdgeRef"/>
        /// component attached to it that is not null</returns>
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
        /// <param name="gameObject">the game object whose graph is requested</param>
        /// <returns>the correponding graph</returns>
        public static Graph ItsGraph(this GameObject gameObject)
        {
            return gameObject.GetNode().ItsGraph;
        }

        /// <summary>
        /// Enables/disables the renderers of <paramref name="gameObject"/> and all its
        /// descendants so that they become visible/invisible.
        /// </summary>
        /// <param name="gameObject">objects whose renderer (and those of its children) is to be enabled/disabled</param>
        /// <param name="isVisible">iff true, the renderers will be enabled</param>
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
        /// <param name="gameObject">game object for which to retrieve the full name</param>
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
        /// <param name="rootNode">the root of the node hierarchy to be collected</param>
        /// <returns>all descendants of <paramref name="rootNode"/> including <paramref name="rootNode"/></returns>
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
        /// <param name="root">the root of the game-object hierarchy to be collected</param>
        /// <param name="result">where to add the descendants</param>
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
        /// <param name="gameObject">game object representing an edge</param>
        /// <returns>the game object representing the source of this edge</returns>
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
        /// <param name="gameObject">game object representing an edge</param>
        /// <returns>the game object representing the target of this edge</returns>
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
        /// <param name="gameObject">The game object whose portal shall be updated</param>
        /// <param name="warnOnFailure">
        /// Whether a warning log message shall be emitted if the <paramref name="gameObject"/>
        /// is not attached to any code city
        /// </param>
        /// <param name="includeDescendants">
        /// Whether the portal of the descendants of this <paramref name="gameObject"/> shall be updated too
        /// </param>
        public static void UpdatePortal(this GameObject gameObject, bool warnOnFailure = false,
                                        Portal.IncludeDescendants includeDescendants = OnlySelf)
        {
            GameObject rootCity = SceneQueries.GetCodeCity(gameObject.transform)?.gameObject;
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
        /// <param name="gameObject">object whose child is to be enabled/disabled</param>
        /// <param name="childName">the name of the child; may be a composite name</param>
        /// <param name="active">whether to enable it</param>
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
        /// Overlap is checked based on the <c>Collider</c> components. Objects with no <c>Collider</c>
        /// component and inactive nodes are ignored.
        /// </para>
        /// </summary>
        /// <remarks>
        /// The <paramref name="gameObject"/> must be a node, i.e., coantain a <c>NodeRef</c> component.
        /// </remarks>
        /// <param name="gameObject">The game object whose operator to retrieve.</param>
        /// <returns><c>false</c> if <paramref name="gameObject"/> does not have a <c>Collider</c> component,
        /// or does not overlap with its siblings.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the object the method is called on is not a node, i.e., has no <c>NodeRef</c>
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
                if (sibling.gameObject == gameObject || !sibling.gameObject.IsNodeAndActiveSelf() || !sibling.gameObject.TryGetComponent(out Collider siblingCollider))
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
        /// <param name="gameObject">The game object whose chidlren should be examined.</param>
        /// <param name="prefix">The prefix to search for.</param>
        /// <returns>the found child or null.</returns>
        public static Transform FindChildWithPrefix(this GameObject gameObject, string prefix)
        {
            foreach (Transform child in gameObject.transform)
            {
                if (child.name.StartsWith(prefix))
                {
                    return child;
                }
            }
            return null;
        }
    }
}
