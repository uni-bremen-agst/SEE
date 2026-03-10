using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Semantic.Exception
{
    /// <summary>
    /// Tests for DOC630 (ExceptionTagWithoutDirectThrow).
    /// </summary>
    public sealed class DOC630_ExceptionTagWithoutDirectThrowTests
    {
        /// <summary>
        /// Ensures that a documented exception that is never thrown triggers DOC630.
        /// </summary>
        [Fact]
        public void DocumentedButNeverThrown_IsDetected()
        {
            string member =
                "/// <summary>Does something.</summary>\n" +
                "/// <exception cref=\"System.InvalidOperationException\">Invalid.</exception>\n" +
                "public void M() { }\n";

            List<Finding> findings = CheckAssert.FindSemanticExceptionFindingsForMember(member);

            Finding finding = Assert.Single(findings);
            Assert.Equal(XmlDocSmells.ExceptionTagWithoutDirectThrow.ID, finding.Smell.ID);
            Assert.Equal("exception", finding.TagName);
        }

        /// <summary>
        /// Ensures that a directly thrown documented exception does not trigger DOC630.
        /// </summary>
        [Fact]
        public void DocumentedAndDirectlyThrown_IsNotDetected()
        {
            string member =
                "/// <summary>Does something.</summary>\n" +
                "/// <exception cref=\"System.InvalidOperationException\">Invalid.</exception>\n" +
                "public void M()\n" +
                "{\n" +
                "    throw new System.InvalidOperationException();\n" +
                "}\n";

            List<Finding> findings = CheckAssert.FindSemanticExceptionFindingsForMember(member);

            Assert.Empty(findings);
        }

        /// <summary>
        /// Ensures that a transitively thrown documented exception does not trigger DOC630.
        /// </summary>
        [Fact]
        public void DocumentedAndTransitivelyThrown_IsNotDetected()
        {
            string source =
                "public class TestClass\n" +
                "{\n" +
                "    /// <summary>Does something.</summary>\n" +
                "    /// <exception cref=\"System.InvalidOperationException\">Invalid.</exception>\n" +
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

            Assert.Empty(findings);
        }

        /// <summary>
        /// Ensures that invalid cref targets are ignored here because DOC660 handles them.
        /// </summary>
        [Fact]
        public void InvalidCref_IsIgnoredHere()
        {
            string member =
                "/// <summary>Does something.</summary>\n" +
                "/// <exception cref=\"NotExistingException\">Invalid.</exception>\n" +
                "public void M() { }\n";

            List<Finding> findings = CheckAssert.FindSemanticExceptionFindingsForMember(member);

            Assert.DoesNotContain(findings, finding => finding.Smell.ID == XmlDocSmells.ExceptionTagWithoutDirectThrow.ID);
        }

        /// <summary>
        /// Ensures that non-exception cref targets are ignored here because DOC670 handles them.
        /// </summary>
        [Fact]
        public void NonExceptionCref_IsIgnoredHere()
        {
            string member =
                "/// <summary>Does something.</summary>\n" +
                "/// <exception cref=\"System.String\">Invalid.</exception>\n" +
                "public void M() { }\n";

            List<Finding> findings = CheckAssert.FindSemanticExceptionFindingsForMember(member);

            Assert.DoesNotContain(findings, finding => finding.Smell.ID == XmlDocSmells.ExceptionTagWithoutDirectThrow.ID);
        }

        /// <summary>
        /// Ensures that a documented base exception type is accepted when a derived exception is thrown.
        /// </summary>
        [Fact]
        public void DocumentedBaseException_IsAcceptedForDerivedThrownException()
        {
            string member =
                "/// <summary>Does something.</summary>\n" +
                "/// <exception cref=\"System.Exception\">Invalid.</exception>\n" +
                "public void M()\n" +
                "{\n" +
                "    throw new System.InvalidOperationException();\n" +
                "}\n";

            List<Finding> findings = CheckAssert.FindSemanticExceptionFindingsForMember(member);

            Assert.Empty(findings);
        }

        /// <summary>
        /// Ensures that among multiple documented exceptions only the never-thrown one triggers DOC630.
        /// </summary>
        [Fact]
        public void MixedDocumentedExceptions_ReportOnlyUnusedDocumentation()
        {
            string member =
                "/// <summary>Does something.</summary>\n" +
                "/// <exception cref=\"System.InvalidOperationException\">Thrown.</exception>\n" +
                "/// <exception cref=\"System.ArgumentException\">Not thrown.</exception>\n" +
                "public void M()\n" +
                "{\n" +
                "    throw new System.InvalidOperationException();\n" +
                "}\n";

            List<Finding> findings = CheckAssert.FindSemanticExceptionFindingsForMember(member);

            Finding finding = Assert.Single(findings);
            Assert.Equal(XmlDocSmells.ExceptionTagWithoutDirectThrow.ID, finding.Smell.ID);
            Assert.Equal("exception", finding.TagName);
        }
    }
}
