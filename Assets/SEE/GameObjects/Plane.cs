using UnityEngine;

namespace SEE.GO
{
    /// <summary>
    /// Represents a plane on which the city objects can be placed defined as
    /// follows:
    ///    (MinX, MinZ) denotes the left front corner
    ///    (MaxX, MaxZ) denotes the right back corner
    ///    YPosition denotes the y co-ordinate of the plane
    /// </summary>
    public class Plane : MonoBehaviour
    {
        /// <summary>
        /// The y offset of the plane when method <see cref="CenterTop"/> is called.
        /// </summary>
        [HideInInspector]
        public float HeightOffset = 0.0f;

        /// <summary>
        /// Returns the scale in Unity units (lossy scale of the transform).
        /// </summary>
        /// <returns>Scale in Unity units after correction.</returns>
        private Vector3 GetScale()
        {
            return transform.lossyScale;
        }

        /// <summary>
        /// The left front corner of the plane.
        /// Note: the y co-ordinate of the resulting Vector2 denotes a value in the z axis.
        /// </summary>
        public Vector2 LeftFrontCorner
        {
            get
            {
                Vector3 position = transform.position;
                Vector3 scale = GetScale();
                float minX = position.x - scale.x / 2.0f;
                float minZ = position.z - scale.z / 2.0f;
                return new Vector2(minX, minZ);
            }
        }

        /// <summary>
        /// The right back corner of the plane.
        /// Note: the y co-ordinate of the resulting Vector2 denotes a value in the z axis.
        /// </summary>
        public Vector2 RightBackCorner
        {
            get
            {
                Vector3 position = transform.position;
                Vector3 scale = GetScale();
                float maxX = position.x + scale.x / 2.0f;
                float maxZ = position.z + scale.z / 2.0f;
                return new Vector2(maxX, maxZ);
            }
        }

        /// <summary>
        /// The center point of the plane in the x/z plane.
        /// </summary>
        public Vector2 CenterXZ
        {
            get
            {
                Vector3 position = transform.position;
                return new Vector2(position.x, position.z);
            }
        }

        /// <summary>
        /// The min length of the plane in either x or z direction.
        /// </summary>
        public float MinLengthXZ
        {
            get
            {
                Vector3 scale = GetScale();
                return scale.x < scale.z ? scale.x : scale.z;
            }
        }

        /// <summary>
        /// The center of the plane's roof (plus some very small y delta, namely, <see cref="HeightOffset"/>).
        /// </summary>
        public Vector3 CenterTop
        {
            get
            {
                return transform.position + new Vector3(0.0f, HeightOffset, 0.0f);
            }
        }
    }
}
