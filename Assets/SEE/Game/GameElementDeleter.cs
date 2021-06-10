using SEE.DataModel;
using SEE.GO;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.Game
{
    /// <summary>
    /// Allows to delete nodes and edges.
    /// </summary>
    class GameElementDeleter
    {
        /// <summary>
        /// Deletes given <paramref GameObject="selectedObject"/> assumed to be either an
        /// edge or node. If it represents a node, the incoming and outgoing edges and
        /// its ancestors will be removed, too. For the possibility of an undo, the deleted objects will be saved.
        ///
        /// Precondition: <paramref name="deletedObject"/> != null.
        /// </summary>
        /// <param name="deletedObject">selected GameObject that along with its children should be removed</param>
        /// <returns>true if <paramref name="deletedObject"/> was actually deleted</returns>
        public static ISet<GameObject> Delete(GameObject deletedObject)
        {
            if (deletedObject.CompareTag(Tags.Edge))
            {
                ISet<GameObject> result = new HashSet<GameObject>() { deletedObject };
                Delete(result);
                return result;
            }
            else if (deletedObject.CompareTag(Tags.Node))
            {
                if (deletedObject.GetNode().IsRoot())
                {
                    // FIXME: We need to throw an exception or show a user notification.
                    throw new Exception("A root shall not be deleted.\n");
                }
                else
                {
                    // The selectedObject (a node) and its ancestors are not deleted immediately. Instead we
                    // will run an animation that moves them into a garbage bin. Only when they arrive there,
                    // we will actually delete them.
                    return DeleteTree(deletedObject);
                }
            }
            else
            {
                throw new ArgumentException($"Game object {deletedObject.name} must be a node or edge.");
            }
        }

        private static ISet<GameObject> DeleteTree(GameObject deletedObject)
        {
            IList<GameObject> ancestors = deletedObject.AllAncestors();
            ISet<GameObject> result = new HashSet<GameObject>(ancestors);

            // FIXME: This may be an expensive operation as it iterates over all game objects in the scene.
            // Note: FindGameObjectsWithTag retrieves only active objects. As we set all deleted objects
            // inactive, those objects will not be retrieved here.
            Dictionary<string, GameObject> idToEdge = new Dictionary<string, GameObject>();
            foreach (GameObject edge in GameObject.FindGameObjectsWithTag(Tags.Edge))
            {
                idToEdge[edge.name] = edge;
            }

            /// Adds all edges having a source or target node in the set of ancestors
            /// to <see cref="implicitlyDeletedNodesAndEdges"/>.
            foreach (GameObject ancestor in ancestors)
            {
                if (ancestor.TryGetComponentOrLog(out NodeRef nodeRef))
                {
                    foreach (string edgeID in nodeRef.GetEdgeIds())
                    {
                        // ancestor may have an edge that is currently inactive because it
                        // was deleted, in which case it does not show up in idToEdge.
                        if (idToEdge.TryGetValue(edgeID, out GameObject edge))
                        {
                            result.Add(edge);
                        }
                    }
                }
            }
            Delete(result);
            return result;
        }

        private static void SetInactive(GameObject gameObject)
        {
            gameObject.SetActive(false);
        }

        private static void SetActive(GameObject gameObject)
        {
            gameObject.SetActive(true);
        }

        public static void Delete(ISet<GameObject> implicitlyDeletedNodesAndEdges)
        {
            foreach (GameObject nodeOrEdge in implicitlyDeletedNodesAndEdges)
            {
                GameObjectFader.FadeOut(nodeOrEdge, SetInactive);
            }

        }

        public static void Revive(ISet<GameObject> implicitlyDeletedNodesAndEdges)
        {
            foreach (GameObject nodeOrEdge in implicitlyDeletedNodesAndEdges)
            {
                SetActive(nodeOrEdge);
                GameObjectFader.FadeIn(nodeOrEdge, null);
            }
        }
    }
}
