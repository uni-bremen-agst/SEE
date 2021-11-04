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
        /// <param name="gameNode">game object whose graph node is to be removed from the graph</param>
        public static void RemoveNodeFromGraph(GameObject gameNode)
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
        /// fade out and eventually be set inactive. However, their corresponding graph node
        /// or edge, respectively, are removed from their graph.
        ///
        /// Precondition: <paramref name="deletedObject"/> != null.
        /// </summary>
        /// <param name="deletedObject">the game object that along with its descendents and
        /// their edges should be removed</param>
        /// <returns>the graph from which <paramref name="deletedObject"/> were removed
        /// along with all descendents of <paramref name="deletedObject"/> and their incoming
        /// and outgoing edges marked as deleted along with <paramref name="deletedObject"/></returns>
        /// <exception cref="Exception">thrown if <paramref name="deletedObject"/> is a root</exception>
        /// <exception cref="ArgumentException">thrown if <paramref name="deletedObject"/> is
        /// neither a node nor an edge</exception>
        public static (GraphElementsMemento, ISet<GameObject>) Delete(GameObject deletedObject)
        {
            if (deletedObject.CompareTag(Tags.Edge))
            {
                return DeleteEdge(deletedObject);
            }
            else if (deletedObject.CompareTag(Tags.Node))
            {
                Node deletedNode = deletedObject.GetNode();
                if (deletedNode.IsRoot())
                {
                    throw new Exception("A root shall not be deleted.\n");
                }
                else
                {
                    MorphParentIfNecessary(deletedObject);
                    // Note: DeleteTree(deletedObject) assumes that the nodes are still
                    // in the underlying graph, that is why we must call deletedNode.DeleteTree()
                    // after DeleteTree(deletedObject).
                    ISet<GameObject> deletedGameObjects = DeleteTree(deletedObject);
                    SubgraphMemento subgraphMemento = deletedNode.DeleteTree();
                    return (subgraphMemento, deletedGameObjects);
                }
            }
            else
            {
                throw new ArgumentException($"Game object {deletedObject.name} must be a node or edge.");
            }
        }

        /// <summary>
        /// Deletes the given <paramref name="gameEdge"/>, that is, removes the associated
        /// graph edge from its graph, lets <paramref name="gameEdge"/> fading out, and
        /// finally sets it inactive. The result is the memorized removed associated
        /// graph edge and <paramref name="gameEdge"/> itself.
        ///
        /// Precondition: <paramref name="gameEdge"/> denotes a game edge.
        /// </summary>
        /// <param name="gameEdge">a game object representing an edge</param>
        /// <returns>memento memorizing the deleted graph edge along with the deleted
        /// <paramref name="gameEdge"/></returns>
        /// <exception cref="Exception">thrown if <paramref name="gameEdge"/> has no valid
        /// edge reference</exception>
        private static (GraphElementsMemento, ISet<GameObject>) DeleteEdge(GameObject gameEdge)
        {
            if (gameEdge.TryGetComponent(out EdgeRef edgeRef) && edgeRef.Value != null)
            {
                // The edge memento must be created before the edge is removed;
                // otherwise ItsGraph would be null.
                EdgeMemento edgeMemento = new EdgeMemento(edgeRef.Value);
                edgeRef.Value.ItsGraph.RemoveEdge(edgeRef.Value);
                GameObjectFader.FadingOut(gameEdge, SetInactive);
                return (edgeMemento, new HashSet<GameObject>() { gameEdge });
            }
            else
            {
                throw new Exception($"Edge {gameEdge.name} to be deleted has no valid edge reference.");
            }
        }

        /// <summary>
        /// In case <paramref name="gameNode"/> is a child of a node with only single child, that is,
        /// if <paramref name="gameNode"/> is its only child, the parent of <paramref name="gameNode"/>
        /// will be morphed into a leaf node.
        /// </summary>
        /// <param name="gameNode">game node whose parent is possibly to be morphed into a leaf</param>
        private static void MorphParentIfNecessary(GameObject gameNode)
        {
            Node node = gameNode.GetNode();
            if (node.Parent != null && node.Parent.NumberOfChildren() == 1)
            {
                // node is a single child; removing it turns its parent into a leaf
                GameObject parent = gameNode.transform.parent?.gameObject;
                if (parent != null)
                {
                    SEECity city = parent.ContainingCity();
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
        /// contained in this tree are the transitive descendants of <paramref name="root"/>.
        /// The edges in this tree are those whose source or target node
        /// is contained in the tree.
        ///
        /// Note: The result consists of both nodes and edges.
        ///
        /// Precondition: <paramref name="root"/> is a game node.
        /// </summary>
        /// <param name="root">the root of the tree to be deleted</param>
        /// <returns>all descendants of <paramref name="root"/> and their incoming
        /// and outgoing edges</returns>
        private static ISet<GameObject> DeleteTree(GameObject root)
        {
            /// The descendants of <see cref="root"/> need to be removed.
            IList<GameObject> descendants = root.AllDescendants();
            ISet<GameObject> result = new HashSet<GameObject>(descendants);

            // FIXME: This may be an expensive operation as it iterates over all game objects in the scene.
            // Note: FindGameObjectsWithTag retrieves only active objects. As we set all deleted objects
            // inactive, those objects will not be retrieved here.
            Dictionary<string, GameObject> idToEdge = new Dictionary<string, GameObject>();
            foreach (GameObject edge in GameObject.FindGameObjectsWithTag(Tags.Edge))
            {
                Debug.Log($"Scene edge {edge.name}.\n");
                idToEdge[edge.name] = edge;
            }

            /// Adds all edges having a source or target node in the set of descendants
            /// to result.
            foreach (GameObject descendant in descendants)
            {
                Debug.Log($"Handling descendant {descendant.name}.\n");
                if (descendant.TryGetComponentOrLog(out NodeRef nodeRef))
                {
                    Debug.Log($"Descendant {descendant.name} has a node reference.\n");
                    foreach (string edgeID in nodeRef.GetEdgeIds())
                    {
                        Debug.Log($"Looking for edge {edgeID}.\n");
                        // descendant may have an edge that is currently inactive because it
                        // was deleted, in which case it does not show up in idToEdge.
                        if (idToEdge.TryGetValue(edgeID, out GameObject edge))
                        {
                            Debug.Log($"Found edge {edgeID}.\n");
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
        /// Note: The objects are not actually destroyed, neither are they removed
        /// from their graph.
        /// </summary>
        /// <param name="nodesOrEdges">nodes and/or edges to be marked as deleted</param>
        private static void Delete(ISet<GameObject> nodesOrEdges)
        {
            foreach (GameObject nodeOrEdge in nodesOrEdges)
            {
                Debug.Log($"Deleting {nodeOrEdge.name}.\n");
                GameObjectFader.FadingOut(nodeOrEdge, SetInactive);
            }
        }

        /// <summary>
        /// Marks all elements in <paramref name="nodesOrEdges"/> by way of
        /// <see cref="GameObjectFader"/> as alive again. That means to let
        /// them be set active, fade in and blink. All <paramref name="nodesOrEdges"/>
        /// are re-added to <paramref name="graph"/>.
        ///
        /// Assumption: The objects were set inactive.
        /// </summary>
        /// <param name="graph">the graph to which all <paramref name="nodesOrEdges"/> need to be
        /// re-added</param>
        /// <param name="nodesOrEdges">nodes and edge to be marked as alive again</param>
        public static void Revive(GraphElementsMemento graph, ISet<GameObject> nodesOrEdges)
        {
            graph.Restore();
            foreach (GameObject nodeOrEdge in nodesOrEdges)
            {
                SetActive(nodeOrEdge);
                GameObjectFader.FadingIn(nodeOrEdge, null);
            }
        }
    }
}
