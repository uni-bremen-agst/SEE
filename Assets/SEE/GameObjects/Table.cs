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



        /// <summary>
        /// The width of the table (x-axis).
        /// </summary>
        public const float Width = MaxX - MinX;

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
    }
}
