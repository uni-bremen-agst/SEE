using System.Collections.Immutable;

namespace XMLDocNormalizer.Execution.Semantic
{
    /// <summary>Describes immutable context-local local Portable PDB sources.</summary>
    internal sealed class ExternalPortablePdbAcquisitionConfiguration
    {
        /// <summary>Compares paths according to platform filename semantics.</summary>
        private static readonly StringComparer PathComparer = OperatingSystem.IsWindows()
            ? StringComparer.OrdinalIgnoreCase
            : StringComparer.Ordinal;

        /// <summary>Initializes one normalized source snapshot.</summary>
        /// <param name="knownPdbPaths">The known Portable PDB candidate paths.</param>
        /// <param name="searchRoots">The configured non-recursive roots.</param>
        /// <param name="packageArchivePaths">The configured package archives.</param>
        private ExternalPortablePdbAcquisitionConfiguration(
            ImmutableArray<string> knownPdbPaths,
            ImmutableArray<string> searchRoots,
            ImmutableArray<string> packageArchivePaths)
        {
            KnownPdbPaths = knownPdbPaths;
            SearchRoots = searchRoots;
            PackageArchivePaths = packageArchivePaths;
        }

        /// <summary>Gets explicitly known Portable PDB paths.</summary>
        /// <value>The normalized deterministic snapshot.</value>
        public ImmutableArray<string> KnownPdbPaths { get; }

        /// <summary>Gets configured non-recursive Portable PDB roots.</summary>
        /// <value>The normalized deterministic snapshot.</value>
        public ImmutableArray<string> SearchRoots { get; }

        /// <summary>Gets explicitly configured NuGet or symbol package archives.</summary>
        /// <value>The normalized deterministic snapshot.</value>
        public ImmutableArray<string> PackageArchivePaths { get; }

        /// <summary>Creates an immutable source configuration without filesystem access.</summary>
        /// <param name="knownPdbPaths">Known Portable PDB candidate paths.</param>
        /// <param name="searchRoots">Non-recursive local roots.</param>
        /// <param name="packageArchivePaths">Explicit package archive paths.</param>
        /// <param name="configuration">The normalized configuration when valid.</param>
        /// <returns><see langword="true"/> when every supplied path is absolute.</returns>
        public static bool TryCreate(
            IEnumerable<string> knownPdbPaths,
            IEnumerable<string> searchRoots,
            IEnumerable<string> packageArchivePaths,
            out ExternalPortablePdbAcquisitionConfiguration configuration)
        {
            if (knownPdbPaths == null
                || searchRoots == null
                || packageArchivePaths == null
                || !TryNormalize(knownPdbPaths, out ImmutableArray<string> known)
                || !TryNormalize(searchRoots, out ImmutableArray<string> roots)
                || !TryNormalize(packageArchivePaths, out ImmutableArray<string> archives)
                || archives.Any(static path => !IsPackageArchivePath(path)))
            {
                configuration = null!;
                return false;
            }

            configuration = new ExternalPortablePdbAcquisitionConfiguration(
                known,
                roots,
                archives);
            return true;
        }

        /// <summary>Compares two normalized source snapshots.</summary>
        /// <param name="other">The other configuration.</param>
        /// <returns><see langword="true"/> when every path is equivalent.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="other"/> is <see langword="null"/>.
        /// </exception>
        public bool IsEquivalentTo(ExternalPortablePdbAcquisitionConfiguration other)
        {
            ArgumentNullException.ThrowIfNull(other);
            return PathsEqual(KnownPdbPaths, other.KnownPdbPaths)
                && PathsEqual(SearchRoots, other.SearchRoots)
                && PathsEqual(PackageArchivePaths, other.PackageArchivePaths);
        }

        /// <summary>Normalizes and deduplicates one absolute path sequence.</summary>
        /// <param name="paths">The supplied paths.</param>
        /// <param name="normalized">The normalized deterministic snapshot.</param>
        /// <returns><see langword="true"/> when all paths are valid and absolute.</returns>
        private static bool TryNormalize(
            IEnumerable<string> paths,
            out ImmutableArray<string> normalized)
        {
            try
            {
                HashSet<string> unique = new(PathComparer);
                foreach (string? path in paths)
                {
                    if (path == null || !Path.IsPathFullyQualified(path))
                    {
                        normalized = default;
                        return false;
                    }

                    unique.Add(Path.TrimEndingDirectorySeparator(Path.GetFullPath(path)));
                }

                normalized = unique.OrderBy(static path => path, StringComparer.Ordinal)
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
            catch (IOException)
            {
                normalized = default;
                return false;
            }
        }

        /// <summary>Compares two deterministic normalized path sequences.</summary>
        /// <param name="left">The first sequence.</param>
        /// <param name="right">The second sequence.</param>
        /// <returns><see langword="true"/> when the sequences are equivalent.</returns>
        private static bool PathsEqual(
            ImmutableArray<string> left,
            ImmutableArray<string> right)
        {
            return left.Length == right.Length
                && left.Zip(right).All(pair => PathComparer.Equals(pair.First, pair.Second));
        }

        /// <summary>Checks the two explicitly supported package container types.</summary>
        /// <param name="path">The normalized configured archive path.</param>
        /// <returns><see langword="true"/> for nupkg or snupkg paths.</returns>
        private static bool IsPackageArchivePath(string path)
        {
            string extension = Path.GetExtension(path);
            return string.Equals(extension, ".nupkg", StringComparison.OrdinalIgnoreCase)
                || string.Equals(extension, ".snupkg", StringComparison.OrdinalIgnoreCase);
        }
    }
}
