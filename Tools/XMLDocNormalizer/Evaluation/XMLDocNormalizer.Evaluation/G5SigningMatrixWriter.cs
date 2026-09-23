using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace XMLDocNormalizer.Evaluation
{
    /// <summary>Writes the G5 exact signed-target identity and fidelity matrix.</summary>
    internal static class G5SigningMatrixWriter
    {
        /// <summary>Writes machine-readable and Markdown matrices beside the evaluation report.</summary>
        public static void Write(string outputDirectory, EvaluationReport report)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(outputDirectory);
            ArgumentNullException.ThrowIfNull(report);

            List<G5SigningMatrixEntry> entries = report.Candidates
                .Where(static candidate => candidate.Categories.Contains(
                    "SignedTarget",
                    StringComparer.Ordinal))
                .OrderBy(static candidate => candidate.Package, StringComparer.Ordinal)
                .Select(CreateEntry)
                .ToList();
            JsonSerializerOptions options = new()
            {
                WriteIndented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };
            File.WriteAllText(
                Path.Combine(outputDirectory, "signing-matrix.json"),
                JsonSerializer.Serialize(entries, options));
            File.WriteAllText(
                Path.Combine(outputDirectory, "signing-matrix.md"),
                CreateMarkdown(entries));
        }

        private static G5SigningMatrixEntry CreateEntry(
            EvaluationCandidateResult candidate)
        {
            EvaluationSigningResult signing = candidate.Signing;
            EvaluationStageResult? p6a = candidate.Stages.LastOrDefault(
                static stage => stage.Stage == "SupportingSourceRegistered");
            EvaluationStageResult? p6b = candidate.Stages.LastOrDefault(
                static stage => stage.Stage == "SourceBodyUsed");
            return new G5SigningMatrixEntry
            {
                Library = candidate.Package,
                PackageVersion = candidate.Version,
                TargetFramework = candidate.TargetFramework,
                AssemblyName = signing.AssemblyName,
                AssemblyVersion = signing.AssemblyVersion,
                Culture = signing.Culture,
                PublicKey = signing.PublicKey,
                PublicKeySha256 = signing.PublicKeySha256,
                PublicKeyToken = signing.PublicKeyToken,
                AssemblyFlags = signing.AssemblyFlags,
                CorFlags = signing.CorFlags,
                SigningState = signing.TargetState,
                StrongNameSignatureSize = signing.StrongNameSignatureSize,
                StrongNameSignatureSha256 = signing.StrongNameSignatureSha256,
                HasStrongNameSignature = signing.HasStrongNameSignature,
                ReconstructedCryptoPublicKey = signing.ReconstructedCryptoPublicKey,
                CryptoKeyFile = signing.CryptoKeyFile,
                CryptoKeyContainer = signing.CryptoKeyContainer,
                DelaySign = signing.DelaySign,
                PublicSign = signing.PublicSign,
                StrongNameProviderConfigured = signing.StrongNameProviderConfigured,
                OriginalAssemblyIdentity = signing.OriginalAssemblyIdentity,
                ReconstructedAssemblyIdentity = signing.ReconstructedAssemblyIdentity,
                AssemblyIdentityMatches = signing.AssemblyIdentityMatches,
                FidelityResult = signing.FidelityResult,
                PreG5RejectReason = "P5G omitted CryptoPublicKey, so P5K's complete AssemblyIdentity postcondition rejected the unsigned reconstructed identity.",
                ExactlyKnown = "Name, version, culture, complete public key, public-key token, AssemblyFlags, CLI flags, signature-directory size, and signature-byte SHA-256.",
                IntentionallyNotReconstructed = "Historical strong-name signature bytes, private key, CryptoKeyFile, CryptoKeyContainer, StrongNameProvider, DelaySign, PublicSign, and all emit output.",
                SemanticIdentityInputs = "Name, version, culture, full public key, public-key token, retargetability/content type, and resulting complete Roslyn AssemblyIdentity.",
                EmitOnlyInputs = "Private signing key, historical signature bytes, signing key source/provider, DelaySign/PublicSign emit behavior, and emitted PE signature.",
                FinalStage = candidate.Stages.LastOrDefault()?.Stage,
                P6AReached = p6a?.Succeeded == true,
                P6BReached = p6b?.Succeeded == true,
                P6BStatus = !candidate.SourceBodyProbeConfigured
                    ? "NotExercised"
                    : p6b?.Succeeded == true
                        ? "ResolvedToSource"
                        : "Failed",
                SourceBodyUsed = candidate.SemanticModelProbeSucceeded,
                AddedFindings = candidate.AddedFindings,
                RemovedFindings = candidate.RemovedFindings
            };
        }

        private static string CreateMarkdown(
            IReadOnlyCollection<G5SigningMatrixEntry> entries)
        {
            StringBuilder builder = new();
            builder.AppendLine("# G5 signed-target reconstruction matrix");
            builder.AppendLine();
            builder.AppendLine("This matrix distinguishes exact semantic assembly-identity reconstruction from emit signing. G5 never reconstructs or requests a private key and never emits a replacement target binary.");
            builder.AppendLine();
            builder.AppendLine("| Library | Identity | State | Public key SHA-256 | Token | AssemblyFlags | CorFlags | Signature | Reconstructed options | P5L | Final stage | P6A | P6B | Findings | ");
            builder.AppendLine("| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |");
            foreach (G5SigningMatrixEntry entry in entries)
            {
                string identity = $"{entry.AssemblyName}, {entry.AssemblyVersion}, {(entry.Culture.Length == 0 ? "neutral" : entry.Culture)}";
                string signature = $"{entry.StrongNameSignatureSize} bytes; SHA-256 `{entry.StrongNameSignatureSha256}`";
                string options = $"CryptoPublicKey={(entry.ReconstructedCryptoPublicKey?.Length ?? 0) / 2} bytes; KeyFile=null; KeyContainer=null; DelaySign={entry.DelaySign?.ToString() ?? "null"}; PublicSign={entry.PublicSign}; Provider={entry.StrongNameProviderConfigured}";
                string findings = $"+{entry.AddedFindings.Count}/-{entry.RemovedFindings.Count}";
                builder.AppendLine($"| {entry.Library} | {identity} | {entry.SigningState} | `{entry.PublicKeySha256}` | `{entry.PublicKeyToken}` | `{entry.AssemblyFlags}` | `{entry.CorFlags}` | {signature} | {options} | {entry.FidelityResult} | {entry.FinalStage} | {entry.P6AReached} | {entry.P6BStatus} | {findings} |");
                builder.AppendLine();
                builder.AppendLine($"Full public key for {entry.Library}: `{entry.PublicKey}`");
            }

            builder.AppendLine();
            builder.AppendLine("Before G5, P5G omitted `CryptoPublicKey`; consequently P5K reconstructed an unsigned assembly identity and its existing complete-identity postcondition rejected each signed target. G5 reconstructs only the complete public key needed for semantic identity. Historical signature bytes and all private-key/emit inputs remain intentionally unreconstructed.");
            return builder.ToString();
        }

        private sealed class G5SigningMatrixEntry
        {
            public string Library { get; set; } = string.Empty;
            public string PackageVersion { get; set; } = string.Empty;
            public string TargetFramework { get; set; } = string.Empty;
            public string AssemblyName { get; set; } = string.Empty;
            public string AssemblyVersion { get; set; } = string.Empty;
            public string Culture { get; set; } = string.Empty;
            public string PublicKey { get; set; } = string.Empty;
            public string PublicKeySha256 { get; set; } = string.Empty;
            public string PublicKeyToken { get; set; } = string.Empty;
            public string AssemblyFlags { get; set; } = string.Empty;
            public string CorFlags { get; set; } = string.Empty;
            public string SigningState { get; set; } = string.Empty;
            public int StrongNameSignatureSize { get; set; }
            public string StrongNameSignatureSha256 { get; set; } = string.Empty;
            public bool HasStrongNameSignature { get; set; }
            public string? ReconstructedCryptoPublicKey { get; set; }
            public string? CryptoKeyFile { get; set; }
            public string? CryptoKeyContainer { get; set; }
            public bool? DelaySign { get; set; }
            public bool PublicSign { get; set; }
            public bool StrongNameProviderConfigured { get; set; }
            public string OriginalAssemblyIdentity { get; set; } = string.Empty;
            public string? ReconstructedAssemblyIdentity { get; set; }
            public bool? AssemblyIdentityMatches { get; set; }
            public string FidelityResult { get; set; } = string.Empty;
            public string PreG5RejectReason { get; set; } = string.Empty;
            public string ExactlyKnown { get; set; } = string.Empty;
            public string IntentionallyNotReconstructed { get; set; } = string.Empty;
            public string SemanticIdentityInputs { get; set; } = string.Empty;
            public string EmitOnlyInputs { get; set; } = string.Empty;
            public string? FinalStage { get; set; }
            public bool P6AReached { get; set; }
            public bool P6BReached { get; set; }
            public string P6BStatus { get; set; } = string.Empty;
            public bool SourceBodyUsed { get; set; }
            public List<string> AddedFindings { get; set; } = new();
            public List<string> RemovedFindings { get; set; } = new();
        }
    }
}
