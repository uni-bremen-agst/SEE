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
    /// Action to mark or unmark a selected node in a code city.
    /// </summary>
    internal class MarkAction : AbstractPlayerAction
    {
        /// <summary>
        /// The node that was marked or unmarked by this action.
        /// </summary>
        private GameObject markedNode;

        /// <summary>
        /// The ID of the node that was marked or unmarked by this action.
        /// </summary>
        private string markedNodeID;

        /// <summary>
        /// If the user clicks on a game object representing a graph node,
        /// the node is marked. If it was already marked, it is unmarked.
        /// </summary>
        /// <returns>True if completed.</returns>
        public override bool Update()
        {
            if (UserSetting.IsDesktop
                && Input.GetMouseButtonDown(0)
                && Raycasting.RaycastGraphElement(out RaycastHit raycastHit,
                                                  out GraphElementRef graphElementRef,
                                                  false) == HitGraphElement.Node
                && graphElementRef.gameObject.TryGetComponent(out InteractableObject interactableObject)
                && interactableObject.IsInteractable(raycastHit.point))
            {
                Mark(raycastHit.collider.gameObject);
                return true;
            }

            if (UserSetting.IsVR
                && XRSEEActions.Selected
                && InteractableObject.HoveredObjectWithWorldFlag.gameObject != null
                && InteractableObject.HoveredObjectWithWorldFlag.gameObject.HasNodeRef()
                && XRSEEActions.RayInteractor.TryGetCurrent3DRaycastHit(out RaycastHit vrRaycastHit))
            {
                XRSEEActions.Selected = false;
                Mark(vrRaycastHit.collider.gameObject);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Marks the <paramref name="node"/> if it is not marked yet.
        /// Otherwise it removes its marker.
        /// </summary>
        /// <param name="node">Node to mark or unmark.</param>
        private void Mark(GameObject node)
        {
            markedNode = node;
            markedNodeID = node.name;

            GameNodeMarker.ToggleMark(node);
            new MarkNetAction(node.name).Execute();

            CurrentState = IReversibleAction.Progress.Completed;
        }

        /// <summary>
        /// Undoes this action by toggling the marker again.
        /// </summary>
        public override void Undo()
        {
            base.Undo();

            markedNode = markedNode != null
                ? markedNode
                : GraphElementIDMap.Find(markedNodeID);

            if (markedNode != null)
            {
                GameNodeMarker.ToggleMark(markedNode);
                new MarkNetAction(markedNode.name).Execute();
            }
        }

        /// <summary>
        /// Redoes this action by toggling the marker again.
        /// </summary>
        public override void Redo()
        {
            base.Redo();

            markedNode = markedNode != null
                ? markedNode
                : GraphElementIDMap.Find(markedNodeID);

            if (markedNode != null)
            {
                GameNodeMarker.ToggleMark(markedNode);
                new MarkNetAction(markedNode.name).Execute();
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
        /// Returns the <see cref="ActionStateType"/> of this action.
        /// </summary>
        /// <returns><see cref="ActionStateTypes.Mark"/>.</returns>
        public override ActionStateType GetActionStateType()
        {
            return ActionStateTypes.Mark;
        }

        /// <summary>
        /// Returns the ID of the game object.
        /// </summary>
        /// <returns>ID of the marked game object.</returns>
        public override HashSet<string> GetChangedObjects()
        {
            return new HashSet<string>
            {
                markedNodeID
            };
        }
    }
}
