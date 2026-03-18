using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Syntax.See
{
    /// <summary>
    /// Verifies detection of DOC941 (invalid href on <seealso>).
    /// </summary>
    public sealed class DOC941_InvalidSeeAlsoHrefTests
    {
        /// <summary>
        /// Provides <seealso> declarations with invalid href values.
        /// </summary>
        /// <returns>Member snippets containing invalid <seealso> href values.</returns>
        public static IEnumerable<object[]> DeclarationSources()
        {
            yield return new object[]
            {
                "/// <summary>Test.</summary>\n" +
                "/// <seealso href=\"not-a-url\" />\n" +
                "public void M() { }\n"
            };

            yield return new object[]
            {
                "/// <summary>Test.</summary>\n" +
                "/// <seealso href=\"\" />\n" +
                "public void M() { }\n"
            };

            yield return new object[]
            {
                "/// <summary>Test.</summary>\n" +
                "/// <seealso href=\"example.com\" />\n" +
                "public void M() { }\n"
            };
        }

        /// <summary>
        /// Ensures DOC941 is reported when a <seealso> tag contains an invalid href value.
        /// </summary>
        /// <param name="memberCode">The member snippet to analyze.</param>
        [Theory]
        [MemberData(nameof(DeclarationSources))]
        public void SeeAlsoWithInvalidHref_IsDetected(string memberCode)
        {
            List<Finding> findings = CheckAssert.FindSeeFindingsForMember(memberCode);

            Assert.Contains(
                findings,
                static finding => finding.Smell.ID == XmlDocSmells.InvalidSeeAlsoHref.ID);
        }
    }
}
