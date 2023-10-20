using SEE.Game;
using System.Collections;
using UnityEngine;

namespace Assets.SEE.Game.Drawable
{
    /// <summary>
    /// This class scales a drawable type object
    /// </summary>
    public static class GameScaler
    {
        /// <summary>
        /// Scales the object by a scale factor.
        /// </summary>
        /// <param name="objectToScale">The drawable type object that should be scaled.</param>
        /// <param name="scaleFactor">The current chosen scale factor.</param>
        /// <returns></returns>
        public static Vector3 Scale(GameObject objectToScale, float scaleFactor)
        {
            Transform transform = objectToScale.transform;
            Vector3 newScale = transform.localScale * scaleFactor;
            newScale.z = 1f;
            transform.localScale = newScale;
            if (objectToScale.CompareTag(Tags.Line))
            {
                GameDrawer.RefreshCollider(objectToScale);
            }
            return newScale;
        }

        /// <summary>
        /// This method set's a new scale to a drawable type object.
        /// </summary>
        /// <param name="objectToScale">The drawable type object that should be scaled.</param>
        /// <param name="scale">The new scale for the object.</param>
        public static void SetScale(GameObject objectToScale, Vector3 scale)
        {
            Transform transform = objectToScale.transform;
            transform.localScale = scale;
            if (objectToScale.CompareTag(Tags.Line))
            {
                GameDrawer.RefreshCollider(objectToScale);
            }
        }
    }
}