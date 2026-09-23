using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace XMLDocNormalizer.Execution.Semantic
{
    /// <summary>
    /// Finds local PE candidate files within explicit context-owned search
    /// roots and accepts them only through existing P5B/P5C validation.
    /// </summary>
    /// <remarks>
    /// Discovery does not establish binary identity. File names, extensions,
    /// and paths are hints only. A path becomes usable only when
    /// <see cref="ValidatedExternalMetadataReferenceMaterialFactory"/>
    /// validates its exact immutable snapshot against P5A provenance.
    /// Search is non-recursive and never leaves the configured roots.
    /// </remarks>
    internal sealed class ExternalBinaryCandidateDiscovery
    {
        /// <summary>
        /// Compares paths according to the current platform's file-name case behavior.
        /// </summary>
        private static readonly StringComparer PathComparer = OperatingSystem.IsWindows()
            ? StringComparer.OrdinalIgnoreCase
            : StringComparer.Ordinal;

        /// <summary>
        /// Serializes configuration and per-binary discovery transitions.
        /// </summary>
        private readonly object gate = new();

        /// <summary>
        /// Stores cached positive, negative, ambiguous, and in-progress results.
        /// </summary>
        private readonly Dictionary<ExternalCompilationMetadataReferenceDescriptor, Entry> entries =
            new(BinaryProvenanceComparer.Instance);

        /// <summary>
        /// Stores normalized, deduplicated roots in deterministic order.
        /// </summary>
        private ImmutableArray<string> searchRoots = ImmutableArray<string>.Empty;

        /// <summary>
        /// Stores optional standard local artifact sources.
        /// </summary>
        private ExternalReferenceArtifactSourceConfiguration? artifactSources;

        /// <summary>
        /// Lazily indexes bounded NuGet asset directories on first broad lookup.
        /// </summary>
        private Lazy<ImmutableArray<string>>? nuGetAssetDirectories;

        /// <summary>
        /// Records whether a root set has been configured.
        /// </summary>
        private bool isConfigured;

        /// <summary>
        /// Records whether standard artifact sources have been configured.
        /// </summary>
        private bool artifactSourcesConfigured;

        /// <summary>
        /// Records whether any discovery lookup has begun.
        /// </summary>
        private bool discoveryStarted;

        /// <summary>Counts examined artifact roots.</summary>
        private long artifactRootsExamined;

        /// <summary>Counts bounded directory enumerations.</summary>
        private long directoriesEnumerated;

        /// <summary>Counts distinct candidate files considered.</summary>
        private long candidateFilesConsidered;

        /// <summary>Counts candidate-open attempts.</summary>
        private long candidateFilesOpened;

        /// <summary>Counts calls to the existing P5 validation boundary.</summary>
        private long validationAttempts;

        /// <summary>
        /// Configures the complete context-local non-recursive search-root set
        /// without touching the filesystem.
        /// </summary>
        /// <param name="roots">
        /// Fully qualified local directory paths. Relative paths are rejected
        /// so later discovery never depends on the working directory.
        /// </param>
        /// <returns>
        /// <see langword="true"/> for a new or idempotent normalized root set;
        /// otherwise <see langword="false"/>. Configuration cannot change
        /// after discovery begins.
        /// </returns>
        public bool TryConfigure(IEnumerable<string> roots)
        {
            if (roots == null || !TryNormalizeRoots(roots, out ImmutableArray<string> normalized))
            {
                return false;
            }

            lock (gate)
            {
                if (isConfigured)
                {
                    return RootsEqual(searchRoots, normalized);
                }

                if (discoveryStarted)
                {
                    return false;
                }

                searchRoots = normalized;
                isConfigured = true;
                return true;
            }
        }

        /// <summary>
        /// Configures optional standard local artifact sources without touching them.
        /// </summary>
        /// <param name="configuration">The immutable context-local source snapshot.</param>
        /// <returns>
        /// <see langword="true"/> for a new or idempotent configuration; otherwise
        /// <see langword="false"/> after discovery starts or for a different snapshot.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="configuration"/> is <see langword="null"/>.
        /// </exception>
        public bool TryConfigureStandardArtifactSources(
            ExternalReferenceArtifactSourceConfiguration configuration)
        {
            ArgumentNullException.ThrowIfNull(configuration);

            lock (gate)
            {
                if (artifactSourcesConfigured)
                {
                    return artifactSources!.IsEquivalentTo(configuration);
                }

                if (discoveryStarted)
                {
                    return false;
                }

                artifactSources = configuration;
                artifactSourcesConfigured = true;
                if (configuration.NuGetGlobalPackagesFolder != null)
                {
                    string root = configuration.NuGetGlobalPackagesFolder;
                    nuGetAssetDirectories = new Lazy<ImmutableArray<string>>(
                        () => EnumerateNuGetAssetDirectories(root),
                        LazyThreadSafetyMode.ExecutionAndPublication);
                }

                return true;
            }
        }

        /// <summary>
        /// Returns an atomic snapshot of bounded discovery work performed so far.
        /// </summary>
        /// <returns>The current context-local counters.</returns>
        public ExternalBinaryCandidateDiscoveryStatistics GetStatistics()
        {
            return new ExternalBinaryCandidateDiscoveryStatistics(
                Interlocked.Read(ref artifactRootsExamined),
                Interlocked.Read(ref directoriesEnumerated),
                Interlocked.Read(ref candidateFilesConsidered),
                Interlocked.Read(ref candidateFilesOpened),
                Interlocked.Read(ref validationAttempts));
        }

        /// <summary>
        /// Tries to find one exact P5-validated local candidate for expected
        /// reference provenance.
        /// </summary>
        /// <param name="expectedReference">The P5A reference provenance.</param>
        /// <param name="candidatePath">The deterministic exact candidate path.</param>
        /// <returns>
        /// <see langword="true"/> only when at least one candidate validates
        /// and every validating path contains identical bytes; otherwise
        /// <see langword="false"/> for no match, ambiguity, or reentrancy.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown transitively when an invalid expected reference reaches the
        /// existing validation boundary.
        /// </exception>
        public bool TryFindReferenceCandidate(
            ExternalCompilationMetadataReferenceDescriptor expectedReference,
            out string candidatePath)
        {
            return TryFindReferenceCandidate(
                expectedReference,
                out candidatePath,
                out _);
        }

        /// <summary>
        /// Tries to find one exact candidate and reports its local artifact source.
        /// </summary>
        /// <param name="expectedReference">The authoritative P5A provenance.</param>
        /// <param name="candidatePath">The deterministic exact candidate path.</param>
        /// <param name="sourceKind">The source of the selected candidate.</param>
        /// <returns><see langword="true"/> only for an exact unambiguous match.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown transitively when an invalid expected reference reaches the
        /// existing validation boundary.
        /// </exception>
        public bool TryFindReferenceCandidate(
            ExternalCompilationMetadataReferenceDescriptor expectedReference,
            out string candidatePath,
            out ExternalReferenceArtifactSourceKind sourceKind)
        {
            if (expectedReference == null)
            {
                candidatePath = null!;
                sourceKind = default;
                return false;
            }

            ImmutableArray<string> roots;
            ExternalReferenceArtifactSourceConfiguration? sources;

            lock (gate)
            {
                discoveryStarted = true;

                if (entries.TryGetValue(expectedReference, out Entry? cached))
                {
                    candidatePath = cached.CandidatePath!;
                    sourceKind = cached.SourceKind.GetValueOrDefault();
                    return cached.State == DiscoveryState.Found;
                }

                entries.Add(expectedReference, new Entry(DiscoveryState.InProgress));
                roots = searchRoots;
                sources = artifactSources;
            }

            DiscoveryState state = TryDiscover(
                expectedReference,
                roots,
                sources,
                out string? discoveredPath,
                out ExternalReferenceArtifactSourceKind? discoveredSource);

            lock (gate)
            {
                Entry entry = entries[expectedReference];
                entry.State = state;
                entry.CandidatePath = discoveredPath;
                entry.SourceKind = discoveredSource;
            }

            candidatePath = discoveredPath!;
            sourceKind = discoveredSource.GetValueOrDefault();
            return state == DiscoveryState.Found;
        }

        /// <summary>
        /// Materializes a local exact candidate first and only then consults
        /// the explicitly configured bounded remote acquisition context.
        /// </summary>
        /// <param name="expectedReference">The authoritative expected P5 reference.</param>
        /// <param name="expectedReferenceOrdinal">The expected reference ordinal.</param>
        /// <param name="remoteAcquisition">The context-local opt-in remote acquisition.</param>
        /// <param name="material">The exact P5-validated material when successful.</param>
        /// <param name="sourceKind">The local or remote artifact source classification.</param>
        /// <param name="remoteProvenance">The remote provenance, or <see langword="null"/> for local material.</param>
        /// <returns><see langword="true"/> only when local or remote P5 validation succeeds.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="expectedReference"/> or
        /// <paramref name="remoteAcquisition"/> is <see langword="null"/>.
        /// </exception>
        public bool TryAcquireReferenceMaterial(
            ExternalCompilationMetadataReferenceDescriptor expectedReference,
            int expectedReferenceOrdinal,
            ExternalRemoteReferenceAcquisition remoteAcquisition,
            out ValidatedExternalMetadataReferenceMaterial material,
            out ExternalReferenceArtifactSourceKind sourceKind,
            out ExternalRemoteReferenceProvenance? remoteProvenance)
        {
            ArgumentNullException.ThrowIfNull(expectedReference);
            ArgumentNullException.ThrowIfNull(remoteAcquisition);

            if (TryFindReferenceCandidate(
                    expectedReference,
                    out string candidatePath,
                    out sourceKind)
                && ValidatedExternalMetadataReferenceMaterialFactory.TryCreateFromFile(
                    expectedReference,
                    candidatePath,
                    out material))
            {
                remoteProvenance = null;
                return true;
            }

            if (remoteAcquisition.TryAcquire(
                    expectedReference,
                    expectedReferenceOrdinal,
                    out material,
                    out ExternalRemoteReferenceProvenance provenance))
            {
                sourceKind = provenance.ProviderKind == ExternalRemoteArtifactProviderKind.DotNetReferencePack
                    ? ExternalReferenceArtifactSourceKind.RemoteDotNetReferencePack
                    : ExternalReferenceArtifactSourceKind.RemoteNuGetPackage;
                remoteProvenance = provenance;
                return true;
            }

            sourceKind = default;
            remoteProvenance = null;
            return false;
        }

        /// <summary>
        /// Executes the expected-name fast path and then the non-recursive fallback.
        /// </summary>
        /// <param name="expectedReference">The authoritative P5A reference provenance.</param>
        /// <param name="roots">The immutable normalized search-root snapshot.</param>
        /// <param name="sources">The optional immutable standard-source snapshot.</param>
        /// <param name="candidatePath">The selected deterministic path when found.</param>
        /// <param name="sourceKind">The selected source kind when found.</param>
        /// <returns>The positive, negative, or ambiguous discovery state.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown transitively when an invalid expected reference reaches the
        /// existing validation boundary.
        /// </exception>
        private DiscoveryState TryDiscover(
            ExternalCompilationMetadataReferenceDescriptor expectedReference,
            ImmutableArray<string> roots,
            ExternalReferenceArtifactSourceConfiguration? sources,
            out string? candidatePath,
            out ExternalReferenceArtifactSourceKind? sourceKind)
        {
            string? fileName = TryGetPlausibleFileName(expectedReference.Name);

            if (fileName != null)
            {
                Dictionary<string, CandidatePath> candidates = new(PathComparer);
                AddExplicitRootFastCandidates(roots, fileName, candidates);
                DiscoveryState state = ValidateCandidates(
                    expectedReference,
                    OrderCandidates(candidates),
                    out candidatePath,
                    out sourceKind);
                if (state != DiscoveryState.NotFound)
                {
                    return state;
                }

                candidates.Clear();
                AddExplicitRootFallbackCandidates(roots, candidates);
                state = ValidateCandidates(
                    expectedReference,
                    OrderCandidates(candidates),
                    out candidatePath,
                    out sourceKind);
                if (state != DiscoveryState.NotFound)
                {
                    return state;
                }

                if (sources != null)
                {
                    candidates.Clear();
                    AddLoadedReferenceCandidates(
                        sources.LoadedReferencePaths,
                        fileName,
                        onlyMatchingFileName: true,
                        candidates);
                    state = ValidateCandidates(
                        expectedReference,
                        OrderCandidates(candidates),
                        out candidatePath,
                        out sourceKind);
                    if (state != DiscoveryState.NotFound)
                    {
                        return state;
                    }

                    foreach (string dotNetRoot in sources.DotNetRoots)
                    {
                        candidates.Clear();
                        AddDotNetPackCandidates(dotNetRoot, fileName, candidates);
                        state = ValidateCandidates(
                            expectedReference,
                            OrderCandidates(candidates),
                            out candidatePath,
                            out sourceKind);
                        if (state != DiscoveryState.NotFound)
                        {
                            return state;
                        }

                        candidates.Clear();
                        AddDotNetSharedFrameworkCandidates(
                            dotNetRoot,
                            fileName,
                            candidates);
                        state = ValidateCandidates(
                            expectedReference,
                            OrderCandidates(candidates),
                            out candidatePath,
                            out sourceKind);
                        if (state != DiscoveryState.NotFound)
                        {
                            return state;
                        }
                    }

                    if (sources.NuGetGlobalPackagesFolder != null)
                    {
                        candidates.Clear();
                        AddNuGetPackageFastCandidates(
                            sources.NuGetGlobalPackagesFolder,
                            fileName,
                            candidates);
                        state = ValidateCandidates(
                            expectedReference,
                            OrderCandidates(candidates),
                            out candidatePath,
                            out sourceKind);
                        if (state != DiscoveryState.NotFound)
                        {
                            return state;
                        }

                        candidates.Clear();
                        AddNuGetFallbackCandidates(fileName, candidates);
                        state = ValidateCandidates(
                            expectedReference,
                            OrderCandidates(candidates),
                            out candidatePath,
                            out sourceKind);
                        if (state != DiscoveryState.NotFound)
                        {
                            return state;
                        }
                    }
                }
            }
            else
            {
                Dictionary<string, CandidatePath> fallbackCandidates = new(PathComparer);
                AddExplicitRootFallbackCandidates(roots, fallbackCandidates);
                if (sources != null)
                {
                    AddLoadedReferenceCandidates(
                        sources.LoadedReferencePaths,
                        fileName: null,
                        onlyMatchingFileName: false,
                        fallbackCandidates);
                }

                return ValidateCandidates(
                    expectedReference,
                    OrderCandidates(fallbackCandidates),
                    out candidatePath,
                    out sourceKind);
            }

            candidatePath = null;
            sourceKind = null;
            return DiscoveryState.NotFound;
        }

        /// <summary>
        /// Validates candidates with P5C and rejects differing validated snapshots.
        /// </summary>
        /// <param name="expectedReference">The authoritative P5A reference provenance.</param>
        /// <param name="candidates">The deterministic candidate path sequence.</param>
        /// <param name="candidatePath">The selected deterministic path when found.</param>
        /// <param name="sourceKind">The selected artifact source when found.</param>
        /// <returns>The positive, negative, or ambiguous validation state.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown transitively when an invalid expected reference reaches the
        /// existing validation boundary.
        /// </exception>
        private DiscoveryState ValidateCandidates(
            ExternalCompilationMetadataReferenceDescriptor expectedReference,
            IReadOnlyList<CandidatePath> candidates,
            out string? candidatePath,
            out ExternalReferenceArtifactSourceKind? sourceKind)
        {
            string? selectedPath = null;
            ExternalReferenceArtifactSourceKind? selectedSource = null;
            ImmutableArray<byte> selectedImage = default;

            foreach (CandidatePath candidate in candidates)
            {
                Interlocked.Increment(ref candidateFilesOpened);
                Interlocked.Increment(ref validationAttempts);
                if (!ValidatedExternalMetadataReferenceMaterialFactory.TryCreateFromFile(
                        expectedReference,
                        candidate.Path,
                        out ValidatedExternalMetadataReferenceMaterial material))
                {
                    continue;
                }

                if (selectedPath == null)
                {
                    selectedPath = candidate.Path;
                    selectedSource = candidate.SourceKind;
                    selectedImage = material.Image;
                    continue;
                }

                if (!selectedImage.AsSpan().SequenceEqual(material.Image.AsSpan()))
                {
                    candidatePath = null;
                    sourceKind = null;
                    return DiscoveryState.Ambiguous;
                }

                if (StringComparer.Ordinal.Compare(candidate.Path, selectedPath) < 0)
                {
                    selectedPath = candidate.Path;
                    selectedSource = candidate.SourceKind;
                }
            }

            candidatePath = selectedPath;
            sourceKind = selectedSource;
            return selectedPath == null
                ? DiscoveryState.NotFound
                : DiscoveryState.Found;
        }

        /// <summary>Adds existing expected-name candidates from explicit roots.</summary>
        /// <param name="roots">The explicit top-level roots.</param>
        /// <param name="fileName">The safe expected filename.</param>
        /// <param name="candidates">The deduplicated candidate destination.</param>
        private void AddExplicitRootFastCandidates(
            ImmutableArray<string> roots,
            string fileName,
            IDictionary<string, CandidatePath> candidates)
        {
            foreach (string root in roots)
            {
                Interlocked.Increment(ref artifactRootsExamined);
                AddCandidate(
                    Path.Combine(root, fileName),
                    ExternalReferenceArtifactSourceKind.ExplicitRoot,
                    candidates);
            }
        }

        /// <summary>Adds plausible top-level fallback files from explicit roots.</summary>
        /// <param name="roots">The explicit top-level roots.</param>
        /// <param name="candidates">The deduplicated candidate destination.</param>
        private void AddExplicitRootFallbackCandidates(
            ImmutableArray<string> roots,
            IDictionary<string, CandidatePath> candidates)
        {
            foreach (string root in roots)
            {
                Interlocked.Increment(ref artifactRootsExamined);
                foreach (string path in EnumerateFiles(root))
                {
                    if (HasPlausibleExtension(path))
                    {
                        AddCandidate(
                            path,
                            ExternalReferenceArtifactSourceKind.ExplicitRoot,
                            candidates);
                    }
                }
            }
        }

        /// <summary>Adds expected-name assets from the filename-derived package directory.</summary>
        /// <param name="nuGetRoot">The resolved global-packages root.</param>
        /// <param name="fileName">The safe expected filename.</param>
        /// <param name="candidates">The deduplicated candidate destination.</param>
        private void AddNuGetPackageFastCandidates(
            string nuGetRoot,
            string fileName,
            IDictionary<string, CandidatePath> candidates)
        {
            string packageId = Path.GetFileNameWithoutExtension(fileName)
                .ToLowerInvariant();
            string packageRoot = Path.Combine(nuGetRoot, packageId);
            Interlocked.Increment(ref artifactRootsExamined);
            foreach (string assetDirectory in EnumerateNuGetPackageAssetDirectories(
                         packageRoot))
            {
                AddCandidate(
                    Path.Combine(assetDirectory, fileName),
                    ExternalReferenceArtifactSourceKind.NuGetGlobalPackages,
                    candidates);
            }
        }

        /// <summary>Adds the expected filename from every bounded NuGet asset directory.</summary>
        /// <param name="fileName">The safe expected filename.</param>
        /// <param name="candidates">The deduplicated candidate destination.</param>
        private void AddNuGetFallbackCandidates(
            string fileName,
            IDictionary<string, CandidatePath> candidates)
        {
            if (nuGetAssetDirectories == null)
            {
                return;
            }

            foreach (string assetDirectory in nuGetAssetDirectories.Value)
            {
                AddCandidate(
                    Path.Combine(assetDirectory, fileName),
                    ExternalReferenceArtifactSourceKind.NuGetGlobalPackages,
                    candidates);
            }
        }

        /// <summary>Adds plausible file-backed Roslyn reference paths.</summary>
        /// <param name="paths">The immutable loaded-reference paths.</param>
        /// <param name="fileName">The optional expected filename.</param>
        /// <param name="onlyMatchingFileName">Whether to require a filename match.</param>
        /// <param name="candidates">The deduplicated candidate destination.</param>
        private void AddLoadedReferenceCandidates(
            ImmutableArray<string> paths,
            string? fileName,
            bool onlyMatchingFileName,
            IDictionary<string, CandidatePath> candidates)
        {
            if (paths.Length != 0)
            {
                Interlocked.Increment(ref artifactRootsExamined);
            }

            foreach (string path in paths)
            {
                if (HasPlausibleExtension(path)
                    && (!onlyMatchingFileName
                        || PathComparer.Equals(Path.GetFileName(path), fileName)))
                {
                    AddCandidate(
                        path,
                        ExternalReferenceArtifactSourceKind.LoadedReference,
                        candidates);
                }
            }
        }

        /// <summary>Adds expected-name candidates from fixed-depth .NET reference packs.</summary>
        /// <param name="dotNetRoot">The local dotnet installation root.</param>
        /// <param name="fileName">The safe expected filename.</param>
        /// <param name="candidates">The deduplicated candidate destination.</param>
        private void AddDotNetPackCandidates(
            string dotNetRoot,
            string fileName,
            IDictionary<string, CandidatePath> candidates)
        {
            string packsRoot = Path.Combine(dotNetRoot, "packs");
            Interlocked.Increment(ref artifactRootsExamined);
            foreach (string packDirectory in EnumerateDirectories(packsRoot))
            {
                foreach (string versionDirectory in EnumerateDirectories(packDirectory))
                {
                    string referenceRoot = Path.Combine(versionDirectory, "ref");
                    foreach (string frameworkDirectory in EnumerateDirectories(referenceRoot))
                    {
                        AddCandidate(
                            Path.Combine(frameworkDirectory, fileName),
                            ExternalReferenceArtifactSourceKind.DotNetReferencePack,
                            candidates);
                    }
                }
            }
        }

        /// <summary>Adds expected-name candidates from fixed-depth shared frameworks.</summary>
        /// <param name="dotNetRoot">The local dotnet installation root.</param>
        /// <param name="fileName">The safe expected filename.</param>
        /// <param name="candidates">The deduplicated candidate destination.</param>
        private void AddDotNetSharedFrameworkCandidates(
            string dotNetRoot,
            string fileName,
            IDictionary<string, CandidatePath> candidates)
        {
            string sharedRoot = Path.Combine(dotNetRoot, "shared");
            Interlocked.Increment(ref artifactRootsExamined);
            foreach (string frameworkDirectory in EnumerateDirectories(sharedRoot))
            {
                foreach (string versionDirectory in EnumerateDirectories(frameworkDirectory))
                {
                    AddCandidate(
                        Path.Combine(versionDirectory, fileName),
                        ExternalReferenceArtifactSourceKind.DotNetSharedFramework,
                        candidates);
                }
            }
        }

        /// <summary>Builds the lazy bounded NuGet asset-directory snapshot.</summary>
        /// <param name="root">The resolved global-packages root.</param>
        /// <returns>Deterministically ordered package asset directories.</returns>
        private ImmutableArray<string> EnumerateNuGetAssetDirectories(string root)
        {
            Interlocked.Increment(ref artifactRootsExamined);
            return EnumerateDirectories(root)
                .SelectMany(EnumerateNuGetPackageAssetDirectories)
                .Distinct(PathComparer)
                .OrderBy(static path => path, StringComparer.Ordinal)
                .ToImmutableArray();
        }

        /// <summary>Enumerates known fixed-depth managed asset directories for one package.</summary>
        /// <param name="packageRoot">The package-ID directory.</param>
        /// <returns>The package's lib, ref, and runtime-lib framework directories.</returns>
        private IEnumerable<string> EnumerateNuGetPackageAssetDirectories(
            string packageRoot)
        {
            List<string> directories = new();
            foreach (string versionDirectory in EnumerateDirectories(packageRoot))
            {
                AddFrameworkDirectories(
                    Path.Combine(versionDirectory, "lib"),
                    directories);
                AddFrameworkDirectories(
                    Path.Combine(versionDirectory, "ref"),
                    directories);

                string runtimesRoot = Path.Combine(versionDirectory, "runtimes");
                foreach (string runtimeDirectory in EnumerateDirectories(runtimesRoot))
                {
                    AddFrameworkDirectories(
                        Path.Combine(runtimeDirectory, "lib"),
                        directories);
                }
            }

            return directories;
        }

        /// <summary>Adds direct framework subdirectories below an asset root.</summary>
        /// <param name="root">The lib or ref asset root.</param>
        /// <param name="directories">The destination collection.</param>
        private void AddFrameworkDirectories(
            string root,
            ICollection<string> directories)
        {
            foreach (string frameworkDirectory in EnumerateDirectories(root))
            {
                directories.Add(frameworkDirectory);
            }
        }

        /// <summary>Safely enumerates direct child directories in stable order.</summary>
        /// <param name="root">The bounded directory to enumerate.</param>
        /// <returns>The direct children, or an empty sequence on a known I/O boundary.</returns>
        private string[] EnumerateDirectories(string root)
        {
            Interlocked.Increment(ref directoriesEnumerated);
            try
            {
                return Directory.EnumerateDirectories(
                        root,
                        "*",
                        SearchOption.TopDirectoryOnly)
                    .OrderBy(static path => path, StringComparer.Ordinal)
                    .ToArray();
            }
            catch (IOException)
            {
                return [];
            }
            catch (UnauthorizedAccessException)
            {
                return [];
            }
            catch (ArgumentException)
            {
                return [];
            }
        }

        /// <summary>Safely enumerates direct child files in stable order.</summary>
        /// <param name="root">The bounded directory to enumerate.</param>
        /// <returns>The direct files, or an empty sequence on a known I/O boundary.</returns>
        private string[] EnumerateFiles(string root)
        {
            Interlocked.Increment(ref directoriesEnumerated);
            try
            {
                return Directory.EnumerateFiles(
                        root,
                        "*",
                        SearchOption.TopDirectoryOnly)
                    .OrderBy(static path => path, StringComparer.Ordinal)
                    .ToArray();
            }
            catch (IOException)
            {
                return [];
            }
            catch (UnauthorizedAccessException)
            {
                return [];
            }
            catch (ArgumentException)
            {
                return [];
            }
        }

        /// <summary>Adds one existing path once without treating its location as identity.</summary>
        /// <param name="path">The possible local candidate path.</param>
        /// <param name="sourceKind">The source that supplied the path.</param>
        /// <param name="candidates">The deduplicated candidate destination.</param>
        private void AddCandidate(
            string path,
            ExternalReferenceArtifactSourceKind sourceKind,
            IDictionary<string, CandidatePath> candidates)
        {
            if (!candidates.ContainsKey(path) && File.Exists(path))
            {
                candidates.Add(path, new CandidatePath(path, sourceKind));
                Interlocked.Increment(ref candidateFilesConsidered);
            }
        }

        /// <summary>Orders deduplicated candidates independently of filesystem order.</summary>
        /// <param name="candidates">The candidates keyed by platform-equivalent path.</param>
        /// <returns>The deterministic candidate sequence.</returns>
        private static CandidatePath[] OrderCandidates(
            IReadOnlyDictionary<string, CandidatePath> candidates)
        {
            return candidates.Values
                .OrderBy(static candidate => candidate.Path, StringComparer.Ordinal)
                .ToArray();
        }

        /// <summary>
        /// Normalizes fully qualified roots and removes platform-equivalent duplicates.
        /// </summary>
        /// <param name="roots">The caller-provided complete search-root set.</param>
        /// <param name="normalized">The immutable deterministic root snapshot.</param>
        /// <returns>
        /// <see langword="true"/> when every root is fully qualified and can be
        /// normalized; otherwise <see langword="false"/>.
        /// </returns>
        private static bool TryNormalizeRoots(
            IEnumerable<string> roots,
            out ImmutableArray<string> normalized)
        {
            try
            {
                HashSet<string> unique = new(PathComparer);

                foreach (string? root in roots)
                {
                    if (root == null || !Path.IsPathFullyQualified(root))
                    {
                        normalized = default;
                        return false;
                    }

                    unique.Add(Path.TrimEndingDirectorySeparator(Path.GetFullPath(root)));
                }

                normalized = unique
                    .OrderBy(static path => path, StringComparer.Ordinal)
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
        /// Extracts a safe expected-name hint with a plausible PE extension.
        /// </summary>
        /// <param name="expectedName">The P5A reference name used only as a hint.</param>
        /// <returns>The plausible file name, or <see langword="null"/>.</returns>
        private static string? TryGetPlausibleFileName(string expectedName)
        {
            string fileName = Path.GetFileName(expectedName);
            return fileName.Length != 0 && HasPlausibleExtension(fileName)
                ? fileName
                : null;
        }

        /// <summary>
        /// Determines whether a path has a supported managed PE candidate extension.
        /// </summary>
        /// <param name="path">The path or file name to inspect.</param>
        /// <returns>
        /// <see langword="true"/> for a DLL, executable, or netmodule extension.
        /// </returns>
        private static bool HasPlausibleExtension(string path)
        {
            string extension = Path.GetExtension(path);
            return extension.Equals(".dll", StringComparison.OrdinalIgnoreCase)
                || extension.Equals(".exe", StringComparison.OrdinalIgnoreCase)
                || extension.Equals(".netmodule", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Compares deterministic normalized root snapshots.
        /// </summary>
        /// <param name="left">The first normalized root snapshot.</param>
        /// <param name="right">The second normalized root snapshot.</param>
        /// <returns>
        /// <see langword="true"/> when both snapshots contain the same roots.
        /// </returns>
        private static bool RootsEqual(
            ImmutableArray<string> left,
            ImmutableArray<string> right)
        {
            return left.Length == right.Length
                && left.Zip(right).All(pair => PathComparer.Equals(pair.First, pair.Second));
        }

        /// <summary>
        /// Stores one cached discovery state and optional path.
        /// </summary>
        private sealed class Entry
        {
            /// <summary>
            /// Initializes one cache entry.
            /// </summary>
            /// <param name="state">The initial discovery state.</param>
            public Entry(DiscoveryState state)
            {
                State = state;
            }

            /// <summary>
            /// Gets or sets the cached state.
            /// </summary>
            /// <value>The current cached discovery state.</value>
            public DiscoveryState State { get; set; }

            /// <summary>
            /// Gets or sets the selected path for a positive result.
            /// </summary>
            /// <value>The selected path, or <see langword="null"/>.</value>
            public string? CandidatePath { get; set; }

            /// <summary>
            /// Gets or sets the source of the selected exact candidate.
            /// </summary>
            /// <value>The selected source kind, or <see langword="null"/>.</value>
            public ExternalReferenceArtifactSourceKind? SourceKind { get; set; }
        }

        /// <summary>Couples one candidate path to its non-authoritative source.</summary>
        /// <param name="Path">The fully qualified local path.</param>
        /// <param name="SourceKind">The source that supplied the path.</param>
        private sealed record CandidatePath(
            string Path,
            ExternalReferenceArtifactSourceKind SourceKind);

        /// <summary>
        /// Represents cached discovery outcomes.
        /// </summary>
        private enum DiscoveryState
        {
            /// <summary>
            /// Indicates that the single discovery attempt is currently running.
            /// </summary>
            InProgress,

            /// <summary>
            /// Indicates that no exact candidate validated.
            /// </summary>
            NotFound,

            /// <summary>
            /// Indicates that different byte snapshots validated exactly.
            /// </summary>
            Ambiguous,

            /// <summary>
            /// Indicates that one binary snapshot validated exactly.
            /// </summary>
            Found,
        }

        /// <summary>
        /// Reuses one discovery result for repeated P5A entries describing the
        /// same binary build while ignoring reference-only alias properties.
        /// </summary>
        private sealed class BinaryProvenanceComparer :
            IEqualityComparer<ExternalCompilationMetadataReferenceDescriptor>
        {
            /// <summary>
            /// Gets the stateless comparer instance.
            /// </summary>
            /// <value>The shared binary-provenance comparer.</value>
            public static BinaryProvenanceComparer Instance { get; } = new();

            /// <inheritdoc/>
            public bool Equals(
                ExternalCompilationMetadataReferenceDescriptor? left,
                ExternalCompilationMetadataReferenceDescriptor? right)
            {
                return ReferenceEquals(left, right)
                    || (left != null
                        && right != null
                        && left.Kind == right.Kind
                        && left.Timestamp == right.Timestamp
                        && left.ImageSize == right.ImageSize
                        && left.ModuleVersionId == right.ModuleVersionId);
            }

            /// <inheritdoc/>
            public int GetHashCode(ExternalCompilationMetadataReferenceDescriptor descriptor)
            {
                return HashCode.Combine(
                    descriptor.Kind,
                    descriptor.Timestamp,
                    descriptor.ImageSize,
                    descriptor.ModuleVersionId);
            }
        }
    }
}
