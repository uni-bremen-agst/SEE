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
        /// <param name="floatString">string to be parsed for a floating point number</param>
        /// <param name="value">parsed floating point value; defined only if this method returns true</param>
        /// <returns>true if a floating point number could be parsed successfully</returns>
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
    }
}
