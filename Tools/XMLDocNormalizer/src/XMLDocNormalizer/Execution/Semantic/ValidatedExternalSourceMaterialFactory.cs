using System.Collections.Immutable;
using System.Runtime.InteropServices;

namespace XMLDocNormalizer.Execution.Semantic
{
    /// <summary>
    /// Materializes one immutable source snapshot and validates those exact
    /// bytes against Portable PDB document-checksum provenance.
    /// </summary>
    internal static class ValidatedExternalSourceMaterialFactory
    {
        /// <summary>
        /// Tries to materialize already retained Embedded Source provenance.
        /// </summary>
        /// <param name="document">
        /// The Portable PDB document containing optional embedded source.
        /// </param>
        /// <param name="material">
        /// The exact checksum-validated embedded material when available.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when P4B retained embedded bytes with a
        /// validated known checksum and local revalidation succeeds; otherwise
        /// <see langword="false"/>.
        /// </returns>
        /// <remarks>
        /// This method performs no PDB, file, Source Link, or network I/O.
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="document"/> is
        /// <see langword="null"/>.
        /// </exception>
        public static bool TryCreateFromEmbeddedSource(
            ExternalSourceDocumentDescriptor document,
            out ValidatedExternalSourceMaterial material)
        {
            ArgumentNullException.ThrowIfNull(document);

            ExternalEmbeddedSourceProvenance? embeddedSource = document.EmbeddedSource;

            if (!embeddedSource.HasValue
                || !embeddedSource.Value.IsDocumentChecksumValidated
                || !ExternalSourceDocumentChecksumValidator.TryValidate(
                    embeddedSource.Value.Image.AsSpan(),
                    document.HashAlgorithm,
                    document.Hash,
                    out bool isChecksumValidated)
                || !isChecksumValidated)
            {
                material = null!;
                return false;
            }

            material = new ValidatedExternalSourceMaterial(
                document,
                embeddedSource.Value.Image,
                ExternalSourceMaterialOrigin.Embedded,
                filePath: null);
            return true;
        }

        /// <summary>
        /// Tries to materialize a caller-owned source stream from its current
        /// position and validate those exact bytes.
        /// </summary>
        /// <param name="document">
        /// The Portable PDB document checksum provenance.
        /// </param>
        /// <param name="candidateSourceStream">
        /// The readable candidate stream beginning at its current position.
        /// The caller retains ownership and the stream remains open.
        /// </param>
        /// <param name="material">
        /// The exact immutable checksum-validated material when successful.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when the remaining stream bytes can be read
        /// once and match a supported document checksum; otherwise
        /// <see langword="false"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="document"/> or
        /// <paramref name="candidateSourceStream"/> is
        /// <see langword="null"/>.
        /// </exception>
        public static bool TryCreate(
            ExternalSourceDocumentDescriptor document,
            Stream candidateSourceStream,
            out ValidatedExternalSourceMaterial material)
        {
            ArgumentNullException.ThrowIfNull(document);
            ArgumentNullException.ThrowIfNull(candidateSourceStream);

            return TryCreate(
                document,
                candidateSourceStream,
                ExternalSourceMaterialOrigin.ExplicitStream,
                filePath: null,
                out material);
        }

        /// <summary>
        /// Tries to materialize one explicitly supplied source file and
        /// validate its exact bytes.
        /// </summary>
        /// <param name="document">
        /// The Portable PDB document checksum provenance.
        /// </param>
        /// <param name="candidatePath">
        /// The exact caller-selected path. It is provenance only and is never
        /// derived from the Portable PDB document name.
        /// </param>
        /// <param name="material">
        /// The exact immutable checksum-validated material when successful.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when the file can be opened once, read once,
        /// and validated; otherwise <see langword="false"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="document"/> or
        /// <paramref name="candidatePath"/> is <see langword="null"/>.
        /// </exception>
        public static bool TryCreateFromFile(
            ExternalSourceDocumentDescriptor document,
            string candidatePath,
            out ValidatedExternalSourceMaterial material)
        {
            ArgumentNullException.ThrowIfNull(document);
            ArgumentNullException.ThrowIfNull(candidatePath);

            try
            {
                using FileStream stream = new(
                    candidatePath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read);
                return TryCreate(
                    document,
                    stream,
                    ExternalSourceMaterialOrigin.ExplicitFile,
                    candidatePath,
                    out material);
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
        /// Validates an already acquired immutable source snapshot without
        /// decoding, newline normalization, or additional source I/O.
        /// </summary>
        /// <param name="document">The Portable PDB document provenance.</param>
        /// <param name="image">The exact candidate source bytes.</param>
        /// <param name="origin">The controlled acquisition origin.</param>
        /// <param name="filePath">Optional local path provenance.</param>
        /// <param name="material">The checksum-validated P5H material.</param>
        /// <returns>
        /// <see langword="true"/> only when the existing Portable PDB checksum
        /// validator accepts the exact supplied bytes.
        /// </returns>
        /// <remarks>
        /// Acquisition locates candidate source bytes. Portable PDB checksum
        /// validation alone establishes whether they belong to the document.
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="document"/> is <see langword="null"/>.
        /// </exception>
        internal static bool TryCreateFromAcquiredImage(
            ExternalSourceDocumentDescriptor document,
            ImmutableArray<byte> image,
            ExternalSourceMaterialOrigin origin,
            string? filePath,
            out ValidatedExternalSourceMaterial material)
        {
            ArgumentNullException.ThrowIfNull(document);

            if (image.IsDefault
                || (origin != ExternalSourceMaterialOrigin.LocalMapping
                    && origin != ExternalSourceMaterialOrigin.SourceLink)
                || !ExternalSourceDocumentChecksumValidator.TryValidate(
                    image.AsSpan(),
                    document.HashAlgorithm,
                    document.Hash,
                    out bool isChecksumValidated)
                || !isChecksumValidated)
            {
                material = null!;
                return false;
            }

            material = new ValidatedExternalSourceMaterial(
                document,
                image,
                origin,
                filePath);
            return true;
        }

        /// <summary>
        /// Reads one complete candidate snapshot and validates those same bytes.
        /// </summary>
        /// <param name="document">The Portable PDB document provenance.</param>
        /// <param name="candidateSourceStream">The caller-owned input stream.</param>
        /// <param name="origin">How the candidate was supplied.</param>
        /// <param name="filePath">The optional caller-selected file path.</param>
        /// <param name="material">The validated immutable material.</param>
        /// <returns>
        /// <see langword="true"/> when one complete read and exact checksum
        /// validation succeed; otherwise <see langword="false"/>.
        /// </returns>
        private static bool TryCreate(
            ExternalSourceDocumentDescriptor document,
            Stream candidateSourceStream,
            ExternalSourceMaterialOrigin origin,
            string? filePath,
            out ValidatedExternalSourceMaterial material)
        {
            if (!candidateSourceStream.CanRead)
            {
                material = null!;
                return false;
            }

            try
            {
                using MemoryStream snapshotStream = new();
                candidateSourceStream.CopyTo(snapshotStream);
                ImmutableArray<byte> image =
                    ImmutableCollectionsMarshal.AsImmutableArray(snapshotStream.ToArray());

                if (!ExternalSourceDocumentChecksumValidator.TryValidate(
                        image.AsSpan(),
                        document.HashAlgorithm,
                        document.Hash,
                        out bool isChecksumValidated)
                    || !isChecksumValidated)
                {
                    material = null!;
                    return false;
                }

                material = new ValidatedExternalSourceMaterial(
                    document,
                    image,
                    origin,
                    filePath);
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
