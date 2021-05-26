using SEE.DataModel;
using SEE.DataModel.DG;
using SEE.Game;
using SEE.GO;
using SEE.Net;
using SEE.Utils;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace SEE.Controls.Actions
{
    /// <summary>
    /// Action to delete the currently selected game object (edge or node).
    /// </summary>
    internal class DeleteAction : AbstractPlayerAction
    {
        /// <summary>
        /// Returns a new instance of <see cref="DeleteAction"/>.
        /// </summary>
        /// <returns>new instance</returns>
        public static ReversibleAction CreateReversibleAction()
        {
            return new DeleteAction();
        }

        /// <summary>
        /// Returns a new instance of <see cref="DeleteAction"/>.
        /// </summary>
        /// <returns>new instance</returns>
        public override ReversibleAction NewInstance()
        {
            return CreateReversibleAction();
        }

        /// <summary>
        /// Contains all nodes and edges deleted as explicitly requested by the user.
        /// As a consequence of deleting a node, its ancestors along with their incoming and outgoing
        /// edges may be deleted implicitly, too. All of these are kept in <see cref="deletedNodes"/>
        /// and <see cref="deletedEdges"/>. Yet, if we need to redo a deletion, we need to remember
        /// the explicitly deleted objects.
        /// </summary>
        private ISet<GameObject> explicitlyDeletedNodesAndEdges = new HashSet<GameObject>();

        /// <summary>
        /// A history of all nodes and the graph they were attached to, deleted by this action.
        /// </summary>
        private Dictionary<GameObject, Graph> deletedNodes { get; set; } = new Dictionary<GameObject, Graph>();

        /// <summary>
        /// A history of all edges and the graph they were attached to, deleted by this action.
        /// </summary>
        private Dictionary<GameObject, Graph> deletedEdges { get; set; } = new Dictionary<GameObject, Graph>();

        /// <summary>
        /// A data structure containing the graph's root of a SEEcity and its graph. 
        /// </summary>
        private Dictionary<GameObject, Graph> roots { get; set; } = new Dictionary<GameObject, Graph>();

        /// <summary>
        /// Disables the general selection provided by <see cref="SEEInput.Select"/>.
        /// We need to avoid that the selection of graph elements to be deleted 
        /// interferes with the general <see cref="SelectAction"/>.
        /// </summary>
        public override void Start()
        {
            base.Start();
            SEEInput.SelectionEnabled = false;
        }

        /// <summary>
        /// Re-enables the general selection provided by <see cref="SEEInput.Select"/>.
        /// </summary>
        public override void Stop()
        {
            base.Stop();
            SEEInput.SelectionEnabled = true;
        }

        /// <summary>
        /// See <see cref="ReversibleAction.Update"/>.
        /// </summary>
        /// <returns>true if completed</returns>
        public override bool Update()
        {
            // FIXME: Needs adaptation for VR where no mouse is available.
            if (Input.GetMouseButtonDown(0)
                && Raycasting.RaycastGraphElement(out RaycastHit raycastHit, out GraphElementRef _) != HitGraphElement.None)
            {
                // the hit object is the one to be deleted
                GameObject hitGraphElement = raycastHit.collider.gameObject;
                Assert.IsTrue(hitGraphElement.HasNodeRef() || hitGraphElement.HasEdgeRef());
                explicitlyDeletedNodesAndEdges.Add(hitGraphElement);
                bool result = Delete(hitGraphElement);
                if (result)
                {
                    currentState = ReversibleAction.Progress.Completed;
                }
                return result; // the selected objects are deleted and this action is done now
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Deletes given <paramref GameObject="selectedObject"/> assumed to be either an
        /// edge or node. If it represents a node, the incoming and outgoing edges and
        /// its ancestors will be removed, too. For the possibility of an undo, the deleted objects will be saved. 
        /// 
        /// Precondition: <paramref name="deletedObject"/> != null.
        /// </summary>
        /// <param name="deletedObject">selected GameObject that along with its children should be removed</param>
        /// <returns>true if <paramref name="deletedObject"/> was actually deleted</returns>
        private bool Delete(GameObject deletedObject)
        {
            if (deletedObject.CompareTag(Tags.Edge))
            {
                InteractableObject.UnselectAll(true);
                DeleteEdge(deletedObject);
            }
            else if (deletedObject.CompareTag(Tags.Node))
            {
                if (deletedObject.GetNode().IsRoot())
                {
                    Debug.LogError("A root shall not be deleted.\n");
                    return false;
                }
                else
                {
                    InteractableObject.UnselectAll(true);
                    // The selectedObject (a node) and its ancestors are not deleted immediately. Instead we
                    // will run an animation that moves them into a garbage bin. Only when they arrive there,
                    // we will actually delete them.
                    PlayerSettings.GetPlayerSettings().StartCoroutine(DeletionAnimation.MoveNodeOrEdgeToGarbage(deletedObject.AllAncestors()));
                    Portal.SetInfinitePortal(deletedObject);
                    MarkAsDeleted(deletedObject.AllAncestors());
                }
            }
            return true;
        }

        /// <summary>
        /// Undoes this DeleteAction.
        /// </summary>
        public override void Undo()
        {
            base.Undo();

            // Re-add all nodes to their graphs.
            foreach (KeyValuePair<GameObject, Graph> nodeGraphPair in deletedNodes)
            {
                if (nodeGraphPair.Key.TryGetComponentOrLog(out NodeRef nodeRef))
                {
                    if (!nodeGraphPair.Value.Contains(nodeRef.Value))
                    {
                        nodeGraphPair.Value.AddNode(nodeRef.Value);
                        if (!roots.ContainsValue(nodeGraphPair.Value))
                        {
                            FindRoot(nodeGraphPair.Value);
                        }

                        // although a loop within a loop should be avoided in general due to performance reasons,
                        // in this case the amount of objects in <paramref name="roots"/> is limited to the amount of codecites, which likely
                        // will be kept below three.
                        foreach (KeyValuePair<GameObject, Graph> rootReference in roots)
                        {
                            if (rootReference.Value == nodeGraphPair.Key)
                            {
                                new UndoDeleteNetAction(nodeGraphPair.Key.name, rootReference.Key.name).Execute(null);
                            }
                        }
                    }
                }
            }

            // Re-add all edges to their graphs.
            foreach (KeyValuePair<GameObject, Graph> edgeGraphPair in deletedEdges)
            {
                if (edgeGraphPair.Key.TryGetComponentOrLog(out EdgeRef edgeReference))
                {
                    edgeGraphPair.Value.AddEdge(edgeReference.Value);
                    PlayerSettings.GetPlayerSettings().StartCoroutine(DeletionAnimation.UnhideEdge
                        (edgeGraphPair.Key));
                    if (!roots.ContainsValue(edgeGraphPair.Value))
                    {
                        FindRoot(edgeGraphPair.Value);
                    }
                    foreach (KeyValuePair<GameObject, Graph> rootReference in roots)
                    {
                        if (rootReference.Value == edgeGraphPair.Key)
                        {
                            new UndoDeleteNetAction(edgeGraphPair.Key.name, rootReference.Key.name).Execute(null);
                        }
                    }
                }
            }
            PlayerSettings.GetPlayerSettings().StartCoroutine(DeletionAnimation.RemoveNodeFromGarbage(new List<GameObject>(deletedNodes.Keys)));
            PlayerSettings.GetPlayerSettings().StartCoroutine(DeletionAnimation.RemoveNodeFromGarbage(new List<GameObject>(deletedEdges.Keys)));
        }

        /// <summary>
        /// Redoes this DeleteAction.
        /// </summary>
        public override void Redo()
        {
            foreach (GameObject gameObject in explicitlyDeletedNodesAndEdges)
            {
                Delete(gameObject);
                new DeleteNetAction(gameObject.name).Execute(null);
            }
        }

        /// <summary>
        /// Marks the given <paramref name="gameNodesToDelete"/> as deleted, i.e.,
        /// 1) removes the associated nodes represented by thos <paramref name="gameNodesToDelete"/> 
        ///    from their graph
        /// 2) removes the incoming and outgoing edges of <paramref name="gameNodesToDelete"/>
        ///    from their graph and makes those invisible
        /// 
        /// Assumption: <paramref name="gameNodesToDelete"/> contains all nodes in a subtree
        /// of the game-node hierarchy. All of them represent graph nodes.
        /// </summary>
        /// <param name="gameNodesToDelete">all deleted objects of the last operation</param>
        private void MarkAsDeleted(IList<GameObject> gameNodesToDelete)
        {
            ISet<GameObject> edgesInScene = new HashSet<GameObject>(GameObject.FindGameObjectsWithTag(Tags.Edge));
            // First identify all incoming and outgoing edges for all nodes in gameNodesToDelete
            HashSet<GameObject> implicitlyDeletedEdges = new HashSet<GameObject>();
            foreach (GameObject deletedGameNode in gameNodesToDelete)
            {
                if (deletedGameNode.TryGetComponentOrLog(out NodeRef nodeRef))
                {
                    ISet<string> attachedEdges = nodeRef.GetEdgeIds();
                    foreach (GameObject edge in edgesInScene)
                    {
                        if (edge.activeInHierarchy && attachedEdges.Contains(edge.name))
                        {
                            // We will not immediately delete this edge here, because it may be an
                            // edge inbetween two nodes both contained in gameNodesToDelete, in which
                            // case it will show up as an incoming and outgoing edge.
                            implicitlyDeletedEdges.Add(edge);
                        }
                    }
                }
            }

            // Now delete the incoming and outgoing edges.
            foreach (GameObject implicitlyDeletedEdge in implicitlyDeletedEdges)
            {
                DeleteEdge(implicitlyDeletedEdge);
            }

            // Finally, we remove the nodes themselves.
            foreach (GameObject deletedGameNode in gameNodesToDelete)
            {
                DeleteNode(deletedGameNode);
            }
        }

        /// <summary>
        /// Deletes the given <paramref name="gameNode"/>, that is, it will remove
        /// the associated Node it from its graph. The <paramref name="gameNode"/>
        /// itself is not deleted or made invisible (because it will be needed during 
        /// the animation).
        /// 
        /// Precondition: <paramref name="gameNode"/> must have an <see cref="NodeRef"/>
        /// attached to it.
        /// </summary>
        /// <param name="gameNode">a game object representing an edge</param>
        private void DeleteNode(GameObject gameNode)
        {
            if (gameNode.TryGetComponentOrLog(out NodeRef nodeRef))
            {
                Graph graph = nodeRef.Value.ItsGraph;
                deletedNodes[gameNode] = graph;
                if (!roots.ContainsValue(graph))
                {
                    FindRoot(graph);
                }
                new DeleteNetAction(gameNode.name).Execute(null);
                graph.RemoveNode(nodeRef.Value);
                graph.FinalizeNodeHierarchy();
            }
        }

        /// <summary>
        /// Deletes the given <paramref name="gameEdge"/>, that is, it will make it
        /// invisible and remove it from its graph.
        /// 
        /// Precondition: <paramref name="gameEdge"/> must have an <see cref="EdgeRef"/>
        /// attached to it.
        /// </summary>
        /// <param name="gameEdge">a game object representing an edge</param>
        private void DeleteEdge(GameObject gameEdge)
        {
            if (gameEdge.TryGetComponentOrLog(out EdgeRef edgeRef))
            {
                Graph graph = edgeRef.Value.ItsGraph;
                if (!roots.ContainsValue(graph))
                {
                    FindRoot(graph);
                }
                new DeleteNetAction(gameEdge.name).Execute(null);
                deletedEdges[gameEdge] = graph;
                PlayerSettings.GetPlayerSettings().StartCoroutine(DeletionAnimation.MoveNodeOrEdgeToGarbage(new List<GameObject> {gameEdge}));
                graph.RemoveEdge(edgeRef.Value);
            }
        }

        /// <summary>
        /// Returns the <see cref="ActionStateType"/> of this action.
        /// </summary>
        /// <returns><see cref="ActionStateType.Delete"/></returns>
        public override ActionStateType GetActionStateType()
        {
            return ActionStateType.Delete;
        }

        /// <summary>
        /// Finds the root of the <paramref name="graph"/> and saves the gameobject and the <paramref name="graph"/> in <see cref="roots"/>
        /// </summary>
        /// <param name="graph">graph to be added</param>
        private void FindRoot(Graph graph)
        {
            List<Node> rootNodes = graph.GetRoots();
            GameObject rootOfCity = new GameObject();
            foreach (Node root in rootNodes)
            {
                rootOfCity = SceneQueries.RetrieveGameNode(root.ID);
                roots.Add(rootOfCity, graph);
            }  
        }
    }
}
