using SEE.Game.Drawable;
using SEE.Game.Drawable.Configurations;
using UnityEngine;

namespace SEE.Game.UI.Drawable
{
    /// <summary>
    /// The controller for collisions between 
    /// <see cref="DrawableType"/> objects and a drawable border.
    /// </summary>
    public class CollisionController : MonoBehaviour
    {
        /// <summary>
        /// Attribut that represents that the object is in a collision.
        /// </summary>
        private bool isInCollision = false;

        /// <summary>
        /// Sets the isInCollision true if a collision occurs.
        /// Only the borders of the used Drawable can have a collision with the <see cref="DrawableType"/> object. 
        /// This case is necessary, for example, if a sticky note is placed on a whiteboard.
        /// </summary>
        /// <param name="other">The object that causes the collision.</param>
        private void OnTriggerEnter(Collider other)
        {
            if (GameFinder.GetHighestParent(gameObject).
                Equals(GameFinder.GetHighestParent(other.gameObject)))
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
            if (GameFinder.GetHighestParent(gameObject).
                Equals(GameFinder.GetHighestParent(other.gameObject)))
            {
                isInCollision = false;
            }
        }

        /// <summary>
        /// Gets the state of whether the object is in a collision.
        /// </summary>
        /// <returns>whether the object is in a collision</returns>
        public bool IsInCollision()
        {
            return isInCollision;
        }
    }
}