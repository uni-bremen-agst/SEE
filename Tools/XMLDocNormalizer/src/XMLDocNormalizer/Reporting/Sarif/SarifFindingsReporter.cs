using XMLDocNormalizer.Models;
using XMLDocNormalizer.Reporting.Abstractions;
using XMLDocNormalizer.Reporting.Sarif.Contract;

namespace XMLDocNormalizer.Reporting.Sarif
{
    /// <summary>
    /// Collects findings and writes a SARIF 2.1.0 report suitable for GitHub code scanning.
    /// </summary>
    internal sealed class SarifFindingsReporter : IResultAwareFindingsReporter
    {
        private readonly string outputPath;
        private readonly List<Finding> findings;

        /// <summary>
        /// Initializes a new instance of the <see cref="SarifFindingsReporter"/> class.
        /// </summary>
        /// <param name="outputPath">Output path where the SARIF report should be written.</param>
        public SarifFindingsReporter(string outputPath)
        {
            if (string.IsNullOrWhiteSpace(outputPath))
            {
                throw new ArgumentException("Output path must not be null or whitespace.", nameof(outputPath));
            }

            this.outputPath = outputPath;
            findings = new List<Finding>();
        }

        /// <inheritdoc/>
        public void ReportFile(string filePath, IReadOnlyList<Finding> findings)
        {
            if (findings == null || findings.Count == 0)
            {
                return;
            }

            this.findings.AddRange(findings);
        }

        /// <inheritdoc/>
        public void Complete()
        {
            Complete(new RunResult());
        }

        /// <summary>
        /// Writes the buffered findings and run metrics to the SARIF output path.
        /// </summary>
        /// <param name="result">The aggregated run result.</param>
        public void Complete(RunResult result)
        {
            SarifLog log = SarifLogBuilder.Build(findings, result);
            SarifReportWriter.Write(outputPath, log);
        }
    }
}
