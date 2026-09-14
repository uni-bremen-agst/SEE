using System.Collections.Immutable;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Security.Cryptography;

namespace XMLDocNormalizer.Execution.Semantic
{
    /// <summary>
    /// Validates an explicitly supplied Portable PDB candidate against PE
    /// debug provenance and reads source-document provenance from that same
    /// candidate snapshot.
    /// </summary>
    internal static class ExternalPortablePdbDescriptorFactory
    {
        /// <summary>
        /// The standardized module-level Source Link custom debug information
        /// identifier.
        /// </summary>
        private static readonly Guid SourceLinkKind =
            new("cc110556-a091-4d38-9fec-25ab9a351a6a");

        /// <summary>
        /// The standardized document-level Embedded Source custom debug
        /// information identifier.
        /// </summary>
        private static readonly Guid EmbeddedSourceKind =
            new("0e8a571b-6926-466e-b4ad-8ab04611f5fe");

        /// <summary>
        /// The fixed Portable PDB content-ID size in bytes.
        /// </summary>
        private const int PortablePdbIdSize = 20;

        /// <summary>
        /// Tries to validate and describe a caller-owned Portable PDB stream.
        /// </summary>
        /// <param name="expectedDebugDescriptor">
        /// The validated PE debug provenance that identifies acceptable PDBs.
        /// </param>
        /// <param name="portablePdbStream">
        /// The readable candidate stream, beginning at its current position.
        /// The caller retains ownership and the stream remains open.
        /// </param>
        /// <param name="descriptor">
        /// The validated Portable PDB and source provenance when available.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when the candidate is a matching Portable
        /// PDB, all required PE checksums validate, and its supported source
        /// provenance is internally valid; otherwise <see langword="false"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="expectedDebugDescriptor"/> or
        /// <paramref name="portablePdbStream"/> is <see langword="null"/>.
        /// </exception>
        public static bool TryCreate(
            ExternalPeDebugDirectoryDescriptor expectedDebugDescriptor,
            Stream portablePdbStream,
            out ExternalPortablePdbDescriptor descriptor)
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
                byte[] candidateBytes = snapshotStream.ToArray();
                using MemoryStream metadataStream = new(candidateBytes, writable: false);
                using MetadataReaderProvider provider =
                    MetadataReaderProvider.FromPortablePdbStream(
                        metadataStream,
                        MetadataStreamOptions.LeaveOpen |
                        MetadataStreamOptions.PrefetchMetadata);
                MetadataReader metadataReader = provider.GetMetadataReader();
                DebugMetadataHeader? debugHeader = metadataReader.DebugMetadataHeader;

                if (debugHeader == null || debugHeader.Id.Length != PortablePdbIdSize)
                {
                    descriptor = null!;
                    return false;
                }

                BlobContentId candidateId = new(debugHeader.Id);

                if (!IsExpectedCandidate(expectedDebugDescriptor, candidateId)
                    || !TryValidatePdbChecksums(
                        expectedDebugDescriptor.PdbChecksums,
                        candidateBytes,
                        debugHeader.IdStartOffset,
                        out PortablePdbValidationKind validationKind)
                    || !TryReadSourceProvenance(
                        metadataReader,
                        out ImmutableArray<ExternalSourceDocumentDescriptor> documents,
                        out ExternalSourceLinkDescriptor? sourceLink))
                {
                    descriptor = null!;
                    return false;
                }

                descriptor = new ExternalPortablePdbDescriptor(
                    candidateId,
                    validationKind,
                    documents,
                    sourceLink);
                return true;
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
        /// Tries to validate and describe one explicitly supplied Portable PDB
        /// file candidate.
        /// </summary>
        /// <param name="expectedDebugDescriptor">
        /// The validated PE debug provenance that identifies acceptable PDBs.
        /// </param>
        /// <param name="candidatePdbPath">
        /// The caller-selected candidate path. It is not part of PDB identity.
        /// </param>
        /// <param name="descriptor">
        /// The validated Portable PDB and source provenance when available.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when the explicitly selected file contains
        /// a valid matching Portable PDB; otherwise <see langword="false"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="expectedDebugDescriptor"/> or
        /// <paramref name="candidatePdbPath"/> is <see langword="null"/>.
        /// </exception>
        public static bool TryCreateFromFile(
            ExternalPeDebugDirectoryDescriptor expectedDebugDescriptor,
            string candidatePdbPath,
            out ExternalPortablePdbDescriptor descriptor)
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
        /// Determines whether the candidate ID matches any Portable PDB ID
        /// explicitly recorded by P4A.
        /// </summary>
        /// <param name="expectedDebugDescriptor">The PE debug provenance.</param>
        /// <param name="candidateId">The candidate Portable PDB ID.</param>
        /// <returns>
        /// <see langword="true"/> when at least one expected ID exists and
        /// matches; otherwise <see langword="false"/>.
        /// </returns>
        private static bool IsExpectedCandidate(
            ExternalPeDebugDirectoryDescriptor expectedDebugDescriptor,
            BlobContentId candidateId)
        {
            foreach (ExternalCodeViewPdbReference codeView in
                     expectedDebugDescriptor.CodeViewPdbReferences)
            {
                if (codeView.PortablePdbId.HasValue
                    && codeView.PortablePdbId.Value.Equals(candidateId))
                {
                    return true;
                }
            }

            return expectedDebugDescriptor.EmbeddedPortablePdbIds.Contains(candidateId);
        }

