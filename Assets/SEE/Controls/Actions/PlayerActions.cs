using SEE.DataModel;
using SEE.Game;
using SEE.Game.Charts;
using SEE.Game.UI3D;
using SEE.Utils;
using UnityEngine;

namespace SEE.Controls.Actions
{
    /// <summary>
    /// Actions of a player. To be attached to a game object representing a 
    /// player (desktop, VR, etc.).
    /// </summary>
    public class PlayerActions : MonoBehaviour
    {
        /// <summary>
        /// The possible states a player can be in. The state determines how the player
        /// reacts to events.
        /// </summary>
        private enum State
        {
            Browse,   // the user just browses the city; this is the default
            MoveNode, // a game node is being moved within its city
            MapNode,   // a game node is mapped from one city to another city
            DrawEdge  // a new edge has to be drawn
        }

        /// <summary>
        /// The current state of the player.
        /// </summary>
        private State state = State.Browse;

        private void Update()
        {
            // If the local player presses U, we deselect all currently selected interactable objects.
            if (Input.GetKeyDown(KeyCode.U))
            {
                InteractableObject.UnselectAll(true);
            }
            // If the local player presses Delete, we delete the currently selected object
            if (Input.GetKeyDown(KeyCode.Delete))
            {
                if (selectedObject.gameObject != null)
                {
                        Transform selected = selectedObject.gameObject.transform;
                        InteractableObject interactable = selected.GetComponent<InteractableObject>();
                        if (interactable)
                        {                                             
                            if (selected.CompareTag(Tags.Edge))
                            {
                                Destroyer.DestroyGameObject(selected.gameObject);
                            }
                            else
                            {
                                Destroyer.DestroyGameObjectWithChildren(selected.gameObject);
                            }                      
                    }                    
                }
            }
            switch (state)
            {
                case State.MoveNode:
                    // an object must be selected; otherwise we cannot move it
                    if (selectedObject.gameObject != null)
                    {                        
                        if (UserWantsToMove())
                        {
                            GameNodeMover.MoveTo(selectedObject.gameObject);
                        }
                        else
                        {
                            // The selected object has reached its final destination.
                            // It needs to be placed there.
                            GameNodeMover.FinalizePosition(selectedObject.gameObject, selectedObject.originalPosition);
                            selectedObject.gameObject = null;
                        }
                    }
                    break;

                case State.DrawEdge:
                    // first we need a game node to be selected as source
                    if (!gameObject.TryGetComponent(out AddEdge addEdge))
                    {
                        addEdge = gameObject.AddComponent<AddEdge>();
                    }
                    addEdge.SetHoveredObject(hoveredObject);
                    break; 
            }
        }

        // -------------------------------------------------------------
        // The callbacks from the circular menu to trigger state changes
        // -------------------------------------------------------------

        /// <summary>
        /// Changes the state to Browse. 
        /// 
        /// This method is called as a callback from the menu.
        /// </summary>
        public void Browse()
        {
            Enter(State.Browse);
        }

        /// <summary>
        /// Changes the state to MoveNode. 
        /// 
        /// This method is called as a callback from the menu.
        /// </summary>
        public void Move()
        {
            Enter(State.MoveNode);
        }

        /// <summary>
        /// Changes the state to MapNode. 
        /// 
        /// This method is called as a callback from the menu.
        /// </summary>
        public void Map()
        {
            Enter(State.MapNode);
        }

        /// <summary>
        /// Changes the state to DrawEdge. 
        /// 
        /// This method is called as a callback from the menu.
        /// </summary>
        public void Draw()
        {
            Enter(State.DrawEdge);
        }

        /// <summary>
        /// If <paramref name="newState"/> is different from the current state,
        /// <see cref="Cancel"/> is called and <paramref name="newState"/> is
        /// entered.
        /// </summary>
        /// <param name="newState">new state to be entered</param>
        private void Enter(State newState)
        {
            if (state != newState)
            {
                Cancel();
                state = newState;
            }
        }

