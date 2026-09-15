using System.Buffers.Binary;
using System.Collections.Immutable;
using System.IO.Compression;
using System.Runtime.InteropServices;

namespace XMLDocNormalizer.Execution.Semantic
{
    /// <summary>
    /// Materializes and validates embedded-source custom debug information.
    /// </summary>
    internal static class ExternalEmbeddedSourceProvenanceFactory
    {
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
                ImmutableArray<byte> sourceImage =
                    ImmutableCollectionsMarshal.AsImmutableArray(sourceBytes.ToArray());

                if (!ExternalSourceDocumentChecksumValidator.TryValidate(
                        sourceImage.AsSpan(),
                        documentHashAlgorithm,
                        expectedDocumentHash,
                        out bool isChecksumValidated))
                {
                    provenance = default;
                    return false;
                }

                return ExternalEmbeddedSourceProvenance.TryCreate(
                    sourceImage,
                    isCompressed: false,
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
                byte[] sourceBytes = new byte[expectedSize];
                int uncompressedSize = 0;

                while (uncompressedSize < sourceBytes.Length)
                {
                    int bytesRead = deflateStream.Read(
                        sourceBytes,
                        uncompressedSize,
                        sourceBytes.Length - uncompressedSize);

                    if (bytesRead == 0)
                    {
                        provenance = default;
                        return false;
                    }

                    uncompressedSize += bytesRead;
                }

                if (deflateStream.ReadByte() != -1)
                {
                    provenance = default;
                    return false;
                }

                ImmutableArray<byte> sourceImage =
                    ImmutableCollectionsMarshal.AsImmutableArray(sourceBytes);

                if (!ExternalSourceDocumentChecksumValidator.TryValidate(
                        sourceImage.AsSpan(),
                        documentHashAlgorithm,
                        expectedDocumentHash,
                        out bool isChecksumValidated))
                {
                    provenance = default;
                    return false;
                }

                return ExternalEmbeddedSourceProvenance.TryCreate(
                    sourceImage,
                    isCompressed: true,
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

    }
}
