using System.Collections.Generic;
using UnityEngine;
using SEE.SceneManipulation;
using SEE.Extensions;
using SEE.Utils;
using SEE.XR;
using SEE.GraphElementRefs;
using SEE.UserSettings;
using SEE.Net.Actions.GraphElement;

namespace SEE.Controls.ReversibleActions
{
    /// <summary>
    /// Action to mark a node by a sphere above it or to remove this mark again.
    /// </summary>
    internal class MarkAction : AbstractPlayerAction
    {
        /// <summary>
        /// The node marked or unmarked by this action. May become null, in which
        /// case it is looked up again by <see cref="Memento.NodeID"/>.
        /// </summary>
        private GameObject markedNode;

        /// <summary>
        /// Memento capturing the data necessary to undo and redo this action.
        /// </summary>
        private Memento memento;

        /// <summary>
        /// The information we need to undo and redo this action.
        /// </summary>
        private struct Memento
        {
            /// <summary>
            /// The ID of the node marked or unmarked by this action.
            /// </summary>
            public string NodeID;
            /// <summary>
            /// Whether the node was marked before this action was executed.
            /// </summary>
            public bool WasMarked;
        }

        /// <summary>
        /// If the user selects an active node, the node is marked if it was not marked
        /// before and unmarked otherwise.
        /// <see cref="IReversibleAction.Update"/>.
        /// </summary>
        /// <returns>True if completed.</returns>
        public override bool Update()
        {
            GameObject node = null;
            if (UserSetting.IsDesktop && Input.GetMouseButtonDown(0)
                && Raycasting.RaycastGraphElement(out RaycastHit raycastHit, out GraphElementRef ger, false) == HitGraphElement.Node
                && ger.gameObject.TryGetComponent(out InteractableObject io)
                && io.IsInteractable(raycastHit.point))
            {
                node = raycastHit.collider.gameObject;
            }
            else if (UserSetting.IsVR
                && XRSEEActions.Selected
                && InteractableObject.HoveredObjectWithWorldFlag.gameObject != null
                && InteractableObject.HoveredObjectWithWorldFlag.gameObject.HasNodeRef()
                && XRSEEActions.RayInteractor.TryGetCurrent3DRaycastHit(out raycastHit))
            {
                XRSEEActions.Selected = false;
                node = raycastHit.collider.gameObject;
            }

            // An inactive node has no size, so its sphere would be invisible.
            if (node == null || !node.activeInHierarchy)
            {
                return false;
            }

            markedNode = node;
            memento = new Memento
            {
                NodeID = node.name,
                WasMarked = GameNodeMarker.IsMarked(node)
            };
            SetMark(!memento.WasMarked);
            CurrentState = IReversibleAction.Progress.Completed;
            return true;
        }

        /// <summary>
        /// Undoes this action.
        /// </summary>
        public override void Undo()
        {
            base.Undo();
            SetMark(memento.WasMarked);
        }

        /// <summary>
        /// Redoes this action.
        /// </summary>
        public override void Redo()
        {
            base.Redo();
            SetMark(!memento.WasMarked);
        }

        /// <summary>
        /// Marks (if <paramref name="mark"/> is true) or unmarks the node of this action
        /// locally and on all other clients.
        /// </summary>
        /// <param name="mark">Whether the node should be marked.</param>
        private void SetMark(bool mark)
        {
            if (markedNode == null)
            {
                markedNode = GraphElementIDMap.Find(memento.NodeID);
            }
            if (markedNode == null)
            {
                Debug.LogWarning($"Node {memento.NodeID} no longer exists and cannot be (un)marked.\n");
                return;
            }
            if (mark)
            {
                GameNodeMarker.Mark(markedNode);
            }
            else
            {
                GameNodeMarker.Unmark(markedNode);
            }
            new MarkNetAction(memento.NodeID, mark).Execute();
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
        /// Returns the ID of the node marked or unmarked by this action, if any.
        /// </summary>
        /// <returns>The ID of the node or an empty set if no node was selected yet.</returns>
        public override HashSet<string> GetChangedObjects()
        {
            HashSet<string> result = new();
            if (memento.NodeID != null)
            {
                result.Add(memento.NodeID);
            }
            return result;
        }
    }
}
