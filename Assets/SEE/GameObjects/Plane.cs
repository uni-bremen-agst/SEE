using UnityEngine;

namespace SEE
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
        /// The left front corner of the plane.
        /// Note: the y co-ordinate of the resulting Vector2 denotes a value in the z axis.
        /// </summary>
        public static Vector2 LeftFrontCorner
        {
            get
            {
                Vector3 position = Instance.gameObject.transform.position;
                Vector3 scale = Instance.gameObject.transform.lossyScale;
                float MinX = position.x - scale.x / 2.0f;
                float MinZ = position.z - scale.z / 2.0f;
                return new Vector2(MinX, MinZ);
            }
        }

        /// <summary>
        /// The right back corner of the plane.
        /// Note: the y co-ordinate of the resulting Vector2 denotes a value in the z axis.
        /// </summary>
        public static Vector2 RightBackCorner
        {
            get
            {
                Vector3 position = Instance.gameObject.transform.position;
                Vector3 scale = Instance.gameObject.transform.lossyScale;
                float MaxX = position.x + scale.x / 2.0f;
                float MaxZ = position.z + scale.z / 2.0f;
                return new Vector2(MaxX, MaxZ);
            }
        }

        /// <summary>
        /// The center point of the plane in the x/z plane.
        /// </summary>
        public static Vector2 CenterXZ
        {
            get
            {
                Vector3 position = Instance.gameObject.transform.position;
                return new Vector2(position.x, position.z);
            }
        }

        /// <summary>
        /// The min length of the plane in either x or z direction.
        /// </summary>
        public static float MinLengthXZ
        {
            get
            {
                Vector3 scale = Instance.gameObject.transform.lossyScale;
                return scale.x < scale.z ? scale.x : scale.z;
            }
        }
        
        /// <summary>
        /// The instance of the plane.
        /// </summary>
        public static Plane Instance { get; private set; }

        /// <summary>
        /// The center of the plane's roof (plus some very small y delta).
        /// </summary>
        public static Vector3 CenterTop
        {
            get
            {
                Vector3 scale = Instance.transform.lossyScale;
                return Instance.transform.position + new Vector3(0.0f, scale.y / 2.0f + float.Epsilon, 0.0f);
            }
        }

        private void Awake()
        {
            if (Instance != null)
            {
                Debug.LogWarning("There is more than one plane!\n");
            }
            Instance = this;
        }
    }
}
