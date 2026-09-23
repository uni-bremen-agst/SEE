using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace XMLDocNormalizer.Evaluation
{
    /// <summary>Writes the G4B inventory for references unavailable to local discovery.</summary>
    internal static class G4BReferenceMatrixWriter
    {
        /// <summary>Writes machine-readable and Markdown matrices beside the evaluation report.</summary>
        public static void Write(
            string outputDirectory,
            EvaluationManifest manifest,
            EvaluationReport report)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(outputDirectory);
            ArgumentNullException.ThrowIfNull(manifest);
            ArgumentNullException.ThrowIfNull(report);

            Dictionary<string, EvaluationCandidate> candidates = manifest.Candidates
                .ToDictionary(static candidate => candidate.Id, StringComparer.Ordinal);
            List<G4BReferenceMatrixEntry> entries = new();
            foreach (EvaluationCandidateResult result in report.Candidates)
            {
                if (!candidates.TryGetValue(result.Id, out EvaluationCandidate? candidate))
                {
                    continue;
                }

                foreach (EvaluationReferenceResult reference in result.References.Where(
                             static reference => reference.AcquisitionResult != "LocalExact"))
                {
                    string simpleName = Path.GetFileNameWithoutExtension(reference.Name);
                    EvaluationReferencePackageHint[] hints = candidate.ReferencePackages
                        .Where(hint => hint.AssemblySimpleName == "*"
                            || hint.AssemblySimpleName.Equals(
                                simpleName,
                                StringComparison.OrdinalIgnoreCase))
                        .ToArray();
                    string artifactClass = hints.Any(static hint => hint.Provider == "DotNetReferencePack")
                        ? "A. .NET Reference-/Targeting-Pack Candidate"
                        : hints.Length != 0
                            ? "C. Known Package Dependency"
                            : "D. Third-Party Package Candidate";
                    entries.Add(new G4BReferenceMatrixEntry
                    {
                        Library = result.Package,
                        LibraryVersion = result.Version,
                        TargetFramework = result.TargetFramework,
                        Ordinal = reference.Ordinal,
                        Reference = reference.Name,
                        AssemblySimpleName = simpleName,
                        AssemblyVersion = null,
                        PublicKeyToken = null,
                        MetadataImageKind = reference.MetadataImageKind,
                        ModuleVersionId = reference.ModuleVersionId,
                        Timestamp = reference.Timestamp,
                        SizeOfImage = reference.ImageSize,
                        Aliases = reference.Aliases,
                        EmbedInteropTypes = reference.EmbedInteropTypes,
                        LocalCandidateResult = "Unavailable",
                        LikelyArtifactClass = artifactClass,
                        AvailableDiscoveryHints = hints.Select(static hint => hint.Evidence).ToList(),
                        PotentialRemoteProviders = hints.Select(static hint => hint.Provider)
                            .Distinct(StringComparer.Ordinal)
                            .Order(StringComparer.Ordinal)
                            .ToList(),
                        FinalAcquisitionResult = reference.AcquisitionResult,
                        RemoteProvider = reference.ArtifactSource,
                        RemoteArtifactIdentity = reference.RemoteArtifactIdentity,
                        RemoteArtifactVersion = reference.RemoteArtifactVersion,
                        RemoteArtifactSha512 = reference.RemoteArtifactSha512,
                        RemoteArchiveEntry = reference.RemoteArchiveEntry
                    });
                }
            }

            entries = entries
                .OrderBy(static entry => entry.Library, StringComparer.Ordinal)
                .ThenBy(static entry => entry.Ordinal)
                .ToList();
            JsonSerializerOptions jsonOptions = new()
            {
                WriteIndented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };
            File.WriteAllText(
                Path.Combine(outputDirectory, "missing-reference-matrix.json"),
                JsonSerializer.Serialize(entries, jsonOptions));
            File.WriteAllText(
                Path.Combine(outputDirectory, "missing-reference-matrix.md"),
                CreateMarkdown(entries));
        }

        private static string CreateMarkdown(IReadOnlyCollection<G4BReferenceMatrixEntry> entries)
        {
            StringBuilder builder = new();
            builder.AppendLine("# G4B missing-reference matrix");
            builder.AppendLine();
            builder.AppendLine("Assembly version and public-key token are `null` because P5A Portable-PDB reference provenance does not serialize them. Names, TFMs, package coordinates, and versions remain discovery hints; unchanged P5 validation alone establishes LocalExact or RemoteExact.");
            foreach (IGrouping<string, G4BReferenceMatrixEntry> library in entries.GroupBy(
                         static entry => entry.Library,
                         StringComparer.Ordinal))
            {
                builder.AppendLine();
                builder.AppendLine($"## {library.Key}");
                builder.AppendLine();
                builder.AppendLine("| Ordinal | Reference | Kind | MVID | Timestamp | SizeOfImage | Local | Class | Hints/providers | Final | Remote artifact | Entry |");
                builder.AppendLine("| ---: | --- | --- | --- | ---: | ---: | --- | --- | --- | --- | --- | --- |");
                foreach (G4BReferenceMatrixEntry entry in library)
                {
                    string hints = string.Join("; ", entry.AvailableDiscoveryHints)
                        + " / " + string.Join(", ", entry.PotentialRemoteProviders);
                    string artifact = entry.RemoteArtifactIdentity == null
                        ? "none"
                        : $"{entry.RemoteArtifactIdentity} {entry.RemoteArtifactVersion}";
                    builder.AppendLine(
                        $"| {entry.Ordinal} | `{entry.Reference}` | {entry.MetadataImageKind} | `{entry.ModuleVersionId}` | {entry.Timestamp} | {entry.SizeOfImage} | {entry.LocalCandidateResult} | {entry.LikelyArtifactClass} | {hints} | {entry.FinalAcquisitionResult} | {artifact} | `{entry.RemoteArchiveEntry ?? "none"}` |");
                }
            }

            return builder.ToString();
        }

        private sealed class G4BReferenceMatrixEntry
        {
            public string Library { get; set; } = string.Empty;
            public string LibraryVersion { get; set; } = string.Empty;
            public string TargetFramework { get; set; } = string.Empty;
            public int Ordinal { get; set; }
            public string Reference { get; set; } = string.Empty;
            public string AssemblySimpleName { get; set; } = string.Empty;
            public string? AssemblyVersion { get; set; }
            public string? PublicKeyToken { get; set; }
            public string MetadataImageKind { get; set; } = string.Empty;
            public Guid ModuleVersionId { get; set; }
            public int Timestamp { get; set; }
            public int SizeOfImage { get; set; }
            public List<string> Aliases { get; set; } = new();
            public bool EmbedInteropTypes { get; set; }
            public string LocalCandidateResult { get; set; } = string.Empty;
            public string LikelyArtifactClass { get; set; } = string.Empty;
            public List<string> AvailableDiscoveryHints { get; set; } = new();
            public List<string> PotentialRemoteProviders { get; set; } = new();
            public string FinalAcquisitionResult { get; set; } = string.Empty;
            public string? RemoteProvider { get; set; }
            public string? RemoteArtifactIdentity { get; set; }
            public string? RemoteArtifactVersion { get; set; }
            public string? RemoteArtifactSha512 { get; set; }
            public string? RemoteArchiveEntry { get; set; }
        }
    }
}
