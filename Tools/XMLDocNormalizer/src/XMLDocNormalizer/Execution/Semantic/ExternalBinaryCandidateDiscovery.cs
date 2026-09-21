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
        /// Records whether a root set has been configured.
        /// </summary>
        private bool isConfigured;

        /// <summary>
        /// Records whether any discovery lookup has begun.
        /// </summary>
        private bool discoveryStarted;

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
            if (expectedReference == null)
            {
                candidatePath = null!;
                return false;
            }

            ImmutableArray<string> roots;

            lock (gate)
            {
                discoveryStarted = true;

                if (entries.TryGetValue(expectedReference, out Entry? cached))
                {
                    candidatePath = cached.CandidatePath!;
                    return cached.State == DiscoveryState.Found;
                }

                entries.Add(expectedReference, new Entry(DiscoveryState.InProgress));
                roots = searchRoots;
            }

            DiscoveryState state = TryDiscover(
                expectedReference,
                roots,
                out string? discoveredPath);

            lock (gate)
            {
                Entry entry = entries[expectedReference];
                entry.State = state;
                entry.CandidatePath = discoveredPath;
            }

            candidatePath = discoveredPath!;
            return state == DiscoveryState.Found;
        }

        /// <summary>
        /// Executes the expected-name fast path and then the non-recursive fallback.
        /// </summary>
        /// <param name="expectedReference">The authoritative P5A reference provenance.</param>
        /// <param name="roots">The immutable normalized search-root snapshot.</param>
        /// <param name="candidatePath">The selected deterministic path when found.</param>
        /// <returns>The positive, negative, or ambiguous discovery state.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown transitively when an invalid expected reference reaches the
        /// existing validation boundary.
        /// </exception>
        private static DiscoveryState TryDiscover(
            ExternalCompilationMetadataReferenceDescriptor expectedReference,
            ImmutableArray<string> roots,
            out string? candidatePath)
        {
            string? fileName = TryGetPlausibleFileName(expectedReference.Name);

            if (fileName != null)
            {
                string[] fastCandidates = roots
                    .Select(root => Path.Combine(root, fileName))
                    .Distinct(PathComparer)
                    .OrderBy(static path => path, StringComparer.Ordinal)
                    .ToArray();
                DiscoveryState fastState = ValidateCandidates(
                    expectedReference,
                    fastCandidates,
                    out candidatePath);

                if (fastState != DiscoveryState.NotFound)
                {
                    return fastState;
                }
            }

            if (!TryEnumerateCandidates(roots, out string[] fallbackCandidates))
            {
                candidatePath = null;
                return DiscoveryState.NotFound;
            }

            return ValidateCandidates(
                expectedReference,
                fallbackCandidates,
                out candidatePath);
        }

        /// <summary>
        /// Validates candidates with P5C and rejects differing validated snapshots.
        /// </summary>
        /// <param name="expectedReference">The authoritative P5A reference provenance.</param>
        /// <param name="candidatePaths">The deterministic candidate path sequence.</param>
        /// <param name="candidatePath">The selected deterministic path when found.</param>
        /// <returns>The positive, negative, or ambiguous validation state.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown transitively when an invalid expected reference reaches the
        /// existing validation boundary.
        /// </exception>
        private static DiscoveryState ValidateCandidates(
            ExternalCompilationMetadataReferenceDescriptor expectedReference,
            IReadOnlyList<string> candidatePaths,
            out string? candidatePath)
        {
            string? selectedPath = null;
            ImmutableArray<byte> selectedImage = default;

            foreach (string path in candidatePaths)
            {
                if (!ValidatedExternalMetadataReferenceMaterialFactory.TryCreateFromFile(
                        expectedReference,
                        path,
                        out ValidatedExternalMetadataReferenceMaterial material))
                {
                    continue;
                }

                if (selectedPath == null)
                {
                    selectedPath = path;
                    selectedImage = material.Image;
                    continue;
                }

                if (!selectedImage.AsSpan().SequenceEqual(material.Image.AsSpan()))
                {
                    candidatePath = null;
                    return DiscoveryState.Ambiguous;
                }

                if (StringComparer.Ordinal.Compare(path, selectedPath) < 0)
                {
                    selectedPath = path;
                }
            }

            candidatePath = selectedPath;
            return selectedPath == null
                ? DiscoveryState.NotFound
                : DiscoveryState.Found;
        }

        /// <summary>
        /// Enumerates plausible PE extensions directly inside each allowed root.
        /// </summary>
        /// <param name="roots">The immutable normalized search-root snapshot.</param>
        /// <param name="candidates">The deterministic deduplicated candidate paths.</param>
        /// <returns><see langword="true"/> when at least one path is found.</returns>
        private static bool TryEnumerateCandidates(
            ImmutableArray<string> roots,
            out string[] candidates)
        {
            HashSet<string> paths = new(PathComparer);

            foreach (string root in roots)
            {
                try
                {
                    foreach (string path in Directory.EnumerateFiles(
                                 root,
                                 "*",
                                 SearchOption.TopDirectoryOnly))
                    {
                        if (HasPlausibleExtension(path))
                        {
                            paths.Add(path);
                        }
                    }
                }
                catch (IOException)
                {
                }
                catch (UnauthorizedAccessException)
                {
                }
                catch (ArgumentException)
                {
                }
            }

            candidates = paths.OrderBy(static path => path, StringComparer.Ordinal).ToArray();
            return candidates.Length != 0;
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
        }

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
