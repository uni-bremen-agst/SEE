using System.Collections.Generic;

namespace SEE.DataModel.DG.IO
{
    /// <summary>
    /// Provides XPath expressions and normalization helpers tailored to JaCoCo XML reports.
    /// Preconditions: An instance must be used only with JaCoCo-compatible XML input.
    /// </summary>
    internal class JaCoCoParsingConfig : ParsingConfig
    {
        /// <summary>
        /// Initializes the XPath mapping that knows how to interpret JaCoCo report nodes.
        /// Preconditions: Must be called before this instance is used to create parsers.
        /// </summary>
        public JaCoCoParsingConfig()
        {
            ToolId = "JaCoCo";

            XPathMapping = new XPathMapping
            {
                SearchedNodes = "//package|//class|//method",

                PathBuilders = new Dictionary<string, string>
                {
                    ["package"] = "string(@name)",
                    ["class"] = "string(@name)",
                    ["method"] = "concat(string(ancestor::class/@name), '#', string(@name))"
                },

                FileName = new Dictionary<string, string>
                {
                    ["method"] = "string(ancestor::class/@sourcefilename)",
                    ["class"] = "string(@sourcefilename)",
                    ["package"] = "string(@name)"
                },

                LocationMapping = new Dictionary<string, string>
                {
                    ["StartLine"] = "string(@line)"
                },

                Metrics = new Dictionary<string, string>
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
                },

                MapContext = new Dictionary<string, string>
                {
                    ["package"] = "package",
                    ["class"] = "class",
                    ["method"] = "method"
                }
            };
        }

        /// <summary>
        /// Creates an <see cref="XmlReportParser"/> configured for JaCoCo input.
        /// Preconditions: <see cref="XPathMapping"/> and <see cref="ToolId"/> must be initialized.
        /// </summary>
        /// <returns>An <see cref="IReportParser"/> instance for JaCoCo XML reports.</returns>
        internal override IReportParser CreateParser()
        {
            return new XmlReportParser(this);
        }

        /// <summary>
        /// Creates the index node strategy for Java sources associated with JaCoCo reports.
        /// Preconditions: Must be used only for Java projects whose coverage is reported by JaCoCo.
        /// </summary>
        /// <returns>An index node strategy for Java files.</returns>
        public override IIndexNodeStrategy CreateIndexNodeStrategy()
        {
            return new JaCoCoIndexNodeStrategy();
        }
    }
}
