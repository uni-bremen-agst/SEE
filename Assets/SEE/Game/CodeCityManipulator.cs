using UnityEngine;

namespace SEE.Game
{
    internal static class CodeCityManipulator
    {
        internal static void Set(Transform transform, Vector3 position, float yAngle)
        {
            transform.position = position;
            transform.rotation = Quaternion.Euler(0.0f, yAngle, 0.0f);
        }

        internal static void Set(Transform transform, Vector3 position)
        {
            transform.position = position;
        }
    }
}
