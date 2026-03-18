using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Syntax.See
{
    /// <summary>
    /// Verifies detection of DOC910 (invalid target combinations on <see>).
    /// </summary>
    public sealed class DOC910_InvalidSeeAttributeCombinationTests
    {
        /// <summary>
        /// Provides <see> declarations that combine multiple mutually exclusive target attributes.
        /// </summary>
        /// <returns>Member snippets containing invalid <see> combinations.</returns>
        public static IEnumerable<object[]> DeclarationSources()
        {
            yield return new object[]
            {
                "/// <summary><see cref=\"string\" href=\"https://example.com\" /></summary>\n" +
                "public void M() { }\n"
            };

            yield return new object[]
            {
                "/// <summary><see cref=\"string\" langword=\"null\" /></summary>\n" +
                "public void M() { }\n"
            };

            yield return new object[]
            {
                "/// <summary><see href=\"https://example.com\" langword=\"null\" /></summary>\n" +
                "public void M() { }\n"
            };

            yield return new object[]
            {
                "/// <summary><see cref=\"string\" href=\"https://example.com\" langword=\"null\" /></summary>\n" +
                "public void M() { }\n"
            };
        }

        /// <summary>
        /// Ensures DOC910 is reported when a <see> tag combines multiple target attributes.
        /// </summary>
        /// <param name="memberCode">The member snippet to analyze.</param>
        [Theory]
        [MemberData(nameof(DeclarationSources))]
        public void SeeWithMultipleTargets_IsDetected(string memberCode)
        {
            List<Finding> findings = CheckAssert.FindSeeFindingsForMember(memberCode);

            FindingAsserts.HasExactlySmells(findings, XmlDocSmells.InvalidSeeAttributeCombination.ID);

            Finding finding = findings.Single();
            Assert.Equal("see", finding.TagName);
        }
    }
}
