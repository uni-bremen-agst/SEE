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
        /// Returns true if <paramref name="left"/> and <paramref name="right"/> are
        /// equal to within floating-point precision, that is, if they differ by less
        /// than a millionth of the larger of the two magnitudes.
        ///
        /// This is <c>UnityEngine.Mathf.Approximately</c> without the dependency on
        /// Unity, and computes the same expression. The only difference concerns
        /// the floor that keeps the comparison meaningful near zero: Unity raises it
        /// from the smallest denormal to the smallest normal float on a platform
        /// that flushes denormals to zero, which matters solely for two values that
        /// are both within about 1e-38 of zero.
        /// </summary>
        /// <param name="left">Left operand of the comparison.</param>
        /// <param name="right">Right operand of the comparison.</param>
        /// <returns>True if the two are equal to within floating-point precision.</returns>
        public static bool Approximately(float left, float right)
        {
            return Math.Abs(right - left)
                   < Math.Max(1E-06f * Math.Max(Math.Abs(left), Math.Abs(right)),
                              float.Epsilon * 8f);
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
        /// <param name="tolerance">The tolerance of the comparison. 1e-5f is a common default tolerance (0.00001).
        /// Must not be negative.</param>
        /// <returns>True if <paramref name="left"/> <= <paramref name="right"/> + <paramref name="tolerance"/>.</returns>
        /// <exception cref="ArgumentException">Thrown if <paramref name="tolerance"/> is negative.</exception>
        public static bool IsLessThanOrEqualWithinTolerance(float left, float right, float tolerance = 1e-5f)
        {
            if (tolerance < 0)
            {
                throw new ArgumentException($"{nameof(tolerance)} must not be negative.");
            }
            // This handles both "left < right" and "left is roughly equal to right"
            return left <= (right + tolerance);
        }
    }
}
