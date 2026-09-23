using System.Collections.Immutable;
using System.Runtime.InteropServices;

namespace XMLDocNormalizer.Execution.Semantic
{
    /// <summary>
    /// Acquires local or Source Link candidate bytes for one expected Portable
    /// PDB document and accepts them only through existing P5H validation.
    /// </summary>
    /// <remarks>
    /// Acquisition locates candidate source bytes. It does not establish
    /// source identity. Search mappings and Source Link are explicit,
    /// context-local, immutable after first use, and demand-driven.
    /// </remarks>
    internal sealed class ExternalSourceAcquisition
    {
        /// <summary>
        /// Compares local paths according to current platform conventions.
        /// </summary>
        private static readonly StringComparer PathComparer = OperatingSystem.IsWindows()
            ? StringComparer.OrdinalIgnoreCase
            : StringComparer.Ordinal;

        /// <summary>
        /// Serializes configuration and per-document acquisition transitions.
        /// </summary>
        private readonly object gate = new();

        /// <summary>
        /// Stores positive, negative, and in-progress acquisition results.
        /// </summary>
        private readonly Dictionary<AcquisitionKey, Entry> entries = new();

        /// <summary>
        /// Stores normalized local mappings in deterministic precedence order.
        /// </summary>
        private ImmutableArray<NormalizedMapping> mappings =
            ImmutableArray<NormalizedMapping>.Empty;

        /// <summary>
        /// Stores the optional explicitly enabled context-local network client.
        /// </summary>
        private ExternalSourceLinkClient? sourceLinkClient;

        /// <summary>
        /// Stores the context-local reconstruction policy. Strict is the default.
        /// </summary>
        private ExternalSourceReconstructionPolicy reconstructionPolicy =
            ExternalSourceReconstructionPolicy.Strict;

        /// <summary>
        /// Records whether local mappings were configured.
        /// </summary>
        private bool mappingsConfigured;

        /// <summary>
        /// Records whether Source Link policy was configured.
        /// </summary>
        private bool sourceLinkConfigured;

        /// <summary>
        /// Records whether the reconstruction policy was explicitly configured.
        /// </summary>
        private bool reconstructionPolicyConfigured;

        /// <summary>
        /// Records whether any acquisition attempt has started.
        /// </summary>
        private bool acquisitionStarted;

        /// <summary>Counts directly validated acquired candidates.</summary>
        private long directExactSourceCount;

        /// <summary>Counts verified-policy reconstruction attempts.</summary>
        private long reconstructionAttemptCount;

        /// <summary>Counts reconstruction attempts accepted by P5H.</summary>
        private long reconstructionSuccessCount;

        /// <summary>Counts reconstruction attempts with no P5H-valid candidate.</summary>
        private long reconstructionFailureCount;

        /// <summary>Counts accepted bare-LF to CRLF candidates.</summary>
        private long lfToCrlfSuccessCount;

        /// <summary>Counts accepted CRLF to LF candidates.</summary>
        private long crlfToLfSuccessCount;

        /// <summary>Counts P5H validations of acquired and reconstructed bytes.</summary>
        private long p5HValidationAttempts;

        /// <summary>Counts Source Link requests.</summary>
        private long sourceLinkRequests;

        /// <summary>Counts successfully downloaded original bytes.</summary>
        private long downloadedSourceBytes;

        /// <summary>Counts bytes produced across reconstruction candidates.</summary>
        private long reconstructionBytesProduced;

        /// <summary>Counts elapsed reconstruction timer ticks.</summary>
        private long reconstructionDurationTicks;

        /// <summary>Counts successful cache lookups.</summary>
        private long positiveCacheHits;

        /// <summary>Counts failed cache lookups.</summary>
        private long negativeCacheHits;

