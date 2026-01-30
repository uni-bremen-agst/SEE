using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Syntax.Exception
{
    /// <summary>
    /// Tests for DOC620 (EmptyExceptionDescription): an <exception> tag exists but its description is empty.
    /// </summary>
    public sealed class DOC620_EmptyExceptionDescriptionTests
    {
        /// <summary>
        /// Provides member snippets where an <exception> tag exists with a cref but has no meaningful content.
        /// Each case is designed to produce exactly one DOC620 finding and no other exception smells.
        /// </summary>
        /// <returns>Test cases consisting of member code and the expected cref string.</returns>
        public static IEnumerable<object[]> DeclarationSources()
        {
            yield return new object[]
            {
                "/// <summary>Test.</summary>\n" +
                "/// <exception cref=\"System.InvalidOperationException\"></exception>\n" +
                "public void M() { }\n",
                "System.InvalidOperationException"
            };

            yield return new object[]
            {
                "/// <summary>Test.</summary>\n" +
                "/// <exception cref=\"System.InvalidOperationException\"> \n" +
                "/// </exception>\n" +
                "public void M() { }\n",
                "System.InvalidOperationException"
            };
        }

        /// <summary>
        /// Ensures that an empty exception description is reported as DOC620 only and the message is correctly formatted.
        /// </summary>
        /// <param name="memberCode">The member snippet.</param>
        /// <param name="cref">The cref expected in the formatted message.</param>
        [Theory]
        [MemberData(nameof(DeclarationSources))]
        public void EmptyExceptionDescription_IsDetected(string memberCode, string cref)
        {
            List<Finding> findings = CheckAssert.FindExceptionFindingsForMember(memberCode);

            FindingAsserts.HasExactlySmells(findings, "DOC620");

            Finding finding = findings.Single();
            string expected = string.Format(finding.Smell.MessageTemplate, cref);

            Assert.Equal("exception", finding.TagName);
            Assert.Equal(expected, finding.Message);
        }
    }
}