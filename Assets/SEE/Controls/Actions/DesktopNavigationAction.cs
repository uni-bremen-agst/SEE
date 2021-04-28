using System;
using SEE.DataModel.DG;
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
    [RequireComponent(typeof(CityCursor))]
    [RequireComponent(typeof(Collider))]
    [RequireComponent(typeof(SEECity))]
    public class DesktopNavigationAction : NavigationAction
    {
        /// <summary>
        /// The state for moving the city or parts of the city.
        /// </summary>
        private struct _MoveState
        {
            internal const float SnapStepCount = 8;
            internal const float SnapStepAngle = 360.0f / SnapStepCount;

            internal MoveGizmo moveGizmo;
            internal Bounds cityBounds;
            internal Vector3 dragStartTransformPosition;
            internal Vector3 dragStartOffset;
            internal Vector3 dragCanonicalOffset;
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
            internal bool startDrag;
            internal bool dragHoveredOnly;    // true, if only the element, that is hovered by the mouse should be moved instead of whole city
            internal bool drag;
            internal bool cancel;
            internal bool snap;
            internal bool reset;
            internal float zoomStepsDelta;
            internal bool zoomToggleToObject;
        }

        /// <summary>
        /// The cursor visually represents the center of all selected objects and is used
        /// for the center of rotations.
        /// </summary>
        [Tooltip("The cursor to visualize the center of the selection")]
        [SerializeField] private CityCursor cursor;

        /// <summary>
        /// The plane, the city is located on top of.
        /// </summary>
        private UnityEngine.Plane raycastPlane;

        /// <summary>
        /// Whether the city is currently moved or rotated by the player.
        /// </summary>
        private bool movingOrRotating;

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
            cursor = GetComponent<CityCursor>();
        }

        protected sealed override void OnCityAvailable()
        {
            raycastPlane = new UnityEngine.Plane(Vector3.up, CityTransform.position);
            movingOrRotating = false;

            moveState.moveGizmo = MoveGizmo.Create();
            moveState.cityBounds = CityTransform.GetComponent<Collider>().bounds;
            moveState.dragStartTransformPosition = Vector3.zero;
            moveState.dragStartOffset = Vector3.zero;
            moveState.dragCanonicalOffset = Vector3.zero;

            rotateState.rotateGizmo = RotateGizmo.Create(1024);
            rotateState.originalEulerAngleY = 0.0f;
            rotateState.originalPosition = Vector3.zero;
            rotateState.startAngle = 0.0f;
        }

        public sealed override void Update()
        {
            base.Update();

            if (CityTransform != null)
            {
                bool isMouseOverGUI = Raycasting.IsMouseOverGUI();

                // Fill action state with player input.
                // Note: Input MUST NOT be inquired in FixedUpdate() for the input to
                // feel responsive!
                actionState.drag = Input.GetMouseButton(2);
                actionState.startDrag |= !isMouseOverGUI && Input.GetMouseButtonDown(2);
                actionState.dragHoveredOnly = SEEInput.DragHovered();
                actionState.cancel |= SEEInput.Cancel();
                actionState.snap = SEEInput.Snap();
                actionState.reset |= (actionState.drag || !isMouseOverGUI) && SEEInput.Reset();

                // FIXME: The selection of graph elements below will executed only if the 
                // ray hits the clipping area. If the player looks at the city from aside,
                // hit nodes and edges will not selected because the ray may not hit the 
                // area. Large nodes and in particular edges above nodes tend to be high
                // in the sky and may not be selectable if the player views the city from
                // a suboptimal angle.
                RaycastClippingPlane(out bool _, out bool insideClippingArea, out Vector3 _);

                // Find hovered GameObject with node or edge, if it exists
                if (insideClippingArea)
                {
                    // For simplicity, zooming is only allowed if the city is not
                    // currently dragged
                    if (!isMouseOverGUI && !actionState.drag)
                    {
                        actionState.zoomStepsDelta += Input.mouseScrollDelta.y;
                    }
                }

                actionState.zoomToggleToObject = !actionState.drag && (actionState.zoomToggleToObject || SEEInput.ZoomInto());
                if (cursor.E != null && cursor.E.HasFocus())
                {
                    rotateState.rotateGizmo.Center = cursor.E.GetPosition();
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
            RaycastClippingPlane(out _, out bool insideClippingArea, out Vector3 planeHitPoint);

            #region Zoom

            if (actionState.zoomToggleToObject)
            {
                actionState.zoomToggleToObject = false;
                if (cursor.E.HasFocus())
                {
                    float optimalTargetZoomFactor = portalPlane.MinLengthXZ / (cursor.E.GetDiameterXZ() / zoomState.currentZoomFactor);
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
                        Vector2 centerOfTableAfterZoom = zoomSteps == -(int)zoomState.currentTargetZoomSteps ? CityTransform.position.XZ() : cursor.E.GetPosition().XZ();
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
                enabled = true;
            }
            else if (Equals(value, ActionStateType.Rotate))
            {
                moveState.moveGizmo?.gameObject.SetActive(false);
                enabled = true;
            }
            else
            {
                enabled = false;
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
        /// Raycasts against the clipping plane of the city ground.
        /// </summary>
        /// <param name="hitPlane">Whether the infinite plane was hit
        /// (<code>false</code>, if ray is parallel to plane).</param>
        /// <param name="insideClippingArea">Whether the plane was hit inside of its
        /// clipping area.</param>
        /// <param name="planeHitPoint">The point, the plane was hit by the ray.</param>
        private void RaycastClippingPlane(out bool hitPlane, out bool insideClippingArea, out Vector3 planeHitPoint)
        {
            Ray ray = MainCamera.Camera.ScreenPointToRay(Input.mousePosition);

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
