using System;
using System.Collections.Generic;
using SEE.DataModel.DG.IO;

namespace SEE.GraphProviders
{
    /// <summary>
    /// Integration-style tests for parsing an MSBuild (C# Compiler) text report and applying the 
    /// resulting metrics to a graph.
    ///
    /// This test class verifies that:
    /// - The configured parser (<see cref="MSBuildParsingConfig"/>) can read a concrete MSBuild log file.
    /// - The regex patterns correctly extract file paths, line numbers, and error details.
    /// - The individual attributes are correctly formatted into a single, UI-safe <c>ContextLevel.Issue</c> metric string.
    ///
    /// The test data is based on a real-world `errors.log`. The test focuses exclusively on 
    /// actual syntax errors (<c>CS1002</c>) found within the <c>Assets/SEE</c> directory, 
    /// ignoring noise from system assemblies.
    ///
    /// Preconditions:
    /// - The referenced report and graph files must exist under <c>Application.streamingAssetsPath</c> at the
    ///   paths returned by <see cref="GetRelativeReportPath"/> and <see cref="GetRelativeGlxPath"/>.
    /// </summary>
    internal class TestMSBuildReport : TestReportGraphProviderBase
    {
        /// <summary>
        /// Returns the relative path (under <c>Application.streamingAssetsPath</c>) to the MSBuild text report
        /// used by this test.
        /// </summary>
        /// <returns>Relative file system path to the MSBuild log file.</returns>
        protected override string GetRelativeReportPath()
        {
            return @"\MSBuild\errors.log";
        }

        /// <summary>
        /// Returns the relative path (under <c>Application.streamingAssetsPath</c>) to the GLX graph file
        /// that serves as the target for applying parsed metrics.
        /// </summary>
        /// <returns>Relative file system path to the compressed GLX graph.</returns>
        protected override string GetRelativeGlxPath()
        {
            return @"\MSBuild\MSBuild.glx.xz";
        }

        /// <summary>
        /// Provides the parsing configuration.
        /// 
        /// This uses the updated <see cref="MSBuildParsingConfig"/> which maps all capture groups 
        /// to a single formatted string metric (<c>ContextLevel.Issue</c>).
        /// </summary>
        /// <returns>A <see cref="ParsingConfig"/> instance configured for MSBuild reports.</returns>
        protected override ParsingConfig GetParsingConfig()
        {
            return new MSBuildParsingConfig();
        }

        /// <summary>
        /// Returns the list of context names that should be parsed/considered during this test run.
        /// </summary>
        /// <returns>An array containing the "issue" context.</returns>
        protected override string[] GetNodesToParse()
        {
            return new[] { "issue" };
        }

        /// <summary>
        /// Supplies the expected findings for this test.
        ///
        /// The base class compares the parsed findings against this dictionary. Keys are constructed
        /// using the file path and line number to ensure uniqueness.
        /// </summary>
        /// <returns>Dictionary of expected findings keyed by a composite identifier (Path:Line).</returns>
        protected override Dictionary<string, Finding> GetTestFindings()
        {
            return CreateTestFindings();
        }

        /// <summary>
        /// Creates a curated set of expected findings extracted from the MSBuild report.
        ///
        /// Keying:
        /// The dictionary uses a composite key (<c>FilePath:LineNumber</c>) to uniquely identify findings.
        /// </summary>
        /// <returns>Dictionary of expected findings.</returns>
        private static Dictionary<string, Finding> CreateTestFindings()
        {
            // Shared base prefix for the relevant C# files in the report.
            const string basePath = @"C:\Users\ferri\Documents\GitHub\SEE\Assets\SEE\DataModel\DG\";
            const string projectPath = @"C:\Users\ferri\Documents\GitHub\SEE\SEE.csproj";

            // Common error details for the selected findings.
            const string errorCode = "CS1002";
            const string errorMessage = "; erwartet.";
            const string severity = "error";

            // Use OrdinalIgnoreCase because Windows paths are typically case-insensitive.
            Dictionary<string, Finding> findings = new Dictionary<string, Finding>(StringComparer.OrdinalIgnoreCase);

            // 1) Node.cs at Line 82
            string nodePath = basePath + "Node.cs";
            AddFinding(findings, nodePath, 82, 53, severity, errorCode, errorMessage, projectPath);

            // 2) Graph.cs at Line 81
            string graphPath = basePath + "Graph.cs";
            AddFinding(findings, graphPath, 81, 34, severity, errorCode, errorMessage, projectPath);

            // 3) Graph.cs at Line 100
            AddFinding(findings, graphPath, 100, 32, severity, errorCode, errorMessage, projectPath);

            // 4) Graph.cs at Line 335
            AddFinding(findings, graphPath, 335, 46, severity, errorCode, errorMessage, projectPath);

            return findings;
        }

        /// <summary>
        /// Helper method to create and add a <see cref="Finding"/> to the dictionary with a unique key.
        ///
        /// It formats the metric string to match the template defined in <see cref="MSBuildParsingConfig"/>:
        /// <c>"Line {line}: [{severity}] {code}: {message} ({project})"</c>.
        /// </summary>
        /// <param name="dict">The dictionary to populate.</param>
        /// <param name="filePath">The absolute path to the file.</param>
        /// <param name="line">The line number of the error.</param>
        /// <param name="column">The column number of the error.</param>
        /// <param name="severity">The severity (e.g., "error", "warning").</param>
        /// <param name="code">The compiler error code (e.g., "CS1002").</param>
        /// <param name="message">The human-readable error message.</param>
        /// <param name="project">The project file path causing the error.</param>
        private static void AddFinding(
            Dictionary<string, Finding> dict,
            string filePath,
            int line,
            int column,
            string severity,
            string code,
            string message,
            string project)
        {
            // Composite key to distinguish multiple errors in the same file for test lookup.
            string uniqueKey = $"{filePath}:{line}";

            // Construct the single-line metric value as defined in MSBuildParsingConfig.
            // Starts with "Line " to ensure PropertyWindow treats it as a string, not a list.
            string formattedIssue = $"Line {line}: [{severity}] {code}: {message} ({project})";

            dict[uniqueKey] = new Finding
            {
                FullPath = filePath,
                FileName = filePath,
                Context = "issue",
                Location = new MetricLocation
                {
                    StartLine = line,
                    StartColumn = column
                },
                Metrics = new Dictionary<string, string>
                {
                    // Consolidate all details into one "ContextLevel.Issue" metric.
                    ["ContextLevel.Issue"] = formattedIssue
                }
            };
        }
    }
}
