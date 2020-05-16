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
    }
}
