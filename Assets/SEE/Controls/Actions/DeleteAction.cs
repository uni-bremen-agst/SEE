using SEE.DataModel;
using SEE.DataModel.DG;
using SEE.Game;
using SEE.GO;
using SEE.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
        /// Start() will register an anonymous delegate of type 
        /// <see cref="ActionState.OnStateChangedFn"/> on the event
        /// <see cref="ActionState.OnStateChanged"/> to be called upon every
        /// change of the action state, where the newly entered state will
        /// be passed as a parameter. The anonymous delegate will compare whether
        /// this state equals <see cref="ThisActionState"/> and if so, execute
        /// what needs to be done for this action here. If that parameter is
        /// different from <see cref="ThisActionState"/>, this action will
        /// put itself to sleep. 
        /// Thus, this action will be executed only if the new state is 
        /// <see cref="ThisActionState"/>.
        /// </summary>
        private readonly ActionStateType ThisActionState = ActionStateType.Delete;

        /// <summary>
        /// The currently selected object (a node or edge).
        /// </summary>
        private GameObject selectedObject;

        /// <summary>
        /// The waiting time of the animation for moving a node into a garbage can from over the garbage can.
        /// </summary>
        private const float TimeToWait = 1f;

        /// <summary>
        /// The animation time of the animation of moving a node to the top of the garbage can.
        /// </summary>
        private const float TimeForAnimation = 1f;

        /// <summary>
        /// A history of all nodes deleted by this action.
        /// </summary>
        public List<GameObject> DeletedNodes { get; set; }

        /// <summary>
        /// A history of the old positions of the nodes deleted by this action.
        /// </summary>
        public List<Vector3> OldPositions { get; set; }

        /// <summary>
        /// A history of all edges deleted by this action.
        /// </summary>
        public List<GameObject> DeletedEdges { get; set; }

        /// <summary>
        /// The graph where this action is executed.
        /// </summary>
        public Graph Graph { get; set; }

        /// <summary>
        /// Constructor for UndoActions
        /// </summary>
        /// <param name="deletedNodes">the deleted nodes</param>
        /// <param name="oldPositions">the positions of the deleted nodes</param>
        /// <param name="deletedEdges">the deleted edges</param>
        /// <param name="graph">the graph which has the deleted node attached</param>
        public DeleteAction(List<GameObject> deletedNodes, List<Vector3> oldPositions, List<GameObject> deletedEdges, Graph graph)
        {
            this.DeletedNodes = deletedNodes;
            this.OldPositions = oldPositions;
            this.DeletedEdges = deletedEdges;
            this.Graph = graph;
        }

        public DeleteAction()
        {
            // Necessary for ActionStates
        }

        public void Start()
        {
            // An anonymous delegate is registered for the event <see cref="ActionState.OnStateChanged"/>.
            // This delegate will be called from <see cref="ActionState"/> upon every
            // state changed where the passed parameter is the newly entered state.
            ActionState.OnStateChanged += newState =>
            {
                if (Equals(newState, ThisActionState))
                {
                    // The MonoBehaviour is enabled and Update() will be called by Unity.
                    UndoInitialisation();
                    InteractableObject.LocalAnySelectIn += LocalAnySelectIn;
                    InteractableObject.LocalAnySelectOut += LocalAnySelectOut;
                }
                else
                {
                    // The MonoBehaviour is disabled and Update() no longer be called by Unity.
                    InteractableObject.LocalAnySelectIn -= LocalAnySelectIn;
                    InteractableObject.LocalAnySelectOut -= LocalAnySelectOut;
                }
            };
        }

        public override void Update()
        {
            // This script should be disabled, if the action state is not this action's type
            if (!ActionState.Is(ThisActionState))
            {
                // The MonoBehaviour is disabled and Update() no longer be called by Unity.
                InteractableObject.LocalAnySelectIn -= LocalAnySelectIn;
                InteractableObject.LocalAnySelectOut -= LocalAnySelectOut;
                return;
            }

            // Delete a gameobject and all children
            if (selectedObject != null && Input.GetMouseButtonDown(0))
            {
                Assert.IsTrue(selectedObject.HasNodeRef() || selectedObject.HasEdgeRef());
                // FIXME:(Thore) NetAction is no longer up to date
                new DeleteNetAction(selectedObject.name).Execute(null);
                DeleteSelectedObject(selectedObject);
            }

            // Undo last deletion
            if (Input.GetKeyDown(KeyCode.Z))
            {
                try
                {
                    DeleteAction deleteAction = (DeleteAction)actionHistory.ActionHistoryList.Last();
                    List<GameObject> objectToBeMoved = deleteAction.DeletedNodes;
                 //   StartCoroutine(RemoveNodeFromGarbage(objectToBeMoved));
                }
                catch (InvalidOperationException)
                {
                    Debug.LogError("No history detected");
                }
            }
        }

        /// <summary>
        /// Deletes given <paramref GameObject="selectedObject"/> assumed to be either an
        /// edge or node. If it represents a node, the incoming and outgoing edges and
        /// its ancestors will be removed, too. For the possibility of an undo, the deleted objects will be saved. 
        /// </summary>
        /// <param GameObject="selectedObject">selected GameObject that along with its children should be removed</param>
        public void DeleteSelectedObject(GameObject selectedObject)
        {
            if (selectedObject != null)
            {
                if (selectedObject.CompareTag(Tags.Edge))
                {
                    List<GameObject> edge = new List<GameObject>() { selectedObject };
                    SaveObjectForDeleteUndo(edge, new List<Vector3>());
                }
                else if (selectedObject.CompareTag(Tags.Node))
                {
                    if (selectedObject.GetNode().IsRoot())
                    {
                        Debug.LogError("Root shall not be deleted");
                        return;
                    }
                    List<GameObject> allNodesToBeDeleted = GameObjectTraversion.GetAllChildNodes(new List<GameObject>(), selectedObject);
                  //  StartCoroutine(MoveNodeToGarbage(allNodesToBeDeleted));
                }
            }
        }

        /// <summary>
        /// Undoes this DeleteAction
        /// </summary>
        public override void Undo()
        {
            foreach (GameObject node in DeletedNodes)
            {
                if (node.TryGetComponentOrLog(out NodeRef nodeRef))
                {
                    if (!Graph.Contains(nodeRef.Value))
                    {
                        Graph.AddNode(nodeRef.Value);
                    }
                }
            }

            foreach (GameObject edge in DeletedEdges)
            {
                if (edge.TryGetComponentOrLog(out EdgeRef edgeReference))
                {
                    Graph.AddEdge(edgeReference.edge);
                    edge.SetVisibility(true, false);
                }
            }
        }

        /// <summary>
        /// Redoes this DeleteAction
        /// </summary>
        public override void Redo()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Moves all nodes which were deleted in the last operation to the garbage can.
        /// </summary>
        /// <param name="deletedNodes">the deleted nodes which will be moved to the garbage can.</param>
        /// <returns>the waiting time between moving deleted nodes over the garbage can and then into the garbage can.</returns>
        private IEnumerator MoveNodeToGarbage(List<GameObject> deletedNodes)
        {
            List<Vector3> oldPositions = new List<Vector3>();
            
            foreach (GameObject deletedNode in deletedNodes)
            {
                if (deletedNode.CompareTag(Tags.Node))
                {
                    oldPositions.Add(deletedNode.transform.position);
                    Portal.SetInfinitePortal(deletedNode);
                }
            }
            SaveObjectForDeleteUndo(deletedNodes, oldPositions);
            foreach (GameObject deletedNode in deletedNodes)
            {
                Tweens.Move(deletedNode, new Vector3(garbageCan.transform.position.x, garbageCan.transform.position.y + 1.4f, garbageCan.transform.position.z), TimeForAnimation);
            }

            yield return new WaitForSeconds(TimeToWait);

            foreach (GameObject deletedNode in deletedNodes)
            {
                if (deletedNode.CompareTag(Tags.Node))
                {
                    Tweens.Move(deletedNode, new Vector3(garbageCan.transform.position.x, garbageCan.transform.position.y, garbageCan.transform.position.z), TimeForAnimation);
                }
            }

            yield return new WaitForSeconds(TimeToWait);
            InteractableObject.UnselectAll(true);
        }

        /// <summary>
        /// Removes all given nodes from the garbage can and back into the city.
        /// </summary>
        /// <param name="deletedNode">The nodes to be removed from the garbage-can</param>
        /// <returns>the waiting time between moving deleted nodes from the garbage-can and then to the city</returns>
        private IEnumerator RemoveNodeFromGarbage(List<GameObject> deletedNodes)
        {
            // In case that the deleted object is a single edge - waiting-time is unintended
            if (deletedNodes.Count != 0)
            {
                for (int i = 0; i < deletedNodes.Count; i++)
                {
                        Tweens.Move(deletedNodes[i], new Vector3(garbageCan.transform.position.x, garbageCan.transform.position.y + 1.4f, garbageCan.transform.position.z), TimeForAnimation);
                }

                yield return new WaitForSeconds(TimeToWait);

                for (int i = 0; i < deletedNodes.Count; i++)
                {
                        Tweens.Move(deletedNodes[i], OldPositions[i], TimeForAnimation);
                }

                yield return new WaitForSeconds(TimeToWait);
            }
            Undo();
            InteractableObject.UnselectAll(true);
        }

        /// <summary>
        /// Saves the deleted nodes and/or edges for the possibility of an undo. 
        /// Removes the gameObjects from the graph.
        /// Precondition: <see cref="deletedNodes""/> != null.
        /// </summary>
        /// <param name="deletedNodes">all deleted nodes of the last operation</param>
        /// <param name="oldPositionsOfDeletedNodes">all old positions of the deleted nodes of the last operation</param>
        public void SaveObjectForDeleteUndo(List<GameObject> deletedNodes, List<Vector3> oldPositionsOfDeletedNodes)
        {
            SEECity city = SceneQueries.GetCodeCity(deletedNodes[0].transform)?.gameObject.GetComponent<SEECity>();
            Graph graph = city.LoadedGraph;
            List<GameObject> nodesAndAscendingEdges = new List<GameObject>();
            List<GameObject> edgesToHide = new List<GameObject>();

            foreach (GameObject actionHistoryObject in deletedNodes)
            {
                if (actionHistoryObject.TryGetComponent(out NodeRef nodeRef))
                {
                    HashSet<string> edgeIDs = Destroyer.GetEdgeIds(nodeRef);
                    foreach (GameObject edge in GameObject.FindGameObjectsWithTag(Tags.Edge))
                    {
                        if (edge.activeInHierarchy && edgeIDs.Contains(edge.name))
                        {
                            edge.SetVisibility(false, true);

                            if (!nodesAndAscendingEdges.Contains(edge))
                            {
                                edgesToHide.Add(edge);
                            }
                            edge.TryGetComponent(out EdgeRef edgeRef);
                            graph.RemoveEdge(edgeRef.edge);
                        }
                    }

                    nodesAndAscendingEdges.Add(actionHistoryObject);
                }
            }

            List<GameObject> deletedNodesReverse = deletedNodes;
            // For deletion bottom-up
            deletedNodesReverse.Reverse();

            foreach (GameObject deletedNode in deletedNodesReverse)
            {
                if (deletedNode.CompareTag(Tags.Node))
                {
                    deletedNode.TryGetComponent(out NodeRef nodeRef);
                    if (graph.Contains(nodeRef.Value))
                    {
                        graph.RemoveNode(nodeRef.Value);
                    }
                }
                if (deletedNode.CompareTag(Tags.Edge))
                {
                    deletedNode.SetVisibility(false, true);
                    edgesToHide.Add(deletedNode);
                    deletedNode.TryGetComponent(out EdgeRef edgeRef);
                    graph.RemoveEdge(edgeRef.edge);
                }
            }

            oldPositionsOfDeletedNodes.Reverse();
            nodesAndAscendingEdges.Reverse();
            actionHistory.ActionHistoryList.Add(new DeleteAction(nodesAndAscendingEdges, oldPositionsOfDeletedNodes, edgesToHide, graph));
        }


        private void LocalAnySelectIn(InteractableObject interactableObject)
        {
            // FIXME: For an unknown reason, the mouse events in InteractableObject will be
            // triggered twice per frame, which causes this method to be called twice.
            // We need to further investigate this issue.
            // Assert.IsNull(selectedObject);
            selectedObject = interactableObject.gameObject;
        }

        private void LocalAnySelectOut(InteractableObject interactableObject)
        {
            // FIXME: For an unknown reason, the mouse events in InteractableObject will be
            // triggered twice per frame, which causes this method to be called twice.
            // We need to further investigate this issue.
            // Assert.IsTrue(selectedObject == interactableObject.gameObject);
            selectedObject = null;
        }
    }
}