        /// <summary>
        /// Validates optional PE-provided Portable PDB checksums against the
        /// unchanged candidate snapshot.
        /// </summary>
        /// <param name="expectedChecksums">The PE-provided checksum entries.</param>
        /// <param name="candidateBytes">The complete candidate snapshot.</param>
        /// <param name="idStartOffset">
        /// The metadata-reported offset of the 20-byte PDB ID.
        /// </param>
        /// <param name="validationKind">
        /// The successful validation strength when a match is found.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when no checksum is required or at least one
        /// supported checksum matches; otherwise <see langword="false"/>.
        /// </returns>
        private static bool TryValidatePdbChecksums(
            ImmutableArray<ExternalPdbChecksum> expectedChecksums,
            byte[] candidateBytes,
            int idStartOffset,
            out PortablePdbValidationKind validationKind)
        {
            if (expectedChecksums.IsDefaultOrEmpty)
            {
                validationKind = PortablePdbValidationKind.Identity;
                return true;
            }

            if (idStartOffset < 0
                || idStartOffset > candidateBytes.Length - PortablePdbIdSize)
            {
                validationKind = default;
                return false;
            }

            Dictionary<string, byte[]> calculatedChecksums =
                new(StringComparer.Ordinal);

            foreach (ExternalPdbChecksum expectedChecksum in expectedChecksums)
            {
                if (!TryGetPdbChecksumAlgorithm(
                        expectedChecksum.AlgorithmName,
                        out HashAlgorithmName hashAlgorithm))
                {
                    continue;
                }

                if (!calculatedChecksums.TryGetValue(
                        expectedChecksum.AlgorithmName,
                        out byte[]? calculatedChecksum))
                {
                    calculatedChecksum = CalculatePortablePdbChecksum(
                        candidateBytes,
                        idStartOffset,
                        hashAlgorithm);
                    calculatedChecksums.Add(
                        expectedChecksum.AlgorithmName,
                        calculatedChecksum);
                }

                if (CryptographicOperations.FixedTimeEquals(
                        calculatedChecksum,
                        expectedChecksum.Checksum.AsSpan()))
                {
                    validationKind = PortablePdbValidationKind.IdentityAndChecksum;
                    return true;
                }
            }

            validationKind = default;
            return false;
        }

        /// <summary>
        /// Calculates a Portable PDB checksum with the metadata-reported ID
        /// bytes treated as zero.
        /// </summary>
        /// <param name="candidateBytes">The complete candidate snapshot.</param>
        /// <param name="idStartOffset">The PDB ID start offset.</param>
        /// <param name="hashAlgorithm">The selected cryptographic algorithm.</param>
        /// <returns>The calculated checksum bytes.</returns>
        private static byte[] CalculatePortablePdbChecksum(
            byte[] candidateBytes,
            int idStartOffset,
            HashAlgorithmName hashAlgorithm)
        {
            using IncrementalHash hash = IncrementalHash.CreateHash(hashAlgorithm);
            hash.AppendData(candidateBytes, 0, idStartOffset);
            hash.AppendData(new byte[PortablePdbIdSize]);
            int trailingDataOffset = idStartOffset + PortablePdbIdSize;
            hash.AppendData(
                candidateBytes,
                trailingDataOffset,
                candidateBytes.Length - trailingDataOffset);
            return hash.GetHashAndReset();
        }

        /// <summary>
        /// Maps supported case-sensitive PE PDB checksum names to concrete
        /// cryptographic algorithms.
        /// </summary>
        /// <param name="algorithmName">The exact PE algorithm name.</param>
        /// <param name="hashAlgorithm">The concrete algorithm when supported.</param>
        /// <returns>
        /// <see langword="true"/> for SHA256, SHA384, or SHA512; otherwise
        /// <see langword="false"/>.
        /// </returns>
        private static bool TryGetPdbChecksumAlgorithm(
            string algorithmName,
            out HashAlgorithmName hashAlgorithm)
        {
            switch (algorithmName)
            {
                case "SHA256":
                    hashAlgorithm = HashAlgorithmName.SHA256;
                    return true;

                case "SHA384":
                    hashAlgorithm = HashAlgorithmName.SHA384;
                    return true;

                case "SHA512":
                    hashAlgorithm = HashAlgorithmName.SHA512;
                    return true;

                default:
                    hashAlgorithm = default;
                    return false;
            }
        }

