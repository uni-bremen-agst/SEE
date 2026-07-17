using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Semantic.Exception
{
    /// <summary>
    /// Tests inheritdoc interactions for semantic exception documentation findings.
    /// </summary>
    public sealed class DOC610_611_InheritdocExceptionDocumentationTests
    {
        /// <summary>
        /// Ensures that inheritdoc suppresses missing documentation for directly thrown exceptions in direct mode.
        /// </summary>
        [Fact]
        public void DirectExceptionWithInheritdoc_DoesNotTriggerMissingExceptionTag_InDirectMode()
        {
            string member =
                "/// <inheritdoc/>\n" +
                "public void M()\n" +
                "{\n" +
                "    throw new System.InvalidOperationException();\n" +
                "}\n";

            List<Finding> findings =
                CheckAssert.FindSemanticExceptionFindingsForMember(member, ExceptionAnalysisMode.Direct);

            FindingAsserts.HasExactlySmells(findings);
        }

        /// <summary>
        /// Ensures that inheritdoc suppresses missing documentation for directly thrown exceptions in transitive mode.
        /// </summary>
        [Fact]
        public void DirectExceptionWithInheritdoc_DoesNotTriggerMissingExceptionTag_InTransitiveMode()
        {
            string source =
                "public class TestClass\n" +
                "{\n" +
                "    /// <inheritdoc/>\n" +
                "    public void M()\n" +
                "    {\n" +
                "        throw new System.InvalidOperationException();\n" +
                "    }\n" +
                "}\n";

            List<Finding> findings =
                CheckAssert.FindSemanticExceptionFindingsForSource(source, ExceptionAnalysisMode.ProjectTransitive);

            FindingAsserts.HasExactlySmells(findings);
        }

        /// <summary>
        /// Ensures that inheritdoc suppresses missing documentation for transitively thrown exceptions.
        /// </summary>
        [Fact]
        public void TransitiveExceptionWithInheritdoc_DoesNotTriggerMissingTransitiveExceptionDocumentation()
        {
            string source =
                "public class TestClass\n" +
                "{\n" +
                "    /// <inheritdoc/>\n" +
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

            FindingAsserts.HasExactlySmells(findings);
        }

        /// <summary>
        /// Ensures that inheritdoc does not suppress semantic validation of explicit exception tags.
        /// </summary>
        [Fact]
        public void InheritdocWithInvalidExplicitExceptionCref_ReportsInvalidExceptionCref()
        {
            string member =
                "/// <inheritdoc/>\n" +
                "/// <exception cref=\"MissingException\">Documented explicitly.</exception>\n" +
                "public void M()\n" +
                "{\n" +
                "}\n";

            List<Finding> findings =
                CheckAssert.FindSemanticExceptionFindingsForMember(member, ExceptionAnalysisMode.Direct);

            FindingAsserts.HasExactlySmells(findings, XmlDocSmells.InvalidExceptionCref.ID);
        }
    }
}
