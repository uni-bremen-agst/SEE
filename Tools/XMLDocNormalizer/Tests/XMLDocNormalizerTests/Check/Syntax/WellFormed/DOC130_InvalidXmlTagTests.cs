using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.WellFormed
{
    /// <summary>
    /// Rule tests for DOC130: invalid XML documentation tags (syntactically invalid tag name).
    /// </summary>
    public sealed class DOC130_InvalidXmlTagTests
    {
        /// <summary>
        /// Ensures that an empty tag token "&lt;&gt;" is detected as an invalid XML tag.
        /// </summary>
        [Fact]
        public void EmptyTag_IsDetected()
        {
            string source =
                "/// <summary>Test</summary>\n" +
                "/// <>\n" +
                "public int M(int x) { return x; }\n";

            List<Finding> findings = CheckAssert.FindWellFormedFindingsForMember(source);

            FindingAsserts.HasExactlySmells(findings, "DOC130");

            Finding finding = findings.Single();
            Assert.Equal("<invalid-xml-tag>", finding.TagName);
            Assert.Equal(Severity.Error, finding.Smell.Severity);
            Assert.Contains("<>", finding.Message, StringComparison.Ordinal);
        }

        /// <summary>
        /// Ensures that a tag token with an invalid starting character is detected (e.g. "&lt;:&gt;").
        /// </summary>
        [Fact]
        public void InvalidNameStart_IsDetected()
        {
            string source =
                "/// <summary>Test</summary>\n" +
                "/// <:>\n" +
                "public int M(int x) { return x; }\n";

            List<Finding> findings = CheckAssert.FindWellFormedFindingsForMember(source);

            FindingAsserts.HasExactlySmells(findings, "DOC130");

            Finding finding = findings.Single();
            Assert.Equal("<invalid-xml-tag>", finding.TagName);
            Assert.Contains("<:>", finding.Message, StringComparison.Ordinal);
        }

        /// <summary>
        /// Ensures that well-formed, known tags do not produce DOC130 findings.
        /// </summary>
        [Fact]
        public void ValidTags_ProduceNoInvalidTagFindings()
        {
            string source =
                "/// <summary>Ok</summary>\n" +
                "/// <param name=\"x\">Ok</param>\n" +
                "public int M(int x) { return x; }\n";

            List<Finding> findings = CheckAssert.FindWellFormedFindingsForMember(source);

            FindingAsserts.DoesNotContainSmell(findings, "DOC130");
        }
    }
}
