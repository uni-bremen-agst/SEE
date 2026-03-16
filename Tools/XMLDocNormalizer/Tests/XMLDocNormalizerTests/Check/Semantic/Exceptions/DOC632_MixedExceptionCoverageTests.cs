using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Semantic.Exception
{
    /// <summary>
    /// Tests mixed coverage scenarios for DOC632 in transitive exception analysis.
    /// </summary>
    public sealed class DOC632_MixedExceptionCoverageTests
    {
        /// <summary>
        /// Ensures that when one documented exception is covered transitively and another is not,
        /// only the uncovered one produces DOC632.
        /// </summary>
        [Fact]
        public void MixedDocumentedExceptions_ReportOnlyUncoveredException()
        {
            string source =
                "public class TestClass\n" +
                "{\n" +
                "    /// <summary>Entry point.</summary>\n" +
                "    /// <exception cref=\"System.InvalidOperationException\">Covered.</exception>\n" +
                "    /// <exception cref=\"System.ArgumentException\">Not covered.</exception>\n" +
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

            Finding finding = Assert.Single(findings);
            Assert.Equal(XmlDocSmells.ExceptionTagWithoutTransitiveThrow.ID, finding.Smell.ID);
            Assert.Equal("exception", finding.TagName);
            Assert.Contains("System.ArgumentException", finding.Message, StringComparison.Ordinal);
        }

        /// <summary>
        /// Ensures that a documented base exception type covers a transitively thrown derived exception.
        /// </summary>
        [Fact]
        public void DocumentedBaseException_CoversDerivedThrownException()
        {
            string source =
                "public class TestClass\n" +
                "{\n" +
                "    /// <summary>Entry point.</summary>\n" +
                "    /// <exception cref=\"System.Exception\">Base type.</exception>\n" +
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

            Assert.DoesNotContain(
                findings,
                finding => finding.Smell.ID == XmlDocSmells.ExceptionTagWithoutTransitiveThrow.ID);
        }

        /// <summary>
        /// Ensures that multiple uncovered documented exceptions each produce a DOC632 finding.
        /// The actually thrown exception is documented as well so that no DOC611 is produced.
        /// </summary>
        [Fact]
        public void MultipleUncoveredDocumentedExceptions_AllProduceFindings()
        {
            string source =
                "public class TestClass\n" +
                "{\n" +
                "    /// <summary>Entry point.</summary>\n" +
                "    /// <exception cref=\"System.InvalidOperationException\">Covered.</exception>\n" +
                "    /// <exception cref=\"System.ArgumentException\">Not covered.</exception>\n" +
                "    /// <exception cref=\"System.InvalidCastException\">Also not covered.</exception>\n" +
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
            Assert.All(findings, finding =>
            {
                Assert.Equal(XmlDocSmells.ExceptionTagWithoutTransitiveThrow.ID, finding.Smell.ID);
                Assert.Equal("exception", finding.TagName);
            });
        }

        /// <summary>
        /// Ensures that no DOC632 finding is produced when all documented exceptions
        /// are covered by the transitive throw flow.
        /// </summary>
        [Fact]
        public void AllDocumentedExceptionsCovered_ProducesNoDoc632()
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
                finding => finding.Smell.ID == XmlDocSmells.ExceptionTagWithoutTransitiveThrow.ID);
        }
    }
}
