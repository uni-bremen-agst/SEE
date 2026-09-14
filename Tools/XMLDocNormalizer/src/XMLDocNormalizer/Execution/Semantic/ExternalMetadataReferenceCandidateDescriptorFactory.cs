using System.Collections.Immutable;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using Microsoft.CodeAnalysis;

namespace XMLDocNormalizer.Execution.Semantic
{
    /// <summary>
    /// Validates one explicitly supplied PE candidate against serialized
    /// compilation metadata-reference provenance.
    /// </summary>
    internal static class ExternalMetadataReferenceCandidateDescriptorFactory
    {
        /// <summary>
        /// Tries to validate a caller-owned PE stream from its current
        /// position.
        /// </summary>
        /// <param name="expectedReference">
        /// The original serialized reference provenance and properties.
        /// </param>
        /// <param name="candidatePeStream">
        /// The readable PE stream beginning at its current position. The
        /// caller retains ownership and the stream remains open.
        /// </param>
        /// <param name="descriptor">The validated candidate descriptor.</param>
        /// <returns>
        /// <see langword="true"/> when kind, COFF timestamp, PE image size,
        /// and MVID all match and the candidate metadata is valid; otherwise
        /// <see langword="false"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="expectedReference"/> or
        /// <paramref name="candidatePeStream"/> is <see langword="null"/>.
        /// </exception>
        public static bool TryCreate(
            ExternalCompilationMetadataReferenceDescriptor expectedReference,
            Stream candidatePeStream,
            out ExternalMetadataReferenceCandidateDescriptor descriptor)
        {
            ArgumentNullException.ThrowIfNull(expectedReference);
            ArgumentNullException.ThrowIfNull(candidatePeStream);

            return TryCreate(expectedReference, candidatePeStream, filePath: null, out descriptor);
        }

