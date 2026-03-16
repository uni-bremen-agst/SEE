using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Semantic.Exception
{
    /// <summary>
    /// Tests multiple and combined semantic exception findings involving DOC610, DOC611, DOC630 and DOC632.
    /// </summary>
    public sealed class DOC611_632_MultipleAndCombinedFindingsTests
    {
        /// <summary>
        /// Ensures that multiple undocumented transitively thrown exceptions each produce their own DOC611 finding.
        /// </summary>
        [Fact]
        public void MultipleUndocumentedThrownExceptions_AllProduceDoc611()
        {
            string source =
                "public class TestClass\n" +
                "{\n" +
                "    /// <summary>Entry point.</summary>\n" +
                "    public void M()\n" +
                "    {\n" +
                "        First();\n" +
                "        Second();\n" +
                "    }\n" +
                "\n" +
                "    private void First()\n" +
                "    {\n" +
                "        throw new System.InvalidOperationException();\n" +
                "    }\n" +
                "\n" +
                "    private void Second()\n" +
                "    {\n" +
                "        throw new System.ArgumentException();\n" +
                "    }\n" +
                "}\n";

            List<Finding> findings =
                CheckAssert.FindSemanticExceptionFindingsForSource(source, ExceptionAnalysisMode.ProjectTransitive);

            Assert.Equal(2, findings.Count);
            Assert.All(findings, finding =>
            {
                Assert.Equal(XmlDocSmells.MissingTransitiveExceptionDocumentation.ID, finding.Smell.ID);
                Assert.Equal("exception", finding.TagName);
            });
        }

        /// <summary>
        /// Ensures that when one documented exception is not thrown transitively and one thrown exception is not documented,
        /// both DOC632 and DOC611 are reported.
        /// </summary>
        [Fact]
        public void MismatchedDocumentedAndThrownExceptions_ReportDoc632AndDoc611()
        {
            string source =
                "public class TestClass\n" +
                "{\n" +
                "    /// <summary>Entry point.</summary>\n" +
                "    /// <exception cref=\"System.ArgumentException\">Documented but not thrown.</exception>\n" +
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
                CheckAssert.FindSemanticExceptionFindingsForSource(source, ExceptionAnalysisMode.ProjectTransitive);

            Assert.Equal(2, findings.Count);
            Assert.Contains(findings, finding => finding.Smell.ID == XmlDocSmells.ExceptionTagWithoutTransitiveThrow.ID);
            Assert.Contains(findings, finding => finding.Smell.ID == XmlDocSmells.MissingTransitiveExceptionDocumentation.ID);
        }

        /// <summary>
        /// Ensures that no DOC611 finding is produced when all transitively thrown exceptions
        /// are covered by documentation.
        /// </summary>
        [Fact]
        public void AllThrownExceptionsCoveredByDocumentation_ProducesNoDoc611()
        {
            string source =
                "public class TestClass\n" +
                "{\n" +
                "    /// <summary>Entry point.</summary>\n" +
                "    /// <exception cref=\"System.InvalidOperationException\">Covered.</exception>\n" +
                "    /// <exception cref=\"System.ArgumentException\">Covered.</exception>\n" +
                "    public void M()\n" +
                "    {\n" +
                "        First();\n" +
                "        Second();\n" +
                "    }\n" +
                "\n" +
                "    private void First()\n" +
                "    {\n" +
                "        throw new System.InvalidOperationException();\n" +
                "    }\n" +
                "\n" +
                "    private void Second()\n" +
                "    {\n" +
                "        throw new System.ArgumentException();\n" +
                "    }\n" +
                "}\n";

            List<Finding> findings =
                CheckAssert.FindSemanticExceptionFindingsForSource(source, ExceptionAnalysisMode.ProjectTransitive);

            Assert.DoesNotContain(
                findings,
                finding => finding.Smell.ID == XmlDocSmells.MissingTransitiveExceptionDocumentation.ID);
        }

        /// <summary>
        /// Ensures that a documented base exception type covers multiple derived transitively thrown exceptions.
        /// </summary>
        [Fact]
        public void DocumentedBaseException_CoversMultipleDerivedThrownExceptions()
        {
            string source =
                "public class TestClass\n" +
                "{\n" +
                "    /// <summary>Entry point.</summary>\n" +
                "    /// <exception cref=\"System.Exception\">Covered by base type.</exception>\n" +
                "    public void M()\n" +
                "    {\n" +
                "        First();\n" +
                "        Second();\n" +
                "    }\n" +
                "\n" +
                "    private void First()\n" +
                "    {\n" +
                "        throw new System.InvalidOperationException();\n" +
                "    }\n" +
                "\n" +
                "    private void Second()\n" +
                "    {\n" +
                "        throw new System.ArgumentException();\n" +
                "    }\n" +
                "}\n";

            List<Finding> findings =
                CheckAssert.FindSemanticExceptionFindingsForSource(source, ExceptionAnalysisMode.ProjectTransitive);

            Assert.DoesNotContain(
                findings,
                finding => finding.Smell.ID == XmlDocSmells.MissingTransitiveExceptionDocumentation.ID);
        }
    }
}
