using System;
using System.Collections.Generic;
using System.Linq;
using SEE.DataModel.DG;
using SEE.Game;
using SEE.Game.Operator;
using SEE.GO;
using SEE.Layout.EdgeLayouts;
using SEE.Net.Actions;
using SEE.Utils;
using TinySpline;
using UnityEngine;
using UnityEngine.Assertions;

namespace SEE.Controls.Actions
{
    /// <summary>
    /// An action to grab and move nodes.
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
        /// <returns>returns the ID of the currently grabbed object if any; otherwise the empty set</returns>
        public override HashSet<string> GetChangedObjects()
        {
            return grabbedObject.IsGrabbed ? new HashSet<string> { grabbedObject.Name } : new HashSet<string>();
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
        /// Provides the necessary capability for Undo/Redo.
        /// </summary>
        private struct GrabbedObject
        {
            /// <summary>
            /// The game object that was grabbed. May be null.
            /// </summary>
            private GameObject gameObject;

            /// <summary>
            /// The node operator to move <see cref="gameObject"/>. Different from null only
            /// if <see cref="gameObject"/> is different from null.
            /// </summary>
            private NodeOperator nodeOperator;

            /// <summary>
            /// The list of <see cref="SEESpline"/>s of the incoming and outgoing edges
            /// of <paramref name="gameNode"/>. The boolean in the returned pair indicates
            /// whether the edge is outgoing (if it is false, the edge is incoming).
            /// </summary>
            private IList<(SEESpline, bool nodeIsSource)> ConnectedEdges;

            /// <summary>
            /// The animation duration for morphing edges in seconds.
            /// FIXME: Why should that be different from <see cref="AnimationTime"/>?
            /// </summary>
            private const float SplineAnimationDuration = 2f;
            /// <summary>
            /// The duration of any animation to move the grabbed object for Undo/Redo
            /// in seconds.
            /// </summary>
            private const float AnimationTime = 1.0f;

            /// <summary>
            /// Grabs the given <paramref name="gameObject"/>.
            /// </summary>
            /// <param name="gameObject">object to be grabbed</param>
            /// <exception cref="ArgumentNullException">thrown if <paramref name="gameObject"/> is null</exception>
            public void Grab(GameObject gameObject)
            {
                if (gameObject != null)
                {
                    this.gameObject = gameObject;
                    nodeOperator = gameObject.AddOrGetComponent<NodeOperator>();
                    originalPositionOfGrabbedObject = gameObject.transform.position;
                    ConnectedEdges = GetConnectedEdges(gameObject);
                    MorphEdgesToSplines(SplineAnimationDuration);
                    IsGrabbed = true;
                    if (gameObject.TryGetComponent(out InteractableObject interactableObject))
                    {
                        interactableObject.SetGrab(true, true);
                    }
                }
                else
                {
                    throw new ArgumentNullException("Parameter must not be null");
                }
            }

            /// <summary>
            /// The currently grabbed object is considered to be ungrabbed.
            /// </summary>
            /// <exception cref="InvalidOperationException">thrown if not object is currently grabbed</exception>
            public void UnGrab()
            {
                if (!IsGrabbed || gameObject == null)
                {
                    throw new InvalidOperationException("No object is being grabbed.");
                }
                else
                {
                    IsGrabbed = false;
                    if (gameObject.TryGetComponent(out InteractableObject interactableObject))
                    {
                        interactableObject.SetGrab(false, true);
                    }
                    if (originalPositionOfGrabbedObject != currentPositionOfGrabbedObject)
                    {
                        // The grabbed object has actually been moved.
                        Finalize();
                    }
                    // Note: We do not set gameObject to null because we may need its
                    // value later for Undo/Redo.
                }
            }

            /// <summary>
            /// Memorizes the new parent of <see cref="gameObject"/> after it was moved.
            /// Can be the original parent. Relevant for <see cref="Redo"/>.
            /// </summary>
            private GameObject newParent;

            /// <summary>
            /// The original parent of <see cref="gameObject"/> before it was grabbed.
            /// </summary>
            private Transform originalParent;

            /// <summary>
            /// The original scale of <see cref="gameObject"/> before it was grabbed.
            /// </summary>
            private Vector3 originalScale;

            /// <summary>
            /// Called when the grabbed object has reached a new final destination.
            /// It will finalize its position.
            /// </summary>
            private void Finalize()
            {
                // -------------------------------------------------------------------
                // FIXME: When moving the grabbed node, we move its connecting edges
                // along with it. To do that they are morphed into splines. When the
                // movement has come to an end, the edges should be morphed back
                // again into to their original layout.
                // -------------------------------------------------------------------

                bool movementAllowed = GameNodeMover.FinalizePosition(gameObject, out GameObject parent);
                if (movementAllowed)
                {
                    if (parent != null)
                    {
                        // The node has been re-parented.
                        new ReparentNetAction(gameObject.name, parent.name, gameObject.transform.position).Execute();
                        newParent = parent;
                    }
                    currentPositionOfGrabbedObject = gameObject.transform.position;
                }
                else
                {
                    // An attempt was made to move the hovered object illegally.
                    // We need to reset it to its original position. And then we start from scratch.
                    // TODO: Instead of manually restoring the position like this, we can maybe use the memento
                    //       or ReversibleActions for resetting.
                    gameObject.transform.SetParent(originalParent);
                    nodeOperator.ScaleTo(originalScale, AnimationTime);
                    nodeOperator.MoveTo(originalPositionOfGrabbedObject, AnimationTime);

                    new MoveNodeNetAction(gameObject.name, originalPositionOfGrabbedObject).Execute();
                }
            }

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
                get => gameObject != null ? gameObject.name : String.Empty;
            }

            /// <summary>
            /// The position of the grabbed object in world space.
            /// </summary>
            /// <exception cref="InvalidOperationException">in case no object is currently grabbed</exception>
            internal Vector3 Position
            {
                get
                {
                    if (IsGrabbed && gameObject != null)
                    {
                        return gameObject.transform.position;
                    }
                    else
                    {
                        throw new InvalidOperationException("No object is being grabbed.");
                    }
                }
            }

            /// <summary>
            /// The original position of <see cref="GameObject"/> when it was grabbed.
            /// Required to return it to is original position when the action is canceled
            /// or undone.
            /// </summary>
            private Vector3 originalPositionOfGrabbedObject;

            /// <summary>
            /// The current position of <see cref="GameObject"/> in world space. More precisely, the
            /// position that was last set via <see cref="MoveTo(Vector3)"/>.
            /// </summary>
            private Vector3 currentPositionOfGrabbedObject;

            /// <summary>
            /// Returns the grabbed object to its original position when it was grabbed.
            /// This method will called for Undo.
            /// </summary>
            internal void MoveToOrigin()
            {
                /// Note: We cannot call <see cref="MoveTo(Vector3)"/> because that
                /// would also update <see cref="currentPositionOfGrabbedObject"/>.
                if (gameObject)
                {
                    nodeOperator.MoveTo(originalPositionOfGrabbedObject, AnimationTime);
                }
            }

            /// <summary>
            /// Returns the grabbed object to its last position before it was returned
            /// to its origin via <see cref="MoveToOrigin"/>.
            /// This method will called for Redo.
            /// </summary>
            internal void MoveToLastUserRequestedPosition()
            {
                if (gameObject)
                {
                    MoveTo(currentPositionOfGrabbedObject, AnimationTime);
                }
            }

            /// <summary>
            /// Moves the grabbed object to <paramref name="targetPosition"/> in world space
            /// immediately, that is, without any animation.
            /// </summary>
            /// <param name="targetPosition"></param>
            internal void MoveTo(Vector3 targetPosition)
            {
                if (gameObject)
                {
                    currentPositionOfGrabbedObject = targetPosition;
                    MoveTo(targetPosition, 0);
                }
            }

            /// <summary>
            /// Moves the grabbed object to <paramref name="targetPosition"/> in world space.
            /// </summary>
            /// <param name="targetPosition"></param>
            /// <param name="duration">the duration of the animation for moving the grabbed object in seconds</param>
            private void MoveTo(Vector3 targetPosition, float duration)
            {
                // FIXME: This code must be factored out into a helper class that can be called
                // from the corresponding network move action. It should be handled by GameNodeMover.
                nodeOperator.MoveTo(targetPosition, duration);
                MorphEdgesToSplines(duration);
                // TODO: Propagate to clients.
            }

            /// <summary>
            /// Morphs the incoming and outgoing edges of <see cref="gameObject"/> to simple splines.
            /// </summary>
            /// <param name="duration">the duration of the morphing animation in seconds</param>
            private void MorphEdgesToSplines(float duration)
            {
                // The minimal y offset for the point in between the start and end
                // of a spline through which the spline should pass.
                const float MinimalSplineOffset = 0.05f;

                // We will also "stick" the connected edges to the moved node during its movement.
                // In order to do this, we need to modify the splines of each one.
                // --------------------------------------------------------------------------------
                // FIXME: This is too simplistic. It does not handle the case of moving an inner
                // node whose descendants have connecting edges. The descendants will be moved
                // along with the inner node, but not their edges.
                // --------------------------------------------------------------------------------
                foreach ((SEESpline connectedSpline, bool nodeIsSource) hitEdge in ConnectedEdges)
                {
                    Edge edge = hitEdge.connectedSpline.gameObject.GetComponent<EdgeRef>().Value;
                    BSpline spline;
                    if (hitEdge.nodeIsSource)
                    {
                        spline = SplineEdgeLayout.CreateSpline(gameObject.transform.position,
                                                               edge.Target.RetrieveGameNode().transform.position,
                                                               true,
                                                               MinimalSplineOffset);
                    }
                    else
                    {
                        spline = SplineEdgeLayout.CreateSpline(edge.Source.RetrieveGameNode().transform.position,
                                                               gameObject.transform.position,
                                                               true,
                                                               MinimalSplineOffset);
                    }

                    if (hitEdge.connectedSpline.gameObject.TryGetComponentOrLog(out EdgeOperator edgeOperator))
                    {
                        edgeOperator.MorphTo(spline, duration);
                    }
                }
            }

            /// <summary>
            /// Restores the original state of the grabbed object just before it was grabbed.
            /// </summary>
            internal void Undo()
            {
                MoveToOrigin();
                // FIXME: We also neet to reset the parent in the graph.
                gameObject.transform.SetParent(originalParent);
                // FIXME: MoveToOrigin() and ReparentNetAction both move the node.
                new ReparentNetAction(gameObject.name, originalParent.name, originalPositionOfGrabbedObject).Execute();
            }

            /// <summary>
            /// Reverts <see cref="Undo"/>.
            /// </summary>
            internal void Redo()
            {
                MoveToLastUserRequestedPosition();
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
        /// Index of the left mouse button.
        /// </summary>
        private const int LeftMouseButton = 0;

        /// <summary>
        /// Returns the list of <see cref="SEESpline"/>s of the incoming and outgoing edges
        /// of <paramref name="gameNode"/>. The boolean in the returned pair indicates
        /// whether the edge is outgoing (if it is false, the edge is incoming).
        /// </summary>
        private static IList<(SEESpline, bool nodeIsSource)> GetConnectedEdges(GameObject gameNode)
        {
            IList<(SEESpline, bool nodeIsSource)> ConnectedEdges = new List<(SEESpline, bool)>();
            if (gameNode.TryGetNode(out Node node))
            {
                foreach (Edge edge in node.Incomings.Union(node.Outgoings).Where(x => !x.HasToggle(Edge.IsVirtualToggle)))
                {
                    GameObject gameEdge = GraphElementIDMap.Find(edge.ID);
                    Assert.IsNotNull(gameEdge);
                    if (gameEdge.TryGetComponentOrLog(out SEESpline spline))
                    {
                        ConnectedEdges.Add((spline, node == edge.Source));
                    }
                }
            }
            return ConnectedEdges;
        }

        /// <summary>
        /// <see cref="ReversibleAction.Update"/>.
        /// </summary>
        /// <returns>true if completed</returns>
        public override bool Update()
        {
            if (SEEInput.Cancel()) // cancel movement
            {
                Debug.Log("Dragging cancelled.\n");
                if (grabbedObject.IsGrabbed)
                {
                    Debug.Log($"Dragged object {grabbedObject.Name} returned to its location.\n");

                    // The grabbed object needs to be returned to its original location.
                    grabbedObject.MoveToOrigin();

                    // Reset action.
                    grabbedObject.UnGrab();
                    currentState = ReversibleAction.Progress.NoEffect;
                }

            }
            else if (Input.GetMouseButton(LeftMouseButton)) // start or continue moving a grabbed object
            {
                if (!grabbedObject.IsGrabbed)
                {
                    // User is starting dragging.
                    InteractableObject hoveredObject = InteractableObject.HoveredObjectWithWorldFlag;
                    // An object to be grabbed must be representing a node that is not the root.
                    if (hoveredObject && hoveredObject.gameObject.TryGetNode(out Node node) && !node.IsRoot())
                    {
                        grabbedObject.Grab(hoveredObject.gameObject);
                        distanceToUser = Vector3.Distance(Raycasting.UserPointsTo().origin, grabbedObject.Position);
                        currentState = ReversibleAction.Progress.InProgress;
                        Debug.Log($"Starting dragging of {grabbedObject.Name} at distance {distanceToUser}.\n");
                    }
                    else
                    {
                        Debug.Log("Nothing to be dragged.\n");
                    }
                }
                else
                {
                    // Assert: grabbedObject != null
                    Ray ray = Raycasting.UserPointsTo();
                    Vector3 targetPosition = ray.origin + distanceToUser * ray.direction;
                    // User is continuing dragging.
                    Debug.Log($"Continuing dragging {grabbedObject.Name} to {targetPosition}.\n");
                    grabbedObject.MoveTo(targetPosition);
                }
            }
            else if (grabbedObject.IsGrabbed) // dragging has ended
            {
                Debug.Log($"Dragging of {grabbedObject.Name} finalized.\n");
                // Finalize the action with the grabbed object.
                grabbedObject.UnGrab();
                // Action is finished.
                currentState = ReversibleAction.Progress.Completed;
                return true;
            }
            return false;
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