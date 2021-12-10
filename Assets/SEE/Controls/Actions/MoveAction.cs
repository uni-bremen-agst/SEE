using System.Collections.Generic;
﻿using SEE.Game;
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

        private bool moving;
        private Hit hit;
        private Vector3 dragStartTransformPosition;
        private Vector3 dragStartOffset;
        private Vector3 dragCanonicalOffset;

        /// <summary>
        /// Returns a new instance of <see cref="MoveAction"/>.
        /// </summary>
        /// <returns>new instance of <see cref="MoveAction"/></returns>
        internal static ReversibleAction CreateReversibleAction() => new MoveAction
        {
            moving = false,
            hit = new Hit(),
            dragStartTransformPosition = Vector3.positiveInfinity,
            dragStartOffset = Vector3.positiveInfinity,
            dragCanonicalOffset = Vector3.positiveInfinity
        };

        /// <summary>
        /// Returns a new instance of <see cref="MoveAction"/>.
        /// </summary>
        /// <returns>new instance</returns>
        public override ReversibleAction NewInstance() => new MoveAction
        {
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
            return new HashSet<string>();
        }

        /// Returns the <see cref="ActionStateType"/> of this action.
        /// </summary>
        /// <returns><see cref="ActionStateType.Move"/></returns>
        public override ActionStateType GetActionStateType()
        {
            return ActionStateType.Move;
        }

        /// <summary>
        /// <see cref="ReversibleAction.Update"/>.
        /// </summary>
        /// <returns>always true</returns>
        public override bool Update()
        {
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
                    CodeCityManipulator.Set(hit.hoveredObject, dragStartTransformPosition + dragStartOffset - Vector3.Scale(dragCanonicalOffset, hit.hoveredObject.localScale));
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
                if (SEEInput.StartDrag() && hoveredObject && Raycasting.RaycastPlane(new Plane(Vector3.up, cityRootNode.position), out Vector3 planeHitPoint)) // start movement
                {
                    moving = true;
                    hit = new Hit(SEEInput.DragHovered() ? hoveredObject.transform : cityRootNode);

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
                    CodeCityManipulator.Set(hit.hoveredObject, dragStartTransformPosition + totalDragOffsetFromStart);
                    Vector3 startPoint = dragStartTransformPosition + dragStartOffset;
                    Vector3 endPoint = hit.hoveredObject.position + Vector3.Scale(dragCanonicalOffset, hit.hoveredObject.localScale);
                    gizmo.SetPositions(startPoint, endPoint);

                    synchronize = true;
                }
            }
            else if (SEEInput.Reset()) // reset to center of table
            {
                if (hoveredObject && !moving)
                {
                    GO.Plane plane = cityRootNode.GetComponentInParent<GO.Plane>();
                    cityRootNode.position = plane.CenterTop;
                    gizmo.gameObject.SetActive(false);

                    synchronize = true;
                }
            }
            else if (moving) // finalize movement
            {
                if (hit.hoveredObject != hit.cityRootNode) // only reparent non-root nodes
                {
                    GameObject parent = GameNodeMover.FinalizePosition(hit.hoveredObject.gameObject);
                    if (parent != null)
                    {
                        new ReparentNetAction(hit.hoveredObject.gameObject.name, parent.name, hit.hoveredObject.position).Execute();
                        synchronize = false; // false because we just called the necessary network action ReparentNetAction().
                    }
                    else
                    {
                        Vector3 originalPosition = dragStartTransformPosition + dragStartOffset - Vector3.Scale(dragCanonicalOffset, hit.hoveredObject.localScale);
                        hit.hoveredObject.position = originalPosition;
                        // We run MoveCityNetAction here because hit will be reset below.
                        new MoveCityNetAction(hit.hoveredObject.name, hit.hoveredObject.position).Execute();
                        synchronize = false; // false because we just called MoveCityNetAction
                    }
                }
                hit.interactableObject.SetGrab(false, true);
                gizmo.gameObject.SetActive(false);

                moving = false;
                hit = new Hit();

                currentState = ReversibleAction.Progress.Completed;
            }

            if (synchronize)
            {
                new MoveCityNetAction(hit.hoveredObject.name, hit.hoveredObject.position).Execute();
            }

            if (currentState != ReversibleAction.Progress.Completed)
            {
                currentState = moving ? ReversibleAction.Progress.InProgress : ReversibleAction.Progress.NoEffect;
            }

            return true;
        }
    }
}
