using SEE.Extensions;
using SEE.GraphElementRefs;
using SEE.Net.Actions.GraphElement;
using SEE.SceneManipulation;
using SEE.UserSettings;
using SEE.Utils;
using SEE.XR;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.Controls.ReversibleActions
{
    /// <summary>
    /// Action to mark or unmark a node in the city.
    /// </summary>
    internal class MarkAction : AbstractPlayerAction
    {
        /// <summary>
        /// ID of the selected node.
        /// </summary>
        private string nodeID;

        /// <summary>
        /// Whether the node was marked before this action.
        /// </summary>
        private bool previouslyMarked;

        /// <summary>
        /// If the user clicks with the mouse (or VR input) hitting a game object representing a graph node,
        /// the node is marked. If it was previously marked, it gets unmarked.
        /// </summary>
        /// <returns>True if completed.</returns>
        public override bool Update()
        {
            if (UserSetting.IsDesktop && Input.GetMouseButtonDown(0)
                && Raycasting.RaycastGraphElement(out RaycastHit raycastHit, out GraphElementRef elementRef) == HitGraphElement.Node
                && elementRef.gameObject.TryGetComponent(out InteractableObject io)
                && io.IsInteractable(raycastHit.point))
            {
                Mark(elementRef.gameObject);
                return true;
            }
            else if (UserSetting.IsVR
                && XRSEEActions.Selected
                && InteractableObject.HoveredObjectWithWorldFlag.gameObject != null
                && InteractableObject.HoveredObjectWithWorldFlag.gameObject.HasNodeRef()
                && XRSEEActions.RayInteractor.TryGetCurrent3DRaycastHit(out raycastHit))
            {
                XRSEEActions.Selected = false;
                Mark(raycastHit.collider.gameObject);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Marks the selected node if unmarked, or unmarks it if already marked
        /// while remembering the previous state for Undo/Redo.
        /// </summary>
        /// <param name="node">The node that is to be marked or unmarked.</param>
        private void Mark(GameObject node)
        {
            nodeID = node.name;
            previouslyMarked = GameNodeMarker.IsMarked(node);
            SetMarked(!previouslyMarked);
            CurrentState = IReversibleAction.Progress.Completed;
        }

        /// <summary>
        /// Applies the requested marked state to the node stored by nodeID locally
        /// then broadcasts the change to connected clients.
        /// </summary>
        /// <param name="marked">Requested marked state.</param>
        private void SetMarked(bool marked)
        {
            GameObject node = GraphElementIDMap.Find(nodeID);
            if (node != null)
            {
                GameNodeMarker.SetMarked(node, marked);
                new MarkNetAction(nodeID, marked).Execute();
            }
        }

        /// <summary>
        /// Undoes this action.
        /// </summary>
        public override void Undo()
        {
            base.Undo();
            SetMarked(previouslyMarked);
        }

        /// <summary>
        /// Redoes this action.
        /// </summary>
        public override void Redo()
        {
            base.Redo();
            SetMarked(!previouslyMarked);
        }

        /// <summary>
        /// Returns a new instance of <see cref="MarkAction"/>.
        /// </summary>
        /// <returns>New instance.</returns>
        public static IReversibleAction CreateReversibleAction()
        {
            return new MarkAction();
        }

        /// <summary>
        /// Returns a new instance of <see cref="MarkAction"/>.
        /// </summary>
        /// <returns>New instance.</returns>
        public override IReversibleAction NewInstance()
        {
            return CreateReversibleAction();
        }

        /// <summary>
        /// Returns the <see cref="ActionStateType"/> of this action.
        /// </summary>
        /// <returns><see cref="ActionStateType.Mark"/>.</returns>
        public override ActionStateType GetActionStateType()
        {
            return ActionStateTypes.Mark;
        }

        /// <summary>
        /// Returns all IDs of gameObjects manipulated by this action.
        /// </summary>
        /// <returns>All IDs of gameObjects manipulated by this action.</returns>
        public override HashSet<string> GetChangedObjects()
        {
            return nodeID == null ? new HashSet<string>() : new HashSet<string> { nodeID };
        }
    }
}
