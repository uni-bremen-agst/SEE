using System;
using System.Collections.Generic;
using System.Linq;
using SEE.DataModel.DG;
using SEE.Game;
using SEE.Game.City;
using SEE.Game.Operator;
using SEE.GO;
using SEE.Layout.EdgeLayouts;
using SEE.Net.Actions;
using SEE.Tools.ReflexionAnalysis;
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
            /// TODO: Documentation.
            /// </summary>
            private SEEReflexionCity reflexionCity;

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
                    originalParent = gameObject.transform.parent;
                    originalLocalScale = gameObject.transform.localScale;
                    originalWorldPosition = gameObject.transform.position;
                    nodeOperator = gameObject.AddOrGetComponent<NodeOperator>();
                    KillActiveAnimations(nodeOperator);
                    ConnectedEdges = GetConnectedEdges(gameObject);
                    MorphEdgesToSplines(SplineAnimationDuration);
                    IsGrabbed = true;
                    if (gameObject.TryGetComponent(out InteractableObject interactableObject))
                    {
                        interactableObject.SetGrab(true, true);
                    }
                    // We need the reflexion city for later.
                    reflexionCity = gameObject.ContainingCity<SEEReflexionCity>();
                }
                else
                {
                    throw new ArgumentNullException("Parameter must not be null");
                }

                void KillActiveAnimations(NodeOperator nodeOperator)
                {
                    if (gameObject.TryGetNodeRef(out NodeRef node))
                    {
                        // We will also kill any active tweens (=> Reflexion Analysis), if necessary.
                        if (node.Value.IsInImplementation() || node.Value.IsInArchitecture())
                        {
                            // TODO: Instead of just killing animations here with this trick,
                            //       handle all movement inside the NodeOperator.
                            nodeOperator.MoveTo(nodeOperator.TargetPosition, 0);
                        }
                    }
                }
            }

            /// <summary>
            /// The currently grabbed object is considered to be ungrabbed and its movement
            /// is to be finalized.
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
                    if (gameObject.TryGetComponent(out InteractableObject interactableObject))
                    {
                        interactableObject.SetGrab(false, true);
                    }
                    if (originalWorldPosition != currentPositionOfGrabbedObject)
                    {
                        // The grabbed object has actually been moved.
                        //Finalize();
                    }
                    IsGrabbed = false;
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
            private Vector3 originalLocalScale;

            /// <summary>
            /// Called when the grabbed object has reached a new final destination.
            /// It will finalize its position.
            /// </summary>
            //private void Finalize()
            //{
            //    // -------------------------------------------------------------------
            //    // FIXME: When moving the grabbed node, we move its connecting edges
            //    // along with it. To do that they are morphed into splines. When the
            //    // movement has come to an end, the edges should be morphed back
            //    // again into to their original layout.
            //    // -------------------------------------------------------------------

            //    bool movementAllowed = GameNodeMover.FinalizePosition(gameObject, out GameObject parent);
            //    if (movementAllowed)
            //    {
            //        if (parent != null)
            //        {
            //            // The node has been re-parented.
            //            new ReparentNetAction(gameObject.name, parent.name, gameObject.transform.position).Execute();
            //            newParent = parent;
            //        }
            //        currentPositionOfGrabbedObject = gameObject.transform.position;
            //    }
            //    else
            //    {
            //        // An attempt was made to move the hovered object illegally.
            //        // We need to reset it to its original position. And then we start from scratch.
            //        // TODO: Instead of manually restoring the position like this, we can maybe use the memento
            //        //       or ReversibleActions for resetting.
            //        gameObject.transform.SetParent(originalParent);
            //        nodeOperator.ScaleTo(originalScale, AnimationTime);
            //        nodeOperator.MoveTo(originalPositionOfGrabbedObject, AnimationTime);

            //        new MoveNodeNetAction(gameObject.name, originalPositionOfGrabbedObject).Execute();
            //    }
            //}

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
            /// The node reference associated with the grabbed object. May be null if no
            /// node is associated with the grabbed object.
            /// </summary>
            public NodeRef Node
            {
                get => gameObject.TryGetNodeRef(out NodeRef result) ? result : null;
            }

            /// <summary>
            /// The original position of <see cref="GameObject"/> when it was grabbed.
            /// Required to return it to is original position when the action is canceled
            /// or undone.
            /// </summary>
            private Vector3 originalWorldPosition;

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
                if (gameObject)
                {
                    MoveTo(originalWorldPosition, AnimationTime);
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
                // MorphEdgesToSplines() must be moved into this helper class, too.
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
                new ReparentNetAction(gameObject.name, originalParent.name, originalWorldPosition).Execute();
            }

            /// <summary>
            /// Reverts <see cref="Undo"/>.
            /// </summary>
            internal void Redo()
            {
                MoveToLastUserRequestedPosition();
            }

            /// <summary>
            /// Temporary Maps-To edge which will have to be deleted if the node isn't finalized.
            /// </summary>
            private Edge temporaryMapsTo;

            internal void TemporaryMapTo(GameObject mappingTarget)
            {
                // The mapping is only possible if we are in a reflexion city and if the
                // mapping target is actually a node.
                if (reflexionCity != null && mappingTarget.TryGetNode(out Node target))
                {
                    // The source of the mapping
                    Node source = gameObject.GetNode();

                    if (source.ItsGraph != target.ItsGraph)
                    {
                        Debug.LogError("For a mapping, both nodes must be in the same graph.\n");
                        return;
                    }

                    // implementation -> architecture
                    if (source.IsInImplementation() && target.IsInArchitecture())
                    {
                        // If there is a previous mapping that already mapped the node
                        // on the current target, nothing needs to be done.
                        // If there is a previous mapping that mapped the node onto
                        // another target, the previous mapping must be reverted and the
                        // node must be mapped onto the new target.

                        if (temporaryMapsTo == null)
                        {
                            // If there is no previous mapping, we can just map the node.
                            MapTo(source, mappingTarget, target);
                        }
                        else // If there is a previous mapping.
                        {
                            Assert.IsTrue(reflexionCity.LoadedGraph.ContainsEdge(temporaryMapsTo));
                            // A temporary mapping exists already. This mapping can only be from an implementation
                            // node onto an architecture node.
                            Assert.IsTrue(temporaryMapsTo.Source == source);
                            // If the mapping hasn't changed, there is nothing to do.
                            if (temporaryMapsTo.Target != target)
                            {
                                // The grabbed object was previously temporarily mapped onto another target.
                                // The temporary mapping must be reverted.
                                reflexionCity.ReflexionGraph.RemoveFromMapping(temporaryMapsTo);
                                MapTo(source, mappingTarget, target);
                            }
                        }
                    }
                    // implementation -> implementation
                    else if (source.IsInImplementation() && target.IsInImplementation())
                    {
                        // This changes the node hierarchy in the implementation only.
                        PutOn(mappingTarget);
                        reflexionCity.ReflexionGraph.UnparentInImplementation(source);
                        reflexionCity.ReflexionGraph.AddChildInImplementation(source, target);
                    }
                    // architecture -> architecture
                    else if (source.IsInArchitecture() && target.IsInArchitecture())
                    {
                        // This changes the node hierarchy in the implementation only.
                        PutOn(mappingTarget);
                        reflexionCity.ReflexionGraph.UnparentInArchitecture(source);
                        reflexionCity.ReflexionGraph.AddChildInArchitecture(source, target);
                    }
                    // architecture -> implementation: forbidden
                    else
                    {
                        // nothing to be done
                    }
                }
            }

            /// <summary>
            /// Puts the <see cref="grabbedObject"/> onto <paramref name="mappingTarget"/> graphically.
            /// Adds a mapping from <paramref name="source"/> to <paramref name="target"/> to the
            /// reflexion analysis overriding any existing mapping.
            /// </summary>
            /// <param name="source">the source node of the maps-to edge</param>
            /// <param name="mappingTarget">the game node onto which to put <see cref="gameObject"/></param>
            /// <param name="target">the target node of the maps-to edge</param>
            private void MapTo(Node source, GameObject mappingTarget, Node target)
            {
                // If we are in a reflexion city, we will simply
                // trigger the incremental reflexion analysis here.
                // That way, the relevant code is in one place
                // and edges will be colored on hover (#451).
                PutOn(mappingTarget);
                temporaryMapsTo = reflexionCity.ReflexionGraph.AddToMapping(source, target, overrideMapping: true);
            }

            /// <summary>
            /// Puts <see cref="gameObject"/> onto <paramref name="mappingTarget"/> graphically.
            /// No change to the underlying graph is made.
            /// </summary>
            /// <param name="mappingTarget">the game node onto which to put <see cref="gameObject"/></param>
            private void PutOn(GameObject mappingTarget)
            {
                bool scaleDown = mappingTarget != originalParent.gameObject;
                if (!scaleDown)
                {
                    // The gameObject may have already been scaled down, hence,
                    // we need to restore its original scale.
                    nodeOperator.ScaleTo(originalLocalScale, 0);
                }
                GameNodeMover.PutOn(gameObject.transform, mappingTarget, scaleDown: scaleDown);
            }

            internal void TemporaryUnMap()
            {
                if (reflexionCity != null && temporaryMapsTo != null && reflexionCity.LoadedGraph.ContainsEdge(temporaryMapsTo))
                {
                    // The Maps-To edge will have to be deleted once the node no longer hovers over it.
                    // We'll change its parent so it becomes a root node in the implementation city.
                    // The user will have to drop it on another node to re-parent it.
                    gameObject.transform.SetParent(reflexionCity.ImplementationRoot.RetrieveGameNode().transform);
                    reflexionCity.ReflexionGraph.RemoveFromMapping(temporaryMapsTo);
                    temporaryMapsTo = null;
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

        #region HitColor

        /// <summary>
        /// Original color of the object the user hovered over.
        /// </summary>
        private Color hitObjectColor;

        /// <summary>
        /// Material of the object the user hovered over.
        /// </summary>
        private Material hitObjectMaterial;

        /// <summary>
        /// Inverts the color of the hit object (and saves its original one to be able to revert
        /// this color change later).
        /// </summary>
        /// <param name="hitObject">the object that was hit</param>
        void SetHitObjectColor(Transform hitObject)
        {
            hitObjectMaterial = hitObject.GetComponent<Renderer>().material;
            // We persist hoveredObjectColor in case we want to use something different than simple
            // inversion in the future, such as a constant color (we would then need the original color).
            hitObjectColor = hitObjectMaterial.color;
            hitObjectMaterial.color = hitObjectColor.Invert();
        }

        /// <summary>
        /// Restores the color of <see cref="hitObjectMaterial"/> to <see cref="hitObjectColor"/>.
        /// Attribute <see cref="hitObjectMaterial"/> will be <c>null</c> afterwards.
        /// </summary>
        void ResetHitObjectColor()
        {
            if (hitObjectMaterial != null)
            {
                hitObjectMaterial.color = hitObjectColor;
            }
            hitObjectMaterial = null;
        }

        #endregion HitColor

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
                if (grabbedObject.IsGrabbed)
                {
                    Debug.Log("Dragging cancelled.\n");
                    Debug.Log($"Dragged object {grabbedObject.Name} will be returned to its original location.\n");

                    // Reset action.
                    grabbedObject.Undo();
                    currentState = ReversibleAction.Progress.NoEffect;
                }
                ResetHitObjectColor();
            }
            else if (Input.GetMouseButton(LeftMouseButton)) // start to grab the object or continue to move the grabbed object
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
                        Debug.Log($"Starting dragging of {grabbedObject.Name} at distance {distanceToUser}.\n");
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
                    Vector3 targetPosition = ray.origin + distanceToUser * ray.direction;
                    Debug.Log($"Continuing dragging {grabbedObject.Name} to {targetPosition}.\n");
                    grabbedObject.MoveTo(targetPosition);
                }

                // The grabbed node is not yet at its final destination. The user is still moving
                // it. We will run a what-if reflexion analysis to give immediate feedback on the
                // consequences if the user were putting the grabbed node onto the node the user
                // is currently aiming at.
                if (grabbedObject.IsGrabbed)
                {
                    ResetHitObjectColor();
                    TemporaryUpdateReflexion();
                }
            }
            else if (grabbedObject.IsGrabbed) // dragging has ended
            {
                Debug.Log($"Dragging of {grabbedObject.Name} finalized.\n");
                // Finalize the action with the grabbed object.
                grabbedObject.UnGrab();
                // Action is finished.
                ResetHitObjectColor();
                currentState = ReversibleAction.Progress.Completed;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Runs a what-if reflexion analysis to give immediate feedback on the
        /// consequences if the user were putting the grabbed node onto the node the user
        /// is currently aiming at. (if any). The what-if analysis will update the reflexion
        /// analysis and show its results. The mapping, however, is considered only
        /// temporary, that is, the modifications and visualizations will be finalized
        /// only if the user actually drops the grabbed object.
        /// </summary>
        /// <remarks>Precondition: a node is grabbed.</remarks>
        private void TemporaryUpdateReflexion()
        {
            Assert.IsTrue(grabbedObject.IsGrabbed);
            if (Raycasting.RaycastLowestNode(out RaycastHit? raycastHit, out Node _, grabbedObject.Node))
            {
                // Note: the root node can never be grabbed. See above.
                if (raycastHit.HasValue)
                {
                    // The user is currently aiming at a node. The grabbed node is mapped onto
                    // this aimed node.
                    grabbedObject.TemporaryMapTo(raycastHit.Value.transform.gameObject);
                    SetHitObjectColor(raycastHit.Value.transform);
                }
                else
                {
                    // The user is currently not aiming at a node. The temporary mapping
                    // of the grabbed must be reverted.
                    grabbedObject.TemporaryUnMap();
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