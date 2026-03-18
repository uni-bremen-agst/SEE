using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Syntax.See
{
    /// <summary>
    /// Verifies detection of DOC980 (see must be empty).
    /// </summary>
    public sealed class DOC980_SeeNotEmptyTests
    {
        /// <summary>
        /// Provides <see> elements with invalid inner content.
        /// </summary>
        public static IEnumerable<object[]> DeclarationSources()
        {
            yield return new object[]
            {
                "/// <summary><see cref=\"string\">Text</see></summary>\n" +
                "public void M() { }\n"
            };

            yield return new object[]
            {
                "/// <summary><see cref=\"string\"><para>Text</para></see></summary>\n" +
                "public void M() { }\n"
            };
        }

        /// <summary>
        /// Ensures DOC980 is reported when a <see> tag contains content.
        /// </summary>
        [Theory]
        [MemberData(nameof(DeclarationSources))]
        public void SeeWithContent_IsDetected(string memberCode)
        {
            List<Finding> findings = CheckAssert.FindSeeFindingsForMember(memberCode);

            Assert.Contains(
                findings,
                static f => f.Smell.ID == XmlDocSmells.SeeNotEmpty.ID);
        }
    }
}
