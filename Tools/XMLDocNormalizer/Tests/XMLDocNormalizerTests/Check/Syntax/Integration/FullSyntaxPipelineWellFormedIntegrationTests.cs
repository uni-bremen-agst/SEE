using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Syntax.Integration
{
    /// <summary>
    /// Integration tests for malformed and invalid XML documentation tag interactions in the full syntax pipeline.
    /// </summary>
    public sealed class FullSyntaxPipelineWellFormedIntegrationTests
    {
        /// <summary>
        /// Ensures that an unknown documentation tag is reported precisely.
        /// </summary>
        [Fact]
        public void UnknownTag_ReportsOnlyUnknownTag()
        {
            string memberCode =
                "/// <summary>Runs the operation.</summary>\n" +
                "/// <unknown>Unexpected documentation.</unknown>\n" +
                "public void M()\n" +
                "{\n" +
                "}\n";

            List<Finding> findings = CheckAssert.FindAllFindingsForMember(memberCode);

            FindingAsserts.HasExactlySmells(findings, XmlDocSmells.UnknownTag.ID);
        }

        /// <summary>
        /// Ensures that a param tag without a name is reported together with the missing declared parameter.
        /// </summary>
        [Fact]
        public void ParamWithoutName_ReportsParamMissingNameAndMissingDeclaredParam()
        {
            string memberCode =
                "/// <summary>Runs the operation.</summary>\n" +
                "/// <param>Missing name.</param>\n" +
                "public void M(int value)\n" +
                "{\n" +
                "}\n";

            List<Finding> findings = CheckAssert.FindAllFindingsForMember(memberCode);

            FindingAsserts.HasExactlySmells(
                findings,
                XmlDocSmells.MissingParamTag.ID,
                XmlDocSmells.ParamMissingName.ID);
        }

        /// <summary>
        /// Ensures that a typeparam tag without a name is reported together with the missing declared type parameter.
        /// </summary>
        [Fact]
        public void TypeParamWithoutName_ReportsTypeParamMissingNameAndMissingDeclaredTypeParam()
        {
            string memberCode =
                "/// <summary>Runs the operation.</summary>\n" +
                "/// <typeparam>Missing name.</typeparam>\n" +
                "public void M<T>()\n" +
                "{\n" +
                "}\n";

            List<Finding> findings = CheckAssert.FindAllFindingsForMember(memberCode);

            FindingAsserts.HasExactlySmells(
                findings,
                XmlDocSmells.MissingTypeParamTag.ID,
                XmlDocSmells.TypeParamMissingName.ID);
        }

        /// <summary>
        /// Ensures that non-empty paramref and typeparamref tags are reported precisely.
        /// </summary>
        [Fact]
        public void NonEmptyParamRefAndTypeParamRef_ReportOnlyNotEmptyFindings()
        {
            string memberCode =
                "/// <summary>Uses <paramref name=\"value\">text</paramref> and <typeparamref name=\"T\">text</typeparamref>.</summary>\n" +
                "/// <typeparam name=\"T\">The input type.</typeparam>\n" +
                "/// <param name=\"value\">The input value.</param>\n" +
                "public void M<T>(T value)\n" +
                "{\n" +
                "}\n";

            List<Finding> findings = CheckAssert.FindAllFindingsForMember(memberCode);

            FindingAsserts.HasExactlySmells(
                findings,
                XmlDocSmells.ParamRefNotEmpty.ID,
                XmlDocSmells.TypeParamRefNotEmpty.ID);
        }

        /// <summary>
        /// Ensures that missing paramref and typeparamref names are reported precisely.
        /// </summary>
        [Fact]
        public void ParamRefAndTypeParamRefWithoutName_ReportOnlyMissingReferenceNames()
        {
            string memberCode =
                "/// <summary>Uses <paramref/> and <typeparamref/>.</summary>\n" +
                "/// <typeparam name=\"T\">The input type.</typeparam>\n" +
                "/// <param name=\"value\">The input value.</param>\n" +
                "public void M<T>(T value)\n" +
                "{\n" +
                "}\n";

            List<Finding> findings = CheckAssert.FindAllFindingsForMember(memberCode);

            FindingAsserts.HasExactlySmells(
                findings,
                XmlDocSmells.ParamRefMissingName.ID,
                XmlDocSmells.TypeParamRefMissingName.ID);
        }

        /// <summary>
        /// Ensures that invalid paramref and typeparamref attributes are reported precisely.
        /// </summary>
        [Fact]
        public void ParamRefAndTypeParamRefWithInvalidAttributes_ReportOnlyInvalidReferenceAttributes()
        {
            string memberCode =
                "/// <summary>Uses <paramref name=\"value\" unknown=\"x\"/> and <typeparamref name=\"T\" unknown=\"x\"/>.</summary>\n" +
                "/// <typeparam name=\"T\">The input type.</typeparam>\n" +
                "/// <param name=\"value\">The input value.</param>\n" +
                "public void M<T>(T value)\n" +
                "{\n" +
                "}\n";

            List<Finding> findings = CheckAssert.FindAllFindingsForMember(memberCode);

            FindingAsserts.HasExactlySmells(
                findings,
                XmlDocSmells.InvalidParamRefAttribute.ID,
                XmlDocSmells.InvalidTypeParamRefAttribute.ID);
        }

        /// <summary>
        /// Ensures that a top-level tag order mismatch is reported precisely.
        /// </summary>
        [Fact]
        public void TopLevelTagOrderMismatch_ReportsOnlyOrderMismatch()
        {
            string memberCode =
                "/// <summary>Calculates the result.</summary>\n" +
                "/// <returns>The result.</returns>\n" +
                "/// <param name=\"value\">The input value.</param>\n" +
                "public int M(int value)\n" +
                "{\n" +
                "    return value;\n" +
                "}\n";

            List<Finding> findings = CheckAssert.FindAllFindingsForMember(memberCode);

            FindingAsserts.HasExactlySmells(findings, XmlDocSmells.TopLevelTagOrderMismatch.ID);
        }
    }
}
