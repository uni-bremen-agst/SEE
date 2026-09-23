using System.Text.Json;
using System.Text.Json.Serialization;

namespace XMLDocNormalizer.Evaluation
{
    /// <summary>
    /// Loads and validates the explicit E1 manifest without touching package artifacts.
    /// </summary>
    public static class EvaluationManifestLoader
    {
        /// <summary>
        /// Loads one manifest and rejects floating versions, unsafe paths, and duplicates.
        /// </summary>
        /// <param name="path">The manifest path.</param>
        /// <returns>The validated manifest.</returns>
        public static EvaluationManifest Load(string path)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(path);
            string json = File.ReadAllText(path);
            JsonSerializerOptions options = new()
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new JsonStringEnumConverter() }
            };
            EvaluationManifest manifest = JsonSerializer.Deserialize<EvaluationManifest>(json, options)
                ?? throw new InvalidOperationException("The evaluation manifest is empty.");
            Validate(manifest);
            manifest.Artifacts = manifest.Artifacts
                .OrderBy(static artifact => artifact.Id, StringComparer.Ordinal)
                .ToList();
            manifest.Candidates = manifest.Candidates
                .OrderBy(static candidate => candidate.Id, StringComparer.Ordinal)
                .ToList();
            return manifest;
        }

        /// <summary>
        /// Resolves one manifest path while requiring containment in the workspace.
        /// </summary>
        /// <param name="workspace">The fully qualified workspace root.</param>
        /// <param name="relativePath">The manifest-relative path.</param>
        /// <returns>The contained fully qualified path.</returns>
        public static string ResolveWorkspacePath(string workspace, string relativePath)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(workspace);
            ArgumentException.ThrowIfNullOrWhiteSpace(relativePath);

            if (!Path.IsPathFullyQualified(workspace) || Path.IsPathFullyQualified(relativePath))
            {
                throw new InvalidOperationException("Evaluation paths must be workspace-relative.");
            }

            string root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(workspace));
            string result = Path.GetFullPath(Path.Combine(root, relativePath));
            StringComparison comparison = OperatingSystem.IsWindows()
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal;

            if (!result.StartsWith(root + Path.DirectorySeparatorChar, comparison))
            {
                throw new InvalidOperationException("Evaluation path leaves the workspace.");
            }

            return result;
        }

        private static void Validate(EvaluationManifest manifest)
        {
            if (manifest.SchemaVersion != 1
                || manifest.Artifacts.Count == 0
                || manifest.Candidates.Count == 0)
            {
                throw new InvalidOperationException("Unsupported or empty evaluation manifest.");
            }

            HashSet<string> artifactIds = new(StringComparer.Ordinal);
            foreach (EvaluationArtifact artifact in manifest.Artifacts)
            {
                if (!artifactIds.Add(artifact.Id)
                    || !Uri.TryCreate(artifact.Url, UriKind.Absolute, out Uri? uri)
                    || uri.Scheme != Uri.UriSchemeHttps
                    || artifact.Sha256.Length != 64
                    || !artifact.Sha256.All(Uri.IsHexDigit)
                    || !IsSafeRelativePath(artifact.ExtractTo))
                {
                    throw new InvalidOperationException($"Invalid evaluation artifact '{artifact.Id}'.");
                }
            }

            HashSet<string> candidateIds = new(StringComparer.Ordinal);
            foreach (EvaluationCandidate candidate in manifest.Candidates)
            {
                if (!candidateIds.Add(candidate.Id)
                    || string.IsNullOrWhiteSpace(candidate.Package)
                    || string.IsNullOrWhiteSpace(candidate.Version)
                    || candidate.Version.Contains('*')
                    || string.Equals(candidate.Version, "latest", StringComparison.OrdinalIgnoreCase)
                    || !IsSafeRelativePath(candidate.AssemblyPath)
                    || (candidate.PdbPath != null && !IsSafeRelativePath(candidate.PdbPath))
                    || candidate.ReferenceRoots.Any(static path => !IsSafeRelativePath(path))
                    || candidate.ReferencePackages.Any(static hint =>
                        string.IsNullOrWhiteSpace(hint.AssemblySimpleName)
                        || string.IsNullOrWhiteSpace(hint.PackageId)
                        || hint.Versions.Count == 0
                        || hint.Versions.Any(string.IsNullOrWhiteSpace)
                        || hint.Provider is not ("NuGetPackage" or "DotNetReferencePack")
                        || string.IsNullOrWhiteSpace(hint.Evidence)))
                {
                    throw new InvalidOperationException($"Invalid evaluation candidate '{candidate.Id}'.");
                }
            }
        }

        private static bool IsSafeRelativePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || Path.IsPathFullyQualified(path))
            {
                return false;
            }

            return !path.Split(['/', '\\'], StringSplitOptions.RemoveEmptyEntries)
                .Any(static segment => segment is "." or "..");
        }
    }
}
