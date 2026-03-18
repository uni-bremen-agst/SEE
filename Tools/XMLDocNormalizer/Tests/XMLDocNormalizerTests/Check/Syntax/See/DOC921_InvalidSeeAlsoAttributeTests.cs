using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Syntax.See
{
    /// <summary>
    /// Verifies detection of DOC921 (invalid attribute on <seealso>).
    /// </summary>
    public sealed class DOC921_InvalidSeeAlsoAttributeTests
    {
        /// <summary>
        /// Provides <seealso> declarations with unsupported attributes.
        /// </summary>
        /// <returns>Member snippets containing invalid <seealso> attributes.</returns>
        public static IEnumerable<object[]> DeclarationSources()
        {
            yield return new object[]
            {
                "/// <summary>Test.</summary>\n" +
                "/// <seealso foo=\"bar\" />\n" +
                "public void M() { }\n"
            };

            yield return new object[]
            {
                "/// <summary>Test.</summary>\n" +
                "/// <seealso cref=\"string\" foo=\"bar\" />\n" +
                "public void M() { }\n"
            };
        }

        /// <summary>
        /// Ensures DOC921 is reported when a <seealso> tag uses an unsupported attribute.
        /// </summary>
        /// <param name="memberCode">The member snippet to analyze.</param>
        [Theory]
        [MemberData(nameof(DeclarationSources))]
        public void SeeAlsoWithInvalidAttribute_IsDetected(string memberCode)
        {
            List<Finding> findings = CheckAssert.FindSeeFindingsForMember(memberCode);

            Assert.Contains(findings, static finding => finding.Smell.ID == XmlDocSmells.InvalidSeeAlsoAttribute.ID);
        }
    }
}
