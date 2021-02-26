using SEE.DataModel;
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
    internal class DeleteAction : MonoBehaviour
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
        /// The ActionHistory which is responsible for the undo/redo-operations.
        /// </summary>
        private ActionHistory actionHistory;

        /// <summary>
        /// The Garbage-can where the deleted nodes will be moved.
        /// </summary>
        private GameObject garbageCan;

        /// <summary>
        /// The name of the garbage-can gameObject.
        /// </summary>
        private static readonly string GarbageCanName = "GarbageCan";

        private void Start()
        {
            // An anonymous delegate is registered for the event <see cref="ActionState.OnStateChanged"/>.
            // This delegate will be called from <see cref="ActionState"/> upon every
            // state changed where the passed parameter is the newly entered state.
            ActionState.OnStateChanged += newState =>
            {
                if (Equals(newState, ThisActionState))
                {
                    // The monobehaviour is enabled and Update() will be called by Unity.
                    garbageCan = GameObject.Find(GarbageCanName);
                    garbageCan.TryGetComponent(out ActionHistory actionHistory);
                    this.actionHistory = actionHistory;
                    enabled = true;
                    InteractableObject.LocalAnySelectIn += LocalAnySelectIn;
                    InteractableObject.LocalAnySelectOut += LocalAnySelectOut;
                }
                else
                {
                    // The MonoBehaviour is disabled and Update() no longer be called by Unity.
                    enabled = false;
                    InteractableObject.LocalAnySelectIn -= LocalAnySelectIn;
                    InteractableObject.LocalAnySelectOut -= LocalAnySelectOut;
                }
            };
            enabled = ActionState.Is(ThisActionState);
        }

        private void Update()
        {
            // This script should be disabled, if the action state is not this action's type
            if (!ActionState.Is(ThisActionState))
            {
                // The MonoBehaviour is disabled and Update() no longer be called by Unity.
                enabled = false;
                InteractableObject.LocalAnySelectIn -= LocalAnySelectIn;
                InteractableObject.LocalAnySelectOut -= LocalAnySelectOut;
                return;
            }

            //Delete a gameobject and all children
            if (selectedObject != null && Input.GetMouseButtonDown(0))
            {
                
                Assert.IsTrue(selectedObject.HasNodeRef() || selectedObject.HasEdgeRef());
                //FIXME:(Thore) NetAction is no longer up to date
                new DeleteNetAction(selectedObject.name).Execute(null);
                DeleteSelectedObject(selectedObject);
            }

            //Undo last deletion
            if (Input.GetKeyDown(KeyCode.Z))
            {
                try
                {
                    List<GameObject> objectToBeMoved = actionHistory.deletedNodeHistory.Last();
                    StartCoroutine(RemoveNodeFromGarbage(objectToBeMoved));
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
        /// its ancestors will be removed, too. For the possibility of an undo - the deleted objects will be saved. 
        /// </summary>
        /// <param GameObject="selectedObject">selected GameObject that and thats children should be removed</param>
        public void DeleteSelectedObject(GameObject selectedObject)
        {
            if (selectedObject != null)
            {
                if (selectedObject.CompareTag(Tags.Edge))
                {
                    List<GameObject> edge = new List<GameObject>();
                    edge.Add(selectedObject);
                    actionHistory.SaveObjectForUndo(edge, new List<Vector3>());
                    // Destroyer.DestroyGameObject(selectedObject);
                }
                else if (selectedObject.CompareTag(Tags.Node))
                {
                    if (selectedObject.GetNode().IsRoot())
                    {
                        Debug.LogError("Root shall not be deleted");
                        return;
                    }
                    List<GameObject> allNodesToBeDeleted = GameObjectTraversion.GetAllChildNodesAsGameObject(new List<GameObject>(), selectedObject);
                    StartCoroutine(MoveNodeToGarbage(allNodesToBeDeleted));
                }
            }
        }

        /// <summary>
        /// Moves all nodes which were deleted in the last operation to the garbage-can
        /// </summary>
        /// <param name="deletedNodes">the deleted nodes which will be moved to garbage-can</param>
        /// <returns>the waiting time between moving deleted nodes over the garbage-can and then in the garbage-can</returns>
        public IEnumerator MoveNodeToGarbage(List<GameObject> deletedNodes)
        {
            List<Vector3> oldPositions = new List<Vector3>();
            float timeToWait = 1f;
            float timeForAnimation = 1f; 
            foreach (GameObject deletedNode in deletedNodes)
            {
                if (deletedNode.CompareTag(Tags.Node))
                {
                    float xPosition = deletedNode.transform.position.x;
                    float yPosition = deletedNode.transform.position.y;
                    float zPosition = deletedNode.transform.position.z;
                    oldPositions.Add(new Vector3(xPosition, yPosition, zPosition));
                    Portal.SetInfinitePortal(deletedNode);
                }
            }
            actionHistory.SaveObjectForUndo(deletedNodes, oldPositions);
            foreach (GameObject deletedNode in deletedNodes)
            {
                Tweens.Move(deletedNode, new Vector3(garbageCan.transform.position.x, garbageCan.transform.position.y + 1.4f, garbageCan.transform.position.z), timeForAnimation);
            }

            yield return new WaitForSeconds(timeToWait);

            foreach (GameObject deletedNode in deletedNodes)
            {
                if (deletedNode.CompareTag(Tags.Node))
                {
                    Tweens.Move(deletedNode, new Vector3(garbageCan.transform.position.x, garbageCan.transform.position.y, garbageCan.transform.position.z), timeForAnimation);
                }
            }

            yield return new WaitForSeconds(timeToWait);
            InteractableObject.UnselectAll(true);
            
        }

        /// <summary>
        /// Removes all nodes from the garbage-can to the city.
        /// </summary>
        /// <param name="deletedNode"></param>
        /// <returns>the waiting time between moving deleted nodes from the garbage-can and then to the city</returns>
        public IEnumerator RemoveNodeFromGarbage(List<GameObject> deletedNodes)
        {
            List<Vector3> oldPositionOfDeletedObject = actionHistory.oldPositionHistory.Last(); 

            for (int i = 0; i < deletedNodes.Count; i++)
            {
                
                {
                    Tweens.Move(deletedNodes[i], new Vector3(garbageCan.transform.position.x, garbageCan.transform.position.y + 1.4f, garbageCan.transform.position.z), 1f);
                }
            }

            yield return new WaitForSeconds(1.2f);

            for (int i = 0; i < deletedNodes.Count; i++)
            {
                
                {
                    Tweens.Move(deletedNodes[i], oldPositionOfDeletedObject[i], 1f);
                }
            }
            
            yield return new WaitForSeconds(1.0f);
            oldPositionOfDeletedObject = actionHistory.UndoDeleteOperation();
            InteractableObject.UnselectAll(true);
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
