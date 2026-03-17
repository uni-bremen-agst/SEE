using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Semantic.Exception
{
    /// <summary>
    /// Tests that transitively thrown exceptions caught inside the same member do not produce DOC611.
    /// </summary>
    public sealed class DOC611_CaughtTransitiveExceptionTests
    {
        /// <summary>
        /// Ensures that a transitively thrown exception caught by one of multiple catch-clauses is not reported.
        /// </summary>
        [Fact]
        public void TransitivelyThrownException_CaughtByMatchingCatch_IsNotDetected()
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
                "        catch (System.ArgumentException)\n" +
                "        {\n" +
                "        }\n" +
                "        catch (System.InvalidOperationException)\n" +
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
        /// Ensures that a transitively thrown exception remains relevant when the matching catch-clause rethrows it.
        /// </summary>
        [Fact]
        public void TransitivelyThrownException_RethrownFromCatch_IsStillDetected()
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
                "        catch (System.InvalidOperationException)\n" +
                "        {\n" +
                "            throw;\n" +
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

            Finding finding = Assert.Single(findings);
            Assert.Equal(XmlDocSmells.MissingTransitiveExceptionDocumentation.ID, finding.Smell.ID);
        }
    }
}
