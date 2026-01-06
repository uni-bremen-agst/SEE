using System.Collections.Generic;

/// <summary>
/// Contains types for parsing external tool reports and applying their metrics to SEE dependency graphs.
/// </summary>
namespace SEE.DataModel.DG.IO
{
    /// <summary>
    /// Parsing configuration for JaCoCo XML reports.
    /// </summary>
    /// <remarks>Preconditions: An instance must be used only with JaCoCo-compatible XML input.</remarks>
    internal sealed class JaCoCoParsingConfig : XmlParsingConfig
    {
        /// <summary>
        /// Context name for JaCoCo method nodes (<method>).
        /// </summary>
        private const string methodContext = "method";

        /// <summary>
        /// Context name for JaCoCo class nodes (<class>).
        /// </summary>
        private const string classContext = "class";

        /// <summary>
        /// Context name for JaCoCo package nodes (<package>).
        /// </summary>
        private const string packageContext = "package";

        /// <summary>
        /// Metric definitions shared across all supported JaCoCo contexts (package, class, method).
        /// </summary>
        /// <remarks>
        /// Keys are exported metric names and values are XPath expressions evaluated on the corresponding node.
        /// </remarks>
        private readonly Dictionary<string, string> metrics = new Dictionary<string, string>
        {
            ["INSTRUCTION_missed"] = "number(counter[@type='INSTRUCTION']/@missed)",
            ["INSTRUCTION_covered"] = "number(counter[@type='INSTRUCTION']/@covered)",
            ["BRANCH_missed"] = "number(counter[@type='BRANCH']/@missed)",
            ["BRANCH_covered"] = "number(counter[@type='BRANCH']/@covered)",
            ["LINE_missed"] = "number(counter[@type='LINE']/@missed)",
            ["LINE_covered"] = "number(counter[@type='LINE']/@covered)",
            ["COMPLEXITY_missed"] = "number(counter[@type='COMPLEXITY']/@missed)",
            ["COMPLEXITY_covered"] = "number(counter[@type='COMPLEXITY']/@covered)",
            ["CLASS_missed"] = "number(counter[@type='CLASS']/@missed)",
            ["CLASS_covered"] = "number(counter[@type='CLASS']/@covered)",
            ["METHOD_missed"] = "number(counter[@type='METHOD']/@missed)",
            ["METHOD_covered"] = "number(counter[@type='METHOD']/@covered)",

            ["INSTRUCTION_percentage"] =
                "round(100 * number(counter[@type='INSTRUCTION']/@covered) div " +
                "(number(counter[@type='INSTRUCTION']/@covered) + number(counter[@type='INSTRUCTION']/@missed)))",

            ["BRANCH_percentage"] =
                "round(100 * number(counter[@type='BRANCH']/@covered) div " +
                "(number(counter[@type='BRANCH']/@covered) + number(counter[@type='BRANCH']/@missed)))",

            ["LINE_percentage"] =
                "round(100 * number(counter[@type='LINE']/@covered) div " +
                "(number(counter[@type='LINE']/@covered) + number(counter[@type='LINE']/@missed)))",

            ["COMPLEXITY_percentage"] =
                "round(100 * number(counter[@type='COMPLEXITY']/@covered) div " +
                "(number(counter[@type='COMPLEXITY']/@covered) + number(counter[@type='COMPLEXITY']/@missed)))",

            ["CLASS_percentage"] =
                "round(100 * number(counter[@type='CLASS']/@covered) div " +
                "(number(counter[@type='CLASS']/@covered) + number(counter[@type='CLASS']/@missed)))",

            ["METHOD_percentage"] =
                "round(100 * number(counter[@type='METHOD']/@covered) div " +
                "(number(counter[@type='METHOD']/@covered) + number(counter[@type='METHOD']/@missed)))"
        };

        /// <summary>
        /// Initializes a new instance of the <see cref="JaCoCoParsingConfig"/> class.
        /// </summary>
        /// <remarks>Preconditions: Must be called before this instance is used to create parsers.</remarks>
        public JaCoCoParsingConfig()
        {
            ToolId = "JaCoCo";

            XPathMapping = new XPathMapping
            {
                SearchedNodes = $"//{packageContext}|//{classContext}|//{methodContext}",

                PathBuilders = new Dictionary<string, string>
                {
                    [packageContext] = "string(@name)",
                    [classContext] = "string(@name)",
                    [methodContext] = "concat(string(ancestor::class/@name), '#', string(@name))"
                },

                FileName = new Dictionary<string, string>
                {
                    [methodContext] = "string(ancestor::class/@sourcefilename)",
                    [classContext] = "string(@sourcefilename)",
                    [packageContext] = "string(@name)"
                },

                LocationMapping = new Dictionary<string, string>
                {
                    ["StartLine"] = "string(@line)"
                },

                MetricsByContext = new Dictionary<string, Dictionary<string, string>>
                {
                    [packageContext] = metrics,
                    [classContext] = metrics,
                    [methodContext] = metrics
                }
            };
        }

        /// <summary>
        /// Creates the index node strategy for Java sources associated with JaCoCo reports.
        /// </summary>
        /// <remarks>Preconditions: Must be used only for Java projects whose coverage is reported by JaCoCo.</remarks>
        /// <returns>An index node strategy for Java files.</returns>
        public override IIndexNodeStrategy CreateIndexNodeStrategy()
        {
            return new JavaIndexNodeStrategy(this);
        }
    }
}
