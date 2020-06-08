using SEE.GO;
using System;
using UnityEngine;

namespace SEE.Controls
{
    /// <summary>
    /// Selection of objects when the selection device offers only 2D locations
    /// on the screen (e.g., mouse or touch devices).
    /// </summary>
    public class Selection2DAction : SelectionAction
    {
        /// <summary>
        /// Returns a ray going from the MainCamera through the pointing direction of 
        /// the selection device.
        /// </summary>
        /// <returns>ray from MainCamera through selectionDevice.Direction</returns>
        protected override Ray GetRay()
        {
            return MainCamera.ScreenPointToRay(selectionDevice.Direction);
        }

        /// <summary>
        /// Casts a ray from the MainCamera through the selection.Direction (position
        /// on the screen) to hit a game object. Returns true if one was hit.
        /// </summary>
        /// <param name="hitInfo">additional information on the hit; defined only if this
        /// method returns true</param>
        /// <returns>true if an object was hit</returns>
        protected override bool Detect(out RaycastHit hitInfo)
        {
            Ray ray = GetRay();
            Debug.DrawRay(ray.origin, ray.direction);

            // TODO: currently, only the 2D selection ignored not rendered GameObjects!
            RaycastHit[] hits = Physics.RaycastAll(ray);
            Array.Sort(hits, (h0, h1) => h0.distance.CompareTo(h1.distance));
            foreach (RaycastHit hit in hits)
            {
                if (hit.collider.gameObject.GetComponent<Renderer>().enabled)
                {
                    hitInfo = hit;
                    return true;
                }
            }
            hitInfo = new RaycastHit();
            return false;
        }

        protected Vector3 GrabbingRayStart()
        {
            // The origin of the ray must be slightly off the camera. Otherwise it cannot be seen.
            return MainCamera.transform.position + Vector3.down * 0.1f;
        }

        //-----------------------------------------------------------------------
        // Visual grabbing feedback
        //-----------------------------------------------------------------------

        /// <summary>
        /// The line drawn from the player to the grabbed object when an object
        /// is grabbed.
        /// </summary>
        private GrabLine grabLine;

        /// <summary>
        /// Terminates the visual feedback on the currently ongoing grabbing.
        /// </summary>
        protected override void HideGrabbingFeedback()
        {
            grabLine?.Off();
        }

        /// <summary>
        /// Gives visual feedback when an object was grabbed. Draws a line from the 
        /// player to <paramref name="heldObject"/>.
        /// </summary>
        /// <param name="heldObject">the object selected or null if none was selected</param>
        public override void ShowGrabbingFeedback(GameObject heldObject)
        {
            if (grabLine == null)
            {
                grabLine = new GrabLine(gameObject);
            }
            // Draw a ray from the player to the held object.
            grabLine.Draw(GrabbingRayStart(), heldObject.transform.position);
        }

        /// <summary>
        /// To draw a line from the player to the grabbed object when an object
        /// is grabbed.
        /// </summary>
        private class GrabLine
        {
            /// <summary>
            /// Line renderer use to draw the line.
            /// </summary>
            private readonly LineRenderer renderer;

            /// <summary>
            /// The start and end position of the line in world space.
            /// </summary>
            private readonly Vector3[] linePoints = new Vector3[2];

            /// <summary>
            /// Constructor.
            /// </summary>
            /// <param name="gameObject">the object where to attach the line renderer</param>
            public GrabLine(GameObject gameObject)
            {
                renderer = gameObject.AddComponent<LineRenderer>();
                LineFactory.SetDefaults(renderer);
                LineFactory.SetWidth(renderer, 0.01f);
                renderer.positionCount = linePoints.Length;
                renderer.useWorldSpace = true;
            }

            /// <summary>
            /// Draws a line from <paramref name="from"/> to <paramref name="to"/>.
            /// </summary>
            /// <param name="from">begin of the line</param>
            /// <param name="to">end of the line</param>
            public void Draw(Vector3 from, Vector3 to)
            {
                renderer.enabled = true;
                linePoints[0] = from;
                linePoints[1] = to;
                renderer.SetPositions(linePoints);
            }

            /// <summary>
            /// Turns the line off.
            /// </summary>
            public void Off()
            {
                linePoints[1] = linePoints[0];
                renderer.SetPositions(linePoints);
                renderer.enabled = false;
            }
        }
    }
}
