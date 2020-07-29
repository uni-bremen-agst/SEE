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

        private struct CameraState
        {
            public float distance;
            public float yaw;
            public float pitch;
        }

        private const float MaxVelocity = 10.0f;
        private const float MaxSqrVelocity = MaxVelocity * MaxVelocity;

        private const float MaxDistanceX = 1.2f * Table.Width;
        private const float MaxSqrDistanceX = MaxDistanceX * MaxDistanceX;
        private const float MaxDistanceZ = 1.2f * Table.Depth;
        private const float MaxSqrDistanceZ = MaxDistanceZ * MaxDistanceZ;

        private const float SnapStepCount = 8;
        private const float SnapStepAngle = 360.0f / SnapStepCount;
        private const float DragFrictionFactor = 32.0f;

        private const float ZoomDuration = 0.1f;
        private const uint ZoomMaxSteps = 32;
        private const float ZoomFactor = 0.5f;



        private Transform cityTransform;
        private Vector3 originalScale;
        private Bounds cityBounds;
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
        private Vector3 dragStartTransformPosition;
        private Vector3 dragStartOffset;
        private Vector3 dragCanonicalOffset;
        private Vector3 moveVelocity;
        private MovePivotBase movePivot;
        private RotatePivot rotatePivot;

        private float originalEulerAngleY;
        private float startAngle;

        private List<ZoomCommand> zoomCommands;
        private uint zoomStepsInProgress;

        private Cursor cursor;

        // Camera
        CameraState cameraState;

        private void Start()
        {
            cityTransform = GameObject.Find("Implementation").transform.GetChild(0).transform; // TODO(torben): find it some more robust way
            originalScale = cityTransform.localScale;
            cityBounds = cityTransform.GetComponent<MeshCollider>().bounds;
            raycastPlane = new Plane(Vector3.up, cityTransform.position);
            
            dragStartTransformPosition = cityTransform.position;
            dragCanonicalOffset = Vector3.zero;
            moveVelocity = Vector3.zero;
            movePivot = new LineMovePivot(0.008f * Table.MinDimXZ);
            rotatePivot = new RotatePivot(1024);

            zoomCommands = new List<ZoomCommand>((int)ZoomMaxSteps);
            zoomStepsInProgress = 0;



            cursor = Cursor.Create(cityTransform);






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
                rotatePivot.Enable(false);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                mode = NavigationMode.Rotate;
                movingOrRotating = false;
                movePivot.Enable(false);
            }

            rotatePivot.Radius = 0.2f * (Camera.main.transform.position - rotatePivot.Center).magnitude;

            if (Input.GetMouseButton(0))
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                RaycastHit[] hits = Physics.RaycastAll(ray);
                Array.Sort(hits, (h0, h1) => h0.distance.CompareTo(h1.distance));
                foreach (RaycastHit hit in hits)
                {
                    if (hit.transform.gameObject.GetComponent<GO.NodeRef>() != null)
                    {
                        cursor.Focus = hit.transform;
                        break;
                    }
                }
            }
            cursor.transform.position = cursor.Focus.position;
            rotatePivot.Center = cursor.transform.position;

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
                    movePivot.Enable(false);

                    cityTransform.position = Table.TableTopCenterEpsilon;
                }
                else if (cancel) // cancel movement
                {
                    cancel = false;
                    if (movingOrRotating)
                    {
                        movingOrRotating = false;
                        movePivot.Enable(false);

                        moveVelocity = Vector3.zero;
                        cityTransform.position = dragStartTransformPosition + dragStartOffset - Vector3.Scale(dragCanonicalOffset, cityTransform.localScale);
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
                            movePivot.Enable(true);

                            dragStartTransformPosition = cityTransform.position;
                            dragStartOffset = planeHitPoint - cityTransform.position;
                            dragCanonicalOffset = dragStartOffset.DividePairwise(cityTransform.localScale);
                            moveVelocity = Vector3.zero;
                        }
                        if (movingOrRotating) // continue movement
                        {
                            Vector3 totalDragOffsetFromStart = planeHitPoint - (dragStartTransformPosition + dragStartOffset);

                            if (snap)
                            {
                                totalDragOffsetFromStart = Project(totalDragOffsetFromStart);
                            }

                            Vector3 oldPosition = cityTransform.position;
                            Vector3 newPosition = dragStartTransformPosition + totalDragOffsetFromStart;

                            moveVelocity = (newPosition - oldPosition) / Time.fixedDeltaTime;
                            cityTransform.position = newPosition;
                            movePivot.SetPositions(dragStartTransformPosition + dragStartOffset, cityTransform.position + Vector3.Scale(dragCanonicalOffset, cityTransform.localScale));
                        }
                    }
                }
                else if (movingOrRotating) // finalize movement
                {
                    movingOrRotating = false;
                    movePivot.Enable(false);
                }
            }
            else // mode == NavigationMode.Rotate
            {
                if (reset) // reset rotation to identity();
                {
                    reset = false;
                    movingOrRotating = false;
                    rotatePivot.Enable(false);

                    cityTransform.rotation = Quaternion.identity;
                }
                else if (cancel) // cancel rotation
                {
                    cancel = false;
                    if (movingOrRotating)
                    {
                        movingOrRotating = false;
                        rotatePivot.Enable(false);

                        cityTransform.rotation = Quaternion.Euler(0.0f, originalEulerAngleY, 0.0f);
                    }
                }
                else if (drag) // start or continue rotation
                {
                    if (raycastResult)
                    {
                        Vector2 toHit = new Vector2(planeHitPoint.x - rotatePivot.Center.x, planeHitPoint.z - rotatePivot.Center.z);
                        float toHitAngle = toHit.Angle360();

                        if (startDrag) // start rotation
                        {
                            startDrag = false;
                            movingOrRotating = true;
                            rotatePivot.Enable(true);

                            originalEulerAngleY = cityTransform.rotation.eulerAngles.y;
                            startAngle = AngleMod(cityTransform.rotation.eulerAngles.y - toHitAngle);
                            rotatePivot.SetMinAngle(Mathf.Deg2Rad * toHitAngle);
                            rotatePivot.SetMaxAngle(Mathf.Deg2Rad * toHitAngle);
                        }

                        if (movingOrRotating) // continue rotation
                        {
                            float angle = AngleMod(startAngle + toHitAngle);
                    
                            if (snap)
                            {
                                angle = AngleMod(Mathf.Round(angle / SnapStepAngle) * SnapStepAngle);
                            }

                            Vector3 pre = cursor.Focus.position;
                            cityTransform.rotation = Quaternion.Euler(0.0f, angle, 0.0f);
                            Vector3 post = cursor.Focus.position;
                            cityTransform.position += pre - post;

                            float prevAngle = Mathf.Rad2Deg * rotatePivot.GetMaxAngle();
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
                                currAngle = Mathf.Round((currAngle + startAngle) / (SnapStepAngle)) * (SnapStepAngle) - startAngle;
                            }

                            rotatePivot.SetMaxAngle(Mathf.Deg2Rad * currAngle);
                        }
                    }
                }
                else if (movingOrRotating) // finalize rotation
                {
                    movingOrRotating = false;
                    rotatePivot.Enable(false);
                }
            }

            if (!movingOrRotating)
            {
                Vector3 acceleration = Vector3.zero;

                // TODO(torben): this whole thing currently assumes the shape of a quad!
                // therefore, circular cities can be lost in corners of the table!
                float cityMinX = cityTransform.position.x + (cityTransform.localScale.x * cityBounds.min.x);
                float cityMaxX = cityTransform.position.x + (cityTransform.localScale.x * cityBounds.max.x);
                float cityMinZ = cityTransform.position.z + (cityTransform.localScale.z * cityBounds.min.z);
                float cityMaxZ = cityTransform.position.z + (cityTransform.localScale.z * cityBounds.max.z);

                if (cityMaxX < Table.MinX || cityMaxZ < Table.MinZ || cityMinX > Table.MaxX || cityMinZ > Table.MaxZ)
                {
                    float toTableCenterX = Table.CenterX - cityTransform.position.x;
                    float toTableCenterZ = Table.CenterZ - cityTransform.position.z;
                    float length = Mathf.Sqrt(toTableCenterX * toTableCenterX + toTableCenterZ * toTableCenterZ);
                    toTableCenterX /= length;
                    toTableCenterZ /= length;
                    acceleration = new Vector3(32.0f * toTableCenterX, 0.0f, 32.0f * toTableCenterZ);
                }
                else
                {
                    acceleration = DragFrictionFactor * -moveVelocity;
                }
                moveVelocity += acceleration * Time.fixedDeltaTime;

                float dragVelocitySqrMag = moveVelocity.sqrMagnitude;
                if (dragVelocitySqrMag > MaxSqrVelocity)
                {
                    moveVelocity = moveVelocity / Mathf.Sqrt(dragVelocitySqrMag) * MaxVelocity;
                }
                cityTransform.position += moveVelocity * Time.fixedDeltaTime;
            }

            // Keep city close to table

            // TODO(torben): is it possible for the city to temporarily move very far
            // away, such that floating-point-errors may occur? that would happen above
            // where the city is initially moved.
            if (!movingOrRotating && zoomCommands.Count == 0)
            {
                // TODO(torben): similar TODO as above with circular cities!
                float tableToCityCenterX = cityTransform.position.x - Table.CenterX;
                float tableToCityCenterZ = cityTransform.position.z - Table.CenterZ;
                float distance = Mathf.Sqrt(tableToCityCenterX * tableToCityCenterX + tableToCityCenterZ * tableToCityCenterZ);
                float maxDistance = Mathf.Max(cityTransform.localScale.x * MaxDistanceX, cityTransform.localScale.z * MaxDistanceZ);
                if (distance > maxDistance)
                {
                    float offsetX = tableToCityCenterX / distance * maxDistance;
                    float offsetZ = tableToCityCenterZ / distance * maxDistance;
                    cityTransform.position = new Vector3(Table.CenterX + offsetX, cityTransform.position.y, Table.CenterZ + offsetZ);
                }
            }



            // Zoom into city
            int zoomSteps = Mathf.RoundToInt(mouseScrollDelta);
            mouseScrollDelta = 0.0f;
            int newZoomStepsInProgress = (int)zoomStepsInProgress + zoomSteps;

            if (zoomSteps != 0 && newZoomStepsInProgress >= 0 && newZoomStepsInProgress <= ZoomMaxSteps)
            {
                zoomCommands.Add(new ZoomCommand(zoomSteps, ZoomDuration));
                zoomStepsInProgress = (uint)newZoomStepsInProgress;
            }

            if (zoomCommands.Count != 0)
            {
                float currentZoomSteps = (float)zoomStepsInProgress;

                for (int i = 0; i < zoomCommands.Count; i++)
                {
                    if (zoomCommands[i].IsFinished())
                    {
                        zoomCommands.RemoveAt(i--);
                    }
                    else
                    {
                        currentZoomSteps = currentZoomSteps - zoomCommands[i].targetZoomSteps + zoomCommands[i].CurrentDeltaScale();
                    }
                }

                float f = Mathf.Pow(2, currentZoomSteps * ZoomFactor);
                Vector3 cityCenterToHitPoint = planeHitPoint - cityTransform.position;
                Vector3 cityCenterToHitPointUnscaled = cityCenterToHitPoint.DividePairwise(cityTransform.localScale);

                cityTransform.position += cityCenterToHitPoint;
                cityTransform.localScale = f * originalScale;
                cityTransform.position -= Vector3.Scale(cityCenterToHitPointUnscaled, cityTransform.localScale);

                dragStartTransformPosition += dragStartOffset;
                dragStartOffset = Vector3.Scale(dragCanonicalOffset, cityTransform.localScale);
                dragStartTransformPosition -= dragStartOffset;
            }
        }

        private Vector3 Project(Vector3 offset)
        {
            Vector2 point2 = new Vector2(offset.x, offset.z);
            float angleDeg = point2.Angle360();
            float snappedAngleDeg = Mathf.Round(angleDeg / SnapStepAngle) * SnapStepAngle;
            float snappedAngleRad = Mathf.Deg2Rad * snappedAngleDeg;
            Vector2 dir = new Vector2(Mathf.Cos(snappedAngleRad), Mathf.Sin(-snappedAngleRad));
            Vector2 proj = dir * Vector2.Dot(point2, dir);
            Vector3 result = new Vector3(proj.x, offset.y, proj.y);
            return result;
        }

        private float AngleMod(float degrees) => ((degrees % 360.0f) + 360.0f) % 360.0f;
    }
}
