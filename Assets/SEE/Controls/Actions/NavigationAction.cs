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

        private const float MaxVelocity = 10.0f;
        private const float MaxSqrVelocity = MaxVelocity * MaxVelocity;

        private const float MaxDistanceX = 1.2f * Table.Width;
        private const float MaxSqrDistanceX = MaxDistanceX * MaxDistanceX;
        private const float MaxDistanceZ = 1.2f * Table.Depth;
        private const float MaxSqrDistanceZ = MaxDistanceZ * MaxDistanceZ;

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
        private bool lockAxisButton;
        private Vector3 mousePosition;
        private float mouseScrollDelta;

        private NavigationMode mode;
        private bool movingOrRotating;
        private Vector3 dragStartTransformPosition;
        private Vector3 dragStartOffset;
        private Vector3 dragCanonicalOffset;
        private Vector3 moveVelocity;
        private PivotBase pivot;

        private float startAngleDeg;

        private List<ZoomCommand> zoomCommands;
        private uint zoomStepsInProgress;



        private void Start()
        {
            cityTransform = GameObject.Find("Implementation").transform.GetChild(0).transform; // TODO(torben): find it some more robust way
            originalScale = cityTransform.localScale;
            cityBounds = cityTransform.GetComponent<MeshCollider>().bounds;
            raycastPlane = new Plane(Vector3.up, cityTransform.position);

            movingOrRotating = false;
            dragStartTransformPosition = cityTransform.position;
            dragCanonicalOffset = Vector3.zero;
            moveVelocity = Vector3.zero;
            pivot = new LinePivot(0.008f * Table.MinDimXZ);

            zoomCommands = new List<ZoomCommand>((int)ZoomMaxSteps);
            zoomStepsInProgress = 0;
        }

        private void Update()
        {
            // Input MUST NOT be inquired in FixedUpdate()!

            startDrag |= Input.GetMouseButtonDown(2);
            drag = Input.GetMouseButton(2);
            cancel |= Input.GetKeyDown(KeyCode.Escape);
            lockAxisButton = Input.GetKey(KeyCode.LeftAlt);
            mouseScrollDelta = Input.mouseScrollDelta.y;
            mousePosition = Input.mousePosition;

            if (Input.GetKeyDown(KeyCode.M))
            {
                mode = NavigationMode.Move;
                movingOrRotating = false;
                pivot.Enable(false);
            }
            else if (Input.GetKeyDown(KeyCode.R))
            {
                mode = NavigationMode.Rotate;
                movingOrRotating = false;
                pivot.Enable(false);
            }
        }

        private void FixedUpdate()
        {
            // TODO(torben): abstract mouse away!

            Ray ray = Camera.main.ScreenPointToRay(mousePosition);
            bool raycastResult = raycastPlane.Raycast(ray, out float enter);
            Vector3 planeHitPoint = ray.GetPoint(enter);

            if (mode == NavigationMode.Move)
            {
                if (cancel)
                {
                    cancel = false;
                    if (movingOrRotating)
                    {
                        movingOrRotating = false;
                        moveVelocity = Vector3.zero;
                        pivot.Enable(false);
                        cityTransform.position = dragStartTransformPosition + dragStartOffset - Vector3.Scale(dragCanonicalOffset, cityTransform.localScale);
                    }
                }
                else if (drag)
                {
                    if (raycastResult)
                    {
                        if (!movingOrRotating && startDrag)
                        {
                            startDrag = false;
                            movingOrRotating = true;
                            dragStartTransformPosition = cityTransform.position;
                            dragStartOffset = planeHitPoint - cityTransform.position;
                            dragCanonicalOffset = dragStartOffset.DividePairwise(cityTransform.localScale);
                            moveVelocity = Vector3.zero;
                            pivot.Enable(true);
                        }
                        if (movingOrRotating)
                        {
                            Vector3 totalDragOffsetFromStart = planeHitPoint - (dragStartTransformPosition + dragStartOffset);

                            Vector3 axisMask = Vector3.one;
                            if (lockAxisButton)
                            {
                                float absX = Mathf.Abs(totalDragOffsetFromStart.x);
                                float absY = Mathf.Abs(totalDragOffsetFromStart.y);
                                float absZ = Mathf.Abs(totalDragOffsetFromStart.z);

                                if (absX < absY || absX < absZ)
                                {
                                    axisMask.x = 0.0f;
                                }
                                if (absY < absX || absY < absZ)
                                {
                                    axisMask.y = 0.0f;
                                }
                                if (absZ < absX || absZ < absY)
                                {
                                    axisMask.z = 0.0f;
                                }
                            }

                            Vector3 oldPosition = cityTransform.position;
                            Vector3 newPosition = dragStartTransformPosition + Vector3.Scale(totalDragOffsetFromStart, axisMask);

                            moveVelocity = (newPosition - oldPosition) / Time.fixedDeltaTime;
                            cityTransform.position = newPosition;
                            pivot.SetPositions(dragStartTransformPosition + dragStartOffset, cityTransform.position + Vector3.Scale(dragCanonicalOffset, cityTransform.localScale));
                        }
                    }
                }
                else if (movingOrRotating)
                {
                    movingOrRotating = false;
                    pivot.Enable(false);
                }
            }
            else // mode = Mode.Rotate
            {
                if (drag)
                {
                    Vector2 toHit = new Vector2(planeHitPoint.x - cityTransform.position.x, planeHitPoint.z - cityTransform.position.z);
                    if (startDrag)
                    {
                        startDrag = false;
                        movingOrRotating = true;
                        startAngleDeg = cityTransform.rotation.eulerAngles.y - toHit.Angle360();
                    }
                    float angle = startAngleDeg + toHit.Angle360();
                    if (lockAxisButton)
                    {
                        angle = Mathf.Round(angle / 90.0f) * 90.0f;
                    }
                    cityTransform.rotation = Quaternion.Euler(0.0f, angle, 0.0f);
                }
                else if (movingOrRotating)
                {
                    movingOrRotating = false;
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
            int zoomSteps = Mathf.RoundToInt(Mathf.Clamp(mouseScrollDelta, -1.0f, 1.0f)); // TODO(torben): this does not work in fixed
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
    }
}
