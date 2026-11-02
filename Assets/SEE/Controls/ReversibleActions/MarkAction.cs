using System.Collections.Generic;
using UnityEngine;
using SEE.Controls.KeyActions;
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
    /// Action to toggle a sphere marking above a node in a code city.
    /// </summary>
    internal class MarkAction : AbstractPlayerAction
    {
        /// <summary>
        /// Identifier of the modified node, or null before a node has been selected.
        /// </summary>
        private string nodeID;

        /// <summary>
        /// Marking state before this action, retained for undo and redo.
        /// </summary>
        private bool wasMarked;

        /// <summary>
        /// Returns a new marking action.
        /// </summary>
        /// <returns>A new instance of <see cref="MarkAction"/>.</returns>
        public static IReversibleAction CreateReversibleAction()
        {
            return new MarkAction();
        }

        /// <summary>
        /// Returns a new action of the same type.
        /// </summary>
        /// <returns>A new instance of <see cref="MarkAction"/>.</returns>
        public override IReversibleAction NewInstance()
        {
            return CreateReversibleAction();
        }

        /// <summary>
        /// Processes user interaction to toggle the selected node's marking.
        /// </summary>
        /// <returns>True when the action is completed.</returns>
        public override bool Update()
        {
            if (CurrentState == IReversibleAction.Progress.Completed)
            {
                return true;
            }

            GameObject node = null;
            if (UserSetting.IsDesktop && SEEInput.Select()
                && Raycasting.RaycastGraphElement(out RaycastHit _, out GraphElementRef element)
                    == HitGraphElement.Node)
            {
                node = element.gameObject;
            }
            else if (UserSetting.IsVR && XRSEEActions.Selected)
            {
                XRSEEActions.Selected = false;
                InteractableObject hovered = InteractableObject.HoveredObjectWithWorldFlag;
                if (hovered != null && hovered.gameObject.HasNodeRef())
                {
                    node = hovered.gameObject;
                }
            }
            if (node == null)
            {
                return false;
            }

            nodeID = node.name;
            wasMarked = GameNodeMarker.IsMarked(node);
            SetMarkedAndSynchronize(node, !wasMarked);
            CurrentState = IReversibleAction.Progress.Completed;
            return true;
        }

        /// <summary>
        /// Restores the marking state from before this action.
        /// </summary>
        public override void Undo()
        {
            base.Undo();
            SetMarkedAndSynchronize(GraphElementIDMap.Find(nodeID), wasMarked);
        }

        /// <summary>
        /// Reapplies the marking state changed by this action.
        /// </summary>
        public override void Redo()
        {
            base.Redo();
            SetMarkedAndSynchronize(GraphElementIDMap.Find(nodeID), !wasMarked);
        }

        /// <summary>
        /// Returns the action state type identifying the marking action.
        /// </summary>
        /// <returns>The marking action state type.</returns>
        public override ActionStateType GetActionStateType()
        {
            return ActionStateTypes.Mark;
        }

        /// <summary>
        /// Returns the identifiers of objects modified by this action.
        /// </summary>
        /// <returns>The identifiers of the modified objects.</returns>
        public override HashSet<string> GetChangedObjects()
        {
            return nodeID == null ? new HashSet<string>() : new HashSet<string> { nodeID };
        }

        /// <summary>
        /// Applies a marking state locally and broadcasts it to the other clients.
        /// Does nothing if the node no longer exists.
        /// </summary>
        /// <param name="node">The node to modify, or null if it no longer exists.</param>
        /// <param name="marked">The desired marking state.</param>
        private static void SetMarkedAndSynchronize(GameObject node, bool marked)
        {
            if (node != null)
            {
                GameNodeMarker.SetMarked(node, marked);
                new MarkNetAction(node.name, marked).Execute();
            }
        }
    }
}
