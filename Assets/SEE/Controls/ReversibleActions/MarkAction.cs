using System.Collections.Generic;
using UnityEngine;
using SEE.Utils;
using SEE.GraphElementRefs;
using SEE.Net.Actions.GraphElement;
using SEE.SceneManipulation;
using SEE.UserSettings;

namespace SEE.Controls.ReversibleActions
{
    /// <summary>
    /// Marks or unmarks a selected node.
    /// </summary>
    internal class MarkAction : AbstractPlayerAction
    {
        /// <summary>
        /// The node affected by this action.
        /// </summary>
        private GameObject markedNode;

        /// <summary>
        /// The ID of the affected node.
        /// </summary>
        private string markedNodeID;

        /// <summary>
        /// Whether the node was marked before this action.
        /// </summary>
        private bool wasMarked;

        /// <summary>
        /// Color used for the marker.
        /// </summary>
        private readonly Color color = Color.yellow;

        /// <summary>
        /// Waits for the user to select a node and toggles its marker.
        /// </summary>
        public override bool Update()
        {
            if (!UserSetting.IsDesktop
                || !Input.GetMouseButtonDown(0)
                || Raycasting.RaycastGraphElement(
                    out RaycastHit raycastHit,
                    out GraphElementRef graphElementRef,
                    false) != HitGraphElement.Node)
            {
                return false;
            }

            markedNode = graphElementRef.gameObject;

            if (markedNode == null)
            {
                return false;
            }

            markedNodeID = markedNode.name;
            wasMarked = GameNodeMarker.IsMarked(markedNode);

            bool shouldBeMarked = !wasMarked;

            ApplyState(shouldBeMarked);

            CurrentState = IReversibleAction.Progress.Completed;
            return true;
        }

        /// <summary>
        /// Applies the requested marker state locally and over the network.
        /// </summary>
        private void ApplyState(bool shouldBeMarked)
        {
            if (shouldBeMarked)
            {
                GameNodeMarker.Mark(markedNode, color);
            }
            else
            {
                GameNodeMarker.Unmark(markedNode);
            }

            new MarkNetAction(
                markedNodeID,
                shouldBeMarked,
                color
            ).Execute();
        }

        /// <summary>
        /// Undoes this action.
        /// </summary>
        public override void Undo()
        {
            base.Undo();

            if (markedNode == null)
            {
                return;
            }

            ApplyState(wasMarked);
        }

        /// <summary>
        /// Redoes this action.
        /// </summary>
        public override void Redo()
        {
            base.Redo();

            if (markedNode == null)
            {
                return;
            }

            ApplyState(!wasMarked);
        }

        /// <summary>
        /// Creates a new MarkAction.
        /// </summary>
        public static IReversibleAction CreateReversibleAction()
        {
            return new MarkAction();
        }

        /// <summary>
        /// Creates a new instance of this action.
        /// </summary>
        public override IReversibleAction NewInstance()
        {
            return CreateReversibleAction();
        }

        /// <summary>
        /// Returns the action type used by the player menu.
        /// </summary>
        public override ActionStateType GetActionStateType()
        {
            return ActionStateTypes.Mark;
        }

        /// <summary>
        /// Returns the IDs changed by this action.
        /// </summary>
        public override HashSet<string> GetChangedObjects()
        {
            return new HashSet<string>
            {
                markedNodeID
            };
        }
    }
}
