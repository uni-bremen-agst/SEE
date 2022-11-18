using System;
using System.Collections.Generic;
using SEE.DataModel.DG;
using SEE.Game;
using SEE.Game.City;
using SEE.Game.Operator;
using SEE.GO;
using SEE.Net.Actions;
using SEE.Utils;
using UnityEngine;

namespace SEE.Controls.Actions
{
    /// <summary>
    /// An action to grab, move, and drop nodes.
    /// </summary>
    internal class MoveAction : AbstractPlayerAction
    {
        /// <summary>
        /// Returns a new instance of <see cref="MoveAction"/>.
        /// </summary>
        /// <returns>new instance of <see cref="MoveAction"/></returns>
        internal static ReversibleAction CreateReversibleAction() => new MoveAction();

        /// <summary>
        /// Returns a new instance of <see cref="MoveAction"/> that can continue
        /// with the user interaction so far.
        /// </summary>
        /// <returns>new instance</returns>
        public override ReversibleAction NewInstance() => new MoveAction();

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
        /// Returns the <see cref="ActionStateType"/> of this action.
        /// </summary>
        /// <returns><see cref="ActionStateType.Move"/></returns>
        public override ActionStateType GetActionStateType()
        {
            return ActionStateType.Move;
        }

        /// <summary>
        /// Data structure to manage the game object that was grabbed.
        /// Provides also the necessary capability for Undo/Redo.
        /// </summary>
        private struct GrabbedObject
        {
            /// <summary>
            /// The game object that was grabbed. May be null.
            /// </summary>
            private GameObject grabbedObject;

            /// <summary>
            /// The duration of any animation to move the grabbed object for Undo/Redo
            /// in seconds.
            /// </summary>
            private const float AnimationTime = 1.0f;

            /// <summary>
            /// Whether the currently grabbed node is contained in a <see cref="SEEReflexionCity"/>.
            /// This needs to be known to interpret the re-parenting of a node properly.
            /// </summary>
            private bool withinReflexionCity;

            /// <summary>
            /// Grabs the given <paramref name="gameObject"/>.
            /// </summary>
            /// <param name="gameObject">object to be grabbed</param>
            /// <exception cref="ArgumentNullException">thrown if <paramref name="gameObject"/> is null</exception>
            public void Grab(GameObject gameObject)
            {
                if (gameObject != null)
                {
                    this.grabbedObject = gameObject;
                    originalParent = gameObject.transform.parent;
                    originalLocalScale = gameObject.transform.localScale;
                    originalWorldPosition = gameObject.transform.position;
                    IsGrabbed = true;
                    if (gameObject.TryGetComponent(out InteractableObject interactableObject))
                    {
                        interactableObject.SetGrab(grab: true, isInitiator: true);
                    }

                    // We need to know whether we are in a reflexion city in order to
                    // interpret the re-parenting of a node properly.
                    withinReflexionCity = gameObject.ContainingCity<SEEReflexionCity>() != null;
                }
                else
                {
                    throw new ArgumentNullException(nameof(gameObject));
                }
            }

            /// <summary>
            /// The currently grabbed object is considered to be ungrabbed.
            /// </summary>
            /// <exception cref="InvalidOperationException">thrown if no object is currently
            /// grabbed</exception>
            public void UnGrab()
            {
                if (!IsGrabbed || grabbedObject == null)
                {
                    throw new InvalidOperationException("No object is being grabbed.");
                }
                else
                {
                    UnmarkAsTarget();
                    if (grabbedObject.TryGetComponent(out InteractableObject interactableObject))
                    {
                        interactableObject.SetGrab(grab: false, isInitiator: true) ;
                    }
                    ShowLabel.Off(grabbedObject);
                    IsGrabbed = false;
                    // Note: We do not set grabbedObject to null because we may need its
                    // value later for Undo/Redo.
                }
            }

            /// <summary>
            /// Memorizes the new parent of <see cref="grabbedObject"/> after it was moved.
            /// Can be the original parent. Relevant for <see cref="Redo"/>.
            /// </summary>
            private GameObject newParent;

            /// <summary>
            /// The original parent of <see cref="grabbedObject"/> before it was grabbed.
            /// </summary>
            private Transform originalParent;

            /// <summary>
            /// The original local scale of <see cref="grabbedObject"/> before it was grabbed.
            /// </summary>
            private Vector3 originalLocalScale;

