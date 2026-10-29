using System.Collections.Generic;
using SEE.Controls.KeyActions;
using SEE.Extensions;
using SEE.GraphElementRefs;
using SEE.Net.Actions.GraphElement;
using SEE.SceneManipulation;
using SEE.UserSettings;
using SEE.Utils;
using SEE.XR;
using Unity.Netcode;
using UnityEngine;

namespace SEE.Controls.ReversibleActions
{
    /// <summary>
    /// Toggles a node's marking and remembers its previous state for undo and redo.
    /// </summary>
    internal class MarkAction : AbstractPlayerAction
    {
        /// <summary>
        /// ID of the selected node. The ID also permits undo after the node has been recreated.
        /// </summary>
        private string nodeID;

        /// <summary>
        /// Whether the node was marked before this action.
        /// </summary>
        private bool wasMarked;

        /// <summary>
        /// Waits for the player to select an interactable game node, then toggles its marking.
        /// </summary>
        /// <returns>True once a node's marking has been changed.</returns>
        public override bool Update()
        {
            if (CurrentState == IReversibleAction.Progress.Completed)
            {
                return true;
            }

            GameObject target = null;
            if (UserSetting.IsDesktop && SEEInput.Select()
                && Raycasting.RaycastGraphElement(out RaycastHit hit, out GraphElementRef element,
                                                  requireInteractable: false) == HitGraphElement.Node
                && element.gameObject.TryGetComponent(out InteractableObject interactable)
                && interactable.IsInteractable(hit.point))
            {
                target = element.gameObject;
            }
            else if (UserSetting.IsVR && XRSEEActions.Selected
                     && InteractableObject.HoveredObjectWithWorldFlag != null
                     && InteractableObject.HoveredObjectWithWorldFlag.gameObject.HasNodeRef())
            {
                XRSEEActions.Selected = false;
                target = InteractableObject.HoveredObjectWithWorldFlag.gameObject;
            }

            if (target == null)
            {
                return false;
            }

            ToggleMark(target);
            return true;
        }

        /// <summary>
        /// Changes the marking of a selected node and records its original state.
        /// </summary>
        /// <param name="node">The selected game node.</param>
        internal void ToggleMark(GameObject node)
        {
            nodeID = node.name;
            wasMarked = GameNodeMarker.IsMarked(node);
            SetMarked(node, !wasMarked);
            CurrentState = IReversibleAction.Progress.Completed;
        }

        /// <summary>
        /// Applied a marking locally and broadcasts its intended state to connected clients.
        /// </summary>
        /// <param name="node">The game node to change.</param>
        /// <param name="marked">The intended marking state.</param>
        private static void SetMarked(GameObject node, bool marked)
        {
            GameNodeMarker.SetMarked(node, marked);
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            {
                new MarkNetAction(node.name, marked).Execute();
            }
        }

        /// <summary>
        /// Restored the node's original marking state.
        /// </summary>
        public override void Undo()
        {
            base.Undo();
            GameObject node = GraphElementIDMap.Find(nodeID);
            if (node != null)
            {
                SetMarked(node, wasMarked);
            }
        }

        /// <summary>
        /// Reapplies the marking state produced by this action.
        /// </summary>
        public override void Redo()
        {
            base.Redo();
            GameObject node = GraphElementIDMap.Find(nodeID);
            if (node != null)
            {
                SetMarked(node, !wasMarked);
            }
        }

        /// <summary>
        /// Created a new marking action.
        /// </summary>
        /// <returns>A marking action with no selected node.</returns>
        public static IReversibleAction CreateReversibleAction()
        {
            return new MarkAction();
        }

        /// <summary>
        /// Created a new marking action of the same type.
        /// </summary>
        /// <returns>A marking action with no selected node.</returns>
        public override IReversibleAction NewInstance()
        {
            return CreateReversibleAction();
        }

        /// <summary>
        /// Returned the player-menu entry for marking nodes.
        /// </summary>
        /// <returns>The Mark action state type.</returns>
        public override ActionStateType GetActionStateType()
        {
            return ActionStateTypes.Mark;
        }

        /// <summary>
        /// Returned the ID of the node changed by this action.
        /// </summary>
        /// <returns>The selected node ID, or an empty set before selection.</returns>
        public override HashSet<string> GetChangedObjects()
        {
            return nodeID == null ? new HashSet<string>() : new HashSet<string> { nodeID };
        }
    }
}
