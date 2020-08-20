using System;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.Controls
{

    public class NavigationAction : MonoBehaviour
    {
        private enum NavigationMode
        {
            Move = 0,
            Rotate
        }

        private class ZoomCommand
        {
            public readonly int TargetZoomSteps;
            private readonly float duration;
            private readonly float startTime;

            internal ZoomCommand(int targetZoomSteps, float duration)
            {
                TargetZoomSteps = targetZoomSteps;
                this.duration = duration;
                startTime = Time.realtimeSinceStartup;
            }

            internal bool IsFinished()
            {
                bool result = Time.realtimeSinceStartup - startTime >= duration;
                return result;
            }

            internal float CurrentDeltaScale()
            {
                float x = Mathf.Min((Time.realtimeSinceStartup - startTime) / duration, 1.0f);
                float t = 0.5f - 0.5f * Mathf.Cos(x * Mathf.PI);
                float result = t * (float)TargetZoomSteps;
                return result;
            }
        }

        private struct MoveState
        {
            internal const float MaxVelocity = 10.0f;
            internal const float MaxSqrVelocity = MaxVelocity * MaxVelocity;
            internal const float SnapStepCount = 8;
            internal const float SnapStepAngle = 360.0f / SnapStepCount;
            internal const float DragFrictionFactor = 32.0f;

            internal UI3D.MoveGizmo moveGizmo;
            internal Bounds cityBounds;
            internal Vector3 dragStartTransformPosition;
            internal Vector3 dragStartOffset;
            internal Vector3 dragCanonicalOffset;
            internal Vector3 moveVelocity;
        }

        private struct RotateState
        {
            internal const float SnapStepCount = 8;
            internal const float SnapStepAngle = 360.0f / SnapStepCount;

            internal UI3D.RotateGizmo rotateGizmo;
            internal float originalEulerAngleY;
            internal Vector3 originalPosition;
            internal float startAngle;
        }

        private struct ZoomState
        {
            internal const float DefaultZoomDuration = 0.1f;
            internal const uint ZoomMaxSteps = 32;
            internal const float ZoomFactor = 0.5f;

            internal Vector3 originalScale;
            internal List<ZoomCommand> zoomCommands;
            internal uint currentTargetZoomSteps;
            internal float currentZoomFactor;
        }

        private struct CameraState
        {
            internal float distance;
            internal float yaw;
            internal float pitch;
        }

        private struct ActionState
        {
            internal bool startDrag;
            internal bool drag;
            internal bool cancel;
            internal bool snap;
            internal bool reset;
            internal Vector3 mousePosition; // TODO(torben): this needs to be abstracted for other modalities
            internal float zoomStepsDelta;
            internal bool zoomToggleToObject;
        }



        private Transform cityTransform;
        private Plane raycastPlane;

        private NavigationMode mode;
        private bool movingOrRotating;
        private UI3D.Cursor cursor;

        MoveState moveState;
        RotateState rotateState;
        ZoomState zoomState;
        CameraState cameraState;
        ActionState actionState;

        private void Start()
        {
            cityTransform = GameObject.Find("Implementation").transform.GetChild(0).transform; // TODO(torben): find it some more robust way
            zoomState.originalScale = cityTransform.localScale;
            moveState.cityBounds = cityTransform.GetComponent<MeshCollider>().bounds;
            raycastPlane = new Plane(Vector3.up, cityTransform.position);
            
            moveState.dragStartTransformPosition = cityTransform.position;
            moveState.dragCanonicalOffset = Vector3.zero;
            moveState.moveVelocity = Vector3.zero;
            moveState.moveGizmo = UI3D.MoveGizmo.Create(0.008f * Table.MinDimXZ);
            rotateState.rotateGizmo = UI3D.RotateGizmo.Create(1024);

            zoomState.zoomCommands = new List<ZoomCommand>((int)ZoomState.ZoomMaxSteps);
            zoomState.currentTargetZoomSteps = 0;
            zoomState.currentZoomFactor = 1.0f;

            cursor = UI3D.Cursor.Create();
            Select(cityTransform.gameObject);

            Camera.main.transform.position = Table.TableTopCenterEpsilon;
            cameraState.distance = 1.0f;
            cameraState.yaw = 0.0f;
            cameraState.pitch = 30.0f;
            Camera.main.transform.rotation = Quaternion.Euler(cameraState.pitch, cameraState.yaw, 0.0f);
            Camera.main.transform.position -= Camera.main.transform.forward * cameraState.distance;
        }

        private void Update()
        {
            // Input MUST NOT be inquired in FixedUpdate()!
            actionState.startDrag |= Input.GetMouseButtonDown(2);
            actionState.drag = Input.GetMouseButton(2);
            actionState.cancel |= Input.GetKeyDown(KeyCode.Escape);
            actionState.snap = Input.GetKey(KeyCode.LeftAlt);
            actionState.reset |= Input.GetKey(KeyCode.R);
            actionState.mousePosition = Input.mousePosition;
            if (!actionState.drag)
            {
                actionState.zoomStepsDelta += Input.mouseScrollDelta.y;
                actionState.zoomToggleToObject |= Input.GetKeyDown(KeyCode.F);
            }

            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                mode = NavigationMode.Move;
                movingOrRotating = false;
                rotateState.rotateGizmo.gameObject.SetActive(false);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                mode = NavigationMode.Rotate;
                movingOrRotating = false;
                moveState.moveGizmo.gameObject.SetActive(false);
            }

            rotateState.rotateGizmo.Radius = 0.2f * (Camera.main.transform.position - rotateState.rotateGizmo.Center).magnitude;

            if (!actionState.drag && Input.GetMouseButton(0))
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                RaycastHit[] hits = Physics.RaycastAll(ray);
                Array.Sort(hits, (h0, h1) => h0.distance.CompareTo(h1.distance));
                foreach (RaycastHit hit in hits)
                {
                    if (hit.transform.gameObject.GetComponent<GO.NodeRef>() != null)
                    {
                        Select(hit.transform.gameObject);
                        break;
                    }
                }
            }
            cursor.transform.position = cursor.Focus.position;
            rotateState.rotateGizmo.Center = cursor.transform.position;

            // Camera
            const float Speed = 2.0f; // TODO(torben): this is arbitrary
            float speed = Input.GetKey(KeyCode.LeftShift) ? 4.0f * Speed : Speed;
            if (Input.GetKey(KeyCode.W))
            {
                cameraState.distance -= speed * Time.deltaTime;
            }
            if (Input.GetKey(KeyCode.S))
            {
                cameraState.distance += speed * Time.deltaTime;
            }
            if (Input.GetMouseButton(1))
            {
                float x = Input.GetAxis("mouse x");
                float y = Input.GetAxis("mouse y");
                cameraState.yaw += x;
                cameraState.pitch -= y;
            }
            Camera.main.transform.position = Table.TableTopCenterEpsilon;
            Camera.main.transform.rotation = Quaternion.Euler(cameraState.pitch, cameraState.yaw, 0.0f);
            Camera.main.transform.position -= Camera.main.transform.forward * cameraState.distance;
        }

        // This logic is in FixedUpdate(), so that the behaviour is framerate-
        // 'independent'.
        private void FixedUpdate()
        {
            // TODO(torben): abstract mouse away!

            Ray ray = Camera.main.ScreenPointToRay(actionState.mousePosition);
            bool raycastResult = raycastPlane.Raycast(ray, out float enter);
            Vector3 planeHitPoint = ray.GetPoint(enter);

            #region Move

            if (mode == NavigationMode.Move)
            {
                if (actionState.reset) // reset to center of table
                {
                    actionState.reset = false;
                    movingOrRotating = false;
                    moveState.moveGizmo.gameObject.SetActive(false);

                    cityTransform.position = Table.TableTopCenterEpsilon;
                }
                else if (actionState.cancel) // cancel movement
                {
                    actionState.cancel = false;
                    if (movingOrRotating)
                    {
                        movingOrRotating = false;
                        moveState.moveGizmo.gameObject.SetActive(false);

                        moveState.moveVelocity = Vector3.zero;
                        cityTransform.position = moveState.dragStartTransformPosition + moveState.dragStartOffset - Vector3.Scale(moveState.dragCanonicalOffset, cityTransform.localScale);
                    }
                }
                else if (actionState.drag) // start or continue movement
                {
                    if (raycastResult)
                    {
                        if (actionState.startDrag) // start movement
                        {
                            actionState.startDrag = false;
                            movingOrRotating = true;
                            moveState.moveGizmo.gameObject.SetActive(true);

                            moveState.dragStartTransformPosition = cityTransform.position;
                            moveState.dragStartOffset = planeHitPoint - cityTransform.position;
                            moveState.dragCanonicalOffset = moveState.dragStartOffset.DividePairwise(cityTransform.localScale);
                            moveState.moveVelocity = Vector3.zero;
                        }
                        if (movingOrRotating) // continue movement
                        {
                            Vector3 totalDragOffsetFromStart = planeHitPoint - (moveState.dragStartTransformPosition + moveState.dragStartOffset);

                            if (actionState.snap)
                            {
                                totalDragOffsetFromStart = Project(totalDragOffsetFromStart);
                            }

                            Vector3 oldPosition = cityTransform.position;
                            Vector3 newPosition = moveState.dragStartTransformPosition + totalDragOffsetFromStart;

                            moveState.moveVelocity = (newPosition - oldPosition) / Time.fixedDeltaTime;
                            cityTransform.position = newPosition;
                            moveState.moveGizmo.SetPositions(moveState.dragStartTransformPosition + moveState.dragStartOffset, cityTransform.position + Vector3.Scale(moveState.dragCanonicalOffset, cityTransform.localScale));
                        }
                    }
                }
                else if (movingOrRotating) // finalize movement
                {
                    movingOrRotating = false;
                    moveState.moveGizmo.gameObject.SetActive(false);
                }
            }

            #endregion

            #region Rotate

            else // mode == NavigationMode.Rotate
            {
                if (actionState.reset) // reset rotation to identity();
                {
                    actionState.reset = false;
                    movingOrRotating = false;
                    rotateState.rotateGizmo.gameObject.SetActive(false);

                    cityTransform.RotateAround(rotateState.rotateGizmo.Center, Vector3.up, -cityTransform.rotation.eulerAngles.y);
                }
                else if (actionState.cancel) // cancel rotation
                {
                    actionState.cancel = false;
                    if (movingOrRotating)
                    {
                        movingOrRotating = false;
                        rotateState.rotateGizmo.gameObject.SetActive(false);

                        cityTransform.rotation = Quaternion.Euler(0.0f, rotateState.originalEulerAngleY, 0.0f);
                        cityTransform.position = rotateState.originalPosition;
                    }
                }
                else if (actionState.drag) // start or continue rotation
                {
                    if (raycastResult)
                    {
                        Vector2 toHit = new Vector2(planeHitPoint.x - rotateState.rotateGizmo.Center.x, planeHitPoint.z - rotateState.rotateGizmo.Center.z);
                        float toHitAngle = toHit.Angle360();

                        if (actionState.startDrag) // start rotation
                        {
                            actionState.startDrag = false;
                            movingOrRotating = true;
                            rotateState.rotateGizmo.gameObject.SetActive(true);

                            rotateState.originalEulerAngleY = cityTransform.rotation.eulerAngles.y;
                            rotateState.originalPosition = cityTransform.position;
                            rotateState.startAngle = AngleMod(cityTransform.rotation.eulerAngles.y - toHitAngle);
                            rotateState.rotateGizmo.SetMinAngle(Mathf.Deg2Rad * toHitAngle);
                            rotateState.rotateGizmo.SetMaxAngle(Mathf.Deg2Rad * toHitAngle);
                        }

                        if (movingOrRotating) // continue rotation
                        {
                            float angle = AngleMod(rotateState.startAngle + toHitAngle);
                            if (actionState.snap)
                            {
                                angle = AngleMod(Mathf.Round(angle / RotateState.SnapStepAngle) * RotateState.SnapStepAngle);
                            }
                            cityTransform.RotateAround(cursor.Focus.position, Vector3.up, angle - cityTransform.rotation.eulerAngles.y);

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
                                currAngle = Mathf.Round((currAngle + rotateState.startAngle) / (RotateState.SnapStepAngle)) * (RotateState.SnapStepAngle) - rotateState.startAngle;
                            }

                            rotateState.rotateGizmo.SetMaxAngle(Mathf.Deg2Rad * currAngle);
                        }
                    }
                }
                else if (movingOrRotating) // finalize rotation
                {
                    movingOrRotating = false;
                    rotateState.rotateGizmo.gameObject.SetActive(false);
                }
            }

            #endregion

            // TODO(torben): is it possible for the city to temporarily move very far
            // away, such that floating-point-errors may occur? that would happen above
            // where the city is initially moved.

            #region ApplyVelocityAndConstraints
            if (!movingOrRotating)
            {
                float sqrMag = moveState.moveVelocity.sqrMagnitude;
                if (sqrMag > MoveState.MaxSqrVelocity)
                {
                    moveState.moveVelocity = moveState.moveVelocity / Mathf.Sqrt(sqrMag) * MoveState.MaxVelocity;
                }
                cityTransform.position += moveState.moveVelocity * Time.fixedDeltaTime;

                float radius = 0.5f * cityTransform.lossyScale.x;
                bool outside = !MathExtensions.TestCircleRect(cityTransform.position.XZ(), radius, Table.MinXZ, Table.MaxXZ, out float sqrDistance);

                if (outside) // Keep city on visible area of the table
                {
                    moveState.moveVelocity = (Table.TableTopCenterEpsilon - cityTransform.position).normalized * MoveState.MaxVelocity;

                    if (zoomState.zoomCommands.Count == 0) // TODO(torben): why is this condition necessary?
                    {
                        Vector2 toCenter = (Table.CenterXZ - cityTransform.position.XZ()).normalized;
                        Vector2 dirX = new Vector2(1.0f, 0.0f); float dotX = Mathf.Abs(Vector2.Dot(dirX, toCenter));
                        Vector2 dirZ = new Vector2(0.0f, 1.0f); float dotZ = Mathf.Abs(Vector2.Dot(dirZ, toCenter));
                        float f = 1.0f;
                        if (dotX > dotZ)
                        {
                            f = (Mathf.Sqrt(sqrDistance) - radius) / dotX;
                        }
                        else
                        {
                            f = (Mathf.Sqrt(sqrDistance) - radius) / dotZ;
                        }
                        cityTransform.position += f * new Vector3(toCenter.x, 0.0f, toCenter.y);
                    }
                }
                else // Decelerate by adding friction
                {
                    Vector3 acceleration = MoveState.DragFrictionFactor * -moveState.moveVelocity;
                    moveState.moveVelocity += acceleration * Time.fixedDeltaTime;
                }
            }

            #endregion

            #region Zoom

            if (actionState.zoomToggleToObject)
            {
                actionState.zoomToggleToObject = false;
                if (cursor.Focus != null)
                {
                    float optimalZoomFactor = Table.MinDimXZ / (cursor.Focus.lossyScale.x / zoomState.currentZoomFactor);
                    float optimalZoomSteps = ConvertZoomFactorToZoomSteps(optimalZoomFactor);
                    int flooredZoomSteps = Mathf.FloorToInt(optimalZoomSteps);
                    int zoomSteps = flooredZoomSteps - (int)zoomState.currentTargetZoomSteps;
                    PushZoomCommand(zoomSteps != 0 ? zoomSteps : -(int)zoomState.currentTargetZoomSteps, 2.0f * ZoomState.DefaultZoomDuration);
                }
            }

            if (Mathf.Abs(actionState.zoomStepsDelta) >= 1.0f)
            {
                float fZoomSteps = actionState.zoomStepsDelta >= 0.0f ? Mathf.Floor(actionState.zoomStepsDelta) : Mathf.Ceil(actionState.zoomStepsDelta);
                actionState.zoomStepsDelta -= fZoomSteps;
                int zoomSteps = Mathf.Clamp((int)fZoomSteps, -(int)zoomState.currentTargetZoomSteps, (int)ZoomState.ZoomMaxSteps - (int)zoomState.currentTargetZoomSteps);
                PushZoomCommand(zoomSteps);
            }

            if (zoomState.zoomCommands.Count != 0)
            {
                float zoomSteps = (float)zoomState.currentTargetZoomSteps;

                for (int i = 0; i < zoomState.zoomCommands.Count; i++)
                {
                    if (zoomState.zoomCommands[i].IsFinished())
                    {
                        zoomState.zoomCommands.RemoveAt(i--);
                    }
                    else
                    {
                        zoomSteps -= zoomState.zoomCommands[i].TargetZoomSteps - zoomState.zoomCommands[i].CurrentDeltaScale();
                    }
                }

                zoomState.currentZoomFactor = ConvertZoomStepsToZoomFactor(zoomSteps);
                Vector3 cityCenterToHitPoint = planeHitPoint - cityTransform.position;
                Vector3 cityCenterToHitPointUnscaled = cityCenterToHitPoint.DividePairwise(cityTransform.localScale);

                cityTransform.position += cityCenterToHitPoint;
                cityTransform.localScale = zoomState.currentZoomFactor * zoomState.originalScale;
                cityTransform.position -= Vector3.Scale(cityCenterToHitPointUnscaled, cityTransform.localScale);

                moveState.dragStartTransformPosition += moveState.dragStartOffset;
                moveState.dragStartOffset = Vector3.Scale(moveState.dragCanonicalOffset, cityTransform.localScale);
                moveState.dragStartTransformPosition -= moveState.dragStartOffset;
            }

            #endregion
        }

        private float ConvertZoomStepsToZoomFactor(float zoomSteps)
        {
            float result = Mathf.Pow(2, zoomSteps * ZoomState.ZoomFactor);
            return result;
        }

        private float ConvertZoomFactorToZoomSteps(float zoomFactor)
        {
            float result = Mathf.Log(zoomFactor, 2) / ZoomState.ZoomFactor;
            return result;
        }

        private void PushZoomCommand(int zoomSteps = 1, float duration = ZoomState.DefaultZoomDuration)
        {
            if (zoomSteps != 0)
            {
                uint newZoomStepsInProgress = (uint)((int)zoomState.currentTargetZoomSteps + zoomSteps);
                if (duration != 0)
                {
                    zoomState.zoomCommands.Add(new ZoomCommand(zoomSteps, duration));
                }
                zoomState.currentTargetZoomSteps = newZoomStepsInProgress;
            }
        }

        private float AngleMod(float degrees)
        {
            float result = ((degrees % 360.0f) + 360.0f) % 360.0f;
            return result;
        }

        private Vector3 Project(Vector3 offset)
        {
            Vector2 point2 = new Vector2(offset.x, offset.z);
            float angleDeg = point2.Angle360();
            float snappedAngleDeg = Mathf.Round(angleDeg / MoveState.SnapStepAngle) * MoveState.SnapStepAngle;
            float snappedAngleRad = Mathf.Deg2Rad * snappedAngleDeg;
            Vector2 dir = new Vector2(Mathf.Cos(snappedAngleRad), Mathf.Sin(-snappedAngleRad));
            Vector2 proj = dir * Vector2.Dot(point2, dir);
            Vector3 result = new Vector3(proj.x, offset.y, proj.y);
            return result;
        }

        private void Select(GameObject go)
        {
            if (go != null && go.transform != cursor.Focus)
            {
                if (cursor.Focus)
                {
                    Destroy(cursor.Focus.gameObject.GetComponent<Outline>());
                }
                cursor.Focus = go.transform;
                Outline outline = go.transform.gameObject.AddComponent<Outline>();
                outline.OutlineMode = Outline.Mode.OutlineAll;
                outline.OutlineColor = UI3D.UI3DProperties.DefaultColor;
                outline.OutlineWidth = 4.0f;
            }
        }
    }
}