            /// <summary>
            /// Whether an object has been grabbed.
            /// </summary>
            /// <returns>true if an object has been grabbed</returns>
            /// <remarks>The default value for <c>bool</c> is <c>false</c>, which is exactly
            /// what we need</remarks>
            internal bool IsGrabbed { private set; get; }

            /// <summary>
            /// The name of the grabbed object if any was grabbed; otherwise the empty string.
            /// </summary>
            internal string Name
            {
                get => grabbedObject?.name ?? String.Empty;
            }

            /// <summary>
            /// The position of the grabbed object in world space.
            /// </summary>
            /// <exception cref="InvalidOperationException">in case no object is currently grabbed</exception>
            internal Vector3 Position
            {
                get
                {
                    if (IsGrabbed && grabbedObject != null)
                    {
                        return grabbedObject.transform.position;
                    }
                    else
                    {
                        throw new InvalidOperationException("No object is being grabbed.");
                    }
                }
            }

            /// <summary>
            /// The node reference associated with the grabbed object. May be null if no
            /// node is associated with the grabbed object.
            /// </summary>
            public NodeRef Node
            {
                get => grabbedObject.TryGetNodeRef(out NodeRef result) ? result : null;
            }

            /// <summary>
            /// The original position of <see cref="grabbedObject"/> when it was grabbed.
            /// Required to return it to its original position when the action is undone.
            /// </summary>
            private Vector3 originalWorldPosition;

            /// <summary>
            /// The current position of <see cref="GameObject"/> in world space. More precisely, the
            /// position that was last set via <see cref="MoveTo(Vector3)"/>.
            /// </summary>
            private Vector3 currentPositionOfGrabbedObject;

            /// <summary>
            /// Returns the grabbed object to its original position when it was grabbed.
            /// This method will be called for Undo.
            /// </summary>
            internal void MoveToOrigin()
            {
                if (grabbedObject)
                {
                    MoveTo(grabbedObject, originalWorldPosition, AnimationTime);
                }
            }

            /// <summary>
            /// Returns the grabbed object to its last position before it was returned
            /// to its origin via <see cref="MoveToOrigin"/>.
            /// This method will be called for Redo.
            /// </summary>
            private void MoveToLastUserRequestedPosition()
            {
                if (grabbedObject)
                {
                    MoveTo(grabbedObject, currentPositionOfGrabbedObject, AnimationTime);
                }
            }

            /// <summary>
            /// Moves the grabbed object to <paramref name="targetPosition"/> in world space
            /// immediately, that is, without any animation.
            /// </summary>
            /// <param name="targetPosition">the position where the grabbed object
            /// should be moved in world space</param>
            internal void MoveTo(Vector3 targetPosition)
            {
                if (grabbedObject)
                {
                    currentPositionOfGrabbedObject = targetPosition;
                    MoveTo(grabbedObject, targetPosition, 0);
                }
            }

            #region HitColor

            /// <summary>
            /// The game node we have marked as a target of the grabbed and moved node (where it
            /// should be put on / mapped onto). May be null if none has been marked. It is saved
            /// here because we need to do the unmarking later.
            /// </summary>
            private GameObject markedGameObject;

            /// <summary>
            /// Highlights <paramref name="hitObject"/> as a target of the grabbed and moved node.
            /// </summary>
            /// <param name="hitObject">the target of the grabbed and moved node</param>
            void MarkAsTarget(Transform hitObject)
            {
                markedGameObject = hitObject.gameObject;
                // [Highlight Plus note] Important! If you change the hierarchy of your object
                // (change its parent or attach it to another object), you need to call effect.Refresh()
                // to make Highlight Plus update its internal data.
                Highlighter.SetHighlight(markedGameObject, true);
                new HighlightNetAction(markedGameObject.name, true);
            }

            /// <summary>
            /// Turns off the highlighting of <see cref="markedGameObject"/> if not <c>null</c>.
            /// </summary>
            void UnmarkAsTarget()
            {
                if (markedGameObject)
                {
                    Highlighter.SetHighlight(markedGameObject, false);
                    new HighlightNetAction(markedGameObject.name, false).Execute();
                }
            }

            #endregion HitColor

