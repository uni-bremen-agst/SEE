using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Semantic.Exception
{
    /// <summary>
    /// Tests DOC630 for exceptions that are thrown only through transitive execution paths.
    /// </summary>
    public sealed class DOC630_TransitiveExceptionFlowTests
    {
        /// <summary>
        /// Ensures that a documented exception does not trigger DOC630 when it is thrown
        /// three calls deep in the transitive call chain.
        /// </summary>
        [Fact]
        public void DocumentedException_ThrownThreeCallsDeep_IsNotDetected()
        {
            string source =
                "public class TestClass\n" +
                "{\n" +
                "    /// <summary>Entry point.</summary>\n" +
                "    /// <exception cref=\"System.InvalidOperationException\">Thrown transitively.</exception>\n" +
                "    public void A()\n" +
                "    {\n" +
                "        B();\n" +
                "    }\n" +
                "\n" +
                "    private void B()\n" +
                "    {\n" +
                "        C();\n" +
                "    }\n" +
                "\n" +
                "    private void C()\n" +
                "    {\n" +
                "        throw new System.InvalidOperationException();\n" +
                "    }\n" +
                "}\n";

            List<Finding> findings = CheckAssert.FindSemanticExceptionFindingsForSource(source);

            Assert.DoesNotContain(
                findings,
                finding => finding.Smell.ID == XmlDocSmells.ExceptionTagWithoutDirectThrow.ID);
        }

        /// <summary>
        /// Ensures that recursive or cyclic call graphs do not cause DOC630
        /// when the documented exception is still thrown transitively.
        /// </summary>
        [Fact]
        public void DocumentedException_InRecursiveCallGraph_IsNotDetected()
        {
            string source =
                "public class TestClass\n" +
                "{\n" +
                "    /// <summary>Entry point.</summary>\n" +
                "    /// <exception cref=\"System.InvalidOperationException\">Thrown transitively.</exception>\n" +
                "    public void A()\n" +
                "    {\n" +
                "        B();\n" +
                "    }\n" +
                "\n" +
                "    private void B()\n" +
                "    {\n" +
                "        C();\n" +
                "    }\n" +
                "\n" +
                "    private void C()\n" +
                "    {\n" +
                "        D();\n" +
                "    }\n" +
                "\n" +
                "    private void D()\n" +
                "    {\n" +
                "        B();\n" +
                "        throw new System.InvalidOperationException();\n" +
                "    }\n" +
                "}\n";

            List<Finding> findings = CheckAssert.FindSemanticExceptionFindingsForSource(source);

            Assert.DoesNotContain(
                findings,
                finding => finding.Smell.ID == XmlDocSmells.ExceptionTagWithoutDirectThrow.ID);
        }

        /// <summary>
        /// Ensures that a documented exception does not trigger DOC630 when it is thrown
        /// by a transitively invoked constructor.
        /// </summary>
        [Fact]
        public void DocumentedException_ThrownByConstructor_IsNotDetected()
        {
            string source =
                "public class Helper\n" +
                "{\n" +
                "    public Helper()\n" +
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
        /// Ensures that a documented exception does not trigger DOC630 when it is thrown
        /// by a transitively accessed property getter.
        /// </summary>
        [Fact]
        public void DocumentedException_ThrownByPropertyGetter_IsNotDetected()
        {
            string source =
                "public class Helper\n" +
                "{\n" +
                "    public int Value\n" +
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
    }
}
