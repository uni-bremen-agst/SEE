using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Syntax.See
{
    /// <summary>
    /// Verifies detection of DOC950 (invalid langword on <see>).
    /// </summary>
    public sealed class DOC950_InvalidSeeLangwordTests
    {
        /// <summary>
        /// Provides <see> declarations with invalid langword values.
        /// </summary>
        /// <returns>Member snippets containing invalid <see> langword values.</returns>
        public static IEnumerable<object[]> DeclarationSources()
        {
            yield return new object[]
            {
                "/// <summary><see langword=\"invalid\" /></summary>\n" +
                "public void M() { }\n"
            };

            yield return new object[]
            {
                "/// <summary><see langword=\"String\" /></summary>\n" +
                "public void M() { }\n"
            };

            yield return new object[]
            {
                "/// <summary><see langword=\"123\" /></summary>\n" +
                "public void M() { }\n"
            };
        }

        /// <summary>
        /// Ensures DOC950 is reported when a <see> tag contains an invalid langword value.
        /// </summary>
        /// <param name="memberCode">The member snippet to analyze.</param>
        [Theory]
        [MemberData(nameof(DeclarationSources))]
        public void SeeWithInvalidLangword_IsDetected(string memberCode)
        {
            List<Finding> findings = CheckAssert.FindSeeFindingsForMember(memberCode);

            Assert.Contains(
                findings,
                static finding => finding.Smell.ID == XmlDocSmells.InvalidSeeLangword.ID);
        }
    }
}
