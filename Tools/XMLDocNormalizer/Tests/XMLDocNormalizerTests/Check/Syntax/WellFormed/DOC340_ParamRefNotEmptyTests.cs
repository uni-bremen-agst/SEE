using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Syntax.WellFormed
{
    /// <summary>
    /// Rule tests for DOC340: paramref should be an empty element.
    /// </summary>
    public sealed class DOC340_ParamRefNotEmptyTests
    {
        /// <summary>
        /// Ensures that a non-empty paramref-tag is detected.
        /// </summary>
        [Fact]
        public void Malformed_Paramref_IsDetected()
        {
            string source =
                "/// <summary>Test</summary>\n" +
                "/// <returns><paramref name=\"x\"> equals 1</returns>\n" +
                "int M(int x) { return x; }\n";

            List<Finding> findings = CheckAssert.FindWellFormedFindingsForMember(source);

            Finding finding = Assert.Single(findings);
            Assert.Equal("paramref", finding.TagName);
            Assert.Equal(XmlDocSmells.ParamRefNotEmpty.Id, finding.Smell.Id);
        }

        /// <summary>
        /// Ensures that multiple non-empty paramref-tags are detected individually.
        /// </summary>
        [Fact]
        public void Malformed_Multiple_Paramref_IsDetected()
        {
            string source =
                "/// <summary>Test</summary>\n" +
                "/// <returns><paramref name=\"x\"> equals 1.\n" +
                "/// and <paramref name=\"y\"> equals 0.</returns>\n" +
                "int M(int x) { return x; }\n";

            List<Finding> findings = CheckAssert.FindWellFormedFindingsForMember(source);

            FindingAsserts.ContainsSmellTimes(findings, XmlDocSmells.ParamRefNotEmpty.Id, 2);
        }
    }
}
