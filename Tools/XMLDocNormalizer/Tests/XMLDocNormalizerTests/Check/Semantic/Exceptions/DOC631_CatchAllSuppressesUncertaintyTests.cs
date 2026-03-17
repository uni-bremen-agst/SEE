using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Semantic.Exception
{
    /// <summary>
    /// Tests that catch-all handlers suppress transitive uncertainty when the try-block cannot escape.
    /// </summary>
    public sealed class DOC631_CatchAllSuppressesUncertaintyTests
    {
        /// <summary>
        /// Ensures that a catch-all clause suppresses DOC631 for uncertain targets inside the try-block.
        /// </summary>
        [Fact]
        public void UncertainTransitiveFlow_CaughtByCatchAll_DoesNotProduceDoc631()
        {
            string source =
                "public sealed class CustomException : System.Exception { }\n" +
                "\n" +
                "public class TestClass\n" +
                "{\n" +
                "    /// <summary>Test.</summary>\n" +
                "    /// <exception cref=\"CustomException\">Would only matter if the try escaped.</exception>\n" +
                "    public void M()\n" +
                "    {\n" +
                "        try\n" +
                "        {\n" +
                "            Unknown();\n" +
                "        }\n" +
                "        catch\n" +
                "        {\n" +
                "        }\n" +
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

        /// <summary>
        /// Ensures that a catch(System.Exception) clause also suppresses DOC631 for uncertain targets.
        /// </summary>
        [Fact]
        public void UncertainTransitiveFlow_CaughtBySystemExceptionCatch_DoesNotProduceDoc631()
        {
            string source =
                "public sealed class CustomException : System.Exception { }\n" +
                "\n" +
                "public class TestClass\n" +
                "{\n" +
                "    /// <summary>Test.</summary>\n" +
                "    /// <exception cref=\"CustomException\">Would only matter if the try escaped.</exception>\n" +
                "    public void M()\n" +
                "    {\n" +
                "        try\n" +
                "        {\n" +
                "            Unknown();\n" +
                "        }\n" +
                "        catch (System.Exception)\n" +
                "        {\n" +
                "        }\n" +
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
