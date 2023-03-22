using System.Collections.Generic;
using SEE.Utils;
using UnityEngine;

namespace SEE.Controls.Actions
{
    public class VrMoveAction : AbstractMoveAction
    {
        /// <summary>
        /// The currently grabbed object if any.
        /// </summary>
        private GrabbedObject grabbedObject;

        private bool IsGrabbing = false;

        internal static ReversibleAction CreateReversibleAction() => new VrMoveAction(); // obsolete?

        public override ReversibleAction NewInstance() => new VrMoveAction();

        public override bool Update()
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// Returns true if the user is currently grabbing.
        /// </summary>
        /// <returns>true if user is grabbing</returns>
        private static bool UserIsGrabbing()
        {
            // Index of the left mouse button.
            const int LeftMouseButton = 0;
            // FIXME: We need a VR interaction, too.
            return Input.GetMouseButton(LeftMouseButton);
        }

        /// <summary>
        /// If no node is grabbed, nothing happens. Otherwise:
        /// (1) If the user is currently pointing on a node, the grabbed object
        /// will be re-parented onto this node (<see cref="AbstractMoveAction.GrabbedObject.Reparent(GameObject)"/>.
        ///
        /// (2) If the user currently not pointing to any node, the grabbed object
        /// will be un-parented (<see cref="AbstractMoveAction.GrabbedObject.UnReparent"/>.
        /// </summary>
        private void UpdateHierarchy()
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// Returns the set of IDs of all game objects changed by this action.
        /// <see cref="ReversibleAction.GetChangedObjects"/>
        /// </summary>
        /// <returns>returns the ID of the currently grabbed object if any; otherwise
        /// the empty set</returns>
        public override HashSet<string> GetChangedObjects()
        {
            return grabbedObject.IsGrabbed ? new HashSet<string> { grabbedObject.Name }
                : new HashSet<string>();
        }

        /// <summary>
        /// <see cref="ReversibleAction.Undo"/>.
        /// </summary>
        public override void Undo()
        {
            base.Undo();
            grabbedObject.Undo();
        }

        /// <summary>
        /// <see cref="ReversibleAction.Redo"/>.
        /// </summary>
        public override void Redo()
        {
            base.Redo();
            grabbedObject.Redo();
        }
    }
}