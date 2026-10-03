using SEE.Graphs.Config;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.Utils.Config
{
    internal static class UnityConfigIO
    {
        #region Color

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
        internal static void SaveColor(this ConfigWriter writer, Color color, string label = "")
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

        #endregion Color

        #region Vector3

        /// <summary>
        /// Writes <paramref name="label"/> and its <paramref name="vector"/> to <see cref="stream"/>
        /// as a composite value of its constituents (X, Y, Z).
        /// </summary>
        /// <param name="label">Label to be emitted.</param>
        /// <param name="vector">Value to be emitted.</param>
        internal static void SaveVector(this ConfigWriter configWriter, Vector3 vector, string label = "")
        {
            configWriter.BeginGroup(label);
            configWriter.Save(vector.x, XLabel);
            configWriter.Save(vector.y, YLabel);
            configWriter.Save(vector.z, ZLabel);
            configWriter.EndGroup();
        }

        /// <summary>
        /// Looks up the <paramref name="value"/> in <paramref name="attributes"/> using the key <paramref name="label"/>.
        /// If no such <paramref name="label"/> exists, false is returned and <paramref name="value"/> remains unchanged.
        /// Otherwise, <paramref name="value"/> receives the looked up value. Only those parts of the Vector3 (x, y, z)
        /// will be updated in <paramref name="value"/> that are actually found in <paramref name="attributes"/>; all others remain unchanged.
        /// </summary>
        /// <param name="attributes">The dictionary where to look up the <paramref name="label"/>.</param>
        /// <param name="label">The label/key to look up in <paramref name="attributes"/>.</param>
        /// <param name="value">The Vector3 that will be updated if <paramref name="label"/> exists.</param>
        /// <returns>True if the <paramref name="label"/> was found; otherwise false.</returns>
        /// <exception cref="InvalidCastException">
        /// Thrown if the value found in <paramref name="attributes"/> is not a <see cref="Dictionary{String, Object}"/>.
        /// </exception>
        /// <remarks>
        /// This method is intended specifically for Vector3 values. For enums, use <see cref="RestoreEnum()"/>,
        /// and for all other types, use <see cref="Restore{T}()"/> instead.
        /// </remarks>
        internal static bool RestoreVector(Dictionary<string, object> attributes, string label, ref Vector3 value)
        {
            if (TryRestoreVector3(attributes, label, out Vector3 temp))
            {
                value = temp;
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Looks up a Vector3 value in <paramref name="attributes"/> using the key <paramref name="label"/>.
        /// If the label exists, the <paramref name="setter"/> is invoked with the restored value. Only those parts
        /// of the Vector3 (x, y, z) that are present in <paramref name="attributes"/> will be updated; all others remain unchanged.
        /// </summary>
        /// <param name="attributes">The dictionary where to look up the <paramref name="label"/>.</param>
        /// <param name="label">The label/key to look up in <paramref name="attributes"/>.</param>
        /// <param name="setter">An action to apply the restored Vector3 value (e.g., a property setter).</param>
        /// <returns>True if the <paramref name="label"/> was found; otherwise false.</returns>
        /// <exception cref="InvalidCastException">
        /// Thrown if the value found in <paramref name="attributes"/> is not a <see cref="Dictionary{String, Object}"/>.
        /// </exception>
        /// <remarks>
        /// This variant allows direct use with properties or methods that encapsulate setting logic,
        /// such as Unity objects with local or world-space transformations.
        /// </remarks>
        internal static bool RestoreVector(Dictionary<string, object> attributes, string label, Action<Vector3> setter)
        {
            if (TryRestoreVector3(attributes, label, out Vector3 temp))
            {
                setter(temp);
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Core helper method that extracts a Vector3 from <paramref name="attributes"/> using <paramref name="label"/>.
        /// The values of x, y, and z are updated if found in the <paramref name="attributes"/>; missing components will be zero.
        /// </summary>
        /// <param name="attributes">The dictionary to look up the label in.</param>
        /// <param name="label">The key representing the Vector3 in <paramref name="attributes"/>.</param>
        /// <param name="result">The Vector3 extracted from <paramref name="attributes"/>.</param>
        /// <returns>True if the label was found and <paramref name="result"/> was populated; otherwise false.</returns>
        /// <exception cref="InvalidCastException">
        /// Thrown if the value found in <paramref name="attributes"/> is not a <see cref="Dictionary{String, Object}"/>.
        /// </exception>
        /// <remarks>
        /// This method is intended as a common implementation for both the ref and Action variants of Restore.
        /// </remarks>
        private static bool TryRestoreVector3(Dictionary<string, object> attributes, string label, out Vector3 result)
        {
            result = Vector3.zero;
            if (!attributes.TryGetValue(label, out object dictionary))
            {
                return false;
            }
            if (dictionary is not Dictionary<string, object> values)
            {
                throw new InvalidCastException($"Types are not assignment compatible for attribute {label}. Expected type: Dictionary<string, float>. Actual type: {dictionary.GetType()}");
            }
            if (values.TryGetValue(XLabel, out object x))
            {
                result.x = (float)x;
            }
            if (values.TryGetValue(YLabel, out object y))
            {
                result.y = (float)y;
            }
            if (values.TryGetValue(ZLabel, out object z))
            {
                result.z = (float)z;
            }
            return true;
        }

        /// <summary>
        /// Label for the X coordinate of a Vector3.
        /// </summary>
        private const string XLabel = "X";

        /// <summary>
        /// Label for the Y coordinate of a Vector3.
        /// </summary>
        private const string YLabel = "Y";

        /// <summary>
        /// Label for the Z coordinate of a Vector3.
        /// </summary>
        private const string ZLabel = "Z";

        #endregion Vector3
    }
}