        /// <summary>
        /// Configures the complete local document-prefix projection set
        /// without touching the filesystem.
        /// </summary>
        /// <param name="sourceMappings">
        /// Mappings from PDB document-name prefixes to fully qualified roots.
        /// Multiple roots may share one prefix.
        /// </param>
        /// <returns>
        /// <see langword="true"/> for a new or idempotent normalized mapping
        /// set; otherwise <see langword="false"/>.
        /// </returns>
        public bool TryConfigureMappings(
            IEnumerable<ExternalSourcePathMapping> sourceMappings)
        {
            if (sourceMappings == null
                || !TryNormalizeMappings(sourceMappings, out var normalized))
            {
                return false;
            }

            lock (gate)
            {
                if (mappingsConfigured)
                {
                    return MappingsEqual(mappings, normalized);
                }

                if (acquisitionStarted)
                {
                    return false;
                }

                mappings = normalized;
                mappingsConfigured = true;
                return true;
            }
        }

        /// <summary>
        /// Configures the sole context-local Source Link network client.
        /// </summary>
        /// <param name="client">The explicitly enabled bounded HTTPS client.</param>
        /// <returns>
        /// <see langword="true"/> for new or reference-idempotent
        /// configuration; otherwise <see langword="false"/>.
        /// </returns>
        public bool TryConfigureSourceLink(ExternalSourceLinkClient client)
        {
            if (client == null)
            {
                return false;
            }

            lock (gate)
            {
                if (sourceLinkConfigured)
                {
                    return ReferenceEquals(sourceLinkClient, client);
                }

                if (acquisitionStarted)
                {
                    return false;
                }

                sourceLinkClient = client;
                sourceLinkConfigured = true;
                return true;
            }
        }

        /// <summary>
        /// Configures the context-local source reconstruction policy before
        /// any acquisition attempt begins.
        /// </summary>
        /// <param name="policy">
        /// Strict direct-byte validation or verified line-ending candidates.
        /// </param>
        /// <returns>
        /// <see langword="true"/> for new or value-idempotent configuration;
        /// otherwise <see langword="false"/>.
        /// </returns>
        public bool TryConfigureReconstructionPolicy(
            ExternalSourceReconstructionPolicy policy)
        {
            if (!Enum.IsDefined(policy))
            {
                return false;
            }

            lock (gate)
            {
                if (reconstructionPolicyConfigured)
                {
                    return reconstructionPolicy == policy;
                }

                if (acquisitionStarted)
                {
                    return false;
                }

                reconstructionPolicy = policy;
                reconstructionPolicyConfigured = true;
                return true;
            }
        }

        /// <summary>
        /// Gets an atomic snapshot of bounded acquisition and reconstruction work.
        /// </summary>
        /// <returns>The current context-local statistics.</returns>
        public ExternalSourceAcquisitionStatistics GetStatistics()
        {
            lock (gate)
            {
                return new ExternalSourceAcquisitionStatistics(
                    directExactSourceCount,
                    reconstructionAttemptCount,
                    reconstructionSuccessCount,
                    reconstructionFailureCount,
                    lfToCrlfSuccessCount,
                    crlfToLfSuccessCount,
                    p5HValidationAttempts,
                    sourceLinkRequests,
                    downloadedSourceBytes,
                    reconstructionBytesProduced,
                    reconstructionDurationTicks,
                    positiveCacheHits,
                    negativeCacheHits);
            }
        }

        /// <summary>
        /// Tries local projections and then explicitly enabled Source Link for
        /// one source document, returning only checksum-validated P5H material.
        /// </summary>
        /// <param name="document">The expected Portable PDB document.</param>
        /// <param name="sourceLink">The optional P4B Source Link provenance.</param>
        /// <param name="material">The validated material when successful.</param>
        /// <returns>
        /// <see langword="true"/> only when controlled acquisition supplies
        /// bytes accepted by P5H; otherwise <see langword="false"/>.
        /// </returns>
        public bool TryAcquire(
            ExternalSourceDocumentDescriptor document,
            ExternalSourceLinkDescriptor? sourceLink,
            out ValidatedExternalSourceMaterial material)
        {
            return TryAcquire(
                document,
                sourceLink,
                configuration: null,
                out material);
        }

