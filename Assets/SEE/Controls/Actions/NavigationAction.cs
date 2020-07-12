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

    public class NavigationAction : MonoBehaviour
    {
        // TODO: put these somewhere else? Materials.cs is using this as well
        public const float TableMinX = -1.0f;
        public const float TableMaxX = 1.0f;
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

        private const float DragFrictionFactor = 16.0f;

        private const float ZoomDuration = 0.1f;
        private const uint ZoomMaxSteps = 16;
        [Range(0.5f, 16.0f)] private const float ZoomFactor = 4.0f;



        private Transform cityTransform;
        private Vector3 originalScale;
        private Bounds cityBounds;
        private Plane raycastPlane;

        private bool dragging;
        private Vector3 dragCanonicalOffset;
        private Vector3 dragVelocity;

        private List<ZoomCommand> zoomCommands;
        private int zoomStepsInProgress;



        private void Start()
        {
            cityTransform = GameObject.Find("Implementation").transform.GetChild(0).transform; // TODO: find it some more robust way
            originalScale = cityTransform.localScale;
            cityBounds = cityTransform.GetComponent<MeshCollider>().bounds;
            raycastPlane = new Plane(Vector3.up, cityTransform.position);

            dragging = false;
            dragCanonicalOffset = Vector3.zero;
            dragVelocity = Vector3.zero;

            zoomCommands = new List<ZoomCommand>(16);
            zoomStepsInProgress = 0;
        }

        private void Update()
        {
            // TODO: abstract mouse away!

            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            bool raycastResult = raycastPlane.Raycast(ray, out float enter);
            Vector3 planeHitPoint = ray.GetPoint(enter);



            // Moving city
            if (Input.GetMouseButton(2))
            {
                if (raycastResult)
                {
                    if (!dragging && Input.GetMouseButtonDown(2))
                    {
                        dragging = true;

                        Vector3 scaledDragOffset = planeHitPoint - cityTransform.position;
                        dragCanonicalOffset = scaledDragOffset.DividePairwise(cityTransform.localScale);
                        dragVelocity = Vector3.zero;
                    }
                    else if (dragging)
                    {
                        dragVelocity = (planeHitPoint - cityTransform.position - Vector3.Scale(dragCanonicalOffset, cityTransform.localScale)) / Time.deltaTime;
                    }
                }
            }
            else if (dragging)
            {
                dragging = false;
            }

            if (!dragging)
            {
                Vector3 acceleration = Vector3.zero;

                // TODO: this whole thing currently assumes the shape of a quad!
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
            }
            cityTransform.position += dragVelocity * Time.deltaTime;

            // TODO: similar TODO as above with circular cities!
            float toCityCenterX = cityTransform.position.x - TableCenterX;
            float toCityCenterZ = cityTransform.position.z - TableCenterZ;
            float distance = Mathf.Sqrt(toCityCenterX * toCityCenterX + toCityCenterZ * toCityCenterZ);
            float maxDistance = Mathf.Max(cityTransform.localScale.x * MaxDistanceX, cityTransform.localScale.z * MaxDistanceZ);
            if (distance > maxDistance)
            {
                float offsetX = toCityCenterX / distance * maxDistance;
                float offsetZ = toCityCenterZ / distance * maxDistance;
                cityTransform.position = new Vector3(TableCenterX + offsetX, cityTransform.position.y, TableCenterZ + offsetZ);
            }



            // Zoom into city
            int steps = Mathf.RoundToInt(Mathf.Clamp(Input.mouseScrollDelta.y, -1.0f, 1.0f));
            if (steps != 0 && Mathf.Abs(zoomStepsInProgress + steps) <= ZoomMaxSteps)
            {
                zoomCommands.Add(new ZoomCommand(steps, ZoomDuration));
                zoomStepsInProgress += steps;
            }
            if (zoomCommands.Count != 0)
            {
                float currentZoomSteps = (float)zoomStepsInProgress; // (-ZoomMaxSteps, ZoomMaxSteps)
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
                float x = (float)currentZoomSteps / ZoomMaxSteps; // (-1, 1)

                float Square(float f) => f * f;
                float y = Square(Square(x + 1.0f)) * (1.0f - (0.5f / ZoomFactor)) + (0.5f / ZoomFactor);

                Vector3 offset = planeHitPoint - cityTransform.position;
                Vector3 canonicalOffset = offset.DividePairwise(cityTransform.localScale);

                cityTransform.position += offset;
                cityTransform.localScale = y * originalScale;
                cityTransform.position -= Vector3.Scale(canonicalOffset, cityTransform.localScale);
            }
        }
    }
}
