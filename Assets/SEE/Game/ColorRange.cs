using SEE.Utils;
using System.Collections.Generic;
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
            NumberOfColors = numberOfColors;
        }

        public ColorRange(Color color)
        {
            lower = color;
            upper = color;
            NumberOfColors = 1;
        }

        private const string LowerLabel = "Lower";
        private const string UpperLabel = "Upper";
        private const string NumberOfColorsLabel = "NumberOfColors";

        /// <summary>
        /// Saves this <see cref="ColorRange"/> using <paramref name="writer"/> under the given <paramref name="label"/>.
        /// </summary>
        /// <param name="writer">used to emit the settings</param>
        /// <param name="label">the label under which to emit the settings</param>
        internal void Save(ConfigWriter writer, string label)
        {
            writer.BeginGroup(label);
            writer.Save(lower, LowerLabel);
            writer.Save(upper, UpperLabel);
            writer.Save((int)NumberOfColors, NumberOfColorsLabel);
            writer.EndGroup();
        }

        /// <summary>
        /// Restores the label settings based on the values of the entry in <paramref name="attributes"/>
        /// via key <paramref name="label"/>. If there is no such label, nothing happens. If any of the
        /// values is missing, the original value will be kept.
        /// </summary>
        /// <param name="attributes">where to look up the values</param>
        /// <param name="label">the key for the lookup</param>
        internal void Restore(Dictionary<string, object> attributes, string label)
        {
            if (attributes.TryGetValue(label, out object dictionary))
            {
                Dictionary<string, object> values = dictionary as Dictionary<string, object>;
                {
                    ConfigIO.Restore(values, LowerLabel, ref lower);
                    ConfigIO.Restore(values, UpperLabel, ref upper);
                    long storedNumberOfColors = 0;
                    if (ConfigIO.Restore(values, NumberOfColorsLabel, ref storedNumberOfColors))
                    {
                        NumberOfColors = (uint)storedNumberOfColors;
                    }
                }
            }
        }
    }
}