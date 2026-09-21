namespace XMLDocNormalizer.Evaluation
{
    /// <summary>
    /// Stores validated paths and Source Link policy for one evaluation run.
    /// </summary>
    internal sealed record EvaluationArguments(
        string ManifestPath,
        string WorkspacePath,
        string OutputDirectory,
        bool SourceLinkEnabled)
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

            if (policy is not ("enabled" or "disabled")
                || values.Keys.Any(static key => key is not (
                    "--manifest" or "--workspace" or "--output" or "--source-link")))
            {
                options = null!;
                return false;
            }

            options = new EvaluationArguments(
                Path.GetFullPath(manifestPath),
                Path.GetFullPath(workspacePath),
                Path.GetFullPath(outputDirectory),
                string.Equals(policy, "enabled", StringComparison.Ordinal));
            return true;
        }
    }
}
