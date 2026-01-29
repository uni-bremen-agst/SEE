using System.Linq;
using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Semantic
{
    /// <summary>
    /// Traversal tests ensuring that the returns detector analyzes nested types and their members.
    /// </summary>
    public sealed class Traversal_NestedTypes_ReturnsDetectorTests
    {
        /// <summary>
        /// Ensures that a missing <returns> inside a nested type is detected.
        /// </summary>
        [Fact]
        public void MissingReturns_InNestedType_IsDetected()
        {
            string source =
                "public class Outer\n" +
                "{\n" +
                "    public class Inner\n" +
                "    {\n" +
                "        /// <summary>Test</summary>\n" +
                "        public int M() { return 0; }\n" +
                "    }\n" +
                "}\n";

            List<Finding> findings = CheckAssert.FindReturnsFindingsForSource(source);

            FindingAsserts.ContainsSmell(findings, "DOC500");
            Assert.Contains(findings, f => f.Smell.Id == "DOC500" && f.TagName == "returns");
        }

        /// <summary>
        /// Ensures that a correctly documented nested member produces no returns findings.
        /// </summary>
        [Fact]
        public void ValidReturns_InNestedType_ProducesNoFindings()
        {
            string source =
                "public class Outer\n" +
                "{\n" +
                "    public class Inner\n" +
                "    {\n" +
                "        /// <summary>Test</summary>\n" +
                "        /// <returns>Ok</returns>\n" +
                "        public int M() { return 0; }\n" +
                "    }\n" +
                "}\n";

            List<Finding> findings = CheckAssert.FindReturnsFindingsForSource(source);

            Assert.Empty(findings);
        }
    }
}
