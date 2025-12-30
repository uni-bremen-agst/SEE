using System.Collections.Generic;
using System.Globalization;
using SEE.DataModel.DG.IO;

/// <summary>
/// Providers that load graph data from various sources (files, version control, etc.).
/// </summary>
namespace SEE.GraphProviders
{
    /// <summary>
    /// Tests for NUnit JSON coverage report with concrete expected metrics.
    /// </summary>
    internal class TestNUnitReport : TestReportGraphProviderBase
    {
        /// <inheritdoc/>
        protected override string GetRelativeReportPath()
        {
            return "/NUnit/Summary.json";
        }

        /// <inheritdoc/>
        protected override string GetRelativeGlxPath()
        {
            return "/NUnit/NUnit.glx.xz";
        }

        /// <inheritdoc/>
        protected override ParsingConfig GetParsingConfig()
        {
            return new NUnitJsonParsingConfig();
        }

        /// <summary>
        /// All NUnit contexts that this test verifies.
        /// </summary>
        private static readonly string[] parsedNodes = { "class" };

        /// <inheritdoc/>
        protected override string[] GetNodesToParse()
        {
            return parsedNodes;
        }

        /// <inheritdoc/>
        protected override Dictionary<string, Finding> GetTestFindings()
        {
            return CreateTestFindings();
        }

        /// <summary>
        /// Helper to create the metrics dictionary matching NUnitJsonParsingConfig keys.
        /// </summary>
        /// <param name="lineCoverage">The percentage of lines covered.</param>
        /// <param name="coveredLines">The number of covered lines.</param>
        /// <param name="totalLines">The total number of coverable lines.</param>
        /// <param name="loc">The total lines of code.</param>
        /// <param name="methodCoverage">The percentage of methods covered.</param>
        /// <param name="coveredMethods">The number of covered methods.</param>
        /// <param name="totalMethods">The total number of methods.</param>
        /// <param name="branchCoverage">The percentage of branches covered (optional, nullable).</param>
        /// <param name="coveredBranches">The number of covered branches.</param>
        /// <param name="totalBranches">The total number of branches.</param>
        /// <returns>A dictionary containing the metric keys and their string values.</returns>
        private static Dictionary<string, string> BuildMetrics(
            double lineCoverage, int coveredLines, int totalLines, int loc,
            double methodCoverage, int coveredMethods, int totalMethods,
            double? branchCoverage = null, int coveredBranches = 0, int totalBranches = 0)
        {
            Dictionary<string, string> metrics = new Dictionary<string, string>();

            // --- Lines ---
            metrics["LINES.COVERAGE"] = lineCoverage.ToString(CultureInfo.InvariantCulture);
            metrics["LINES.COVERED"] = coveredLines.ToString(CultureInfo.InvariantCulture);
            metrics["LINES.TOTAL"] = totalLines.ToString(CultureInfo.InvariantCulture);
            metrics["LINES.LOC"] = loc.ToString(CultureInfo.InvariantCulture);

            // --- Methods ---
            metrics["METHOD.COVERAGE"] = methodCoverage.ToString(CultureInfo.InvariantCulture);
            metrics["METHOD.COVERED"] = coveredMethods.ToString(CultureInfo.InvariantCulture);
            metrics["METHOD.TOTAL"] = totalMethods.ToString(CultureInfo.InvariantCulture);

            // --- Branches ---
            // If branchCoverage is null (because it was null/empty in the JSON),
            // we do NOT expect the key, as the parser skips null values.
            if (branchCoverage.HasValue)
            {
                metrics["BRANCH.COVERAGE"] = branchCoverage.Value.ToString(CultureInfo.InvariantCulture);
            }

            // The absolute numbers are usually 0 and not null in the JSON, so we expect them.
            metrics["BRANCH.COVERED"] = coveredBranches.ToString(CultureInfo.InvariantCulture);
            metrics["BRANCH.TOTAL"] = totalBranches.ToString(CultureInfo.InvariantCulture);

            return metrics;
        }

        /// <summary>
        /// Creates expected findings based on the provided data.
        /// </summary>
        /// <returns>A dictionary of findings keyed by their full path.</returns>
        private static Dictionary<string, Finding> CreateTestFindings()
        {
            // 1. Class: SEE.Audio.AudioGameObject
            Finding audioClassFinding = new Finding
            {
                FullPath = "SEE.Audio.AudioGameObject",
                FileName = string.Empty,
                Context = "class",
                Metrics = BuildMetrics(
                    lineCoverage: 0,
                    coveredLines: 0,
                    totalLines: 56,
                    loc: 143,
                    methodCoverage: 0,
                    coveredMethods: 0,
                    totalMethods: 10,
                    branchCoverage: null // Null/empty in JSON since totalbranches=0
                )
            };

            // 2. Class: SEE.Controls.Actions.AbstractActionStateType
            Finding actionStateFinding = new Finding
            {
                FullPath = "SEE.Controls.Actions.AbstractActionStateType",
                FileName = string.Empty,
                Context = "class",
                Metrics = BuildMetrics(
                    lineCoverage: 76.4,
                    coveredLines: 13,
                    totalLines: 17,
                    loc: 68,
                    methodCoverage: 85.7,
                    coveredMethods: 6,
                    totalMethods: 7,
                    branchCoverage: null
                )
            };

            // 3. Class: SEE.Controls.Actions.ActionStateType
            Finding actionStateTypeFinding = new Finding
            {
                FullPath = "SEE.Controls.Actions.ActionStateType",
                FileName = string.Empty,
                Context = "class",
                Metrics = BuildMetrics(
                    lineCoverage: 100,
                    coveredLines: 17,
                    totalLines: 17,
                    loc: 62,
                    methodCoverage: 100,
                    coveredMethods: 4,
                    totalMethods: 4,
                    branchCoverage: null
                )
            };

            return new Dictionary<string, Finding>
            {
                { audioClassFinding.FullPath, audioClassFinding },
                { actionStateFinding.FullPath, actionStateFinding },
                { actionStateTypeFinding.FullPath, actionStateTypeFinding }
            };
        }
    }
}