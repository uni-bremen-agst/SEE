using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Syntax.Returns
{
    /// <summary>
    /// Verifies detection of DOC500 (MissingReturns).
    /// 
    /// DOC500 is reported when a member that supports the returns tag
    /// (method, delegate, operator, conversion operator) returns a non-void type
    /// but does not contain a returns element.
    /// 
    /// Property and indexer members must not trigger this smell,
    /// as they use the value tag instead.
    /// </summary>
    public sealed class DOC500_MissingReturnsTests
    {
        #region Positive Cases
        /// <summary>
        /// Provides members that require a returns tag but omit it.
        /// </summary>
        public static IEnumerable<object[]> DeclarationSources()
        {
            yield return new object[]
            {
                "/// <summary>Test.</summary>\n" +
                "public int M() { return 0; }\n"
            };

            yield return new object[]
            {
                "/// <summary>Test.</summary>\n" +
                "public delegate int D();\n"
            };

            yield return new object[]
            {
                "/// <summary>Test.</summary>\n" +
                "public static Wrapper operator +(Wrapper left, Wrapper right)\n" +
                "{ return left; }\n"
            };

            yield return new object[]
            {
                "/// <summary>Test.</summary>\n" +
                "public static explicit operator int(Wrapper value)\n" +
                "{ return 0; }\n"
            };
        }

        /// <summary>
        /// Ensures DOC500 is reported for members missing a required returns tag.
        /// </summary>
        [Theory]
        [MemberData(nameof(DeclarationSources))]
        public void MissingReturns_IsDetected(string memberCode)
        {
            List<Finding> findings = CheckAssert.FindReturnsFindingsForMember(memberCode);

            FindingAsserts.HasExactlySmells(findings, "DOC500");

            Finding finding = findings.Single();
            Assert.Equal("returns", finding.TagName);
        }
        #endregion


        #region Negative Cases (Property and Indexer)
        /// <summary>
        /// Provides property and indexer members.
        /// These must not trigger DOC500 because they use value instead of returns.
        /// </summary>
        public static IEnumerable<object[]> PropertyAndIndexerSources()
        {
            yield return new object[]
            {
                "/// <summary>Test.</summary>\n" +
                "public int P { get { return 0; } }\n"
            };

            yield return new object[]
            {
                "/// <summary>Test.</summary>\n" +
                "public int this[int i] { get { return i; } }\n"
            };
        }

        /// <summary>
        /// Ensures property and indexer members do not trigger DOC500.
        /// </summary>
        [Theory]
        [MemberData(nameof(PropertyAndIndexerSources))]
        public void PropertyAndIndexer_DoNotTrigger_DOC500(string memberCode)
        {
            List<Finding> findings = CheckAssert.FindReturnsFindingsForMember(memberCode);

            Assert.DoesNotContain(findings, f => f.Smell.Id == "DOC500");
        }
        #endregion
    }
}
