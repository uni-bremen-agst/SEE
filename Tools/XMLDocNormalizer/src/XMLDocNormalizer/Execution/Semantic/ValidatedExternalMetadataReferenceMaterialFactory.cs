using System.Collections.Immutable;
using System.Runtime.InteropServices;

namespace XMLDocNormalizer.Execution.Semantic
{
    /// <summary>
    /// Materializes one immutable PE snapshot and validates that exact image
    /// through P5B against original metadata-reference provenance.
    /// </summary>
    internal static class ValidatedExternalMetadataReferenceMaterialFactory
    {
        /// <summary>
        /// Tries to materialize and validate a caller-owned PE stream from its
        /// current position.
        /// </summary>
        /// <param name="expectedReference">
        /// The original serialized reference provenance and properties.
        /// </param>
        /// <param name="candidatePeStream">
        /// The readable candidate stream beginning at its current position.
        /// The caller retains ownership and the stream remains open.
        /// </param>
        /// <param name="material">
        /// The immutable image and its P5B candidate descriptor when valid.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when the stream can be materialized once and
        /// that exact image passes P5B validation; otherwise
        /// <see langword="false"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="expectedReference"/> or
        /// <paramref name="candidatePeStream"/> is <see langword="null"/>.
        /// </exception>
        public static bool TryCreate(
            ExternalCompilationMetadataReferenceDescriptor expectedReference,
            Stream candidatePeStream,
            out ValidatedExternalMetadataReferenceMaterial material)
        {
            ArgumentNullException.ThrowIfNull(expectedReference);
            ArgumentNullException.ThrowIfNull(candidatePeStream);

            return TryCreate(expectedReference, candidatePeStream, filePath: null, out material);
        }

        /// <summary>
        /// Tries to materialize and validate one explicitly supplied PE file.
        /// </summary>
        /// <param name="expectedReference">
        /// The original serialized reference provenance and properties.
        /// </param>
        /// <param name="candidatePath">
        /// The exact caller-selected candidate path. It is provenance only
        /// and is never derived from the expected reference name.
        /// </param>
        /// <param name="material">
        /// The immutable image and its P5B candidate descriptor when valid.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when the file can be opened once,
        /// materialized, and validated through P5B; otherwise
        /// <see langword="false"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="expectedReference"/> or
        /// <paramref name="candidatePath"/> is <see langword="null"/>.
        /// </exception>
        public static bool TryCreateFromFile(
            ExternalCompilationMetadataReferenceDescriptor expectedReference,
            string candidatePath,
            out ValidatedExternalMetadataReferenceMaterial material)
        {
            ArgumentNullException.ThrowIfNull(expectedReference);
            ArgumentNullException.ThrowIfNull(candidatePath);

            try
            {
                using FileStream stream = new(
                    candidatePath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read);
                return TryCreate(expectedReference, stream, candidatePath, out material);
            }
            catch (IOException)
            {
                material = null!;
                return false;
            }
            catch (UnauthorizedAccessException)
            {
                material = null!;
                return false;
            }
            catch (ArgumentException)
            {
                material = null!;
                return false;
            }
            catch (NotSupportedException)
            {
                material = null!;
                return false;
            }
        }

        /// <summary>
        /// Reads one complete candidate snapshot and validates those same
        /// immutable bytes through P5B.
        /// </summary>
        /// <param name="expectedReference">The expected reference provenance.</param>
        /// <param name="candidatePeStream">The caller-owned input stream.</param>
        /// <param name="filePath">The optional explicit path provenance.</param>
        /// <param name="material">The validated immutable material.</param>
        /// <returns>
        /// <see langword="true"/> when materialization and P5B validation
        /// succeed; otherwise <see langword="false"/>.
        /// </returns>
        private static bool TryCreate(
            ExternalCompilationMetadataReferenceDescriptor expectedReference,
            Stream candidatePeStream,
            string? filePath,
            out ValidatedExternalMetadataReferenceMaterial material)
        {
            if (!candidatePeStream.CanRead)
            {
                material = null!;
                return false;
            }

            try
            {
                using MemoryStream snapshotStream = new();
                candidatePeStream.CopyTo(snapshotStream);
                ImmutableArray<byte> image =
                    ImmutableCollectionsMarshal.AsImmutableArray(snapshotStream.ToArray());

                if (!ExternalMetadataReferenceCandidateDescriptorFactory.TryCreate(
                        expectedReference,
                        image,
                        filePath,
                        out ExternalMetadataReferenceCandidateDescriptor candidate))
                {
                    material = null!;
                    return false;
                }

                material = new ValidatedExternalMetadataReferenceMaterial(candidate, image);
                return true;
            }
            catch (IOException)
            {
                material = null!;
                return false;
            }
            catch (InvalidOperationException)
            {
                material = null!;
                return false;
            }
            catch (ArgumentException)
            {
                material = null!;
                return false;
            }
        }
    }
}
