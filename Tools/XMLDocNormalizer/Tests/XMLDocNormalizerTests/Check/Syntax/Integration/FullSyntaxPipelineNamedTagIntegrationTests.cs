using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Syntax.Integration
{
    /// <summary>
    /// Integration tests for parameter and type-parameter tag interactions in the full syntax detector pipeline.
    /// </summary>
    public sealed class FullSyntaxPipelineNamedTagIntegrationTests
    {
        /// <summary>
        /// Ensures that missing parameter and type-parameter tags are reported without unrelated findings.
        /// </summary>
        [Fact]
        public void MissingParamAndTypeParamTags_ReportOnlyMissingNamedTags()
        {
            string memberCode =
                "/// <summary>Calculates the result.</summary>\n" +
                "/// <returns>The calculated result.</returns>\n" +
                "public int M<T>(T value, int count)\n" +
                "{\n" +
                "    return count;\n" +
                "}\n";

            List<Finding> findings = CheckAssert.FindAllFindingsForMember(memberCode);

            FindingAsserts.HasExactlySmells(
                findings,
                XmlDocSmells.MissingParamTag.ID,
                XmlDocSmells.MissingParamTag.ID,
                XmlDocSmells.MissingTypeParamTag.ID);
        }

        /// <summary>
        /// Ensures that empty parameter and type-parameter descriptions are reported precisely.
        /// </summary>
        [Fact]
        public void EmptyParamAndTypeParamTags_ReportOnlyEmptyNamedTags()
        {
            string memberCode =
                "/// <summary>Calculates the result.</summary>\n" +
                "/// <typeparam name=\"T\"></typeparam>\n" +
                "/// <param name=\"value\"></param>\n" +
                "/// <returns>The calculated result.</returns>\n" +
                "public int M<T>(T value)\n" +
                "{\n" +
                "    return 0;\n" +
                "}\n";

            List<Finding> findings = CheckAssert.FindAllFindingsForMember(memberCode);

            FindingAsserts.HasExactlySmells(
                findings,
                XmlDocSmells.EmptyParamDescription.ID,
                XmlDocSmells.EmptyTypeParamDescription.ID);
        }

        /// <summary>
        /// Ensures that unknown parameter and type-parameter tags are reported with the missing declared tags.
        /// </summary>
        [Fact]
        public void UnknownParamAndTypeParamTags_ReportUnknownAndMissingNamedTags()
        {
            string memberCode =
                "/// <summary>Calculates the result.</summary>\n" +
                "/// <typeparam name=\"TUnknown\">Unknown type parameter.</typeparam>\n" +
                "/// <param name=\"unknown\">Unknown parameter.</param>\n" +
                "/// <returns>The calculated result.</returns>\n" +
                "public int M<T>(T value)\n" +
                "{\n" +
                "    return 0;\n" +
                "}\n";

            List<Finding> findings = CheckAssert.FindAllFindingsForMember(memberCode);

            FindingAsserts.HasExactlySmells(
                findings,
                XmlDocSmells.MissingParamTag.ID,
                XmlDocSmells.MissingTypeParamTag.ID,
                XmlDocSmells.UnknownParamTag.ID,
                XmlDocSmells.UnknownTypeParamTag.ID);
        }

        /// <summary>
        /// Ensures that duplicate parameter and type-parameter tags are reported precisely.
        /// </summary>
        [Fact]
        public void DuplicateParamAndTypeParamTags_ReportOnlyDuplicateNamedTags()
        {
            string memberCode =
                "/// <summary>Calculates the result.</summary>\n" +
                "/// <typeparam name=\"T\">First type parameter description.</typeparam>\n" +
                "/// <typeparam name=\"T\">Second type parameter description.</typeparam>\n" +
                "/// <param name=\"value\">First value description.</param>\n" +
                "/// <param name=\"value\">Second value description.</param>\n" +
                "/// <returns>The calculated result.</returns>\n" +
                "public int M<T>(T value)\n" +
                "{\n" +
                "    return 0;\n" +
                "}\n";

            List<Finding> findings = CheckAssert.FindAllFindingsForMember(memberCode);

            FindingAsserts.HasExactlySmells(
                findings,
                XmlDocSmells.DuplicateParamTag.ID,
                XmlDocSmells.DuplicateTypeParamTag.ID);
        }

        /// <summary>
        /// Ensures that parameter and type-parameter order mismatches are reported precisely.
        /// </summary>
        [Fact]
        public void ParamAndTypeParamOrderMismatch_ReportOnlyOrderFindings()
        {
            string memberCode =
                "/// <summary>Calculates the result.</summary>\n" +
                "/// <typeparam name=\"U\">The second type.</typeparam>\n" +
                "/// <typeparam name=\"T\">The first type.</typeparam>\n" +
                "/// <param name=\"second\">The second value.</param>\n" +
                "/// <param name=\"first\">The first value.</param>\n" +
                "/// <returns>The calculated result.</returns>\n" +
                "public int M<T, U>(T first, U second)\n" +
                "{\n" +
                "    return 0;\n" +
                "}\n";

            List<Finding> findings = CheckAssert.FindAllFindingsForMember(memberCode);

            FindingAsserts.HasExactlySmells(
                findings,
                XmlDocSmells.ParamOrderMismatch.ID,
                XmlDocSmells.TypeParamOrderMismatch.ID);
        }

        /// <summary>
        /// Ensures that parameter and type-parameter references are validated in the full syntax pipeline.
        /// </summary>
        [Fact]
        public void ParamRefAndTypeParamRefIssues_ReportOnlyReferenceFindings()
        {
            string memberCode =
                "/// <summary>Uses <paramref name=\"missing\"/> and <typeparamref name=\"TMissing\"/>.</summary>\n" +
                "/// <typeparam name=\"T\">The input type.</typeparam>\n" +
                "/// <param name=\"value\">The input value.</param>\n" +
                "public void M<T>(T value)\n" +
                "{\n" +
                "}\n";

            List<Finding> findings = CheckAssert.FindAllFindingsForMember(memberCode);

            FindingAsserts.HasExactlySmells(
                findings,
                XmlDocSmells.UnknownParamRef.ID,
                XmlDocSmells.UnknownTypeParamRef.ID);
        }
    }
}
