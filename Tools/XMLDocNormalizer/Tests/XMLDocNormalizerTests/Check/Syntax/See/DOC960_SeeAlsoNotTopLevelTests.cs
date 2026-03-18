using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Syntax.See
{
    /// <summary>
    /// Verifies detection of DOC960 (seealso is not top-level).
    /// </summary>
    public sealed class DOC960_SeeAlsoNotTopLevelTests
    {
        /// <summary>
        /// Provides <seealso> declarations that are nested inside other XML elements.
        /// </summary>
        /// <returns>Member snippets containing nested <seealso> elements.</returns>
        public static IEnumerable<object[]> DeclarationSources()
        {
            yield return new object[]
            {
                "/// <summary><seealso cref=\"string\" /></summary>\n" +
                "public void M() { }\n"
            };

            yield return new object[]
            {
                "/// <remarks><seealso cref=\"string\" /></remarks>\n" +
                "public void M() { }\n"
            };

            yield return new object[]
            {
                "/// <summary><para><seealso cref=\"string\" /></para></summary>\n" +
                "public void M() { }\n"
            };
        }

        /// <summary>
        /// Ensures DOC960 is reported when a <seealso> tag is not placed top-level.
        /// </summary>
        /// <param name="memberCode">The member snippet to analyze.</param>
        [Theory]
        [MemberData(nameof(DeclarationSources))]
        public void NestedSeeAlso_IsDetected(string memberCode)
        {
            List<Finding> findings = CheckAssert.FindSeeFindingsForMember(memberCode);

            Assert.Contains(
                findings,
                static finding => finding.Smell.ID == XmlDocSmells.SeeAlsoNotTopLevel.ID);
        }
    }
}
