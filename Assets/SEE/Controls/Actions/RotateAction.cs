using System;
using System.Collections.Generic;
﻿using SEE.Game;
using SEE.Game.UI3D;
using SEE.GO;
using SEE.Utils;
using UnityEngine;

namespace SEE.Controls.Actions
{
    /// <summary>
    /// An action to rotate nodes.
    /// </summary>
    internal class RotateAction : AbstractPlayerAction
    {
        private struct Hit
        {
            /// <summary>
            /// The root of the code city. This the top-most game object representing a node,
            /// i.e., is tagged by <see cref="Tags.Node"/>.
            /// </summary>
            internal Transform cityRootNode;
            internal CityCursor cursor;
            internal UnityEngine.Plane plane;
        }

        private const float SnapStepCount = 8;
        private const float SnapStepAngle = 360.0f / SnapStepCount;

        private static readonly RotateGizmo gizmo = RotateGizmo.Create(1024);

        private bool rotating;
        private Hit hit;
        private float originalEulerAngleY;
        private Vector3 originalPosition;
        private float startAngle;

        /// <summary>
        /// Returns a new instance of <see cref="RotateAction"/>.
        /// </summary>
        /// <returns>new instance of <see cref="RotateAction"/></returns>
        internal static ReversibleAction CreateReversibleAction() => new RotateAction
        {
            rotating = false,
            hit = new Hit(),
            originalEulerAngleY = 0.0f,
            originalPosition = Vector3.zero,
            startAngle = 0.0f
        };

        /// <summary>
        /// Returns a new instance of <see cref="RotateAction"/>.
        /// </summary>
        /// <returns>new instance</returns>
        public override ReversibleAction NewInstance() => new RotateAction
        {
            rotating = rotating,
            hit = hit,
            originalEulerAngleY = originalEulerAngleY,
            originalPosition = originalPosition,
            startAngle = startAngle
        };

        /// <summary>
        /// Returns the <see cref="ActionStateType"/> of this action.
        /// </summary>
        /// <returns><see cref="ActionStateType.Rotate"/></returns>
        public override ActionStateType GetActionStateType()
        {
            return ActionStateType.Rotate;
        }

        /// <summary>
        /// Returns the set of IDs of all game objects changed by this action.
        /// <see cref="ReversibleAction.GetChangedObjects"/>
        /// </summary>
        /// <returns>empty set because this action does not change anything</returns>
        public override HashSet<string> GetChangedObjects()
        {
            return new HashSet<string>();
        }