            /// <summary>
            /// Restores the original state of the grabbed object just before it was grabbed.
            ///
            /// The new state of <see cref="grabbedObject"/> after this call is its inital
            /// state at the point in time when it was grabbed, i.e.
            /// (1) has its <see cref="originalWorldPosition"/>
            /// (2) has its <see cref="originalLocalScale"/>
            /// (3) has its <see cref="originalParent"/>
            /// (4) all side effects of re-parenting have been undone (e.g.,
            /// the parent of the graph node associated with <see cref="grabbedObject"/>
            /// is the graph node associated with <see cref="originalParent"/>;
            /// there may be additional side effects of re-parenting, however,
            /// triggered by the reflexion analysis; all of these are reverted).
            ///
            /// Important note: Some of these changes do not come into effect immediately.
            /// They may be delayed by an animation duration.
            /// </summary>
            internal void Undo()
            {
                UnReparent();
            }

            /// <summary>
            /// Reverts <see cref="Undo"/>.
            ///
            /// The new state of <see cref="grabbedObject"/> after this call is its
            /// state at the point in time just before <see cref="Undo"/> was called, i.e.
            ///
            /// (1) is put on the roof of <see cref="newParent"/> possibly scaled down to fit
            /// at the world-space position <see cref="currentPositionOfGrabbedObject"/>
            /// (2) has <see cref="newParent"/> as its game-object parent
            /// (4) all side effects of re-parenting have been are in place (e.g.,
            /// the parent of the graph node associated with <see cref="grabbedObject"/>
            /// is the graph node associated with <see cref="newParent"/>
            /// if both game nodes represent implementation entities; there may be other
            /// side effects of re-parenting, however, triggered by the reflexion analysis;
            /// all of these are in place).
            ///
            /// Important note: Some of these changes do not come into effect immediately.
            /// They may be delayed by an animation duration.
            /// </summary>
            internal void Redo()
            {
                MoveToLastUserRequestedPosition();
                Reparent(newParent);
            }

            /// <summary>
            /// Moves <see cref="grabbedObject"/> onto the roof of <paramref name="target"/>
            /// visually and marks it as target (the previously marked object is unmarked).
            /// If <see cref="withinReflexionCity"/>,the exact semantics of the re-parenting
            /// is determined by <see cref="ReflexionMapper.SetParent"/>;
            /// otherwise by <see cref="GameNodeMover.SetParent"/>.
            /// </summary>
            /// <param name="target">the target node of the re-parenting</param>
            internal void Reparent(GameObject target)
            {
                PutOnAndFit(grabbedObject, target, originalParent.gameObject, originalLocalScale);
                UnmarkAsTarget();
                MarkAsTarget(target.transform);

                if (target != newParent)
                {
                    newParent = target;
                    // The mapping is only possible if we are in a reflexion city
                    // and the mapping target is not the root of the graph.
                    if (withinReflexionCity && !target.IsRoot())
                    {
                        ReflexionMapperSetParent(grabbedObject, target);
                    }
                    else
                    {
                        GameNodeMoverSetParent(grabbedObject, target);
                    }
                }
            }

            /// <summary>
            /// Reverts the parenting of the <see cref="grabbedObject"/>, i.e., unmarks the
            /// current target and restores original position and scale of <see cref="grabbedObject"/>.
            /// If <see cref="withinReflexionCity"/>, its explicit architecture mapping will be
            /// removed; otherwise its <see cref="originalParent"/> will be restored.
            ///
            /// This method is the reverse function of <see cref="Reparent(GameObject)"/>.
            /// </summary>
            internal void UnReparent()
            {
                UnmarkAsTarget();
                if (grabbedObject.transform.parent.gameObject != originalParent.gameObject)
                {
                    if (withinReflexionCity)
                    {
                        ReflexionMapperSetParent(grabbedObject, originalParent.gameObject);
                    }
                    else
                    {
                        GameNodeMoverSetParent(grabbedObject, originalParent.gameObject);
                    }
                }
                RestoreOriginalAppearance();
            }

            #region Basic Scene Manipulators Propagated to all Clients

            /// <summary>
            /// Moves the grabbed object to <paramref name="targetPosition"/> in world space.
            /// </summary>
            /// <param name="targetPosition">the position where the grabbed object
            /// should be moved in world space</param>
            /// <param name="duration">the duration of the animation for moving the grabbed
            /// object in seconds</param>
            /// <remarks>This is only a movement, not a change to any hierarchy.</remarks>
            private static void MoveTo(GameObject grabbedObject, Vector3 targetPosition, float duration)
            {
                GameNodeMover.MoveTo(grabbedObject, targetPosition, duration);
                new MoveNetAction(grabbedObject.name, targetPosition, duration).Execute();
            }

