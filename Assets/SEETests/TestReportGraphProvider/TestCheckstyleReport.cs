using System;
using System.Collections.Generic;
using System.Globalization;
using SEE.DataModel.DG.IO;

namespace SEE.GraphProviders
{
    /// <summary>
    /// Integration-style tests for parsing a Checkstyle XML report and applying the resulting metrics to a graph.
    ///
    /// This test class verifies that:
    /// - The configured parser (<see cref="CheckstyleParsingConfig"/>) can read a concrete Checkstyle report.
    /// - The graph provider pipeline associates findings with the expected file/type nodes.
    /// - Aggregated, file-level metrics match the expected counts for a small, representative selection of files.
    ///
    /// The test data is based on a specific Checkstyle report file and a corresponding GLX graph file.
    /// The file findings are treated as "aggregated" (context <c>file</c>), while a single example "error"
    /// finding is included to validate location-based parsing and message construction.
    ///
    /// Preconditions:
    /// - The referenced report and graph files must exist under <c>Application.streamingAssetsPath</c> at the
    ///   paths returned by <see cref="GetRelativeReportPath"/> and <see cref="GetRelativeGlxPath"/>.
    /// - The GLX graph must contain nodes that can be resolved by <see cref="CheckstyleIndexNodeStrategy"/>
    ///   (i.e., file/type identifiers that correspond to the normalized FQCNs derived from the report paths).
    /// </summary>
    internal class TestCheckstyleReport : TestReportGraphProviderBase
    {
        /// <summary>
        /// Returns the relative path (under <c>Application.streamingAssetsPath</c>) to the Checkstyle XML report
        /// used by this test.
        ///
        /// Notes:
        /// - The base class is expected to combine this with <c>Application.streamingAssetsPath</c>.
        /// - The string is returned exactly as expected by the test infrastructure (including leading separators).
        /// </summary>
        /// <returns>Relative file system path to the Checkstyle report XML.</returns>
        protected override string GetRelativeReportPath()
        {
            // Path relative to Application.streamingAssetsPath:
            // Place your checkstyle-result.xml there.
            return @"\checkstyle\checkstyle-result.xml";
        }

        /// <summary>
        /// Returns the relative path (under <c>Application.streamingAssetsPath</c>) to the GLX graph file
        /// that serves as the target for applying parsed metrics.
        ///
        /// Preconditions:
        /// - The file must exist and be readable by the graph loader used in the base class.
        /// </summary>
        /// <returns>Relative file system path to the compressed GLX graph.</returns>
        protected override string GetRelativeGlxPath()
        {
            // Path to the GXL/GLX graph used in this test.
            return @"\checkstyle\checkstyle.glx.xz";
        }

        /// <summary>
        /// Provides the parsing configuration that defines:
        /// - Which XML nodes to search (<c>file</c>, <c>error</c>),
        /// - How to build paths and locations,
        /// - Which aggregated metrics to compute.
        ///
        /// This test uses <see cref="CheckstyleParsingConfig"/> which is specialized for Checkstyle XML structure.
        /// </summary>
        /// <returns>A <see cref="ParsingConfig"/> instance configured for Checkstyle reports.</returns>
        protected override ParsingConfig GetParsingConfig()
        {
            return new CheckstyleParsingConfig();
        }

        /// <summary>
        /// List of XML node names that the parser should consider when scanning the report.
        ///
        /// For Checkstyle, we typically care about:
        /// - <c>file</c>: the outer container for violations per file (used for aggregated metrics)
        /// - <c>error</c>: individual violations (used for optional per-issue context)
        ///
        /// Note:
        /// The base class may use this list to validate parsing coverage or to drive a node counter.
        /// </summary>
        private static readonly string[] parsedNodes = { "file", "error" };

        /// <summary>
        /// Returns the list of XML node names that should be parsed/considered during this test run.
        /// </summary>
        /// <returns>An array of node names relevant for Checkstyle parsing.</returns>
        protected override string[] GetNodesToParse()
        {
            return parsedNodes;
        }

        /// <summary>
        /// Supplies the expected findings for this test.
        ///
        /// The base class compares the parsed findings against this dictionary. Keys are typically
        /// derived from finding paths (and potentially additional discriminators) and values contain
        /// the expected <see cref="Finding"/> objects, including metrics and optional locations.
        /// </summary>
        /// <returns>Dictionary of expected findings keyed by their full path identifier.</returns>
        protected override Dictionary<string, Finding> GetTestFindings()
        {
            return CreateTestFindings();
        }

        /// <summary>
        /// Builds the metric dictionary for a single Checkstyle "file finding".
        ///
        /// This mirrors the aggregated metrics emitted by <see cref="CheckstyleParsingConfig"/> for a <c>&lt;file&gt;</c> node:
        /// - <c>Aggregated.WarningCount</c>
        /// - <c>Aggregated.ErrorCount</c>
        /// - <c>Aggregated.InfoCount</c>
        /// - <c>Aggregated.ViolationCount</c>
        ///
        /// All values are stored as invariant-culture strings because:
        /// - XML parsing produces strings,
        /// - metric appliers often attempt numeric parsing using invariant culture,
        /// - tests should be culture-stable regardless of the system locale.
        /// </summary>
        /// <param name="warningCount">Expected number of warnings for the file.</param>
        /// <param name="errorCount">Expected number of errors for the file.</param>
        /// <param name="infoCount">Expected number of info-level violations for the file.</param>
        /// <returns>A dictionary containing the aggregated metrics as strings.</returns>
        private static Dictionary<string, string> BuildFileMetrics(
            int warningCount,
            int errorCount,
            int infoCount)
        {
            // Total violations are defined as the sum of warnings + errors + infos in this test.
            // This matches the intent of Aggregated.ViolationCount in the parsing configuration.
            int total = warningCount + errorCount + infoCount;

            return new Dictionary<string, string>
            {
                ["Aggregated.WarningCount"] = warningCount.ToString(CultureInfo.InvariantCulture),
                ["Aggregated.ErrorCount"] = errorCount.ToString(CultureInfo.InvariantCulture),
                ["Aggregated.InfoCount"] = infoCount.ToString(CultureInfo.InvariantCulture),
                ["Aggregated.ViolationCount"] = total.ToString(CultureInfo.InvariantCulture)
            };
        }

