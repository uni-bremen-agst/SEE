using System.Collections.Generic;
using System.Linq;
using SEE.DataModel;
using SEE.DataModel.DG;
using SEE.Game;
using SEE.Game.City;
using SEE.Game.Operator;
using SEE.Game.UI3D;
using SEE.GO;
using SEE.Layout.EdgeLayouts;
using SEE.Net.Actions;
using SEE.Tools.ReflexionAnalysis;
using SEE.Utils;
using TinySpline;
using UnityEngine;
using UnityEngine.Assertions;
using static SEE.Utils.Raycasting;
using Node = SEE.DataModel.DG.Node;
using Plane = UnityEngine.Plane;

namespace SEE.Controls.Actions
{
    /// <summary>
    /// An action to move nodes.
    /// </summary>
    internal class MoveAction : AbstractPlayerAction
    {
        private struct Hit
        {
            internal Hit(Transform hoveredObject)
            {
                CityRootNode = SceneQueries.GetCityRootTransformUpwards(hoveredObject);
                HoveredObject = hoveredObject;
                InteractableObject = hoveredObject.GetComponent<InteractableObject>();
                Plane = new Plane(Vector3.up, CityRootNode.position);
                node = hoveredObject.GetComponent<NodeRef>();
                ConnectedEdges = new List<(SEESpline, bool)>();

                // We want to animate the edges attached to the moving node, so we cache them here.
                // TODO: Why can this be null?
                if (node.Value != null)
                {
                    foreach (Edge edge in node.Value.Incomings.Union(node.Value.Outgoings).Where(x => !x.HasToggle(Edge.IsVirtualToggle)))
                    {
                        GameObject gameEdge = GraphElementIDMap.Find(edge.ID);
                        Assert.IsNotNull(gameEdge);
                        if (gameEdge.TryGetComponentOrLog(out SEESpline spline))
                        {
                            ConnectedEdges.Add((spline, node.Value == edge.Source));
                        }
                    }
                }
            }

            /// <summary>
            /// The root of the code city. This is the top-most game object representing a node,
            /// i.e., is tagged by <see cref="Tags.Node"/>.
            /// </summary>
            internal readonly Transform CityRootNode;

            /// <summary>
            /// The game object currently being hovered over. It is a descendant of <see cref="CityRootNode"/>
            /// or <see cref="CityRootNode"/> itself.
            /// </summary>
            internal readonly Transform HoveredObject;

            /// <summary>
            /// The interactable component attached to <see cref="HoveredObject"/>.
            /// </summary>
            internal readonly InteractableObject InteractableObject;

            /// <summary>
            /// Map from connected edges represented as <see cref="SEESpline"/>s to a boolean
            /// representing whether <see cref="node"/> is the source for this edge (otherwise, it's the target).
            /// </summary>
            internal readonly IList<(SEESpline, bool nodeIsSource)> ConnectedEdges;

            internal readonly Plane Plane;
            internal readonly NodeRef node;
        }

        /// <summary>
        /// The number of degrees in a full circle.
        /// </summary>
        private const float FullCircleDegree = 360.0f;

        private const float SnapStepCount = 8;
        private const float SnapStepAngle = FullCircleDegree / SnapStepCount;
        private const float MIN_SPLINE_OFFSET = 0.05f;
        private const float SPLINE_ANIMATION_DURATION = 2f;
        private const bool FOLLOW_RAYCAST_HIT = false;

        private static readonly MoveGizmo gizmo = MoveGizmo.Create();

        /// <summary>
        /// Whether moving a node has been initiated.
        /// </summary>
        private bool moving = false;

        private Hit hit;
        private Vector3 dragStartTransformPosition = Vector3.positiveInfinity;
        private Vector3 dragStartOffset = Vector3.positiveInfinity;
        private Vector3 dragCanonicalOffset = Vector3.positiveInfinity;

