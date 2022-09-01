using System;
using System.Collections.Generic;
using Sirenix.Serialization.Utilities;
using SEE.DataModel;
using SEE.DataModel.DG;
using SEE.Game;
using SEE.Game.City;
using SEE.Utils;
using UnityEngine;
using static SEE.Game.Portal.IncludeDescendants;
using Sirenix.Utilities;

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
        /// Precondition: <paramref name="gameNode"/> has a NodeRef component attached to it
        /// that is a valid graph node reference.
        /// </summary>
        /// <param name="gameNode">game object representing a Node to be queried whether it is a leaf</param>
        /// <returns>true if <paramref name="gameNode"/> represents a leaf in the graph</returns>
        public static bool IsLeaf(this GameObject gameNode)
        {
            return gameNode.GetComponent<NodeRef>()?.Value?.IsLeaf() ?? false;
        }

        /// <summary>
        /// Returns all transitive children of <paramref name="gameObject"/> tagged by
        /// given <paramref name="tag"/> (including <paramref name="gameObject"/> itself).
        /// </summary>
        /// <param name="tag">tag the descendants must have</param>
        /// <returns>all transitive children with <paramref name="tag"/></returns>
        public static List<GameObject> AllDescendants(this GameObject gameObject, string tag)
        {
            List<GameObject> result = new List<GameObject>();
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
            List<GameObject> result = new List<GameObject>();
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
            if (gameObject.TryGetComponent(out Renderer renderer))
            {
                return renderer.sharedMaterial.color;
            }

            throw new InvalidOperationException($"GameObject {gameObject.name} has no renderer component.");
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
        /// Sets the scale of this <paramref name="node"/> to <paramref name="scale"/> independent from
        /// the local scale from the parent.
        /// </summary>
        /// <param name="node">object whose scale should be set</param>
        /// <param name="scale">the new scale in world space</param>
        public static void SetScale(this GameObject node, Vector3 scale)
        {
            Transform parent = node.transform.parent;
            node.transform.parent = null;
            node.transform.localScale = scale;
            node.transform.parent = parent;
        }

        /// <summary>
        /// Returns the world-space position of the roof of this <paramref name="gameObject"/>.
        /// </summary>
        /// <param name="gameObject">game object whose roof has to be determined</param>
        /// <returns>world-space position of the roof of this <paramref name="gameObject"/></returns>
        public static float GetRoof(this GameObject gameObject)
        {
            return gameObject.transform.position.y + gameObject.WorldSpaceScale().y / 2.0f;
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
        /// <returns>world-space position of the roof of this <paramref name="gameObject"/>
        /// or any of its active descendants</returns>
        public static float GetMaxY(this GameObject gameObject)
        {
            float result = float.NegativeInfinity;
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
                    if (child.gameObject.activeInHierarchy)
                    {
                        Recurse(child.gameObject, ref max);
                    }
                }
            }
        }

        /// <summary>
        /// Returns the size of the given <paramref name="gameObject"/> in world space.
        /// </summary>
        /// <param name="gameObject">object whose size is requested</param>
        /// <returns>size of given <paramref name="gameObject"/></returns>
        public static Vector3 WorldSpaceScale(this GameObject gameObject)
        {
            // For some objects, such as capsules, lossyScale gives wrong results.
            // The more reliable option to determine the size is using the
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
                               + $"on game object '{gameObject.name}'.\n");
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
            return gameObject.GetComponent<T>() ?? gameObject.AddComponent<T>();
        }

        /// <summary>
        /// Tries to get the component of the given type <typeparamref name="T"/> of this <paramref name="gameObject"/>.
        /// If the component was found, it will be stored in <paramref name="component"/>.
        /// If it wasn't found, <paramref name="component"/> will be <code>null</code> and
        /// <see cref="InvalidOperationException"/> will be thrown.
        /// </summary>
        /// <param name="gameObject">The game object the component should be gotten from. Must not be null.</param>
        /// <param name="component">The variable in which to save the component.</param>
        /// <typeparam name="T">The type of the component.</typeparam>
        /// <exception cref="InvalidOperationException">thrown if <paramref name="gameObject"/> has no
        /// component of type <typeparamref name="T"/></exception>
        public static void MustGetComponent<T>(this GameObject gameObject, out T component)
        {
            if (!gameObject.TryGetComponent(out component))
            {
                throw new InvalidOperationException($"Couldn't find component '{typeof(T).GetNiceName()}' on game object '{gameObject.name}'");
            }
        }

        /// <summary>
        /// Returns true if <paramref name="gameObject"/> has a <see cref="NodeRef"/>
        /// component attached to it that is actually referring to a valid node
        /// (i.e., its Value is not null).
        /// </summary>
        /// <param name="gameObject">the game object whose NodeRef is checked</param>
        /// <returns>true if <paramref name="gameObject"/> has a <see cref="NodeRef"/>
        /// component attached to it</returns>
        public static bool HasNodeRef(this GameObject gameObject)
        {
            return gameObject.TryGetComponent(out NodeRef nodeRef) && nodeRef.Value != null;
        }

        /// <summary>
        /// Returns true if <paramref name="gameObject"/> has a <see cref="NodeRef"/>
        /// component attached to it.
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
        /// Returns the graph node represented by this <paramref name="gameObject"/>.
        ///
        /// Precondition: <paramref name="gameObject"/> must have a <see cref="NodeRef"/>
        /// attached to it referring to a valid node; if not, an exception is raised.
        /// </summary>
        /// <param name="gameObject">the game object whose Node is requested</param>
        /// <returns>the correponding graph node (will never be null)</returns>
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
        /// component attached to it.
        /// </summary>
        /// <param name="gameObject">the game object whose EdgeRef is checked</param>
        /// <returns>true if <paramref name="gameObject"/> has an <see cref="EdgeRef"/>
        /// component attached to it</returns>
        public static bool HasEdgeRef(this GameObject gameObject)
        {
            return gameObject.TryGetComponent(out EdgeRef _);
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
                return SceneQueries.RetrieveGameNode(edgeRef.SourceNodeID);
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
                return SceneQueries.RetrieveGameNode(edgeRef.SourceNodeID);
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
                                        Portal.IncludeDescendants includeDescendants = ONLY_SELF)
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
    }
}