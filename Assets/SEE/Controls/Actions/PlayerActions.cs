using SEE.DataModel.DG;
using SEE.Game;
using SEE.Game.Charts;
using SEE.GO;
using SEE.Tools;
using SEE.Utils;
using UnityEngine;
using UnityEngine.Assertions;

namespace SEE.Controls.Actions
{
    /// <summary>
    /// Actions of a player. To be attached to a game object representing a 
    /// player (desktop, VR, etc.).
    /// </summary>
    public class PlayerActions : MonoBehaviour
    {
        [SerializeField] private MappingAction mappingAction;

        private ActionState.Type state = ActionState.Value;

        private void Start()
        {
            Assert.IsNotNull(mappingAction, "The mapping action must not be null!");
            ActionState.OnStateChanged += OnStateChanged;
        }

        private void OnDestroy()
        {
            ActionState.OnStateChanged -= OnStateChanged;
        }

        private void Update()
        {
            // If the local player presses U, we deselect all currently selected interactable objects.
            if (Input.GetKeyDown(KeyCode.U))
            {
                InteractableObject.UnselectAll(true);
                ChartManager.Instance.UnselectAll(); // TODO(torben): this should happen via callbacks through InteractableObject. i believe, that should already work in the charts branch...
            }

            switch (state)
            {
                case ActionState.Type.Move:
                    {
                        // an object must be selected; otherwise we cannot move it
                        if (selectedNodeRef != null)
                        {
                            if (UserWantsToMove())
                            {
                                GameNodeMover.MoveTo(selectedNodeRef.gameObject);
                            }
                            else
                            {
                                // The selected object has reached its final destination.
                                // It needs to be placed there.
                                GameNodeMover.FinalizePosition(selectedNodeRef.gameObject);
                                selectedNodeRef = null;
                            }
                        }
                    }
                    break;
                case ActionState.Type.Rotate:
                    {
                    }
                    break;
                case ActionState.Type.Map:
                    {
                        if (selectedNodeRef != null && Input.GetMouseButtonDown(0) && Raycasting.RaycastNodes(out RaycastHit raycastHit, out NodeRef nodeRef))
                        {
                            Node from = selectedNodeRef.node;
                            Node to = nodeRef.node;

                            if (from.ItsGraph.Name != to.ItsGraph.Name)
                            {
                                Reflexion reflexion = mappingAction.Reflexion;
                                if (reflexion.Is_Mapper(from))
                                {
                                    Assert.IsTrue(from.Outgoings.Count == 1);
                                    reflexion.Delete_From_Mapping(from.Outgoings[0]);
                                }
                                reflexion.Add_To_Mapping(from, to);

                                SetAlpha(1.0f);
                                selectedNodeRef = null;
                            }
                        }
                    }
                    break;
            }
        }

        // -------------------------------------------------------------
        // The callbacks from the circular menu to trigger state changes
        // -------------------------------------------------------------

        private void OnStateChanged(ActionState.Type value)
        {
            Enter(value);
        }

        /// <summary>
        /// If <paramref name="newState"/> is different from the current state,
        /// <see cref="Cancel"/> is called and <paramref name="newState"/> is
        /// entered.
        /// </summary>
        /// <param name="newState">new state to be entered</param>
        private void Enter(ActionState.Type newState)
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
                case ActionState.Type.Move:
                    {
                    }
                    break;
                case ActionState.Type.Rotate:
                    {
                    }
                    break;
                case ActionState.Type.Map:
                    {
                        if (selectedNodeRef)
                        {
                            SetAlpha(1.0f);
                        }
                    }
                    break;
                default: throw new System.NotImplementedException();
            }
        }

        private void SetAlpha(float alpha)
        {
            MeshRenderer meshRenderer = selectedNodeRef.GetComponent<MeshRenderer>();
            Color color = meshRenderer.material.color;
            color.a = alpha;
            meshRenderer.material.color = color;
        }

        // ------------------------------------------------------------
        // The management of the currently selected interactable object
        // ------------------------------------------------------------

        /// <summary>
        /// The currently selected object. May be null if none is selected.
        /// Do not use this attribute directly. Use <see cref="SelectedObject"/>
        /// instead.
        /// </summary>
        private NodeRef selectedNodeRef;

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
            // TODO(torben): if mapping, only select if node is from 'implementation' and not from 'architecture'
            selectedNodeRef = selection.GetComponent<NodeRef>();
            if (state == ActionState.Type.Map)
            {
                SetAlpha(0.8f);
            }
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
            if (state == ActionState.Type.Map && selectedNodeRef)
            {
                SetAlpha(1.0f);
            }
            selectedNodeRef = null;
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