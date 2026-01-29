using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Syntax.Returns
{
    /// <summary>
    /// Tests for DOC500 (MissingReturns): a non-void member has no <returns> documentation.
    /// </summary>
    public sealed class DOC500_MissingReturnsTests
    {
        /// <summary>
        /// Provides code samples of non-void members where <returns> is missing.
        /// Each case is designed to produce exactly one DOC500 finding and no other returns smells.
        /// </summary>
        /// <returns>Test cases consisting of member code.</returns>
        public static IEnumerable<object[]> DeclarationSources()
        {
            // Method returning int.
            yield return new object[]
            {
                "/// <summary>Test.</summary>\n" +
                "public int M() { return 0; }\n"
            };

            // Property returning int.
            yield return new object[]
            {
                "/// <summary>Test.</summary>\n" +
                "public int P { get { return 0; } }\n"
            };

            // Indexer returning int.
            yield return new object[]
            {
                "/// <summary>Test.</summary>\n" +
                "public int this[int index]\n" +
                "{\n" +
                "    get { return 0; }\n" +
                "}\n"
            };

            // Delegate returning int.
            yield return new object[]
            {
                "/// <summary>Test.</summary>\n" +
                "public delegate int D();\n"
            };

            // Operator returning Wrapper.
            yield return new object[]
            {
                "/// <summary>Test.</summary>\n" +
                "public static Wrapper operator +(Wrapper left, Wrapper right)\n" +
                "{\n" +
                "    return left;\n" +
                "}\n"
            };

            // Conversion operator returning int.
            yield return new object[]
            {
                "/// <summary>Test.</summary>\n" +
                "public static explicit operator int(Wrapper value)\n" +
                "{\n" +
                "    return 0;\n" +
                "}\n"
            };
        }

        /// <summary>
        /// Ensures that missing <returns> is reported as DOC500 and that no other returns smells are produced.
        /// </summary>
        /// <param name="memberCode">The member code snippet.</param>
        [Theory]
        [MemberData(nameof(DeclarationSources))]
        public void MissingReturns_IsDetected(string memberCode)
        {
            List<Finding> findings = CheckAssert.FindReturnsFindingsForMember(memberCode);

            FindingAsserts.HasExactlySmells(findings, "DOC500");

            Finding finding = findings.Single();
            Assert.Equal("returns", finding.TagName);
            Assert.Equal(finding.Smell.MessageTemplate, finding.Message);
        }
    }
}
