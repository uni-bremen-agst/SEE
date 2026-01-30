using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Syntax.Exception
{
    /// <summary>
    /// Smoke tests ensuring that the syntax-only exception detector produces no findings for valid inputs.
    /// </summary>
    public sealed class Syntax_NoFinding_ExceptionDetector_SmokeTests
    {
        /// <summary>
        /// Ensures that a member without exception tags and without rethrow produces no exception findings.
        /// </summary>
        [Fact]
        public void NoExceptionTags_NoRethrow_ProducesNoFindings()
        {
            string member =
                "/// <summary>Test.</summary>\n" +
                "public void M() { }\n";

            var findings = CheckAssert.FindExceptionFindingsForMember(member);

            Assert.Empty(findings);
        }

        /// <summary>
        /// Ensures that a member with a non-empty exception tag and no duplicates produces no findings.
        /// </summary>
        [Fact]
        public void ValidExceptionTag_ProducesNoFindings()
        {
            string member =
                "/// <summary>Test.</summary>\n" +
                "/// <exception cref=\"System.InvalidOperationException\">Ok</exception>\n" +
                "public void M() { }\n";

            var findings = CheckAssert.FindExceptionFindingsForMember(member);

            Assert.Empty(findings);
        }
    }
}