        /// <summary>
        /// Tries controlled acquisition with optional P5G encoding provenance
        /// for explicitly enabled verified line-ending reconstruction.
        /// </summary>
        /// <param name="document">The expected Portable PDB document.</param>
        /// <param name="sourceLink">The optional P4B Source Link provenance.</param>
        /// <param name="configuration">
        /// The P5G configuration used only to establish a safe source encoding.
        /// </param>
        /// <param name="material">The checksum-validated material.</param>
        /// <returns><see langword="true"/> only for P5H-valid bytes.</returns>
        public bool TryAcquire(
            ExternalSourceDocumentDescriptor document,
            ExternalSourceLinkDescriptor? sourceLink,
            ExternalCSharpCompilationConfiguration? configuration,
            out ValidatedExternalSourceMaterial material)
        {
            if (document == null)
            {
                material = null!;
                return false;
            }

            ExternalSourceLinkClient? client;
            ImmutableArray<NormalizedMapping> mappingSnapshot;
            ExternalSourceReconstructionPolicy policy;

            lock (gate)
            {
                acquisitionStarted = true;
                client = sourceLinkClient;
                mappingSnapshot = mappings;
                policy = reconstructionPolicy;
            }

            Uri? sourceUri = TryResolveSafeSourceUri(
                client,
                sourceLink,
                document.Name);
            AcquisitionKey key = AcquisitionKey.Create(
                document,
                sourceUri,
                policy,
                configuration);

            lock (gate)
            {
                if (entries.TryGetValue(key, out Entry? cached))
                {
                    if (cached.State == AcquisitionState.Succeeded)
                    {
                        positiveCacheHits++;
                    }
                    else if (cached.State == AcquisitionState.Failed)
                    {
                        negativeCacheHits++;
                    }

                    material = cached.Material!;
                    return cached.State == AcquisitionState.Succeeded;
                }

                entries.Add(key, new Entry(AcquisitionState.InProgress));
            }

            bool succeeded = TryAcquireLocally(
                    document,
                    mappingSnapshot,
                    policy,
                    configuration,
                    out ValidatedExternalSourceMaterial? acquired)
                || (client != null
                    && sourceUri != null
                    && TryDownload(client, sourceUri, out ImmutableArray<byte> image)
                    && TryValidateCandidate(
                        document,
                        image,
                        ExternalSourceMaterialOrigin.SourceLink,
                        filePath: null,
                        sourceUri.AbsoluteUri,
                        policy,
                        configuration,
                        out acquired));

            lock (gate)
            {
                Entry entry = entries[key];
                entry.State = succeeded
                    ? AcquisitionState.Succeeded
                    : AcquisitionState.Failed;
                entry.Material = acquired;
            }

            material = acquired!;
            return succeeded;
        }

