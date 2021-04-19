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
    internal class HideAction : AbstractPlayerAction
    {
        /// <summary>
        /// The currently selected object (a node or edge).
        /// </summary>
        private GameObject selectedObject;

        private List<GameObject> hiddenObjects;

        private List<GameObject> undoneList;

        enum EdgeSelector
        {
            Incomming,
            Outgoing
        }


        /// <summary>
        /// Returns a new instance of <see cref="HideAction"/>.
        /// </summary>
        /// <returns>new instance</returns>
        public static ReversibleAction CreateReversibleAction()
        {
            return new HideAction();
        }

        /// <summary>
        /// Returns a new instance of <see cref="HideAction"/>.
        /// </summary>
        /// <returns>new instance</returns>
        public override ReversibleAction NewInstance()
        {
            return CreateReversibleAction();
        }

        // Start is called before the first frame update
        public override void Start()
        {
            base.Stop();
            Debug.Log("Start\n");
            hiddenObjects = new List<GameObject>();
            undoneList = new List<GameObject>();
            InteractableObject.LocalAnySelectIn += LocalAnySelectIn;
            InteractableObject.LocalAnySelectOut += LocalAnySelectOut;
        }

        public override void Stop()
        {
            base.Stop();
            Debug.Log("Stop\n");
            InteractableObject.LocalAnySelectIn -= LocalAnySelectIn;
            InteractableObject.LocalAnySelectOut -= LocalAnySelectOut;
        }

        // Update is called once per frame
        public override bool Update()
        {
           if (selectedObject != null)
            {
                Debug.Log(selectedObject);
                Assert.IsTrue(selectedObject.HasNodeRef() || selectedObject.HasEdgeRef());
                if (selectedObject.CompareTag(Tags.Edge))
                {
                    hiddenObjects.Add(selectedObject);
                    GameObjectExtensions.SetVisibility(selectedObject, false, true);
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
                            bool rendered = false;
                            if (edge.TryGetComponent(out Renderer renderer))
                            {
                                rendered = renderer.enabled;
                            }
                            if (edge.activeInHierarchy && edgeIDs.Contains(edge.name) && rendered)
                            {
                                hiddenObjects.Add(edge);
                                GameObjectExtensions.SetVisibility(edge, false, true);
                            }
                        }
                    }
                    hiddenObjects.Add(selectedObject);
                    GameObjectExtensions.SetVisibility(selectedObject, false, true);
                    selectedObject = null;
                }
                hadAnEffect = true;
                return true;
            }
            else
            {
                return false;
            }

        }



        private bool HideOutgoingEdges(GameObject node)
        {
            List<GameObject> hiddenList = new List<GameObject>();
            if (node.TryGetComponent(out NodeRef nodeRef))
            {

                HashSet<String> edgeIDs = new HashSet<string>();
                foreach (Edge edge in nodeRef.Value.Outgoings)
                {
                    edgeIDs.Add(edge.ID);
                }

                foreach (GameObject edge in GameObject.FindGameObjectsWithTag(Tags.Edge))
                {
                            
                    if (edge.activeInHierarchy && edgeIDs.Contains(edge.name))
                    {
                        hiddenList.Add(edge);
                        GameObjectExtensions.SetVisibility(edge, false, true);
                    }
                }
                selectedObject = null;
                //hiddenObjects.Add(hiddenList);
                return true;
                }
            return false;   
        }

        private bool HideIncommingEdges(GameObject node)
        {
            List<GameObject> hiddenList = new List<GameObject>();
            if (node.TryGetComponent(out NodeRef nodeRef))
            {

                HashSet<String> edgeIDs = new HashSet<string>();
                foreach (Edge edge in nodeRef.Value.Incomings)
                {
                    edgeIDs.Add(edge.ID);
                }

                foreach (GameObject edge in GameObject.FindGameObjectsWithTag(Tags.Edge))
                {
                            
                    if (edge.activeInHierarchy && edgeIDs.Contains(edge.name))
                    {
                        hiddenList.Add(edge);
                        GameObjectExtensions.SetVisibility(edge, false, true);
                    }
                }
                selectedObject = null;
                //hiddenObjects.Add(hiddenList);
                return true;
                }
            return false;         
        }

        public override void Undo()
        {
            foreach (GameObject g in hiddenObjects)
            {
                GameObjectExtensions.SetVisibility(g, true, false);
            }
            hiddenObjects.Clear();
            base.Undo();
        }

        public override void Redo()
        {
            foreach (GameObject g in undoneList)
            {
                GameObjectExtensions.SetVisibility(g, false, false);
            }
            undoneList.Clear();
            base.Redo();
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

        /// <summary>
        /// Returns the <see cref="ActionStateType"/> of this action.
        /// </summary>
        /// <returns><see cref="ActionStateType.Hide"/></returns>
        public override ActionStateType GetActionStateType()
        {
            return ActionStateType.Hide;
        }
    }
}