        /// <summary>
        /// Reads Source Link and document provenance after candidate identity
        /// and checksum validation have succeeded.
        /// </summary>
        /// <param name="metadataReader">The validated Portable PDB reader.</param>
        /// <param name="documents">The ordered document provenance.</param>
        /// <param name="sourceLink">The optional module Source Link descriptor.</param>
        /// <returns>
        /// <see langword="true"/> when supported custom debug information and
        /// all documents are internally valid; otherwise
        /// <see langword="false"/>.
        /// </returns>
        private static bool TryReadSourceProvenance(
            MetadataReader metadataReader,
            out ImmutableArray<ExternalSourceDocumentDescriptor> documents,
            out ExternalSourceLinkDescriptor? sourceLink)
        {
            if (!TryIndexCustomDebugInformation(
                    metadataReader,
                    out BlobHandle? sourceLinkBlob,
                    out Dictionary<DocumentHandle, BlobHandle> embeddedSources))
            {
                documents = default;
                sourceLink = null;
                return false;
            }

            if (sourceLinkBlob.HasValue)
            {
                if (!ExternalSourceLinkDescriptor.TryCreate(
                        metadataReader.GetBlobContent(sourceLinkBlob.Value),
                        out ExternalSourceLinkDescriptor parsedSourceLink))
                {
                    documents = default;
                    sourceLink = null;
                    return false;
                }

                sourceLink = parsedSourceLink;
            }
            else
            {
                sourceLink = null;
            }

            ImmutableArray<ExternalSourceDocumentDescriptor>.Builder documentBuilder =
                ImmutableArray.CreateBuilder<ExternalSourceDocumentDescriptor>(
                    metadataReader.Documents.Count);
            HashSet<string> documentNames = new(StringComparer.Ordinal);

            foreach (DocumentHandle handle in metadataReader.Documents)
            {
                Document document = metadataReader.GetDocument(handle);
                string? name = metadataReader.GetString(document.Name);

                if (name == null || !documentNames.Add(name))
                {
                    documents = default;
                    sourceLink = null;
                    return false;
                }

                Guid hashAlgorithm = metadataReader.GetGuid(document.HashAlgorithm);
                ImmutableArray<byte> documentHash =
                    metadataReader.GetBlobContent(document.Hash);
                ExternalEmbeddedSourceProvenance? embeddedSource = null;

                if (embeddedSources.TryGetValue(handle, out BlobHandle embeddedSourceBlob))
                {
                    if (!ExternalEmbeddedSourceProvenanceFactory.TryCreate(
                            metadataReader.GetBlobContent(embeddedSourceBlob),
                            hashAlgorithm,
                            documentHash,
                            out ExternalEmbeddedSourceProvenance parsedEmbeddedSource))
                    {
                        documents = default;
                        sourceLink = null;
                        return false;
                    }

                    embeddedSource = parsedEmbeddedSource;
                }

                documentBuilder.Add(
                    new ExternalSourceDocumentDescriptor(
                        name,
                        hashAlgorithm,
                        documentHash,
                        metadataReader.GetGuid(document.Language),
                        embeddedSource));
            }

            documents = documentBuilder.MoveToImmutable();
            return true;
        }

        /// <summary>
        /// Traverses custom debug information once and indexes only the
        /// standardized Source Link and Embedded Source records.
        /// </summary>
        /// <param name="metadataReader">The validated Portable PDB reader.</param>
        /// <param name="sourceLinkBlob">The unique module Source Link blob.</param>
        /// <param name="embeddedSources">
        /// The unique Embedded Source blob for each document.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when standardized records are unambiguous;
        /// otherwise <see langword="false"/>.
        /// </returns>
        private static bool TryIndexCustomDebugInformation(
            MetadataReader metadataReader,
            out BlobHandle? sourceLinkBlob,
            out Dictionary<DocumentHandle, BlobHandle> embeddedSources)
        {
            sourceLinkBlob = null;
            embeddedSources = new Dictionary<DocumentHandle, BlobHandle>();
            EntityHandle moduleHandle = MetadataTokens.EntityHandle(TableIndex.Module, 1);

            foreach (CustomDebugInformationHandle handle in
                     metadataReader.CustomDebugInformation)
            {
                CustomDebugInformation customDebugInformation =
                    metadataReader.GetCustomDebugInformation(handle);
                Guid kind = metadataReader.GetGuid(customDebugInformation.Kind);

                if (kind == SourceLinkKind
                    && customDebugInformation.Parent.Equals(moduleHandle))
                {
                    if (sourceLinkBlob.HasValue)
                    {
                        return false;
                    }

                    sourceLinkBlob = customDebugInformation.Value;
                }
                else if (kind == EmbeddedSourceKind
                    && customDebugInformation.Parent.Kind == HandleKind.Document)
                {
                    DocumentHandle documentHandle =
                        (DocumentHandle)customDebugInformation.Parent;
                    int rowNumber = MetadataTokens.GetRowNumber(documentHandle);

                    if (rowNumber <= 0
                        || rowNumber > metadataReader.Documents.Count
                        || !embeddedSources.TryAdd(
                            documentHandle,
                            customDebugInformation.Value))
                    {
                        return false;
                    }
                }
            }

            return true;
        }
    }
}
