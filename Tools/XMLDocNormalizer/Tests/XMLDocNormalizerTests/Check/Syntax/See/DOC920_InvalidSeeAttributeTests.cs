using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Syntax.See
{
    /// <summary>
    /// Verifies detection of DOC920 (invalid attribute on <see>).
    /// </summary>
    public sealed class DOC920_InvalidSeeAttributeTests
    {
        /// <summary>
        /// Provides <see> declarations with unsupported attributes.
        /// </summary>
        /// <returns>Member snippets containing invalid <see> attributes.</returns>
        public static IEnumerable<object[]> DeclarationSources()
        {
            yield return new object[]
            {
                "/// <summary><see foo=\"bar\" /></summary>\n" +
                "public void M() { }\n"
            };

            yield return new object[]
            {
                "/// <summary><see cref=\"string\" foo=\"bar\" /></summary>\n" +
                "public void M() { }\n"
            };
        }

        /// <summary>
        /// Ensures DOC920 is reported when a <see> tag uses an unsupported attribute.
        /// </summary>
        /// <param name="memberCode">The member snippet to analyze.</param>
        [Theory]
        [MemberData(nameof(DeclarationSources))]
        public void SeeWithInvalidAttribute_IsDetected(string memberCode)
        {
            List<Finding> findings = CheckAssert.FindSeeFindingsForMember(memberCode);

            Assert.Contains(findings, static finding => finding.Smell.ID == XmlDocSmells.InvalidSeeAttribute.ID);
        }
    }
}
