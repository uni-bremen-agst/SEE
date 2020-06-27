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

    public class NavigationAction : MonoBehaviour
    {
        private const float DragFrictionFactor = 16.0f;
        private const float ZoomDuration = 0.2f;
        private const float ZoomMaxSteps = 4.0f;
        private const float ZoomSpeed = 1.0f;



        private Transform cityTransform;
        private Plane raycastPlane;

        private bool dragging;
        private Vector3 dragCanonicalOffset;
        private Vector3 dragVelocity;

        private bool zooming;
        private Vector3 zoomCanonicalOffset;
        private float zoomStartTime;
        private Vector3 zoomStartScale;
        private Vector3 zoomTargetScale;
        private float zoomSteps;



        private void Start()
        {
            cityTransform = GameObject.Find("Implementation").transform.GetChild(0).transform; // TODO: find it some more robust way
            raycastPlane = new Plane(Vector3.up, cityTransform.position);

            dragging = false;
            dragCanonicalOffset = Vector3.zero;
            dragVelocity = Vector3.zero;

            zooming = false;
            zoomStartTime = float.MinValue;
            zoomStartScale = cityTransform.localScale;
            zoomTargetScale = cityTransform.localScale;
            zoomSteps = 0.0f;
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
                    else
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
                Vector3 acceleration = DragFrictionFactor * -dragVelocity;
                dragVelocity += acceleration * Time.deltaTime;
            }
            cityTransform.position += dragVelocity * Time.deltaTime;


            
            // Zoom into city
            float delta = Mathf.Clamp(Input.mouseScrollDelta.y, -1.0f, 1.0f);
            if (delta != 0.0f && Mathf.Abs(zoomSteps + delta) < ZoomMaxSteps)
            {
                zooming = true;

                Vector3 scaledOffset = planeHitPoint - cityTransform.position;
                zoomCanonicalOffset = scaledOffset.DividePairwise(cityTransform.localScale);

                zoomStartTime = Time.realtimeSinceStartup;

                zoomStartScale = zoomTargetScale;
                float zoomInFactor = 2.0f * ZoomSpeed;
                float zoomOutFactor = 1.0f / zoomInFactor;
                float normalizedDelta = delta * 0.5f + 0.5f;
                float zoomFactor = normalizedDelta * (zoomInFactor - zoomOutFactor) + zoomOutFactor;
                zoomTargetScale = zoomTargetScale * zoomFactor;

                zoomSteps += delta;
            }
            if (zooming)
            {
                float deltaTime = Time.realtimeSinceStartup - zoomStartTime;
                float x = Mathf.Min(1.0f, deltaTime / ZoomDuration);
                float xMinusOne = x - 1.0f;
                float t = -(xMinusOne * xMinusOne) + 1.0f;

                cityTransform.position += Vector3.Scale(zoomCanonicalOffset, cityTransform.localScale);
                cityTransform.localScale = Vector3.Lerp(zoomStartScale, zoomTargetScale, t);
                cityTransform.position -= Vector3.Scale(zoomCanonicalOffset, cityTransform.localScale);

                if (t == 1.0f)
                {
                    zooming = false;
                }
            }
        }
    }
}
