using System.Collections.Generic;
using SEE.Game;
using SEE.Game.UI3D;
using SEE.Net;
using SEE.Utils;
using UnityEngine;
using UnityEngine.Assertions;

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
                cityRootNode = SceneQueries.GetCityRootTransformUpwards(hoveredObject);
                this.hoveredObject = hoveredObject;
                interactableObject = hoveredObject.GetComponent<InteractableObject>();
                plane = new Plane(Vector3.up, cityRootNode.position);
            }
            /// <summary>
            /// The root of the code city. This the top-most game object representing a node,
            /// i.e., is tagged by <see cref="Tags.Node"/>.
            /// </summary>
            internal Transform cityRootNode;
            /// <summary>
            /// The game object currently being hovered over. It is a descendant of <see cref="cityRootNode"/>
            /// or <see cref="cityRootNode"/> itself.
            /// </summary>
            internal Transform hoveredObject;
            /// <summary>
            /// The interactable component attached to <see cref="hoveredObject"/>.
            /// </summary>
            internal InteractableObject interactableObject;
            internal Plane plane;
        }

        private const float SnapStepCount = 8;
        private const float SnapStepAngle = 360.0f / SnapStepCount;

        private static readonly MoveGizmo gizmo = MoveGizmo.Create();

        /// <summary>
        /// Whether moving a node has been initiated.
        /// </summary>
        private bool moving = false;
        private Hit hit = new Hit();
        private Vector3 dragStartTransformPosition = Vector3.positiveInfinity;
        private Vector3 dragStartOffset = Vector3.positiveInfinity;
        private Vector3 dragCanonicalOffset = Vector3.positiveInfinity;

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
            if (memento != null && memento.gameObject != null)
            {
                return new HashSet<string> { memento.gameObject.name };
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
            public Transform gameObject;
            /// <summary>
            /// The parent of <see cref="gameObject"/> at the time before it was moved.
            /// This will be used to restore the original parent upon <see cref="Undo"/>.
            /// </summary>
            private Transform oldParent;
            /// <summary>
            /// The position of <see cref="gameObject"/> in world space at the time before it was moved.
            /// This will be used to restore the original world-space position upon <see cref="Undo"/>.
            /// </summary>
            private Vector3 oldPosition;
            /// <summary>
            /// The new parent of <see cref="gameObject"/> at the time after it was moved.
            /// Maybe the same value as <see cref="oldParent"/>.
            /// This will be used to restore the new parent upon <see cref="Redo"/>.
            /// </summary>
            private GameObject newParent;
            /// <summary>
            /// The new position of <see cref="gameObject"/> in world space at the time after it was moved.
            /// This will be used to restore the new position upon <see cref="Redo"/>.
            /// </summary>
            private Vector3 newPosition;

            /// <summary>
            /// Constructor.
            /// </summary>
            /// <param name="movedGameNode">the transform of the game node that was moved</param>
            internal Memento(Transform movedGameNode)
            {
                this.gameObject = movedGameNode;
                this.oldParent = movedGameNode.transform.parent;
                this.oldPosition = movedGameNode.position;
            }

            /// <summary>
            /// Restores the original state of <see cref="gameObject"/> before it was moved
            /// regarding its original parent and position. Will also propagate that state
            /// through the network to all clients.
            /// </summary>
            internal void Undo()
            {
                gameObject.position = oldPosition;
                gameObject.SetParent(oldParent);
                new ReparentNetAction(gameObject.name, oldParent.name, oldPosition).Execute();
            }

            /// <summary>
            /// Restores the state of <see cref="gameObject"/> after it was moved regarding its
            /// new parent and position. Will also propagate that state through the network to
            /// all clients.
            ///
            /// Precondition: <see cref="Undo"/> has been called before.
            /// </summary>
            internal void Redo()
            {
                gameObject.position = newPosition;
                gameObject.SetParent(newParent.transform);
                new ReparentNetAction(gameObject.name, newParent.name, gameObject.position).Execute();
            }

            /// <summary>
            /// Memorizes the new position of <see cref="gameObject"/> after it was moved.
            /// Relevant for <see cref="Redo"/>.
            /// </summary>
            /// <param name="position">new position</param>
            internal void SetNewPosition(Vector3 position)
            {
                newPosition = position;
            }

            /// <summary>
            /// Memorizes the new parent of <see cref="gameObject"/> after it was moved.
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
        /// <see cref="ReversibleAction.Update"/>.
        /// </summary>
        /// <returns>true if completed</returns>
        public override bool Update()
        {
            bool result = false;
            InteractableObject hoveredObject = InteractableObject.HoveredObjectWithWorldFlag;
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
                    Vector3 originalPosition = dragStartTransformPosition + dragStartOffset - Vector3.Scale(dragCanonicalOffset, hit.hoveredObject.localScale);
                    Positioner.Set(hit.hoveredObject, originalPosition);
                    hit.interactableObject.SetGrab(false, true);
                    gizmo.gameObject.SetActive(false);

                    moving = false;
                    hit = new Hit();
                    synchronize = true;
                }
                else if (hoveredObject)
                {
                    InteractableObject.UnselectAllInGraph(hoveredObject.ItsGraph, true); // TODO(torben): this should be in SelectAction.cs
                }
            }
            else if (SEEInput.Drag()) // start or continue movement
            {
                if (SEEInput.StartDrag() && hoveredObject
                    && Raycasting.RaycastPlane(new Plane(Vector3.up, cityRootNode.position), out Vector3 planeHitPoint)) // start movement
                {
                    moving = true;
                    // If SEEInput.StartDrag() is combined with SEEInput.DragHovered(), the hoveredObject is to
                    // be dragged; otherwise the whole city (city root node). Note: the hoveredObject may in
                    // fact be cityRootNode.
                    Transform draggedObject = SEEInput.DragHovered() ? hoveredObject.transform : cityRootNode;
                    hit = new Hit(draggedObject);
                    memento = new Memento(draggedObject);

                    hit.interactableObject.SetGrab(true, true);
                    gizmo.gameObject.SetActive(true);
                    dragStartTransformPosition = hit.hoveredObject.position;
                    dragStartOffset = planeHitPoint - hit.hoveredObject.position;
                    dragCanonicalOffset = dragStartOffset.DividePairwise(hit.hoveredObject.localScale);
                }

                if (moving && Raycasting.RaycastPlane(hit.plane, out planeHitPoint)) // continue movement
                {
                    Vector3 totalDragOffsetFromStart = planeHitPoint - (dragStartTransformPosition + dragStartOffset);
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
                    Positioner.Set(hit.hoveredObject, dragStartTransformPosition + totalDragOffsetFromStart);
                    Vector3 startPoint = dragStartTransformPosition + dragStartOffset;
                    Vector3 endPoint = hit.hoveredObject.position + Vector3.Scale(dragCanonicalOffset, hit.hoveredObject.localScale);
                    gizmo.SetPositions(startPoint, endPoint);

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
            }
            else if (moving)
            {
                InteractableObject interactableObjectToBeUngrabbed = hit.interactableObject;
                // No canceling, no dragging, no reset, but still moving =>  finalize movement
                if (hit.hoveredObject != hit.cityRootNode) // only reparent non-root nodes
                {
                    GameObject parent = GameNodeMover.FinalizePosition(hit.hoveredObject.gameObject);
                    if (parent != null)
                    {
                        // The move has come to a successful end.
                        new ReparentNetAction(hit.hoveredObject.gameObject.name, parent.name, hit.hoveredObject.position).Execute();
                        memento.SetNewParent(parent);
                        memento.SetNewPosition(hit.hoveredObject.position);
                        currentState = ReversibleAction.Progress.Completed;
                        result = true;
                    }
                    else
                    {
                        // An attempt was made to move the hovered object outside of the city.
                        // We need to reset it to its original position. And then we start from scratch.
                        Vector3 originalPosition = dragStartTransformPosition + dragStartOffset - Vector3.Scale(dragCanonicalOffset, hit.hoveredObject.localScale);
                        hit.hoveredObject.position = originalPosition;
                        new MoveNodeNetAction(hit.hoveredObject.name, hit.hoveredObject.position).Execute();
                        // The following assignment will override hit.interactableObject; that is why we
                        // stored its value in interactableObjectToBeUngrabbed above.
                        hit = new Hit();
                    }
                    synchronize = false; // false because we just called the necessary network action ReparentNetAction() or MoveNodeNetAction, respectively.
                }
                interactableObjectToBeUngrabbed.SetGrab(false, true);
                gizmo.gameObject.SetActive(false);
                moving = false;
            }

            if (synchronize)
            {
                new MoveNodeNetAction(hit.hoveredObject.name, hit.hoveredObject.position).Execute();
            }

            if (currentState != ReversibleAction.Progress.Completed)
            {
                currentState = moving ? ReversibleAction.Progress.InProgress : ReversibleAction.Progress.NoEffect;
            }

            return result;
        }

        public override void Undo()
        {
            base.Undo();
            memento?.Undo();
        }

        public override void Redo()
        {
            base.Redo();
            memento?.Redo();
        }
    }
}