        private Vector3 originalScale = Vector3.one;

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
        public override ReversibleAction NewInstance() => new MoveAction
        {
            // We will be re-using the current settings so that the new action
            // can continue.
            moving = moving,
            hit = hit,
            dragStartTransformPosition = dragStartTransformPosition,
            dragStartOffset = dragStartOffset,
            dragCanonicalOffset = dragCanonicalOffset
        };

        /// <summary>
        /// Returns the set of IDs of all game objects changed by this action.
        /// <see cref="ReversibleAction.GetChangedObjects"/>
        /// </summary>
        /// <returns>empty set because this action does not change anything</returns>
        public override HashSet<string> GetChangedObjects()
        {
            if (memento != null && memento.GameObject != null)
            {
                return new HashSet<string> { memento.GameObject.name };
            }
            else
            {
                return new HashSet<string>();
            }
        }

        /// Returns the <see cref="ActionStateType"/> of this action.
        /// </summary>
        /// <returns><see cref="ActionStateType.Move"/></returns>
        public override ActionStateType GetActionStateType()
        {
            return ActionStateType.Move;
        }

        /// <summary>
        /// A memento for memorizing game nodes that were moved by this action.
        /// Used for Undo/Redo.
        /// </summary>
        private class Memento
        {
            /// <summary>
            /// The transform of the game object that was moved.
            /// </summary>
            internal Transform GameObject;

            /// <summary>
            /// The parent of <see cref="GameObject"/> at the time before it was moved.
            /// This will be used to restore the original parent upon <see cref="Undo"/>.
            /// </summary>
            private readonly Transform oldParent;

            /// <summary>
            /// The position of <see cref="GameObject"/> in world space at the time before it was moved.
            /// This will be used to restore the original world-space position upon <see cref="Undo"/>.
            /// </summary>
            private Vector3 oldPosition;

            /// <summary>
            /// The new parent of <see cref="GameObject"/> at the time after it was moved.
            /// Maybe the same value as <see cref="oldParent"/>.
            /// This will be used to restore the new parent upon <see cref="Redo"/>.
            /// </summary>
            private GameObject newParent;

            /// <summary>
            /// The new position of <see cref="GameObject"/> in world space at the time after it was moved.
            /// This will be used to restore the new position upon <see cref="Redo"/>.
            /// </summary>
            private Vector3 newPosition;

            /// <summary>
            /// Constructor.
            /// </summary>
            /// <param name="movedGameNode">the transform of the game node that was moved</param>
            internal Memento(Transform movedGameNode)
            {
                this.GameObject = movedGameNode;
                this.oldParent = movedGameNode.transform.parent;
                this.oldPosition = movedGameNode.position;
            }

            /// <summary>
            /// Restores the original state of <see cref="GameObject"/> before it was moved
            /// regarding its original parent and position. Will also propagate that state
            /// through the network to all clients.
            /// </summary>
            internal void Undo()
            {
                GameObject.position = oldPosition;
                GameObject.SetParent(oldParent);
                new ReparentNetAction(GameObject.name, oldParent.name, oldPosition).Execute();
            }

            /// <summary>
            /// Restores the state of <see cref="GameObject"/> after it was moved regarding its
            /// new parent and position. Will also propagate that state through the network to
            /// all clients.
            ///
            /// Precondition: <see cref="Undo"/> has been called before.
            /// </summary>
            internal void Redo()
            {
                GameObject.position = newPosition;
                GameObject.SetParent(newParent.transform);
                new ReparentNetAction(GameObject.name, newParent.name, GameObject.position).Execute();
            }

            /// <summary>
            /// Memorizes the new position of <see cref="GameObject"/> after it was moved.
            /// Relevant for <see cref="Redo"/>.
            /// </summary>
            /// <param name="position">new position</param>
            internal void SetNewPosition(Vector3 position)
            {
                newPosition = position;
            }

