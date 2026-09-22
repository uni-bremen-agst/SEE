using System.Buffers.Binary;
using System.Collections.Immutable;
using System.IO.Compression;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using System.Text;

namespace XMLDocNormalizer.Execution.Semantic
{
    /// <summary>
    /// Acquires bounded local Portable PDB candidates and delegates all
    /// identity decisions to the existing P4B validation boundary.
    /// </summary>
    internal sealed class ExternalPortablePdbAcquisition
    {
        /// <summary>The maximum accepted uncompressed Portable PDB size.</summary>
        internal const int MaximumPortablePdbBytes = 128 * 1024 * 1024;

        /// <summary>The maximum number of entries accepted in one package archive.</summary>
        private const int MaximumArchiveEntries = 4096;

        /// <summary>The embedded Portable PDB payload signature.</summary>
        private const uint EmbeddedPortablePdbSignature = 0x4244504D;

        /// <summary>Compares paths according to platform filename semantics.</summary>
        private static readonly StringComparer PathComparer = OperatingSystem.IsWindows()
            ? StringComparer.OrdinalIgnoreCase
            : StringComparer.Ordinal;

        /// <summary>Protects configuration and context-local cache entries.</summary>
        private readonly object gate = new();

        /// <summary>Stores positive, negative, ambiguous, and in-progress results.</summary>
        private readonly Dictionary<string, Entry> entries = new(StringComparer.Ordinal);

        /// <summary>The optional immutable local-source snapshot.</summary>
        private ExternalPortablePdbAcquisitionConfiguration? configuration;

        /// <summary>Whether a source snapshot was configured.</summary>
        private bool isConfigured;

        /// <summary>Whether any acquisition lookup has started.</summary>
        private bool acquisitionStarted;

        /// <summary>The number of candidate images considered.</summary>
        private long candidatesConsidered;

        /// <summary>The number of file or archive-entry open attempts.</summary>
        private long candidatesOpened;

        /// <summary>The number of local package archives inspected.</summary>
        private long localSymbolPackagesInspected;

        /// <summary>The number of existing P4B validation attempts.</summary>
        private long validationAttempts;

        /// <summary>The number of positive context-local cache hits.</summary>
        private long positiveCacheHits;

        /// <summary>The number of negative context-local cache hits.</summary>
        private long negativeCacheHits;

        /// <summary>Configures immutable local sources without touching them.</summary>
        /// <param name="sourceConfiguration">The context-local source snapshot.</param>
        /// <returns>
        /// <see langword="true"/> for a new or idempotent configuration;
        /// otherwise <see langword="false"/> after acquisition starts.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="sourceConfiguration"/> is
        /// <see langword="null"/>.
        /// </exception>
        public bool TryConfigure(
            ExternalPortablePdbAcquisitionConfiguration sourceConfiguration)
        {
            ArgumentNullException.ThrowIfNull(sourceConfiguration);

            lock (gate)
            {
                if (isConfigured)
                {
                    return configuration!.IsEquivalentTo(sourceConfiguration);
                }

                if (acquisitionStarted)
                {
                    return false;
                }

                configuration = sourceConfiguration;
                isConfigured = true;
                return true;
            }
        }

        /// <summary>Gets an atomic snapshot of bounded acquisition work.</summary>
        /// <returns>The current context-local counters.</returns>
        public ExternalPortablePdbAcquisitionStatistics GetStatistics()
        {
            return new ExternalPortablePdbAcquisitionStatistics(
                Interlocked.Read(ref candidatesConsidered),
                Interlocked.Read(ref candidatesOpened),
                Interlocked.Read(ref localSymbolPackagesInspected),
                RemoteSymbolRequests: 0,
                DownloadedPdbBytes: 0,
                Interlocked.Read(ref validationAttempts),
                Interlocked.Read(ref positiveCacheHits),
                Interlocked.Read(ref negativeCacheHits));
        }