        /// <summary>
        /// Tries to validate one explicitly supplied PE file candidate.
        /// </summary>
        /// <param name="expectedReference">
        /// The original serialized reference provenance and properties.
        /// </param>
        /// <param name="candidatePath">
        /// The exact caller-selected candidate path. It is not binary
        /// identity and is never derived from the expected reference name.
        /// </param>
        /// <param name="descriptor">The validated candidate descriptor.</param>
        /// <returns>
        /// <see langword="true"/> when the selected file is readable and its
        /// complete required PE provenance matches; otherwise
        /// <see langword="false"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="expectedReference"/> or
        /// <paramref name="candidatePath"/> is <see langword="null"/>.
        /// </exception>
        public static bool TryCreateFromFile(
            ExternalCompilationMetadataReferenceDescriptor expectedReference,
            string candidatePath,
            out ExternalMetadataReferenceCandidateDescriptor descriptor)
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
                return TryCreate(expectedReference, stream, candidatePath, out descriptor);
            }
            catch (IOException)
            {
                descriptor = null!;
                return false;
            }
            catch (UnauthorizedAccessException)
            {
                descriptor = null!;
                return false;
            }
            catch (ArgumentException)
            {
                descriptor = null!;
                return false;
            }
            catch (NotSupportedException)
            {
                descriptor = null!;
                return false;
            }
        }

        /// <summary>
        /// Tries to validate an immutable PE candidate image without copying
        /// its bytes.
        /// </summary>
        /// <param name="expectedReference">
        /// The original serialized reference provenance and properties.
        /// </param>
        /// <param name="candidateImage">The complete immutable PE image.</param>
        /// <param name="filePath">
        /// The explicit candidate path provenance, or <see langword="null"/>
        /// for a stream candidate.
        /// </param>
        /// <param name="descriptor">The validated candidate descriptor.</param>
        /// <returns>
        /// <see langword="true"/> when the immutable image matches all
        /// required provenance; otherwise <see langword="false"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="expectedReference"/> is
        /// <see langword="null"/>.
        /// </exception>
        internal static bool TryCreate(
            ExternalCompilationMetadataReferenceDescriptor expectedReference,
            ImmutableArray<byte> candidateImage,
            string? filePath,
            out ExternalMetadataReferenceCandidateDescriptor descriptor)
        {
            ArgumentNullException.ThrowIfNull(expectedReference);

            if (candidateImage.IsDefaultOrEmpty)
            {
                descriptor = null!;
                return false;
            }

            try
            {
                using PEReader peReader = new(candidateImage);
                return TryCreate(expectedReference, peReader, filePath, out descriptor);
            }
            catch (BadImageFormatException)
            {
                descriptor = null!;
                return false;
            }
            catch (InvalidOperationException)
            {
                descriptor = null!;
                return false;
            }
            catch (ArgumentException)
            {
                descriptor = null!;
                return false;
            }
        }

        /// <summary>
        /// Validates all candidate properties from one prefetched PE image.
        /// </summary>
        /// <param name="expectedReference">The expected reference provenance.</param>
        /// <param name="candidatePeStream">The candidate PE stream.</param>
        /// <param name="filePath">The optional candidate path provenance.</param>
        /// <param name="descriptor">The validated candidate descriptor.</param>
        /// <returns>
        /// <see langword="true"/> when the complete required provenance
        /// matches; otherwise <see langword="false"/>.
        /// </returns>
        private static bool TryCreate(
            ExternalCompilationMetadataReferenceDescriptor expectedReference,
            Stream candidatePeStream,
            string? filePath,
            out ExternalMetadataReferenceCandidateDescriptor descriptor)
        {
            if (!candidatePeStream.CanRead)
            {
                descriptor = null!;
                return false;
            }

            try
            {
                using PEReader peReader = new(
                    candidatePeStream,
                    PEStreamOptions.LeaveOpen | PEStreamOptions.PrefetchEntireImage);
                return TryCreate(expectedReference, peReader, filePath, out descriptor);
            }
            catch (BadImageFormatException)
            {
                descriptor = null!;
                return false;
            }
            catch (IOException)
            {
                descriptor = null!;
                return false;
            }
            catch (InvalidOperationException)
            {
                descriptor = null!;
                return false;
            }
            catch (ArgumentException)
            {
                descriptor = null!;
                return false;
            }
        }

        /// <summary>
        /// Validates and describes one complete prefetched PE snapshot.
        /// </summary>
        /// <param name="expectedReference">The expected reference provenance.</param>
        /// <param name="peReader">The reader for the prefetched PE snapshot.</param>
        /// <param name="filePath">The optional candidate path provenance.</param>
        /// <param name="descriptor">The validated candidate descriptor.</param>
        /// <returns>
        /// <see langword="true"/> when the candidate is valid and all required
        /// provenance matches; otherwise <see langword="false"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="expectedReference"/> or
        /// <paramref name="peReader"/> is <see langword="null"/>.
        /// </exception>
        private static bool TryCreate(
            ExternalCompilationMetadataReferenceDescriptor expectedReference,
            PEReader peReader,
            string? filePath,
            out ExternalMetadataReferenceCandidateDescriptor descriptor)
        {
            ArgumentNullException.ThrowIfNull(expectedReference);
            ArgumentNullException.ThrowIfNull(peReader);

            PEHeader? peHeader = peReader.PEHeaders.PEHeader;

            if (peHeader == null || !peReader.HasMetadata)
            {
                descriptor = null!;
                return false;
            }

            MetadataReader metadataReader = peReader.GetMetadataReader();
            MetadataImageKind kind = metadataReader.IsAssembly
                ? MetadataImageKind.Assembly
                : MetadataImageKind.Module;
            int timeDateStamp = peReader.PEHeaders.CoffHeader.TimeDateStamp;
            int imageSize = peHeader.SizeOfImage;

            if (kind != expectedReference.Kind
                || timeDateStamp != expectedReference.Timestamp
                || imageSize != expectedReference.ImageSize)
            {
                descriptor = null!;
                return false;
            }

            ModuleDefinition moduleDefinition = metadataReader.GetModuleDefinition();
            ExternalModuleIdentity module = new(
                metadataReader.GetString(moduleDefinition.Name),
                metadataReader.GetGuid(moduleDefinition.Mvid));

            if (module.ModuleVersionId != expectedReference.ModuleVersionId)
            {
                descriptor = null!;
                return false;
            }

            AssemblyIdentity? assemblyIdentity = null;

            if (kind == MetadataImageKind.Assembly
                && !ExternalPeMetadataIdentityReader.TryReadAssemblyIdentity(
                    metadataReader,
                    out assemblyIdentity))
            {
                descriptor = null!;
                return false;
            }

            descriptor = new ExternalMetadataReferenceCandidateDescriptor(
                expectedReference,
                kind,
                module,
                assemblyIdentity,
                timeDateStamp,
                imageSize,
                filePath);
            return true;
        }
    }
}