        /// <summary>
        /// Cancels the current action before the next new state is entered.
        /// This method can implement the "last wishes" of a running action.
        /// </summary>
        private void Cancel()
        {
            switch (state)
            {
                case State.Browse:
                    // nothing to be done
                    break;
                case State.MapNode:
                    break;
                case State.MoveNode:
                    break;
                case State.DrawEdge:
                    Destroy(gameObject.GetComponent<AddEdge>());
                    break;
                default:
                    throw new System.NotImplementedException();
            }
        }

        // ------------------------------------------------------------
        // The management of the currently selected interactable object
        // ------------------------------------------------------------

        private struct ObjectInfo
        {
            public GameObject gameObject;
            public Vector3 originalPosition;
        }

        /// <summary>
        /// The currently selected object. May be null if none is selected.
        /// Do not use this attribute directly. Use <see cref="SelectedObject"/>
        /// instead.
        /// </summary>
        private ObjectInfo selectedObject = new ObjectInfo();

        // ------------------------------------------------------------------------------
        // Events triggered by interactable objects when they are selected, hovered over,
        // or grabbed.
        // ------------------------------------------------------------------------------

        /// <summary>
        /// Assigns the value of given <paramref name="selection"/> to
        /// <see cref="SelectedObject"/>.
        /// 
        /// Called by an interactable object when it is selected (only once when the
        /// selection starts).
        /// </summary>
        /// <param name="selection">the selected interactable object</param>
        public void SelectOn(GameObject selection)
        {
            selectedObject.gameObject = selection;
            selectedObject.originalPosition = selection.transform.position;
            //Debug.Log($"Player selects {selection.name}.\n");
        }

        /// <summary>
        /// Resets <see cref="SelectedObject"/> to null.
        /// 
        /// Called by an interactable object when it is unselected (only once when the
        /// selection ends).
        /// </summary>
        /// <param name="selection">the interactable object no longer selected</param>
        public void SelectOff(GameObject selection)
        {
            //Debug.Log($"Player deselects {selection.name}.\n");
            selectedObject.gameObject = null;
        }

        /// <summary>
        /// The interactable object that is currently being hovered over.
        /// </summary>
        private GameObject hoveredObject;

        /// <summary>
        /// Assigns the value of given <paramref name="hovered"/> to
        /// <see cref="hoveredObject"/>.
        /// 
        /// Called by an interactable object when it is being hovered over
        /// (only once when the hovering starts).
        /// </summary>
        /// <param name="hovered">the hovered interactable object</param>
        public void HoverOn(GameObject hovered)
        {
            //Debug.Log($"Player hovers on {hovered.name}.\n");
            hoveredObject = hovered;
        }

        /// <summary>
        /// Resets <see cref="hoveredObject"/> to null.
        /// 
        /// Called by an interactable object when it is no longer being hovered over
        /// (only once when the hovering ends).
        /// </summary>
        /// <param name="hovered">the interactable object no longer hovered</param>
        public void HoverOff(GameObject hovered)
        {
            //Debug.Log($"Player hovers off {hovered.name}.\n");
            hoveredObject = null;
        }

        /// <summary>
        /// Called by an interactable object when it is being grabbed
        /// (only once when the grabbing begins).
        /// </summary>
        /// <param name="grabbed">the grabbed interactable object</param>
        public void GrabOn(GameObject grabbed)
        {
            // currently empty
        }

        /// <summary>
        /// Called by an interactable object when it is no longer being grabbed
        /// (only once when the grabbing ends).
        /// </summary>
        /// <param name="grabbed">the interactable object no longer grabbed</param>
        public void GrabOff(GameObject grabbed)
        {
            // currently empty
        }

        // -------------------------------------------------------------
        // User input
        // -------------------------------------------------------------

        /// <summary>
        /// True iff the user expresses that the moving action should start or continue.
        /// The expression depends upon the environment (desktop, VR, etc.).
        /// </summary>
        /// <returns>user wants to move a selected object</returns>
        private static bool UserWantsToMove()
        {
            // FIXME: We need to an interaction for VR, too.
            // We move the node while the left mouse button is pressed.
            return Input.GetMouseButton(0);
        }
    }
}