        /// <summary>
        /// Tries to acquire one exact Portable PDB for validated PE debug provenance.
        /// </summary>
        /// <param name="debugDirectory">The authoritative P4A provenance.</param>
        /// <param name="targetPePath">The exact target PE candidate path.</param>
        /// <param name="provenance">The P4B/P5A result when successful.</param>
        /// <param name="origin">The source of the selected exact candidate.</param>
        /// <returns><see langword="true"/> only for an exact unambiguous candidate.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown transitively if validated PE debug provenance is malformed.
        /// </exception>
        public bool TryAcquire(
            ExternalPeDebugDirectoryDescriptor debugDirectory,
            string targetPePath,
            out ExternalCompilationProvenanceDescriptor provenance,
            out ExternalPortablePdbOrigin origin)
        {
            if (debugDirectory == null || string.IsNullOrWhiteSpace(targetPePath))
            {
                provenance = null!;
                origin = default;
                return false;
            }

            string key = CreateCacheKey(debugDirectory, targetPePath);
            ExternalPortablePdbAcquisitionConfiguration? sources;

            lock (gate)
            {
                acquisitionStarted = true;
                while (entries.TryGetValue(key, out Entry? cached))
                {
                    if (cached.State == AcquisitionState.InProgress)
                    {
                        Monitor.Wait(gate);
                        continue;
                    }

                    if (cached.State == AcquisitionState.Found)
                    {
                        Interlocked.Increment(ref positiveCacheHits);
                        provenance = cached.Provenance!;
                        origin = cached.Origin.GetValueOrDefault();
                        return true;
                    }

                    Interlocked.Increment(ref negativeCacheHits);
                    provenance = null!;
                    origin = default;
                    return false;
                }

                entries.Add(key, new Entry(AcquisitionState.InProgress));
                sources = configuration;
            }

            AcquisitionState state = AcquisitionState.NotFound;
            ExternalCompilationProvenanceDescriptor? acquired = null;
            ExternalPortablePdbOrigin? acquiredOrigin = null;
            try
            {
                state = TryAcquireCore(
                    debugDirectory,
                    targetPePath,
                    sources,
                    out acquired,
                    out acquiredOrigin);
            }
            finally
            {
                lock (gate)
                {
                    Entry entry = entries[key];
                    entry.State = state;
                    entry.Provenance = acquired;
                    entry.Origin = acquiredOrigin;
                    Monitor.PulseAll(gate);
                }
            }

            provenance = acquired!;
            origin = acquiredOrigin.GetValueOrDefault();
            return state == AcquisitionState.Found;
        }

        /// <summary>Executes source tiers in deterministic local-first order.</summary>
        /// <param name="debugDirectory">The authoritative P4A provenance.</param>
        /// <param name="targetPePath">The exact target PE candidate path.</param>
        /// <param name="sources">The optional configured source snapshot.</param>
        /// <param name="provenance">The exact P4B/P5A result.</param>
        /// <param name="origin">The selected source tier.</param>
        /// <returns>The positive, negative, or ambiguous acquisition state.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown transitively if validated PE debug provenance is malformed.
        /// </exception>
        private AcquisitionState TryAcquireCore(
            ExternalPeDebugDirectoryDescriptor debugDirectory,
            string targetPePath,
            ExternalPortablePdbAcquisitionConfiguration? sources,
            out ExternalCompilationProvenanceDescriptor? provenance,
            out ExternalPortablePdbOrigin? origin)
        {
            List<CandidateImage> candidates = ReadEmbeddedCandidates(targetPePath);
            AcquisitionState state = ValidateCandidates(
                debugDirectory,
                candidates,
                out provenance,
                out origin);
            if (state != AcquisitionState.NotFound)
            {
                return state;
            }

            ImmutableArray<string> pdbNames = GetPortablePdbFileNames(debugDirectory);
            if (pdbNames.IsEmpty)
            {
                provenance = null;
                origin = null;
                return AcquisitionState.NotFound;
            }

            if (sources != null)
            {
                candidates = ReadPathCandidates(
                    sources.KnownPdbPaths,
                    ExternalPortablePdbOrigin.KnownPath);
                state = ValidateCandidates(debugDirectory, candidates, out provenance, out origin);
                if (state != AcquisitionState.NotFound)
                {
                    return state;
                }
            }

            string? targetDirectory = Path.GetDirectoryName(targetPePath);
            if (targetDirectory != null)
            {
                candidates = ReadNamedPathCandidates(
                    [targetDirectory],
                    pdbNames,
                    ExternalPortablePdbOrigin.Sibling);
                state = ValidateCandidates(debugDirectory, candidates, out provenance, out origin);
                if (state != AcquisitionState.NotFound)
                {
                    return state;
                }
            }

            if (sources != null)
            {
                candidates = ReadNamedPathCandidates(
                    sources.SearchRoots,
                    pdbNames,
                    ExternalPortablePdbOrigin.ConfiguredRoot);
                state = ValidateCandidates(debugDirectory, candidates, out provenance, out origin);
                if (state != AcquisitionState.NotFound)
                {
                    return state;
                }

                candidates = ReadArchiveCandidates(sources.PackageArchivePaths, pdbNames);
                state = ValidateCandidates(debugDirectory, candidates, out provenance, out origin);
                if (state != AcquisitionState.NotFound)
                {
                    return state;
                }
            }

            provenance = null;
            origin = null;
            return AcquisitionState.NotFound;
        }

