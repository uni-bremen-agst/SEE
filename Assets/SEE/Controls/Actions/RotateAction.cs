using System.Collections.Generic;
using SEE.Game;
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
        /// <summary>
        /// The number of degrees in a full circle.
        /// </summary>
        private const float FullCircleDegree = 360.0f;

        private struct Hit
        {
            /// <summary>
            /// The root of the code city. This is the top-most game object representing a node,
            /// i.e., is tagged by <see cref="Tags.Node"/>.
            /// </summary>
            internal Transform CityRootNode;
            internal CityCursor Cursor;
            internal UnityEngine.Plane Plane;
        }

        private const float SnapStepCount = 8;
        private const float SnapStepAngle = FullCircleDegree / SnapStepCount;
        private const int TextureResolution = 1024;
        private static readonly RotateGizmo gizmo = RotateGizmo.Create(TextureResolution);

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
        /// Resets the rotation of the last rotated CityRootNode
        /// </summary>
        public static void ResetRotate()
        {
            if (CityRootNodeMemory.CityRootNode)
            {
                gizmo.gameObject.SetActive(false);

                CityRootNodeMemory.CityRootNode.RotateAround(CityRootNodeMemory.CityRootNode.position, Vector3.up, -CityRootNodeMemory.CityRootNode.rotation.eulerAngles.y);
                SynchronizeCityRootNode();
            }
        }

        /// <summary>
        /// Creates a new <see cref="RotateNodeNetAction"/> to synchronize the CityRootNode with the network
        /// </summary>
        private static void SynchronizeCityRootNode()
        {
            new Net.RotateNodeNetAction(CityRootNodeMemory.CityRootNode.name, CityRootNodeMemory.CityRootNode.position, CityRootNodeMemory.CityRootNode.eulerAngles.y).Execute();
        }

        /// <summary>
        /// Returns a new instance of <see cref="RotateAction"/>.
        /// <see cref="ReversibleAction.Update"/>.
        /// </summary>
        /// <returns>always false</returns>
        public override bool Update()
        {
#if UNITY_ANDROID
            // FIXME: This branch of the the #ifdef and the #else branch should be consolidated.
            // Check for touch input
            if (Input.touchCount != 1)
            {
                rotating = false;
                return false;
            }

            Transform cityRootNodeMobile = null;
            Touch touch = Input.touches[0];
            Vector3 touchPosition = touch.position;
            Vector3 planeHitPoint;
            CityCursor cityCursor = null;

            if (touch.phase == TouchPhase.Began) // start or continue rotation
            {
                Ray ray = Camera.main.ScreenPointToRay(touchPosition);
                RaycastHit raycastHit;
                if (Physics.Raycast(ray, out raycastHit))
                {
                    if (raycastHit.collider.tag == DataModel.Tags.Node)
                    {
                        cityRootNodeMobile = SceneQueries.GetCityRootTransformUpwards(raycastHit.transform);
                        CityRootNodeMemory.CityRootNode = cityRootNodeMobile;
                        cityCursor = cityRootNodeMobile.GetComponentInParent<CityCursor>();
                    }
                }
                if (cityRootNodeMobile)
                {
                    UnityEngine.Plane plane = new UnityEngine.Plane(Vector3.up, cityRootNodeMobile.position);
                    if (Raycasting.RaycastPlane(plane, out planeHitPoint)) // start rotation
                    {
                        rotating = true;
                        hit.CityRootNode = cityRootNodeMobile;
                        hit.Cursor = cityCursor;
                        hit.Plane = plane;

                        gizmo.gameObject.SetActive(true);
                        gizmo.Center = cityCursor.E.HasFocus() ? hit.Cursor.E.ComputeCenter() : hit.CityRootNode.position;

                        Vector2 toHit = planeHitPoint.XZ() - gizmo.Center.XZ();
                        float toHitAngle = toHit.Angle360();

                        originalEulerAngleY = cityRootNodeMobile.rotation.eulerAngles.y;
                        originalPosition = cityRootNodeMobile.position;
                        startAngle = AngleMod(cityRootNodeMobile.rotation.eulerAngles.y - toHitAngle);
                        gizmo.StartAngle = Mathf.Deg2Rad * toHitAngle;
                        gizmo.TargetAngle = Mathf.Deg2Rad * toHitAngle;
                    }
                }
            }
            else if (rotating && Raycasting.RaycastPlane(hit.Plane, out planeHitPoint) && touch.phase == TouchPhase.Moved) // continue rotation
            {
                Vector2 toHit = planeHitPoint.XZ() - gizmo.Center.XZ();
                float toHitAngle = toHit.Angle360();
                float angle = AngleMod(startAngle + toHitAngle);
                if (SEEInput.SnapMobile)
                {
                    angle = AngleMod(Mathf.Round(angle / SnapStepAngle) * SnapStepAngle);
                }

                hit.CityRootNode.RotateAround(gizmo.Center, Vector3.up, angle - hit.CityRootNode.rotation.eulerAngles.y);

                float prevAngle = Mathf.Rad2Deg * gizmo.TargetAngle;
                float currAngle = toHitAngle;

                while (Mathf.Abs(currAngle + FullCircleDegree - prevAngle) < Mathf.Abs(currAngle - prevAngle))
                {
                    currAngle += FullCircleDegree;
                }
                while (Mathf.Abs(currAngle - FullCircleDegree - prevAngle) < Mathf.Abs(currAngle - prevAngle))
                {
                    currAngle -= FullCircleDegree;
                }
                if (SEEInput.SnapMobile)
                {
                    currAngle = Mathf.Round((currAngle + startAngle) / (SnapStepAngle)) * (SnapStepAngle) - startAngle;
                }
                gizmo.TargetAngle = Mathf.Deg2Rad * currAngle;

                SynchronizeCityRootNode();
            }
            else if (rotating && touch.phase == TouchPhase.Ended) // finalize rotation
            {
                rotating = false;
                gizmo.gameObject.SetActive(false);

                currentState = ReversibleAction.Progress.Completed;
            }

            if (currentState != ReversibleAction.Progress.Completed)
            {
                currentState = rotating ? ReversibleAction.Progress.InProgress : ReversibleAction.Progress.NoEffect;
            }

            return true;
#else
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
                    Positioner.Set(hit.CityRootNode, position: originalPosition, yAngle: originalEulerAngleY);
                    foreach (InteractableObject interactable in hit.Cursor.E.GetFocusses())
                    {
                        if (interactable.IsGrabbed)
                        {
                            interactable.SetGrab(false, true);
                        }
                    }
                    gizmo.gameObject.SetActive(false);

                    rotating = false;
                    synchronize = true;
                }
                else if (obj)
                {
                    // TODO(torben): Explanation in MoveAction.cs: @UnselectInWrongPlace
                    InteractableObject.UnselectAllInGraph(obj.ItsGraph, true);
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
                        hit.CityRootNode = cityRootNode;
                        hit.Cursor = cityCursor;
                        hit.Plane = plane;

                        foreach (InteractableObject interactable in hit.Cursor.E.GetFocusses())
                        {
                            interactable.SetGrab(true, true);
                        }
                        gizmo.gameObject.SetActive(true);
                        gizmo.Center = cityCursor.E.HasFocus() ? hit.Cursor.E.ComputeCenter() : hit.CityRootNode.position;

                        Vector2 toHit = planeHitPoint.XZ() - gizmo.Center.XZ();
                        float toHitAngle = toHit.Angle360();

                        originalEulerAngleY = cityRootNode.rotation.eulerAngles.y;
                        originalPosition = cityRootNode.position;
                        startAngle = AngleMod(cityRootNode.rotation.eulerAngles.y - toHitAngle);
                        gizmo.StartAngle = Mathf.Deg2Rad * toHitAngle;
                        gizmo.TargetAngle = Mathf.Deg2Rad * toHitAngle;
                    }
                }

                if (rotating && Raycasting.RaycastPlane(hit.Plane, out planeHitPoint)) // continue rotation
                {
                    Vector2 toHit = planeHitPoint.XZ() - gizmo.Center.XZ();
                    float toHitAngle = toHit.Angle360();
                    float angle = AngleMod(startAngle + toHitAngle);
                    if (SEEInput.Snap())
                    {
                        angle = AngleMod(Mathf.Round(angle / SnapStepAngle) * SnapStepAngle);
                    }

                    hit.CityRootNode.RotateAround(gizmo.Center, Vector3.up, angle - hit.CityRootNode.rotation.eulerAngles.y);

                    float prevAngle = Mathf.Rad2Deg * gizmo.TargetAngle;
                    float currAngle = toHitAngle;

                    while (Mathf.Abs(currAngle + FullCircleDegree - prevAngle) < Mathf.Abs(currAngle - prevAngle))
                    {
                        currAngle += FullCircleDegree;
                    }
                    while (Mathf.Abs(currAngle - FullCircleDegree - prevAngle) < Mathf.Abs(currAngle - prevAngle))
                    {
                        currAngle -= FullCircleDegree;
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
                    foreach (InteractableObject interactable in cityCursor.E.GetFocusses())
                    {
                        if (interactable.IsGrabbed)
                        {
                            interactable.SetGrab(false, true);
                        }
                    }
                    gizmo.gameObject.SetActive(false);

                    cityRootNode.RotateAround(cityCursor.E.HasFocus() ?
                          cityCursor.E.ComputeCenter()
                        : cityRootNode.position, Vector3.up, -cityRootNode.rotation.eulerAngles.y);
                    synchronize = true;
                }
            }
            else if (rotating) // finalize rotation
            {
                rotating = false;

                foreach (InteractableObject interactable in hit.Cursor.E.GetFocusses())
                {
                    interactable.SetGrab(false, true);
                }
                gizmo.gameObject.SetActive(false);

                currentState = ReversibleAction.Progress.Completed;
            }

            if (synchronize)
            {
                new Net.RotateNodeNetAction(hit.CityRootNode.name, hit.CityRootNode.position, hit.CityRootNode.eulerAngles.y).Execute();
            }

            if (currentState != ReversibleAction.Progress.Completed)
            {
                currentState = rotating ? ReversibleAction.Progress.InProgress : ReversibleAction.Progress.NoEffect;
            }

            return true;
#endif
        }

        /// <summary>
        /// Converts the given angle in degrees into the range [0, 360) degrees and returns the result.
        /// </summary>
        /// <param name="degrees">The angle in degrees.</param>
        /// <returns>The angle in the range [0, 360) degrees.</returns>
        private static float AngleMod(float degrees)
        {
            return ((degrees % FullCircleDegree) + FullCircleDegree) % FullCircleDegree;
        }
    }

    /// <summary>
    /// Saves the CityRootNode for mobile applications because a CityRootNode cant be detected by mouse hovering.
    /// </summary>
    internal static class CityRootNodeMemory
    {
        public static Transform CityRootNode;
    }
}
