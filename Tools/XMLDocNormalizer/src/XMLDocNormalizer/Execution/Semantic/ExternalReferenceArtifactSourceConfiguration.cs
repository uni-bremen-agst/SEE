using System.Collections.Immutable;
using System.Runtime.InteropServices;
using System.Xml.Linq;

namespace XMLDocNormalizer.Execution.Semantic
{
    /// <summary>
    /// Describes immutable, context-local standard artifact sources for P7A.
    /// </summary>
    internal sealed class ExternalReferenceArtifactSourceConfiguration
    {
        /// <summary>Compares paths according to platform filename semantics.</summary>
        private static readonly StringComparer PathComparer = OperatingSystem.IsWindows()
            ? StringComparer.OrdinalIgnoreCase
            : StringComparer.Ordinal;

        /// <summary>Initializes one already normalized immutable source snapshot.</summary>
        /// <param name="loadedReferencePaths">The loaded reference paths.</param>
        /// <param name="nuGetGlobalPackagesFolder">The optional NuGet root.</param>
        /// <param name="dotNetRoots">The local dotnet installation roots.</param>
        private ExternalReferenceArtifactSourceConfiguration(
            ImmutableArray<string> loadedReferencePaths,
            string? nuGetGlobalPackagesFolder,
            ImmutableArray<string> dotNetRoots)
        {
            LoadedReferencePaths = loadedReferencePaths;
            NuGetGlobalPackagesFolder = nuGetGlobalPackagesFolder;
            DotNetRoots = dotNetRoots;
        }

        /// <summary>Gets already loaded file-backed metadata-reference paths.</summary>
        /// <value>The normalized deterministic path snapshot.</value>
        public ImmutableArray<string> LoadedReferencePaths { get; }

        /// <summary>Gets the resolved NuGet global-packages folder, when available.</summary>
        /// <value>The normalized folder, or <see langword="null"/>.</value>
        public string? NuGetGlobalPackagesFolder { get; }

        /// <summary>Gets local dotnet installation roots.</summary>
        /// <value>The normalized deterministic root snapshot.</value>
        public ImmutableArray<string> DotNetRoots { get; }

        /// <summary>
        /// Creates a validated immutable source configuration without filesystem access.
        /// </summary>
        /// <param name="loadedReferencePaths">Known file-backed Roslyn reference paths.</param>
        /// <param name="nuGetGlobalPackagesFolder">The resolved NuGet package root.</param>
        /// <param name="dotNetRoots">Local dotnet installation roots.</param>
        /// <param name="configuration">The immutable configuration when valid.</param>
        /// <returns><see langword="true"/> when every supplied path is absolute.</returns>
        public static bool TryCreate(
            IEnumerable<string> loadedReferencePaths,
            string? nuGetGlobalPackagesFolder,
            IEnumerable<string> dotNetRoots,
            out ExternalReferenceArtifactSourceConfiguration configuration)
        {
            if (loadedReferencePaths == null
                || dotNetRoots == null
                || !TryNormalizePaths(loadedReferencePaths, out ImmutableArray<string> loaded)
                || !TryNormalizePaths(dotNetRoots, out ImmutableArray<string> dotNet)
                || !TryNormalizeOptionalPath(nuGetGlobalPackagesFolder, out string? nuGet))
            {
                configuration = null!;
                return false;
            }

            configuration = new ExternalReferenceArtifactSourceConfiguration(
                loaded,
                nuGet,
                dotNet);
            return true;
        }

        /// <summary>
        /// Resolves standard local sources for the current process without scanning them.
        /// </summary>
        /// <param name="loadedReferencePaths">Known file-backed Roslyn reference paths.</param>
        /// <param name="configurationDirectory">
        /// The directory whose NuGet.Config hierarchy applies.
        /// </param>
        /// <param name="configuration">The resolved immutable configuration.</param>
        /// <returns><see langword="true"/> when source paths can be normalized.</returns>
        public static bool TryCreateForCurrentProcess(
            IEnumerable<string> loadedReferencePaths,
            string configurationDirectory,
            out ExternalReferenceArtifactSourceConfiguration configuration)
        {
            if (configurationDirectory == null
                || !Path.IsPathFullyQualified(configurationDirectory))
            {
                configuration = null!;
                return false;
            }

            string? nuGetRoot = TryResolveNuGetGlobalPackagesFolder(configurationDirectory);
            IEnumerable<string> dotNetRoots = ResolveDotNetRoots();
            return TryCreate(
                loadedReferencePaths,
                nuGetRoot,
                dotNetRoots,
                out configuration);
        }

