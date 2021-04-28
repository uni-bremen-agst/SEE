﻿using SEE.Game;
using SEE.Game.UI3D;
using SEE.GO;
using SEE.Utils;
using System;
using UnityEngine;

namespace SEE.Controls.Actions
{
    /// <summary>
    /// An action to rotate nodes. 
    /// </summary>
    class RotateAction : AbstractPlayerAction
    {
        private struct Hit
        {
            internal Transform root;
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
        internal static ReversibleAction CreateReversibleAction()
        {
            RotateAction result = new RotateAction
            {
                rotating = false,
                hit = new Hit(),
                originalEulerAngleY = 0.0f,
                originalPosition = Vector3.zero,
                startAngle = 0.0f
            };
            return result;
        }

        /// <summary>
        /// Returns a new instance of <see cref="RotateAction"/>.
        /// </summary>
        /// <returns>new instance</returns>
        public override ReversibleAction NewInstance()
        {
            RotateAction result = new RotateAction
            {
                rotating = rotating,
                hit = hit,
                originalEulerAngleY = originalEulerAngleY,
                originalPosition = originalPosition,
                startAngle = startAngle
            };
            return result;
        }

        /// <summary>
        /// Returns the <see cref="ActionStateType"/> of this action.
        /// </summary>
        /// <returns><see cref="ActionStateType.Rotate"/></returns>
        public override ActionStateType GetActionStateType()
        {
            return ActionStateType.Rotate;
        }

        /// <summary>
        /// <see cref="ReversibleAction.Update"/>.
        /// </summary>
        /// <returns>always false</returns>
        public override bool Update()
        {
            InteractableObject obj = InteractableObject.HoveredObject;
            Transform root = null;
            CityCursor cursor = null;

            if (obj)
            {
                root = SceneQueries.GetCityRootTransformUpwards(obj.transform);
                cursor = root.GetComponentInParent<CityCursor>();
            }

            bool synchronize = false;

            if (SEEInput.Cancel()) // cancel rotation
            {
                if (rotating)
                {
                    hit.root.position = originalPosition;
                    hit.root.rotation = Quaternion.Euler(0.0f, originalEulerAngleY, 0.0f);
                    Array.ForEach(hit.cursor.E.GetFocusses(), e =>
                    {
                        InteractableObject o = e.GetComponent<InteractableObject>();
                        if (o.IsGrabbed)
                        {
                            o.SetGrab(false, true);
                        }
                    });
                    gizmo.gameObject.SetActive(false);

                    rotating = false;
                    synchronize = true;
                }
                else if (obj)
                {
                    InteractableObject.UnselectAllInGraph(obj.ItsGraph(), true); // TODO(torben): this should be in SelectAction.cs
                }
            }
            else if (SEEInput.Drag()) // start or continue rotation
            {
                Vector3 planeHitPoint;
                if (root)
                {
                    UnityEngine.Plane plane = new UnityEngine.Plane(Vector3.up, root.position);
                    if (SEEInput.StartDrag() && Raycasting.RaycastPlane(plane, out planeHitPoint)) // start rotation
                    {
                        rotating = true;
                        hit.root = root;
                        hit.cursor = cursor;
                        hit.plane = plane;

                        Array.ForEach(hit.cursor.E.GetFocusses(), e => e.GetComponent<InteractableObject>().SetGrab(true, true));
                        gizmo.gameObject.SetActive(true);
                        gizmo.Center = cursor.E.HasFocus() ? hit.cursor.E.GetPosition() : hit.root.position;

                        Vector2 toHit = planeHitPoint.XZ() - gizmo.Center.XZ();
                        float toHitAngle = toHit.Angle360();

                        originalEulerAngleY = root.rotation.eulerAngles.y;
                        originalPosition = root.position;
                        startAngle = AngleMod(root.rotation.eulerAngles.y - toHitAngle);
                        gizmo.SetMinAngle(Mathf.Deg2Rad * toHitAngle);
                        gizmo.SetMaxAngle(Mathf.Deg2Rad * toHitAngle);
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
                    hit.root.RotateAround(gizmo.Center, Vector3.up, angle - hit.root.rotation.eulerAngles.y);

                    float prevAngle = Mathf.Rad2Deg * gizmo.GetMaxAngle();
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
                    gizmo.SetMaxAngle(Mathf.Deg2Rad * currAngle);

                    synchronize = true;
                }
            }
            else if (SEEInput.Reset()) // reset rotation to identity();
            {
                if (obj && !rotating)
                {
                    foreach (Transform t in cursor.E.GetFocusses())
                    {
                        InteractableObject o = t.GetComponent<InteractableObject>();
                        if (o.IsGrabbed)
                        {
                            o.SetGrab(false, true);
                        }
                    }
                    gizmo.gameObject.SetActive(false);

                    root.RotateAround(cursor.E.HasFocus() ? cursor.E.GetPosition() : root.position, Vector3.up, -root.rotation.eulerAngles.y);
                    synchronize = true;
                }
            }
            else if (rotating) // finalize rotation
            {
                rotating = false;

                Array.ForEach(hit.cursor.E.GetFocusses(), e => e.GetComponent<InteractableObject>().SetGrab(false, true));
                gizmo.gameObject.SetActive(false);
            }

            if (synchronize)
            {
                // TODO(torben): synchronize
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