        /// <summary>
        /// Returns a new instance of <see cref="RotateAction"/>.
        /// <see cref="ReversibleAction.Update"/>.
        /// </summary>
        /// <returns>always false</returns>
        public override bool Update()
        {
            InteractableObject obj = InteractableObject.HoveredObjectWithWorldFlag;
            Transform cityRootNode = null;
            CityCursor cityCursor = null;

            if (obj)
            {
                cityRootNode = SceneQueries.GetCityRootTransformUpwards(obj.transform);
                cityCursor = cityRootNode.GetComponentInParent<CityCursor>();
            }

            bool synchronize = false;

            if (SEEInput.Cancel()) // cancel rotation
            {
                if (rotating)
                {
                    CodeCityManipulator.Set(hit.cityRootNode, position: originalPosition, yAngle: originalEulerAngleY);
                    foreach (InteractableObject o in hit.cursor.E.GetFocusses())
                    {
                        if (o.IsGrabbed)
                        {
                            o.SetGrab(false, true);
                        }
                    }
                    gizmo.gameObject.SetActive(false);

                    rotating = false;
                    synchronize = true;
                }
                else if (obj)
                {
                    InteractableObject.UnselectAllInGraph(obj.ItsGraph, true); // TODO(torben): Explanation in MoveAction.cs: @UnselectInWrongPlace
                }
            }
            else if (SEEInput.Drag()) // start or continue rotation
            {
                Vector3 planeHitPoint;
                if (cityRootNode)
                {
                    UnityEngine.Plane plane = new UnityEngine.Plane(Vector3.up, cityRootNode.position);
                    if (SEEInput.StartDrag() && Raycasting.RaycastPlane(plane, out planeHitPoint)) // start rotation
                    {
                        rotating = true;
                        hit.cityRootNode = cityRootNode;
                        hit.cursor = cityCursor;
                        hit.plane = plane;

                        foreach (InteractableObject o in hit.cursor.E.GetFocusses())
                        {
                            o.SetGrab(true, true);
                        }
                        gizmo.gameObject.SetActive(true);
                        gizmo.Center = cityCursor.E.HasFocus() ? hit.cursor.E.ComputeCenter() : hit.cityRootNode.position;

                        Vector2 toHit = planeHitPoint.XZ() - gizmo.Center.XZ();
                        float toHitAngle = toHit.Angle360();

                        originalEulerAngleY = cityRootNode.rotation.eulerAngles.y;
                        originalPosition = cityRootNode.position;
                        startAngle = AngleMod(cityRootNode.rotation.eulerAngles.y - toHitAngle);
                        gizmo.StartAngle = Mathf.Deg2Rad * toHitAngle;
                        gizmo.TargetAngle = Mathf.Deg2Rad * toHitAngle;
                    }
                }

                if (rotating && Raycasting.RaycastPlane(hit.plane, out planeHitPoint)) // continue rotation
                {
                    Vector2 toHit = planeHitPoint.XZ() - gizmo.Center.XZ();
                    float toHitAngle = toHit.Angle360();
                    float angle = AngleMod(startAngle + toHitAngle);
                    if (SEEInput.Snap())
                    {
                        angle = AngleMod(Mathf.Round(angle / SnapStepAngle) * SnapStepAngle);
                    }

                    hit.cityRootNode.RotateAround(gizmo.Center, Vector3.up, angle - hit.cityRootNode.rotation.eulerAngles.y);

                    float prevAngle = Mathf.Rad2Deg * gizmo.TargetAngle;
                    float currAngle = toHitAngle;

                    while (Mathf.Abs(currAngle + 360.0f - prevAngle) < Mathf.Abs(currAngle - prevAngle))
                    {
                        currAngle += 360.0f;
                    }
                    while (Mathf.Abs(currAngle - 360.0f - prevAngle) < Mathf.Abs(currAngle - prevAngle))
                    {
                        currAngle -= 360.0f;
                    }
                    if (SEEInput.Snap())
                    {
                        currAngle = Mathf.Round((currAngle + startAngle) / (SnapStepAngle)) * (SnapStepAngle) - startAngle;
                    }
                    gizmo.TargetAngle = Mathf.Deg2Rad * currAngle;

                    synchronize = true;
                }
            }
            else if (SEEInput.Reset()) // reset rotation to identity();
            {
                if (obj && !rotating)
                {
                    foreach (InteractableObject o in cityCursor.E.GetFocusses())
                    {
                        if (o.IsGrabbed)
                        {
                            o.SetGrab(false, true);
                        }
                    }
                    gizmo.gameObject.SetActive(false);

                    cityRootNode.RotateAround(cityCursor.E.HasFocus() ? cityCursor.E.ComputeCenter() : cityRootNode.position, Vector3.up, -cityRootNode.rotation.eulerAngles.y);
                    synchronize = true;
                }
            }
            else if (rotating) // finalize rotation
            {
                rotating = false;

                foreach (InteractableObject o in hit.cursor.E.GetFocusses())
                {
                    o.SetGrab(false, true);
                }
                gizmo.gameObject.SetActive(false);

                currentState = ReversibleAction.Progress.Completed;
            }

            if (synchronize)
            {
                new Net.RotateNodeNetAction(hit.cityRootNode.name, hit.cityRootNode.position, hit.cityRootNode.eulerAngles.y).Execute();
            }

            if (currentState != ReversibleAction.Progress.Completed)
            {
                currentState = rotating ? ReversibleAction.Progress.InProgress : ReversibleAction.Progress.NoEffect;
            }

            return true;
        }

        /// <summary>
        /// Converts the given angle in degrees into the range [0, 360) degrees and returns the result.
        /// </summary>
        /// <param name="degrees">The angle in degrees.</param>
        /// <returns>The angle in the range [0, 360) degrees.</returns>
        private static float AngleMod(float degrees)
        {
            return ((degrees % 360.0f) + 360.0f) % 360.0f;
        }
    }
}