        /// <summary>
        /// Compares two normalized configurations.
        /// </summary>
        /// <param name="other">The other configuration.</param>
        /// <returns><see langword="true"/> when every source path is equivalent.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="other"/> is <see langword="null"/>.
        /// </exception>
        public bool IsEquivalentTo(ExternalReferenceArtifactSourceConfiguration other)
        {
            ArgumentNullException.ThrowIfNull(other);
            return PathsEqual(LoadedReferencePaths, other.LoadedReferencePaths)
                && PathComparer.Equals(
                    NuGetGlobalPackagesFolder,
                    other.NuGetGlobalPackagesFolder)
                && PathsEqual(DotNetRoots, other.DotNetRoots);
        }

        /// <summary>Resolves the effective NuGet global-packages folder.</summary>
        /// <param name="configurationDirectory">The NuGet settings start directory.</param>
        /// <returns>The normalized folder, or <see langword="null"/>.</returns>
        private static string? TryResolveNuGetGlobalPackagesFolder(
            string configurationDirectory)
        {
            string? environmentPath = Environment.GetEnvironmentVariable("NUGET_PACKAGES");
            if (!string.IsNullOrWhiteSpace(environmentPath))
            {
                string expanded = Environment.ExpandEnvironmentVariables(environmentPath);
                return TryGetFullPath(expanded, Environment.CurrentDirectory, out string path)
                    ? path
                    : null;
            }

            string? configuredPath = null;
            foreach (string configPath in EnumerateNuGetConfigPaths(configurationDirectory))
            {
                if (TryReadGlobalPackagesFolder(configPath, out string? value))
                {
                    configuredPath = value;
                }
            }

            if (configuredPath != null)
            {
                return configuredPath;
            }

            string userProfile = Environment.GetFolderPath(
                Environment.SpecialFolder.UserProfile);
            return TryGetFullPath(
                Path.Combine(userProfile, ".nuget", "packages"),
                userProfile,
                out string defaultPath)
                ? defaultPath
                : null;
        }

        /// <summary>Collects applicable NuGet.Config files from broad to narrow scope.</summary>
        /// <param name="configurationDirectory">The settings start directory.</param>
        /// <returns>The deterministic settings-file sequence.</returns>
        private static IEnumerable<string> EnumerateNuGetConfigPaths(
            string configurationDirectory)
        {
            List<string> paths = new();
            string applicationData = Environment.GetFolderPath(
                Environment.SpecialFolder.ApplicationData);
            string userConfig = Path.Combine(applicationData, "NuGet", "NuGet.Config");
            if (File.Exists(userConfig))
            {
                paths.Add(userConfig);
            }

            Stack<string> hierarchy = new();
            DirectoryInfo? directory = new(configurationDirectory);
            while (directory != null)
            {
                hierarchy.Push(directory.FullName);
                directory = directory.Parent;
            }

            while (hierarchy.Count != 0)
            {
                string candidate = Path.Combine(hierarchy.Pop(), "NuGet.Config");
                if (File.Exists(candidate))
                {
                    paths.Add(candidate);
                }
            }

            return paths;
        }

        /// <summary>Reads one optional globalPackagesFolder setting.</summary>
        /// <param name="configPath">The fully qualified NuGet.Config path.</param>
        /// <param name="resolvedPath">The normalized configured folder.</param>
        /// <returns><see langword="true"/> when the setting is valid.</returns>
        private static bool TryReadGlobalPackagesFolder(
            string configPath,
            out string? resolvedPath)
        {
            try
            {
                XDocument document = XDocument.Load(configPath, LoadOptions.None);
                XElement? config = document.Root?.Elements()
                    .FirstOrDefault(static element => element.Name.LocalName == "config");
                XElement? entry = config?.Elements()
                    .LastOrDefault(element => element.Name.LocalName == "add"
                        && string.Equals(
                            element.Attribute("key")?.Value,
                            "globalPackagesFolder",
                            StringComparison.OrdinalIgnoreCase));
                string? value = entry?.Attribute("value")?.Value;
                if (string.IsNullOrWhiteSpace(value))
                {
                    resolvedPath = null;
                    return false;
                }

                string expanded = Environment.ExpandEnvironmentVariables(value);
                string baseDirectory = Path.GetDirectoryName(configPath)!;
                return TryGetFullPath(expanded, baseDirectory, out resolvedPath);
            }
            catch (IOException)
            {
                resolvedPath = null;
                return false;
            }
            catch (UnauthorizedAccessException)
            {
                resolvedPath = null;
                return false;
            }
            catch (System.Xml.XmlException)
            {
                resolvedPath = null;
                return false;
            }
        }

