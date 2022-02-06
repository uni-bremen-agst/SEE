namespace Dissonance.Extensions
{
    internal static class UShortExtensions
    {
        /// <summary>
        /// Wrapping delta for 2 bit numbers
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        internal static int WrappedDelta2(this ushort a, ushort b)
        {
            return WrappedDelta(a, b, 2);
        }

        /// <summary>
        /// Wrapping delta for 7 bit numbers
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        internal static int WrappedDelta7(this ushort a, ushort b)
        {
            return WrappedDelta(a, b, 7);
        }

        /// <summary>
        /// Wrapping delta for 16 bit numbers
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        internal static int WrappedDelta16(this ushort a, ushort b)
        {
            return WrappedDelta(a, b, 16);
        }

        /// <summary>
        /// Calculate what value needs to be added to A to get to B
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="bits"></param>
        /// <returns></returns>
        private static int WrappedDelta(this ushort a, ushort b, int bits)
        {
            // Based on: https://stackoverflow.com/questions/44166714/in-c-how-do-i-calculate-the-signed-difference-between-two-48-bit-unsigned-integ

            var mask = (1 << bits) - 1;
            unchecked
            {
                var udiff = b - (uint)a;
                var diff = udiff & mask;
                var idiff = (int)diff;

                if ((udiff & (1 << (bits - 1))) != 0)
                    idiff = -(int)(mask - diff + 1);

                return idiff;
            }
        }
    }
}
