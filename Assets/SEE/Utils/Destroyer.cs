using SEE.Game;
using SEE.GO;
using UnityEngine;

namespace SEE.Utils
{
    /// <summary>
    /// Functions to destroy game objects in game or editor mode.
    /// They will also maintain the consistency of <see cref="GraphElementIDMap"/>.
    /// </summary>
    public static class Destroyer
    {
        /// <summary>
        /// Destroys given <paramref name="gameObject"/> using <see cref="UnityEngine.Object.Destroy(Object)"/>
        /// when in play mode or <see cref="UnityEngine.Object.DestroyImmediate(Object)"/> when in editor mode.
        /// Children of <paramref name="gameObject"/> in the node hierarchy are not effected.
        /// If <paramref name="gameObject"/> has a <see cref="NodeRef"/> attached to it, i.e.,
        /// represents a node, its incoming and outgoing edges are destroyed, too.
        /// The <paramref name="gameObject"/> including its incoming and outgoing edges (if any)
        /// are removed from <see cref="GraphElementIDMap"/>.
        /// </summary>
        /// <param name="gameObject">game object to be destroyed (generally, but not necessarily
        /// representing a node or edge)</param>
        public static void DestroyGameObject(GameObject gameObject)
        {
            if (gameObject != null)
            {
                if (gameObject.IsNode() || gameObject.IsEdge())
                {
                    GraphElementIDMap.Remove(gameObject);
                }
                gameObject.transform.SetParent(null);
                // We must use DestroyImmediate when we are in the editor mode.
                if (Application.isPlaying)
                {
                    // playing either in a built player or in the player of the editor
                    Object.Destroy(gameObject);
                }
                else
                {
                    // game is not played; we are in the editor mode
                    Object.DestroyImmediate(gameObject);
                }
            }
        }

        /// <summary>
        /// Destroys given <paramref name="component"/> using <see cref="UnityEngine.Object.Destroy(Object)"/>
        /// when in play mode or <see cref="UnityEngine.Object.DestroyImmediate(Object)"/> when in editor mode.
        /// </summary>
        /// <param name="component">component to be destroyed</param>
        public static void DestroyComponent(Component component)
        {
            if (component != null)
            {
                // We must use DestroyImmediate when we are in the editor mode.
                if (Application.isPlaying)
                {
                    // playing either in a built player or in the player of the editor
                    Object.Destroy(component);
                }
                else
                {
                    // game is not played; we are in the editor mode
                    Object.DestroyImmediate(component);
                }
            }
        }

        /// <summary>
        /// Destroys given <paramref name="gameObject"/> with all its incoming and outgoing
        /// edges and recursively all its ancestors (including their edges). This method is
        /// intended to be used for game objects representing graph nodes having a component
        /// <see cref=">SEE.DataModel.NodeRef"/>. All destroyed game objects will also be
        /// removed from <see cref="GraphElementIDMap"/>.
        ///
        /// If the <paramref name="gameObject"/> does not have a
        /// <see cref=">SEE.DataModel.NodeRef"/> component, nothing happens.
        /// </summary>
        /// <param name="gameObject">game object to be destroyed</param>
        public static void DestroyGameObjectWithChildren(GameObject gameObject)
        {
            if (gameObject.TryGetComponent(out NodeRef _))
            {
                foreach (Transform child in gameObject.transform)
                {
                    DestroyGameObjectWithChildren(child.gameObject);
                }
                DestroyEdges(gameObject);
                DestroyGameObject(gameObject);
            }
        }

        /// <summary>
        /// Destroys the game objects of all incoming and outgoing edges of
        /// given <paramref name="gameObject"/>. All destroyed game objects will also be
        /// removed from <see cref="GraphElementIDMap"/>.
        ///
        /// This method is intended for game objects representing graph nodes
        /// having a component <see cref=">SEE.DataModel.NodeRef"/>. If the
        /// <paramref name="gameObject"/> does not have a <see cref=">SEE.DataModel.NodeRef"/>
        /// component, nothing happens.
        /// </summary>
        /// <param name="gameObject">game node whose incoming and outgoing edges are to be destroyed</param>
        private static void DestroyEdges(GameObject gameObject)
        {
            if (gameObject.TryGetComponent(out NodeRef nodeRef))
            {
                foreach (string ID in nodeRef.GetIdsOfIncomingOutgoingEdges())
                {
                    GameObject edge = GraphElementIDMap.Find(ID);
                    if (edge != null)
                    {
                        Destroyer.DestroyGameObject(edge);
                    }
                }
            }
        }
    }
}