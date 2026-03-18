using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Syntax.See
{
    /// <summary>
    /// Verifies detection of DOC901 (missing target on <seealso>).
    /// </summary>
    public sealed class DOC901_SeeAlsoMissingTargetTests
    {
        /// <summary>
        /// Provides <seealso> declarations without any valid target attribute.
        /// Each case is expected to produce exactly one DOC901 finding.
        /// </summary>
        /// <returns>Member snippets containing invalid <seealso> elements.</returns>
        public static IEnumerable<object[]> DeclarationSources()
        {
            yield return new object[]
            {
                "/// <summary>Test.</summary>\n" +
                "/// <seealso />\n" +
                "public void M() { }\n"
            };

            yield return new object[]
            {
                "/// <summary>Test.</summary>\n" +
                "/// <seealso title=\"Text\" />\n" +
                "public void M() { }\n"
            };

            yield return new object[]
            {
                "/// <summary>Test.</summary>\n" +
                "/// <seealso></seealso>\n" +
                "public void M() { }\n"
            };

            yield return new object[]
            {
                "/// <summary>Test.</summary>\n" +
                "/// <seealso langword=\"null\" />\n" +
                "public void M() { }\n"
            };
        }

        /// <summary>
        /// Ensures DOC901 is reported when a <seealso> tag has no valid target attribute.
        /// </summary>
        /// <param name="memberCode">The member snippet to analyze.</param>
        [Theory]
        [MemberData(nameof(DeclarationSources))]
        public void SeeAlsoWithoutTarget_IsDetected(string memberCode)
        {
            List<Finding> findings = CheckAssert.FindSeeFindingsForMember(memberCode);

            FindingAsserts.HasExactlySmells(findings, XmlDocSmells.SeeAlsoMissingTarget.ID);

            Finding finding = findings.Single();
            Assert.Equal("seealso", finding.TagName);
        }
    }
}
