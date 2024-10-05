using SEE.GO;
using SEE.UI.Drawable;
using SEE.Utils;
using UnityEngine;

namespace SEE.Game.Drawable.ActionHelpers
{
    /// <summary>
    /// This class provides methods to enable and disable the collision detection.
    /// </summary>
    public static class CollisionDetectionManager
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
            if (obj != null)
            {
                if (obj.GetComponent<Rigidbody>() != null)
                {
                    Destroyer.Destroy(obj.GetComponent<Rigidbody>());
                }
                if (obj.GetComponent<CollisionController>() != null)
                {
                    Destroyer.Destroy(obj.GetComponent<CollisionController>());
                }
            }
        }
    }
}