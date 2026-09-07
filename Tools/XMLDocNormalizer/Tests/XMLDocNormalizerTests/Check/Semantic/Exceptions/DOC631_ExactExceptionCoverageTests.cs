using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Semantic.Exception
{
    /// <summary>
    /// Tests exact exception-type matching for DOC631 findings.
    /// </summary>
    public sealed class DOC631_ExactExceptionCoverageTests
    {
        /// <summary>
        /// Ensures that a proven derived exception does not suppress DOC631 for a documented
        /// base exception when an independent transitive path remains uncertain.
        /// </summary>
        [Fact]
        public void ProvenDerivedException_WithDocumentedBaseAndUncertainty_ProducesDoc631()
        {
            List<Finding> findings = FindCoverageFindings("System.ArgumentException");

            Finding finding = Assert.Single(
                findings,
                finding => finding.Smell.ID == XmlDocSmells.ExceptionFlowNotDecidable.ID);

            Assert.Contains("System.ArgumentException", finding.Message, StringComparison.Ordinal);
        }

        /// <summary>
        /// Ensures that an exactly proven exception suppresses DOC631 for that documented type
        /// even when an independent transitive path remains uncertain.
        /// </summary>
        [Fact]
        public void ProvenExactException_WithIndependentUncertainty_DoesNotProduceDoc631()
        {
            List<Finding> findings = FindCoverageFindings("System.ArgumentNullException");

            Assert.DoesNotContain(
                findings,
                finding => finding.Smell.ID == XmlDocSmells.ExceptionFlowNotDecidable.ID);
        }

        /// <summary>
        /// Analyzes a proven <see cref="ArgumentNullException"/> together with an independent
        /// unresolved call and the supplied documented exception type.
        /// </summary>
        /// <param name="documentedType">The exception type used by the exception tag.</param>
        /// <returns>The semantic exception findings produced for the source.</returns>
        private static List<Finding> FindCoverageFindings(string documentedType)
        {
            string source =
                "public class TestClass\n" +
                "{\n" +
                "    /// <summary>Entry point.</summary>\n" +
                $"    /// <exception cref=\"{documentedType}\">Documented.</exception>\n" +
                "    public void M()\n" +
                "    {\n" +
                "        ThrowKnown();\n" +
                "        Unknown();\n" +
                "    }\n" +
                "\n" +
                "    private static void ThrowKnown()\n" +
                "    {\n" +
                "        throw new System.ArgumentNullException();\n" +
                "    }\n" +
                "\n" +
                "    private static extern void Unknown();\n" +
                "}\n";

            return CheckAssert.FindSemanticExceptionFindingsForSource(
                source,
                ExceptionAnalysisMode.ProjectTransitive);
        }
    }
}
