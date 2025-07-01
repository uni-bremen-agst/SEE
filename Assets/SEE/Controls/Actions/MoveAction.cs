using System;
using System.Collections.Generic;
using UnityEngine;
using SEE.Audio;
using SEE.Controls.Interactables;
using SEE.DataModel.DG;
using SEE.Game;
using SEE.Game.City;
using SEE.Game.SceneManipulation;
using SEE.GO;
using SEE.Net.Actions;
using SEE.Tools.ReflexionAnalysis;
using SEE.UI.Notification;
using SEE.Utils;
using SEE.Utils.History;
using SEE.XR;

namespace SEE.Controls.Actions
{
    /// <summary>
    /// An action to grab, move, and drop nodes.
    /// </summary>
    internal class MoveAction : AbstractPlayerAction
    {
        /// <summary>
        /// The currently grabbed object if any.
        /// </summary>
        private GrabbedObject grabbedObject;

        /// <summary>
        /// The object to move which was selected via context menu.
        /// </summary>
        private GameObject contextMenuObjectToMove;

        /// <summary>
        /// The offset of the cursor to the pivot of <see cref="GrabbedGameObject"/>.
        /// </summary>
        private Vector3 cursorOffset = Vector3.zero;

        #region ReversibleAction
        /// <summary>
        /// This is true if the user is dragging an object.
        /// We use this in VR to activate/deactivate this action.
        /// </summary>
        private bool activeAction;

        /// <summary>
        /// Returns a new instance of <see cref="MoveAction"/>.
        /// </summary>
        /// <returns>new instance of <see cref="MoveAction"/></returns>
        internal static IReversibleAction CreateReversibleAction() => new MoveAction();

        /// <summary>
        /// Returns a new instance of <see cref="MoveAction"/>.
        /// </summary>
        /// <returns>new instance</returns>
        public override IReversibleAction NewInstance() => new MoveAction();

        /// <summary>
        /// Returns the set of IDs of all game objects changed by this action.
        /// <see cref="IReversibleAction.GetChangedObjects"/>
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
            return ActionStateTypes.Move;
        }

