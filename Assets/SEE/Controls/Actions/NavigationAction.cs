using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

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

    internal abstract class PivotBase
    {
        protected const string DefaultShaderName = "Unlit/3DUIShader";
        protected const float DefaultPrimaryAlpha = 0.5f;
        protected const float DefaultSecondaryAlpha = 0.5f * DefaultPrimaryAlpha;

        protected readonly float scale;

        protected PivotBase(float scale)
        {
            this.scale = scale;
        }

        internal abstract void Enable(bool enable);
        internal abstract void SetPositions(Vector3 startPoint, Vector3 endPoint);

        protected Material CreateDefaultMaterial(bool primary)
        {
            Shader shader = Shader.Find(DefaultShaderName);
            Material material = null;
            if (shader)
            {
                material = new Material(shader);
                material.SetInt("_ZTest", (int)(primary ? UnityEngine.Rendering.CompareFunction.Greater : UnityEngine.Rendering.CompareFunction.LessEqual));
            }
            else
            {
                Debug.LogWarning("Shader could not be found!");
            }
            return material;
        }

        protected Color CreateDefaultColor(Vector3 startToEnd, bool primary)
        {
            float length = startToEnd.magnitude;
            float f = Mathf.Clamp(length / (0.5f * scale), 0.0f, 1.0f);
            Vector3 startToEndMapped = ((length == 0 ? Vector3.zero : startToEnd / length) * 0.5f + new Vector3(0.5f, 0.5f, 0.5f)) * f;
            Color color = new Color(startToEndMapped.x, startToEndMapped.y, startToEndMapped.z, primary ? DefaultPrimaryAlpha : DefaultSecondaryAlpha);
            return color;
        }
    }
    
    internal class LinePivot : PivotBase
    {
        private const float GoldenRatio = 1.618034f;

        private readonly GameObject[] starts;
        private readonly GameObject[] ends;
        private readonly GameObject[] mains;

        internal LinePivot(float scale) : base(scale)
        {
            starts = new GameObject[2]
            {
                GameObject.CreatePrimitive(PrimitiveType.Sphere),
                GameObject.CreatePrimitive(PrimitiveType.Sphere)
            };
            ends = new GameObject[2]
            {
                GameObject.CreatePrimitive(PrimitiveType.Sphere),
                GameObject.CreatePrimitive(PrimitiveType.Sphere)
            };
            mains = new GameObject[2]
            {
                GameObject.CreatePrimitive(PrimitiveType.Cylinder),
                GameObject.CreatePrimitive(PrimitiveType.Cylinder)
            };

            Material[] materials = new Material[2]
            {
                CreateDefaultMaterial(true),
                CreateDefaultMaterial(false)
            };

            for (int i = 0; i < 2; i++)
            {
                starts[i].GetComponent<MeshRenderer>().sharedMaterial = materials[i];
                ends[i].GetComponent<MeshRenderer>().sharedMaterial = materials[i];
                mains[i].GetComponent<MeshRenderer>().sharedMaterial = materials[i];

                starts[i].transform.position = Vector3.zero;
                ends[i].transform.position = Vector3.zero;
                mains[i].transform.position = Vector3.zero;

                starts[i].transform.localScale = new Vector3(scale, scale, scale);
                ends[i].transform.localScale = new Vector3(scale, scale, scale);
                mains[i].transform.localScale = new Vector3(scale, scale, scale) / GoldenRatio;

                starts[i].SetActive(false);
                ends[i].SetActive(false);
                mains[i].SetActive(false);
            }
        }

        internal override void Enable(bool enable)
        {
            for (int i = 0; i < 2; i++)
            {
                starts[i].SetActive(enable);
                ends[i].SetActive(enable);
                mains[i].SetActive(enable);
            }
        }

        internal override void SetPositions(Vector3 startPoint, Vector3 endPoint)
        {
            Vector3 startToEnd = endPoint - startPoint;
            Color color0 = CreateDefaultColor(startToEnd, true);
            Color color1 = CreateDefaultColor(startToEnd, false);

            for (int i = 0; i < 2; i++)
            {
                starts[i].transform.up = startToEnd;
                ends[i].transform.up = startToEnd;
                mains[i].transform.up = startToEnd;

                starts[i].transform.position = startPoint;
                ends[i].transform.position = endPoint;
                mains[i].transform.position = (startPoint + endPoint) / 2.0f;
                mains[i].transform.localScale = new Vector3(scale / GoldenRatio, 0.5f * startToEnd.magnitude, scale / GoldenRatio);
            }

            starts[0].GetComponent<MeshRenderer>().sharedMaterial.color = color0;
            ends[0].GetComponent<MeshRenderer>().sharedMaterial.color = color0;
            mains[0].GetComponent<MeshRenderer>().sharedMaterial.color = color0;
            starts[1].GetComponent<MeshRenderer>().sharedMaterial.color = color1;
            ends[1].GetComponent<MeshRenderer>().sharedMaterial.color = color1;
            mains[1].GetComponent<MeshRenderer>().sharedMaterial.color = color1;
        }
    }

    internal class PointPivot : PivotBase
    {
        private readonly GameObject[] pivots;

        internal PointPivot(float scale) : base(scale)
        {
            Material[] materials = new Material[2]
            {
                CreateDefaultMaterial(true),
                CreateDefaultMaterial(false)
            };

            pivots = new GameObject[2];
            for (int i = 0; i < 2; i++)
            {
                pivots[i] = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                pivots[i].GetComponent<MeshRenderer>().sharedMaterial = materials[i];
                pivots[i].transform.position = Vector3.zero;
                pivots[i].transform.localScale = new Vector3(scale, scale, scale);
                pivots[i].SetActive(false);
            }
        }

        internal override void Enable(bool enable)
        {
            pivots[0].SetActive(enable);
            pivots[1].SetActive(enable);
        }

        internal override void SetPositions(Vector3 startPoint, Vector3 endPoint)
        {
            pivots[0].transform.position = startPoint;
            pivots[1].transform.position = startPoint;
            Vector3 startToEnd = endPoint - startPoint;
            pivots[0].GetComponent<MeshRenderer>().sharedMaterial.color = CreateDefaultColor(startToEnd, true);
            pivots[1].GetComponent<MeshRenderer>().sharedMaterial.color = CreateDefaultColor(startToEnd, false);
        }
    }

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
        private PivotBase pivot;

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
            // TODO(torben): abstract mouse away!

            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            bool raycastResult = raycastPlane.Raycast(ray, out float enter);
            Vector3 planeHitPoint = ray.GetPoint(enter);



            // Moving city
            if (Input.GetKeyDown(KeyCode.Escape) && dragging)
            {
                dragging = false;
                dragVelocity = Vector3.zero;
                pivot.Enable(false);
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
                        pivot.Enable(true);
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
                        pivot.SetPositions(dragStartTransformPosition + dragStartOffset, cityTransform.position + Vector3.Scale(dragCanonicalOffset, cityTransform.localScale));
                    }
                }
            }
            else if (dragging)
            {
                dragging = false;
                pivot.Enable(false);
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
