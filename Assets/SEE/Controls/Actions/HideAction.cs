using SEE.DataModel;
using SEE.DataModel.DG;
using SEE.Game;
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

        HashSet<InteractableObject> selectedObjects;

        private ISet<GameObject> hiddenObjects = new HashSet<GameObject>();

        private ISet<GameObject> undoneList = new HashSet<GameObject>();

        /// <summary>
        /// The code city to perform actions on, only necessary for 
        /// </summary>
        SEECity CodeCity;

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
            selectedObjects = new HashSet<InteractableObject>();
            InteractableObject.LocalAnySelectIn += LocalAnySelectIn;
            InteractableObject.LocalAnySelectOut += LocalAnySelectOut;
        }

        public override void Stop()
        {
            base.Stop();
            selectedObjects = null;
            InteractableObject.LocalAnySelectIn -= LocalAnySelectIn;
            InteractableObject.LocalAnySelectOut -= LocalAnySelectOut;
        }

        // Update is called once per frame
        public override bool Update()
        {
            if (selectedObjects != null && selectedObjects.Count > 0 && Input.GetKey(KeyCode.Return))
            {
                foreach(InteractableObject o in selectedObjects)
                {
                    GameObject g = o.gameObject;
                    Assert.IsTrue(g.HasNodeRef() || g.HasEdgeRef());
                    if (g.CompareTag(Tags.Edge))
                    {
                        HideEdge(g);
                    }
                    else if (g.CompareTag(Tags.Node))
                    {
                        HideNodeIncludingConnectedEdges(g);
                    }
                }                
                hadAnEffect = true;
                return true;
            }
            else
            {
                return false;
            }
        }


        private bool HideEdge(GameObject edge)
        {
            hiddenObjects.Add(edge);
            GameObjectExtensions.SetVisibility(edge, false, true);
            return true;
        }

        private bool HideNodeIncludingConnectedEdges(GameObject node)
        {
            if (node.TryGetComponent(out NodeRef nodeRef))
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
            hiddenObjects.Add(node);
            GameObjectExtensions.SetVisibility(node, false, true);
            node = null;
            return true;
        }

        private bool HideAll()
        {
            GameObject city = selectedObject;
            while (!city.CompareTag(Tags.CodeCity))
            {
                city = city.transform.parent.gameObject;
            }

            List<GameObject> nodesEdgesDecorations = GetAllChildrenRecursively(city.transform, new List<GameObject>());
            
            foreach (GameObject g in nodesEdgesDecorations)
            {
                GameObjectExtensions.SetVisibility(g, false, true);
                hiddenObjects.Add(g);
            }
            return true;
        }

        private List<GameObject> GetAllChildrenRecursively(Transform transform, List<GameObject> objectList)
        {
            foreach (Transform child in transform)
            {
                if (child.CompareTag(Tags.Node) || child.CompareTag(Tags.Edge) || child.CompareTag(Tags.Decoration))
                {
                    objectList.Add(child.gameObject);
                }
                if(child.childCount > 0)
                {
                    GetAllChildrenRecursively(child, objectList);
                }
            }
            return objectList;
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
            Debug.Log(hiddenObjects.Count);

            base.Undo();
            foreach (GameObject g in hiddenObjects)
            {
                GameObjectExtensions.SetVisibility(g, true, false);
                undoneList.Add(g);
            }
            hiddenObjects.Clear();
        }

        public override void Redo()
        {
            base.Redo();
            foreach (GameObject g in undoneList)
            {
                GameObjectExtensions.SetVisibility(g, false, false);
                hiddenObjects.Add(g);
            }
            undoneList.Clear();
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
            if (interactableObject != null)
            {
                selectedObjects.Add(interactableObject);
            }
        }

        private void LocalAnySelectOut(InteractableObject interactableObject)
        {
            if (selectedObjects.Contains(interactableObject))
            {
                selectedObjects.Remove(interactableObject);
            }
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