        /// <summary>
        /// Creates a small, representative set of expected findings extracted from the Checkstyle report.
        ///
        /// Scope:
        /// - Primarily verifies aggregated file findings (context <c>file</c>).
        /// - Includes one additional finding in <c>error</c> context to validate location parsing and issue
        ///   message composition for an individual violation.
        ///
        /// Keying:
        /// The dictionary uses the absolute file path strings as keys (case-insensitive). This matches the
        /// raw paths found in the report and allows the indexing strategy to normalize them later.
        ///
        /// Preconditions:
        /// - The paths used here must correspond to entries in the report under test.
        /// - The GLX graph used in the test must include resolvable nodes for these paths after normalization.
        /// </summary>
        /// <returns>Dictionary of expected findings keyed by their identifying path.</returns>
        private static Dictionary<string, Finding> CreateTestFindings()
        {
            // Shared base prefix for all Java files in the report:
            // C:\Users\ferri\Documents\GitHub\AppointmentBookingBackend\src\main\java\com\medical\services\
            //
            // Note:
            // This is environment-specific test data. The test assumes the report contains absolute Windows paths.
            const string BasePath =
                @"C:\Users\ferri\Documents\GitHub\AppointmentBookingBackend\src\main\java\com\medical\services\";

            // Use OrdinalIgnoreCase because Windows paths are typically case-insensitive
            // and Checkstyle may vary casing depending on environment.
            var findings = new Dictionary<string, Finding>(StringComparer.OrdinalIgnoreCase);

            // 1) CustomUserDetailsService.java – expected: 1 warning, 3 errors
            string customUserDetailsService =
                BasePath + @"auth\CustomUserDetailsService.java";

            findings[customUserDetailsService] = new Finding
            {
                // FullPath is the primary identifier used by the parsing/indexing pipeline.
                FullPath = customUserDetailsService,

                // FileName is populated here with the same string. In some pipelines this might be only the leaf name,
                // but CheckstyleIndexNodeStrategy can normalize from full path when needed.
                FileName = customUserDetailsService,

                // "file" context indicates an aggregated finding representing the whole file.
                Context = "file",

                // No specific location because this finding summarizes the entire file.
                Location = null,

                // Aggregated metrics expected from the parser.
                Metrics = BuildFileMetrics(
                    warningCount: 1,
                    errorCount: 3,
                    infoCount: 0)
            };

            // 2) JwtAuthenticationFilter.java – expected: 4 warnings, 7 errors
            string jwtAuthenticationFilter =
                BasePath + @"auth\JwtAuthenticationFilter.java";

            findings[jwtAuthenticationFilter] = new Finding
            {
                FullPath = jwtAuthenticationFilter,
                FileName = jwtAuthenticationFilter,
                Context = "file",
                Location = null,
                Metrics = BuildFileMetrics(
                    warningCount: 4,
                    errorCount: 7,
                    infoCount: 0)
            };

            // 3) SecurityConfig.java – expected: 5 warnings, 9 errors
            string securityConfig =
                BasePath + @"Configurations\SecurityConfig.java";

            findings[securityConfig] = new Finding
            {
                FullPath = securityConfig,
                FileName = securityConfig,
                Context = "file",
                Location = null,
                Metrics = BuildFileMetrics(
                    warningCount: 5,
                    errorCount: 9,
                    infoCount: 0)
            };

            // 4) Appointment.java – expected: 2 warnings, 6 errors
            string appointmentEntity =
                BasePath + @"Entities\Appointment.java";

            findings[appointmentEntity] = new Finding
            {
                FullPath = appointmentEntity,
                FileName = appointmentEntity,
                Context = "file",
                Location = null,
                Metrics = BuildFileMetrics(
                    warningCount: 2,
                    errorCount: 6,
                    infoCount: 0)
            };

            // 5) JwtUtil.java – expected: 2 warnings, 14 errors
            string jwtUtil =
                BasePath + @"utils\JwtUtil.java";

            findings[jwtUtil] = new Finding
            {
                FullPath = jwtUtil,
                FileName = jwtUtil,
                Context = "file",
                Location = null,
                Metrics = BuildFileMetrics(
                    warningCount: 2,
                    errorCount: 14,
                    infoCount: 0)
            };

            // 6) Example "error" finding in JwtUtil.java at a specific location.
            //
            // This entry is keyed differently (jwtUtil + "method") to avoid overwriting the file-level finding.
            // It demonstrates:
            // - Location mapping (line/column),
            // - The "Context-Level.Issue" metric produced by CheckstyleParsingConfig for an <error> node.
            findings[jwtUtil + "method"] = new Finding
            {
                // The FullPath still points to the file containing the error.
                FullPath = jwtUtil,

                FileName = jwtUtil,

                // "error" context indicates this finding originates from an <error> node (an individual violation).
                Context = "error",

                // Provide a concrete source location as expected from the report attributes.
                Location = new MetricLocation
                {
                    StartLine = 60,
                    StartColumn = 34
                },

                // Minimal per-error metric payload: a human-readable issue summary.
                Metrics = new Dictionary<string, string>
                {
                    ["ContextLevel.Issue"] = "Line 60: [error] Parameter token should be final."
                }
            };

            return findings;
        }
    }
}
