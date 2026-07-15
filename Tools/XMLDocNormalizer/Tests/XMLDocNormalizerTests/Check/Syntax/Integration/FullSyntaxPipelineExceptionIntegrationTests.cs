using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Syntax.Integration
{
    /// <summary>
    /// Integration tests for exception tag interactions in the full syntax detector pipeline.
    /// </summary>
    public sealed class FullSyntaxPipelineExceptionIntegrationTests
    {
        /// <summary>
        /// Ensures that a syntactically valid exception tag does not produce syntax findings.
        /// </summary>
        [Fact]
        public void ValidExceptionTagOnExecutableMember_DoesNotTriggerAnySyntaxFindings()
        {
            string memberCode =
                "/// <summary>Runs the operation.</summary>\n" +
                "/// <exception cref=\"System.InvalidOperationException\">Thrown when the operation is invalid.</exception>\n" +
                "public void M()\n" +
                "{\n" +
                "}\n";

            List<Finding> findings = CheckAssert.FindAllFindingsForMember(memberCode);

            FindingAsserts.HasExactlySmells(findings);
        }

        /// <summary>
        /// Ensures that an exception tag without cref is reported precisely.
        /// </summary>
        [Fact]
        public void ExceptionWithoutCref_ReportsOnlyExceptionMissingCref()
        {
            string memberCode =
                "/// <summary>Runs the operation.</summary>\n" +
                "/// <exception>Thrown when the operation is invalid.</exception>\n" +
                "public void M()\n" +
                "{\n" +
                "}\n";

            List<Finding> findings = CheckAssert.FindAllFindingsForMember(memberCode);

            FindingAsserts.HasExactlySmells(findings, XmlDocSmells.ExceptionMissingCref.ID);
        }

        /// <summary>
        /// Ensures that an empty exception description is reported precisely.
        /// </summary>
        [Fact]
        public void EmptyExceptionDescription_ReportsOnlyEmptyExceptionDescription()
        {
            string memberCode =
                "/// <summary>Runs the operation.</summary>\n" +
                "/// <exception cref=\"System.InvalidOperationException\"></exception>\n" +
                "public void M()\n" +
                "{\n" +
                "}\n";

            List<Finding> findings = CheckAssert.FindAllFindingsForMember(memberCode);

            FindingAsserts.HasExactlySmells(findings, XmlDocSmells.EmptyExceptionDescription.ID);
        }

        /// <summary>
        /// Ensures that duplicate exception tags are reported precisely.
        /// </summary>
        [Fact]
        public void DuplicateExceptionTags_ReportOnlyDuplicateExceptionTag()
        {
            string memberCode =
                "/// <summary>Runs the operation.</summary>\n" +
                "/// <exception cref=\"System.InvalidOperationException\">First description.</exception>\n" +
                "/// <exception cref=\"System.InvalidOperationException\">Second description.</exception>\n" +
                "public void M()\n" +
                "{\n" +
                "}\n";

            List<Finding> findings = CheckAssert.FindAllFindingsForMember(memberCode);

            FindingAsserts.HasExactlySmells(findings, XmlDocSmells.DuplicateExceptionTag.ID);
        }

        /// <summary>
        /// Ensures that exception documentation on a non-executable member is reported by the specific exception rule only.
        /// </summary>
        [Fact]
        public void ExceptionTagOnProperty_ReportsOnlyExceptionTagOnNonExecutableMember()
        {
            string memberCode =
                "/// <summary>Gets the current count.</summary>\n" +
                "/// <value>The current count.</value>\n" +
                "/// <exception cref=\"System.InvalidOperationException\">Thrown when invalid.</exception>\n" +
                "public int Count { get; }\n";

            List<Finding> findings = CheckAssert.FindAllFindingsForMember(memberCode);

            FindingAsserts.HasExactlySmells(findings, XmlDocSmells.ExceptionTagOnNonExecutableMember.ID);
        }
    }
}
