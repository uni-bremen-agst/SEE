using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Semantic.Exception
{
    /// <summary>
    /// Tests additional transitive exception-flow scenarios used by DOC630 and DOC610.
    /// </summary>
    public sealed class DOC630_AdvancedTransitiveExceptionFlowTests
    {
        /// <summary>
        /// Ensures that a documented exception does not trigger DOC630 when it is thrown
        /// by a transitively accessed indexer getter.
        /// </summary>
        [Fact]
        public void DocumentedException_ThrownByIndexerGetter_IsNotDetected()
        {
            string source =
                "public class Helper\n" +
                "{\n" +
                "    public int this[int index]\n" +
                "    {\n" +
                "        get\n" +
                "        {\n" +
                "            throw new System.InvalidOperationException();\n" +
                "        }\n" +
                "    }\n" +
                "}\n" +
                "\n" +
                "public class TestClass\n" +
                "{\n" +
                "    /// <summary>Reads an indexed value.</summary>\n" +
                "    /// <exception cref=\"System.InvalidOperationException\">Thrown transitively.</exception>\n" +
                "    public int M()\n" +
                "    {\n" +
                "        return ReadValue();\n" +
                "    }\n" +
                "\n" +
                "    private int ReadValue()\n" +
                "    {\n" +
                "        Helper helper = new Helper();\n" +
                "        return helper[0];\n" +
                "    }\n" +
                "}\n";

            List<Finding> findings = CheckAssert.FindSemanticExceptionFindingsForSource(source);

            Assert.DoesNotContain(
                findings,
                finding => finding.Smell.ID == XmlDocSmells.ExceptionTagWithoutDirectThrow.ID);
        }

        /// <summary>
        /// Ensures that a documented exception does not trigger DOC630 when it is thrown
        /// by a transitively accessed expression-bodied property getter.
        /// </summary>
        [Fact]
        public void DocumentedException_ThrownByExpressionBodiedPropertyGetter_IsNotDetected()
        {
            string source =
                "public class Helper\n" +
                "{\n" +
                "    public int Value => throw new System.InvalidOperationException();\n" +
                "}\n" +
                "\n" +
                "public class TestClass\n" +
                "{\n" +
                "    /// <summary>Reads a value.</summary>\n" +
                "    /// <exception cref=\"System.InvalidOperationException\">Thrown transitively.</exception>\n" +
                "    public int M()\n" +
                "    {\n" +
                "        return ReadValue();\n" +
                "    }\n" +
                "\n" +
                "    private int ReadValue()\n" +
                "    {\n" +
                "        Helper helper = new Helper();\n" +
                "        return helper.Value;\n" +
                "    }\n" +
                "}\n";

            List<Finding> findings = CheckAssert.FindSemanticExceptionFindingsForSource(source);

            Assert.DoesNotContain(
                findings,
                finding => finding.Smell.ID == XmlDocSmells.ExceptionTagWithoutDirectThrow.ID);
        }

        /// <summary>
        /// Ensures that a documented exception does not trigger DOC630 when it is thrown
        /// through a constructor that transitively calls another throwing method.
        /// </summary>
        [Fact]
        public void DocumentedException_ThrownFromConstructorCallChain_IsNotDetected()
        {
            string source =
                "public class Helper\n" +
                "{\n" +
                "    public Helper()\n" +
                "    {\n" +
                "        Initialize();\n" +
                "    }\n" +
                "\n" +
                "    private void Initialize()\n" +
                "    {\n" +
                "        throw new System.InvalidOperationException();\n" +
                "    }\n" +
                "}\n" +
                "\n" +
                "public class TestClass\n" +
                "{\n" +
                "    /// <summary>Creates a helper.</summary>\n" +
                "    /// <exception cref=\"System.InvalidOperationException\">Thrown transitively.</exception>\n" +
                "    public void M()\n" +
                "    {\n" +
                "        CreateHelper();\n" +
                "    }\n" +
                "\n" +
                "    private void CreateHelper()\n" +
                "    {\n" +
                "        _ = new Helper();\n" +
                "    }\n" +
                "}\n";

            List<Finding> findings = CheckAssert.FindSemanticExceptionFindingsForSource(source);

            Assert.DoesNotContain(
                findings,
                finding => finding.Smell.ID == XmlDocSmells.ExceptionTagWithoutDirectThrow.ID);
        }

        /// <summary>
        /// Ensures that mismatched documented and transitively thrown exceptions
        /// produce both DOC630 and DOC610.
        /// </summary>
        [Fact]
        public void DocumentedException_NotCoveredByTransitiveFlow_IsDetected()
        {
            string source =
                "public class TestClass\n" +
                "{\n" +
                "    /// <summary>Entry point.</summary>\n" +
                "    /// <exception cref=\"System.ArgumentException\">Not thrown.</exception>\n" +
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
    }
}
