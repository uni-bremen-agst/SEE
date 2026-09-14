using System.Buffers.Binary;
using System.Collections.Immutable;
using System.IO.Compression;
using System.Security.Cryptography;

namespace XMLDocNormalizer.Execution.Semantic
{
    /// <summary>
    /// Validates embedded-source custom debug information without retaining
    /// the source bytes.
    /// </summary>
    internal static class ExternalEmbeddedSourceProvenanceFactory
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
        /// The fixed buffer size used while validating compressed source.
        /// </summary>
        private const int DecompressionBufferSize = 81920;

        /// <summary>
        /// Tries to validate embedded-source custom debug information against
        /// its document checksum.
        /// </summary>
        /// <param name="embeddedSourceBlob">
        /// The complete embedded-source custom debug information blob.
        /// </param>
        /// <param name="documentHashAlgorithm">
        /// The document hash-algorithm GUID.
        /// </param>
        /// <param name="expectedDocumentHash">
        /// The expected document hash bytes.
        /// </param>
        /// <param name="provenance">
        /// The validated embedded-source provenance when available.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when the raw or compressed source format is
        /// valid and any known document checksum matches; otherwise
        /// <see langword="false"/>. Unknown document hash algorithms preserve
        /// provenance without claiming checksum validation.
        /// </returns>
        public static bool TryCreate(
            ImmutableArray<byte> embeddedSourceBlob,
            Guid documentHashAlgorithm,
            ImmutableArray<byte> expectedDocumentHash,
            out ExternalEmbeddedSourceProvenance provenance)
        {
            if (embeddedSourceBlob.IsDefault || embeddedSourceBlob.Length < sizeof(int))
            {
                provenance = default;
                return false;
            }

            int format = BinaryPrimitives.ReadInt32LittleEndian(
                embeddedSourceBlob.AsSpan(0, sizeof(int)));

            if (format < 0)
            {
                provenance = default;
                return false;
            }

            if (format == 0)
            {
                ReadOnlySpan<byte> sourceBytes =
                    embeddedSourceBlob.AsSpan().Slice(sizeof(int));

                if (!TryValidateDocumentHash(
                        sourceBytes,
                        documentHashAlgorithm,
                        expectedDocumentHash,
                        out bool isChecksumValidated))
                {
                    provenance = default;
                    return false;
                }

                return ExternalEmbeddedSourceProvenance.TryCreate(
                    isCompressed: false,
                    sourceBytes.Length,
                    isChecksumValidated,
                    out provenance);
            }

            return TryReadCompressedSource(
                embeddedSourceBlob,
                format,
                documentHashAlgorithm,
                expectedDocumentHash,
                out provenance);
        }

