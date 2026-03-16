using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Semantic.Exception
{
    /// <summary>
    /// Tests DOC632 for advanced transitive exception-flow scenarios.
    /// </summary>
    public sealed class DOC632_AdvancedTransitiveExceptionFlowTests
    {
        /// <summary>
        /// Ensures that a documented exception is covered when it is thrown
        /// three calls deep in the transitive call graph.
        /// </summary>
        [Fact]
        public void DocumentedException_ThrownThreeCallsDeep_IsNotDetected()
        {
            string source =
                "public class TestClass\n" +
                "{\n" +
                "    /// <summary>Entry point.</summary>\n" +
                "    /// <exception cref=\"System.InvalidOperationException\">Thrown transitively.</exception>\n" +
                "    public void M()\n" +
                "    {\n" +
                "        A();\n" +
                "    }\n" +
                "\n" +
                "    private void A()\n" +
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

            List<Finding> findings =
                CheckAssert.FindSemanticExceptionFindingsForSource(source, ExceptionAnalysisMode.ProjectTransitive);

            Assert.DoesNotContain(
                findings,
                finding => finding.Smell.ID == XmlDocSmells.ExceptionTagWithoutTransitiveThrow.ID);
        }

        /// <summary>
        /// Ensures that a documented exception is covered when it is thrown
        /// through a recursive call graph.
        /// </summary>
        [Fact]
        public void DocumentedException_InRecursiveCallGraph_IsNotDetected()
        {
            string source =
                "public class TestClass\n" +
                "{\n" +
                "    /// <summary>Entry point.</summary>\n" +
                "    /// <exception cref=\"System.InvalidOperationException\">Thrown transitively.</exception>\n" +
                "    public void M()\n" +
                "    {\n" +
                "        A(0);\n" +
                "    }\n" +
                "\n" +
                "    private void A(int depth)\n" +
                "    {\n" +
                "        if (depth == 1)\n" +
                "        {\n" +
                "            throw new System.InvalidOperationException();\n" +
                "        }\n" +
                "\n" +
                "        A(depth + 1);\n" +
                "    }\n" +
                "}\n";

            List<Finding> findings =
                CheckAssert.FindSemanticExceptionFindingsForSource(source, ExceptionAnalysisMode.ProjectTransitive);

            Assert.DoesNotContain(
                findings,
                finding => finding.Smell.ID == XmlDocSmells.ExceptionTagWithoutTransitiveThrow.ID);
        }

        /// <summary>
        /// Ensures that a documented exception is covered when it is thrown by a constructor
        /// reached transitively from the documented member.
        /// </summary>
        [Fact]
        public void DocumentedException_ThrownByConstructor_IsNotDetected()
        {
            string source =
                "public class Created\n" +
                "{\n" +
                "    public Created()\n" +
                "    {\n" +
                "        throw new System.InvalidOperationException();\n" +
                "    }\n" +
                "}\n" +
                "\n" +
                "public class TestClass\n" +
                "{\n" +
                "    /// <summary>Entry point.</summary>\n" +
                "    /// <exception cref=\"System.InvalidOperationException\">Thrown transitively.</exception>\n" +
                "    public void M()\n" +
                "    {\n" +
                "        Create();\n" +
                "    }\n" +
                "\n" +
                "    private void Create()\n" +
                "    {\n" +
                "        _ = new Created();\n" +
                "    }\n" +
                "}\n";

            List<Finding> findings =
                CheckAssert.FindSemanticExceptionFindingsForSource(source, ExceptionAnalysisMode.ProjectTransitive);

            Assert.DoesNotContain(
                findings,
                finding => finding.Smell.ID == XmlDocSmells.ExceptionTagWithoutTransitiveThrow.ID);
        }

        /// <summary>
        /// Ensures that a documented exception is covered when it is thrown by a property getter
        /// reached transitively from the documented member.
        /// </summary>
        [Fact]
        public void DocumentedException_ThrownByPropertyGetter_IsNotDetected()
        {
            string source =
                "public class Provider\n" +
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
                "    private readonly Provider provider = new();\n" +
                "\n" +
                "    /// <summary>Entry point.</summary>\n" +
                "    /// <exception cref=\"System.InvalidOperationException\">Thrown transitively.</exception>\n" +
                "    public void M()\n" +
                "    {\n" +
                "        Read();\n" +
                "    }\n" +
                "\n" +
                "    private int Read()\n" +
                "    {\n" +
                "        return provider.Value;\n" +
                "    }\n" +
                "}\n";

            List<Finding> findings =
                CheckAssert.FindSemanticExceptionFindingsForSource(source, ExceptionAnalysisMode.ProjectTransitive);

            Assert.DoesNotContain(
                findings,
                finding => finding.Smell.ID == XmlDocSmells.ExceptionTagWithoutTransitiveThrow.ID);
        }

        /// <summary>
        /// Ensures that a documented exception that is not covered by the transitive flow
        /// produces DOC632 and that a transitively thrown undocumented exception produces DOC611.
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

            List<Finding> findings =
                CheckAssert.FindSemanticExceptionFindingsForSource(source, ExceptionAnalysisMode.ProjectTransitive);

            Assert.Equal(2, findings.Count);
            Assert.Contains(findings, finding => finding.Smell.ID == XmlDocSmells.ExceptionTagWithoutTransitiveThrow.ID);
            Assert.Contains(findings, finding => finding.Smell.ID == XmlDocSmells.MissingTransitiveExceptionDocumentation.ID);
        }

        /// <summary>
        /// Ensures that a documented exception is covered when it is thrown by an indexer getter
        /// reached transitively from the documented member.
        /// </summary>
        [Fact]
        public void DocumentedException_ThrownByIndexerGetter_IsNotDetected()
        {
            string source =
                "public class Provider\n" +
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
                "    private readonly Provider provider = new();\n" +
                "\n" +
                "    /// <summary>Entry point.</summary>\n" +
                "    /// <exception cref=\"System.InvalidOperationException\">Thrown transitively.</exception>\n" +
                "    public void M()\n" +
                "    {\n" +
                "        Read();\n" +
                "    }\n" +
                "\n" +
                "    private int Read()\n" +
                "    {\n" +
                "        return provider[0];\n" +
                "    }\n" +
                "}\n";

            List<Finding> findings =
                CheckAssert.FindSemanticExceptionFindingsForSource(source, ExceptionAnalysisMode.ProjectTransitive);

            Assert.DoesNotContain(
                findings,
                finding => finding.Smell.ID == XmlDocSmells.ExceptionTagWithoutTransitiveThrow.ID);
        }

        /// <summary>
        /// Ensures that a documented exception is covered when it is thrown by an expression-bodied property getter.
        /// </summary>
        [Fact]
        public void DocumentedException_ThrownByExpressionBodiedPropertyGetter_IsNotDetected()
        {
            string source =
                "public class Provider\n" +
                "{\n" +
                "    public int Value => throw new System.InvalidOperationException();\n" +
                "}\n" +
                "\n" +
                "public class TestClass\n" +
                "{\n" +
                "    private readonly Provider provider = new();\n" +
                "\n" +
                "    /// <summary>Entry point.</summary>\n" +
                "    /// <exception cref=\"System.InvalidOperationException\">Thrown transitively.</exception>\n" +
                "    public void M()\n" +
                "    {\n" +
                "        Read();\n" +
                "    }\n" +
                "\n" +
                "    private int Read()\n" +
                "    {\n" +
                "        return provider.Value;\n" +
                "    }\n" +
                "}\n";

            List<Finding> findings =
                CheckAssert.FindSemanticExceptionFindingsForSource(source, ExceptionAnalysisMode.ProjectTransitive);

            Assert.DoesNotContain(
                findings,
                finding => finding.Smell.ID == XmlDocSmells.ExceptionTagWithoutTransitiveThrow.ID);
        }

        /// <summary>
        /// Ensures that a documented exception is covered when it is thrown by an expression-bodied method.
        /// </summary>
        [Fact]
        public void DocumentedException_ThrownByExpressionBodiedMethod_IsNotDetected()
        {
            string source =
                "public class TestClass\n" +
                "{\n" +
                "    /// <summary>Entry point.</summary>\n" +
                "    /// <exception cref=\"System.InvalidOperationException\">Thrown transitively.</exception>\n" +
                "    public void M()\n" +
                "    {\n" +
                "        Helper();\n" +
                "    }\n" +
                "\n" +
                "    private void Helper() => throw new System.InvalidOperationException();\n" +
                "}\n";

            List<Finding> findings =
                CheckAssert.FindSemanticExceptionFindingsForSource(source, ExceptionAnalysisMode.ProjectTransitive);

            Assert.DoesNotContain(
                findings,
                finding => finding.Smell.ID == XmlDocSmells.ExceptionTagWithoutTransitiveThrow.ID);
        }
    }
}
