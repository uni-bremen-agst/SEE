using SEE.DataModel;
using SEE.DataModel.DG;
using SEE.Game;
using SEE.Game.UI;
using SEE.GO;
using SEE.Utils;
using System.Collections;
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
        /// <returns></returns>
        public static ReversibleAction CreateReversibleAction()
        {
            return new DeleteAction();
        }

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
        private List<GameObject> DeletedNodes { get; set; } = new List<GameObject>();

        /// <summary>
        /// A history of the old positions of the nodes deleted by this action.
        /// </summary>
        public List<Vector3> OldPositions { get; set; } = new List<Vector3>();

        /// <summary>
        /// A history of all edges deleted by this action.
        /// </summary>
        public List<GameObject> DeletedEdges { get; set; } = new List<GameObject>();

        /// <summary>
        /// The graph where this action is executed.
        /// </summary>
        public Graph Graph { get; set; }

        public List<GameObject> deletedParent = new List<GameObject>();

        /// <summary>
        /// FIXME: This does not belong here.
        /// The garbage can the deleted nodes will be moved to.
        /// </summary>
        protected GameObject garbageCan;

        private ActionStateIndicator actionStateIndicator;

        /// <summary>
        /// FIXME: This does not belong here.
        /// The name of the garbage can gameObject.
        /// </summary>
        protected const string GarbageCanName = "GarbageCan";

        private bool isRunning = false;

        public override void Awake()
        {
            garbageCan = GameObject.Find(GarbageCanName);
        }

        public override void Start()
        {
            // The MonoBehaviour is enabled and Update() will be called by Unity.
            InteractableObject.LocalAnySelectIn += LocalAnySelectIn;
            InteractableObject.LocalAnySelectOut += LocalAnySelectOut;
            GameObject playerSettings = GameObject.Find("Player Settings");
            actionStateIndicator = playerSettings.GetComponentInChildren<ActionStateIndicator>();
        }

        public override void Update()
        {
            // Delete a gameobject and all children
            if (selectedObject != null && isRunning == false)
            {
                isRunning = true;
                GameObject selected = selectedObject;
                Assert.IsTrue(selected.HasNodeRef() || selected.HasEdgeRef());
                // FIXME:(Thore) NetAction is no longer up to date
                new DeleteNetAction(selected.name).Execute(null);
                DeleteSelectedObject(selected);
                deletedParent.Add(selected);
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
            if (selectedObject == null)
            {
                return;
            }
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
                    List<GameObject> deletedNodes = GameObjectTraversion.GetAllChildNodes(new List<GameObject>(), selectedObject);
                    actionStateIndicator.StartCoroutine(this.MoveNodeToGarbage(deletedNodes));
                }
                CurrentState = CurrentActionState.Executed;
            }
        }

        /// <summary>
        /// Undoes this DeleteAction
        /// </summary>
        public override void Undo()
        {
            if (DeletedNodes != null)
            {
                actionStateIndicator.StartCoroutine(this.RemoveNodeFromGarbage(DeletedNodes));
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
                    Debug.Log(DeletedEdges.Count);
                    if (edge.TryGetComponentOrLog(out EdgeRef edgeReference))
                    {
                        Graph.AddEdge(edgeReference.edge);
                        edge.SetVisibility(true, false);
                    }
                }
            }
        }

        /// <summary>
        /// Redoes this DeleteAction
        /// </summary>
        public override void Redo()
        {
            foreach(GameObject parent in deletedParent)
            {
                DeleteSelectedObject(parent);
            }
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
            isRunning = false;
            InteractableObject.UnselectAll(true);
        }

        /// <summary>
        /// Removes all given nodes from the garbage can and back into the city.
        /// </summary>
        /// <param name="deletedNode">The nodes to be removed from the garbage-can</param>
        /// <returns>the waiting time between moving deleted nodes from the garbage-can and then to the city</returns>
        private IEnumerator RemoveNodeFromGarbage(List<GameObject> deletedNodes)
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
            OldPositions.Clear();
            DeletedNodes.Clear();
            DeletedEdges.Clear();
            InteractableObject.UnselectAll(true);
        }

        /// <summary>
        /// Saves the deleted nodes and/or edges for the possibility of an undo. 
        /// Removes the gameObjects from the graph.
        /// Precondition: <see cref="deletedNodes""/> != null.
        /// </summary>
        /// <param name="deletedObject">all deleted objects of the last operation</param>
        /// <param name="oldPositionsOfDeletedNodes">all old positions of the deleted nodes of the last operation</param>
        private void SaveObjectForDeleteUndo(List<GameObject> deletedObject, List<Vector3> oldPositionsOfDeletedNodes)
        {
            SEECity city = SceneQueries.GetCodeCity(deletedObject[0].transform)?.gameObject.GetComponent<SEECity>();
            Graph graph = city.LoadedGraph;
            List<GameObject> nodesAndAscendingEdges = new List<GameObject>();
            List<GameObject> edgesToHide = new List<GameObject>();

            foreach (GameObject actionHistoryObject in deletedObject)
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

            List<GameObject> deletedNodesReverse = deletedObject;
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
            DeletedNodes.AddRange(nodesAndAscendingEdges);
            OldPositions.AddRange(oldPositionsOfDeletedNodes);
            DeletedEdges.AddRange(edgesToHide);
            Graph = graph;
            isRunning = false;
            selectedObject = null;
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
