using UnityEngine;

namespace SEE.Controls
{
    /// <summary>
    /// Selection of objects when the selection device offers 3D directions
    /// (e.g., XR controllers). A line is drawn as a visual feedback for the
    /// search. 
    /// </summary>
    public class Selection3DAction : SelectionAction
    {
        /// <summary>
        /// The line renderer to draw the ray.
        /// </summary>
        private LineRenderer line;
        /// <summary>
        /// The game object holding the line renderer.
        /// </summary>
        private GameObject lineHolder;

        /// <summary>
        /// The width of the ray line.
        /// </summary>
        [Tooltip("The width of the selection ray line")]
        public float rayWidth = 0.005f;

        /// <summary>
        /// The maximal length the casted ray can reach.
        /// </summary>
        [Tooltip("The maximal length the selection ray can reach.")]
        public float RayDistance = 5.0f;

        [Tooltip("The color of the selection ray used when an object was hit.")]
        public Color colorOnSelectionHit = Color.green;
        [Tooltip("The color of the selection ray used when no object was hit.")]
        public Color colorOnSelectionMissed = Color.red;
        [Tooltip("The color of the grabbing ray used when an object was hit.")]
        public Color colorOnGrabbingHit = Color.blue;
        [Tooltip("The color of the grabbing ray used when no object was hit.")]
        public Color colorOnGrabbingMissed = Color.yellow;
        /// <summary>
        /// Sets up the object holding the line renderer for the shown ray
        /// and other parameters of the line.
        /// </summary>
        private void Start()
        {
            // We create game object holding the line renderer for the ray.
            // This game object will be added to the game object this component
            // is attached to.
            lineHolder = new GameObject();
            lineHolder.name = "Ray";
            lineHolder.transform.parent = this.transform;
            lineHolder.transform.localPosition = Vector3.up;

            line = lineHolder.AddComponent<LineRenderer>();

            // simplify rendering; no shadows
            line.receiveShadows = false;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            line.startWidth = rayWidth;
            line.endWidth = rayWidth;
        }

        /// <summary>
        /// Draws a line (using colorOnHit) from the current position of the selectionDevice to the
        /// location of <paramref name="selectedObject"/> if it is not null. Otherwise, a line is
        /// drawn (using defaultColor) from the the current position of the selectionDevice
        /// of length RayDistance in the pointing direction.
        /// </summary>
        /// <param name="selectedObject">the object selected or null</param>
        /// <param name="hitInfo">information about the hit (used only if <paramref name="selectedObject"/>
        /// is not null)</param>
        protected override void ShowSearchFeedback(GameObject selectedObject, RaycastHit hitInfo)
        {
            Vector3 origin = selectionDevice.Position;
            line.SetPosition(0, origin);
            
            if (selectedObject != null)
            {
                line.SetPosition(1, hitInfo.point);
                if (selectionDevice.IsGrabbing)
                {
                    line.material.color = colorOnGrabbingHit;
                }
                else
                {
                    line.material.color = colorOnSelectionHit;
                }
            }
            else
            {
                if (selectionDevice.IsGrabbing)
                {
                    line.material.color = colorOnGrabbingMissed;
                }
                else
                {
                    line.material.color = colorOnSelectionMissed;
                }
                line.SetPosition(1, origin + RayDistance * selectionDevice.Direction.normalized);
            }
        }

        /// <summary>
        /// Resets the ray line so that it becomes invisible again.
        /// </summary>
        protected override void HideSearchFeedback()
        {
            Vector3 origin = selectionDevice.Position;
            line.SetPosition(0, origin);
            line.SetPosition(1, origin);
        }

        /// <summary>
        /// Casts a physics ray to try to hit a game object. Returns true if one was hit.
        /// The ray cast is limited to length RayDistance.
        /// </summary>
        /// <param name="hitInfo">additional information on the hit; defined only if this
        /// method returns true</param>
        /// <returns>true if an object was hit</returns>
        protected override bool Detect(out RaycastHit hitInfo)
        {
            return Physics.Raycast(selectionDevice.Position, selectionDevice.Direction, out hitInfo, RayDistance, ignoreLayer);
        }
    }
}
