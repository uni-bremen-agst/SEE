using System.Collections.Generic;

namespace SEE.DataModel.DG.IO
{
    /// <summary>
    /// Parsing configuration for OpenCover/ReportGenerator JSON summary reports.
    /// Supports 'class' contexts.
    /// </summary>
    internal sealed class NUnitJsonParsingConfig : JsonParsingConfig
    {
        /// <summary>
        /// The context name for classes.
        /// </summary>
        private const string classContext = "class";

        /// <summary>
        /// Initializes a new instance of the <see cref="NUnitJsonParsingConfig"/> class.
        /// </summary>
        public NUnitJsonParsingConfig()
        {
            ToolId = "NUnit";

            JsonMapping = new JsonPathMapping
            {
                // 1. Definition of contexts and their selectors
                SelectElements = new Dictionary<string, string>
                {
                    // Selects all class objects within all assemblies
                    { classContext, "$.coverage.assemblies[*].classesinassembly[*]" }
                },

                // 2. Definition of ID paths per context
                PathBuilders = new Dictionary<string, string>
                {
                    // Classes have a "name" field (e.g. "AmplifyShaderEditor.About")
                    { classContext, "name" }
                },

                // No file names available in the Summary JSON
                FileName = new Dictionary<string, string>(),

                // 3. Definition of metrics per context
                MetricsByContext = new Dictionary<string, Dictionary<string, string>>
                {
                    {
                        classContext,
                        new Dictionary<string, string>
                        {
                            // --- Lines (Line Coverage) ---
                            { "LINES.COVERAGE", "coverage" },           // %
                            { "LINES.COVERED", "coveredlines" },        // Count
                            { "LINES.TOTAL", "coverablelines" },        // Count (relevant lines)
                            { "LINES.LOC", "totallines" },              // Total lines in file

                            // --- Branches (Branch Coverage) ---
                            { "BRANCH.COVERAGE", "branchcoverage" },    // %
                            { "BRANCH.COVERED", "coveredbranches" },    // Count
                            { "BRANCH.TOTAL", "totalbranches" },        // Count

                            // --- Methods (Method Coverage) ---
                            { "METHOD.COVERAGE", "methodcoverage" },    // %
                            { "METHOD.COVERED", "coveredmethods" },     // Count
                            { "METHOD.TOTAL", "totalmethods" }          // Count
                        }
                    }
                }
            };
        }

        /// <summary>
        /// Creates the index node strategy for C# identifiers.
        /// </summary>
        /// <returns>A new instance of <see cref="CSharpIndexNodeStrategy"/>.</returns>
        public override IIndexNodeStrategy CreateIndexNodeStrategy()
        {
            return new CSharpIndexNodeStrategy(this);
        }
    }
}