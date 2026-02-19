using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Syntax.Returns
{
    /// <summary>
    /// Verifies detection of DOC530 (DuplicateReturnsTag).
    /// 
    /// DOC530 is reported when more than one returns tag
    /// is declared on a member that supports it.
    /// </summary>
    public sealed class DOC530_DuplicateReturnsTagTests
    {
        #region Positive Cases

        /// <summary>
        /// Provides members containing duplicate returns tags.
        /// </summary>
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
                "public delegate int D();\n"
            };

            yield return new object[]
            {
                "/// <summary>Test.</summary>\n" +
                "/// <returns>first</returns>\n" +
                "/// <returns>second</returns>\n" +
                "public static Wrapper operator +(Wrapper left, Wrapper right)\n" +
                "{ return left; }\n"
            };

            yield return new object[]
            {
                "/// <summary>Test.</summary>\n" +
                "/// <returns>first</returns>\n" +
                "/// <returns>second</returns>\n" +
                "public static explicit operator int(Wrapper value)\n" +
                "{ return 0; }\n"
            };
        }

        /// <summary>
        /// Ensures DOC530 is reported for duplicate returns tags.
        /// </summary>
        [Theory]
        [MemberData(nameof(DeclarationSources))]
        public void DuplicateReturns_IsDetected(string memberCode)
        {
            List<Finding> findings = CheckAssert.FindReturnsFindingsForMember(memberCode);

            FindingAsserts.HasExactlySmells(findings, "DOC530");

            Finding finding = findings.Single();
            Assert.Equal("returns", finding.TagName);
        }
        #endregion
    }
}
