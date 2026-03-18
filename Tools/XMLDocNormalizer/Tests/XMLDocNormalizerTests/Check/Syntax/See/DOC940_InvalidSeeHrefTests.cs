using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Syntax.See
{
    /// <summary>
    /// Verifies detection of DOC940 (invalid href on <see>).
    /// </summary>
    public sealed class DOC940_InvalidSeeHrefTests
    {
        /// <summary>
        /// Provides <see> declarations with invalid href values.
        /// </summary>
        /// <returns>Member snippets containing invalid <see> href values.</returns>
        public static IEnumerable<object[]> DeclarationSources()
        {
            yield return new object[]
            {
                "/// <summary><see href=\"not-a-url\" /></summary>\n" +
                "public void M() { }\n"
            };

            yield return new object[]
            {
                "/// <summary><see href=\"\" /></summary>\n" +
                "public void M() { }\n"
            };

            yield return new object[]
            {
                "/// <summary><see href=\"example.com\" /></summary>\n" +
                "public void M() { }\n"
            };
        }

        /// <summary>
        /// Ensures DOC940 is reported when a <see> tag contains an invalid href value.
        /// </summary>
        /// <param name="memberCode">The member snippet to analyze.</param>
        [Theory]
        [MemberData(nameof(DeclarationSources))]
        public void SeeWithInvalidHref_IsDetected(string memberCode)
        {
            List<Finding> findings = CheckAssert.FindSeeFindingsForMember(memberCode);

            Assert.Contains(
                findings,
                static finding => finding.Smell.ID == XmlDocSmells.InvalidSeeHref.ID);
        }
    }
}
