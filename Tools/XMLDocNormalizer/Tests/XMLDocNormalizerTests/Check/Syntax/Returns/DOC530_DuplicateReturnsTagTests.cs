using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Syntax.Returns
{
    /// <summary>
    /// Tests for DOC530 (DuplicateReturnsTag): multiple <returns> tags exist.
    /// </summary>
    public sealed class DOC530_DuplicateReturnsTagTests
    {
        /// <summary>
        /// Provides code samples where multiple <returns> tags exist.
        /// Each case is designed to produce exactly one DOC530 finding (reported at the second occurrence).
        /// </summary>
        /// <returns>Test cases consisting of member code.</returns>
        public static IEnumerable<object[]> DeclarationSources()
        {
            yield return new object[]
            {
                "/// <summary>Test.</summary>\n" +
                "/// <returns>first</returns>\n" +
                "/// <returns>second</returns>\n" +
                "public int M() { return 0; }\n"
            };

            yield return new object[]
            {
                "/// <summary>Test.</summary>\n" +
                "/// <returns>first</returns>\n" +
                "/// <returns>second</returns>\n" +
                "public int P { get { return 0; } }\n"
            };

            yield return new object[]
            {
                "/// <summary>Test.</summary>\n" +
                "/// <returns>first</returns>\n" +
                "/// <returns>second</returns>\n" +
                "public int this[int index]\n" +
                "{\n" +
                "    get { return 0; }\n" +
                "}\n"
            };
        }

        /// <summary>
        /// Ensures that duplicate <returns> tags are reported as DOC530 and that no other returns smells are produced.
        /// </summary>
        /// <param name="memberCode">The member code snippet.</param>
        [Theory]
        [MemberData(nameof(DeclarationSources))]
        public void DuplicateReturnsTag_IsDetected(string memberCode)
        {
            List<Finding> findings = CheckAssert.FindReturnsFindingsForMember(memberCode);

            FindingAsserts.HasExactlySmells(findings, "DOC530");

            Finding finding = findings.Single();
            Assert.Equal("returns", finding.TagName);
            Assert.Equal(finding.Smell.MessageTemplate, finding.Message);
        }
    }
}
