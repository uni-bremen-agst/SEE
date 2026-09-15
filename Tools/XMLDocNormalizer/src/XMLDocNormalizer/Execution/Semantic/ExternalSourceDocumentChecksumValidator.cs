using System.Collections.Immutable;
using System.Security.Cryptography;
using Microsoft.CodeAnalysis.Text;

namespace XMLDocNormalizer.Execution.Semantic
{
    /// <summary>
    /// Validates exact source bytes against Portable PDB document-checksum
    /// provenance using the standardized supported algorithms.
    /// </summary>
    internal static class ExternalSourceDocumentChecksumValidator
    {
        /// <summary>
        /// The Portable PDB SHA-1 document hash-algorithm identifier.
        /// </summary>
        private static readonly Guid Sha1DocumentHashAlgorithm =
            new("ff1816ec-aa5e-4d10-87f7-6f4963833460");

        /// <summary>
        /// The Portable PDB SHA-256 document hash-algorithm identifier.
        /// </summary>
        private static readonly Guid Sha256DocumentHashAlgorithm =
            new("8829d00f-11b8-4213-878b-770e8597ac16");

        /// <summary>
        /// The Portable PDB SHA-384 document hash-algorithm identifier.
        /// </summary>
        private static readonly Guid Sha384DocumentHashAlgorithm =
            new("d99cfeb1-8c43-444a-8a6c-b61269d2a0bf");

        /// <summary>
        /// The Portable PDB SHA-512 document hash-algorithm identifier.
        /// </summary>
        private static readonly Guid Sha512DocumentHashAlgorithm =
            new("ef2d1afc-6550-46d6-b14b-d70afe9a5566");

        /// <summary>
        /// Tries to validate exact source bytes against document-checksum
        /// provenance.
        /// </summary>
        /// <param name="sourceImage">The exact source bytes to validate.</param>
        /// <param name="documentHashAlgorithm">
        /// The Portable PDB document hash-algorithm identifier.
        /// </param>
        /// <param name="expectedDocumentHash">
        /// The exact expected document hash bytes.
        /// </param>
        /// <param name="isChecksumValidated">
        /// Whether a supported algorithm was identified and its exact-length
        /// checksum matched.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when the algorithm is unknown or the known
        /// algorithm has a correctly sized matching checksum; otherwise
        /// <see langword="false"/>. Unknown algorithms never set
        /// <paramref name="isChecksumValidated"/>.
        /// </returns>
        public static bool TryValidate(
            ReadOnlySpan<byte> sourceImage,
            Guid documentHashAlgorithm,
            ImmutableArray<byte> expectedDocumentHash,
            out bool isChecksumValidated)
        {
            if (!TryGetDocumentHashAlgorithm(
                    documentHashAlgorithm,
                    out HashAlgorithmName hashAlgorithm,
                    out int expectedHashLength))
            {
                isChecksumValidated = false;
                return true;
            }

            if (expectedDocumentHash.IsDefault
                || expectedDocumentHash.Length != expectedHashLength)
            {
                isChecksumValidated = false;
                return false;
            }

            using IncrementalHash hash = IncrementalHash.CreateHash(hashAlgorithm);
            hash.AppendData(sourceImage);
            byte[] actualHash = hash.GetHashAndReset();
            isChecksumValidated = CryptographicOperations.FixedTimeEquals(
                actualHash,
                expectedDocumentHash.AsSpan());
            return isChecksumValidated;
        }

        /// <summary>
        /// Maps a Portable PDB document hash identifier to the exact source
        /// hash algorithm representable by the current Roslyn API.
        /// </summary>
        /// <param name="identifier">The Portable PDB algorithm identifier.</param>
        /// <param name="algorithm">The exact Roslyn source hash algorithm.</param>
        /// <returns>
        /// <see langword="true"/> for SHA-1 or SHA-256; otherwise
        /// <see langword="false"/>. In particular, SHA-384 and SHA-512 are
        /// not approximated by another algorithm.
        /// </returns>
        public static bool TryGetRoslynSourceHashAlgorithm(
            Guid identifier,
            out SourceHashAlgorithm algorithm)
        {
            if (!TryGetDocumentHashAlgorithm(
                    identifier,
                    out HashAlgorithmName hashAlgorithm,
                    out _))
            {
                algorithm = default;
                return false;
            }

            if (hashAlgorithm == HashAlgorithmName.SHA1)
            {
                algorithm = SourceHashAlgorithm.Sha1;
                return true;
            }

            if (hashAlgorithm == HashAlgorithmName.SHA256)
            {
                algorithm = SourceHashAlgorithm.Sha256;
                return true;
            }

            algorithm = default;
            return false;
        }

        /// <summary>
        /// Maps standardized Portable PDB document hash identifiers to their
        /// concrete algorithms and exact digest lengths.
        /// </summary>
        /// <param name="identifier">The Portable PDB algorithm identifier.</param>
        /// <param name="algorithm">The concrete hash algorithm when known.</param>
        /// <param name="hashLength">The exact digest length when known.</param>
        /// <returns>
        /// <see langword="true"/> for SHA-1, SHA-256, SHA-384, or SHA-512;
        /// otherwise <see langword="false"/>.
        /// </returns>
        private static bool TryGetDocumentHashAlgorithm(
            Guid identifier,
            out HashAlgorithmName algorithm,
            out int hashLength)
        {
            if (identifier == Sha1DocumentHashAlgorithm)
            {
                algorithm = HashAlgorithmName.SHA1;
                hashLength = 20;
                return true;
            }

            if (identifier == Sha256DocumentHashAlgorithm)
            {
                algorithm = HashAlgorithmName.SHA256;
                hashLength = 32;
                return true;
            }

            if (identifier == Sha384DocumentHashAlgorithm)
            {
                algorithm = HashAlgorithmName.SHA384;
                hashLength = 48;
                return true;
            }

            if (identifier == Sha512DocumentHashAlgorithm)
            {
                algorithm = HashAlgorithmName.SHA512;
                hashLength = 64;
                return true;
            }

            algorithm = default;
            hashLength = default;
            return false;
        }
    }
}
