using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Syntax.See
{
    /// <summary>
    /// Verifies detection of DOC911 (invalid target combinations on <seealso>).
    /// </summary>
    public sealed class DOC911_InvalidSeeAlsoAttributeCombinationTests
    {
        /// <summary>
        /// Provides <seealso> declarations that combine multiple mutually exclusive target attributes.
        /// </summary>
        /// <returns>Member snippets containing invalid <seealso> combinations.</returns>
        public static IEnumerable<object[]> DeclarationSources()
        {
            yield return new object[]
            {
                "/// <summary>Test.</summary>\n" +
                "/// <seealso cref=\"string\" href=\"https://example.com\" />\n" +
                "public void M() { }\n"
            };
        }

        /// <summary>
        /// Ensures DOC911 is reported when a <seealso> tag combines multiple target attributes.
        /// </summary>
        /// <param name="memberCode">The member snippet to analyze.</param>
        [Theory]
        [MemberData(nameof(DeclarationSources))]
        public void SeeAlsoWithMultipleTargets_IsDetected(string memberCode)
        {
            List<Finding> findings = CheckAssert.FindSeeFindingsForMember(memberCode);

            FindingAsserts.HasExactlySmells(findings, XmlDocSmells.InvalidSeeAlsoAttributeCombination.ID);

            Finding finding = findings.Single();
            Assert.Equal("seealso", finding.TagName);
        }
    }
}
