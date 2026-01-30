using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Syntax.Exception
{
    /// <summary>
    /// Tests for DOC640 (RethrowCannotInferException): a rethrow statement ('throw;') was detected.
    /// </summary>
    public sealed class DOC640_RethrowCannotInferExceptionTests
    {
        /// <summary>
        /// Ensures that a rethrow statement is reported as DOC640 only.
        /// </summary>
        [Fact]
        public void Rethrow_IsDetected()
        {
            string member =
                "/// <summary>Test.</summary>\n" +
                "public void M()\n" +
                "{\n" +
                "    try { }\n" +
                "    catch\n" +
                "    {\n" +
                "        throw;\n" +
                "    }\n" +
                "}\n";

            List<Finding> findings = CheckAssert.FindExceptionFindingsForMember(member);

            FindingAsserts.HasExactlySmells(findings, "DOC640");

            Finding finding = findings.Single();

            Assert.Equal("exception", finding.TagName);
            Assert.Equal(finding.Smell.MessageTemplate, finding.Message);
        }
    }
}