        /// <summary>
        /// Tests deterministic longest-prefix local projections through P5H.
        /// </summary>
        /// <param name="document">The expected Portable PDB document.</param>
        /// <param name="mappingSnapshot">The immutable mapping snapshot.</param>
        /// <param name="policy">The active reconstruction policy.</param>
        /// <param name="configuration">Optional P5G encoding provenance.</param>
        /// <param name="material">The first checksum-valid material.</param>
        /// <returns><see langword="true"/> when one local candidate validates.</returns>
        private bool TryAcquireLocally(
            ExternalSourceDocumentDescriptor document,
            ImmutableArray<NormalizedMapping> mappingSnapshot,
            ExternalSourceReconstructionPolicy policy,
            ExternalCSharpCompilationConfiguration? configuration,
            out ValidatedExternalSourceMaterial? material)
        {
            int selectedPrefixLength = -1;
            HashSet<string> candidates = new(PathComparer);

            foreach (NormalizedMapping mapping in mappingSnapshot)
            {
                if (!document.Name.StartsWith(
                        mapping.DocumentPrefix,
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (selectedPrefixLength < 0)
                {
                    selectedPrefixLength = mapping.DocumentPrefix.Length;
                }
                else if (mapping.DocumentPrefix.Length < selectedPrefixLength)
                {
                    break;
                }

                if (TryProjectCandidate(document.Name, mapping, out string candidatePath))
                {
                    candidates.Add(candidatePath);
                }
            }

            foreach (string candidatePath in candidates.OrderBy(
                         static path => path,
                         StringComparer.Ordinal))
            {
                if (!IsReparsePointFree(candidatePath, mappingSnapshot)
                    || !TryReadBoundedFile(candidatePath, out ImmutableArray<byte> image)
                    || !TryValidateCandidate(
                        document,
                        image,
                        ExternalSourceMaterialOrigin.LocalMapping,
                        candidatePath,
                        candidatePath,
                        policy,
                        configuration,
                        out ValidatedExternalSourceMaterial validated))
                {
                    continue;
                }

                material = validated;
                return true;
            }

            material = null;
            return false;
        }

        /// <summary>
        /// Downloads one original Source Link candidate and records only
        /// successful response bytes for bounded evaluation statistics.
        /// </summary>
        /// <param name="client">The configured bounded HTTPS client.</param>
        /// <param name="sourceUri">The safe resolved Source Link URI.</param>
        /// <param name="image">The downloaded original bytes.</param>
        /// <returns><see langword="true"/> when a bounded download succeeds.</returns>
        private bool TryDownload(
            ExternalSourceLinkClient client,
            Uri sourceUri,
            out ImmutableArray<byte> image)
        {
            lock (gate)
            {
                sourceLinkRequests++;
            }

            if (!client.TryDownload(sourceUri, out image))
            {
                return false;
            }

            lock (gate)
            {
                downloadedSourceBytes += image.Length;
            }

            return true;
        }

        /// <summary>
        /// Prefers the original bytes, then optionally produces deterministic
        /// line-ending candidates and sends each through unchanged P5H.
        /// </summary>
        /// <param name="document">The expected PDB document.</param>
        /// <param name="image">The original acquired bytes.</param>
        /// <param name="origin">The original acquisition origin.</param>
        /// <param name="filePath">Optional mapped local path.</param>
        /// <param name="sourceIdentity">The original path or safe URI.</param>
        /// <param name="policy">The context-local reconstruction policy.</param>
        /// <param name="configuration">Optional P5G encoding provenance.</param>
        /// <param name="material">The P5H-valid material.</param>
        /// <returns><see langword="true"/> only when P5H accepts exact bytes.</returns>
        private bool TryValidateCandidate(
            ExternalSourceDocumentDescriptor document,
            ImmutableArray<byte> image,
            ExternalSourceMaterialOrigin origin,
            string? filePath,
            string sourceIdentity,
            ExternalSourceReconstructionPolicy policy,
            ExternalCSharpCompilationConfiguration? configuration,
            out ValidatedExternalSourceMaterial material)
        {
            lock (gate)
            {
                p5HValidationAttempts++;
            }

            if (ValidatedExternalSourceMaterialFactory.TryCreateFromAcquiredImage(
                    document,
                    image,
                    origin,
                    filePath,
                    sourceIdentity,
                    ExternalSourceMaterialExactness.DirectExact,
                    ExternalSourceLineEndingTransformation.None,
                    out material))
            {
                lock (gate)
                {
                    directExactSourceCount++;
                }

                return true;
            }

            if (policy != ExternalSourceReconstructionPolicy.VerifiedLineEndings)
            {
                return false;
            }

            bool reconstructed =
                ExternalSourceLineEndingReconstructor.TryCreateValidatedMaterial(
                    document,
                    image,
                    origin,
                    filePath,
                    sourceIdentity,
                    configuration,
                    out material,
                    out ExternalSourceReconstructionResult result);

            lock (gate)
            {
                reconstructionAttemptCount++;
                reconstructionBytesProduced += result.BytesProduced;
                reconstructionDurationTicks += result.DurationTicks;
                p5HValidationAttempts += result.CandidateCount;

                if (reconstructed)
                {
                    reconstructionSuccessCount++;

                    if (result.SuccessfulTransformation
                        == ExternalSourceLineEndingTransformation.LfToCrlf)
                    {
                        lfToCrlfSuccessCount++;
                    }
                    else if (result.SuccessfulTransformation
                        == ExternalSourceLineEndingTransformation.CrlfToLf)
                    {
                        crlfToLfSuccessCount++;
                    }
                }
                else
                {
                    reconstructionFailureCount++;
                }
            }

            return reconstructed;
        }

        /// <summary>
        /// Projects a matched document suffix while preventing rooted paths
        /// and parent traversal from leaving the configured root.
        /// </summary>
        /// <param name="documentName">The untrusted PDB document name.</param>
        /// <param name="mapping">The normalized matched mapping.</param>
        /// <param name="candidatePath">The contained absolute path.</param>
        /// <returns><see langword="true"/> for a safe nonempty projection.</returns>
        private static bool TryProjectCandidate(
            string documentName,
            NormalizedMapping mapping,
            out string candidatePath)
        {
            string suffix = documentName[mapping.DocumentPrefix.Length..];
            string[] segments = suffix.Split(
                ['/', '\\'],
                StringSplitOptions.RemoveEmptyEntries);

            if (segments.Length == 0
                || segments.Any(segment => segment is "." or ".." || segment.Contains(':')))
            {
                candidatePath = null!;
                return false;
            }

            try
            {
                string relative = Path.Combine(segments);

                if (Path.IsPathRooted(relative))
                {
                    candidatePath = null!;
                    return false;
                }

                candidatePath = Path.GetFullPath(Path.Combine(mapping.LocalRoot, relative));
                string rootPrefix = mapping.LocalRoot + Path.DirectorySeparatorChar;
                return PathStartsWith(candidatePath, rootPrefix);
            }
            catch (ArgumentException)
            {
                candidatePath = null!;
                return false;
            }
            catch (NotSupportedException)
            {
                candidatePath = null!;
                return false;
            }
        }

        /// <summary>
        /// Rejects mapped candidates whose selected root or descendant path
        /// traverses a filesystem reparse point.
        /// </summary>
        /// <param name="candidatePath">The contained projected path.</param>
        /// <param name="mappingSnapshot">The mappings used for projection.</param>
        /// <returns><see langword="true"/> only when the existing path is direct.</returns>
        private static bool IsReparsePointFree(
            string candidatePath,
            ImmutableArray<NormalizedMapping> mappingSnapshot)
        {
            NormalizedMapping? owner = null;

            foreach (NormalizedMapping mapping in mappingSnapshot)
            {
                if (PathStartsWith(
                        candidatePath,
                        mapping.LocalRoot + Path.DirectorySeparatorChar)
                    && (!owner.HasValue
                        || mapping.LocalRoot.Length > owner.Value.LocalRoot.Length))
                {
                    owner = mapping;
                }
            }

            if (!owner.HasValue)
            {
                return false;
            }

            string relative = Path.GetRelativePath(owner.Value.LocalRoot, candidatePath);
            string current = owner.Value.LocalRoot;

            try
            {
                if ((File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
                {
                    return false;
                }

                foreach (string segment in relative.Split(Path.DirectorySeparatorChar))
                {
                    current = Path.Combine(current, segment);

                    if ((File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
                    {
                        return false;
                    }
                }

                return true;
            }
            catch (FileNotFoundException)
            {
                return true;
            }
            catch (DirectoryNotFoundException)
            {
                return true;
            }
            catch (IOException)
            {
                return false;
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }
        }

        /// <summary>
        /// Reads one local candidate under the same bound used for downloads.
        /// </summary>
        /// <param name="candidatePath">The contained candidate path.</param>
        /// <param name="image">The exact bounded bytes.</param>
        /// <returns><see langword="true"/> when the complete file fits the limit.</returns>
        private static bool TryReadBoundedFile(
            string candidatePath,
            out ImmutableArray<byte> image)
        {
            try
            {
                using FileStream stream = new(
                    candidatePath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read);

                if (stream.Length > ExternalSourceLinkClient.DefaultMaximumResponseBytes)
                {
                    image = default;
                    return false;
                }

                using MemoryStream snapshot = new();
                byte[] buffer = new byte[81920];

                while (true)
                {
                    int count = stream.Read(buffer, 0, buffer.Length);

                    if (count == 0)
                    {
                        image = ImmutableCollectionsMarshal.AsImmutableArray(
                            snapshot.ToArray());
                        return true;
                    }

                    if (snapshot.Length + count
                        > ExternalSourceLinkClient.DefaultMaximumResponseBytes)
                    {
                        image = default;
                        return false;
                    }

                    snapshot.Write(buffer, 0, count);
                }
            }
            catch (IOException)
            {
                image = default;
                return false;
            }
            catch (UnauthorizedAccessException)
            {
                image = default;
                return false;
            }
            catch (ArgumentException)
            {
                image = default;
                return false;
            }
            catch (NotSupportedException)
            {
                image = default;
                return false;
            }
        }

        /// <summary>
        /// Resolves only credential-free absolute locator syntax for cache identity.
        /// </summary>
        /// <param name="client">The enabled client, when any.</param>
        /// <param name="sourceLink">The P4B mapping provenance.</param>
        /// <param name="documentName">The exact PDB document name.</param>
        /// <returns>The safe parsed locator, or <see langword="null"/>.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown transitively when <paramref name="documentName"/> is
        /// <see langword="null"/>.
        /// </exception>
        private static Uri? TryResolveSafeSourceUri(
            ExternalSourceLinkClient? client,
            ExternalSourceLinkDescriptor? sourceLink,
            string documentName)
        {
            if (client == null
                || sourceLink == null
                || !sourceLink.TryResolveDocument(documentName, out string locator)
                || !Uri.TryCreate(locator, UriKind.Absolute, out Uri? uri)
                || uri.UserInfo.Length != 0
                || uri.Fragment.Length != 0)
            {
                return null;
            }

            return uri;
        }

        /// <summary>
        /// Normalizes and deterministically orders caller mappings.
        /// </summary>
        /// <param name="sourceMappings">The complete caller mapping set.</param>
        /// <param name="normalized">The immutable normalized snapshot.</param>
        /// <returns><see langword="true"/> when every mapping is valid.</returns>
        private static bool TryNormalizeMappings(
            IEnumerable<ExternalSourcePathMapping> sourceMappings,
            out ImmutableArray<NormalizedMapping> normalized)
        {
            try
            {
                List<NormalizedMapping> mappings = new();

                foreach (ExternalSourcePathMapping? mapping in sourceMappings)
                {
                    if (mapping == null
                        || mapping.DocumentPrefix.Length == 0
                        || !Path.IsPathFullyQualified(mapping.LocalRoot))
                    {
                        normalized = default;
                        return false;
                    }

                    NormalizedMapping candidate = new(
                        mapping.DocumentPrefix,
                        Path.TrimEndingDirectorySeparator(
                            Path.GetFullPath(mapping.LocalRoot)));

                    if (!mappings.Any(existing => MappingEquals(existing, candidate)))
                    {
                        mappings.Add(candidate);
                    }
                }

                normalized = mappings
                    .OrderByDescending(static mapping => mapping.DocumentPrefix.Length)
                    .ThenBy(static mapping => mapping.DocumentPrefix, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(static mapping => mapping.LocalRoot, StringComparer.Ordinal)
                    .ToImmutableArray();
                return true;
            }
            catch (ArgumentException)
            {
                normalized = default;
                return false;
            }
            catch (NotSupportedException)
            {
                normalized = default;
                return false;
            }
        }

        /// <summary>
        /// Compares two complete normalized mapping snapshots.
        /// </summary>
        /// <param name="left">The first snapshot.</param>
        /// <param name="right">The second snapshot.</param>
        /// <returns><see langword="true"/> when the mappings are equivalent.</returns>
        private static bool MappingsEqual(
            ImmutableArray<NormalizedMapping> left,
            ImmutableArray<NormalizedMapping> right)
        {
            return left.Length == right.Length
                && left.Zip(right).All(pair => MappingEquals(pair.First, pair.Second));
        }

        /// <summary>
        /// Compares one normalized mapping pair.
        /// </summary>
        /// <param name="left">The first mapping.</param>
        /// <param name="right">The second mapping.</param>
        /// <returns><see langword="true"/> when prefix and root are equivalent.</returns>
        private static bool MappingEquals(NormalizedMapping left, NormalizedMapping right)
        {
            return string.Equals(
                    left.DocumentPrefix,
                    right.DocumentPrefix,
                    StringComparison.OrdinalIgnoreCase)
                && PathComparer.Equals(left.LocalRoot, right.LocalRoot);
        }

        /// <summary>
        /// Applies the platform path comparison to a containment prefix.
        /// </summary>
        /// <param name="path">The normalized candidate path.</param>
        /// <param name="prefix">The normalized root prefix.</param>
        /// <returns><see langword="true"/> when the path begins inside the root.</returns>
        private static bool PathStartsWith(string path, string prefix)
        {
            return path.StartsWith(
                prefix,
                OperatingSystem.IsWindows()
                    ? StringComparison.OrdinalIgnoreCase
                    : StringComparison.Ordinal);
        }

        /// <summary>
        /// Represents one normalized local projection.
        /// </summary>
        /// <param name="DocumentPrefix">The exact document-name prefix.</param>
        /// <param name="LocalRoot">The normalized fully qualified root.</param>
        private readonly record struct NormalizedMapping(
            string DocumentPrefix,
            string LocalRoot);

        /// <summary>
        /// Represents source identity plus a credential-free Source Link hint.
        /// </summary>
        /// <param name="Name">The exact PDB document name.</param>
        /// <param name="HashAlgorithm">The PDB hash algorithm.</param>
        /// <param name="Hash">The expected hash bytes as hexadecimal text.</param>
        /// <param name="Language">The PDB source language.</param>
        /// <param name="SourceUri">The safe resolved URI, when any.</param>
        /// <param name="ReconstructionPolicy">The context-local reconstruction policy.</param>
        /// <param name="DefaultEncodingWebName">The P5G default encoding identity.</param>
        /// <param name="FallbackEncodingWebName">The P5G fallback encoding identity.</param>
        private readonly record struct AcquisitionKey(
            string Name,
            Guid HashAlgorithm,
            string Hash,
            Guid Language,
            string? SourceUri,
            ExternalSourceReconstructionPolicy ReconstructionPolicy,
            string? DefaultEncodingWebName,
            string? FallbackEncodingWebName)
        {
            /// <summary>
            /// Creates a stable key from complete source identity provenance.
            /// </summary>
            /// <param name="document">The expected Portable PDB document.</param>
            /// <param name="sourceUri">The optional safe retrieval hint.</param>
            /// <param name="reconstructionPolicy">The active reconstruction policy.</param>
            /// <param name="configuration">The optional P5G encoding provenance.</param>
            /// <returns>The immutable acquisition key.</returns>
            public static AcquisitionKey Create(
                ExternalSourceDocumentDescriptor document,
                Uri? sourceUri,
                ExternalSourceReconstructionPolicy reconstructionPolicy,
                ExternalCSharpCompilationConfiguration? configuration)
            {
                return new AcquisitionKey(
                    document.Name,
                    document.HashAlgorithm,
                    Convert.ToHexString(document.Hash.AsSpan()),
                    document.Language,
                    sourceUri?.AbsoluteUri,
                    reconstructionPolicy,
                    configuration?.DefaultEncodingWebName,
                    configuration?.FallbackEncodingWebName);
            }
        }

        /// <summary>
        /// Stores one cached acquisition state and optional P5H material.
        /// </summary>
        private sealed class Entry
        {
            /// <summary>
            /// Initializes one cache entry.
            /// </summary>
            /// <param name="state">The initial acquisition state.</param>
            public Entry(AcquisitionState state)
            {
                State = state;
            }

            /// <summary>
            /// Gets or sets the cached state.
            /// </summary>
            /// <value>The current acquisition state.</value>
            public AcquisitionState State { get; set; }

            /// <summary>
            /// Gets or sets the successful P5H material.
            /// </summary>
            /// <value>The material, or <see langword="null"/>.</value>
            public ValidatedExternalSourceMaterial? Material { get; set; }
        }

        /// <summary>
        /// Represents cached acquisition transitions.
        /// </summary>
        private enum AcquisitionState
        {
            /// <summary>
            /// The sole acquisition attempt is currently running.
            /// </summary>
            InProgress,

            /// <summary>
            /// Controlled acquisition failed.
            /// </summary>
            Failed,

            /// <summary>
            /// Acquisition produced checksum-validated P5H material.
            /// </summary>
            Succeeded
        }
    }
}