        /// <summary>
        /// Reacts to the user interactions. An object can be grabbed and moved
        /// around. If it is put onto another node, it will be re-parented onto this
        /// node. If we are operating in a <see cref="SEEReflexionCity"/>, re-parenting
        /// may be a mapping of an implementation node onto an architecture node
        /// or a hierarchical re-parenting. If we are operating in a different kind
        /// of city, re-parenting is always hierarchically interpreted. A hierarchical
        /// re-parenting means that the moved node becomes a child of the target node
        /// both in the game-node hierarchy as well as in the underlying graph.
        /// <seealso cref="IReversibleAction.Update"/>.
        /// </summary>
        /// <returns><c>true</c> if completed</returns>
        public override bool Update()
        {
            if (Raycasting.IsMouseOverGUI())
            {
                return false;
            }

            bool mouseHeldDown = SEEInput.LeftMouseInteraction();

            // Grab object
            if (!grabbedObject.IsGrabbed)
            {
                if ((SEEInput.LeftMouseDown() || (XRSEEActions.Selected && !activeAction))
                    && !ExecuteViaContextMenu)
                {
                    if (Raycasting.RaycastInteractableObject(out RaycastHit raycastHit, out InteractableObject io) != HitGraphElement.Node)
                    {
                        return false;
                    }
                    cursorOffset = raycastHit.point - io.gameObject.transform.position;

                    // An object to be grabbed must be representing a node that is not the root.
                    if (io.gameObject.TryGetNode(out Node node) && !node.IsRoot() && !node.IsArchitectureOrImplementationRoot())
                    {
                        XRSEEActions.Selected = false;
                        activeAction = true;
                        grabbedObject.Grab(io.gameObject);
                        AudioManagerImpl.EnqueueSoundEffect(IAudioManager.SoundEffect.PickupSound, io.gameObject, true);
                        CurrentState = IReversibleAction.Progress.InProgress;
                    }
                    else
                    {
                        XRSEEActions.Selected = false;
                        return false;
                    }
                }
                else if ((!mouseHeldDown || SceneSettings.InputType == PlayerInputType.VRPlayer) && ExecuteViaContextMenu)
                {
                    // User starts dragging object selected via context menu.
                    // Override the initial cursorOffset based on new mouse position to reduce jump
                    if (contextMenuObjectToMove.TryGetNodeRef(out NodeRef nodeRef)
                        && Raycasting.RaycastLowestNode(out RaycastHit? targetObjectHit, out Node _, nodeRef))
                    {
                        // Calculate position on object and close to the cursor
                        Vector3 objectSize = contextMenuObjectToMove.WorldSpaceSize();
                        Vector3 objectPosition = contextMenuObjectToMove.transform.position;
                        Vector3 anchorPosition = targetObjectHit.Value.point;
                        anchorPosition.x = Mathf.Clamp(anchorPosition.x,
                            objectPosition.x - 0.5f * objectSize.x,
                            objectPosition.x + 0.5f * objectSize.x);
                        anchorPosition.z = Mathf.Clamp(anchorPosition.z,
                            objectPosition.z - 0.5f * objectSize.z,
                            objectPosition.z + 0.5f * objectSize.z);
                        cursorOffset = anchorPosition - objectPosition;
                    }
                    XRSEEActions.Selected = false;
                    grabbedObject.Grab(contextMenuObjectToMove);
                    activeAction = true;
                    CurrentState = IReversibleAction.Progress.InProgress;
                }
            }
            // Drag grabbed object
            else if (
                    ((SceneSettings.InputType == PlayerInputType.DesktopPlayer && (mouseHeldDown ^ ExecuteViaContextMenu)) // exclusive OR
                        || (SceneSettings.InputType == PlayerInputType.VRPlayer && !XRSEEActions.Selected))
                    && activeAction)
            {
                Raycasting.RaycastLowestNode(out RaycastHit? targetObjectHit, out Node _, grabbedObject.Node, false);
                if (targetObjectHit.HasValue)
                {
                    GameObject newTarget = targetObjectHit.Value.transform.gameObject;
                    grabbedObject.MoveToTarget(newTarget, targetObjectHit.Value.point - cursorOffset);
                    // The grabbed node is not yet at its final destination. The user is still moving
                    // it. We will run a what-if reflexion analysis to give immediate feedback on the
                    // consequences if the user were putting the grabbed node onto the node the user
                    // is currently aiming at.
                    grabbedObject.Reparent(newTarget, true);
                    grabbedObject.NewParentID = newTarget.name;
                }
            }
            // End dragging
            else
            {
                if (grabbedObject.GrabbedGameObject != null)
                {
                    AudioManagerImpl.EnqueueSoundEffect(IAudioManager.SoundEffect.DropSound,
                        grabbedObject.GrabbedGameObject, true);
                }
                activeAction = false;
                ExecuteViaContextMenu = false;
                XRSEEActions.Selected = false;
                bool wasMoved = grabbedObject.UnGrab();
                // Action is finished.
                CurrentState = wasMoved ? IReversibleAction.Progress.Completed : IReversibleAction.Progress.NoEffect;
                return wasMoved;
            }
            return false;
        }

        /// <summary>
        /// <see cref="IReversibleAction.Undo"/>.
        /// </summary>
        public override void Undo()
        {
            base.Undo();
            grabbedObject.Undo();
        }

        /// <summary>
        /// <see cref="IReversibleAction.Redo"/>.
        /// </summary>
        public override void Redo()
        {
            base.Redo();
            grabbedObject.Redo();
        }

        #endregion ReversibleAction

        /// <summary>
        /// Used to execute the <see cref="MoveAction"/> from the context menu.
        /// It ensures that the <see cref="Update"/> method performs the execution via context menu for
        /// the selected game object <paramref name="objToMove"/>.
        /// </summary>
        /// <param name="objToMove">The object to be moved.</param>
        /// <param name="raycastHitPosition">The hit position of the object</param>
        public void ContextMenuExecution(GameObject objToMove, Vector3 raycastHitPosition)
        {
            ExecuteViaContextMenu = true;
            cursorOffset = raycastHitPosition - objToMove.transform.position;
            contextMenuObjectToMove = objToMove;
        }

        /// <summary>
        /// Data structure to manage the game object that was grabbed.
        /// Provides also the necessary capability for Undo/Redo.
        /// </summary>
        private struct GrabbedObject
        {
            /// <summary>
            /// The game object that is currently grabbed.
            /// </summary>
            public GameObject GrabbedGameObject
            {
                get;
                private set;
            }

