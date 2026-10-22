using System.Collections.Generic;
using UnityEngine;
using SEE.SceneManipulation;
using SEE.Utils;
using SEE.GraphElementRefs;
using SEE.UserSettings;
using SEE.Net.Actions.GraphElement;

namespace SEE.Controls.ReversibleActions
{
    /// <summary>
    /// Action to mark or unmark a selected node by a sphere above it.
    /// </summary>
    internal class MarkAction : AbstractPlayerAction
    {
        /// <summary>
        /// The ID of the node whose sphere marker was toggled by this action.
        /// </summary>
        private string toggledNodeID;

        /// <summary>
        /// If the user clicks on a graph node, the marking of this node is toggled.
        /// </summary>
        /// <returns>True if completed.</returns>
        public override bool Update()
        {
            if (UserSetting.IsDesktop
                && Input.GetMouseButtonDown(0)
                && Raycasting.RaycastGraphElement(out RaycastHit raycastHit, out GraphElementRef ger, false) == HitGraphElement.Node
                && ger.gameObject.TryGetComponent(out InteractableObject io)
                && io.IsInteractable(raycastHit.point))
            {
                toggledNodeID = ger.gameObject.name;
                Toggle(ger.gameObject);
                CurrentState = IReversibleAction.Progress.Completed;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Toggles the marking of <paramref name="node"/> for everyone.
        /// </summary>
        /// <param name="node">The node whose marking is to be toggled.</param>
        private void Toggle(GameObject node)
        {
            GameNodeMarker.ToggleMarker(node);
            new MarkNetAction(toggledNodeID).Execute();
        }

        /// <summary>
        /// Undoes this action by toggling the marking of the node again.
        /// </summary>
        public override void Undo()
        {
            base.Undo();
            ToggleByID();
        }

        /// <summary>
        /// Redoes this action by toggling the marking of the node again.
        /// </summary>
        public override void Redo()
        {
            base.Redo();
            ToggleByID();
        }

        /// <summary>
        /// Toggles the marking of the node with <see cref="toggledNodeID"/>, if it still exists.
        /// </summary>
        private void ToggleByID()
        {
            GameObject node = GraphElementIDMap.Find(toggledNodeID);
            if (node != null)
            {
                Toggle(node);
            }
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
        /// Returns the IDs of all game objects manipulated by this action.
        /// </summary>
        /// <returns>The ID of the toggled node.</returns>
        public override HashSet<string> GetChangedObjects()
        {
            return new HashSet<string> { toggledNodeID };
        }

        /// <summary>
        /// Returns the <see cref="ActionStateType"/> of this action.
        /// </summary>
        /// <returns><see cref="ActionStateTypes.Mark"/>.</returns>
        public override ActionStateType GetActionStateType()
        {
            return ActionStateTypes.Mark;
        }
    }
}