        /// <summary>Validates one source tier exclusively through P4B/P5A.</summary>
        /// <param name="debugDirectory">The authoritative P4A provenance.</param>
        /// <param name="candidates">The immutable candidate images.</param>
        /// <param name="provenance">The exact selected provenance.</param>
        /// <param name="origin">The selected source tier.</param>
        /// <returns>The tier validation state.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown transitively if validated PE debug provenance is malformed.
        /// </exception>
        private AcquisitionState ValidateCandidates(
            ExternalPeDebugDirectoryDescriptor debugDirectory,
            IReadOnlyList<CandidateImage> candidates,
            out ExternalCompilationProvenanceDescriptor? provenance,
            out ExternalPortablePdbOrigin? origin)
        {
            ImmutableArray<byte> selectedImage = default;
            ExternalCompilationProvenanceDescriptor? selected = null;
            ExternalPortablePdbOrigin? selectedOrigin = null;

            foreach (CandidateImage candidate in candidates)
            {
                Interlocked.Increment(ref validationAttempts);
                if (!ExternalCompilationProvenanceDescriptorFactory.TryCreate(
                        debugDirectory,
                        candidate.Image,
                        out ExternalCompilationProvenanceDescriptor descriptor))
                {
                    continue;
                }

                if (selected == null)
                {
                    selectedImage = candidate.Image;
                    selected = descriptor;
                    selectedOrigin = candidate.Origin;
                }
                else if (!selectedImage.AsSpan().SequenceEqual(candidate.Image.AsSpan()))
                {
                    provenance = null;
                    origin = null;
                    return AcquisitionState.Ambiguous;
                }
            }

            provenance = selected;
            origin = selectedOrigin;
            return selected == null ? AcquisitionState.NotFound : AcquisitionState.Found;
        }

        /// <summary>Reads embedded Portable PDB candidates from the exact target PE.</summary>
        /// <param name="targetPePath">The exact target PE path.</param>
        /// <returns>The bounded candidate images.</returns>
        private List<CandidateImage> ReadEmbeddedCandidates(string targetPePath)
        {
            List<CandidateImage> candidates = new();
            try
            {
                using FileStream stream = new(
                    targetPePath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read);
                using PEReader reader = new(
                    stream,
                    PEStreamOptions.LeaveOpen | PEStreamOptions.PrefetchEntireImage);

                foreach (DebugDirectoryEntry entry in reader.ReadDebugDirectory())
                {
                    if (entry.Type == DebugDirectoryEntryType.EmbeddedPortablePdb
                        && TryReadEmbeddedImage(reader, entry, out ImmutableArray<byte> image))
                    {
                        candidates.Add(new CandidateImage(image, ExternalPortablePdbOrigin.Embedded));
                    }
                }
            }
            catch (BadImageFormatException)
            {
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
            catch (InvalidOperationException)
            {
            }
            catch (ArgumentException)
            {
            }

            return candidates;
        }

        /// <summary>Decompresses one bounded embedded Portable PDB payload.</summary>
        /// <param name="reader">The target PE reader.</param>
        /// <param name="entry">The embedded debug-directory entry.</param>
        /// <param name="image">The complete decompressed candidate.</param>
        /// <returns><see langword="true"/> when the payload is structurally bounded.</returns>
        private static bool TryReadEmbeddedImage(
            PEReader reader,
            DebugDirectoryEntry entry,
            out ImmutableArray<byte> image)
        {
            if (entry.DataSize < 8)
            {
                image = default;
                return false;
            }

            ImmutableArray<byte> payload = reader.GetSectionData(
                    entry.DataRelativeVirtualAddress)
                .GetContent(0, entry.DataSize);
            ReadOnlySpan<byte> span = payload.AsSpan();
            int uncompressedSize = BinaryPrimitives.ReadInt32LittleEndian(span.Slice(4, 4));
            if (BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(0, 4))
                    != EmbeddedPortablePdbSignature
                || uncompressedSize <= 0
                || uncompressedSize > MaximumPortablePdbBytes)
            {
                image = default;
                return false;
            }

            byte[] result = new byte[uncompressedSize];
            using MemoryStream compressed = new(payload.ToArray(), 8, payload.Length - 8, false);
            using DeflateStream deflate = new(compressed, CompressionMode.Decompress, false);
            int total = 0;
            while (total < result.Length)
            {
                int read = deflate.Read(result, total, result.Length - total);
                if (read == 0)
                {
                    image = default;
                    return false;
                }

                total += read;
            }

            if (deflate.ReadByte() != -1)
            {
                image = default;
                return false;
            }

            image = ImmutableCollectionsMarshal.AsImmutableArray(result);
            return true;
        }

