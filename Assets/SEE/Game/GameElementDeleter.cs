using SEE.DataModel;
using SEE.DataModel.DG;
using SEE.GO;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.Game
{
    /// <summary>
    /// Allows to delete nodes and edges.
    /// </summary>
    internal class GameElementDeleter
    {
        /// <summary>
        /// Removes the graph node associated with the given <paramref name="gameNode"/>
        /// from its graph. <paramref name="gameNode"/> is not actually destroyed.
        ///
        /// Precondition: <paramref name="gameNode"/> must have a valid NodeRef; otherwise
        /// an exception will be thrown.
        /// </summary>
        /// <param name="gameNode">game node whose graph node is to be removed from the graph</param>
        public static void RemoveFromGraph(GameObject gameNode)
        {
            Node node = gameNode.GetNode();
            Graph graph = node.ItsGraph;
            graph.RemoveNode(node);
        }

        /// <summary>
        /// Deletes given  <paramref name="deletedObject"/> assumed to be either an
        /// edge or node. If it represents a node, its ancestors and any edges related
        /// to those will be removed, too. If it is an edge, only that edge is deleted.
        ///
        /// Note: The objects are not actually destroyed. They are just marked as deleted
        /// by way of <see cref="GameObjectFader"/>. That means to let them blink and
        /// fade out and eventually be set inactive.
        ///
        /// Precondition: <paramref name="deletedObject"/> != null.
        /// </summary>
        /// <param name="deletedObject">the game object that along with its ancestors and
        /// their edges should be removed</param>
        /// <returns>all ancestors of <paramref name="deletedObject"/> and their incoming
        /// and outgoing edges marked as deleted along with <paramref name="deletedObject"/></returns>
        /// <exception cref="Exception">thrown if <paramref name="deletedObject"/> is a root</exception>
        /// <exception cref="ArgumentException">thrown if <paramref name="deletedObject"/> is
        /// neither a node nor an edge</exception>
        public static ISet<GameObject> Delete(GameObject deletedObject)
        {
            Debug.Log($"GameElementDeleter.Delete({deletedObject.name})\n");
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
                    throw new Exception("A root shall not be deleted.\n");
                }
                else
                {
                    MorphGameNodeIfNecessary(deletedObject);
                    return DeleteTree(deletedObject);
                }
            }
            else
            {
                throw new ArgumentException($"Game object {deletedObject.name} must be a node or edge.");
            }
        }

        private static void MorphGameNodeIfNecessary(GameObject gameNode)
        {
            // FIXME: The graph nodes are not actually removed from the graph by Delete.
            Node node = gameNode.GetNode();
            Debug.Log($"GameElementDeleter.MorphGameNodeIfNecessary: parent has {node.Parent.NumberOfChildren()} children\n");
            if (node.Parent != null && node.Parent.NumberOfChildren() == 1)
            {
                // node is a single child; removing it turns its parent into a leaf
                GameObject parent = gameNode.transform.parent?.gameObject;
                if (parent != null)
                {
                    SEECity city = parent.gameObject.ContainingCity();
                    if (city != null)
                    {
                        city.Renderer.RedrawAsLeafNode(parent);
                    }
                    else
                    {
                        throw new Exception($"Parent node {parent.name} is not contained in a code city.");
                    }
                }
            }
        }

        /// <summary>
        /// Deletes and returns all game objects representing nodes or edges
        /// of the node tree rooted by <paramref name="root"/>. The nodes
        /// contained in this tree are the transitive ancestors of <paramref name="root"/>.
        /// The edges in this tree are those whose source or target node
        /// is contained in the tree.
        ///
        /// Note: The result consists of both nodes and edges.
        /// </summary>
        /// <param name="root">the root of the tree to be deleted</param>
        /// <returns>all ancestors of <paramref name="root"/> and their incoming
        /// and outgoing edges</returns>
        private static ISet<GameObject> DeleteTree(GameObject root)
        {
            IList<GameObject> ancestors = root.AllAncestors();
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

        /// <summary>
        /// Sets <paramref name="gameObject"/> inactive.
        /// </summary>
        /// <param name="gameObject">object to be set inactive</param>
        private static void SetInactive(GameObject gameObject)
        {
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Sets <paramref name="gameObject"/> active.
        /// </summary>
        /// <param name="gameObject">object to be set active</param>
        private static void SetActive(GameObject gameObject)
        {
            gameObject.SetActive(true);
        }

        /// <summary>
        /// Marks all elements in <paramref name="nodesOrEdges"/> by way of
        /// <see cref="GameObjectFader"/> as deleted. That means to let
        /// them blink and fade out and eventually be set inactive.
        ///
        /// Note: The objects are not actually destroyed.
        /// </summary>
        /// <param name="nodesOrEdges">nodes and edge to be marked as deleted</param>
        public static void Delete(ISet<GameObject> nodesOrEdges)
        {
            foreach (GameObject nodeOrEdge in nodesOrEdges)
            {
                GameObjectFader.FadingOut(nodeOrEdge, SetInactive);
            }
        }

        /// <summary>
        /// Marks all elements in <paramref name="nodesOrEdges"/> by way of
        /// <see cref="GameObjectFader"/> as alive again. That means to let
        /// them be set active, fade in and blink.
        ///
        /// Assumption: The objects were set inactive.
        /// </summary>
        /// <param name="nodesOrEdges">nodes and edge to be marked as alive again</param>
        public static void Revive(ISet<GameObject> implicitlyDeletedNodesAndEdges)
        {
            foreach (GameObject nodeOrEdge in implicitlyDeletedNodesAndEdges)
            {
                SetActive(nodeOrEdge);
                GameObjectFader.FadingIn(nodeOrEdge, null);
            }
        }
    }
}
