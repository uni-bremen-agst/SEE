using SEE.Utils;
using UnityEngine;

namespace SEE.Game.Table
{
    /// <summary>
    /// This class manages collision detection for a universal table.
    /// </summary>
	public class CollisionDetectionManager : MonoBehaviour
	{
        /// <summary>
        /// Indicates whether the trigger option was enabled.
        /// </summary>
		private bool changedTrigger = false;

        /// <summary>
        /// Indicates whether an <see cref="Rigidbody"/> was added.
        /// </summary>
        private bool addRigidbody = false;

        /// <summary>
        /// Indicates whether the object is colliding with another object.
        /// </summary>
		private bool isColliding = false;

        /// <summary>
        /// Activates the trigger option of the collider and adds a <see cref="Rigidbody"/>.
        /// </summary>
        private void Awake()
        {
            Collider collider = gameObject.GetComponentInChildren<Collider>();
            if (collider != null && !collider.isTrigger)
            {
                collider.isTrigger = true;
                changedTrigger = true;
            }
            if (!gameObject.TryGetComponent<Rigidbody>(out Rigidbody rb))
            {
                Rigidbody rigidbody = gameObject.AddComponent<Rigidbody>();
                rigidbody.isKinematic = true;
                addRigidbody = true;
            }
        }

        /// <summary>
        /// Removes the changes made in <see cref="Awake"/>.
        /// </summary>
        private void OnDestroy()
        {
            Collider collider = gameObject.GetComponentInChildren<Collider>();
            if (collider != null && changedTrigger)
            {
                collider.isTrigger = false;
            }
			if (gameObject.TryGetComponent<Rigidbody>(out Rigidbody rb)
				&& addRigidbody)
			{
				Destroyer.Destroy(rb);
			}
        }

        /// <summary>
        /// Sets the collision status indicator.
        /// </summary>
        /// <param name="other">The colliding object.</param>
        private void OnTriggerEnter(Collider other)
        {
			isColliding = true;
        }

        /// <summary>
        /// Sets the collision status indicator.
        /// </summary>
        /// <param name="other">The colliding object.</param>
        private void OnTriggerStay(Collider other)
        {
			isColliding = true;
        }

        /// <summary>
        /// Resets the collision status indicator.
        /// </summary>
        /// <param name="other">The colliding object.</param>
        private void OnTriggerExit(Collider other)
        {
			isColliding = false;
        }

        /// <summary>
        /// Indicates whether the object is colliding with another object.
        /// </summary>
        /// <returns>if the object is colliding with another object.</returns>
		public bool IsInCollision()
		{
			return isColliding;
		}
    }
}