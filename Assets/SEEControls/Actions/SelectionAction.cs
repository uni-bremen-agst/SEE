using System;
using UnityEngine;

namespace SEE.Controls
{
    public class SelectionAction : MonoBehaviour
    {
        /// <summary>
        /// The direction of pointing.
        /// </summary>
        private Vector3 direction;
        /// <summary>
        /// The line renderer to draw the ray.
        /// </summary>
        private LineRenderer line;
        /// <summary>
        /// The game object holding the line renderer.
        /// </summary>
        private GameObject lineHolder;

        private Camera mainCamera;

        const float defaultWidth = 0.1f;

        private bool threeDimensions = true;

        public bool ThreeDimensions {
            get => threeDimensions;
            set => threeDimensions = value;
        }

        public Color colorOnHit = Color.green;
        public Color defaultColor = Color.red;

        private bool active = true;

        private void Start()
        {
            lineHolder = new GameObject();
            lineHolder.name = "Ray";
            lineHolder.transform.parent = this.transform;
            lineHolder.transform.localPosition = Vector3.up;

            line = lineHolder.AddComponent<LineRenderer>();

            // simplify rendering; no shadows
            line.receiveShadows = false;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            line.startWidth = defaultWidth;
            line.endWidth = defaultWidth;
        }

        private Vector3 Origin()
        {
            return threeDimensions ? gameObject.transform.position : mainCamera.transform.position;
        }

        private void Update()
        {
            if (active)
            {
                GameObject selectedObject = Select(out RaycastHit hitInfo);
                ResetLine();

                if (selectedObject != null)
                {
                    Debug.LogFormat("hit point at {0}\n", hitInfo.point);
                    line.SetPosition(1, hitInfo.point);
                    line.material.color = colorOnHit;
                }
                else
                {
                    line.material.color = defaultColor;
                }

                // Do something with the selected object.
                if (selectedObject != null)
                {
                    Debug.LogFormat("Selected object {0}\n", selectedObject.name);
                }
            }
        }

        private void ResetLine()
        {
            Vector3 origin = Origin();
            line.SetPosition(0, origin);
            line.SetPosition(1, origin);
        }

        internal void SetCamera(Camera camera)
        {
            mainCamera = camera;
            Debug.LogFormat("SelectionAction camera={0}\n", camera.name);
        }

        private GameObject Select(out RaycastHit hitInfo)
        {
            bool hit;

            if (ThreeDimensions)
            {
                hit = Physics.Raycast(gameObject.transform.position, direction, out hitInfo, Mathf.Infinity);                
            }
            else
            {                
                Ray ray = mainCamera.ScreenPointToRay(direction);
                Debug.LogFormat("2d direction={0} at origin {1} and direction {2} camera {3}\n", direction, ray.origin, ray.direction, mainCamera.transform.position);
                Debug.DrawRay(ray.origin, ray.direction * 10000, Color.yellow);
                hit = Physics.Raycast(ray, out hitInfo);
            }
            if (hit)
            {
                return hitInfo.collider.gameObject;
            }
            else
            {
                return null;
            }
        }

        public void RayOnOff(bool turnOn)
        {
            if (!turnOn && line != null)
            {
                ResetLine();
            }
            active = turnOn;
            //Debug.LogFormat("Ray on/off: {0}\n", active);
        }

        public void SelectAt(Vector3 direction)
        {
            this.direction = direction;
        }
    }
}
