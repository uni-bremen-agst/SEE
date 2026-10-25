using System.Collections.Generic;
using SEE.Extensions;
using UnityEngine;

using SEE.GraphElementRefs;
using SEE.Net.Actions.GraphElement;
using SEE.SceneManipulation;
using SEE.UserSettings;
using SEE.Utils;
using SEE.XR;

namespace SEE.Controls.ReversibleActions
{
    /// <summary>
    /// Action for marking or unmarking a game node.
    /// </summary>

    internal class MarkAction : AbstractPlayerAction
    {
        /// <summary>
        /// The ID of the node affected by this action.
        /// </summary>
        private string nodeID;
        /// <summary>
        /// Whether the target node was marked before this action.
        /// </summary>
        private bool wasMarked;

        /// <summary>
        /// Handles desktop or XR input to toggle a graph node's marking.
        /// </summary>
        /// <returns>True if a marking operation completed; otherwise, false.</returns>
        public override bool Update()
        {
            if (UserSetting.IsDesktop
                && Input.GetMouseButtonDown(0)
                && Raycasting.RaycastGraphElement(
                    out RaycastHit raycastHit,
                    out GraphElementRef graphElementRef,
                    false) == HitGraphElement.Node
                && graphElementRef.gameObject.TryGetComponent(out InteractableObject interactableObject)
                && interactableObject.IsInteractable(raycastHit.point))
            {
                ExecuteMarking(graphElementRef.gameObject);
                return true;
            }

            if (UserSetting.IsVR
                && XRSEEActions.Selected
                && InteractableObject.HoveredObjectWithWorldFlag.gameObject != null
                && InteractableObject.HoveredObjectWithWorldFlag.gameObject.HasNodeRef()
                && XRSEEActions.RayInteractor.TryGetCurrent3DRaycastHit(out RaycastHit _))
            {
                XRSEEActions.Selected = false;

                ExecuteMarking(InteractableObject.HoveredObjectWithWorldFlag.gameObject);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Records the original state, toggles the marking, and broadcasts the resulting state.
        /// </summary>
        /// <param name="target">The selected graph node to affect; must not be null.</param>
        private void ExecuteMarking(GameObject target)
        {
            nodeID = target.name;
            wasMarked = GameNodeMarker.IsMarked(target);

            bool marked = !wasMarked;

            GameNodeMarker.SetMarked(target, marked);
            new MarkNetAction(nodeID, marked).Execute();

            CurrentState = IReversibleAction.Progress.Completed;
        }

        /// <summary>
        /// Restores and broadcasts the original marking state if the target node still exists.
        /// </summary>
        public override void Undo()
        {
            base.Undo();

            GameObject gameNode = GraphElementIDMap.Find(nodeID);

            if (gameNode != null)
            {
                GameNodeMarker.SetMarked(gameNode, wasMarked);
                new MarkNetAction(nodeID, wasMarked).Execute();
            }
        }

        /// <summary>
        /// Restores and broadcasts the toggled marking state if the target node still exists.
        /// </summary>
        public override void Redo()
        {
            base.Redo();

            GameObject gameNode = GraphElementIDMap.Find(nodeID);

            if (gameNode != null)
            {
                bool marked = !wasMarked;

                GameNodeMarker.SetMarked(gameNode, marked);
                new MarkNetAction(nodeID, marked).Execute();
            }
        }

        /// <summary>
        /// Creates an unexecuted marking action without a selected target.
        /// </summary>
        /// <returns>A new <see cref="MarkAction"/> with no recorded marking state.</returns>
        public static IReversibleAction CreateReversibleAction()
        {
            return new MarkAction();
        }

        /// <summary>
        /// Creates a fresh marking action for another node selection.
        /// </summary>
        /// <returns>A new marking action without this action's target or recorded state.</returns>
        public override IReversibleAction NewInstance()
        {
            return CreateReversibleAction();
        }

        /// <summary>
        /// Identifies this action as the marking mode.
        /// </summary>
        /// <returns>The registered <see cref="ActionStateTypes.Mark"/> action type.</returns>
        public override ActionStateType GetActionStateType()
        {
            return ActionStateTypes.Mark;
        }

        /// <summary>
        /// Reports the target node ID for action-history conflict detection.
        /// </summary>
        /// <returns>The target node ID, or an empty set if no target has been recorded.</returns>
        public override HashSet<string> GetChangedObjects()
        {
            HashSet<string> changedObjects = new();

            if (nodeID != null)
            {
                changedObjects.Add(nodeID);
            }

            return changedObjects;
        }
    }
}