            /// <summary>
            /// Memorizes the new parent of <see cref="GameObject"/> after it was moved.
            /// Can be the original parent. Relevant for <see cref="Redo"/>.
            /// </summary>
            /// <param name="parent">new parent</param>
            internal void SetNewParent(GameObject parent)
            {
                newParent = parent;
            }
        }

        /// <summary>
        /// The memento memorizing the original state of the hovered object that was moved.
        /// Will be null until a node was actually moved.
        /// </summary>
        private Memento memento;

        /// <summary>
        /// Original color of the object the user hovered over.
        /// </summary>
        private Color hitObjectColor;

        /// <summary>
        /// Material of the object the user hovered over.
        /// </summary>
        private Material hitObjectMaterial;

        /// <summary>
        /// Reflexion city belonging to the dragged node, if it does belong to one.
        /// </summary>
        private SEEReflexionCity reflexionCity;

        /// <summary>
        /// The Operator of this node.
        /// </summary>
        private NodeOperator nodeOperator;

        /// <summary>
        /// Temporary Maps-To edge which will have to be deleted if the node isn't finalized.
        /// </summary>
        private Edge temporaryMapsTo;

        /// <summary>
        /// Original parent of the dragged node before temporarily changing it.
        /// </summary>
        private Transform originalParent;

