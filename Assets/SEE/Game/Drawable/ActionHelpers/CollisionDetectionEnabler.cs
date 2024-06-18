using SEE.GO;
using SEE.UI.Drawable;
using SEE.Utils;
using UnityEngine;

namespace Assets.SEE.Game.Drawable.ActionHelpers
{
    /// <summary>
    /// This class provides methods to enable the collision detection and disable them.  
    /// </summary>
    public static class CollisionDetectionEnabler
    {
        /// <summary>
        /// Enables the collision detection for the given object.
        /// </summary>
        /// <param name="obj">The object for which collision detection should be activated.</param>
        public static void Enable(GameObject obj)
        {
            obj.AddOrGetComponent<Rigidbody>().isKinematic = true;
            obj.AddOrGetComponent<CollisionController>();
        }

        /// <summary>
        /// Disables collision detection for the given object.
        /// </summary>
        /// <param name="obj">The object for which collision detection should be deactivated.</param>
        public static void Disable(GameObject obj) 
        {
            Destroyer.Destroy(obj.GetComponent<Rigidbody>());
            Destroyer.Destroy(obj.GetComponent<CollisionController>());
        }
    }
}