using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Syntax.See
{
    /// <summary>
    /// Verifies detection of DOC981 (seealso must be empty).
    /// </summary>
    public sealed class DOC981_SeeAlsoNotEmptyTests
    {
        /// <summary>
        /// Provides <seealso> elements with invalid inner content.
        /// </summary>
        public static IEnumerable<object[]> DeclarationSources()
        {
            yield return new object[]
            {
                "/// <summary>Test</summary>\n" +
                "/// <seealso cref=\"string\">Text</seealso>\n" +
                "public void M() { }\n"
            };

            yield return new object[]
            {
                "/// <summary>Test</summary>\n" +
                "/// <seealso cref=\"string\"><para>Text</para></seealso>\n" +
                "public void M() { }\n"
            };
        }

        /// <summary>
        /// Ensures DOC981 is reported when a <seealso> tag contains content.
        /// </summary>
        [Theory]
        [MemberData(nameof(DeclarationSources))]
        public void SeeAlsoWithContent_IsDetected(string memberCode)
        {
            List<Finding> findings = CheckAssert.FindSeeFindingsForMember(memberCode);

            Assert.Contains(
                findings,
                static f => f.Smell.ID == XmlDocSmells.SeeAlsoNotEmpty.ID);
        }
    }
}
