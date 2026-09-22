using System.Collections.Immutable;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices;

namespace XMLDocNormalizer.Execution.Semantic
{
    /// <summary>
    /// Validates an explicitly supplied Portable PDB and reads its serialized
    /// compilation-metadata provenance from the same immutable snapshot.
    /// </summary>
    internal static class ExternalCompilationProvenanceDescriptorFactory
    {
        /// <summary>
        /// The standardized module-level Compilation Options CDI identifier.
        /// </summary>
        private static readonly Guid CompilationOptionsKind =
            new("b5feec05-8cd0-4a83-96da-466284bb4bd8");

        /// <summary>
        /// The standardized module-level Compilation Metadata References CDI
        /// identifier.
        /// </summary>
        private static readonly Guid CompilationMetadataReferencesKind =
            new("7e4d4708-096e-4c5c-aeda-cb10ba6a740d");

        /// <summary>
        /// Tries to validate and describe compilation provenance from a
        /// caller-owned Portable PDB stream.
        /// </summary>
        /// <param name="expectedDebugDescriptor">
        /// The validated PE debug provenance that identifies acceptable PDBs.
        /// </param>
        /// <param name="portablePdbStream">
        /// The readable candidate stream, beginning at its current position.
        /// The caller retains ownership and the stream remains open.
        /// </param>
        /// <param name="descriptor">
        /// The validated PDB and compilation provenance when available.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when P4B validates the candidate and both
        /// supported module CDI blobs are absent or well formed; otherwise
        /// <see langword="false"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="expectedDebugDescriptor"/> or
        /// <paramref name="portablePdbStream"/> is <see langword="null"/>.
        /// </exception>
        public static bool TryCreate(
            ExternalPeDebugDirectoryDescriptor expectedDebugDescriptor,
            Stream portablePdbStream,
            out ExternalCompilationProvenanceDescriptor descriptor)
        {
            ArgumentNullException.ThrowIfNull(expectedDebugDescriptor);
            ArgumentNullException.ThrowIfNull(portablePdbStream);

            if (!portablePdbStream.CanRead)
            {
                descriptor = null!;
                return false;
            }

            try
            {
                using MemoryStream snapshotStream = new();
                portablePdbStream.CopyTo(snapshotStream);
                ImmutableArray<byte> portablePdbImage =
                    ImmutableCollectionsMarshal.AsImmutableArray(snapshotStream.ToArray());
                return TryCreate(expectedDebugDescriptor, portablePdbImage, out descriptor);
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
        /// Tries to validate and describe one explicitly supplied Portable PDB
        /// file candidate.
        /// </summary>
        /// <param name="expectedDebugDescriptor">
        /// The validated PE debug provenance that identifies acceptable PDBs.
        /// </param>
        /// <param name="candidatePdbPath">
        /// The caller-selected candidate path, which is not PDB identity.
        /// </param>
        /// <param name="descriptor">
        /// The validated PDB and compilation provenance when available.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when the explicitly selected file contains
        /// valid P4B and P5A provenance; otherwise <see langword="false"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="expectedDebugDescriptor"/> or
        /// <paramref name="candidatePdbPath"/> is <see langword="null"/>.
        /// </exception>
        public static bool TryCreateFromFile(
            ExternalPeDebugDirectoryDescriptor expectedDebugDescriptor,
            string candidatePdbPath,
            out ExternalCompilationProvenanceDescriptor descriptor)
        {
            ArgumentNullException.ThrowIfNull(expectedDebugDescriptor);
            ArgumentNullException.ThrowIfNull(candidatePdbPath);

            try
            {
                using FileStream stream = new(
                    candidatePdbPath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read);
                return TryCreate(expectedDebugDescriptor, stream, out descriptor);
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
        /// Applies P4B validation and reads P5A provenance from the same image.
        /// </summary>
        /// <param name="expectedDebugDescriptor">The expected PE debug provenance.</param>
        /// <param name="portablePdbImage">The complete immutable candidate image.</param>
        /// <param name="descriptor">The combined provenance when valid.</param>
        /// <returns>
        /// <see langword="true"/> when validation and metadata parsing
        /// succeed; otherwise <see langword="false"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="expectedDebugDescriptor"/> is
        /// <see langword="null"/>.
        /// </exception>
        internal static bool TryCreate(
            ExternalPeDebugDirectoryDescriptor expectedDebugDescriptor,
            ImmutableArray<byte> portablePdbImage,
            out ExternalCompilationProvenanceDescriptor descriptor)
        {
            if (!ExternalPortablePdbDescriptorFactory.TryCreate(
                    expectedDebugDescriptor,
                    portablePdbImage,
                    out ExternalPortablePdbDescriptor portablePdb))
            {
                descriptor = null!;
                return false;
            }

            try
            {
                using MetadataReaderProvider provider =
                    MetadataReaderProvider.FromPortablePdbImage(portablePdbImage);
                MetadataReader metadataReader = provider.GetMetadataReader();

                if (!TryReadCompilationMetadata(
                        metadataReader,
                        out ExternalCompilationOptionsDescriptor? compilationOptions,
                        out ExternalCompilationMetadataReferencesDescriptor? metadataReferences))
                {
                    descriptor = null!;
                    return false;
                }

                descriptor = new ExternalCompilationProvenanceDescriptor(
                    expectedDebugDescriptor,
                    portablePdb,
                    compilationOptions,
                    metadataReferences);
                return true;
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
        /// Reads the unique supported module-level compilation metadata blobs.
        /// </summary>
        /// <param name="metadataReader">The P4B-validated PDB reader.</param>
        /// <param name="compilationOptions">
        /// The parsed options, or <see langword="null"/> when absent.
        /// </param>
        /// <param name="metadataReferences">
        /// The parsed references, or <see langword="null"/> when absent.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when supported records are unambiguous and
        /// valid; otherwise <see langword="false"/>.
        /// </returns>
        private static bool TryReadCompilationMetadata(
            MetadataReader metadataReader,
            out ExternalCompilationOptionsDescriptor? compilationOptions,
            out ExternalCompilationMetadataReferencesDescriptor? metadataReferences)
        {
            BlobHandle? compilationOptionsBlob = null;
            BlobHandle? metadataReferencesBlob = null;
            EntityHandle moduleHandle = MetadataTokens.EntityHandle(TableIndex.Module, 1);

            foreach (CustomDebugInformationHandle handle in
                     metadataReader.GetCustomDebugInformation(moduleHandle))
            {
                CustomDebugInformation customDebugInformation =
                    metadataReader.GetCustomDebugInformation(handle);
                Guid kind = metadataReader.GetGuid(customDebugInformation.Kind);

                if (kind == CompilationOptionsKind)
                {
                    if (compilationOptionsBlob.HasValue)
                    {
                        compilationOptions = null;
                        metadataReferences = null;
                        return false;
                    }

                    compilationOptionsBlob = customDebugInformation.Value;
                }
                else if (kind == CompilationMetadataReferencesKind)
                {
                    if (metadataReferencesBlob.HasValue)
                    {
                        compilationOptions = null;
                        metadataReferences = null;
                        return false;
                    }

                    metadataReferencesBlob = customDebugInformation.Value;
                }
            }

            if (compilationOptionsBlob.HasValue)
            {
                if (!ExternalCompilationOptionsDescriptorFactory.TryCreate(
                        metadataReader.GetBlobContent(compilationOptionsBlob.Value),
                        out ExternalCompilationOptionsDescriptor parsedOptions))
                {
                    compilationOptions = null;
                    metadataReferences = null;
                    return false;
                }

                compilationOptions = parsedOptions;
            }
            else
            {
                compilationOptions = null;
            }

            if (metadataReferencesBlob.HasValue)
            {
                if (!ExternalCompilationMetadataReferencesDescriptorFactory.TryCreate(
                        metadataReader.GetBlobContent(metadataReferencesBlob.Value),
                        out ExternalCompilationMetadataReferencesDescriptor parsedReferences))
                {
                    compilationOptions = null;
                    metadataReferences = null;
                    return false;
                }

                metadataReferences = parsedReferences;
            }
            else
            {
                metadataReferences = null;
            }

            return true;
        }
    }
}