            /// <summary>
            /// The ID of the grabbed game object.
            /// Required for undoing after a redraw.
            /// </summary>
            private string grabbedObjID;

            /// <summary>
            /// Whether an object has been grabbed.
            /// </summary>
            /// <returns><c>true</c> if an object has been grabbed</returns>
            /// <remarks>The default value for <c>bool</c> is <c>false</c>, which is exactly
            /// what we need</remarks>
            internal bool IsGrabbed { private set; get; }

            /// <summary>
            /// The name of the grabbed object if any was grabbed; otherwise the empty string.
            /// </summary>
            internal readonly string Name => GrabbedGameObject != null ? GrabbedGameObject.name : string.Empty;

            /// <summary>
            /// The node reference associated with the grabbed object. May be null if no
            /// node is associated with the grabbed object.
            /// </summary>
            public readonly NodeRef Node => GrabbedGameObject.TryGetNodeRef(out NodeRef result) ? result : null;

            /// <summary>
            /// Memorizes the new parent of <see cref="grabbedObject"/> after it was moved.
            /// Can be the original parent. Relevant for <see cref="Redo"/>.
            /// </summary>
            internal GameObject NewParent { private set; get; }

            /// <summary>
            /// The new parent id of <see cref="grabbedObject"/> after it was grabbed.
            /// Required for undoing after a redraw.
            /// </summary>
            public string NewParentID;

            /// <summary>
            /// The original parent of <see cref="grabbedObject"/> before it was grabbed.
            /// </summary>
            private Transform originalParent;

            /// <summary>
            /// The original parent id of <see cref="grabbedObject"/>.
            /// Required for undoing after a redraw.
            /// </summary>
            private string originalParentID;

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
                    GrabbedGameObject = gameObject;
                    grabbedObjID = gameObject.name;
                    if (SceneSettings.InputType == PlayerInputType.VRPlayer)
                    {
                        originalParent = XRSEEActions.OldParent;
                    }
                    else
                    {
                        originalParent = gameObject.transform.parent;
                    }
                    originalParentID = originalParent.name;
                    originalWorldPosition = gameObject.transform.position;
                    IsGrabbed = true;
                    if (gameObject.TryGetComponent(out InteractableObject interactableObject))
                    {
                        interactableObject.SetGrab(grab: true, isInitiator: true);
                    }

                    // We need to know whether we are in a reflexion city in order to
                    // interpret the re-parenting of a node properly.
                    withinReflexionCity = gameObject.ContainingCity<SEEReflexionCity>() != null;

