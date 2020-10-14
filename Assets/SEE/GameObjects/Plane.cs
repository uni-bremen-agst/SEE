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
        /// The scale of the objects may not always be in Unity units (for Unity's
        /// primitive Cubes they are, but for other objects provided by third parties
        /// and also by Unity's plane they are not) in which case the scale needs to be
        /// multiplied by a correction factor to get the value in Unity units.
        /// </summary>
        [Tooltip("The correction factor by which to scale to the true lengths in Unity units.")]
        public float ScaleFactor = 1.0f;

        [HideInInspector]
        public float HeightOffset = 0.0f;

        /// <summary>
        /// Returns the scale in Unity units by applying the ScaleFactor to the lossy scale
        /// of the transform.
        /// </summary>
        /// <returns>scale in Unity units after correction</returns>
        private Vector3 GetScale()
        {
            return transform.lossyScale * ScaleFactor;
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
                float MinX = position.x - scale.x / 2.0f;
                float MinZ = position.z - scale.z / 2.0f;
                return new Vector2(MinX, MinZ);
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
                float MaxX = position.x + scale.x / 2.0f;
                float MaxZ = position.z + scale.z / 2.0f;
                return new Vector2(MaxX, MaxZ);
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
        /// The center of the plane's roof (plus some very small y delta).
        /// </summary>
        public Vector3 CenterTop
        {
            get
            {
                Vector3 scale = GetScale();
                return transform.position + new Vector3(0.0f, HeightOffset, 0.0f);
            }
        }
    }
}
