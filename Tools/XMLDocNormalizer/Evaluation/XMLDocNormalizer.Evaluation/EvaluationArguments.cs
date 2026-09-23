using XMLDocNormalizer.Execution.Semantic;

namespace XMLDocNormalizer.Evaluation
{
    /// <summary>
    /// Stores validated paths and Source Link policy for one evaluation run.
    /// </summary>
    internal sealed record EvaluationArguments(
        string ManifestPath,
        string WorkspacePath,
        string OutputDirectory,
        bool SourceLinkEnabled,
        ExternalSourceReconstructionPolicy SourceReconstructionPolicy)
    {
        /// <summary>
        /// Parses the small explicit evaluation command line.
        /// </summary>
        /// <param name="args">The raw command-line arguments.</param>
        /// <param name="options">The parsed options when valid.</param>
        /// <returns><see langword="true"/> when all arguments are valid.</returns>
        public static bool TryParse(string[] args, out EvaluationArguments options)
        {
            Dictionary<string, string> values = new(StringComparer.OrdinalIgnoreCase);

            if (args.Length % 2 != 0)
            {
                options = null!;
                return false;
            }

            for (int index = 0; index < args.Length; index += 2)
            {
                if (!args[index].StartsWith("--", StringComparison.Ordinal)
                    || !values.TryAdd(args[index], args[index + 1]))
                {
                    options = null!;
                    return false;
                }
            }

            if (!values.TryGetValue("--manifest", out string? manifestPath)
                || !values.TryGetValue("--workspace", out string? workspacePath)
                || !values.TryGetValue("--output", out string? outputDirectory))
            {
                options = null!;
                return false;
            }

            string policy = values.GetValueOrDefault("--source-link", "enabled");
            string reconstruction = values.GetValueOrDefault(
                "--source-reconstruction",
                "strict");

            if (policy is not ("enabled" or "disabled")
                || reconstruction is not ("strict" or "verified-line-endings")
                || values.Keys.Any(static key => key is not (
                    "--manifest" or "--workspace" or "--output" or "--source-link"
                    or "--source-reconstruction")))
            {
                options = null!;
                return false;
            }

            options = new EvaluationArguments(
                Path.GetFullPath(manifestPath),
                Path.GetFullPath(workspacePath),
                Path.GetFullPath(outputDirectory),
                string.Equals(policy, "enabled", StringComparison.Ordinal),
                reconstruction == "verified-line-endings"
                    ? ExternalSourceReconstructionPolicy.VerifiedLineEndings
                    : ExternalSourceReconstructionPolicy.Strict);
            return true;
        }
    }
}
