using UnityEngine;

namespace SEE
{
    /// <summary>
    /// Represents a table on which the city can be placed.
    /// </summary>
    public class Table : MonoBehaviour
    {
        /// <summary>
        /// The min surface position of the table on the x-axis.
        /// </summary>
        public const float MinX = -0.8f;

        /// <summary>
        /// The max surface position of the table on the x-axis.
        /// </summary>
        public const float MaxX = 0.8f;

        /// <summary>
        /// The center of the table on the x-axis.
        /// </summary>
        public const float CenterX = (MinX + MaxX) / 2;



        /// <summary>
        /// The min surface position of the table on the y-axis.
        /// </summary>
        public const float MinY = 0.0f;

        /// <summary>
        /// The max surface position of the table on the y-axis.
        /// </summary>
        public const float MaxY = 1.0f;

        /// <summary>
        /// The center of the table on the y-axis.
        /// </summary>
        public const float CenterY = (MinY + MaxY) / 2;



        /// <summary>
        /// The min surface position of the table on the z-axis.
        /// </summary>
        public const float MinZ = -0.5f;

        /// <summary>
        /// The max surface position of the table on the z-axis.
        /// </summary>
        public const float MaxZ = 0.5f;

        /// <summary>
        /// The center of the table on the z-axis.
        /// </summary>
        public const float CenterZ = (MinZ + MaxZ) / 2;



        public static readonly Vector3 Min = new Vector3(MinX, MinY, MinZ);
        public static readonly Vector2 MinXY = new Vector2(MinX, MinY);
        public static readonly Vector2 MinXZ = new Vector2(MinX, MinZ);
        public static readonly Vector2 MinYZ = new Vector2(MinY, MinZ);
        public static readonly Vector3 Max = new Vector3(MaxX, MaxY, MaxZ);
        public static readonly Vector2 MaxXY = new Vector2(MaxX, MaxY);
        public static readonly Vector2 MaxXZ = new Vector2(MaxX, MaxZ);
        public static readonly Vector2 MaxYZ = new Vector2(MaxY, MaxZ);
        public static readonly Vector3 Center = new Vector3(CenterX, CenterY, CenterZ);
        public static readonly Vector2 CenterXY = new Vector2(CenterX, CenterY);
        public static readonly Vector2 CenterXZ = new Vector2(CenterX, CenterZ);
        public static readonly Vector2 CenterYZ = new Vector2(CenterY, CenterZ);



        /// <summary>
        /// The width of the table (x-axis).
        /// </summary>
        public const float Width = MaxX - MinX;

        /// <summary>
        /// The height of the table (y-axis);
        /// </summary>
        public const float Height = 1.0f;

        /// <summary>
        /// The depth of the table (z-axis).
        /// </summary>
        public const float Depth = MaxZ - MinZ;



        /// <summary>
        /// The max size of the table in either x or z direction.
        /// </summary>
        public const float MaxDimXZ = Width > Depth ? Width : Depth;

        /// <summary>
        /// The min size of the table in either x or z direction.
        /// </summary>
        public const float MinDimXZ = Width < Depth ? Width : Depth;

        // TODO(torben): determine this more dynamic? also, add height
        
        /// <summary>
        /// The instance of the table.
        /// </summary>
        public static Table Instance { get; private set; }
        public static Vector3 TableTopCenter { get => Instance.transform.position + new Vector3(0.0f, MaxY, 0.0f); }
        public static Vector3 TableTopCenterEpsilon { get => Instance.transform.position + new Vector3(0.0f, MaxY + float.Epsilon, 0.0f); }

        private void Awake()
        {
            if (Instance != null)
            {
                Debug.LogWarning("There is more than one table!");
            }
            Instance = this;
        }
    }
}
