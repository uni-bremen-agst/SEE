using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Syntax.Exception
{
    /// <summary>
    /// Tests for DOC650 (DuplicateExceptionTag): multiple <exception> tags exist for the same cref.
    /// </summary>
    public sealed class DOC650_DuplicateExceptionTagTests
    {
        /// <summary>
        /// Provides member snippets where the same exception cref is documented multiple times.
        /// Each case is designed to produce exactly one DOC650 finding.
        /// </summary>
        /// <returns>Test cases consisting of member code and the duplicate cref string.</returns>
        public static IEnumerable<object[]> DeclarationSources()
        {
            yield return new object[]
            {
                "/// <summary>Test.</summary>\n" +
                "/// <exception cref=\"System.InvalidOperationException\">first</exception>\n" +
                "/// <exception cref=\"System.InvalidOperationException\">second</exception>\n" +
                "public void M() { }\n",
                "System.InvalidOperationException"
            };
        }

        /// <summary>
        /// Ensures that duplicate exception tags are reported as DOC650 only and the message is correctly formatted.
        /// </summary>
        /// <param name="memberCode">The member snippet.</param>
        /// <param name="cref">The cref expected in the formatted message.</param>
        [Theory]
        [MemberData(nameof(DeclarationSources))]
        public void DuplicateExceptionTag_IsDetected(string memberCode, string cref)
        {
            List<Finding> findings = CheckAssert.FindExceptionFindingsForMember(memberCode);

            FindingAsserts.HasExactlySmells(findings, "DOC650");

            Finding finding = findings.Single();
            string expected = string.Format(finding.Smell.MessageTemplate, cref);

            Assert.Equal("exception", finding.TagName);
            Assert.Equal(expected, finding.Message);
        }
    }
}