using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Syntax.Value
{
    /// <summary>
    /// Tests DOC831 for value tags on member kinds that do not support value documentation.
    /// </summary>
    public sealed class DOC831_InvalidMemberMatrixTests
    {
        /// <summary>
        /// Provides member declarations on which a value tag is invalid.
        /// </summary>
        /// <returns>Member declaration samples expected to produce DOC831.</returns>
        public static IEnumerable<object[]> InvalidValueMemberSources()
        {
            yield return new object[]
            {
                "/// <summary>Does something.</summary>\n" +
                "/// <value>Invalid.</value>\n" +
                "public void M() { }\n"
            };

            yield return new object[]
            {
                "/// <summary>Creates something.</summary>\n" +
                "/// <value>Invalid.</value>\n" +
                "public TestClass() { }\n"
            };

            yield return new object[]
            {
                "/// <summary>Stores something.</summary>\n" +
                "/// <value>Invalid.</value>\n" +
                "public int field;\n"
            };

            yield return new object[]
            {
                "/// <summary>Signals something.</summary>\n" +
                "/// <value>Invalid.</value>\n" +
                "public event System.Action E;\n"
            };
        }

        /// <summary>
        /// Ensures that a value tag on an unsupported member kind triggers DOC831.
        /// </summary>
        /// <param name="memberCode">The member declaration to analyze.</param>
        [Theory]
        [MemberData(nameof(InvalidValueMemberSources))]
        public void InvalidValueMember_TriggersDoc831(string memberCode)
        {
            List<Finding> findings = CheckAssert.FindValueFindingsForMember(memberCode);

            Finding finding = Assert.Single(findings);
            Assert.Equal(XmlDocSmells.ValueOnInvalidMember.ID, finding.Smell.ID);
            Assert.Equal("value", finding.TagName);
        }
    }
}
