using System.Collections.Generic;
using SEE.Extensions;
using SEE.GraphElementRefs;
using SEE.Net.Actions.GraphElement;
using SEE.SceneManipulation;
using SEE.UserSettings;
using SEE.Utils;
using SEE.XR;
using UnityEngine;

namespace SEE.Controls.ReversibleActions
{
    /// <summary>
    /// Toggles a marker (a sphere) above a node the user clicks on.
    /// If the node is not marked, a sphere is added above it; if it is
    /// already marked, the sphere is removed.
    /// </summary>
    internal class MarkAction : AbstractPlayerAction
    {
        /// <summary>
        /// The color of the marker sphere.
        /// </summary>
        private static readonly Color markerColor = Color.magenta;

        /// <summary>
        /// The information we need to undo/redo this action.
        /// </summary>
        private struct Memento
        {
            /// <summary>
            /// The ID of the (un)marked node. We keep the ID instead of the game object,
            /// because the game object may have been destroyed and re-created in the meantime.
            /// </summary>
            public string NodeID;

            /// <summary>
            /// True if the node was marked by this action, false if its marker was removed.
            /// </summary>
            public bool Marked;
        }

        /// <summary>
        /// Memento for undo/redo. Only valid once the action has been completed.
        /// </summary>
        private Memento memento;

        /// <summary>
        /// Waits until the user selects a node, then toggles its marker.
        /// See <see cref="IReversibleAction.Update"/>.
        /// </summary>
        /// <returns>True if completed.</returns>
        public override bool Update()
        {
            GameObject node = null;

            if (UserSetting.IsDesktop && Input.GetMouseButtonDown(0)
                && Raycasting.RaycastGraphElement(out RaycastHit raycastHit, out GraphElementRef _, false) == HitGraphElement.Node)
            {
                node = raycastHit.collider.gameObject;
            }
            else if (UserSetting.IsVR
                     && XRSEEActions.Selected
                     && InteractableObject.HoveredObjectWithWorldFlag != null
                     && InteractableObject.HoveredObjectWithWorldFlag.gameObject != null
                     && InteractableObject.HoveredObjectWithWorldFlag.gameObject.HasNodeRef())
            {
                XRSEEActions.Selected = false;
                node = InteractableObject.HoveredObjectWithWorldFlag.gameObject;
            }

            if (node == null)
            {
                return false;
            }

            memento = new Memento
            {
                NodeID = node.name,
                Marked = !GameNodeMarker.IsMarked(node)
            };
            Apply(memento.Marked);
            CurrentState = IReversibleAction.Progress.Completed;
            return true;
        }

        /// <summary>
        /// Sets the marker state of the node in <see cref="memento"/> locally and on all
        /// other clients.
        /// </summary>
        /// <param name="mark">True to mark, false to unmark.</param>
        private void Apply(bool mark)
        {
            GameObject node = GraphElementIDMap.Find(memento.NodeID);
            if (node == null)
            {
                return;
            }
            GameNodeMarker.SetMarked(node, mark, markerColor);
            new MarkNetAction(memento.NodeID, mark, markerColor).Execute();
        }

        /// <summary>
        /// Restores the marker state from before the action.
        /// </summary>
        public override void Undo()
        {
            base.Undo();
            Apply(!memento.Marked);
        }

        /// <summary>
        /// Restores the marker state after the action.
        /// </summary>
        public override void Redo()
        {
            base.Redo();
            Apply(memento.Marked);
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
        /// Returns the ID of the (un)marked node.
        /// </summary>
        /// <returns>The IDs of all game objects changed by this action.</returns>
        public override HashSet<string> GetChangedObjects()
        {
            return new HashSet<string> { memento.NodeID };
        }
    }
}
