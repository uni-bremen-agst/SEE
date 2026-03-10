using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Semantic.Exception
{
    /// <summary>
    /// Tests advanced transitive exception-flow scenarios for DOC610 (MissingExceptionTag).
    /// </summary>
    public sealed class DOC610_AdvancedTransitiveExceptionFlowTests
    {
        /// <summary>
        /// Ensures that an undocumented exception thrown three calls deep
        /// in the transitive call chain triggers DOC610.
        /// </summary>
        [Fact]
        public void UndocumentedException_ThrownThreeCallsDeep_IsDetected()
        {
            string source =
                "public class TestClass\n" +
                "{\n" +
                "    /// <summary>Entry point.</summary>\n" +
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

            Finding finding = Assert.Single(findings);
            Assert.Equal(XmlDocSmells.MissingExceptionTag.ID, finding.Smell.ID);
            Assert.Equal("exception", finding.TagName);
            Assert.Contains("System.InvalidOperationException", finding.Message, StringComparison.Ordinal);
        }

        /// <summary>
        /// Ensures that recursive or cyclic call graphs do not prevent DOC610
        /// when an undocumented exception is still thrown transitively.
        /// </summary>
        [Fact]
        public void UndocumentedException_InRecursiveCallGraph_IsDetected()
        {
            string source =
                "public class TestClass\n" +
                "{\n" +
                "    /// <summary>Entry point.</summary>\n" +
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

            Finding finding = Assert.Single(findings);
            Assert.Equal(XmlDocSmells.MissingExceptionTag.ID, finding.Smell.ID);
            Assert.Equal("exception", finding.TagName);
            Assert.Contains("System.InvalidOperationException", finding.Message, StringComparison.Ordinal);
        }

        /// <summary>
        /// Ensures that an undocumented exception thrown by a transitively invoked constructor
        /// triggers DOC610.
        /// </summary>
        [Fact]
        public void UndocumentedException_ThrownByConstructor_IsDetected()
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

            Finding finding = Assert.Single(findings);
            Assert.Equal(XmlDocSmells.MissingExceptionTag.ID, finding.Smell.ID);
            Assert.Equal("exception", finding.TagName);
            Assert.Contains("System.InvalidOperationException", finding.Message, StringComparison.Ordinal);
        }

        /// <summary>
        /// Ensures that an undocumented exception thrown by a transitively accessed property getter
        /// triggers DOC610.
        /// </summary>
        [Fact]
        public void UndocumentedException_ThrownByPropertyGetter_IsDetected()
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

            Finding finding = Assert.Single(findings);
            Assert.Equal(XmlDocSmells.MissingExceptionTag.ID, finding.Smell.ID);
            Assert.Equal("exception", finding.TagName);
            Assert.Contains("System.InvalidOperationException", finding.Message, StringComparison.Ordinal);
        }

        /// <summary>
        /// Ensures that an undocumented exception thrown by a transitively accessed indexer getter
        /// triggers DOC610.
        /// </summary>
        [Fact]
        public void UndocumentedException_ThrownByIndexerGetter_IsDetected()
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

            Finding finding = Assert.Single(findings);
            Assert.Equal(XmlDocSmells.MissingExceptionTag.ID, finding.Smell.ID);
            Assert.Equal("exception", finding.TagName);
            Assert.Contains("System.InvalidOperationException", finding.Message, StringComparison.Ordinal);
        }

        /// <summary>
        /// Ensures that an undocumented exception thrown by a transitively accessed
        /// expression-bodied property getter triggers DOC610.
        /// </summary>
        [Fact]
        public void UndocumentedException_ThrownByExpressionBodiedPropertyGetter_IsDetected()
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

            Finding finding = Assert.Single(findings);
            Assert.Equal(XmlDocSmells.MissingExceptionTag.ID, finding.Smell.ID);
            Assert.Equal("exception", finding.TagName);
            Assert.Contains("System.InvalidOperationException", finding.Message, StringComparison.Ordinal);
        }

        /// <summary>
        /// Ensures that an undocumented exception thrown by a transitively called
        /// expression-bodied method triggers DOC610.
        /// </summary>
        [Fact]
        public void UndocumentedException_ThrownByExpressionBodiedMethod_IsDetected()
        {
            string source =
                "public class TestClass\n" +
                "{\n" +
                "    /// <summary>Entry point.</summary>\n" +
                "    public int M()\n" +
                "    {\n" +
                "        return Helper();\n" +
                "    }\n" +
                "\n" +
                "    private int Helper() => throw new System.InvalidOperationException();\n" +
                "}\n";

            List<Finding> findings = CheckAssert.FindSemanticExceptionFindingsForSource(source);

            Finding finding = Assert.Single(findings);
            Assert.Equal(XmlDocSmells.MissingExceptionTag.ID, finding.Smell.ID);
            Assert.Equal("exception", finding.TagName);
            Assert.Contains("System.InvalidOperationException", finding.Message, StringComparison.Ordinal);
        }
    }
}
