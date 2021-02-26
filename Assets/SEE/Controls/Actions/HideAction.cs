using SEE.DataModel;
using SEE.DataModel.DG;
using SEE.GO;
using SEE.Utils;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace SEE.Controls.Actions
{
    /// <summary>
    /// Action to hide/show the currently selected game object (edge or node).
    /// </summary>
    internal class HideAction : MonoBehaviour
    {
        private readonly ActionStateType ThisActionState = ActionStateType.Hide;

        /// <summary>
        /// The currently selected object (a node or edge).
        /// </summary>
        private GameObject selectedObject;

        private List<List<GameObject>> hiddenObjects;

        // Start is called before the first frame update
        private void Start()
        {
            hiddenObjects = new List<List<GameObject>>();
            // An anonymous delegate is registered for the event <see cref="ActionState.OnStateChanged"/>.
            // This delegate will be called from <see cref="ActionState"/> upon every
            // state changed where the passed parameter is the newly entered state.
            ActionState.OnStateChanged += newState =>
            {
                
                // Is this our action state where we need to do something?
                if (Equals(newState, ThisActionState))
                {
                    // The MonoBehaviour is enabled and Update() will be called by Unity.
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

        // Update is called once per frame
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

            if(Input.GetKeyDown(KeyCode.Backspace))
            {
                List<GameObject> lastHidden = hiddenObjects[hiddenObjects.Count - 1];
                foreach (GameObject g in lastHidden){
                    g.SetActive(true);
                }
                hiddenObjects.Remove(lastHidden);
                return;
            }

            if (selectedObject != null) // Input.GetMouseButtonDown(0) && 
            {
                Assert.IsTrue(selectedObject.HasNodeRef() || selectedObject.HasEdgeRef());
                if (selectedObject.CompareTag(Tags.Edge))
                {
                    //hiddenObjects.Add(selectedObject);
                    selectedObject.SetActive(false);
                    selectedObject = null;
                }
                else if (selectedObject.CompareTag(Tags.Node))
                {
                    List<GameObject> hiddenList = new List<GameObject>();
                    if (selectedObject.TryGetComponent(out NodeRef nodeRef))
                    {
                        HashSet<String> edgeIDs = GetEdgeIds(nodeRef);

                        foreach (GameObject edge in GameObject.FindGameObjectsWithTag(Tags.Edge))
                        {
                            if (edge.activeInHierarchy && edgeIDs.Contains(edge.name))
                            {
                                hiddenList.Add(edge);
                                edge.SetActive(false);
                            }
                        }
                    }
                    hiddenList.Add(selectedObject);
                    selectedObject.SetActive(false);
                    selectedObject = null;
                    hiddenObjects.Add(hiddenList);
                }
            }
        }

        private void UnhideAll()
        {
            foreach(List<GameObject> l in hiddenObjects)
            {
                foreach(GameObject g in l){
                    g.SetActive(true);
                }
            }
            hiddenObjects.Clear();
        }


        /// <summary>
        /// Returns the IDs of all incoming and outgoing edges for <paramref name="nodeRef"/>.
        /// </summary>
        /// <param name="nodeRef">node whose incoming and outgoing edges are requested</param>
        /// <returns>IDs of all incoming and outgoing edges</returns>
        private static HashSet<string> GetEdgeIds(NodeRef nodeRef)
        {
            HashSet<String> edgeIDs = new HashSet<string>();
            foreach (Edge edge in nodeRef.Value.Outgoings)
            {
                edgeIDs.Add(edge.ID);
            }
            foreach (Edge edge in nodeRef.Value.Incomings)
            {
                edgeIDs.Add(edge.ID);
            }
            return edgeIDs;
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