        /// <summary>
        /// <see cref="ReversibleAction.Update"/>.
        /// </summary>
        /// <returns>true if completed</returns>
        public override bool Update()
        {
            bool result = false;
            InteractableObject hoveredObject = InteractableObject.HoveredObjectWithWorldFlag;
            // The root game node of the code city (tagged by Tags.Node).
            Transform cityRootNode = null;

            if (hoveredObject)
            {
                cityRootNode = SceneQueries.GetCityRootTransformUpwards(hoveredObject.transform);
                Assert.IsNotNull(cityRootNode);
            }

            bool synchronize = false;

            if (SEEInput.Cancel()) // cancel movement
            {
                if (moving)
                {
                    Vector3 originalPosition = dragStartTransformPosition + dragStartOffset - Vector3.Scale(dragCanonicalOffset, hit.HoveredObject.localScale);
                    nodeOperator.MoveTo(originalPosition, 0);
                    hit.InteractableObject.SetGrab(false, true);
                    gizmo.gameObject.SetActive(false);

                    moving = false;
                    hit = new Hit();
                    synchronize = true;
                }
                else if (hoveredObject)
                {
                    // TODO(torben): this should be in SelectAction.cs
                    InteractableObject.UnselectAllInGraph(hoveredObject.ItsGraph, true);
                }

                if (reflexionCity != null && temporaryMapsTo != null && reflexionCity.LoadedGraph.ContainsEdge(temporaryMapsTo))
                {
                    // The Maps-To edge will have to be deleted once the node no longer hovers over it.
                    hit.HoveredObject.SetParent(originalParent);
                    reflexionCity.Analysis.DeleteFromMapping(temporaryMapsTo);
                    temporaryMapsTo = null;
                }

                ResetHitObjectColor();
            }
            else if (SEEInput.Drag() && !SEEInput.DragHovered()) // start or continue moving a grabbed object
            {
                if (!moving && hoveredObject
                            && hoveredObject.transform != cityRootNode
                            && RaycastPlane(new Plane(Vector3.up, cityRootNode.position), out Vector3 planeHitPoint))
                {
                    // start movement of the grabbed object
                    moving = true;
                    // If SEEInput.Drag() is combined with SEEInput.DragHovered(), the hoveredObject is to
                    // be dragged; otherwise the whole city (city root node). Note: the hoveredObject may in
                    // fact be cityRootNode.
                    // FIXME Transform draggedObject = SEEInput.DragHovered() ? hoveredObject.transform : cityRootNode;
                    Transform draggedObject = hoveredObject.transform;
                    hit = new Hit(draggedObject);
                    memento = new Memento(draggedObject);

                    foreach ((SEESpline connectedSpline, bool nodeIsSource) hitEdge in hit.ConnectedEdges)
                    {
                        Edge edge = hitEdge.connectedSpline.gameObject.GetComponent<EdgeRef>().Value;
                        BSpline spline;
                        if (hitEdge.nodeIsSource)
                        {
                            spline = SplineEdgeLayout.CreateSpline(hit.HoveredObject.transform.position, edge.Target.RetrieveGameNode().transform.position, true, MIN_SPLINE_OFFSET);
                        }
                        else
                        {
                            spline = SplineEdgeLayout.CreateSpline(edge.Source.RetrieveGameNode().transform.position, hit.HoveredObject.transform.position, true, MIN_SPLINE_OFFSET);
                        }

                        EdgeOperator edgeOperator = hitEdge.connectedSpline.gameObject.AddOrGetComponent<EdgeOperator>();

                        edgeOperator.MorphTo(spline, SPLINE_ANIMATION_DURATION);
                    }

                    hit.InteractableObject.SetGrab(true, true);
                    gizmo.gameObject.SetActive(true);
                    dragStartTransformPosition = hit.HoveredObject.position;
                    dragStartOffset = planeHitPoint - hit.HoveredObject.position;
                    dragCanonicalOffset = dragStartOffset.DividePairwise(hit.HoveredObject.localScale);
                    originalParent = hit.HoveredObject;
                    originalScale = hit.HoveredObject.localScale;

                    nodeOperator = hit.HoveredObject.gameObject.AddOrGetComponent<NodeOperator>();

                    // We will also kill any active tweens (=> Reflexion Analysis), if necessary.
                    if (hit.node.Value.IsInImplementation() || hit.node.Value.IsInArchitecture())
                    {
                        // We need the reflexion city for later.
                        reflexionCity = hit.HoveredObject.gameObject.ContainingCity<SEEReflexionCity>();

                        // TODO: Instead of just killing animations here with this trick,
                        //       handle all movement inside the NodeOperator.
                        nodeOperator.MoveTo(nodeOperator.TargetPosition, 0);
                    }
                }

                if (moving
                    && RaycastPlane(hit.Plane, out planeHitPoint)) // continue movement
                {
                    // FIXME: Doesn't work in certain perspectives, particularly when looking at the horizon.
                    Vector3 totalDragOffsetFromStart = Vector3.Scale(planeHitPoint - (dragStartTransformPosition + dragStartOffset), hit.HoveredObject.localScale);
                    if (SEEInput.Snap())
                    {
                        Vector2 point2 = new Vector2(totalDragOffsetFromStart.x, totalDragOffsetFromStart.z);
                        float angleDeg = point2.Angle360();
                        float snappedAngleDeg = Mathf.Round(angleDeg / SnapStepAngle) * SnapStepAngle;
                        float snappedAngleRad = Mathf.Deg2Rad * snappedAngleDeg;
                        Vector2 dir = new Vector2(Mathf.Cos(snappedAngleRad), Mathf.Sin(-snappedAngleRad));
                        Vector2 proj = dir * Vector2.Dot(point2, dir);
                        totalDragOffsetFromStart = new Vector3(proj.x, totalDragOffsetFromStart.y, proj.y);
                    }

                    if (hit.HoveredObject == cityRootNode)
                    {
                        // The root node must remain in its plane and must not be re-parented.
                        Vector3 newPosition = dragStartTransformPosition + totalDragOffsetFromStart;
                        ResetHitObjectColor();
                        nodeOperator.MoveXTo(newPosition.x, 0);
                        nodeOperator.MoveZTo(newPosition.z, 0);

                        gizmo.SetPositions(dragStartTransformPosition + dragStartOffset, hit.HoveredObject.position);
                    }
                    else
                    {
                        RaycastLowestNode(out RaycastHit? raycastHit, out Node newParentNode, hit.node);
                        // TODO: Adjust for snapping
                        // FIXME: Position is not exact depending on scale. Needs to be reworked.
                        Vector3 newPosition = FOLLOW_RAYCAST_HIT && raycastHit?.point != null ? raycastHit.Value.point : dragStartTransformPosition + totalDragOffsetFromStart;
                        ResetHitObjectColor();
                        nodeOperator.MoveXTo(newPosition.x, 0);
                        nodeOperator.MoveZTo(newPosition.z, 0);

                        gizmo.SetPositions(dragStartTransformPosition + dragStartOffset, hit.HoveredObject.position);

                        if (raycastHit.HasValue)
                        {
                            // FIXME: Under certain circumstances, "flickering" can occur. Relates to above FIXME comment.
                            GameNodeMover.PutOn(hit.HoveredObject, raycastHit.Value.collider.gameObject, setParent: true);
                            SetHitObjectColor(raycastHit.Value);
                            if (reflexionCity != null)
                            {
                                if (hit.node.Value.IsInImplementation() && newParentNode.IsInArchitecture())
                                {
                                    // If we are in a reflexion city, we will simply
                                    // trigger the incremental reflexion analysis here.
                                    // That way, the relevant code is in one place
                                    // and edges will be colored on hover (#451).
                                    temporaryMapsTo = reflexionCity.Analysis.AddToMapping(hit.node.Value, newParentNode, overrideMapping: true);
                                }
                                else if (temporaryMapsTo != null && reflexionCity.LoadedGraph.ContainsEdge(temporaryMapsTo))
                                {
                                    // The Maps-To edge will have to be deleted once the node no longer hovers over it.
                                    hit.HoveredObject.SetParent(raycastHit.Value.collider.transform);
                                    reflexionCity.Analysis.DeleteFromMapping(temporaryMapsTo);
                                    temporaryMapsTo = null;
                                }
                                else if (hit.node.Value.IsInImplementation() && newParentNode.IsInImplementation())
                                {
                                    // Both are in implementation, so we'll just need to adjust the scaling.
                                    // No need to delete any Maps-To edge, because temporaryMapsTo == null or is deleted.
                                    GameNodeMover.PutOn(hit.HoveredObject, raycastHit.Value.transform.gameObject, scaleDown: true);
                                }
                            }
                        }
                        else if (reflexionCity != null && temporaryMapsTo != null && reflexionCity.LoadedGraph.ContainsEdge(temporaryMapsTo))
                        {
                            // The Maps-To edge will have to be deleted once the node no longer hovers over it.
                            // We'll change its parent so it becomes a root node in the implementation city.
                            // The user will have to drop it on another node to re-parent it.
                            hit.HoveredObject.SetParent(reflexionCity.ImplementationRoot.RetrieveGameNode().transform);
                            reflexionCity.Analysis.DeleteFromMapping(temporaryMapsTo);
                            temporaryMapsTo = null;
                        }
                    }

                    // We will also "stick" the connected edges to the moved node during its movement.
                    // In order to do this, we need to modify the splines of each one.
                    foreach ((SEESpline connectedSpline, bool nodeIsSource) hitEdge in hit.ConnectedEdges)
                    {
                        Edge edge = hitEdge.connectedSpline.gameObject.GetComponent<EdgeRef>().Value;
                        BSpline spline;
                        if (hitEdge.nodeIsSource)
                        {
                            spline = SplineEdgeLayout.CreateSpline(hit.HoveredObject.transform.position, edge.Target.RetrieveGameNode().transform.position, true, MIN_SPLINE_OFFSET);
                        }
                        else
                        {
                            spline = SplineEdgeLayout.CreateSpline(edge.Source.RetrieveGameNode().transform.position, hit.HoveredObject.transform.position, true, MIN_SPLINE_OFFSET);
                        }

                        if (hitEdge.connectedSpline.gameObject.TryGetComponentOrLog(out EdgeOperator edgeOperator))
                        {
                            // Edges should stick to the node and not lag behind, so the duration is set to zero.
                            edgeOperator.MorphTo(spline, 0);
                        }
                    }

                    synchronize = true;
                }
            }
            else if (SEEInput.Reset()) // reset to center of table
            {
                if (cityRootNode && !moving)
                {
                    GO.Plane plane = cityRootNode.GetComponentInParent<GO.Plane>();
                    cityRootNode.position = plane.CenterTop;
                    new MoveNodeNetAction(cityRootNode.name, cityRootNode.position).Execute();
                    gizmo.gameObject.SetActive(false);

                    synchronize = false; // We just called MoveNodeNetAction for the synchronization.
                }

                ResetHitObjectColor();
            }
            else if (moving)
            {
                InteractableObject interactableObjectToBeUngrabbed = hit.InteractableObject;
                // No canceling, no dragging, no reset, but still moving =>  finalize movement
                if (hit.HoveredObject != hit.CityRootNode) // only reparent non-root nodes
                {
                    Vector3 originalPosition = dragStartTransformPosition + dragStartOffset
                                               - Vector3.Scale(dragCanonicalOffset, hit.HoveredObject.localScale);
                    bool movementAllowed = GameNodeMover.FinalizePosition(hit.HoveredObject.gameObject, out GameObject parent);
                    if (movementAllowed)
                    {
                        if (parent != null)
                        {
                            // The node has been re-parented.
                            new ReparentNetAction(hit.HoveredObject.gameObject.name, parent.name, hit.HoveredObject.position).Execute();
                            memento.SetNewParent(parent);
                        }

                        memento.SetNewPosition(hit.HoveredObject.position);
                        currentState = ReversibleAction.Progress.Completed;
                        result = true;
                    }
                    else
                    {
                        // An attempt was made to move the hovered object illegally.
                        // We need to reset it to its original position. And then we start from scratch.
                        // TODO: Instead of manually restoring the position like this, we can maybe use the memento
                        //       or ReversibleActions for resetting.
                        hit.HoveredObject.SetParent(originalParent);
                        nodeOperator.ScaleTo(originalScale, 1f);
                        nodeOperator.MoveTo(originalPosition, 1f);

                        new MoveNodeNetAction(hit.HoveredObject.name, originalPosition).Execute();
                        // The following assignment will override hit.interactableObject; that is why we
                        // stored its value in interactableObjectToBeUngrabbed above.
                        hit = new Hit();
                    }

                    synchronize = false; // false because we just called the necessary network action ReparentNetAction() or MoveNodeNetAction, respectively.
                }

                interactableObjectToBeUngrabbed.SetGrab(false, true);
                gizmo.gameObject.SetActive(false);
                ResetHitObjectColor();
                moving = false;
            }

            if (synchronize)
            {
                new MoveNodeNetAction(hit.HoveredObject.name, hit.HoveredObject.position).Execute();
            }

            if (currentState != ReversibleAction.Progress.Completed)
            {
                currentState = moving ? ReversibleAction.Progress.InProgress : ReversibleAction.Progress.NoEffect;
            }

            return result;

            #region Local Functions

            void SetHitObjectColor(RaycastHit raycastHit)
            {
                hitObjectMaterial = raycastHit.collider.GetComponent<Renderer>().material;
                // We persist hoveredObjectColor in case we want to use something different than simple
                // inversion in the future, such as a constant color (we would then need the original color).
                hitObjectColor = hitObjectMaterial.color;
                hitObjectMaterial.color = hitObjectColor.Invert();
            }

            void ResetHitObjectColor()
            {
                if (hitObjectMaterial != null)
                {
                    hitObjectMaterial.color = hitObjectColor;
                }

                hitObjectMaterial = null;
            }

            #endregion
        }

        /// <summary>
        /// <see cref="ReversibleAction.Undo"/>.
        /// </summary>
        public override void Undo()
        {
            base.Undo();
            memento?.Undo();
        }

        /// <summary>
        /// <see cref="ReversibleAction.Redo"/>.
        /// </summary>
        public override void Redo()
        {
            base.Redo();
            memento?.Redo();
        }
    }
}