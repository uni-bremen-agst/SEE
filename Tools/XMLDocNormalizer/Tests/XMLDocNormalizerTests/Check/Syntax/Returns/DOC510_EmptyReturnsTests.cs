using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Syntax.Returns
{
    /// <summary>
    /// Tests for DOC510 (EmptyReturns): the <returns> tag exists but its description is empty.
    /// </summary>
    public sealed class DOC510_EmptyReturnsTests
    {
        /// <summary>
        /// Provides code samples where <returns> exists but has no meaningful content.
        /// Each case is designed to produce exactly one DOC510 finding and no other returns smells.
        /// </summary>
        /// <returns>Test cases consisting of member code.</returns>
        public static IEnumerable<object[]> DeclarationSources()
        {
            yield return new object[]
            {
                "/// <summary>Test.</summary>\n" +
                "/// <returns></returns>\n" +
                "public int M() { return 0; }\n"
            };

            yield return new object[]
            {
                "/// <summary>Test.</summary>\n" +
                "/// <returns>\n" +
                "/// \n" +
                "/// </returns>\n" +
                "public int M() { return 0; }\n"
            };

            yield return new object[]
            {
                "/// <summary>Test.</summary>\n" +
                "/// <returns></returns>\n" +
                "public int P { get { return 0; } }\n"
            };

            yield return new object[]
            {
                "/// <summary>Test.</summary>\n" +
                "/// <returns></returns>\n" +
                "public int this[int index]\n" +
                "{\n" +
                "    get { return 0; }\n" +
                "}\n"
            };
        }

        /// <summary>
        /// Ensures that empty <returns> is reported as DOC510 and that no other returns smells are produced.
        /// </summary>
        /// <param name="memberCode">The member code snippet.</param>
        [Theory]
        [MemberData(nameof(DeclarationSources))]
        public void EmptyReturns_IsDetected(string memberCode)
        {
            List<Finding> findings = CheckAssert.FindReturnsFindingsForMember(memberCode);

            FindingAsserts.HasExactlySmells(findings, "DOC510");

            Finding finding = findings.Single();
            Assert.Equal("returns", finding.TagName);
            Assert.Equal(finding.Smell.MessageTemplate, finding.Message);
        }

        /// <summary>
        /// Ensures that a <returns> containing nested XML elements is treated as non-empty (no DOC510).
        /// </summary>
        [Fact]
        public void Returns_WithSee_IsNotEmpty()
        {
            string member =
                "/// <summary>Test.</summary>\n" +
                "/// <returns><see cref=\"System.Int32\"/></returns>\n" +
                "public int M() { return 0; }\n";

            List<Finding> findings = CheckAssert.FindReturnsFindingsForMember(member);

            Assert.Empty(findings);
        }
    }
}
