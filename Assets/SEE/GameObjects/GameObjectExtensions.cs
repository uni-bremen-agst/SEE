using SEE.DataModel.DG;
using SEE.Utils;
using System;
using OdinSerializer.Utilities;
using UnityEngine;

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
                    return edgeRef.edge.ID;
                }
            }
            return nodeRef.Value.ID;
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
        /// Sets the visibility of this <paramref name="gameObject"/> to <paramref name="show"/>.
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
            return node.transform.position.y + node.Size().y / 2.0f;
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
            return gameObject.TryGetComponent<NodeRef>(out NodeRef _);
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
            return gameObject.TryGetComponent<EdgeRef>(out EdgeRef _);
        }

        /// <summary>
        /// Returns the graph node represented by this <paramref name="gameObject"/>.
        /// 
        /// Precondition: <paramref name="gameObject"/> must have a <see cref="NodeRef"/>
        /// attached to it referring to a valid node; if not, an exception is raised.
        /// </summary>
        /// <param name="gameObject">the game object whose Node is requested</param>
        /// <returns>the correponding graph node</returns>
        public static Node GetNode(this GameObject gameObject)
        {
            if (gameObject.TryGetComponent<NodeRef>(out NodeRef nodeRef))
            {
                if (nodeRef != null)
                {
                    return nodeRef.Value;
                }
                else
                {
                    throw new Exception($"Node reference of game object {gameObject.name} is null");
                }
            }
            else
            {
                throw new Exception($"Game object {gameObject.name} has no NodeRef");
            }
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
    }
}
