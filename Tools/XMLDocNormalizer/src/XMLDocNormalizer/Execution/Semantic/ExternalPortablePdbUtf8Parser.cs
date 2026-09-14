using System.Text;

namespace XMLDocNormalizer.Execution.Semantic
{
    /// <summary>
    /// Reads strictly encoded null-terminated UTF-8 strings from Portable PDB
    /// custom debug information blobs.
    /// </summary>
    internal static class ExternalPortablePdbUtf8Parser
    {
        /// <summary>
        /// The strict UTF-8 encoding used for untrusted Portable PDB strings.
        /// </summary>
        private static readonly UTF8Encoding StrictUtf8 = new(
            encoderShouldEmitUTF8Identifier: false,
            throwOnInvalidBytes: true);

        /// <summary>
        /// Tries to read one null-terminated UTF-8 string.
        /// </summary>
        /// <param name="blob">The complete custom debug information blob.</param>
        /// <param name="offset">The current offset, advanced past the terminator.</param>
        /// <param name="value">The decoded string when successful.</param>
        /// <returns>
        /// <see langword="true"/> when a terminator exists and all preceding
        /// bytes are valid UTF-8; otherwise <see langword="false"/>.
        /// </returns>
        public static bool TryReadNullTerminatedString(
            ReadOnlySpan<byte> blob,
            ref int offset,
            out string value)
        {
            if (offset < 0 || offset > blob.Length)
            {
                value = null!;
                return false;
            }

            ReadOnlySpan<byte> remaining = blob[offset..];
            int terminatorOffset = remaining.IndexOf((byte)0);

            if (terminatorOffset < 0)
            {
                value = null!;
                return false;
            }

            try
            {
                value = StrictUtf8.GetString(remaining[..terminatorOffset]);
                offset += terminatorOffset + 1;
                return true;
            }
            catch (DecoderFallbackException)
            {
                value = null!;
                return false;
            }
        }
    }
}
