using System;
using System.Globalization;

namespace SEE.Utils
{
    /// <summary>
    /// Utilities for floats.
    /// </summary>
    internal static class FloatUtils
    {
        /// <summary>
        /// Tries to parse <paramref name="floatString"/> as a floating point number.
        /// Upon success, its value is returned in <paramref name="value"/> and true
        /// is returned. Otherwise false is returned and <paramref name="value"/>
        /// is undefined.
        /// </summary>
        /// <param name="floatString">String to be parsed for a floating point number.</param>
        /// <param name="value">Parsed floating point value; defined only if this method returns true.</param>
        /// <returns>True if a floating point number could be parsed successfully.</returns>
        public static bool TryGetFloat(string floatString, out float value)
        {
            try
            {
                value = float.Parse(floatString, CultureInfo.InvariantCulture.NumberFormat);
                return true;
            }
            catch (FormatException)
            {
                value = 0.0f;
                return false;
            }
        }

        /// <summary>
        /// Returns true if <paramref name="left"/> <= <paramref name="right"/> with
        /// some <paramref name="tolerance"/>. The tolerance accounts for imprecision
        /// in floating number representations.
        /// Mathematically, we are checking:
        /// <paramref name="left"/> <= <paramref name="right"/>  + <paramref name="tolerance"/>.
        /// </summary>
        /// <param name="left">Left operand of comparison.</param>
        /// <param name="right">Right operand of comparison.</param>
        /// <param name="tolerance">The tolerance of the comparison. 1e-5f is a common default tolerance (0.00001).</param>
        /// <returns>True if <paramref name="left"/> <= <paramref name="right"/> + <paramref name="tolerance"/>.</returns>
        //
        public static bool IsLessThanOrEqual(float left, float right, float tolerance = 1e-5f)
        {
            // This handles both "left < right" and "left is roughly equal to right"
            return left <= (right + tolerance);
        }
    }
}
