using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Syntax.Integration
{
    /// <summary>
    /// Integration tests for documentation completeness interactions in the full syntax detector pipeline.
    /// </summary>
    public sealed class FullSyntaxPipelineDocumentationIntegrationTests
    {
        /// <summary>
        /// Ensures that a fully documented generic method does not produce syntax findings.
        /// </summary>
        [Fact]
        public void FullyDocumentedGenericMethod_DoesNotTriggerAnySyntaxFindings()
        {
            string memberCode =
                "/// <summary>\n" +
                "/// Calculates the result.\n" +
                "/// </summary>\n" +
                "/// <typeparam name=\"T\">The input type.</typeparam>\n" +
                "/// <param name=\"value\">The input value.</param>\n" +
                "/// <returns>The calculated result.</returns>\n" +
                "public int M<T>(T value)\n" +
                "{\n" +
                "    return 0;\n" +
                "}\n";

            List<Finding> findings = CheckAssert.FindAllFindingsForMember(memberCode);

            FindingAsserts.HasExactlySmells(findings);
        }

        /// <summary>
        /// Ensures that a member documented only by inheritdoc does not produce missing detail-tag findings.
        /// </summary>
        [Fact]
        public void InheritdocOnlyGenericMethod_DoesNotTriggerMissingDetailTagFindings()
        {
            string memberCode =
                "/// <inheritdoc/>\n" +
                "public int M<T>(T value)\n" +
                "{\n" +
                "    return 0;\n" +
                "}\n";

            List<Finding> findings = CheckAssert.FindAllFindingsForMember(memberCode);

            FindingAsserts.HasExactlySmells(findings);
        }

        /// <summary>
        /// Ensures that inheritdoc does not suppress explicitly empty detail tags.
        /// </summary>
        [Fact]
        public void InheritdocWithExplicitEmptyDetailTags_ReportsOnlyExplicitEmptyTags()
        {
            string memberCode =
                "/// <inheritdoc/>\n" +
                "/// <typeparam name=\"T\"></typeparam>\n" +
                "/// <param name=\"value\"></param>\n" +
                "/// <returns></returns>\n" +
                "public int M<T>(T value)\n" +
                "{\n" +
                "    return 0;\n" +
                "}\n";

            List<Finding> findings = CheckAssert.FindAllFindingsForMember(memberCode);

            FindingAsserts.HasExactlySmells(
                findings,
                XmlDocSmells.EmptyParamDescription.ID,
                XmlDocSmells.EmptyReturns.ID,
                XmlDocSmells.EmptyTypeParamDescription.ID);
        }

        /// <summary>
        /// Ensures that missing documentation on a member is reported without additional tag-specific noise.
        /// </summary>
        [Fact]
        public void UndocumentedMethod_ReportsOnlyMissingDocumentation()
        {
            string memberCode =
                "public int M(int value)\n" +
                "{\n" +
                "    return value;\n" +
                "}\n";

            List<Finding> findings = CheckAssert.FindAllFindingsForMember(memberCode);

            FindingAsserts.HasExactlySmells(findings, XmlDocSmells.MissingDocumentation.ID);
        }

        /// <summary>
        /// Ensures that a documented method with no summary reports missing summary and no unrelated findings.
        /// </summary>
        [Fact]
        public void DocumentedMethodWithoutSummary_ReportsOnlyMissingSummary()
        {
            string memberCode =
                "/// <remarks>Additional information.</remarks>\n" +
                "public void M()\n" +
                "{\n" +
                "}\n";

            List<Finding> findings = CheckAssert.FindAllFindingsForMember(memberCode);

            FindingAsserts.HasExactlySmells(findings, XmlDocSmells.MissingSummary.ID);
        }

        /// <summary>
        /// Ensures that empty summary and remarks tags are reported precisely.
        /// </summary>
        [Fact]
        public void EmptySummaryAndRemarks_ReportOnlyEmptySummaryAndEmptyRemarks()
        {
            string memberCode =
                "/// <summary></summary>\n" +
                "/// <remarks></remarks>\n" +
                "public void M()\n" +
                "{\n" +
                "}\n";

            List<Finding> findings = CheckAssert.FindAllFindingsForMember(memberCode);

            FindingAsserts.HasExactlySmells(
                findings,
                XmlDocSmells.EmptyRemarks.ID,
                XmlDocSmells.EmptySummary.ID);
        }

        /// <summary>
        /// Ensures that duplicate summary and remarks tags are reported without an additional order finding.
        /// </summary>
        [Fact]
        public void DuplicateSummaryAndRemarks_ReportOnlyDuplicateSummaryAndRemarks()
        {
            string memberCode =
                "/// <summary>First.</summary>\n" +
                "/// <summary>Second.</summary>\n" +
                "/// <remarks>First remarks.</remarks>\n" +
                "/// <remarks>Second remarks.</remarks>\n" +
                "public void M()\n" +
                "{\n" +
                "}\n";

            List<Finding> findings = CheckAssert.FindAllFindingsForMember(memberCode);

            FindingAsserts.HasExactlySmells(
                findings,
                XmlDocSmells.DuplicateRemarksTag.ID,
                XmlDocSmells.DuplicateRemarksTag.ID,
                XmlDocSmells.DuplicateSummaryTag.ID,
                XmlDocSmells.DuplicateSummaryTag.ID);
        }
    }
}
