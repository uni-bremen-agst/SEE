using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Semantic.Exception
{
    /// <summary>
    /// Tests DOC632 filtering behavior in ProjectTransitiveProjectExceptions mode.
    /// </summary>
    public sealed class DOC632_ProjectTransitiveProjectExceptionsTests
    {
        /// <summary>
        /// Ensures that a documented framework exception is ignored in ProjectTransitiveProjectExceptions mode.
        /// </summary>
        [Fact]
        public void DocumentedFrameworkException_IsIgnoredInProjectTransitiveProjectExceptionsMode()
        {
            string source =
                "public class TestClass\n" +
                "{\n" +
                "    /// <summary>Entry point.</summary>\n" +
                "    /// <exception cref=\"System.InvalidOperationException\">Ignored in this mode.</exception>\n" +
                "    public void M()\n" +
                "    {\n" +
                "        Helper();\n" +
                "    }\n" +
                "\n" +
                "    private void Helper()\n" +
                "    {\n" +
                "        throw new System.ArgumentException();\n" +
                "    }\n" +
                "}\n";

            List<Finding> findings =
                CheckAssert.FindSemanticExceptionFindingsForSource(
                    source,
                    ExceptionAnalysisMode.ProjectTransitiveProjectExceptions);

            Assert.DoesNotContain(
                findings,
                finding => finding.Smell.ID == XmlDocSmells.ExceptionTagWithoutTransitiveThrow.ID);
        }

        /// <summary>
        /// Ensures that a documented project-defined exception still produces DOC632
        /// when it is relevant in ProjectTransitiveProjectExceptions mode and not thrown.
        /// </summary>
        [Fact]
        public void DocumentedProjectDefinedException_IsReportedWhenNotThrown()
        {
            string source =
                "public sealed class CustomException : System.Exception { }\n" +
                "\n" +
                "public class TestClass\n" +
                "{\n" +
                "    /// <summary>Entry point.</summary>\n" +
                "    /// <exception cref=\"CustomException\">Relevant and uncovered.</exception>\n" +
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

            Finding finding = Assert.Single(findings);
            Assert.Equal(XmlDocSmells.ExceptionTagWithoutTransitiveThrow.ID, finding.Smell.ID);
            Assert.Contains("CustomException", finding.Message, StringComparison.Ordinal);
        }
    }
}
