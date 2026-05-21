using SEE.Utils.Config;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.Game
{
    /// <summary>
    ///  A discrete range of numberOfColors colors from lower to upper.
    /// </summary>
    [Serializable]
    public struct ColorRange
    {
        /// <summary>
        /// First color in the range.
        /// </summary>
        [SerializeField]
        public Color Lower;
        /// <summary>
        /// Last color in the range.
        /// </summary>
        [SerializeField]
        public Color Upper;
        /// <summary>
        /// The number of colors in the range.
        /// </summary>
        [SerializeField]
        public uint NumberOfColors;

        public ColorRange(Color lower, Color upper, uint numberOfColors)
        {
            Lower = lower;
            Upper = upper;
            NumberOfColors = numberOfColors;
        }

        public ColorRange(Color color)
        {
            Lower = color;
            Upper = color;
            NumberOfColors = 1;
        }

        /// <summary>
        /// The default color range that we use if the we cannot find an
        /// explicit setting by the user.
        /// </summary>
        /// <returns>Default color range.</returns>
        public static ColorRange Default()
        {
            return new ColorRange(Color.white, Color.black, 10);
        }

        private const string lowerLabel = "Lower";
        private const string upperLabel = "Upper";
        private const string numberOfColorsLabel = "NumberOfColors";

        /// <summary>
        /// Saves this <see cref="ColorRange"/> using <paramref name="writer"/> under the given <paramref name="label"/>.
        /// </summary>
        /// <param name="writer">Used to emit the settings.</param>
        /// <param name="label">The label under which to emit the settings.</param>
        internal void Save(ConfigWriter writer, string label)
        {
            writer.BeginGroup(label);
            writer.Save(Lower, lowerLabel);
            writer.Save(Upper, upperLabel);
            writer.Save((int)NumberOfColors, numberOfColorsLabel);
            writer.EndGroup();
        }

        /// <summary>
        /// Restores the label settings based on the values of the entry in <paramref name="attributes"/>
        /// via key <paramref name="label"/>. If there is no such label, nothing happens. If any of the
        /// values is missing, the original value will be kept.
        /// </summary>
        /// <param name="attributes">Where to look up the values.</param>
        /// <param name="label">The key for the lookup.</param>
        /// <returns>True if this <see cref="ColorRange"/> could be restored.</returns>
        internal bool Restore(Dictionary<string, object> attributes, string label)
        {
            if (attributes.TryGetValue(label, out object dictionary))
            {
                Dictionary<string, object> values = dictionary as Dictionary<string, object>;
                {
                    ConfigIO.Restore(values, lowerLabel, ref Lower);
                    ConfigIO.Restore(values, upperLabel, ref Upper);
                    long storedNumberOfColors = 0;
                    if (ConfigIO.Restore(values, numberOfColorsLabel, ref storedNumberOfColors))
                    {
                        NumberOfColors = (uint)storedNumberOfColors;
                    }
                    return true;
                }
            }
            return false;
        }
    }
}