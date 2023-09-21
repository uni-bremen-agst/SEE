using SEE.Game;
using System.Collections;
using UnityEngine;

namespace Assets.SEE.Game.Drawable
{
    public static class GameScaler
    {
        public static Vector3 Scale(GameObject objectToScale, float scaleFactor)
        {
            Transform transform;
            if (objectToScale.CompareTag(Tags.Line)) {
                transform = objectToScale.transform.parent.transform;
            } else
            {
                transform = objectToScale.transform;
            }
            Vector3 newScale = transform.localScale * scaleFactor;
            newScale.z = 1f;
            transform.localScale = newScale;
            if (objectToScale.CompareTag(Tags.Line))
            {
                GameDrawer.RefreshCollider(objectToScale);
            }
            return newScale;
        }

        public static void SetScale(GameObject objectToScale, Vector3 scale)
        {
            Transform transform;
            if (objectToScale.CompareTag(Tags.Line))
            {
                transform = objectToScale.transform.parent.transform;
            }
            else
            {
                transform = objectToScale.transform;
            }
            transform.localScale = scale;
            if (objectToScale.CompareTag(Tags.Line))
            {
                GameDrawer.RefreshCollider(objectToScale);
            }
        }
    }
}