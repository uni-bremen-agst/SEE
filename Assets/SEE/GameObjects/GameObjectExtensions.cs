using SEE.DataModel.DG;
using System;
using OdinSerializer.Utilities;
using UnityEngine;
using System.Collections.Generic;
using SEE.DataModel;
using SEE.Game;

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
        public static SEECity ContainingCity(this GameObject gameObject)
        {
            if (gameObject == null || (!gameObject.HasNodeRef() && !gameObject.HasEdgeRef()))
            {
                return null;
            }
            else
            {
                Transform codeCityObject = SceneQueries.GetCodeCity(gameObject.transform);
                if (codeCityObject != null && codeCityObject.gameObject.TryGetComponent(out SEECity city))
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
        /// <param name="tag">tag the ancestors must have</param>
        /// <returns>all transitive children with <paramref name="tag"/></returns>
        public static List<GameObject> AllAncestors(this GameObject gameObject, string tag)
        {
            List<GameObject> result = new List<GameObject>();
            if (gameObject.CompareTag(tag))
            {
                result.Add(gameObject);
            }
            foreach (Transform child in gameObject.transform)
            {
                result.AddRange(child.gameObject.AllAncestors(tag));
            }
            return result;
        }

        /// <summary>
        /// Returns the render-queue offset of given <paramref name="gameObject"/>.
        /// 
        /// Precondition: <paramref name="gameObject"/> must have a renderer attached
        /// to it; otherwise 0 will be returned.
        /// </summary>
        /// <param name="gameObject">objects whose render-queue is requested</param>
        /// <returns>render-queue offset</returns>
        public static int GetRenderQueue(this GameObject gameObject)
        {
            if (gameObject.TryGetComponent(out Renderer renderer))
            {
                return renderer.sharedMaterial.renderQueue;
            }

            Debug.LogWarningFormat("GetRenderQueue: Game object {0} has no renderer.\n", gameObject.name);
            return 0;
        }

        /// <summary>
        /// Sets the color for this <paramref name="gameObject"/> to given <paramref name="color"/>.
        /// 
        /// Precondition: <paramref name="gameObject"/> has a renderer whose material has attribute _Color.
        /// </summary>
        /// <param name="gameObject">objects whose color is to be set</param>
        /// <param name="color">the new color to be set</param>
        public static void SetColor(this GameObject gameObject, Color color)
        {
            if (gameObject.TryGetComponent(out Renderer renderer))
            {
                Material material = renderer.sharedMaterial;
                material.SetColor("_Color", color);
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
        /// Gets the Height (Roof) of this <paramref name="node"/>
        /// </summary>
        /// <param name="node">node whose height has to be determined</param>
        /// <returns>The height of the Roof from this <paramref name="node"/></returns>
        public static float GetRoof(this GameObject node)
        {
            return node.transform.position.y + node.WorldSpaceScale().y / 2.0f;
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
        /// Returns true if <paramref name="gameObject"/> has a <see cref="NodeRef"/>
        /// component attached to it.
        /// </summary>
        /// <param name="gameObject">the game object whose NodeRef is checked</param>
        /// <returns>true if <paramref name="gameObject"/> has a <see cref="NodeRef"/>
        /// component attached to it</returns>
        public static bool HasNodeRef(this GameObject gameObject)
        {
            return gameObject.TryGetComponent(out NodeRef _);
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
            if (gameObject.TryGetComponent<NodeRef>(out NodeRef nodeRef))
            {
                if (nodeRef != null)
                {
                    if (nodeRef.Value != null)
                    {
                        return nodeRef.Value;
                    }
                    else
                    {
                        throw new Exception($"Node referenced by game object {gameObject.name} is null.");
                    }
                }
                else
                {
                    throw new Exception($"Node reference of game object {gameObject.name} is null.");
                }
            }
            else
            {
                throw new Exception($"Game object {gameObject.name} has no NodeRef.");
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
        /// Returns the full name of given <paramref name="gameObject"/>.
        /// The full name is the concatenation of all names of the ancestors of <paramref name="gameObject"/>
        /// separated by a period. E.g., if <paramref name="gameObject"/> has name C and its parent
        /// has name B and the parent's parent has name A, then the result will be A.B.C.        
        /// </summary>
        /// <param name="gameObject">the gameObject whose full name is to be retrieved</param>
        /// <returns>full name</returns>
        public static string FullName(this GameObject gameObject)
        {
            if (gameObject == null)
            {
                return "";
            }
            else
            {
                Transform parent = gameObject.transform.parent;
                if (parent != null)
                {
                    return FullName(parent.gameObject) + "." + gameObject.name;
                }
                else
                {
                    return gameObject.name;
                }
            }
        }

        /// <summary>
        /// Returns all ancestors of given <paramref name="rootNode"/> tagged by <see cref="Tags.Node"/>
        /// including <paramref name="rootNode"/> itself.
        /// </summary>
        /// <param name="rootNode">the root of the node hierarchy to be collected</param>
        /// <returns>all ancestors of <paramref name="rootNode"/> including <paramref name="rootNode"/></returns>
        public static IList<GameObject> AllAncestors(this GameObject rootNode)
        {
            IList<GameObject> result = new List<GameObject>() { rootNode };
            AllAncestors(rootNode, result);
            return result;
        }

        /// <summary>
        /// Adds all ancestors of <paramref name="root"/> to <paramref name="result"/>
        /// (only if tagged by <see cref="Tags.Node"/>).
        /// 
        /// Note: <paramref name="root"/> is assumed to be contained in <paramref name="result"/>
        /// already.
        /// </summary>
        /// <param name="root">the root of the game-object hierarchy to be collected</param>
        /// <param name="result">where to add the ancestors</param>
        private static void AllAncestors(GameObject root, IList<GameObject> result)
        {
            foreach (Transform child in root.transform)
            {
                if (child.gameObject.CompareTag(Tags.Node))
                {
                    result.Add(child.gameObject);
                    AllAncestors(child.gameObject, result);
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
    }
}
