using UnityEngine;

namespace SEE.Game
{
    /// <summary>
    ///  A discrete range of numberOfColors colors from lower to upper.
    /// </summary>
    public struct ColorRange
    {
        /// <summary>
        /// First color in the range.
        /// </summary>
        public Color lower;
        /// <summary>
        /// Last color in the range.
        /// </summary>
        public Color upper;
        /// <summary>
        /// The number of colors in the range.
        /// </summary>
        public uint NumberOfColors;

        public ColorRange(Color lower, Color upper, uint numberOfColors)
        {
            this.lower = lower;
            this.upper = upper;
            this.NumberOfColors = numberOfColors;
        }
    }
}