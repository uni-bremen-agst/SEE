using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Semantic.Exception
{
    /// <summary>
    /// Tests exception translation in catch-clauses.
    /// </summary>
    public sealed class DOC611_ExceptionTranslationTests
    {
        /// <summary>
        /// Ensures that a caught transitive exception is no longer reported when it is translated
        /// into a different exception type, while the new escaping exception is reported.
        /// </summary>
        [Fact]
        public void CaughtTransitiveException_TranslatedToNewException_ReportsOnlyNewException()
        {
            string source =
                "public sealed class CustomException : System.Exception { }\n" +
                "\n" +
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
                "            throw new CustomException();\n" +
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
            Assert.Contains("CustomException", finding.Message, StringComparison.Ordinal);
            Assert.DoesNotContain(findings, f => f.Message.Contains("System.InvalidOperationException", StringComparison.Ordinal));
        }

        /// <summary>
        /// Ensures that a directly thrown exception caught and translated to a new exception
        /// only reports the new escaping exception in direct mode.
        /// </summary>
        [Fact]
        public void CaughtDirectException_TranslatedToNewException_ReportsOnlyNewException()
        {
            string member =
                "/// <summary>Test.</summary>\n" +
                "public void M()\n" +
                "{\n" +
                "    try\n" +
                "    {\n" +
                "        throw new System.InvalidOperationException();\n" +
                "    }\n" +
                "    catch (System.InvalidOperationException)\n" +
                "    {\n" +
                "        throw new System.ArgumentException();\n" +
                "    }\n" +
                "}\n";

            List<Finding> findings =
                CheckAssert.FindSemanticExceptionFindingsForMember(member, ExceptionAnalysisMode.Direct);

            Finding finding = Assert.Single(findings);
            Assert.Equal(XmlDocSmells.MissingExceptionTag.ID, finding.Smell.ID);
            Assert.Contains("System.ArgumentException", finding.Message, StringComparison.Ordinal);
            Assert.DoesNotContain(findings, f => f.Message.Contains("System.InvalidOperationException", StringComparison.Ordinal));
        }
    }
}
