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
    /// Action to mark or unmark a node of a code city.
    /// </summary>
    internal class MarkAction : AbstractPlayerAction
    {
        /// <summary>
        /// The node whose mark was changed by this action.
        /// </summary>
        private GameObject node;

        /// <summary>
        /// If the user selects a game object representing a graph node, its mark is
        /// toggled.
        /// See <see cref="IReversibleAction.Update"/>.
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
                HandleNodeClick(raycastHit.collider.gameObject);
                result = true;
            }
            else if (UserSetting.IsVR
                && XRSEEActions.Selected
                && InteractableObject.HoveredObjectWithWorldFlag.gameObject != null
                && InteractableObject.HoveredObjectWithWorldFlag.gameObject.HasNodeRef()
                && XRSEEActions.RayInteractor.TryGetCurrent3DRaycastHit(out raycastHit))
            {
                XRSEEActions.Selected = false;
                HandleNodeClick(raycastHit.collider.gameObject);
                result = true;
            }

            return result;
        }

        /// <summary>
        /// Toggles the mark of the selected node and sends the change through the
        /// network.
        /// </summary>
        /// <param name="clickedNode">The selected node.</param>
        private void HandleNodeClick(GameObject clickedNode)
        {
            node = clickedNode;

            ToggleMarkAndBroadcast();

            CurrentState = IReversibleAction.Progress.Completed;
        }

        /// <summary>
        /// Undoes this action.
        /// </summary>
        public override void Undo()
        {
            base.Undo();
            ToggleMarkAndBroadcast();
        }

        /// <summary>
        /// Redoes this action.
        /// </summary>
        public override void Redo()
        {
            base.Redo();
            ToggleMarkAndBroadcast();
        }

        /// <summary>
        /// Toggles the marking of the node and broadcasts the change to other clients.
        /// </summary>
        private void ToggleMarkAndBroadcast()
        {
            bool isMarked = GameNodeMarker.ToggleMark(node);
            new MarkNetAction(node.ID(), isMarked).Execute();
        }

        /// <summary>
        /// Returns a new instance of <see cref="MarkAction"/>.
        /// </summary>
        /// <returns>New instance.</returns>
        public static IReversibleAction CreateReversibleAction() => new MarkAction();

        /// <summary>
        /// Returns a new instance of <see cref="MarkAction"/>.
        /// </summary>
        /// <returns>New instance.</returns>
        public override IReversibleAction NewInstance() => CreateReversibleAction();

        /// <summary>
        /// Returns the <see cref="ActionStateType"/> of this action.
        /// </summary>
        /// <returns><see cref="ActionStateTypes.MarkNode"/>.</returns>
        public override ActionStateType GetActionStateType() => ActionStateTypes.MarkNode;

        /// <summary>
        /// Returns all IDs of gameObjects manipulated by this action.
        /// </summary>
        /// <returns>All IDs of gameObjects manipulated by this action.</returns>
        public override HashSet<string> GetChangedObjects()
        {
            return node != null ? new() { node.ID() } : new();
        }
    }
}