        /// <summary>
        /// Streams and validates Deflate-compressed embedded source.
        /// </summary>
        /// <param name="embeddedSourceBlob">The complete embedded-source blob.</param>
        /// <param name="expectedSize">The declared uncompressed size.</param>
        /// <param name="documentHashAlgorithm">The document hash-algorithm GUID.</param>
        /// <param name="expectedDocumentHash">The expected document hash.</param>
        /// <param name="provenance">The resulting provenance when valid.</param>
        /// <returns>
        /// <see langword="true"/> when decompression, size, and any known
        /// checksum are valid; otherwise <see langword="false"/>.
        /// </returns>
        private static bool TryReadCompressedSource(
            ImmutableArray<byte> embeddedSourceBlob,
            int expectedSize,
            Guid documentHashAlgorithm,
            ImmutableArray<byte> expectedDocumentHash,
            out ExternalEmbeddedSourceProvenance provenance)
        {
            if (embeddedSourceBlob.IsDefault
                || embeddedSourceBlob.Length < sizeof(int)
                || expectedSize <= 0)
            {
                provenance = default;
                return false;
            }

            byte[] serializedBlob = embeddedSourceBlob.ToArray();
            using MemoryStream compressedStream = new(
                serializedBlob,
                sizeof(int),
                serializedBlob.Length - sizeof(int),
                writable: false);

            try
            {
                using DeflateStream deflateStream = new(
                    compressedStream,
                    CompressionMode.Decompress,
                    leaveOpen: false);
                bool hasKnownAlgorithm = TryGetDocumentHashAlgorithm(
                    documentHashAlgorithm,
                    out HashAlgorithmName hashAlgorithm);
                using IncrementalHash? hash = hasKnownAlgorithm
                    ? IncrementalHash.CreateHash(hashAlgorithm)
                    : null;
                byte[] buffer = new byte[DecompressionBufferSize];
                long uncompressedSize = 0;
                int bytesRead;

                while ((bytesRead = deflateStream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    uncompressedSize += bytesRead;

                    if (uncompressedSize > expectedSize)
                    {
                        provenance = default;
                        return false;
                    }

                    hash?.AppendData(buffer, 0, bytesRead);
                }

                if (uncompressedSize != expectedSize)
                {
                    provenance = default;
                    return false;
                }

                bool isChecksumValidated = false;

                if (hash != null)
                {
                    byte[] actualHash = hash.GetHashAndReset();

                    if (!CryptographicOperations.FixedTimeEquals(
                            actualHash,
                            expectedDocumentHash.AsSpan()))
                    {
                        provenance = default;
                        return false;
                    }

                    isChecksumValidated = true;
                }

                return ExternalEmbeddedSourceProvenance.TryCreate(
                    isCompressed: true,
                    checked((int)uncompressedSize),
                    isChecksumValidated,
                    out provenance);
            }
            catch (InvalidDataException)
            {
                provenance = default;
                return false;
            }
            catch (IOException)
            {
                provenance = default;
                return false;
            }
        }

        /// <summary>
        /// Validates raw source bytes with a known document hash algorithm.
        /// </summary>
        /// <param name="sourceBytes">The uncompressed source bytes.</param>
        /// <param name="documentHashAlgorithm">The document hash-algorithm GUID.</param>
        /// <param name="expectedDocumentHash">The expected document hash.</param>
        /// <param name="isChecksumValidated">
        /// Whether a known checksum was validated.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when the algorithm is unknown or the known
        /// checksum matches; otherwise <see langword="false"/>.
        /// </returns>
        private static bool TryValidateDocumentHash(
            ReadOnlySpan<byte> sourceBytes,
            Guid documentHashAlgorithm,
            ImmutableArray<byte> expectedDocumentHash,
            out bool isChecksumValidated)
        {
            if (!TryGetDocumentHashAlgorithm(
                    documentHashAlgorithm,
                    out HashAlgorithmName hashAlgorithm))
            {
                isChecksumValidated = false;
                return true;
            }

            using IncrementalHash hash = IncrementalHash.CreateHash(hashAlgorithm);
            hash.AppendData(sourceBytes);
            byte[] actualHash = hash.GetHashAndReset();
            isChecksumValidated = CryptographicOperations.FixedTimeEquals(
                actualHash,
                expectedDocumentHash.AsSpan());
            return isChecksumValidated;
        }

        /// <summary>
        /// Maps standardized Portable PDB document hash identifiers to
        /// concrete cryptographic algorithms.
        /// </summary>
        /// <param name="identifier">The Portable PDB algorithm identifier.</param>
        /// <param name="algorithm">The concrete hash algorithm when known.</param>
        /// <returns>
        /// <see langword="true"/> for SHA-1, SHA-256, SHA-384, or SHA-512;
        /// otherwise <see langword="false"/>.
        /// </returns>
        private static bool TryGetDocumentHashAlgorithm(
            Guid identifier,
            out HashAlgorithmName algorithm)
        {
            if (identifier == Sha1DocumentHashAlgorithm)
            {
                algorithm = HashAlgorithmName.SHA1;
                return true;
            }

            if (identifier == Sha256DocumentHashAlgorithm)
            {
                algorithm = HashAlgorithmName.SHA256;
                return true;
            }

            if (identifier == Sha384DocumentHashAlgorithm)
            {
                algorithm = HashAlgorithmName.SHA384;
                return true;
            }

            if (identifier == Sha512DocumentHashAlgorithm)
            {
                algorithm = HashAlgorithmName.SHA512;
                return true;
            }

            algorithm = default;
            return false;
        }
    }
}
