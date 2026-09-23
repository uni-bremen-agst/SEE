namespace XMLDocNormalizer.Evaluation
{
    /// <summary>
    /// Runs the opt-in real-world external-source evaluation.
    /// </summary>
    internal static class Program
    {
        /// <summary>
        /// Loads a prepared workspace, evaluates every candidate, and writes both reports.
        /// </summary>
        /// <param name="args">Manifest, workspace, and output arguments.</param>
        /// <returns>Zero when all observed outcomes match the manifest; otherwise one.</returns>
        public static int Main(string[] args)
        {
            if (!EvaluationArguments.TryParse(args, out EvaluationArguments options))
            {
                Console.Error.WriteLine(
                    "Usage: --manifest <path> --workspace <path> --output <directory> "
                    + "[--source-link enabled|disabled] "
                    + "[--source-reconstruction strict|verified-line-endings] "
                    + "[--references local|bounded-remote]");
                return 1;
            }

            try
            {
                EvaluationManifest manifest = EvaluationManifestLoader.Load(options.ManifestPath);
                RealWorldEvaluationRunner runner = new(manifest, options);
                EvaluationReport report = runner.Run();
                EvaluationReportWriter.Write(options.OutputDirectory, report);
                G4BReferenceMatrixWriter.Write(
                    options.OutputDirectory,
                    manifest,
                    report);
                G5SigningMatrixWriter.Write(options.OutputDirectory, report);
                Console.WriteLine(Path.Combine(options.OutputDirectory, "real-world-evaluation.json"));
                Console.WriteLine(Path.Combine(options.OutputDirectory, "real-world-evaluation.md"));
                return report.Summary.UnexpectedFailures == 0
                    && report.Summary.PotentialBugs == 0
                    ? 0
                    : 1;
            }
            catch (Exception exception) when (exception is ArgumentException
                or IOException
                or InvalidOperationException
                or UnauthorizedAccessException)
            {
                Console.Error.WriteLine(exception.Message);
                return 1;
            }
        }
    }
}
