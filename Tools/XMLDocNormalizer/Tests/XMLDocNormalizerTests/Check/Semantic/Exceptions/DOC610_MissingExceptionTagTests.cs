using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Semantic.Exception
{
    /// <summary>
    /// Tests for DOC610 (MissingExceptionTag).
    /// </summary>
    public sealed class DOC610_MissingExceptionTagTests
    {
        /// <summary>
        /// Ensures that a directly thrown exception without documentation triggers DOC610.
        /// </summary>
        [Fact]
        public void DirectlyThrownExceptionWithoutDocumentation_IsDetected()
        {
            string member =
                "/// <summary>Does something.</summary>\n" +
                "public void M()\n" +
                "{\n" +
                "    throw new System.InvalidOperationException();\n" +
                "}\n";

            List<Finding> findings = CheckAssert.FindSemanticExceptionFindingsForMember(member);

            Finding finding = Assert.Single(findings);
            Assert.Equal(XmlDocSmells.MissingExceptionTag.ID, finding.Smell.ID);
            Assert.Equal("exception", finding.TagName);
        }

        /// <summary>
        /// Ensures that a transitively thrown exception without documentation triggers DOC610.
        /// </summary>
        [Fact]
        public void TransitivelyThrownExceptionWithoutDocumentation_IsDetected()
        {
            string source =
                "public class TestClass\n" +
                "{\n" +
                "    /// <summary>Entry point.</summary>\n" +
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

            Finding finding = Assert.Single(findings);
            Assert.Equal(XmlDocSmells.MissingExceptionTag.ID, finding.Smell.ID);
            Assert.Equal("exception", finding.TagName);
        }

        /// <summary>
        /// Ensures that a documented directly thrown exception does not trigger DOC610.
        /// </summary>
        [Fact]
        public void DocumentedDirectlyThrownException_IsNotDetected()
        {
            string member =
                "/// <summary>Does something.</summary>\n" +
                "/// <exception cref=\"System.InvalidOperationException\">Documented.</exception>\n" +
                "public void M()\n" +
                "{\n" +
                "    throw new System.InvalidOperationException();\n" +
                "}\n";

            List<Finding> findings = CheckAssert.FindSemanticExceptionFindingsForMember(member);

            Assert.DoesNotContain(
                findings,
                finding => finding.Smell.ID == XmlDocSmells.MissingExceptionTag.ID);
        }

        /// <summary>
        /// Ensures that a documented base exception type covers a derived thrown exception.
        /// </summary>
        [Fact]
        public void DocumentedBaseException_CoversDerivedThrownException()
        {
            string member =
                "/// <summary>Does something.</summary>\n" +
                "/// <exception cref=\"System.Exception\">Documented.</exception>\n" +
                "public void M()\n" +
                "{\n" +
                "    throw new System.InvalidOperationException();\n" +
                "}\n";

            List<Finding> findings = CheckAssert.FindSemanticExceptionFindingsForMember(member);

            Assert.DoesNotContain(
                findings,
                finding => finding.Smell.ID == XmlDocSmells.MissingExceptionTag.ID);
        }

        /// <summary>
        /// Ensures that mixed documented and undocumented thrown exceptions report only the missing one.
        /// </summary>
        [Fact]
        public void MixedThrownExceptions_ReportOnlyUndocumentedOnes()
        {
            string source =
                "public class TestClass\n" +
                "{\n" +
                "    /// <summary>Entry point.</summary>\n" +
                "    /// <exception cref=\"System.InvalidOperationException\">Documented.</exception>\n" +
                "    public void M()\n" +
                "    {\n" +
                "        First();\n" +
                "        Second();\n" +
                "    }\n" +
                "\n" +
                "    private void First()\n" +
                "    {\n" +
                "        throw new System.InvalidOperationException();\n" +
                "    }\n" +
                "\n" +
                "    private void Second()\n" +
                "    {\n" +
                "        throw new System.ArgumentException();\n" +
                "    }\n" +
                "}\n";

            List<Finding> findings = CheckAssert.FindSemanticExceptionFindingsForSource(source);

            Finding finding = Assert.Single(findings);
            Assert.Equal(XmlDocSmells.MissingExceptionTag.ID, finding.Smell.ID);
            Assert.Equal("exception", finding.TagName);
            Assert.Contains("System.ArgumentException", finding.Message, StringComparison.Ordinal);
        }
    }
}
