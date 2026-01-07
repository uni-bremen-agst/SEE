using System;

namespace SEE.Utils
{
    /// <summary>
    /// This class contains additional mathematical functions.
    /// <para>
    /// Individual methods may be copied from other sources like newer versions of dotnet framework.
    /// Please consult method descriptions for credits and license.
    /// </para>
    /// </summary>
    public static class SEEMath
    {
        /// <summary>
        /// Returns the largest value that compares less than a specified value.
        /// </summary>
        /// <param name="x">The value to decrement.</param>
        /// <returns>
        /// The largest value that compares less than <paramref name="x"/>.
        /// <para>
        /// -or-
        /// </para>
        /// <para>
        /// <see cref="float.NegativeInfinity"/> if <paramref name="x"/> equals <see cref="float.NegativeInfinity"/>.
        /// </para>
        /// <para>
        /// -or-
        /// </para>
        /// <para>
        /// <see cref="float.NaN"/> if <paramref name="x"/> is equals <see cref="float.NaN"/>.
        /// </para>
        /// </returns>
        /// <remarks>
        /// This method is part of .NET Runtime v8.0.11, licensed from the .NET Foundation under the MIT license:
        /// <para>
        /// https://github.com/dotnet/runtime/blob/v8.0.11/src/libraries/System.Private.CoreLib/src/System/MathF.cs
        /// </para>
        /// </remarks>
        public static float BitDecrement(float x)
        {
            int bits = BitConverter.SingleToInt32Bits(x);

            if ((bits & 0x7F800000) >= 0x7F800000)
            {
                // NaN returns NaN
                // -Infinity returns -Infinity
                // +Infinity returns float.MaxValue
                return (bits == 0x7F800000) ? float.MaxValue : x;
            }

            if (bits == 0x00000000)
            {
                // +0.0 returns -float.Epsilon
                return -float.Epsilon;
            }

            // Negative values need to be incremented
            // Positive values need to be decremented

            bits += ((bits < 0) ? +1 : -1);
            return BitConverter.Int32BitsToSingle(bits);
        }

        /// <summary>
        /// Returns the smallest value that compares greater than a specified value.
        /// </summary>
        /// <param name="x">The value to increment.</param>
        /// <returns>
        /// The smallest value that compares greater than <paramref name="x"/>.
        /// <para>
        /// -or-
        /// </para>
        /// <para>
        /// <see cref="float.PositiveInfinity"/> if <paramref name="x"/> equals <see cref="float.PositiveInfinity"/>.
        /// </para><para>
        /// -or-
        /// </para><para>
        /// <see cref="float.NaN"/> if <paramref name="x"/> is equals <see cref="float.NaN"/>.
        /// </para>
        /// </returns>
        /// <remarks>
        /// This method is part of .NET Runtime v8.0.11, licensed from the .NET Foundation under the MIT license:
        /// <para>
        /// https://github.com/dotnet/runtime/blob/v8.0.11/src/libraries/System.Private.CoreLib/src/System/MathF.cs
        /// </para>
        /// </remarks>
        public static float BitIncrement(float x)
        {
            int bits = BitConverter.SingleToInt32Bits(x);

            if ((bits & 0x7F800000) >= 0x7F800000)
            {
                // NaN returns NaN
                // -Infinity returns float.MinValue
                // +Infinity returns +Infinity
                return (bits == unchecked((int)(0xFF800000))) ? float.MinValue : x;
            }

            if (bits == unchecked((int)(0x80000000)))
            {
                // -0.0 returns float.Epsilon
                return float.Epsilon;
            }

            // Negative values need to be decremented
            // Positive values need to be incremented

            bits += ((bits < 0) ? -1 : +1);
            return BitConverter.Int32BitsToSingle(bits);
        }
    }
}
