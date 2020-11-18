using SEE.DataModel.DG;
using SEE.GO;
using SEE.Utils;
using System;
using UnityEngine;

namespace SEE.Controls.Actions
{
    /// <summary>
    /// Actions of a player. To be attached to a game object representing a 
    /// player (desktop, VR, etc.).
    /// </summary>
    public class PlayerActions : MonoBehaviour
    {
        private enum State
        {
            Idle,
            MoveNode,
            ReparentNode,
            MapNode
        }

        private State state = State.Idle;

        private void Update()
        {
            switch(state)
            {
                case State.MoveNode:
                    if (SelectedObject != null)
                    {
                        if (Input.GetMouseButton(0))
                        {
                            MoveTo(SelectedObject);
                        }
                        else
                        {
                            FinalizePosition(SelectedObject);
                        }
                    }
                    break;
            }
        }

        private void FinalizePosition(GameObject movingObject)
        {
            Node movingNode = movingObject.GetComponent<NodeRef>().node;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            // Note that the order of the results of RaycastAll() is undefined.
            RaycastHit[] hits = Physics.RaycastAll(ray);
            foreach (RaycastHit hit in hits)
            {
                // Must be different from the movingObject itself
                if (hit.collider.gameObject != movingObject)
                {
                    // Is it a node at all?
                    NodeRef nodeRef = hit.transform.GetComponent<NodeRef>();
                    // Are they in the same graph?
                    if (nodeRef != null && nodeRef.node.ItsGraph == movingNode.ItsGraph)
                    {
                        Debug.Log("Final destination reached.\n");
                        movingObject.transform.position = hit.point;
                        SelectedObject = null;
                        // FIXME: If the node has a new parent, we need to adjust the
                        // node hierarchy in the underlying graph.
                        return;
                    }
                }
            }
        }

        public float MovingSpeed = 1.0f;

        private void MoveTo(GameObject movingObject)
        {
            float step = MovingSpeed * Time.deltaTime;
            Vector3 target = TargetPosition(movingObject);            
            movingObject.transform.position = Vector3.MoveTowards(movingObject.transform.position, target, step);
            if (Vector3.Distance(movingObject.transform.position, target) > 0.01)
            {
                Debug.LogFormat("Moving {0} from {1} to {2}.\n", movingObject.name, movingObject.transform.position, target);
            }
        }

        private Vector3 TargetPosition(GameObject selectedObject)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            return ray.GetPoint(Vector3.Distance(ray.origin, selectedObject.transform.position));
        }

        public void Move()
        {
            Debug.Log("Move\n");
            state = State.MoveNode;
        }

        public void Reparent()
        {
            Debug.Log("Reparent\n");
            state = State.ReparentNode;
        }

        public void Map()
        {
            Debug.Log("MapNode\n");
            state = State.MapNode;
        }

        private Vector3 originalPositionOfSelectedObject;
        private GameObject selectedObject;
        private GameObject SelectedObject
        {
            get => selectedObject;
            set
            {
                selectedObject = value;
                if (value != null)
                {
                    originalPositionOfSelectedObject = selectedObject.transform.position;
                }
            }
        }

        public void SelectOn(GameObject selection)
        {
            Debug.LogFormat("selected object {0}\n", selection.name);
            SelectedObject = selection;
        }

        public void SelectOff(GameObject selection)
        {
            Debug.LogFormat("deselected object {0}\n", selection.name);
            SelectedObject = null;
        }

        private GameObject hoveredObject;

        public void HoverOn(GameObject hovered)
        {
            Debug.LogFormat("hovered object {0}\n", hovered.name);
            hoveredObject = hovered;
        }

        public void HoverOff(GameObject hovered)
        {
            Debug.LogFormat("unhovered object {0}\n", hovered.name);
            hoveredObject = null;
        }
    }
}