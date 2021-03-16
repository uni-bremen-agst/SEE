using System;
using SEE.Game;
using SEE.Game.UI3D;
using SEE.GO;
using SEE.Utils;
using UnityEngine;

namespace SEE.Controls.Actions
{
    /// <summary>
    /// Controls the interactions with the city in desktop mode regarding the movement
    /// and perspective on a code city (rotating, dragging, zooming, etc.).
    /// 
    /// Note: These are the interactions on a desktop environment with 2D display,
    /// mouse, and keyboard. Similar interactions specific to VR are implemented
    /// in XRNavigationAction.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class DesktopNavigationAction : NavigationAction
    {
        /// <summary>
        /// The state for moving the city or parts of the city.
        /// </summary>
        private struct _MoveState
        {
            internal const float MaxVelocity = 10.0f;                        // This is only used, if the city was moved as a whole
            internal const float MaxSqrVelocity = MaxVelocity * MaxVelocity;
            internal const float DragFrictionFactor = 32.0f;
            internal const float SnapStepCount = 8;
            internal const float SnapStepAngle = 360.0f / SnapStepCount;

            internal MoveGizmo moveGizmo;
            internal Bounds cityBounds;
            internal Vector3 dragStartTransformPosition;
            internal Vector3 dragStartOffset;
            internal Vector3 dragCanonicalOffset;
            internal Vector3 moveVelocity;
            internal Transform draggedTransform;
        }

        /// <summary>
        /// The state for rotating the city.
        /// </summary>
        private struct _RotateState
        {
            internal const float SnapStepCount = 8;
            internal const float SnapStepAngle = 360.0f / SnapStepCount;

            internal RotateGizmo rotateGizmo;
            internal float originalEulerAngleY;
            internal Vector3 originalPosition;
            internal float startAngle;
        }

        /// <summary>
        /// The actions, that are currently active. This is initially set by
        /// <see cref="Update"/> and used and partly reset in <see cref="FixedUpdate"/>.
        /// </summary>
        private struct _ActionState
        {
            internal bool selectToggle;       // Whether selected elements should be toggled instead of being selected separate
            internal bool startDrag;
            internal bool dragHoveredOnly;    // true, if only the element, that is hovered by the mouse should be moved instead of whole city
            internal bool drag;
            internal bool cancel;
            internal bool snap;
            internal bool reset;
            internal Vector3 mousePosition;
            internal float zoomStepsDelta;
            internal bool zoomToggleToObject;
            internal Transform hoveredTransform;
        }

        /// <summary>
        /// The plane, the city is located on top of.
        /// </summary>
        private UnityEngine.Plane raycastPlane;

        /// <summary>
        /// Whether the city is currently moved or rotated by the player.
        /// </summary>
        private bool movingOrRotating;

        /// <summary>
        /// The cursor visually represents the center of all selected objects and is used
        /// for the center of rotations.
        /// </summary>
        private Game.UI3D.Cursor cursor;

        /// <summary>
        /// The current move state.
        /// </summary>
        private _MoveState moveState;

        /// <summary>
        /// The current rotate state.
        /// </summary>
        private _RotateState rotateState;

        /// <summary>
        /// The current action state.
        /// </summary>
        private _ActionState actionState;

        protected sealed override void Awake()
        {
            if (FindObjectOfType<PlayerSettings>().playerInputType != PlayerInputType.DesktopPlayer)
            {
                Destroy(this);
                return;
            }

            base.Awake();

            ActionState.OnStateChanged += OnStateChanged;
        }

        protected sealed override void OnCityAvailable()
        {
            raycastPlane = new UnityEngine.Plane(Vector3.up, CityTransform.position);
            movingOrRotating = false;
            cursor = Game.UI3D.Cursor.Create();

            moveState.moveGizmo = MoveGizmo.Create(0.008f * portalPlane.MinLengthXZ);
            moveState.cityBounds = CityTransform.GetComponent<Collider>().bounds;
            moveState.dragStartTransformPosition = Vector3.zero;
            moveState.dragStartOffset = Vector3.zero;
            moveState.dragCanonicalOffset = Vector3.zero;
            moveState.moveVelocity = Vector3.zero;

            rotateState.rotateGizmo = RotateGizmo.Create(portalPlane, 1024);
            rotateState.originalEulerAngleY = 0.0f;
            rotateState.originalPosition = Vector3.zero;
            rotateState.startAngle = 0.0f;
        }

        private void OnDestroy()
        {
            ActionState.OnStateChanged -= OnStateChanged;
        }

        public sealed override void Update()
        {
            base.Update();

            if (CityTransform != null)
            {
                bool isMouseOverGUI = Raycasting.IsMouseOverGUI();

                // Fill action state with player input. Input MUST NOT be inquired in
                // FixedUpdate() for the input to feel responsive!
                actionState.selectToggle = Input.GetKey(KeyCode.LeftControl);
                actionState.drag = Input.GetMouseButton(2);
                actionState.startDrag |= !isMouseOverGUI && Input.GetMouseButtonDown(2);
                actionState.dragHoveredOnly = Input.GetKey(KeyCode.LeftControl);
                actionState.cancel |= Input.GetKeyDown(KeyCode.Escape);
                actionState.snap = Input.GetKey(KeyCode.LeftAlt);
                actionState.reset |= (actionState.drag || !isMouseOverGUI) && Input.GetKeyDown(KeyCode.R);
                actionState.mousePosition = Input.mousePosition;

                // FIXME: The selection of graph elements below will executed only if the 
                // ray hits the clipping area. If the player looks at the city from aside,
                // hit nodes and edges will not selected because the ray may not hit the 
                // area. Large nodes and in particular edges above nodes tend to be high
                // in the sky and may not be selectable if the player views the city from
                // a suboptimal angle.
                RaycastClippingPlane(out bool _, out bool insideClippingArea, out Vector3 _);

                // Find hovered GameObject with node or edge, if it exists
                actionState.hoveredTransform = null;
                if (insideClippingArea)
                {
                    if (Input.GetKeyDown(KeyCode.U))
                    {
                        InteractableObject.UnselectAll(true);
                    }
                    else if (Raycasting.RaycastGraphElement(out RaycastHit raycastHit, out GraphElementRef _) != HitGraphElement.None)
                    {
                        Transform hoveredTransform = raycastHit.transform;
                        // parentTransform walks up the game-object hierarchy toward the
                        // containing CityTransform. If the CityTransform is reached, we
                        // know that hoveredTransform is part of the CityTransform, thus,
                        // belongs to the city, we are dealing with.
                        Transform parentTransform = hoveredTransform;
                        do
                        {
                            if (parentTransform == CityTransform)
                            {                                
                                actionState.hoveredTransform = hoveredTransform;
                                break;
                            }
                            else
                            {
                                parentTransform = parentTransform.parent;
                            }
                        } while (parentTransform != null);
                    }

                    // For simplicity, zooming is only allowed if the city is not
                    // currently dragged
                    if (!isMouseOverGUI && !actionState.drag)
                    {
                        actionState.zoomStepsDelta += Input.mouseScrollDelta.y;
                    }
                }

                if (!actionState.drag && !Equals(ActionState.Value, ActionStateType.Map))
                {
                    actionState.zoomToggleToObject |= Input.GetKeyDown(KeyCode.F);

                    if (Input.GetMouseButtonDown(0) && !isMouseOverGUI)
                    {
                        Select(actionState.hoveredTransform ? actionState.hoveredTransform.gameObject : null, !actionState.selectToggle);
                    }
                }

                if (Equals(ActionState.Value, ActionStateType.Rotate) && cursor.HasFocus())
                {
                    rotateState.rotateGizmo.Center = cursor.GetPosition();
                    rotateState.rotateGizmo.Radius = 0.2f * (MainCamera.Camera.transform.position - rotateState.rotateGizmo.Center).magnitude;
                }
            }
        }

        // This logic is in FixedUpdate(), so that the behaviour is framerate-'independent'.
        private void FixedUpdate()
        {
            if (CityTransform == null)
            {
                return;
            }

            bool synchronize = false;
            RaycastClippingPlane(out bool hitPlane, out bool insideClippingArea, out Vector3 planeHitPoint);

            if (actionState.cancel && !movingOrRotating && !actionState.selectToggle)
            {
                Select(null, true);
            }

            #region Move City

            else if (Equals(ActionState.Value, ActionStateType.Move))
            {
                if (actionState.reset) // reset to center of table
                {
                    if ((insideClippingArea && !(actionState.drag ^ movingOrRotating)) || (actionState.drag && movingOrRotating))
                    {
                        movingOrRotating = false;

                        if (moveState.draggedTransform)
                        {
                            moveState.draggedTransform.GetComponent<InteractableObject>().SetGrab(false, true);
                            moveState.draggedTransform = null;
                        }
                        moveState.moveGizmo.gameObject.SetActive(false);

                        CityTransform.position = portalPlane.CenterTop;
                        synchronize = true;
                    }
                }
                else if (actionState.cancel) // cancel movement
                {
                    if (movingOrRotating)
                    {
                        movingOrRotating = false;

                        moveState.draggedTransform.GetComponent<InteractableObject>().SetGrab(false, true);
                        moveState.moveGizmo.gameObject.SetActive(false);

                        moveState.moveVelocity = Vector3.zero;
                        moveState.draggedTransform.position =
                            moveState.dragStartTransformPosition + moveState.dragStartOffset
                            - Vector3.Scale(moveState.dragCanonicalOffset, moveState.draggedTransform.localScale);
                        moveState.draggedTransform = null;
                        synchronize = true;
                    }
                }
                else if (actionState.drag && hitPlane) // start or continue movement
                {
                    if (actionState.startDrag) // start movement
                    {
                        if (insideClippingArea)
                        {
                            if (actionState.dragHoveredOnly)
                            {
                                if (actionState.hoveredTransform != null)
                                {
                                    movingOrRotating = true;
                                    moveState.draggedTransform = actionState.hoveredTransform;

                                }
                            }
                            else
                            {
                                movingOrRotating = true;
                                moveState.draggedTransform = CityTransform;
                            }

                            if (movingOrRotating)
                            {
                                moveState.draggedTransform.GetComponent<InteractableObject>().SetGrab(true, true);
                                moveState.moveGizmo.gameObject.SetActive(true);

                                moveState.dragStartTransformPosition = moveState.draggedTransform.position;
                                moveState.dragStartOffset = planeHitPoint - moveState.draggedTransform.position;
                                moveState.dragCanonicalOffset = moveState.dragStartOffset.DividePairwise(moveState.draggedTransform.localScale);
                                moveState.moveVelocity = Vector3.zero;
                            }
                        }
                    }

                    if (movingOrRotating) // continue movement
                    {
                        Vector3 totalDragOffsetFromStart = planeHitPoint - (moveState.dragStartTransformPosition + moveState.dragStartOffset);

                        if (actionState.snap)
                        {
                            Vector2 point2 = new Vector2(totalDragOffsetFromStart.x, totalDragOffsetFromStart.z);
                            float angleDeg = point2.Angle360();
                            float snappedAngleDeg = Mathf.Round(angleDeg / _MoveState.SnapStepAngle) * _MoveState.SnapStepAngle;
                            float snappedAngleRad = Mathf.Deg2Rad * snappedAngleDeg;
                            Vector2 dir = new Vector2(Mathf.Cos(snappedAngleRad), Mathf.Sin(-snappedAngleRad));
                            Vector2 proj = dir * Vector2.Dot(point2, dir);
                            totalDragOffsetFromStart = new Vector3(proj.x, totalDragOffsetFromStart.y, proj.y);
                        }

                        Vector3 oldPosition = moveState.draggedTransform.position;
                        Vector3 newPosition = moveState.dragStartTransformPosition + totalDragOffsetFromStart;

                        moveState.moveVelocity = (newPosition - oldPosition) / Time.fixedDeltaTime; // TODO(torben): it might be possible to determine velocity only on release
                        moveState.draggedTransform.position = newPosition;
                        moveState.moveGizmo.SetPositions(
                            moveState.dragStartTransformPosition + moveState.dragStartOffset,
                            moveState.draggedTransform.position + Vector3.Scale(moveState.dragCanonicalOffset, moveState.draggedTransform.localScale));
                        synchronize = true;
                    }
                }
                else if (movingOrRotating) // finalize movement
                {
                    if (moveState.draggedTransform != CityTransform) // only reparent non-root nodes
                    {
                        synchronize = true;

                        Transform movingObject = moveState.draggedTransform;
                        Vector3 originalPosition = moveState.dragStartTransformPosition + moveState.dragStartOffset
                                - Vector3.Scale(moveState.dragCanonicalOffset, movingObject.localScale);

                        GameNodeMover.FinalizePosition(movingObject.gameObject, originalPosition);
                    }

                    actionState.startDrag = false;
                    movingOrRotating = false;

                    moveState.draggedTransform.GetComponent<InteractableObject>().SetGrab(false, true);
                    if (moveState.draggedTransform != CityTransform)
                    {
                        moveState.moveVelocity = Vector3.zero; // TODO(torben): do we want to apply velocity to individually moved buildings or keep it like this?
                    }
                    moveState.draggedTransform = null;
                    moveState.moveGizmo.gameObject.SetActive(false);
                }
            }

            #endregion

            #region Rotate

            else if (Equals(ActionState.Value, ActionStateType.Rotate))
            {
                if (actionState.reset) // reset rotation to identity();
                {
                    if ((insideClippingArea && !(actionState.drag ^ movingOrRotating)) || (actionState.drag && movingOrRotating))
                    {
                        movingOrRotating = false;

                        Array.ForEach(cursor.GetFocusses(), e => e.GetComponent<InteractableObject>().SetGrab(false, true));
                        rotateState.rotateGizmo.gameObject.SetActive(false);

                        CityTransform.RotateAround(rotateState.rotateGizmo.Center, Vector3.up, -CityTransform.rotation.eulerAngles.y);
                        synchronize = true;
                    }
                }
                else if (actionState.cancel) // cancel rotation
                {
                    if (movingOrRotating)
                    {
                        movingOrRotating = false;

                        Array.ForEach(cursor.GetFocusses(), e => e.GetComponent<InteractableObject>().SetGrab(false, true));
                        rotateState.rotateGizmo.gameObject.SetActive(false);

                        CityTransform.rotation = Quaternion.Euler(0.0f, rotateState.originalEulerAngleY, 0.0f);
                        CityTransform.position = rotateState.originalPosition;
                        synchronize = true;
                    }
                }
                else if (actionState.drag && hitPlane && cursor.HasFocus()) // start or continue rotation
                {
                    Vector2 toHit = planeHitPoint.XZ() - rotateState.rotateGizmo.Center.XZ();
                    float toHitAngle = toHit.Angle360();

                    if (actionState.startDrag) // start rotation
                    {
                        movingOrRotating = true;

                        Array.ForEach(cursor.GetFocusses(), e => e.GetComponent<InteractableObject>().SetGrab(true, true));
                        rotateState.rotateGizmo.gameObject.SetActive(true);

                        rotateState.originalEulerAngleY = CityTransform.rotation.eulerAngles.y;
                        rotateState.originalPosition = CityTransform.position;
                        rotateState.startAngle = AngleMod(CityTransform.rotation.eulerAngles.y - toHitAngle);
                        rotateState.rotateGizmo.SetMinAngle(Mathf.Deg2Rad * toHitAngle);
                        rotateState.rotateGizmo.SetMaxAngle(Mathf.Deg2Rad * toHitAngle);
                    }

                    if (movingOrRotating) // continue rotation
                    {
                        float angle = AngleMod(rotateState.startAngle + toHitAngle);
                        if (actionState.snap)
                        {
                            angle = AngleMod(Mathf.Round(angle / _RotateState.SnapStepAngle) * _RotateState.SnapStepAngle);
                        }
                        CityTransform.RotateAround(cursor.GetPosition(), Vector3.up, angle - CityTransform.rotation.eulerAngles.y);

                        float prevAngle = Mathf.Rad2Deg * rotateState.rotateGizmo.GetMaxAngle();
                        float currAngle = toHitAngle;

                        while (Mathf.Abs(currAngle + 360.0f - prevAngle) < Mathf.Abs(currAngle - prevAngle))
                        {
                            currAngle += 360.0f;
                        }
                        while (Mathf.Abs(currAngle - 360.0f - prevAngle) < Mathf.Abs(currAngle - prevAngle))
                        {
                            currAngle -= 360.0f;
                        }
                        if (actionState.snap)
                        {
                            currAngle = Mathf.Round((currAngle + rotateState.startAngle) / (_RotateState.SnapStepAngle)) * (_RotateState.SnapStepAngle) - rotateState.startAngle;
                        }

                        rotateState.rotateGizmo.SetMaxAngle(Mathf.Deg2Rad * currAngle);
                        synchronize = true;
                    }
                }
                else if (movingOrRotating) // finalize rotation
                {
                    movingOrRotating = false;

                    Array.ForEach(cursor.GetFocusses(), e => e.GetComponent<InteractableObject>().SetGrab(false, true));
                    rotateState.rotateGizmo.gameObject.SetActive(false);
                }
            }

            #endregion

            #region Zoom

            if (actionState.zoomToggleToObject)
            {
                actionState.zoomToggleToObject = false;
                if (cursor.HasFocus())
                {
                    float optimalTargetZoomFactor = portalPlane.MinLengthXZ / (cursor.GetDiameterXZ() / zoomState.currentZoomFactor);
                    float optimalTargetZoomSteps = ConvertZoomFactorToZoomSteps(optimalTargetZoomFactor);
                    int actualTargetZoomSteps = Mathf.FloorToInt(optimalTargetZoomSteps);
                    float actualTargetZoomFactor = ConvertZoomStepsToZoomFactor(actualTargetZoomSteps);

                    int zoomSteps = actualTargetZoomSteps - (int)zoomState.currentTargetZoomSteps;
                    if (zoomSteps == 0)
                    {
                        zoomSteps = -(int)zoomState.currentTargetZoomSteps;
                        actualTargetZoomFactor = 1.0f;
                    }

                    if (zoomSteps != 0)
                    {
                        float zoomFactor = ConvertZoomStepsToZoomFactor(zoomSteps);
                        Vector2 centerOfTableAfterZoom = zoomSteps == -(int)zoomState.currentTargetZoomSteps ? CityTransform.position.XZ() : cursor.GetPosition().XZ();
                        Vector2 toCenterOfTable = portalPlane.CenterXZ - centerOfTableAfterZoom;
                        Vector2 zoomCenter = portalPlane.CenterXZ - (toCenterOfTable * (zoomFactor / (zoomFactor - 1.0f)));
                        float duration = 2.0f * ZoomState.DefaultZoomDuration;
                        PushZoomCommand(zoomCenter, zoomSteps, duration);
                    }
                }
            }

            if (Mathf.Abs(actionState.zoomStepsDelta) >= 1.0f && insideClippingArea)
            {
                float fZoomSteps = actionState.zoomStepsDelta >= 0.0f ? Mathf.Floor(actionState.zoomStepsDelta) : Mathf.Ceil(actionState.zoomStepsDelta);
                actionState.zoomStepsDelta -= fZoomSteps;
                int zoomSteps = Mathf.Clamp((int)fZoomSteps, -(int)zoomState.currentTargetZoomSteps, (int)ZoomState.ZoomMaxSteps - (int)zoomState.currentTargetZoomSteps);
                PushZoomCommand(planeHitPoint.XZ(), zoomSteps, ZoomState.DefaultZoomDuration);
            }

            if (UpdateZoom())
            {
                synchronize = true;
            }

            #endregion

            #region ApplyVelocityAndConstraints

            if (!movingOrRotating)
            {
                // Clamp velocity
                float sqrMag = moveState.moveVelocity.sqrMagnitude;
                if (sqrMag > _MoveState.MaxSqrVelocity)
                {
                    moveState.moveVelocity = moveState.moveVelocity / Mathf.Sqrt(sqrMag) * _MoveState.MaxVelocity;
                }

                // Apply velocity to city position
                if (moveState.moveVelocity.sqrMagnitude != 0.0f)
                {
                    CityTransform.position += moveState.moveVelocity * Time.fixedDeltaTime;
                    synchronize = true;
                }

                // Apply friction to velocity
                Vector3 acceleration = _MoveState.DragFrictionFactor * -moveState.moveVelocity;
                moveState.moveVelocity += acceleration * Time.fixedDeltaTime;
            }

            // Keep city constrained to table
            float radius = 0.5f * CityTransform.lossyScale.x;
            MathExtensions.TestCircleAABB(CityTransform.position.XZ(),
                                          0.9f * radius,
                                          portalPlane.LeftFrontCorner,
                                          portalPlane.RightBackCorner,
                                          out float distance,
                                          out Vector2 normalizedFromCircleToSurfaceDirection);

            if (distance > 0.0f)
            {
                Vector2 toSurfaceDirection = distance * normalizedFromCircleToSurfaceDirection;
                CityTransform.position += new Vector3(toSurfaceDirection.x, 0.0f, toSurfaceDirection.y);
            }

            #endregion

            #region ResetActionState

            actionState.startDrag = false;
            actionState.cancel = false;
            actionState.reset = false;
            actionState.zoomToggleToObject = false;

            #endregion

            #region Synchronize

            if (synchronize)
            {
                new Net.SyncCitiesAction(this).Execute();
            }
            
            #endregion
        }

        private void OnStateChanged(ActionStateType value)
        {
            movingOrRotating = false;
            if (Equals(value, ActionStateType.Move))
            {
                rotateState.rotateGizmo?.gameObject.SetActive(false);
            }
            else if (Equals(value, ActionStateType.Rotate))
            {
                moveState.moveGizmo?.gameObject.SetActive(false);
            }
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

        /// <summary>
        /// If <paramref name="replaceAndDontToggle"/> is <code>true</code>, the given
        /// object (if not <code>null</code>) will be selected and every selected object
        /// will be deselected. Otherwise, the given objects selection state will be
        /// toggled and the selection state of other selected objects will not change.
        /// </summary>
        /// <param name="go">The object to be selected/toggled.</param>
        /// <param name="replaceAndDontToggle">Whether the object should be selected
        /// solely or be toggled on/off.</param>
        private void Select(GameObject go, bool replaceAndDontToggle)
        {
            if (replaceAndDontToggle)
            {
                foreach (Transform oldFocus in cursor.GetFocusses())
                {
                    InteractableObject interactable = oldFocus.GetComponent<InteractableObject>();
                    if (interactable)
                    {
                        interactable.SetSelect(false, true); // TODO(torben): callback and have cursor always focus on every 'InteractableObject.SelectedObjects'-element
                        cursor.RemoveFocus(oldFocus);
                    }
                }

                if (go)
                {
                    go.GetComponent<InteractableObject>()?.SetSelect(true, true);
                    cursor.ReplaceFocus(go.transform);
                }
            }
            else if (go) // replaceAndDontToggle == false
            {
                InteractableObject interactable = go.GetComponent<InteractableObject>();
                if (interactable)
                {
                    if (interactable.IsSelected)
                    {
                        interactable.SetSelect(false, true);
                        cursor.RemoveFocus(go.transform);
                    }
                    else
                    {
                        interactable.SetSelect(true, true);
                        cursor.AddFocus(go.transform);
                    }
                }
            }
        }

        /// <summary>
        /// Raycasts against the clipping plane of the city ground.
        /// </summary>
        /// <param name="hitPlane">Whether the infinite plane was hit
        /// (<code>false</code>, if ray is parallel to plane).</param>
        /// <param name="insideClippingArea">Whether the plane was hit inside of its
        /// clipping area.</param>
        /// <param name="planeHitPoint">The point, the plane was hit by the ray.</param>
        private void RaycastClippingPlane(out bool hitPlane, out bool insideClippingArea, out Vector3 planeHitPoint)
        {
            Ray ray = MainCamera.Camera.ScreenPointToRay(actionState.mousePosition);

            hitPlane = raycastPlane.Raycast(ray, out float enter);
            if (hitPlane)
            {
                planeHitPoint = ray.GetPoint(enter);
                MathExtensions.TestPointAABB(
                    planeHitPoint.XZ(),
                    portalPlane.LeftFrontCorner,
                    portalPlane.RightBackCorner,
                    out float distanceFromPoint,
                    out Vector2 _
                );
                insideClippingArea = distanceFromPoint < 0.0f;
            }
            else
            {
                insideClippingArea = false;
                planeHitPoint = Vector3.zero;
            }
        }
    }
}
