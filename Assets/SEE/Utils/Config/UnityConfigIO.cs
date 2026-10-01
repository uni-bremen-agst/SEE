using System;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.Utils.Config
{
    internal static class UnityConfigIO
    {
        /// <summary>
        /// Looks up the <paramref name="value"/> in <paramref name="attributes"/> using the
        /// key <paramref name="label"/>. If no such <paramref name="label"/> exists, false
        /// is returned and <paramref name="value"/> remains unchanged. Otherwise <paramref name="value"/>
        /// receives the looked up value. Note that only those parts of the color (red, green, blue,
        /// alpha) will be updated in <paramref name="value"/> that are actually found in <paramref name="attributes"/>;
        /// all others remain unchanged.
        ///
        /// Note: This method is intended specifically for Color. For enums use <see cref="RestoreEnum()"/>
        /// and for all other types, use <see cref="Restore{T}()"/> instead.
        /// </summary>
        /// <param name="attributes">Where to look up the <paramref name="label"/>.</param>
        /// <param name="label">The label to look up.</param>
        /// <param name="value">The value of the looked up <paramref name="label"/> if the <paramref name="label"/>
        /// exists.</param>
        /// <returns>True if the <paramref name="label"/> was found.</returns>
        internal static bool RestoreColor(Dictionary<string, object> attributes, string label, ref Color value)
        {
            if (attributes.TryGetValue(label, out object dictionary))
            {
                if (dictionary is not Dictionary<string, object> values)
                {
                    throw new InvalidCastException($"Types are not assignment compatible for attribute {label}. Expected type: Dictionary<string, float>. Actual type: {dictionary.GetType()}");
                }
                if (values.TryGetValue(RedLabel, out object red))
                {
                    value.r = (float)red;
                }
                if (values.TryGetValue(GreenLabel, out object green))
                {
                    value.g = (float)green;
                }
                if (values.TryGetValue(BlueLabel, out object blue))
                {
                    value.b = (float)blue;
                }
                if (values.TryGetValue(AlphaLabel, out object alpha))
                {
                    value.a = (float)alpha;
                }
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Writes <paramref name="label"/> and its <paramref name="color"/> to <see cref="stream"/>
        /// as a composite value of its constituents (Red, Green, Blue, Alpha).
        /// </summary>
        /// <param name="label">Label to be emitted.</param>
        /// <param name="color">Value to be emitted.</param>
        internal static void Save(this ConfigWriter writer, Color color, string label = "")
        {
            writer.BeginGroup(label);
            writer.Save(color.r, RedLabel);
            writer.Save(color.g, GreenLabel);
            writer.Save(color.b, BlueLabel);
            writer.Save(color.a, AlphaLabel);
            writer.EndGroup();
        }

        /// <summary>
        /// Label for the red part of a color.
        /// </summary>
        private const string RedLabel = "Red";
        /// <summary>
        /// Label for the green part of a color.
        /// </summary>
        private const string GreenLabel = "Green";
        /// <summary>
        /// Label for the blue part of a color.
        /// </summary>
        private const string BlueLabel = "Blue";
        /// <summary>
        /// Label for the alpha part (transparency) of a color.
        /// </summary>
        private const string AlphaLabel = "Alpha";
    }
}
