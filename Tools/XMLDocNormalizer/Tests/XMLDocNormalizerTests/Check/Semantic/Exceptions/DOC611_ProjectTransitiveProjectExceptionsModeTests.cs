using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Semantic.Exception
{
    /// <summary>
    /// Tests the ProjectTransitiveProjectExceptions mode.
    /// </summary>
    public sealed class DOC611_ProjectTransitiveProjectExceptionsModeTests
    {
        /// <summary>
        /// Ensures that transitively thrown framework exceptions are ignored in ProjectTransitiveProjectExceptions mode.
        /// </summary>
        [Fact]
        public void FrameworkException_IsIgnoredInProjectTransitiveProjectExceptionsMode()
        {
            string source =
                "public class TestClass\n" +
                "{\n" +
                "    /// <summary>Entry point.</summary>\n" +
                "    public void M()\n" +
                "    {\n" +
                "        Helper();\n" +
                "    }\n" +
                "\n" +
                "    private void Helper()\n" +
                "    {\n" +
                "        throw new System.InvalidOperationException();\n" +
                "    }\n" +
                "}\n";

            List<Finding> findings =
                CheckAssert.FindSemanticExceptionFindingsForSource(
                    source,
                    ExceptionAnalysisMode.ProjectTransitiveProjectExceptions);

            Assert.DoesNotContain(
                findings,
                finding => finding.Smell.ID == XmlDocSmells.MissingTransitiveExceptionDocumentation.ID);
        }

        /// <summary>
        /// Ensures that transitively thrown project-defined exceptions are still reported.
        /// </summary>
        [Fact]
        public void ProjectDefinedException_IsReportedInProjectTransitiveProjectExceptionsMode()
        {
            string source =
                "public class CustomException : System.Exception { }\n" +
                "\n" +
                "public class TestClass\n" +
                "{\n" +
                "    /// <summary>Entry point.</summary>\n" +
                "    public void M()\n" +
                "    {\n" +
                "        Helper();\n" +
                "    }\n" +
                "\n" +
                "    private void Helper()\n" +
                "    {\n" +
                "        throw new CustomException();\n" +
                "    }\n" +
                "}\n";

            List<Finding> findings =
                CheckAssert.FindSemanticExceptionFindingsForSource(
                    source,
                    ExceptionAnalysisMode.ProjectTransitiveProjectExceptions);

            Finding finding = Assert.Single(findings);
            Assert.Equal(XmlDocSmells.MissingTransitiveExceptionDocumentation.ID, finding.Smell.ID);
            Assert.Contains("CustomException", finding.Message, StringComparison.Ordinal);
        }
    }
}
