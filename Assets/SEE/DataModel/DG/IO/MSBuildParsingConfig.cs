using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace SEE.DataModel.DG.IO
{
    /// <summary>
    /// Parsing configuration for MSBuild C# compiler output (warnings and errors).
    /// </summary>
    /// <remarks>
    /// MSBuild compiler output format:
    /// <code>
    /// Path(Line,Column): Severity Code: Message [Project]
    /// </code>
    /// 
    /// Example:
    /// <code>
    /// ...\PlayerController.cs(42,15): warning CS0219: Variable is assigned but never used [...csproj]
    /// </code>
    /// </remarks>
    internal sealed class MSBuildParsingConfig : TextParsingConfig
    {
        /// <summary>
        /// Context name for compiler warnings and errors.
        /// </summary>
        private const string issueContext = "issue";

        /// <summary>
        /// Initializes a new instance of the <see cref="MSBuildParsingConfig"/> class.
        /// </summary>
        public MSBuildParsingConfig()
        {
            ToolId = "MSBuild";

            // For Unity projects, "Assets" is a reliable source root marker.
            SourceRootMarker = "Assets";

            // Regex pattern to capture: file, line, column, severity, code, message, project
            LinePatterns = new Dictionary<string, string>
            {
                [issueContext] = @"^(?<file>.+?)\((?<line>\d+),(?<column>\d+)\):\s+(?<severity>warning|error)\s+(?<code>\w+):\s+(?<message>.+?)(?:\s+\[(?<project>.+?)\])?$"
            };

            PathBuilders = new Dictionary<string, string>
            {
                [issueContext] = "${file}"
            };

            FileNameTemplates = new Dictionary<string, string>
            {
                [issueContext] = "${file}"
            };

            LocationMapping = new Dictionary<string, string>
            {
                ["StartLine"] = "line",
                ["StartColumn"] = "column"
            };

            // Metrics configuration.
            // We consolidate all details into a single string starting with "Line " to avoid 
            // the UI misinterpreting leading brackets "[" as array definitions.
            MetricsByContext = new Dictionary<string, Dictionary<string, string>>
            {
                [issueContext] = new Dictionary<string, string>
                {
                    ["ContextLevel.Issue"] = "Line ${line}: [${severity}] ${code}: ${message} (${project})"
                }
            };

            RegexOptions = RegexOptions.IgnoreCase;

            // Filter to process only lines that look like MSBuild errors/warnings
            LineFilter = @"\(\d+,\d+\):\s+(warning|error)";
        }

        /// <summary>
        /// Creates the index-node strategy for MSBuild reports (using C# strategy).
        /// </summary>
        /// <returns>An index node strategy suitable for C# file paths.</returns>
        public override IIndexNodeStrategy CreateIndexNodeStrategy()
        {
            return new CSharpIndexNodeStrategy(this);
        }
    }
}