            /// <summary>
            /// Runs <see cref="GameNodeMover.PutOnAndFit"/> and propagates it to all clients.
            /// </summary>
            /// <param name="child">the child to be put on <paramref name="newParent"/></param>
            /// <param name="newParent">new parent of <paramref name="child"/></param>
            /// <param name="originalParent">original parent of <paramref name="child"/></param>
            /// <param name="originalLocalScale">original local scale of <paramref name="child"/></param>
            private static void PutOnAndFit(GameObject child, GameObject newParent, GameObject originalParent, Vector3 originalLocalScale)
            {
                GameNodeMover.PutOnAndFit(child.transform, newParent, originalParent, originalLocalScale);
                new PutOnAndFitNetAction(child.name, newParent.name, originalParent.name, originalLocalScale).Execute();
            }

            /// <summary>
            /// Runs <see cref="ReflexionMapper.SetParent"/> and propagates it to all clients.
            /// </summary>
            /// <param name="child">the child to be put on <paramref name="parent"/></param>
            /// <param name="parent">new parent of <paramref name="child"/></param>
            private static void ReflexionMapperSetParent(GameObject child, GameObject parent)
            {
                ReflexionMapper.SetParent(child, parent);
                new SetParentNetAction(child.name, parent.name, true).Execute();

            }

            /// <summary>
            /// Runs <see cref="GameNodeMover.SetParent"/> and propagates it to all clients.
            /// </summary>
            /// <param name="child">the child to be put on <paramref name="parent"/></param>
            /// <param name="parent">new parent of <paramref name="child"/></param>
            private static void GameNodeMoverSetParent(GameObject child, GameObject parent)
            {
                GameNodeMover.SetParent(child, parent);
                new SetParentNetAction(child.name, parent.name, false).Execute();
            }

            #endregion Basic Scene Manipulators Propagated to all Clients

            /// <summary>
            /// Resets the marking of the target node and moves <see cref="grabbedObject"/>
            /// back to its <see cref="originalWorldPosition"/> and restores
            /// its <see cref="originalLocalScale"/>.
            ///
            /// No changes are made to the game-node hierarchy or graph-node hierarchy.
            /// </summary>
            private void RestoreOriginalAppearance()
            {
                UnmarkAsTarget();
                MoveToOrigin();
                if (grabbedObject.TryGetComponent(out NodeOperator nodeOperator))
                {
                    float animationTime = AnimationTime;
                    nodeOperator.ScaleTo(originalLocalScale, animationTime);
                    new ScaleNodeNetAction(grabbedObject.name, originalLocalScale, animationTime).Execute();
                }
            }
        }

        /// <summary>
        /// The currently grabbed object if any.
        /// </summary>
        private GrabbedObject grabbedObject = new GrabbedObject();

        /// <summary>
        /// The distance from the the position of <see cref="grabbedObject"/> when it was grabbed to
        /// the origin of the user's pointing device (e.g., main camera on a desktop, controller
        /// in VR) in world space. This distance will be maintained while the user has grabbed
        /// an object.
        /// </summary>
        private float distanceToUser;

        /// <summary>
        /// Re-acts to the user interactions. An object can be grabbed by and moved
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
                    else
                    {
                        // Debug.Log("Nothing to be dragged.\n");
                    }
                }
                else // continue moving the grabbed object
                {
                    // Assert: grabbedObject != null
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
            // FIXME: We need a VR interation, too.
            return Input.GetMouseButton(LeftMouseButton);
        }

        /// <summary>
        /// If no node is grabbed, nothing happens. Otherwise:
        /// (1) If the user is currently pointing on a node, the grabbed object
        /// will be re-parented onto this node (<see cref="GrabbedObject.Reparent(GameObject)"/>.
        ///
        /// (2) If the user currently not pointing to any node, the grabbed object
        /// will be un-parented (<see cref="GrabbedObject.UnReparent"/>.
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
                        // The user is currently aiming at a node. The grabbed node is reparented onto
                        // this aimed node.
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