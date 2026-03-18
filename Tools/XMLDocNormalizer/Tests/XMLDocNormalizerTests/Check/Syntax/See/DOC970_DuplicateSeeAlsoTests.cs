using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Syntax.See
{
    /// <summary>
    /// Verifies detection of DOC970 (duplicate seealso targets).
    /// </summary>
    public sealed class DOC970_DuplicateSeeAlsoTargetTests
    {
        /// <summary>
        /// Ensures DOC970 is reported when two top-level <seealso> tags use the same cref target.
        /// </summary>
        [Fact]
        public void DuplicateSeeAlsoCref_IsDetected()
        {
            string member =
                "/// <summary>Test.</summary>\n" +
                "/// <seealso cref=\"string\" />\n" +
                "/// <seealso cref=\"string\" />\n" +
                "public void M() { }\n";

            List<Finding> findings = CheckAssert.FindSeeFindingsForMember(member);

            Assert.Contains(
                findings,
                static finding => finding.Smell.ID == XmlDocSmells.DuplicateSeeAlsoTarget.ID);
        }

        /// <summary>
        /// Ensures DOC970 is reported when two top-level <seealso> tags use the same href target.
        /// </summary>
        [Fact]
        public void DuplicateSeeAlsoHref_IsDetected()
        {
            string member =
                "/// <summary>Test.</summary>\n" +
                "/// <seealso href=\"https://example.com\" />\n" +
                "/// <seealso href=\"https://example.com\" />\n" +
                "public void M() { }\n";

            List<Finding> findings = CheckAssert.FindSeeFindingsForMember(member);

            Assert.Contains(
                findings,
                static finding => finding.Smell.ID == XmlDocSmells.DuplicateSeeAlsoTarget.ID);
        }

        /// <summary>
        /// Ensures DOC970 is not reported when top-level <seealso> tags use different targets.
        /// </summary>
        [Fact]
        public void DifferentSeeAlsoTargets_DoNotProduceFinding()
        {
            string member =
                "/// <summary>Test.</summary>\n" +
                "/// <seealso cref=\"string\" />\n" +
                "/// <seealso cref=\"int\" />\n" +
                "public void M() { }\n";

            List<Finding> findings = CheckAssert.FindSeeFindingsForMember(member);

            Assert.DoesNotContain(
                findings,
                static finding => finding.Smell.ID == XmlDocSmells.DuplicateSeeAlsoTarget.ID);
        }

        /// <summary>
        /// Ensures DOC970 is not reported for nested <seealso> tags because duplicate detection
        /// only applies to top-level references.
        /// </summary>
        [Fact]
        public void NestedSeeAlsoTargets_AreIgnoredForDuplicateDetection()
        {
            string member =
                "/// <summary><seealso cref=\"string\" /></summary>\n" +
                "/// <seealso cref=\"string\" />\n" +
                "public void M() { }\n";

            List<Finding> findings = CheckAssert.FindSeeFindingsForMember(member);

            int duplicateCount =
                findings.Count(static finding => finding.Smell.ID == XmlDocSmells.DuplicateSeeAlsoTarget.ID);

            Assert.Equal(0, duplicateCount);
        }
    }
}