                    if (withinReflexionCity)
                    {
                        // Beginning to drag to, e.g., create a new mapping should lead to a new version,
                        // because then changes will be highlighted relative to the state before moving.
                        NewVersion(gameObject);
                    }
                }
                else
                {
                    throw new ArgumentNullException(nameof(gameObject));
                }
            }

            /// <summary>
            /// Ends the move action by making the new position final.
            /// <para>
            /// The grabbed object will be reset to its original position if placement at the current position is not possible.
            /// </para>
            /// </summary>
            /// <returns><c>true</c> if the object has been placed, or <c>false</c> if placement has been reset</returns>
            /// <exception cref="InvalidOperationException">thrown if no object is currently
            /// grabbed</exception>
            public bool UnGrab()
            {
                if (!IsGrabbed)
                {
                    return false;
                }

                bool wasMoved = originalWorldPosition != currentPositionOfGrabbedObject;
                if (NewParent != null)
                {
                    if (CanBePlaced())
                    {
                        UnHighlightTarget();
                    }
                    else
                    {
                        // Node does not fit, resetting…
                        UnReparent();
                        XRSEEActions.OldParent = null;
                        wasMoved = false;
                    }
                }

                if (GrabbedGameObject.TryGetComponent(out InteractableObject interactableObject))
                {
                    interactableObject.SetGrab(grab: false, isInitiator: true);
                }
                ShowLabel.Off(GrabbedGameObject);
                IsGrabbed = false;
                // Note: We do not set grabbedObject to null because we may need its
                // value later for Undo/Redo.

                return wasMoved;
            }

            /// <summary>
            /// Checks if <see cref="grabbedObject"/> can be placed.
            /// <para>
            /// It can be placed if:
            /// <list type="bullet">
            /// <item><description><see cref="grabbedObject"/> fits in the 2D area of <see cref="NewParent"/>, and</description></item>
            /// <item><description><see cref="grabbedObject"/> does not overlap with its new siblings in <see cref="NewParent"/></description></item>
            /// </list>
            /// </para>
            /// </summary>
            /// <returns><c>true</c> if <see cref="grabbedObject"/> can be placed</returns>
            public readonly bool CanBePlaced()
            {
                Bounds2D parentBounds = new(NewParent);
                Bounds2D grabbedBounds = new(GrabbedGameObject);
                bool portalCheckPassed = true;

                if (Portal.GetPortal(GrabbedGameObject, out Vector2 leftFront, out Vector2 rightBack))
                {
                    Bounds2D portalBounds = Bounds2D.FromPortal(leftFront, rightBack);
                    portalCheckPassed = portalBounds.Contains(grabbedBounds);
                }

                return parentBounds.Contains(grabbedBounds)
                        && portalCheckPassed
                        && !GrabbedGameObject.OverlapsWithSiblings();
            }

            /// <summary>
            /// Returns the grabbed object to its original position when it was grabbed.
            /// This method will be called for Undo.
            /// </summary>
            private readonly void MoveToOrigin()
            {
                if (GrabbedGameObject == null)
                {
                    return;
                }
                MoveTo(GrabbedGameObject, originalWorldPosition);
            }

            /// <summary>
            /// Returns the grabbed object to its last position before it was returned
            /// to its origin via <see cref="MoveToOrigin"/>.
            /// This method will be called for Redo.
            /// </summary>
            private readonly void MoveToNewPosition()
            {
                if (GrabbedGameObject == null)
                {
                    return;
                }
                MoveTo(GrabbedGameObject, currentPositionOfGrabbedObject, 1);
            }

            /// <summary>
            /// Moves the grabbed game object onto <paramref name="targetGameObject"/> at the approximate position
            /// of <paramref name="targetPosition"/> in world space coordinates.
            /// The placement is immediate and without any animation.
            /// <para>
            /// The <paramref name="targetPosition"/> is adapted so that the grabbed node will appear on top of the
            /// <paramref name="targetGameObject"/>.
            /// </para><para>
            /// The <see cref="currentPositionOfGrabbedObject"/> will be updated to reflect the actual target
            /// world-space position after the move operation.
            /// </para><para>
            /// The <see cref="GrabbedGameObject"/> will NOT be reparented to the <paramref name="targetGameObject"/>.
            /// </para>
            /// </summary>
            /// <param name="targetGameObject">the game object to place the grabbed node on</param>
            /// <param name="targetPosition">the world-space position where the grabbed node should be moved</param>
            internal void MoveToTarget(GameObject targetGameObject, Vector3 targetPosition)
            {
                if (GrabbedGameObject == null)
                {
                    return;
                }
                currentPositionOfGrabbedObject = GameNodeMover.GetCoordinatesOn(GrabbedGameObject.WorldSpaceSize(),
                    targetPosition, targetGameObject);
                MoveTo(GrabbedGameObject, currentPositionOfGrabbedObject, 0);
            }

            #region HitColor

            /// <summary>
            /// The game node we have highlighted as a target of the move action.
            /// May be <c>null</c> if none has been marked.
            /// It is stored here because we need to remove the highlight later.
            /// </summary>
            private GameObject highlightedTarget;

            /// <summary>
            /// Highlights the current target, <see cref="NewParent"/>, visually and removes previous target highlight.
            /// </summary>
            private void HighlightTarget()
            {
                if (highlightedTarget == NewParent)
                {
                    return;
                }

                UnHighlightTarget();
                highlightedTarget = NewParent;

                // [Highlight Plus note] Important! If you change the hierarchy of your object
                // (change its parent or attach it to another object), you need to call effect.Refresh()
                // to make Highlight Plus update its internal data.
                Highlighter.SetHighlight(highlightedTarget, true);
                new HighlightNetAction(highlightedTarget.name, true).Execute();
            }

            /// <summary>
            /// Removes the highlight of <see cref="highlightedTarget"/> if not <c>null</c>.
            /// </summary>
            private void UnHighlightTarget()
            {
                if (highlightedTarget != null)
                {
                    Highlighter.SetHighlight(highlightedTarget, false);
                    new HighlightNetAction(highlightedTarget.name, false).Execute();
                    highlightedTarget = null;
                }
            }

            #endregion HitColor

            /// <summary>
            /// Restores the original state of the grabbed object just before it was grabbed.
            ///
            /// The new state of <see cref="grabbedObject"/> after this call is its inital
            /// state at the point in time when it was grabbed, i.e.
            /// <list type="number">
            /// <item><description>has its <see cref="originalWorldPosition"/></description></item>
            /// <item><description>has its <see cref="originalParent"/></description></item>
            /// <item><description>all side effects of re-parenting have been undone (e.g.,
            /// the parent of the graph node associated with <see cref="grabbedObject"/>
            /// is the graph node associated with <see cref="originalParent"/>;
            /// there may be additional side effects of re-parenting, however,
            /// triggered by the reflexion analysis; all of these are reverted).</description></item>
            /// </list>
            /// </summary>
            /// <remarks>
            /// Important note: Some of these changes do not come into effect immediately.
            /// They may be delayed by an animation.
            /// </remarks>
            internal void Undo()
            {
                if (originalParent == null)
                {
                    originalParent = GraphElementIDMap.Find(originalParentID).transform;
                }
                if (GrabbedGameObject == null)
                {
                    GrabbedGameObject = GraphElementIDMap.Find(grabbedObjID);
                }
                UnReparent();
            }

            /// <summary>
            /// Reverts <see cref="Undo"/>.
            ///
            /// <para>
            /// The new state of <see cref="grabbedObject"/> after this call is its
            /// state at the point in time just before <see cref="Undo"/> was called, i.e.
            /// <list type="number">
            /// <item><description>is put on the roof of <see cref="newParent"/> at the world-space position
            /// <see cref="currentPositionOfGrabbedObject"/></description></item>
            /// <item><description>has <see cref="newParent"/> as its game-object parent</description></item>
            /// <item><description>all side effects of re-parenting are applied (e.g.,
            /// the parent of the graph node associated with <see cref="grabbedObject"/>
            /// is the graph node associated with <see cref="newParent"/>
            /// if both game nodes represent implementation entities; there may be other
            /// side effects of re-parenting, however, triggered by the reflexion analysis;
            /// all of these are in place).</description></item>
            /// </list>
            /// </para>
            /// </summary>
            /// <remarks>
            /// Important note: Some of these changes do not come into effect immediately.
            /// They may be delayed by an animation.
            /// </remarks>
            internal void Redo()
            {
                if (NewParent == null)
                {
                    NewParent = GraphElementIDMap.Find(NewParentID);
                }
                MoveToNewPosition();
                if (NewParent != originalParent)
                {
                    Reparent(NewParent, false, true);
                }
            }

            /// <summary>
            /// Reparents the grabbed object to <paramref name="target"/>.
            /// <para>
            /// If the placement <paramref name="isProvisional"/>, the target node will be highlighted
            /// as well as the grabbed node. The color of the grabbed node's outline indicates if a
            /// placement is possible at the current position based available space on <paramref name="target"/>.
            /// </para><para>
            /// Also re-parents <see cref="grabbedObject"/> onto <paramref name="target"/> semantically.
            /// If <see cref="withinReflexionCity"/>, the exact semantics of the re-parenting
            /// is determined by <see cref="ReflexionMapper.SetParent"/>;
            /// otherwise by <see cref="GameNodeMover.SetParent"/>.
            /// </para>
            /// The <paramref name="grabbedObject"/> will be an immediate child of <paramref name="target"/> in the
            /// game-object hierarchy afterwards.
            /// </summary>
            /// <param name="target">the target node of the re-parenting, i.e., the new parent</param>
            /// <param name="isProvisional">if <c>true</c>, the new target is considered temporary</param>
            /// <param name="isUnOrRedo">if <c>true</c>, the method is executed as part of undo or redo</param>
            internal void Reparent(GameObject target, bool isProvisional, bool isUnOrRedo = false)
            {
                // Note: If target is a descendant of the grabbed node something must be wrong with the raycast!
                bool targetChanged = NewParent != target;
                NewParent = isUnOrRedo ? NewParent : target; // continue working with target!

                if (isProvisional)
                {
                    HighlightTarget();
                    if (GrabbedGameObject.TryGetComponent(out Outline outline))
                    {
                        outline.OutlineColor = CanBePlaced() ? Color.green : Color.red;
                    }
                }

                if (!targetChanged && !isUnOrRedo)
                {
                    return;
                }

                // The mapping is only possible if we are in a reflexion city
                // and the mapping target is not the root of the graph.
                if (withinReflexionCity && !target.IsRoot())
                {
                    ReflexionMapperSetParent(GrabbedGameObject, target);
                }
                else
                {
                    GameNodeMoverSetParent(GrabbedGameObject, target);
                }
            }

            /// <summary>
            /// Reverts the parenting of the <see cref="grabbedObject"/>, i.e., unmarks the
            /// current target and restores original position of <see cref="grabbedObject"/>.
            /// If <see cref="withinReflexionCity"/>, its explicit architecture mapping will be
            /// removed; otherwise its <see cref="originalParent"/> will be restored.
            ///
            /// <para>
            /// This method is the reverse function of <see cref="Reparent(GameObject)"/>.
            /// </para>
            /// </summary>
            internal void UnReparent()
            {
                UnHighlightTarget();
                Reparent(originalParent.gameObject, false, true);
                RestoreOriginalAppearance();
            }

            #region Basic Scene Manipulators Propagated to all Clients

            /// <summary>
            /// Creates a new version in the underlying graph, marking the start of a new movement.
            /// </summary>
            /// <param name="grabbedObject">the moved object</param>
            private static void NewVersion(GameObject grabbedObject)
            {
                GameNodeMover.NewMovementVersion(grabbedObject);
                new VersionNetAction(grabbedObject.name).Execute();
            }

            /// <summary>
            /// Moves the grabbed object to <paramref name="targetPosition"/> in world space.
            /// </summary>
            /// <param name="targetPosition">the world-space position the grabbed object should be moved to</param>
            /// <param name="factor">the factor of the animation for moving the grabbed object</param>
            /// <remarks>This is only a movement, not a change to any hierarchy.</remarks>
            private static void MoveTo(GameObject grabbedObject, Vector3 targetPosition, float factor = 1)
            {
                GameNodeMover.MoveTo(grabbedObject, targetPosition, factor);
                new MoveNetAction(grabbedObject.name, targetPosition, factor).Execute();
            }

            /// <summary>
            /// Runs <see cref="ReflexionMapper.SetParent"/> and propagates it to all clients.
            /// </summary>
            /// <param name="child">the child to be put on <paramref name="parent"/></param>
            /// <param name="parent">new parent of <paramref name="child"/></param>
            private static void ReflexionMapperSetParent(GameObject child, GameObject parent)
            {
                try
                {
                    ReflexionMapper.SetParent(child, parent);
                    new SetParentNetAction(child.name, parent.name, true).Execute();
                }
                catch (ArchitectureAnalysisException e)
                {
                    ShowNotification.Error("Reflexion Mapping",
                        $"Parenting {child.name} onto {parent.name} failed: {e.Message}");
                }
            }

            /// <summary>
            /// Runs <see cref="GameNodeMover.SetParent"/> and propagates it to all clients.
            ///
            /// <paramref name="child"/> will be an immediate child of <paramref name="parent"/>
            /// in the game-object hierarchy aftwards.
            /// </summary>
            /// <param name="child">the child to be put on <paramref name="parent"/></param>
            /// <param name="parent">new parent of <paramref name="child"/></param>
            private static void GameNodeMoverSetParent(GameObject child, GameObject parent)
            {
                try
                {
                    GameNodeMover.SetParent(child, parent);
                    new SetParentNetAction(child.name, parent.name, false).Execute();
                }
                catch (ArchitectureAnalysisException e)
                {
                    ShowNotification.Error("Re-parenting",
                        $"Parenting {child.name} onto {parent.name} failed: {e.Message}");
                }
            }

            #endregion Basic Scene Manipulators Propagated to all Clients

            /// <summary>
            /// Resets the marking of the target node and moves <see cref="grabbedObject"/>
            /// back to its <see cref="originalWorldPosition"/>.
            ///
            /// No changes are made to the game-node hierarchy or graph-node hierarchy.
            /// </summary>
            private void RestoreOriginalAppearance()
            {
                UnHighlightTarget();
                MoveToOrigin();
            }
        }
    }
}
