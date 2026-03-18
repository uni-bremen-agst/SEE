using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Syntax.See
{
    /// <summary>
    /// Verifies detection of DOC900 (missing target on <see>).
    /// </summary>
    public sealed class DOC900_SeeMissingTargetTests
    {
        /// <summary>
        /// Provides <see> declarations without any valid target attribute.
        /// Each case is expected to produce exactly one DOC900 finding.
        /// </summary>
        /// <returns>Member snippets containing invalid <see> elements.</returns>
        public static IEnumerable<object[]> DeclarationSources()
        {
            yield return new object[]
            {
                "/// <summary><see /></summary>\n" +
                "public void M() { }\n"
            };

            yield return new object[]
            {
                "/// <summary><see title=\"Text\" /></summary>\n" +
                "public void M() { }\n"
            };

            yield return new object[]
            {
                "/// <summary><see></see></summary>\n" +
                "public void M() { }\n"
            };
        }

        /// <summary>
        /// Ensures DOC900 is reported when a <see> tag has no target attribute.
        /// </summary>
        /// <param name="memberCode">The member snippet to analyze.</param>
        [Theory]
        [MemberData(nameof(DeclarationSources))]
        public void SeeWithoutTarget_IsDetected(string memberCode)
        {
            List<Finding> findings = CheckAssert.FindSeeFindingsForMember(memberCode);

            FindingAsserts.HasExactlySmells(findings, XmlDocSmells.SeeMissingTarget.ID);

            Finding finding = findings.Single();
            Assert.Equal("see", finding.TagName);
        }
    }
}
