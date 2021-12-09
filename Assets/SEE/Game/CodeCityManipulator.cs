using UnityEngine;

namespace SEE.Game
{
    internal static class CodeCityManipulator
    {
        public static void Set(Transform transform, Vector3 position, float yAngle)
        {
            transform.position = position;
            transform.rotation = Quaternion.Euler(0.0f, yAngle, 0.0f);
        }

        /// <summary>
        /// Rotates the transform about axis passing through point in world coordinates by angle degrees.
        /// </summary>
        public static void RotateAround(Transform transform, Vector3 point, float yAngle)
        {

        }
    }
}
