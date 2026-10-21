
using System.Collections.Generic;
using UnityEngine;

using SEE.Extensions;
using SEE.GraphElementRefs;
using SEE.Net.Actions.GraphElement;
using SEE.SceneManipulation;
using SEE.UserSettings;
using SEE.Utils;
using SEE.XR;

namespace SEE.Controls.ReversibleActions
{
    /// <summary>
    /// Action to mark or unmark a node.
    /// </summary>
    internal class MarkAction : AbstractPlayerAction
    {
        /// <summary>
        /// The node which gets marked or unmarked.
        /// </summary>
        private GameObject targetNode;

        /// <summary>
        /// If the user selects a game object representing a graph node,
        /// this graph node gets marked or unmarked depending on its current state.
        /// <see cref="IReversibleAction.Update"/>.
        /// </summary>
        /// <returns>True if completed.</returns>
        public override bool Update()
        {
            bool result = false;

            if (UserSetting.IsDesktop && Input.GetMouseButtonDown(0)
                && Raycasting.RaycastGraphElement(out RaycastHit raycastHit, out GraphElementRef ger, false) == HitGraphElement.Node
                && ger.gameObject.TryGetComponent(out InteractableObject io)
                && io.IsInteractable(raycastHit.point))
            {
                ToggleNodeMark(raycastHit.collider.gameObject);
                result = true;
            }
            else if (UserSetting.IsVR
                && XRSEEActions.Selected
                && InteractableObject.HoveredObjectWithWorldFlag.gameObject != null
                && InteractableObject.HoveredObjectWithWorldFlag.gameObject.HasNodeRef()
                && XRSEEActions.RayInteractor.TryGetCurrent3DRaycastHit(out raycastHit))
            {
                XRSEEActions.Selected = false;
                ToggleNodeMark(raycastHit.collider.gameObject);
                result = true;
            }

            if (result)
            {
                CurrentState = IReversibleAction.Progress.Completed;
            }

            return result;
        }

        /// <summary>
        /// Toggles the marking of the selected node and broadcasts the change.
        /// </summary>
        /// <param name="clickedNode">The node to mark or unmark.</param>
        private void ToggleNodeMark(GameObject clickedNode)
        {
            targetNode = clickedNode;

            GameNodeMarker.ToggleMark(targetNode);
            BroadcastMarkChange();
        }

        /// <summary>
        /// Broadcasts the change to other clients and the server.
        /// </summary>
        private void BroadcastMarkChange()
        {
            new MarkNetAction(targetNode.ID()).Execute();
        }

        /// <summary>
        /// Undoes this action.
        /// </summary>
        public override void Undo()
        {
            base.Undo();

            ToggleNodeMark(targetNode);
        }

        /// <summary>
        /// Redoes this action.
        /// </summary>
        public override void Redo()
        {
            base.Redo();

            ToggleNodeMark(targetNode);
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
        /// <returns>The action state type for marking nodes.</returns>
        public override ActionStateType GetActionStateType()
        {
            return ActionStateTypes.MarkNode;
        }

        /// <summary>
        /// Returns all IDs of gameObjects manipulated by this action.
        /// </summary>
        /// <returns>All IDs of gameObjects manipulated by this action.</returns>
        public override HashSet<string> GetChangedObjects()
        {
            if (targetNode != null)
            {
                return new HashSet<string> { targetNode.ID() };
            }
            else
            {
                return new();
            }
        }
    }
}
