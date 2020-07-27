using System.Collections.Generic;
using UnityEngine;

namespace SEE.Controls
{

    public class NavigationAction : MonoBehaviour
    {
        private enum Mode
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

        // TODO(torben): put these somewhere else? Materials.cs is using this as well
        public const float TableMinX = -0.8f;
        public const float TableMaxX = 0.8f;
        public const float TableCenterX = (TableMinX + TableMaxX) / 2;

        public const float TableMinZ = -0.5f;
        public const float TableMaxZ = 0.5f;
        public const float TableCenterZ = (TableMinZ + TableMaxZ) / 2;

        public const float TableWidth = TableMaxX - TableMinX;
        public const float TableDepth = TableMaxZ - TableMinZ;



        private const float MaxVelocity = 10.0f;
        private const float MaxSqrVelocity = MaxVelocity * MaxVelocity;

        private const float MaxDistanceX = 1.2f * TableWidth;
        private const float MaxSqrDistanceX = MaxDistanceX * MaxDistanceX;
        private const float MaxDistanceZ = 1.2f * TableDepth;
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
        private bool dragButtonDown;
        private bool dragButton;
        private bool cancelButtonDown;
        private bool cancelButton;
        private bool lockAxisButton;
        private Vector3 mousePosition;
        private float mouseScrollDelta;

        private Mode mode;
        private bool dragging;
        private Vector3 dragStartTransformPosition;
        private Vector3 dragStartOffset;
        private Vector3 dragCanonicalOffset;
        private Vector3 dragVelocity;
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

            dragging = false;
            dragStartTransformPosition = cityTransform.position;
            dragCanonicalOffset = Vector3.zero;
            dragVelocity = Vector3.zero;
            pivot = new LinePivot(0.008f * (TableWidth < TableDepth ? TableWidth : TableDepth));

            zoomCommands = new List<ZoomCommand>((int)ZoomMaxSteps);
            zoomStepsInProgress = 0;
        }

        private void Update()
        {
            // Input MUST NOT be inquired in FixedUpdate()!

            dragButtonDown |= Input.GetMouseButtonDown(2);
            dragButton = Input.GetMouseButton(2);
            cancelButtonDown |= Input.GetKeyDown(KeyCode.Escape);
            cancelButton = Input.GetKey(KeyCode.Escape);
            lockAxisButton = Input.GetKey(KeyCode.LeftAlt);
            mouseScrollDelta = Input.mouseScrollDelta.y;
            mousePosition = Input.mousePosition;

            if (Input.GetKeyDown(KeyCode.M))
            {
                mode = Mode.Move;
                dragging = false;
                pivot.Enable(false);
            }
            else if (Input.GetKeyDown(KeyCode.R))
            {
                mode = Mode.Rotate;
                dragging = false;
                pivot.Enable(false);
            }
        }