        /// <summary>Resolves local dotnet roots from process environment and runtime.</summary>
        /// <returns>Possible local dotnet installation roots.</returns>
        private static IEnumerable<string> ResolveDotNetRoots()
        {
            List<string> candidates = new();
            AddEnvironmentPath("DOTNET_ROOT", candidates);
            AddEnvironmentPath("DOTNET_ROOT(x86)", candidates);

            try
            {
                string runtimeDirectory = RuntimeEnvironment.GetRuntimeDirectory();
                DirectoryInfo versionDirectory = new(runtimeDirectory);
                DirectoryInfo? dotNetDirectory = versionDirectory.Parent?.Parent?.Parent;
                if (dotNetDirectory != null)
                {
                    candidates.Add(dotNetDirectory.FullName);
                }
            }
            catch (InvalidOperationException)
            {
            }

            return candidates;
        }

        /// <summary>Adds one non-empty expanded environment path candidate.</summary>
        /// <param name="variableName">The environment variable name.</param>
        /// <param name="paths">The destination collection.</param>
        private static void AddEnvironmentPath(
            string variableName,
            ICollection<string> paths)
        {
            string? value = Environment.GetEnvironmentVariable(variableName);
            if (!string.IsNullOrWhiteSpace(value))
            {
                paths.Add(Environment.ExpandEnvironmentVariables(value));
            }
        }

        /// <summary>Normalizes and deduplicates one complete absolute path sequence.</summary>
        /// <param name="paths">The supplied paths.</param>
        /// <param name="normalized">The deterministic normalized snapshot.</param>
        /// <returns><see langword="true"/> when every path is absolute and valid.</returns>
        private static bool TryNormalizePaths(
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
        }

        /// <summary>Normalizes one optional absolute path.</summary>
        /// <param name="path">The optional supplied path.</param>
        /// <param name="normalized">The normalized path.</param>
        /// <returns><see langword="true"/> when the optional path is valid.</returns>
        private static bool TryNormalizeOptionalPath(
            string? path,
            out string? normalized)
        {
            if (path == null)
            {
                normalized = null;
                return true;
            }

            if (!Path.IsPathFullyQualified(path))
            {
                normalized = null;
                return false;
            }

            try
            {
                normalized = Path.TrimEndingDirectorySeparator(Path.GetFullPath(path));
                return true;
            }
            catch (ArgumentException)
            {
                normalized = null;
                return false;
            }
            catch (NotSupportedException)
            {
                normalized = null;
                return false;
            }
        }

        /// <summary>Resolves one path against an explicit base directory.</summary>
        /// <param name="path">The supplied absolute or relative path.</param>
        /// <param name="baseDirectory">The explicit base directory.</param>
        /// <param name="resolvedPath">The normalized fully qualified path.</param>
        /// <returns><see langword="true"/> when resolution succeeds.</returns>
        private static bool TryGetFullPath(
            string path,
            string baseDirectory,
            out string resolvedPath)
        {
            try
            {
                resolvedPath = Path.TrimEndingDirectorySeparator(
                    Path.GetFullPath(path, baseDirectory));
                return true;
            }
            catch (ArgumentException)
            {
                resolvedPath = null!;
                return false;
            }
            catch (NotSupportedException)
            {
                resolvedPath = null!;
                return false;
            }
        }

        /// <summary>Compares two normalized deterministic path snapshots.</summary>
        /// <param name="left">The first snapshot.</param>
        /// <param name="right">The second snapshot.</param>
        /// <returns><see langword="true"/> when all paths are platform-equivalent.</returns>
        private static bool PathsEqual(
            ImmutableArray<string> left,
            ImmutableArray<string> right)
        {
            return left.Length == right.Length
                && left.Zip(right).All(pair => PathComparer.Equals(pair.First, pair.Second));
        }
    }
}
