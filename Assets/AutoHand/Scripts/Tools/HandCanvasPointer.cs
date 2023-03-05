using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace Autohand
{
    [Serializable]
    public class UnityCanvasPointerEvent : UnityEvent<Vector3, GameObject> { }

    [HelpURL("https://app.gitbook.com/s/5zKO0EvOjzUDeT2aiFk3/auto-hand/extras/ui-interaction")]
    public class HandCanvasPointer : MonoBehaviour
    {
        [Header("References")]
        public GameObject hitPointMarker;
        private LineRenderer lineRenderer;


        [Header("Ray settings")]
        public float raycastLength = 8.0f;
        public bool autoShowTarget = true;
        public LayerMask UILayer;


        [Header("Events")]
        public UnityCanvasPointerEvent StartSelect;
        public UnityCanvasPointerEvent StopSelect;
        public UnityCanvasPointerEvent StartPoint;
        public UnityCanvasPointerEvent StopPoint;



        public GameObject _currTarget;
        public GameObject currTarget
        {
            get { return _currTarget; }
        }


        public RaycastHit lastHit { get; private set; }

        // Internal variables
        private bool hover = false;
        AutoInputModule inputModule = null;
        float lineSegements = 10f;

        static Camera cam = null;
        public static Camera UICamera
        {
            get
            {
                if (cam == null)
                {
                    cam = new GameObject("Camera Canvas Pointer (I AM CREATED AT RUNTIME FOR UI CANVAS INTERACTION, I AM NOT RENDERING ANYTHING, I AM NOT CREATING ADDITIONAL OVERHEAD)").AddComponent<Camera>();
                    cam.clearFlags = CameraClearFlags.Nothing;
                    cam.stereoTargetEye = StereoTargetEyeMask.None;
                    cam.orthographic = true;
                    cam.orthographicSize = 0.001f;
                    cam.cullingMask = 0;
                    cam.nearClipPlane = 0.01f;
                    cam.depth = 0f;
                    cam.allowHDR = false;
                    cam.enabled = false;
                    cam.fieldOfView = 0.00001f;
                    cam.transform.parent = AutoHandExtensions.transformParent;

#if (UNITY_2020_3_OR_NEWER)
                    var canvases = FindObjectsOfType<Canvas>(true);
#else
                    var canvases = FindObjectsOfType<Canvas>();
#endif
                    foreach (var canvas in canvases)
                        if (canvas.renderMode == RenderMode.WorldSpace)
                            canvas.worldCamera = cam;
                }
                return cam;
            }
        }
        int pointerIndex;

        void OnEnable()
        {

            lineRenderer.positionCount = (int)lineSegements;
            if (inputModule.Instance != null)
                pointerIndex = inputModule.Instance.AddPointer(this);
        }

        void OnDisable()
        {
            if(inputModule) inputModule.Instance?.RemovePointer(this);
        }

        public void SetIndex(int index)
        {
            pointerIndex = index;
        }

        internal void Preprocess()
        {
            UICamera.transform.position = transform.position;
            UICamera.transform.forward = transform.forward;
        }

        public void Press()
        {
            // Handle the UI events
            if(inputModule) inputModule.ProcessPress(pointerIndex);

            // Show the ray when they attemp to press
            if (!autoShowTarget && hover) ShowRay(true);

            if (lastHit.collider != null)
            {
                // Fire the Unity event
                StartSelect?.Invoke(lastHit.point, lastHit.transform.gameObject);
            }
            else
            {
                PointerEventData data = inputModule.GetData(pointerIndex);
                float targetLength = data.pointerCurrentRaycast.distance == 0 ? raycastLength : data.pointerCurrentRaycast.distance;
                StartSelect?.Invoke(transform.position + (transform.forward * targetLength), null);
            }
        }

        public void Release()
        {
            // Handle the UI events
            if(inputModule) inputModule.ProcessRelease(pointerIndex);

            if (lastHit.collider != null)
            {
                // Fire the Unity event
                StopSelect?.Invoke(lastHit.point, lastHit.transform.gameObject);
            }
            else
            {
                PointerEventData data = inputModule.GetData(pointerIndex);
                float targetLength = data.pointerCurrentRaycast.distance == 0 ? raycastLength : data.pointerCurrentRaycast.distance;
                StopSelect?.Invoke(transform.position + (transform.forward * targetLength), null);
            }

        }

        private void Awake()
        {
            if (lineRenderer == null)
                gameObject.CanGetComponent(out lineRenderer);

            if (inputModule == null)
            {
                if (gameObject.CanGetComponent<AutoInputModule>(out var inputMod))
                {
                    inputModule = inputMod;
                }
                else if (!(inputModule = FindObjectOfType<AutoInputModule>()))
                {
                    EventSystem system = FindObjectOfType<EventSystem>();
                    if(system == null) {
                        system = new GameObject().AddComponent<EventSystem>();
                        system.name = "UI Input Event System";
                    }
                    inputModule = system.gameObject.AddComponent<AutoInputModule>();
                    inputModule.transform.parent = AutoHandExtensions.transformParent;
                }
            }
        }

        private void Update()
        {
            UpdateLine();
        }

        private void UpdateLine()
        {
            PointerEventData data = inputModule.GetData(pointerIndex);
            float targetLength = data.pointerCurrentRaycast.distance == 0 ? raycastLength : data.pointerCurrentRaycast.distance;

            if (targetLength > 0)
                _currTarget = data.pointerCurrentRaycast.gameObject;
            else
                _currTarget = null;

            if (data.pointerCurrentRaycast.distance != 0 && !hover)
            {
                lastHit = CreateRaycast(targetLength);
                Vector3 endPosition = transform.position + (transform.forward * targetLength);
                if (lastHit.collider) endPosition = lastHit.point;


                if (lastHit.collider != null)
                    StartPoint?.Invoke(lastHit.point, lastHit.transform.gameObject);
                else
                    StartPoint?.Invoke(endPosition, null);


                // Show the ray if autoShowTarget is on when they enter the canvas
                if (autoShowTarget) ShowRay(true);

                hover = true;
            }
            else if (data.pointerCurrentRaycast.distance == 0 && hover)
            {
                lastHit = CreateRaycast(targetLength);
                Vector3 endPosition = transform.position + (transform.forward * targetLength);
                if (lastHit.collider) endPosition = lastHit.point;

                if (lastHit.collider != null)
                    StopPoint?.Invoke(lastHit.point, lastHit.transform.gameObject);
                else
                    StopPoint?.Invoke(endPosition, null);

                // Hide the ray when they leave the canvas
                ShowRay(false);

                hover = false;
            }

            if(hover) {
                lastHit = CreateRaycast(targetLength);

                Vector3 endPosition = transform.position + (transform.forward * targetLength);

                if(lastHit.collider) endPosition = lastHit.point;

                //Handle the hitmarker
                hitPointMarker.transform.position = endPosition;
                hitPointMarker.transform.forward = data.pointerCurrentRaycast.worldNormal;

                if(lastHit.collider) {
                    hitPointMarker.transform.forward = lastHit.collider.transform.forward;
                    hitPointMarker.transform.position = endPosition + hitPointMarker.transform.forward * 0.002f;
                }

                //Handle the line renderer
                for(int i = 0; i < lineSegements; i++) {
                    lineRenderer.SetPosition(i, Vector3.Lerp(transform.position, endPosition, i/ lineSegements));
                }
            }
        }

        private RaycastHit CreateRaycast(float dist)
        {
            RaycastHit hit;
            Ray ray = new Ray(transform.position, transform.forward);
            Physics.Raycast(ray, out hit, dist, UILayer);

            return hit;
        }

        private void ShowRay(bool show)
        {
            hitPointMarker.SetActive(show);
            lineRenderer.enabled = show;
        }

    }
}