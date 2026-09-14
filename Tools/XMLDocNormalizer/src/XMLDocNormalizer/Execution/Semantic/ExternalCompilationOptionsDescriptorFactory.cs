using System.Collections.Immutable;

namespace XMLDocNormalizer.Execution.Semantic
{
    /// <summary>
    /// Parses Portable PDB compilation-options custom debug information.
    /// </summary>
    internal static class ExternalCompilationOptionsDescriptorFactory
    {
        /// <summary>
        /// Tries to parse all null-terminated UTF-8 key/value pairs.
        /// </summary>
        /// <param name="blob">The complete compilation-options blob.</param>
        /// <param name="descriptor">The parsed descriptor when valid.</param>
        /// <returns>
        /// <see langword="true"/> when the complete blob is well formed and
        /// contains unique nonempty keys; otherwise <see langword="false"/>.
        /// </returns>
        public static bool TryCreate(
            ImmutableArray<byte> blob,
            out ExternalCompilationOptionsDescriptor descriptor)
        {
            if (blob.IsDefault)
            {
                descriptor = null!;
                return false;
            }

            ImmutableArray<ExternalCompilationOption>.Builder options =
                ImmutableArray.CreateBuilder<ExternalCompilationOption>();
            HashSet<string> keys = new(StringComparer.Ordinal);
            int offset = 0;

            while (offset < blob.Length)
            {
                if (!ExternalPortablePdbUtf8Parser.TryReadNullTerminatedString(
                        blob.AsSpan(),
                        ref offset,
                        out string key)
                    || key.Length == 0
                    || !ExternalPortablePdbUtf8Parser.TryReadNullTerminatedString(
                        blob.AsSpan(),
                        ref offset,
                        out string value)
                    || !keys.Add(key))
                {
                    descriptor = null!;
                    return false;
                }

                options.Add(new ExternalCompilationOption(key, value));
            }

            descriptor = new ExternalCompilationOptionsDescriptor(options.ToImmutable());
            return true;
        }
    }
}
