using SEE.Controls.Devices;
using UnityEngine;

namespace SEE.Controls
{
    public abstract class SelectionAction : MonoBehaviour
    {
        protected Selection selectionDevice;
        public Selection SelectionDevice
        {
            get => selectionDevice;
            set => selectionDevice = value;
        }

        /// <summary>
        /// The line renderer to draw the ray.
        /// </summary>
        private LineRenderer line;
        /// <summary>
        /// The game object holding the line renderer.
        /// </summary>
        private GameObject lineHolder;

        private Camera mainCamera;
        internal Camera MainCamera
        {
            get => mainCamera;
            set => mainCamera = value;
        }

        const float defaultWidth = 0.1f;

        [Tooltip("The color used when an object was hit.")]
        public Color colorOnHit = Color.green;
        [Tooltip("The color used when no object was hit.")]
        public Color defaultColor = Color.red;

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

        private void Update()
        {
            //Debug.LogFormat("selecting: {0}\n", selectionDevice.Activated);

            if (selectionDevice.Activated)
            {               
                GameObject selectedObject = Select(out RaycastHit hitInfo);
                ResetLine();

                if (selectedObject != null)
                {
                    //Debug.LogFormat("hit point at {0}\n", hitInfo.point);
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

        private GameObject Select(out RaycastHit hitInfo)
        {
            if (Detect(out hitInfo))
            {
                return hitInfo.collider.gameObject;
            }
            else
            {
                return null;
            }
        }

        protected abstract bool Detect(out RaycastHit hitInfo);

        protected abstract Vector3 Origin();
    }
}
