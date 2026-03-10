using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Semantic.Exception
{
    /// <summary>
    /// Tests multiple and combined semantic exception findings involving DOC610.
    /// </summary>
    public sealed class DOC610_MultipleAndCombinedFindingsTests
    {
        /// <summary>
        /// Ensures that multiple undocumented thrown exceptions each produce their own DOC610 finding.
        /// </summary>
        [Fact]
        public void MultipleUndocumentedThrownExceptions_AllProduceDoc610()
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

            List<Finding> findings = CheckAssert.FindSemanticExceptionFindingsForSource(source);

            Assert.Equal(2, findings.Count);
            Assert.All(findings, finding =>
            {
                Assert.Equal(XmlDocSmells.MissingExceptionTag.ID, finding.Smell.ID);
                Assert.Equal("exception", finding.TagName);
            });
        }

        /// <summary>
        /// Ensures that when one documented exception is not thrown and one thrown exception is not documented,
        /// both DOC630 and DOC610 are reported.
        /// </summary>
        [Fact]
        public void MismatchedDocumentedAndThrownExceptions_ReportDoc630AndDoc610()
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

            List<Finding> findings = CheckAssert.FindSemanticExceptionFindingsForSource(source);

            Assert.Equal(2, findings.Count);
            Assert.Contains(findings, finding => finding.Smell.ID == XmlDocSmells.ExceptionTagWithoutDirectThrow.ID);
            Assert.Contains(findings, finding => finding.Smell.ID == XmlDocSmells.MissingExceptionTag.ID);
        }

        /// <summary>
        /// Ensures that no DOC610 finding is produced when all transitively thrown exceptions
        /// are covered by documentation.
        /// </summary>
        [Fact]
        public void AllThrownExceptionsCoveredByDocumentation_ProducesNoDoc610()
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

            List<Finding> findings = CheckAssert.FindSemanticExceptionFindingsForSource(source);

            Assert.DoesNotContain(
                findings,
                finding => finding.Smell.ID == XmlDocSmells.MissingExceptionTag.ID);
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

            List<Finding> findings = CheckAssert.FindSemanticExceptionFindingsForSource(source);

            Assert.DoesNotContain(
                findings,
                finding => finding.Smell.ID == XmlDocSmells.MissingExceptionTag.ID);
        }
    }
}