        /// <summary>Reads caller-known candidate paths without filename assumptions.</summary>
        /// <param name="paths">The known candidate paths.</param>
        /// <param name="origin">The configured source tier.</param>
        /// <returns>The bounded readable candidates.</returns>
        private List<CandidateImage> ReadPathCandidates(
            IEnumerable<string> paths,
            ExternalPortablePdbOrigin origin)
        {
            List<CandidateImage> candidates = new();
            HashSet<string> seen = new(PathComparer);
            foreach (string path in paths)
            {
                if (seen.Add(path) && TryReadFile(path, origin, out CandidateImage candidate))
                {
                    candidates.Add(candidate);
                }
            }

            return candidates;
        }

        /// <summary>Probes expected filenames directly within bounded roots.</summary>
        /// <param name="roots">The local candidate roots.</param>
        /// <param name="pdbNames">The CodeView-derived safe filenames.</param>
        /// <param name="origin">The configured source tier.</param>
        /// <returns>The bounded readable candidates.</returns>
        private List<CandidateImage> ReadNamedPathCandidates(
            IEnumerable<string> roots,
            IEnumerable<string> pdbNames,
            ExternalPortablePdbOrigin origin)
        {
            List<string> paths = new();
            foreach (string root in roots)
            {
                foreach (string pdbName in pdbNames)
                {
                    paths.Add(Path.Combine(root, pdbName));
                }
            }

            return ReadPathCandidates(paths, origin);
        }

        /// <summary>Reads one bounded candidate file without trusting its name.</summary>
        /// <param name="path">The candidate path.</param>
        /// <param name="origin">The local source tier.</param>
        /// <param name="candidate">The complete candidate image.</param>
        /// <returns><see langword="true"/> when the file is readable and bounded.</returns>
        private bool TryReadFile(
            string path,
            ExternalPortablePdbOrigin origin,
            out CandidateImage candidate)
        {
            Interlocked.Increment(ref candidatesOpened);
            try
            {
                using FileStream stream = new(
                    path,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read);
                if (stream.Length <= 0 || stream.Length > MaximumPortablePdbBytes)
                {
                    candidate = default;
                    return false;
                }

                byte[] image = new byte[(int)stream.Length];
                stream.ReadExactly(image);
                Interlocked.Increment(ref candidatesConsidered);
                candidate = new CandidateImage(
                    ImmutableCollectionsMarshal.AsImmutableArray(image),
                    origin);
                return true;
            }
            catch (IOException)
            {
                candidate = default;
                return false;
            }
            catch (UnauthorizedAccessException)
            {
                candidate = default;
                return false;
            }
            catch (ArgumentException)
            {
                candidate = default;
                return false;
            }
            catch (NotSupportedException)
            {
                candidate = default;
                return false;
            }
        }

        /// <summary>Reads matching entries from explicitly configured package archives.</summary>
        /// <param name="archivePaths">The configured archive paths.</param>
        /// <param name="pdbNames">The CodeView-derived safe filenames.</param>
        /// <returns>The bounded candidate images.</returns>
        private List<CandidateImage> ReadArchiveCandidates(
            IEnumerable<string> archivePaths,
            ImmutableArray<string> pdbNames)
        {
            List<CandidateImage> candidates = new();
            HashSet<string> names = new(pdbNames, StringComparer.OrdinalIgnoreCase);
            foreach (string archivePath in archivePaths)
            {
                Interlocked.Increment(ref localSymbolPackagesInspected);
                Interlocked.Increment(ref candidatesOpened);
                try
                {
                    List<CandidateImage> archiveCandidates = new();
                    using FileStream stream = new(
                        archivePath,
                        FileMode.Open,
                        FileAccess.Read,
                        FileShare.Read);
                    using ZipArchive archive = new(stream, ZipArchiveMode.Read, false);
                    if (archive.Entries.Count <= MaximumArchiveEntries
                        && TryReadArchiveEntries(
                            archive,
                            archivePath,
                            names,
                            archiveCandidates))
                    {
                        candidates.AddRange(archiveCandidates);
                    }
                }
                catch (InvalidDataException)
                {
                }
                catch (IOException)
                {
                }
                catch (UnauthorizedAccessException)
                {
                }
            }

            return candidates;
        }

