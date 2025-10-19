using SEE.Game.Drawable;
using SEE.Game.Drawable.Configurations;
using SEE.GO;
using UnityEngine;

namespace SEE.UI.Drawable
{
    /// <summary>
    /// The controller for collisions between
    /// <see cref="DrawableType"/> objects and a drawable border.
    /// </summary>
    public class CollisionController : MonoBehaviour
    {
        /// <summary>
        /// True if the object is in a collision.
        /// </summary>
        private bool isInCollision = false;

        /// <summary>
        /// Sets the <see cref="isInCollision"/> to true if a collision occurs.
        /// Only the borders of the used Drawable can have a collision with the <see cref="DrawableType"/> object.
        /// This case is necessary, for example, if a sticky note is placed on a whiteboard.
        /// </summary>
        /// <param name="other">The object that causes the collision.</param>
        private void OnTriggerEnter(Collider other)
        {
            if (gameObject.GetRootParent().
                Equals(other.gameObject.GetRootParent()))
            {
                isInCollision = true;
            }
        }

        /// <summary>
        /// Is executed as long as the objects are colliding.
        /// Ensures that the variable remains true.
        /// </summary>
        /// <param name="other">The object that causes the collision.</param>
        private void OnTriggerStay(Collider other)
        {
            if (gameObject.GetRootParent().
                Equals(other.gameObject.GetRootParent()))
            {
                isInCollision = true;
            }
        }

        /// <summary>
        /// Sets the isInCollision false when the collision is over.
        /// Only the borders of the used Drawable can resolve a collision with the <see cref="DrawableType"/> object.
        /// This case is necessary, for example, if a sticky note is placed on a whiteboard.
        /// </summary>
        /// <param name="other">The object that causes the collision.</param>
        private void OnTriggerExit(Collider other)
        {
            if (gameObject.GetRootParent().
                Equals(other.gameObject.GetRootParent()))
            {
                isInCollision = false;
            }
        }

        /// <summary>
        /// Returns true if the object is in a collision.
        /// </summary>
        /// <returns>whether the object is in a collision</returns>
        public bool IsInCollision()
        {
            return isInCollision;
        }

        /// <summary>
        /// Sets the <see cref="isInCollision"/> to false.
        /// It is necessary if a trigger exit was not properly registered.
        /// If, nevertheless, they are still involved in a collision, <see cref="OnTriggerStay"/> will set this to true again.
        /// </summary>
        public void SetCollisionToFalse()
        {
            isInCollision = false;
        }
    }
}