using UnityEngine;

namespace SEE.Game.Drawable
{
    /// <summary>
    /// This class scales an object.
    /// </summary>
    public static class GameScaler
    {
        /// <summary>
        /// Scales the object by a scale factor.
        /// </summary>
        /// <param name="objectToScale">The drawable type object that should be scaled.</param>
        /// <param name="scaleFactor">The current chosen scale factor.</param>
        /// <returns>The new scale of the object</returns>
        public static Vector3 Scale(GameObject objectToScale, float scaleFactor)
        {
            Transform transform = objectToScale.transform;

            /// Calculates and sets the new scale for the object.
            Vector3 newScale = transform.localScale * scaleFactor;
            newScale.z = 1f;
            transform.localScale = newScale;

            /// After a line was scaled, the mesh collider needs to be refreshed,
            /// because it is not scaled along with it.
            if (objectToScale.CompareTag(Tags.Line))
            {
                GameDrawer.RefreshCollider(objectToScale);
            }
            return newScale;
        }

        /// <summary>
        /// This method sets a new scale to a drawable type object.
        /// </summary>
        /// <param name="objectToScale">The drawable type object that should be scaled.</param>
        /// <param name="scale">The new scale for the object.</param>
        public static void SetScale(GameObject objectToScale, Vector3 scale)
        {
            Transform transform = objectToScale.transform;
            transform.localScale = scale;

            /// After a line was scaled, the mesh collider needs to be refreshed,
            /// because it is not scaled along with it.
            if (objectToScale.CompareTag(Tags.Line))
            {
                GameDrawer.RefreshCollider(objectToScale);
            }
        }
    }
}