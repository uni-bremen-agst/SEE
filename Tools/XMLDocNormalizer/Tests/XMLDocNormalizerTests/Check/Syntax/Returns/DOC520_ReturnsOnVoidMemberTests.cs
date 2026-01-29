using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Syntax.Returns
{
    /// <summary>
    /// Tests for DOC520 (ReturnsOnVoidMember): a void member contains a <returns> tag.
    /// </summary>
    public sealed class DOC520_ReturnsOnVoidMemberTests
    {
        /// <summary>
        /// Provides code samples where a void member incorrectly contains a <returns> tag.
        /// Each case is designed to produce exactly one DOC520 finding and no other returns smells.
        /// </summary>
        /// <returns>Test cases consisting of member code.</returns>
        public static IEnumerable<object[]> DeclarationSources()
        {
            // void method with returns tag.
            yield return new object[]
            {
                "/// <summary>Test.</summary>\n" +
                "/// <returns>bad</returns>\n" +
                "public void M() { }\n"
            };

            // void delegate with returns tag.
            yield return new object[]
            {
                "/// <summary>Test.</summary>\n" +
                "/// <returns>bad</returns>\n" +
                "public delegate void D();\n"
            };
        }

        /// <summary>
        /// Ensures that <returns> on a void member is reported as DOC520 and that no other returns smells are produced.
        /// </summary>
        /// <param name="memberCode">The member code snippet.</param>
        [Theory]
        [MemberData(nameof(DeclarationSources))]
        public void ReturnsOnVoidMember_IsDetected(string memberCode)
        {
            List<Finding> findings = CheckAssert.FindReturnsFindingsForMember(memberCode);

            FindingAsserts.HasExactlySmells(findings, "DOC520");

            Finding finding = findings.Single();
            Assert.Equal("returns", finding.TagName);
            Assert.Equal(finding.Smell.MessageTemplate, finding.Message);
        }
    }
}
