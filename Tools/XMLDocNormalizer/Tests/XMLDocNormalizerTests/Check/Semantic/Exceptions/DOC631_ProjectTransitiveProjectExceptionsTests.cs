using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Semantic.Exception
{
    /// <summary>
    /// Tests DOC631 behavior in ProjectTransitiveProjectExceptions mode.
    /// </summary>
    public sealed class DOC631_ProjectTransitiveProjectExceptionsTests
    {
        /// <summary>
        /// Ensures that DOC631 is reported for a project-defined documented exception
        /// when the transitive analysis is not decidable for that exception.
        /// </summary>
        [Fact]
        public void ProjectDefinedDocumentedException_WithUncertainFlow_IsReportedAsDoc631()
        {
            string source =
                "public sealed class CustomException : System.Exception { }\n" +
                "\n" +
                "public class TestClass\n" +
                "{\n" +
                "    /// <summary>Entry point.</summary>\n" +
                "    /// <exception cref=\"CustomException\">Might be thrown transitively.</exception>\n" +
                "    public void M()\n" +
                "    {\n" +
                "        Unknown();\n" +
                "    }\n" +
                "\n" +
                "    private static extern void Unknown();\n" +
                "}\n";

            List<Finding> findings =
                CheckAssert.FindSemanticExceptionFindingsForSource(
                    source,
                    ExceptionAnalysisMode.ProjectTransitiveProjectExceptions);

            Finding finding = Assert.Single(findings);
            Assert.Equal(XmlDocSmells.ExceptionFlowNotDecidable.ID, finding.Smell.ID);
            Assert.Equal("exception", finding.TagName);
            Assert.Contains("CustomException", finding.Message, StringComparison.Ordinal);
        }

        /// <summary>
        /// Ensures that DOC631 is not reported when a project-defined documented exception
        /// is already covered by a proven transitive throw.
        /// </summary>
        [Fact]
        public void CoveredProjectDefinedException_DoesNotProduceDoc631()
        {
            string source =
                "public sealed class CustomException : System.Exception { }\n" +
                "\n" +
                "public class TestClass\n" +
                "{\n" +
                "    /// <summary>Entry point.</summary>\n" +
                "    /// <exception cref=\"CustomException\">Covered.</exception>\n" +
                "    public void M()\n" +
                "    {\n" +
                "        Helper();\n" +
                "        Unknown();\n" +
                "    }\n" +
                "\n" +
                "    private void Helper()\n" +
                "    {\n" +
                "        throw new CustomException();\n" +
                "    }\n" +
                "\n" +
                "    private static extern void Unknown();\n" +
                "}\n";

            List<Finding> findings =
                CheckAssert.FindSemanticExceptionFindingsForSource(
                    source,
                    ExceptionAnalysisMode.ProjectTransitiveProjectExceptions);

            Assert.DoesNotContain(
                findings,
                finding => finding.Smell.ID == XmlDocSmells.ExceptionFlowNotDecidable.ID);
        }
    }
}