        /// <summary>Validates archive names and reads bounded matching entries.</summary>
        /// <param name="archive">The opened package archive.</param>
        /// <param name="archivePath">The configured archive path.</param>
        /// <param name="pdbNames">The expected safe PDB filenames.</param>
        /// <param name="candidates">The destination candidate collection.</param>
        /// <returns><see langword="true"/> when the archive is structurally safe.</returns>
        private bool TryReadArchiveEntries(
            ZipArchive archive,
            string archivePath,
            ISet<string> pdbNames,
            ICollection<CandidateImage> candidates)
        {
            HashSet<string> entryNames = new(StringComparer.OrdinalIgnoreCase);
            ExternalPortablePdbOrigin origin = string.Equals(
                Path.GetExtension(archivePath),
                ".snupkg",
                StringComparison.OrdinalIgnoreCase)
                ? ExternalPortablePdbOrigin.SymbolPackage
                : ExternalPortablePdbOrigin.NuGetPackage;

            foreach (ZipArchiveEntry entry in archive.Entries)
            {
                if (!IsSafeArchiveEntry(entry.FullName)
                    || !entryNames.Add(entry.FullName)
                    || entry.Length < 0
                    || entry.Length > MaximumPortablePdbBytes)
                {
                    return false;
                }

                string fileName = GetPortableFileName(entry.FullName);
                if (!pdbNames.Contains(fileName) || entry.Length == 0)
                {
                    continue;
                }

                Interlocked.Increment(ref candidatesOpened);
                using Stream entryStream = entry.Open();
                byte[] image = ReadBounded(entryStream, (int)entry.Length);
                if (image.Length != entry.Length)
                {
                    return false;
                }

                Interlocked.Increment(ref candidatesConsidered);
                candidates.Add(new CandidateImage(
                    ImmutableCollectionsMarshal.AsImmutableArray(image),
                    origin));
            }

            return true;
        }

        /// <summary>Reads exactly one bounded archive entry.</summary>
        /// <param name="stream">The decompressed archive-entry stream.</param>
        /// <param name="expectedLength">The declared uncompressed size.</param>
        /// <returns>The complete bytes, or an empty array when inconsistent.</returns>
        private static byte[] ReadBounded(Stream stream, int expectedLength)
        {
            byte[] image = new byte[expectedLength];
            int total = 0;
            while (total < image.Length)
            {
                int read = stream.Read(image, total, image.Length - total);
                if (read == 0)
                {
                    return [];
                }

                total += read;
            }

            return stream.ReadByte() == -1 ? image : [];
        }

        /// <summary>Checks that an archive path cannot express traversal or rooting.</summary>
        /// <param name="entryName">The raw ZIP entry name.</param>
        /// <returns><see langword="true"/> only for a safe relative entry name.</returns>
        private static bool IsSafeArchiveEntry(string entryName)
        {
            if (string.IsNullOrWhiteSpace(entryName)
                || entryName.StartsWith("/", StringComparison.Ordinal)
                || entryName.StartsWith('\\'))
            {
                return false;
            }

            string normalized = entryName.Replace('\\', '/');
            if (normalized.Length >= 2 && normalized[1] == ':')
            {
                return false;
            }

            return !normalized.Split('/', StringSplitOptions.RemoveEmptyEntries)
                .Any(static segment => segment is "." or "..");
        }

        /// <summary>Extracts safe PDB filenames from portable CodeView provenance.</summary>
        /// <param name="debugDirectory">The authoritative P4A descriptor.</param>
        /// <returns>The deterministic distinct filename sequence.</returns>
        private static ImmutableArray<string> GetPortablePdbFileNames(
            ExternalPeDebugDirectoryDescriptor debugDirectory)
        {
            return debugDirectory.CodeViewPdbReferences
                .Where(static reference => reference.IsPortable)
                .Select(static reference => GetPortableFileName(reference.Path))
                .Where(static name => name.Length != 0
                    && string.Equals(
                        Path.GetExtension(name),
                        ".pdb",
                        StringComparison.OrdinalIgnoreCase))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(static name => name, StringComparer.Ordinal)
                .ToImmutableArray();
        }

