using System.Collections.Immutable;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using Microsoft.CodeAnalysis;

namespace XMLDocNormalizer.Execution.Semantic
{
    /// <summary>
    /// Reads validated debug provenance from an external assembly's manifest
    /// PE image.
    /// </summary>
    internal static class ExternalPeDebugDirectoryDescriptorFactory
    {
        /// <summary>
        /// Tries to read debug provenance from a caller-owned PE stream.
        /// </summary>
        /// <param name="expectedDescriptor">
        /// The binary descriptor previously established from Roslyn's binding.
        /// </param>
        /// <param name="peStream">
        /// The readable PE stream, beginning at its current position. The
        /// caller retains ownership and the stream remains open.
        /// </param>
        /// <param name="descriptor">
        /// The validated debug-directory descriptor when available.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when the stream contains the expected
        /// manifest PE and its supported debug provenance is valid; otherwise
        /// <see langword="false"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="expectedDescriptor"/> or
        /// <paramref name="peStream"/> is <see langword="null"/>.
        /// </exception>
        public static bool TryCreate(
            ExternalAssemblyReferenceDescriptor expectedDescriptor,
            Stream peStream,
            out ExternalPeDebugDirectoryDescriptor descriptor)
        {
            ArgumentNullException.ThrowIfNull(expectedDescriptor);
            ArgumentNullException.ThrowIfNull(peStream);

            if (!peStream.CanRead)
            {
                descriptor = null!;
                return false;
            }

            try
            {
                using PEReader peReader = new(
                    peStream,
                    PEStreamOptions.LeaveOpen | PEStreamOptions.PrefetchEntireImage);
                return TryCreate(expectedDescriptor, peReader, out descriptor);
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
        /// Tries to read debug provenance by reopening the file path recorded
        /// by a binary descriptor.
        /// </summary>
        /// <param name="expectedDescriptor">
        /// The binary descriptor whose current file snapshot must be validated.
        /// </param>
        /// <param name="descriptor">
        /// The validated debug-directory descriptor when available.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when the recorded file exists, is readable,
        /// matches the expected binary identity, and contains valid supported
        /// debug provenance; otherwise <see langword="false"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="expectedDescriptor"/> is
        /// <see langword="null"/>.
        /// </exception>
        public static bool TryCreateFromFile(
            ExternalAssemblyReferenceDescriptor expectedDescriptor,
            out ExternalPeDebugDirectoryDescriptor descriptor)
        {
            ArgumentNullException.ThrowIfNull(expectedDescriptor);

            if (expectedDescriptor.FilePath == null)
            {
                descriptor = null!;
                return false;
            }

            try
            {
                using FileStream stream = new(
                    expectedDescriptor.FilePath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read);
                return TryCreate(expectedDescriptor, stream, out descriptor);
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
        /// Validates a prefetched PE image and reads its supported debug data.
        /// </summary>
        /// <param name="expectedDescriptor">The expected binary descriptor.</param>
        /// <param name="peReader">The reader for the complete PE snapshot.</param>
        /// <param name="descriptor">The resulting descriptor when valid.</param>
        /// <returns>
        /// <see langword="true"/> when the PE identity and debug data are
        /// valid; otherwise <see langword="false"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="expectedDescriptor"/> or
        /// <paramref name="peReader"/> is <see langword="null"/>.
        /// </exception>
        private static bool TryCreate(
            ExternalAssemblyReferenceDescriptor expectedDescriptor,
            PEReader peReader,
            out ExternalPeDebugDirectoryDescriptor descriptor)
        {
            ArgumentNullException.ThrowIfNull(expectedDescriptor);
            ArgumentNullException.ThrowIfNull(peReader);

            if (!TryValidateManifestPe(
                    expectedDescriptor,
                    peReader,
                    out ExternalModuleIdentity manifestModule))
            {
                descriptor = null!;
                return false;
            }

            ImmutableArray<ExternalCodeViewPdbReference>.Builder codeViewReferences =
                ImmutableArray.CreateBuilder<ExternalCodeViewPdbReference>();
            ImmutableArray<BlobContentId>.Builder embeddedPdbIds =
                ImmutableArray.CreateBuilder<BlobContentId>();
            ImmutableArray<ExternalPdbChecksum>.Builder pdbChecksums =
                ImmutableArray.CreateBuilder<ExternalPdbChecksum>();
            bool isDeterministic = false;

            foreach (DebugDirectoryEntry entry in peReader.ReadDebugDirectory())
            {
                switch (entry.Type)
                {
                    case DebugDirectoryEntryType.CodeView:
                        CodeViewDebugDirectoryData codeViewData =
                            peReader.ReadCodeViewDebugDirectoryData(entry);
                        BlobContentId? portablePdbId = entry.IsPortableCodeView
                            ? new BlobContentId(codeViewData.Guid, entry.Stamp)
                            : null;
                        codeViewReferences.Add(
                            new ExternalCodeViewPdbReference(
                                codeViewData.Path,
                                codeViewData.Guid,
                                codeViewData.Age,
                                entry.Stamp,
                                entry.IsPortableCodeView,
                                portablePdbId));
                        break;

                    case DebugDirectoryEntryType.EmbeddedPortablePdb:
                        using (MetadataReaderProvider provider =
                               peReader.ReadEmbeddedPortablePdbDebugDirectoryData(entry))
                        {
                            MetadataReader metadataReader = provider.GetMetadataReader();
                            DebugMetadataHeader? debugHeader = metadataReader.DebugMetadataHeader;

                            if (debugHeader == null)
                            {
                                descriptor = null!;
                                return false;
                            }

                            embeddedPdbIds.Add(new BlobContentId(debugHeader.Id));
                        }

                        break;

                    case DebugDirectoryEntryType.PdbChecksum:
                        PdbChecksumDebugDirectoryData checksumData =
                            peReader.ReadPdbChecksumDebugDirectoryData(entry);
                        pdbChecksums.Add(
                            new ExternalPdbChecksum(
                                checksumData.AlgorithmName,
                                checksumData.Checksum));
                        break;

                    case DebugDirectoryEntryType.Reproducible:
                        isDeterministic = true;
                        break;
                }
            }

            ImmutableArray<ExternalCodeViewPdbReference> codeViews =
                codeViewReferences.ToImmutable();
            ImmutableArray<BlobContentId> embeddedIds = embeddedPdbIds.ToImmutable();

            if (!HaveConsistentEmbeddedPdbIds(codeViews, embeddedIds))
            {
                descriptor = null!;
                return false;
            }

            descriptor = new ExternalPeDebugDirectoryDescriptor(
                manifestModule,
                isDeterministic,
                codeViews,
                embeddedIds,
                pdbChecksums.ToImmutable());
            return true;
        }

        /// <summary>
        /// Validates the assembly and manifest-module identity of a PE image.
        /// </summary>
        /// <param name="expectedDescriptor">The expected P3 binary descriptor.</param>
        /// <param name="peReader">The PE reader.</param>
        /// <param name="manifestModule">The validated manifest module.</param>
        /// <returns>
        /// <see langword="true"/> when all manifest identity fields match;
        /// otherwise <see langword="false"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="expectedDescriptor"/> or
        /// <paramref name="peReader"/> is <see langword="null"/>.
        /// </exception>
        private static bool TryValidateManifestPe(
            ExternalAssemblyReferenceDescriptor expectedDescriptor,
            PEReader peReader,
            out ExternalModuleIdentity manifestModule)
        {
            ArgumentNullException.ThrowIfNull(expectedDescriptor);
            ArgumentNullException.ThrowIfNull(peReader);

            if (expectedDescriptor.Modules.IsDefaultOrEmpty || !peReader.HasMetadata)
            {
                manifestModule = default;
                return false;
            }

            MetadataReader metadataReader = peReader.GetMetadataReader();

            if (!metadataReader.IsAssembly)
            {
                manifestModule = default;
                return false;
            }

            if (!ExternalPeMetadataIdentityReader.TryReadAssemblyIdentity(
                    metadataReader,
                    out AssemblyIdentity assemblyIdentity))
            {
                manifestModule = default;
                return false;
            }

            ModuleDefinition moduleDefinition = metadataReader.GetModuleDefinition();
            manifestModule = new ExternalModuleIdentity(
                metadataReader.GetString(moduleDefinition.Name),
                metadataReader.GetGuid(moduleDefinition.Mvid));

            return expectedDescriptor.AssemblyIdentity.Equals(assemblyIdentity)
                && expectedDescriptor.Modules[0].Equals(manifestModule);
        }

        /// <summary>
        /// Checks that every embedded PDB identity is referenced by portable
        /// CodeView data when both kinds of entries are present.
        /// </summary>
        /// <param name="codeViewReferences">The CodeView references.</param>
        /// <param name="embeddedPdbIds">The embedded PDB identifiers.</param>
        /// <returns>
        /// <see langword="true"/> when no explicit internal identity conflict
        /// exists; otherwise <see langword="false"/>.
        /// </returns>
        private static bool HaveConsistentEmbeddedPdbIds(
            ImmutableArray<ExternalCodeViewPdbReference> codeViewReferences,
            ImmutableArray<BlobContentId> embeddedPdbIds)
        {
            if (embeddedPdbIds.IsEmpty
                || !codeViewReferences.Any(reference => reference.PortablePdbId.HasValue))
            {
                return true;
            }

            return embeddedPdbIds.All(
                embeddedId => codeViewReferences.Any(
                    reference => reference.PortablePdbId.HasValue
                        && reference.PortablePdbId.Value.Equals(embeddedId)));
        }
    }
}
