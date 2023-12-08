using SEE.Game.Drawable;
using UnityEngine;

namespace Assets.SEE.Game.UI.Drawable
{
    /// <summary>
    /// The controller for collisions between 
    /// drawable type objects and a drawable border.
    /// </summary>
    public class CollisionController : MonoBehaviour
    {
        /// <summary>
        /// Attribut that represents that the object is in a collision.
        /// </summary>
        private bool isInCollision = false;

        /// <summary>
        /// Sets the isInCollision true if a collision occurs.
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
        /// </summary>
        /// <param name="other">The object that causes the collision.</param>
        private void OnTriggerExit(Collider other)
        {
            if(GameFinder.GetHighestParent(gameObject).
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