        /// <summary>Extracts a filename without trusting host path syntax.</summary>
        /// <param name="path">The untrusted provenance or archive path.</param>
        /// <returns>The final slash-delimited path component.</returns>
        private static string GetPortableFileName(string path)
        {
            int separator = Math.Max(path.LastIndexOf('/'), path.LastIndexOf('\\'));
            return separator < 0 ? path : path[(separator + 1)..];
        }

        /// <summary>Builds a cache key from exact PE and Portable PDB provenance.</summary>
        /// <param name="debugDirectory">The P4A descriptor.</param>
        /// <param name="targetPePath">The target-specific sibling-search location.</param>
        /// <returns>The deterministic context-local key.</returns>
        private static string CreateCacheKey(
            ExternalPeDebugDirectoryDescriptor debugDirectory,
            string targetPePath)
        {
            StringBuilder builder = new();
            _ = builder.Append(debugDirectory.ManifestModule.ModuleVersionId.ToString("N"));
            string normalizedTargetPath;
            try
            {
                normalizedTargetPath = Path.GetFullPath(targetPePath);
            }
            catch (ArgumentException)
            {
                normalizedTargetPath = targetPePath;
            }
            catch (NotSupportedException)
            {
                normalizedTargetPath = targetPePath;
            }

            if (OperatingSystem.IsWindows())
            {
                normalizedTargetPath = normalizedTargetPath.ToUpperInvariant();
            }

            _ = builder.Append("|P:").Append(normalizedTargetPath);
            foreach (ExternalCodeViewPdbReference reference in
                     debugDirectory.CodeViewPdbReferences)
            {
                if (reference.PortablePdbId.HasValue)
                {
                    BlobContentId id = reference.PortablePdbId.Value;
                    _ = builder.Append('|').Append(id.Guid.ToString("N"))
                        .Append(':').Append(id.Stamp.ToString("X8"));
                }
            }

            foreach (BlobContentId id in debugDirectory.EmbeddedPortablePdbIds)
            {
                _ = builder.Append("|E:").Append(id.Guid.ToString("N"))
                    .Append(':').Append(id.Stamp.ToString("X8"));
            }

            foreach (ExternalPdbChecksum checksum in debugDirectory.PdbChecksums)
            {
                _ = builder.Append("|C:").Append(checksum.AlgorithmName)
                    .Append(':').Append(Convert.ToHexString(checksum.Checksum.AsSpan()));
            }

            return builder.ToString();
        }

        /// <summary>One transient candidate image and its local origin.</summary>
        /// <param name="Image">The complete bounded candidate bytes.</param>
        /// <param name="Origin">The local source tier.</param>
        private readonly record struct CandidateImage(
            ImmutableArray<byte> Image,
            ExternalPortablePdbOrigin Origin);

        /// <summary>One context-local acquisition cache entry.</summary>
        private sealed class Entry
        {
            /// <summary>Initializes an entry in the supplied state.</summary>
            /// <param name="state">The initial acquisition state.</param>
            public Entry(AcquisitionState state)
            {
                State = state;
            }

            /// <summary>Gets or sets the current state.</summary>
            /// <value>The cached state.</value>
            public AcquisitionState State { get; set; }

            /// <summary>Gets or sets validated compilation provenance.</summary>
            /// <value>The positive result, or <see langword="null"/>.</value>
            public ExternalCompilationProvenanceDescriptor? Provenance { get; set; }

            /// <summary>Gets or sets the selected local origin.</summary>
            /// <value>The positive origin, or <see langword="null"/>.</value>
            public ExternalPortablePdbOrigin? Origin { get; set; }
        }

        /// <summary>Describes one context-local cache result.</summary>
        private enum AcquisitionState
        {
            /// <summary>A lookup is currently executing.</summary>
            InProgress,

            /// <summary>No exact candidate exists.</summary>
            NotFound,

            /// <summary>One exact candidate image exists.</summary>
            Found,

            /// <summary>Distinct exact candidate images were observed.</summary>
            Ambiguous
        }
    }
}