        private void FixedUpdate()
        {
            // TODO(torben): abstract mouse away!

            Ray ray = Camera.main.ScreenPointToRay(mousePosition);
            bool raycastResult = raycastPlane.Raycast(ray, out float enter);
            Vector3 planeHitPoint = ray.GetPoint(enter);

            if (mode == Mode.Move)
            {
                if (cancelButtonDown)
                {
                    cancelButtonDown = false;
                    if (dragging)
                    {
                        dragging = false;
                        dragVelocity = Vector3.zero;
                        pivot.Enable(false);
                        cityTransform.position = dragStartTransformPosition + dragStartOffset - Vector3.Scale(dragCanonicalOffset, cityTransform.localScale);
                    }
                }
                else if (dragButton)
                {
                    if (raycastResult)
                    {
                        if (!dragging && dragButtonDown)
                        {
                            dragButtonDown = false;
                            dragging = true;
                            dragStartTransformPosition = cityTransform.position;
                            dragStartOffset = planeHitPoint - cityTransform.position;
                            dragCanonicalOffset = dragStartOffset.DividePairwise(cityTransform.localScale);
                            dragVelocity = Vector3.zero;
                            pivot.Enable(true);
                        }
                        if (dragging)
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

                            dragVelocity = (newPosition - oldPosition) / Time.fixedDeltaTime;
                            cityTransform.position = newPosition;
                            pivot.SetPositions(dragStartTransformPosition + dragStartOffset, cityTransform.position + Vector3.Scale(dragCanonicalOffset, cityTransform.localScale));
                        }
                    }
                }
                else if (dragging)
                {
                    dragging = false;
                    pivot.Enable(false);
                }
            }
            else // mode = Mode.Rotate
            {
                if (dragButton)
                {
                    Vector2 toHit = new Vector2(planeHitPoint.x - cityTransform.position.x, planeHitPoint.z - cityTransform.position.z);
                    if (dragButtonDown)
                    {
                        dragButtonDown = false;
                        dragging = true;
                        startAngleDeg = cityTransform.rotation.eulerAngles.y - toHit.Angle360();
                    }
                    cityTransform.rotation = Quaternion.Euler(0.0f, startAngleDeg + toHit.Angle360(), 0.0f);
                }
                else if (dragging)
                {
                    dragging = false;
                }
            }

            if (!dragging)
            {
                Vector3 acceleration = Vector3.zero;

                // TODO(torben): this whole thing currently assumes the shape of a quad!
                // therefore, circular cities can be lost in corners of the table!
                float cityMinX = cityTransform.position.x + (cityTransform.localScale.x * cityBounds.min.x);
                float cityMaxX = cityTransform.position.x + (cityTransform.localScale.x * cityBounds.max.x);
                float cityMinZ = cityTransform.position.z + (cityTransform.localScale.z * cityBounds.min.z);
                float cityMaxZ = cityTransform.position.z + (cityTransform.localScale.z * cityBounds.max.z);

                if (cityMaxX < TableMinX || cityMaxZ < TableMinZ || cityMinX > TableMaxX || cityMinZ > TableMaxZ)
                {
                    float toTableCenterX = TableCenterX - cityTransform.position.x;
                    float toTableCenterZ = TableCenterZ - cityTransform.position.z;
                    float length = Mathf.Sqrt(toTableCenterX * toTableCenterX + toTableCenterZ * toTableCenterZ);
                    toTableCenterX /= length;
                    toTableCenterZ /= length;
                    acceleration = new Vector3(32.0f * toTableCenterX, 0.0f, 32.0f * toTableCenterZ);
                }
                else
                {
                    acceleration = DragFrictionFactor * -dragVelocity;
                }
                dragVelocity += acceleration * Time.fixedDeltaTime;

                float dragVelocitySqrMag = dragVelocity.sqrMagnitude;
                if (dragVelocitySqrMag > MaxSqrVelocity)
                {
                    dragVelocity = dragVelocity / Mathf.Sqrt(dragVelocitySqrMag) * MaxVelocity;
                }
                cityTransform.position += dragVelocity * Time.fixedDeltaTime;
            }

            if (!dragging && zoomCommands.Count == 0)
            {
                // TODO(torben): similar TODO as above with circular cities!
                float tableToCityCenterX = cityTransform.position.x - TableCenterX;
                float tableToCityCenterZ = cityTransform.position.z - TableCenterZ;
                float distance = Mathf.Sqrt(tableToCityCenterX * tableToCityCenterX + tableToCityCenterZ * tableToCityCenterZ);
                float maxDistance = Mathf.Max(cityTransform.localScale.x * MaxDistanceX, cityTransform.localScale.z * MaxDistanceZ);
                if (distance > maxDistance)
                {
                    float offsetX = tableToCityCenterX / distance * maxDistance;
                    float offsetZ = tableToCityCenterZ / distance * maxDistance;
                    cityTransform.position = new Vector3(TableCenterX + offsetX, cityTransform.position.y, TableCenterZ + offsetZ);
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
