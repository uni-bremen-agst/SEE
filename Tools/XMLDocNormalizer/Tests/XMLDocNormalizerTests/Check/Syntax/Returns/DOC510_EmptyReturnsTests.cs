using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Syntax.Returns
{
    /// <summary>
    /// Verifies detection of DOC510 (EmptyReturns).
    /// 
    /// DOC510 is reported when a member supports returns,
    /// the tag is present, but its content is empty.
    /// 
    /// Property and indexer members must not trigger this smell.
    /// </summary>
    public sealed class DOC510_EmptyReturnsTests
    {
        #region Positive Cases

        /// <summary>
        /// Provides members containing an empty returns tag.
        /// </summary>
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
                "/// <returns></returns>\n" +
                "public delegate int D();\n"
            };

            yield return new object[]
            {
                "/// <summary>Test.</summary>\n" +
                "/// <returns></returns>\n" +
                "public static Wrapper operator +(Wrapper left, Wrapper right)\n" +
                "{ return left; }\n"
            };

            yield return new object[]
            {
                "/// <summary>Test.</summary>\n" +
                "/// <returns></returns>\n" +
                "public static explicit operator int(Wrapper value)\n" +
                "{ return 0; }\n"
            };
        }

        /// <summary>
        /// Ensures DOC510 is reported for empty returns tags.
        /// </summary>
        [Theory]
        [MemberData(nameof(DeclarationSources))]
        public void EmptyReturns_IsDetected(string memberCode)
        {
            List<Finding> findings = CheckAssert.FindReturnsFindingsForMember(memberCode);

            FindingAsserts.HasExactlySmells(findings, "DOC510");

            Finding finding = findings.Single();
            Assert.Equal("returns", finding.TagName);
        }
        #endregion


        #region Negative Cases
        /// <summary>
        /// Ensures property and indexer members do not trigger DOC510.
        /// </summary>
        [Theory]
        [MemberData(nameof(DOC500_MissingReturnsTests.PropertyAndIndexerSources),
            MemberType = typeof(DOC500_MissingReturnsTests))]
        public void PropertyAndIndexer_DoNotTrigger_DOC510(string memberCode)
        {
            List<Finding> findings = CheckAssert.FindReturnsFindingsForMember(memberCode);

            Assert.DoesNotContain(findings, f => f.Smell.Id == "DOC510");
        }
        #endregion
    }
}
