using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Syntax.Returns
{
    /// <summary>
    /// Verifies detection of DOC520 (ReturnsOnVoidMember).
    /// 
    /// DOC520 is reported when a void member incorrectly declares a returns tag.
    /// </summary>
    public sealed class DOC520_ReturnsOnVoidMemberTests
    {
        #region Positive Cases

        /// <summary>
        /// Provides void members containing an invalid returns tag.
        /// </summary>
        public static IEnumerable<object[]> DeclarationSources()
        {
            yield return new object[]
            {
                "/// <summary>Test.</summary>\n" +
                "/// <returns>invalid</returns>\n" +
                "public void M() { }\n"
            };

            yield return new object[]
            {
                "/// <summary>Test.</summary>\n" +
                "/// <returns>invalid</returns>\n" +
                "public delegate void D();\n"
            };
        }

        /// <summary>
        /// Ensures DOC520 is reported when returns is used on void members.
        /// </summary>
        [Theory]
        [MemberData(nameof(DeclarationSources))]
        public void ReturnsOnVoidMember_IsDetected(string memberCode)
        {
            List<Finding> findings = CheckAssert.FindReturnsFindingsForMember(memberCode);

            FindingAsserts.HasExactlySmells(findings, "DOC520");

            Finding finding = findings.Single();
            Assert.Equal("returns", finding.TagName);
        }
        #endregion
    }
}
