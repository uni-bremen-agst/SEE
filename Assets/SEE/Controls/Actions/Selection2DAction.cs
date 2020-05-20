using SEE.GO;
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
            return Physics.Raycast(GetRay(), out hitInfo, Physics.IgnoreRaycastLayer);
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
        protected override void ShowGrabbingFeedback(GameObject heldObject)
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
            private readonly LineRenderer renderer;
            private readonly Vector3[] linePoints = new Vector3[2];

            public GrabLine(GameObject gameObject)
            {
                renderer = gameObject.AddComponent<LineRenderer>();
                LineFactory.SetDefaults(renderer);
                LineFactory.SetWidth(renderer, 0.01f);
                renderer.positionCount = linePoints.Length;
                renderer.useWorldSpace = true;
            }

            public void Draw(Vector3 from, Vector3 to)
            {
                renderer.enabled = true;
                linePoints[0] = from;
                linePoints[1] = to;
                renderer.SetPositions(linePoints);
            }

            public void Off()
            {
                Debug.Log("GrabLine.Off()\n");
                linePoints[1] = linePoints[0];
                renderer.SetPositions(linePoints);
                renderer.enabled = false;
            }
        }
    }
}
