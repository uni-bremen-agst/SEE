using System.Buffers.Binary;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace XMLDocNormalizer.Execution.Semantic
{
    /// <summary>
    /// Parses Portable PDB compilation-metadata-references custom debug
    /// information.
    /// </summary>
    internal static class ExternalCompilationMetadataReferencesDescriptorFactory
    {
        /// <summary>
        /// The byte count after the two strings in every reference entry.
        /// </summary>
        private const int FixedEntrySize = sizeof(byte) + sizeof(int) + sizeof(int) + 16;

        /// <summary>
        /// The only standardized metadata-reference property bits.
        /// </summary>
        private const byte KnownPropertyBits = 0b00000011;

        /// <summary>
        /// Tries to parse every metadata-reference entry to the exact blob end.
        /// </summary>
        /// <param name="blob">The complete metadata-references blob.</param>
        /// <param name="descriptor">The parsed descriptor when valid.</param>
        /// <returns>
        /// <see langword="true"/> when all entries are complete, strictly
        /// encoded, and use known property bits; otherwise
        /// <see langword="false"/>.
        /// </returns>
        public static bool TryCreate(
            ImmutableArray<byte> blob,
            out ExternalCompilationMetadataReferencesDescriptor descriptor)
        {
            if (blob.IsDefault)
            {
                descriptor = null!;
                return false;
            }

            ImmutableArray<ExternalCompilationMetadataReferenceDescriptor>.Builder references =
                ImmutableArray.CreateBuilder<ExternalCompilationMetadataReferenceDescriptor>();
            int offset = 0;

            while (offset < blob.Length)
            {
                if (!TryReadReference(
                        blob.AsSpan(),
                        ref offset,
                        out ExternalCompilationMetadataReferenceDescriptor reference))
                {
                    descriptor = null!;
                    return false;
                }

                references.Add(reference);
            }

            descriptor = new ExternalCompilationMetadataReferencesDescriptor(
                references.ToImmutable());
            return true;
        }

        /// <summary>
        /// Tries to read one complete metadata-reference entry.
        /// </summary>
        /// <param name="blob">The complete metadata-references blob.</param>
        /// <param name="offset">The current offset, advanced past the entry.</param>
        /// <param name="reference">The parsed reference when valid.</param>
        /// <returns>
        /// <see langword="true"/> when one complete valid entry was read;
        /// otherwise <see langword="false"/>.
        /// </returns>
        private static bool TryReadReference(
            ReadOnlySpan<byte> blob,
            ref int offset,
            out ExternalCompilationMetadataReferenceDescriptor reference)
        {
            if (!ExternalPortablePdbUtf8Parser.TryReadNullTerminatedString(
                    blob,
                    ref offset,
                    out string name)
                || !ExternalPortablePdbUtf8Parser.TryReadNullTerminatedString(
                    blob,
                    ref offset,
                    out string aliasesText)
                || !TryParseAliases(aliasesText, out ImmutableArray<string> aliases)
                || blob.Length - offset < FixedEntrySize)
            {
                reference = null!;
                return false;
            }

            byte properties = blob[offset];

            if ((properties & ~KnownPropertyBits) != 0)
            {
                reference = null!;
                return false;
            }

            MetadataImageKind kind = (properties & 0b00000001) != 0
                ? MetadataImageKind.Assembly
                : MetadataImageKind.Module;
            bool embedInteropTypes = (properties & 0b00000010) != 0;
            int timestamp = BinaryPrimitives.ReadInt32LittleEndian(
                blob.Slice(offset + sizeof(byte), sizeof(int)));
            int imageSize = BinaryPrimitives.ReadInt32LittleEndian(
                blob.Slice(offset + sizeof(byte) + sizeof(int), sizeof(int)));
            Guid moduleVersionId = new(
                blob.Slice(offset + sizeof(byte) + sizeof(int) + sizeof(int), 16));
            offset += FixedEntrySize;
            reference = new ExternalCompilationMetadataReferenceDescriptor(
                name,
                aliases,
                kind,
                embedInteropTypes,
                timestamp,
                imageSize,
                moduleVersionId);
            return true;
        }

        /// <summary>
        /// Tries to preserve a comma-separated alias list without normalization.
        /// </summary>
        /// <param name="aliasesText">The exact serialized alias string.</param>
        /// <param name="aliases">The aliases in original order.</param>
        /// <returns>
        /// <see langword="true"/> when the list is empty or contains no empty
        /// elements; otherwise <see langword="false"/>.
        /// </returns>
        private static bool TryParseAliases(
            string aliasesText,
            out ImmutableArray<string> aliases)
        {
            if (aliasesText.Length == 0)
            {
                aliases = ImmutableArray<string>.Empty;
                return true;
            }

            string[] values = aliasesText.Split(',');

            if (values.Any(value => value.Length == 0))
            {
                aliases = default;
                return false;
            }

            aliases = ImmutableArray.CreateRange(values);
            return true;
        }
    }
}
