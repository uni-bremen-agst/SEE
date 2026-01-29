using XMLDocNormalizer.Configuration;
using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Traversal.Syntax
{
    /// <summary>
    /// Traversal tests ensuring that the basic detector analyzes declarations inside nested types.
    /// </summary>
    public sealed class Traversal_NestedTypes_BasicDetectorTests
    {
        /// <summary>
        /// Ensures that all basic smells can be triggered inside nested types and that no additional smells are produced.
        /// </summary>
        [Fact]
        public void NestedTypes_TriggersAllBasicSmells()
        {
            string source =
                "/// <summary>Outer.</summary>\n" +
                "public class Outer\n" +
                "{\n" +
                "    /// <summary>Inner.</summary>\n" +
                "    public class Inner\n" +
                "    {\n" +
                "        public void MissingDoc() { }\n" + // DOC100
                "\n" +
                "        /// <remarks>No summary.</remarks>\n" +
                "        public void MissingSummary() { }\n" + // DOC200
                "\n" +
                "        /// <summary>\n" +
                "        /// \n" +
                "        /// </summary>\n" +
                "        public void EmptySummary() { }\n" + // DOC210
                "    }\n" +
                "}\n";

            XmlDocOptions options = new XmlDocOptions
            {
                CheckEnumMembers = false,
                RequireSummaryForFields = false
            };

            List<Finding> findings = CheckAssert.FindBasicFindingsForSource(source, options);

            // Exactly these three smells, order-independent.
            FindingAsserts.HasExactlySmells(
                findings,
                XmlDocSmells.MissingDocumentation.Id,
                XmlDocSmells.MissingSummary.Id,
                XmlDocSmells.EmptySummary.Id);

            FindingAsserts.ContainsSmellTimes(findings, XmlDocSmells.MissingDocumentation.Id, 1);
            FindingAsserts.ContainsSmellTimes(findings, XmlDocSmells.MissingSummary.Id, 1);
            FindingAsserts.ContainsSmellTimes(findings, XmlDocSmells.EmptySummary.Id, 1);
        }

        /// <summary>
        /// Ensures that nested types with valid documentation produce no basic findings.
        /// </summary>
        [Fact]
        public void NestedTypes_ProducesNoBasicFindings()
        {
            string source =
                "/// <summary>Outer.</summary>\n" +
                "public class Outer\n" +
                "{\n" +
                "    /// <summary>Inner.</summary>\n" +
                "    public class Inner\n" +
                "    {\n" +
                "        /// <summary>Ok.</summary>\n" +
                "        public void M() { }\n" +
                "    }\n" +
                "}\n";

            XmlDocOptions options = new XmlDocOptions
            {
                CheckEnumMembers = false,
                RequireSummaryForFields = false
            };

            List<Finding> findings = CheckAssert.FindBasicFindingsForSource(source, options);

            Assert.Empty(findings);
        }
    }
}
