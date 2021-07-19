using System.Collections.Generic;
 using SEE.Game;
using SEE.Game.UI3D;
using SEE.Utils;
using UnityEngine;
using UnityEngine.Assertions;

namespace SEE.Controls.Actions
{
    /// <summary>
    /// An action to move nodes. 
    /// </summary>
    public class MoveAction : AbstractPlayerAction
    {
        private struct Hit
        {
            internal Hit(Transform hit)
            {
                root = SceneQueries.GetCityRootTransformUpwards(hit);
                transform = hit.transform;
                interactableObject = hit.GetComponent<InteractableObject>();
                plane = new Plane(Vector3.up, root.position);
            }

            internal Transform root;
            internal Transform transform;
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
        /// <returns>always false</returns>
        public override bool Update()
        {
            InteractableObject obj = InteractableObject.HoveredObjectWithWorldFlag;
            Transform root = null;

            if (obj)
            {
                root = SceneQueries.GetCityRootTransformUpwards(obj.transform);
                Assert.IsNotNull(root);
            }

            bool synchronize = false;

            if (SEEInput.Cancel()) // cancel movement
            {
                if (moving)
                {
                    hit.transform.position = dragStartTransformPosition + dragStartOffset - Vector3.Scale(dragCanonicalOffset, hit.transform.localScale);
                    hit.interactableObject.SetGrab(false, true);
                    gizmo.gameObject.SetActive(false);

                    moving = false;
                    hit = new Hit();
                    synchronize = true;
                }
                else if (obj)
                {
                    InteractableObject.UnselectAllInGraph(obj.ItsGraph, true); // TODO(torben): this should be in SelectAction.cs
                }
            }
            else if (SEEInput.Drag()) // start or continue movement
            {
                if (SEEInput.StartDrag() && obj && Raycasting.RaycastPlane(new Plane(Vector3.up, root.position), out Vector3 planeHitPoint)) // start movement
                {
                    moving = true;
                    hit = new Hit(SEEInput.DragHovered() ? obj.transform : root);

                    hit.interactableObject.SetGrab(true, true);
                    gizmo.gameObject.SetActive(true);
                    dragStartTransformPosition = hit.transform.position;
                    dragStartOffset = planeHitPoint - hit.transform.position;
                    dragCanonicalOffset = dragStartOffset.DividePairwise(hit.transform.localScale);
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
                    hit.transform.position = dragStartTransformPosition + totalDragOffsetFromStart;
                    Vector3 startPoint = dragStartTransformPosition + dragStartOffset;
                    Vector3 endPoint = hit.transform.position + Vector3.Scale(dragCanonicalOffset, hit.transform.localScale);
                    gizmo.SetPositions(startPoint, endPoint);

                    synchronize = true;
                }
            }
            else if (SEEInput.Reset()) // reset to center of table
            {
                if (obj && !moving)
                {
                    GO.Plane plane = root.GetComponentInParent<GO.Plane>();
                    root.position = plane.CenterTop;
                    gizmo.gameObject.SetActive(false);

                    synchronize = true;
                }
            }
            else if (moving) // finalize movement
            {
                if (hit.transform != hit.root) // only reparent non-root nodes
                {
                    Vector3 originalPosition = dragStartTransformPosition + dragStartOffset - Vector3.Scale(dragCanonicalOffset, hit.transform.localScale);
                    GameNodeMover.FinalizePosition(hit.transform.gameObject, originalPosition);

                    synchronize = true;
                }
                hit.interactableObject.SetGrab(false, true);
                gizmo.gameObject.SetActive(false);

                moving = false;
                hit = new Hit();

                #region AbstractPlayerAction
                currentState = ReversibleAction.Progress.Completed;
                #endregion
            }

            if (synchronize)
            {
                // TODO(torben): synchronize!
                //new Net.SyncCitiesAction(this).Execute();
            }

            #region AbstractPlayerAction
            if (currentState != ReversibleAction.Progress.Completed)
            {
                currentState = moving ? ReversibleAction.Progress.InProgress : ReversibleAction.Progress.NoEffect;
            }
            #endregion

            return true;
        }
    }
}
