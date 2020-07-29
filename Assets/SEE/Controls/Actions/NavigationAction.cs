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
            internal readonly int targetZoomSteps;

            private readonly float duration;
            private readonly float startTime;

            internal ZoomCommand(int targetZoomSteps, float duration)
            {
                this.targetZoomSteps = targetZoomSteps;
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
                float result = t * (float)targetZoomSteps;
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

            internal MovePivotBase movePivot;
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

            internal RotatePivot rotatePivot;
            internal float originalEulerAngleY;
            internal Vector3 originalPosition;
            internal float startAngle;
        }

        private struct ZoomState
        {
            internal const float ZoomDuration = 0.1f;
            internal const uint ZoomMaxSteps = 32;
            internal const float ZoomFactor = 0.5f;

            internal Vector3 originalScale;
            internal List<ZoomCommand> zoomCommands;
            internal uint zoomStepsInProgress;
        }

        private struct CameraState
        {
            internal float distance;
            internal float yaw;
            internal float pitch;
        }



        private Transform cityTransform;
        private Plane raycastPlane;

        // Buttons
        private bool startDrag;
        private bool drag;
        private bool cancel;
        private bool snap;
        private bool reset;
        private Vector3 mousePosition;
        private float mouseScrollDelta;

        private NavigationMode mode;
        private bool movingOrRotating;
        private Cursor cursor;

        MoveState moveState;
        RotateState rotateState;
        ZoomState zoomState;
        CameraState cameraState;

        private void Start()
        {
            cityTransform = GameObject.Find("Implementation").transform.GetChild(0).transform; // TODO(torben): find it some more robust way
            zoomState.originalScale = cityTransform.localScale;
            moveState.cityBounds = cityTransform.GetComponent<MeshCollider>().bounds;
            raycastPlane = new Plane(Vector3.up, cityTransform.position);
            
            moveState.dragStartTransformPosition = cityTransform.position;
            moveState.dragCanonicalOffset = Vector3.zero;
            moveState.moveVelocity = Vector3.zero;
            moveState.movePivot = new LineMovePivot(0.008f * Table.MinDimXZ);
            rotateState.rotatePivot = new RotatePivot(1024);

            zoomState.zoomCommands = new List<ZoomCommand>((int)ZoomState.ZoomMaxSteps);
            zoomState.zoomStepsInProgress = 0;

            cursor = Cursor.Create();
            Select(cityTransform.gameObject);

            Camera.main.transform.position = Table.TableTopCenterEpsilon;
            cameraState.distance = 1.0f;
            cameraState.yaw = 0.0f;
            cameraState.pitch = 30.0f;
            Camera.main.transform.rotation = Quaternion.Euler(cameraState.pitch, cameraState.yaw, 0.0f);
            Camera.main.transform.position -= Camera.main.transform.forward * cameraState.distance;
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
                outline.OutlineColor = new Color(1.0f, 0.25f, 0.0f, 1.0f);
                outline.OutlineWidth = 4.0f;
            }
        }

        private void Update()
        {
            // Input MUST NOT be inquired in FixedUpdate()!
            startDrag |= Input.GetMouseButtonDown(2);
            drag = Input.GetMouseButton(2);
            cancel |= Input.GetKeyDown(KeyCode.Escape);
            snap = Input.GetKey(KeyCode.LeftAlt);
            reset |= Input.GetKey(KeyCode.R);
            mouseScrollDelta += Input.mouseScrollDelta.y;
            mousePosition = Input.mousePosition;

            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                mode = NavigationMode.Move;
                movingOrRotating = false;
                rotateState.rotatePivot.Enable(false);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                mode = NavigationMode.Rotate;
                movingOrRotating = false;
                moveState.movePivot.Enable(false);
            }

            rotateState.rotatePivot.Radius = 0.2f * (Camera.main.transform.position - rotateState.rotatePivot.Center).magnitude;

            if (Input.GetMouseButton(0))
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
            rotateState.rotatePivot.Center = cursor.transform.position;

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

            Ray ray = Camera.main.ScreenPointToRay(mousePosition);
            bool raycastResult = raycastPlane.Raycast(ray, out float enter);
            Vector3 planeHitPoint = ray.GetPoint(enter);

            if (mode == NavigationMode.Move)
            {
                if (reset) // reset to center of table
                {
                    reset = false;
                    movingOrRotating = false;
                    moveState.movePivot.Enable(false);

                    cityTransform.position = Table.TableTopCenterEpsilon;
                }
                else if (cancel) // cancel movement
                {
                    cancel = false;
                    if (movingOrRotating)
                    {
                        movingOrRotating = false;
                        moveState.movePivot.Enable(false);

                        moveState.moveVelocity = Vector3.zero;
                        cityTransform.position = moveState.dragStartTransformPosition + moveState.dragStartOffset - Vector3.Scale(moveState.dragCanonicalOffset, cityTransform.localScale);
                    }
                }
                else if (drag) // start or continue movement
                {
                    if (raycastResult)
                    {
                        if (startDrag) // start movement
                        {
                            startDrag = false;
                            movingOrRotating = true;
                            moveState.movePivot.Enable(true);

                            moveState.dragStartTransformPosition = cityTransform.position;
                            moveState.dragStartOffset = planeHitPoint - cityTransform.position;
                            moveState.dragCanonicalOffset = moveState.dragStartOffset.DividePairwise(cityTransform.localScale);
                            moveState.moveVelocity = Vector3.zero;
                        }
                        if (movingOrRotating) // continue movement
                        {
                            Vector3 totalDragOffsetFromStart = planeHitPoint - (moveState.dragStartTransformPosition + moveState.dragStartOffset);

                            if (snap)
                            {
                                totalDragOffsetFromStart = Project(totalDragOffsetFromStart);
                            }

                            Vector3 oldPosition = cityTransform.position;
                            Vector3 newPosition = moveState.dragStartTransformPosition + totalDragOffsetFromStart;

                            moveState.moveVelocity = (newPosition - oldPosition) / Time.fixedDeltaTime;
                            cityTransform.position = newPosition;
                            moveState.movePivot.SetPositions(moveState.dragStartTransformPosition + moveState.dragStartOffset, cityTransform.position + Vector3.Scale(moveState.dragCanonicalOffset, cityTransform.localScale));
                        }
                    }
                }
                else if (movingOrRotating) // finalize movement
                {
                    movingOrRotating = false;
                    moveState.movePivot.Enable(false);
                }
            }
            else // mode == NavigationMode.Rotate
            {
                if (reset) // reset rotation to identity();
                {
                    reset = false;
                    movingOrRotating = false;
                    rotateState.rotatePivot.Enable(false);

                    cityTransform.RotateAround(rotateState.rotatePivot.Center, Vector3.up, -cityTransform.rotation.eulerAngles.y);
                }
                else if (cancel) // cancel rotation
                {
                    cancel = false;
                    if (movingOrRotating)
                    {
                        movingOrRotating = false;
                        rotateState.rotatePivot.Enable(false);

                        cityTransform.rotation = Quaternion.Euler(0.0f, rotateState.originalEulerAngleY, 0.0f);
                        cityTransform.position = rotateState.originalPosition;
                    }
                }
                else if (drag) // start or continue rotation
                {
                    if (raycastResult)
                    {
                        Vector2 toHit = new Vector2(planeHitPoint.x - rotateState.rotatePivot.Center.x, planeHitPoint.z - rotateState.rotatePivot.Center.z);
                        float toHitAngle = toHit.Angle360();

                        if (startDrag) // start rotation
                        {
                            startDrag = false;
                            movingOrRotating = true;
                            rotateState.rotatePivot.Enable(true);

                            rotateState.originalEulerAngleY = cityTransform.rotation.eulerAngles.y;
                            rotateState.originalPosition = cityTransform.position;
                            rotateState.startAngle = AngleMod(cityTransform.rotation.eulerAngles.y - toHitAngle);
                            rotateState.rotatePivot.SetMinAngle(Mathf.Deg2Rad * toHitAngle);
                            rotateState.rotatePivot.SetMaxAngle(Mathf.Deg2Rad * toHitAngle);
                        }

                        if (movingOrRotating) // continue rotation
                        {
                            float angle = AngleMod(rotateState.startAngle + toHitAngle);
                            if (snap)
                            {
                                angle = AngleMod(Mathf.Round(angle / RotateState.SnapStepAngle) * RotateState.SnapStepAngle);
                            }
                            cityTransform.RotateAround(cursor.Focus.position, Vector3.up, angle - cityTransform.rotation.eulerAngles.y);

                            float prevAngle = Mathf.Rad2Deg * rotateState.rotatePivot.GetMaxAngle();
                            float currAngle = toHitAngle;

                            while (Mathf.Abs(currAngle + 360.0f - prevAngle) < Mathf.Abs(currAngle - prevAngle))
                            {
                                currAngle += 360.0f;
                            }
                            while (Mathf.Abs(currAngle - 360.0f - prevAngle) < Mathf.Abs(currAngle - prevAngle))
                            {
                                currAngle -= 360.0f;
                            }
                            if (snap)
                            {
                                currAngle = Mathf.Round((currAngle + rotateState.startAngle) / (RotateState.SnapStepAngle)) * (RotateState.SnapStepAngle) - rotateState.startAngle;
                            }

                            rotateState.rotatePivot.SetMaxAngle(Mathf.Deg2Rad * currAngle);
                        }
                    }
                }
                else if (movingOrRotating) // finalize rotation
                {
                    movingOrRotating = false;
                    rotateState.rotatePivot.Enable(false);
                }
            }

            // TODO(torben): is it possible for the city to temporarily move very far
            // away, such that floating-point-errors may occur? that would happen above
            // where the city is initially moved.

            if (!movingOrRotating)
            {
                float sqrMag = moveState.moveVelocity.sqrMagnitude;
                if (sqrMag > MoveState.MaxSqrVelocity)
                {
                    moveState.moveVelocity = moveState.moveVelocity / Mathf.Sqrt(sqrMag) * MoveState.MaxVelocity;
                }
                cityTransform.position += moveState.moveVelocity * Time.fixedDeltaTime;

                float radius = 0.5f * cityTransform.lossyScale.x;
                bool outside = !TestCircleRect(cityTransform.position.XZ(), radius, Table.MinXZ, Table.MaxXZ, out float sqrDistance);

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



            // Zoom into city
            int zoomSteps = Mathf.RoundToInt(mouseScrollDelta);
            mouseScrollDelta = 0.0f;
            int newZoomStepsInProgress = (int)zoomState.zoomStepsInProgress + zoomSteps;

            if (zoomSteps != 0 && newZoomStepsInProgress >= 0 && newZoomStepsInProgress <= ZoomState.ZoomMaxSteps)
            {
                zoomState.zoomCommands.Add(new ZoomCommand(zoomSteps, ZoomState.ZoomDuration));
                zoomState.zoomStepsInProgress = (uint)newZoomStepsInProgress;
            }

            if (zoomState.zoomCommands.Count != 0)
            {
                float currentZoomSteps = (float)zoomState.zoomStepsInProgress;

                for (int i = 0; i < zoomState.zoomCommands.Count; i++)
                {
                    if (zoomState.zoomCommands[i].IsFinished())
                    {
                        zoomState.zoomCommands.RemoveAt(i--);
                    }
                    else
                    {
                        currentZoomSteps = currentZoomSteps - zoomState.zoomCommands[i].targetZoomSteps + zoomState.zoomCommands[i].CurrentDeltaScale();
                    }
                }

                float f = Mathf.Pow(2, currentZoomSteps * ZoomState.ZoomFactor);
                Vector3 cityCenterToHitPoint = planeHitPoint - cityTransform.position;
                Vector3 cityCenterToHitPointUnscaled = cityCenterToHitPoint.DividePairwise(cityTransform.localScale);

                cityTransform.position += cityCenterToHitPoint;
                cityTransform.localScale = f * zoomState.originalScale;
                cityTransform.position -= Vector3.Scale(cityCenterToHitPointUnscaled, cityTransform.localScale);

                moveState.dragStartTransformPosition += moveState.dragStartOffset;
                moveState.dragStartOffset = Vector3.Scale(moveState.dragCanonicalOffset, cityTransform.localScale);
                moveState.dragStartTransformPosition -= moveState.dragStartOffset;
            }
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

        private bool TestCircleRect(Vector2 center, float radius, Vector2 min, Vector2 max, out float sqrDistance)
        {
            float SquaredDistanceVector2Rect()
            {
                float sqDist = 0.0f;
                for (int i = 0; i < 2; i++)
                {
                    float v = center[i];
                    if (v < min[i]) sqDist += (min[i] - v) * (min[i] - v);
                    if (v > max[i]) sqDist += (v - max[i]) * (v - max[i]);
                }
                return sqDist;
            }
            sqrDistance = SquaredDistanceVector2Rect();
            return sqrDistance <= radius * radius;
        }

        private float AngleMod(float degrees) => ((degrees % 360.0f) + 360.0f) % 360.0f;
    }
}
