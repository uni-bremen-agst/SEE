using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Syntax.See
{
    /// <summary>
    /// Verifies detection of DOC912 (langword is not allowed on <seealso>).
    /// </summary>
    public sealed class DOC912_SeeAlsoLangwordNotAllowedTests
    {
        /// <summary>
        /// Provides <seealso> declarations that incorrectly use the langword attribute.
        /// </summary>
        /// <returns>Member snippets containing invalid <seealso> langword usage.</returns>
        public static IEnumerable<object[]> DeclarationSources()
        {
            yield return new object[]
            {
                "/// <summary>Test.</summary>\n" +
                "/// <seealso langword=\"null\" />\n" +
                "public void M() { }\n"
            };

            yield return new object[]
            {
                "/// <summary>Test.</summary>\n" +
                "/// <seealso cref=\"string\" langword=\"null\" />\n" +
                "public void M() { }\n"
            };
        }

        /// <summary>
        /// Ensures DOC912 is reported when a <seealso> tag uses langword.
        /// </summary>
        /// <param name="memberCode">The member snippet to analyze.</param>
        [Theory]
        [MemberData(nameof(DeclarationSources))]
        public void SeeAlsoWithLangword_IsDetected(string memberCode)
        {
            List<Finding> findings = CheckAssert.FindSeeFindingsForMember(memberCode);

            Assert.Contains(findings, static finding => finding.Smell.ID == XmlDocSmells.SeeAlsoLangwordNotSupported.ID);
        }
    }
}
