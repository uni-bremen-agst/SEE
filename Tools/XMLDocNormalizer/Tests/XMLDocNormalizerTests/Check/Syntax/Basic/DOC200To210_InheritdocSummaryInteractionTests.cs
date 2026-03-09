using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Syntax.Basic
{
    /// <summary>
    /// Tests the interaction between <c>inheritdoc</c> and summary-related basic documentation smells.
    /// </summary>
    public sealed class DOC200To210_InheritdocSummaryInteractionTests
    {
        /// <summary>
        /// Ensures that a member documented only with <c>inheritdoc</c> does not trigger
        /// the missing-summary smell.
        /// </summary>
        [Fact]
        public void InheritdocWithoutSummary_DoesNotTriggerMissingSummary()
        {
            string member =
                "/// <inheritdoc/>\n" +
                "public override string ToString() => base.ToString();\n";

            List<Finding> findings = CheckAssert.FindBasicFindingsForMember(member);

            Assert.DoesNotContain(findings, f => f.Smell.ID == XmlDocSmells.MissingSummary.ID);
        }

        /// <summary>
        /// Ensures that a member documented with <c>inheritdoc cref=\"...\"</c> does not trigger
        /// the missing-summary smell.
        /// </summary>
        [Fact]
        public void InheritdocCrefWithoutSummary_DoesNotTriggerMissingSummary()
        {
            string member =
                "/// <inheritdoc cref=\"object.ToString\"/>\n" +
                "public override string ToString() => base.ToString();\n";

            List<Finding> findings = CheckAssert.FindBasicFindingsForMember(member);

            Assert.DoesNotContain(findings, f => f.Smell.ID == XmlDocSmells.MissingSummary.ID);
        }

        /// <summary>
        /// Ensures that an explicitly present but empty <c>summary</c> tag still triggers
        /// the empty-summary smell even when <c>inheritdoc</c> is also present.
        /// </summary>
        [Fact]
        public void EmptySummaryWithInheritdoc_StillTriggersEmptySummary()
        {
            string member =
                "/// <summary></summary>\n" +
                "/// <inheritdoc/>\n" +
                "public override string ToString() => base.ToString();\n";

            List<Finding> findings = CheckAssert.FindBasicFindingsForMember(member);

            Assert.Contains(findings, f => f.Smell.ID == XmlDocSmells.EmptySummary.ID);
        }
    }
}