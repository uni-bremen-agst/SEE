using System.Collections.Generic;
using SEE.Game.City;
using SEE.GO;
using SEE.Utils;
using UnityEngine;
using Node = SEE.DataModel.DG.Node;

namespace SEE.Controls.Actions
{
    /// <summary>
    /// An action to grab, move, and drop nodes.
    /// </summary>
    internal class DesktopMoveAction : AbstractMoveAction
    {
        /// <summary>
        /// Returns a new instance of <see cref="MoveAction"/>.
        /// </summary>
        /// <returns>new instance of <see cref="MoveAction"/></returns>
        internal static ReversibleAction CreateReversibleAction() => new DesktopMoveAction();

        /// <summary>
        /// Returns a new instance of <see cref="MoveAction"/> that can continue
        /// with the user interaction so far.
        /// </summary>
        /// <returns>new instance</returns>
        public override ReversibleAction NewInstance() => new DesktopMoveAction();

        /// <summary>
        /// The currently grabbed object if any.
        /// </summary>
        private GrabbedObject grabbedObject;

        /// <summary>
        /// Reacts to the user interactions. An object can be grabbed and moved
        /// around. If it is put onto another node, it will be re-parented onto this
        /// node. If we are operating in a <see cref="SEEReflexionCity"/>, re-parenting
        /// may be a mapping of an implementation node onto an architecture node
        /// or a hierarchical re-parenting. If we are operating in a different kind
        /// of city, re-parenting is always hierarchically interpreted. A hierarchical
        /// re-parenting means that the moved node becomes a child of the target node
        /// both in the game-node hierarchy as well as in the underlying graph.
        /// <seealso cref="ReversibleAction.Update"/>.
        /// </summary>
        /// <returns>true if completed</returns>
        public override bool Update()
        {
            if (UserIsGrabbing()) // start to grab the object or continue to move the grabbed object
            {
                if (!grabbedObject.IsGrabbed)
                {
                    // User is starting dragging the currently hovered object.
                    InteractableObject hoveredObject = InteractableObject.HoveredObjectWithWorldFlag;
                    // An object to be grabbed must be representing a node that is not the root.
                    if (hoveredObject && hoveredObject.gameObject.TryGetNode(out Node node) && !node.IsRoot())
                    {
                        grabbedObject.Grab(hoveredObject.gameObject);
                        // Remember the current distance from the pointing device to the grabbed object.
                        distanceToUser = Vector3.Distance(Raycasting.UserPointsTo().origin, grabbedObject.Position);
                        currentState = ReversibleAction.Progress.InProgress;
                    }
                }
                else // continue moving the grabbed object
                {
                    // The grabbed object will be moved on the surface of a sphere with
                    // radius distanceToUser in the direction the user is pointing to.
                    Ray ray = Raycasting.UserPointsTo();
                    grabbedObject.MoveTo(ray.origin + distanceToUser * ray.direction);
                }

                // The grabbed node is not yet at its final destination. The user is still moving
                // it. We will run a what-if reflexion analysis to give immediate feedback on the
                // consequences if the user were putting the grabbed node onto the node the user
                // is currently aiming at.
                UpdateHierarchy();
            }
            else if (grabbedObject.IsGrabbed) // dragging has ended
            {
                // Finalize the action with the grabbed object.
                grabbedObject.UnGrab();
                // Action is finished.
                currentState = ReversibleAction.Progress.Completed;
                return true;
            }
            return false;
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
            if (grabbedObject.IsGrabbed)
            {
                if (Raycasting.RaycastLowestNode(out RaycastHit? raycastHit, out Node _, grabbedObject.Node))
                {
                    // Note: the root node can never be grabbed. See above.
                    if (raycastHit.HasValue)
                    {
                        // The user is currently aiming at a node. The grabbed node is reparented onto this aimed node.
                        grabbedObject.Reparent(raycastHit.Value.transform.gameObject);
                    }
                    else
                    {
                        // The user is currently not aiming at a node. The reparenting
                        // of the grabbed must be reverted.
                        grabbedObject.UnReparent();
                    }
                }
            }
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