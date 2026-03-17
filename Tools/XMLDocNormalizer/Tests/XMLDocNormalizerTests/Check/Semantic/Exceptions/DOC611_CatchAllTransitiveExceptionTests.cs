using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Semantic.Exception
{
    /// <summary>
    /// Tests that catch-all handlers suppress transitively thrown exceptions.
    /// </summary>
    public sealed class DOC611_CatchAllTransitiveExceptionTests
    {
        /// <summary>
        /// Ensures that a transitively thrown exception caught by a catch-all clause is not reported.
        /// </summary>
        [Fact]
        public void TransitivelyThrownException_CaughtByCatchAll_IsNotDetected()
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
                "        catch\n" +
                "        {\n" +
                "        }\n" +
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
                finding => finding.Smell.ID == XmlDocSmells.MissingTransitiveExceptionDocumentation.ID);
        }

        /// <summary>
        /// Ensures that a transitively thrown exception caught by catch(System.Exception) is not reported.
        /// </summary>
        [Fact]
        public void TransitivelyThrownException_CaughtBySystemExceptionCatch_IsNotDetected()
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
                "        catch (System.Exception)\n" +
                "        {\n" +
                "        }\n" +
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
                finding => finding.Smell.ID == XmlDocSmells.MissingTransitiveExceptionDocumentation.ID);
        }
    }
}
