using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Syntax.Inheritdoc
{
    /// <summary>
    /// Tests that inheritdoc suppresses missing detail-tag findings across the full syntax detector pipeline.
    /// </summary>
    public sealed class InheritdocMissingDetailTagSuppressionTests
    {
        /// <summary>
        /// Ensures that inheritdoc suppresses missing summary, parameter, type parameter and returns findings
        /// when the full syntax detector pipeline is executed.
        /// </summary>
        [Fact]
        public void Inheritdoc_DoesNotTriggerMissingDetailTagFindings_InFullSyntaxAnalysis()
        {
            string memberCode =
                "/// <inheritdoc/>\n" +
                "public int M<T>(T value)\n" +
                "{\n" +
                "    return 0;\n" +
                "}\n";

            List<Finding> findings = CheckAssert.FindAllFindingsForMember(memberCode);

            Assert.DoesNotContain(
                findings,
                finding => finding.Smell.ID == XmlDocSmells.MissingSummary.ID);

            Assert.DoesNotContain(
                findings,
                finding => finding.Smell.ID == XmlDocSmells.MissingParamTag.ID);

            Assert.DoesNotContain(
                findings,
                finding => finding.Smell.ID == XmlDocSmells.MissingTypeParamTag.ID);

            Assert.DoesNotContain(
                findings,
                finding => finding.Smell.ID == XmlDocSmells.MissingReturns.ID);
        }

        /// <summary>
        /// Ensures that explicitly present but empty detail tags are still reported even when inheritdoc is present.
        /// </summary>
        [Fact]
        public void Inheritdoc_DoesNotSuppressExplicitEmptyDetailTags_InFullSyntaxAnalysis()
        {
            string memberCode =
                "/// <inheritdoc/>\n" +
                "/// <param name=\"value\"></param>\n" +
                "/// <typeparam name=\"T\"></typeparam>\n" +
                "/// <returns></returns>\n" +
                "public int M<T>(T value)\n" +
                "{\n" +
                "    return 0;\n" +
                "}\n";

            List<Finding> findings = CheckAssert.FindAllFindingsForMember(memberCode);

            Assert.Contains(
                findings,
                finding => finding.Smell.ID == XmlDocSmells.EmptyParamDescription.ID);

            Assert.Contains(
                findings,
                finding => finding.Smell.ID == XmlDocSmells.EmptyTypeParamDescription.ID);

            Assert.Contains(
                findings,
                finding => finding.Smell.ID == XmlDocSmells.EmptyReturns.ID);
        }
    }
}
