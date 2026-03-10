using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Semantic.Exception
{
    /// <summary>
    /// Tests DOC630 for mixed coverage scenarios with multiple documented
    /// and transitively thrown exception types.
    /// </summary>
    public sealed class DOC630_MixedExceptionCoverageTests
    {
        /// <summary>
        /// Ensures that only the documented exception without transitive coverage
        /// triggers DOC630 when another documented exception is actually thrown.
        /// </summary>
        [Fact]
        public void MixedDocumentedExceptions_ReportOnlyUncoveredException()
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
            Assert.Contains("System.ArgumentException", finding.Message, StringComparison.Ordinal);
        }

        /// <summary>
        /// Ensures that no DOC630 finding is produced when all documented exceptions
        /// are covered by transitive throws.
        /// </summary>
        [Fact]
        public void AllDocumentedExceptionsCoveredTransitively_ProducesNoFinding()
        {
            string source =
                "public class TestClass\n" +
                "{\n" +
                "    /// <summary>Entry point.</summary>\n" +
                "    /// <exception cref=\"System.InvalidOperationException\">Thrown transitively.</exception>\n" +
                "    /// <exception cref=\"System.ArgumentException\">Thrown transitively.</exception>\n" +
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

            Assert.DoesNotContain(
                findings,
                finding => finding.Smell.ID == XmlDocSmells.ExceptionTagWithoutDirectThrow.ID);
        }

        /// <summary>
        /// Ensures that multiple uncovered documented exceptions each produce their own DOC630 finding.
        /// </summary>
        [Fact]
        public void MultipleUncoveredDocumentedExceptions_AllProduceFindings()
        {
            string member =
                "/// <summary>Does something.</summary>\n" +
                "/// <exception cref=\"System.InvalidOperationException\">Not thrown.</exception>\n" +
                "/// <exception cref=\"System.ArgumentException\">Also not thrown.</exception>\n" +
                "public void M() { }\n";

            List<Finding> findings = CheckAssert.FindSemanticExceptionFindingsForMember(member);

            Assert.Equal(2, findings.Count);
            Assert.All(findings, finding =>
            {
                Assert.Equal(XmlDocSmells.ExceptionTagWithoutDirectThrow.ID, finding.Smell.ID);
                Assert.Equal("exception", finding.TagName);
            });
        }

        /// <summary>
        /// Ensures that a documented base exception type is considered covered
        /// when only a derived exception type is thrown transitively.
        /// </summary>
        [Fact]
        public void DocumentedBaseException_IsCoveredByTransitivelyThrownDerivedException()
        {
            string source =
                "public class TestClass\n" +
                "{\n" +
                "    /// <summary>Entry point.</summary>\n" +
                "    /// <exception cref=\"System.Exception\">Covered by derived throw.</exception>\n" +
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

            Assert.DoesNotContain(
                findings,
                finding => finding.Smell.ID == XmlDocSmells.ExceptionTagWithoutDirectThrow.ID);
        }

        /// <summary>
        /// Ensures that an invalid cref does not additionally produce DOC630
        /// because DOC660 handles unresolved exception references.
        /// </summary>
        [Fact]
        public void InvalidCref_DoesNotAlsoProduceDoc630()
        {
            string member =
                "/// <summary>Does something.</summary>\n" +
                "/// <exception cref=\"NotExistingException\">Invalid cref.</exception>\n" +
                "public void M() { }\n";

            List<Finding> findings = CheckAssert.FindSemanticExceptionFindingsForMember(member);

            Assert.DoesNotContain(
                findings,
                finding => finding.Smell.ID == XmlDocSmells.ExceptionTagWithoutDirectThrow.ID);
        }

        /// <summary>
        /// Ensures that a cref resolving to a non-exception type does not additionally
        /// produce DOC630 because DOC670 handles that case.
        /// </summary>
        [Fact]
        public void NonExceptionCref_DoesNotAlsoProduceDoc630()
        {
            string member =
                "/// <summary>Does something.</summary>\n" +
                "/// <exception cref=\"System.String\">Not an exception.</exception>\n" +
                "public void M() { }\n";

            List<Finding> findings = CheckAssert.FindSemanticExceptionFindingsForMember(member);

            Assert.DoesNotContain(
                findings,
                finding => finding.Smell.ID == XmlDocSmells.ExceptionTagWithoutDirectThrow.ID);
        }
    }
}
