using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Syntax.Basic
{
    /// <summary>
    /// Verifies detection of duplicate top-level summary and remarks tags.
    /// </summary>
    public sealed class DOC220_230_DuplicateSummaryAndRemarksTests
    {
        /// <summary>
        /// Ensures that duplicate summary tags are reported.
        /// </summary>
        [Fact]
        public void DuplicateSummaryTags_AreDetected()
        {
            string member =
                "/// <summary>First.</summary>\n" +
                "/// <summary>Second.</summary>\n" +
                "public void M() { }\n";

            List<Finding> findings = CheckAssert.FindBasicFindingsForMember(member);

            Assert.Contains(
                findings,
                static finding => finding.Smell.ID == XmlDocSmells.DuplicateSummaryTag.ID);
        }

        /// <summary>
        /// Ensures that duplicate remarks tags are reported.
        /// </summary>
        [Fact]
        public void DuplicateRemarksTags_AreDetected()
        {
            string member =
                "/// <summary>Test.</summary>\n" +
                "/// <remarks>First.</remarks>\n" +
                "/// <remarks>Second.</remarks>\n" +
                "public void M() { }\n";

            List<Finding> findings = CheckAssert.FindBasicFindingsForMember(member);

            Assert.Contains(
                findings,
                static finding => finding.Smell.ID == XmlDocSmells.DuplicateRemarksTag.ID);
        }
    }
}
