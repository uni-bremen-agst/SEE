using System;
using UnityEngine;

namespace SEE.Controls
{
    /// <summary>
    /// Selection of objects when the selection device does not offers positional data
    /// (e.g., a gamepad controller). In that case, we simply use the center of the
    /// viewport.
    /// </summary>
    [Obsolete("Use Selection2DAction instead.")]
    public class SelectionViewportAction : SelectionAction
    {
        /// <summary>
        /// Casts a ray from the MainCamera through the viewport selection.Direction (relative
        /// position on the screen) to hit a game object. Returns true if one was hit.
        /// </summary>
        /// <param name="hitInfo">additional information on the hit; defined only if this
        /// method returns true</param>
        /// <returns>true if an object was hit</returns>
        protected override bool Detect(out RaycastHit hitInfo)
        {
            return Physics.Raycast(GetRay(), out hitInfo, Mathf.Infinity, Physics.IgnoreRaycastLayer);
        }

        protected override Ray GetRay()
        {
            return MainCamera.ViewportPointToRay(selectionDevice.Direction);
        }
    }
}
