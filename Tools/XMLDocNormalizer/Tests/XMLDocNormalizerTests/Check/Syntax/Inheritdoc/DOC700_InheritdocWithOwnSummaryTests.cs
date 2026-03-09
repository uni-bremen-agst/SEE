using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Syntax.Inheritdoc
{
    /// <summary>
    /// Tests DOC700 – inheritdoc combined with an explicit summary.
    /// </summary>
    public sealed class DOC700_InheritdocWithOwnSummaryTests
    {
        /// <summary>
        /// Ensures that using <c>inheritdoc</c> together with an explicit
        /// <c>summary</c> triggers DOC700.
        /// </summary>
        [Fact]
        public void InheritdocWithOwnSummary_IsDetected()
        {
            string member =
                "/// <inheritdoc/>\n" +
                "/// <summary>Own summary.</summary>\n" +
                "public override string ToString() => base.ToString();\n";

            List<Finding> findings = CheckAssert.FindInheritdocFindingsForMember(member);

            Finding finding = Assert.Single(findings);
            Assert.Equal(XmlDocSmells.InheritdocWithOwnSummary.ID, finding.Smell.ID);
            Assert.Equal("inheritdoc", finding.TagName);
        }

        /// <summary>
        /// Ensures that documentation containing only <c>inheritdoc</c>
        /// does not trigger DOC700.
        /// </summary>
        [Fact]
        public void InheritdocOnly_DoesNotTriggerFinding()
        {
            string member =
                "/// <inheritdoc/>\n" +
                "public override string ToString() => base.ToString();\n";

            List<Finding> findings = CheckAssert.FindInheritdocFindingsForMember(member);

            Assert.Empty(findings);
        }

        /// <summary>
        /// Ensures that documentation containing an explicit summary
        /// but no <c>inheritdoc</c> does not trigger DOC700.
        /// </summary>
        [Fact]
        public void SummaryWithoutInheritdoc_DoesNotTriggerFinding()
        {
            string member =
                "/// <summary>Own summary.</summary>\n" +
                "public override string ToString() => base.ToString();\n";

            List<Finding> findings = CheckAssert.FindInheritdocFindingsForMember(member);

            Assert.Empty(findings);
        }
    }
}