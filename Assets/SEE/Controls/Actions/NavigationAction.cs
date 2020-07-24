using System.Collections.Generic;
using UnityEngine;

namespace UnityEngine
{
    public static class Vector3ExtensionMethods
    {
        public static Vector3 DividePairwise(this Vector3 a, Vector3 b)
        {
            return new Vector3(
                a.x == 0.0f ? 0.0f : a.x / b.x,
                a.y == 0.0f ? 0.0f : a.y / b.y,
                a.z == 0.0f ? 0.0f : a.z / b.z
            );
        }
    }
}

namespace SEE.Controls
{
    internal class ZoomCommand
    {
        internal int targetZoomSteps;
        internal float duration;
        internal float startTime;

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

#if false
    internal class DragPivot
    {
        private const float GoldenRatio = 1.618034f;

        private float size;
        private GameObject start;
        private GameObject end;
        private GameObject main;

        internal DragPivot(float size)
        {
            this.size = size;

            start = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            end = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            main = GameObject.CreatePrimitive(PrimitiveType.Cylinder);

            start.transform.position = Vector3.zero;
            end.transform.position = Vector3.zero;
            main.transform.position = Vector3.zero;

            start.transform.localScale = new Vector3(size, size, size);
            end.transform.localScale = new Vector3(size, size, size);
            main.transform.localScale = new Vector3(size, size, size) / GoldenRatio;

            start.SetActive(false);
            end.SetActive(false);
            main.SetActive(false);
        }

        internal void Enable(bool enable)
        {
            start.SetActive(enable);
            end.SetActive(enable);
            main.SetActive(enable);
        }

        internal void SetPositions(Vector3 startPoint, Vector3 endPoint)
        {
            Vector3 startToEnd = endPoint - startPoint;
            start.transform.up = startToEnd;
            end.transform.up = startToEnd;
            main.transform.up = startToEnd;

            start.transform.position = startPoint;
            end.transform.position = endPoint;
            main.transform.position = (startPoint + endPoint) / 2.0f;
            main.transform.localScale = new Vector3(size / GoldenRatio, 0.5f * startToEnd.magnitude, size / GoldenRatio);

            Vector3 startToEndMapped = startToEnd.normalized * 0.5f + new Vector3(0.5f, 0.5f, 0.5f);
            Color color = new Color(startToEndMapped.x, startToEndMapped.y, startToEndMapped.z);

            start.GetComponent<MeshRenderer>().sharedMaterial.color = color;
            end.GetComponent<MeshRenderer>().sharedMaterial.color = color;
            main.GetComponent<MeshRenderer>().sharedMaterial.color = color;
        }
    }
#else
    internal class DragPivot
    {
        private float size;
        private GameObject pivot;

        internal DragPivot(float size)
        {
            this.size = size;

            pivot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            pivot.transform.position = Vector3.zero;
            pivot.transform.localScale = new Vector3(size, size, size);
            pivot.SetActive(false);
        }

        internal void Enable(bool enable)
        {
            pivot.SetActive(enable);
        }

        internal void SetPositions(Vector3 startPoint, Vector3 endPoint)
        {
            pivot.transform.position = startPoint;

            Vector3 startToEnd = endPoint - startPoint;
            Vector3 startToEndMapped = startToEnd.normalized * 0.5f + new Vector3(0.5f, 0.5f, 0.5f);
            Color color = new Color(startToEndMapped.x, startToEndMapped.y, startToEndMapped.z);

            pivot.GetComponent<MeshRenderer>().sharedMaterial.color = color;
        }
    }
#endif

    public class NavigationAction : MonoBehaviour
    {
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

        private bool dragging;
        private bool lockAxis;
        private Vector3 dragStartTransformPosition;
        private Vector3 dragStartOffset;
        private Vector3 dragCanonicalOffset;
        private Vector3 dragVelocity;
        private DragPivot dragPivot;

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
            dragPivot = new DragPivot(0.008f * (TableWidth < TableDepth ? TableWidth : TableDepth));

            zoomCommands = new List<ZoomCommand>((int)ZoomMaxSteps);
            zoomStepsInProgress = 0;
        }

        private void Update()
        {
            // TODO(torben): abstract mouse away!

            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            bool raycastResult = raycastPlane.Raycast(ray, out float enter);
            Vector3 planeHitPoint = ray.GetPoint(enter);



            // Moving city
            if (Input.GetKeyDown(KeyCode.Escape) && dragging)
            {
                dragging = false;
                dragVelocity = Vector3.zero;
                dragPivot.Enable(false);
                cityTransform.position = dragStartTransformPosition + dragStartOffset - Vector3.Scale(dragCanonicalOffset, cityTransform.localScale);
            }
            else if (Input.GetMouseButton(2))
            {
                if (raycastResult)
                {
                    if (!dragging && Input.GetMouseButtonDown(2))
                    {
                        dragging = true;
                        dragStartTransformPosition = cityTransform.position;
                        dragStartOffset = planeHitPoint - cityTransform.position;
                        dragCanonicalOffset = dragStartOffset.DividePairwise(cityTransform.localScale);
                        dragVelocity = Vector3.zero;
                        dragPivot.Enable(true);
                    }
                    if (dragging)
                    {
                        Vector3 totalDragOffsetFromStart = planeHitPoint - (dragStartTransformPosition + dragStartOffset);

                        Vector3 axisMask = Vector3.one;
                        if (Input.GetKey(KeyCode.LeftAlt))
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

                        dragVelocity = (newPosition - oldPosition) / Time.deltaTime;
                        cityTransform.position = newPosition;
                        dragPivot.SetPositions(dragStartTransformPosition + dragStartOffset, cityTransform.position + Vector3.Scale(dragCanonicalOffset, cityTransform.localScale));
                    }
                }
            }
            else if (dragging)
            {
                dragging = false;
                dragPivot.Enable(false);
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
                dragVelocity += acceleration * Time.deltaTime;

                float dragVelocitySqrMag = dragVelocity.sqrMagnitude;
                if (dragVelocitySqrMag > MaxSqrVelocity)
                {
                    dragVelocity = dragVelocity / Mathf.Sqrt(dragVelocitySqrMag) * MaxVelocity;
                }
                cityTransform.position += dragVelocity * Time.deltaTime;
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
            int zoomSteps = Mathf.RoundToInt(Mathf.Clamp(Input.mouseScrollDelta.y, -1.0f, 1.0f));
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
