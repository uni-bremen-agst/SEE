using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Semantic.Exception
{
    /// <summary>
    /// Tests conservative handling of catch filters in transitive exception analysis.
    /// </summary>
    public sealed class DOC611_CatchFilterExceptionTests
    {
        /// <summary>
        /// Ensures that a filtered catch-clause does not suppress the exception flow conservatively.
        /// </summary>
        [Fact]
        public void TransitivelyThrownException_CaughtWithFilter_IsStillDetected()
        {
            string source =
                "public class TestClass\n" +
                "{\n" +
                "    /// <summary>Test.</summary>\n" +
                "    public void M()\n" +
                "    {\n" +
                "        try\n" +
                "        {\n" +
                "            Helper();\n" +
                "        }\n" +
                "        catch (System.InvalidOperationException) when (ShouldHandle())\n" +
                "        {\n" +
                "        }\n" +
                "    }\n" +
                "\n" +
                "    private static bool ShouldHandle()\n" +
                "    {\n" +
                "        return true;\n" +
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
                    ExceptionAnalysisMode.ProjectTransitive);

            Finding finding = Assert.Single(findings);
            Assert.Equal(XmlDocSmells.MissingTransitiveExceptionDocumentation.ID, finding.Smell.ID);
            Assert.Contains("System.InvalidOperationException", finding.Message, StringComparison.Ordinal);
        }

        /// <summary>
        /// Ensures that a filtered catch-all clause is also treated conservatively and does not suppress the flow.
        /// </summary>
        [Fact]
        public void TransitivelyThrownException_CaughtByFilteredCatchAll_IsStillDetected()
        {
            string source =
                "public class TestClass\n" +
                "{\n" +
                "    /// <summary>Test.</summary>\n" +
                "    public void M()\n" +
                "    {\n" +
                "        try\n" +
                "        {\n" +
                "            Helper();\n" +
                "        }\n" +
                "        catch when (ShouldHandle())\n" +
                "        {\n" +
                "        }\n" +
                "    }\n" +
                "\n" +
                "    private static bool ShouldHandle()\n" +
                "    {\n" +
                "        return true;\n" +
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
                    ExceptionAnalysisMode.ProjectTransitive);

            Finding finding = Assert.Single(findings);
            Assert.Equal(XmlDocSmells.MissingTransitiveExceptionDocumentation.ID, finding.Smell.ID);
            Assert.Contains("System.InvalidOperationException", finding.Message, StringComparison.Ordinal);
        }
    }
}
