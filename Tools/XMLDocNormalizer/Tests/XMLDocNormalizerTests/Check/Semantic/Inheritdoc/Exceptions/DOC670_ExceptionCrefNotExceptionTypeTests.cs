using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Semantic.Exception
{
    /// <summary>
    /// Tests for DOC670 (ExceptionCrefNotExceptionType):
    /// the cref of an <exception> tag resolves to a symbol that
    /// is not derived from System.Exception.
    /// </summary>
    public sealed class DOC670_ExceptionCrefNotExceptionTypeTests
    {
        /// <summary>
        /// Ensures that a valid exception type does not trigger DOC670.
        /// </summary>
        [Fact]
        public void ValidExceptionType_IsNotDetected()
        {
            string member =
                "/// <summary>Test.</summary>\n" +
                "/// <exception cref=\"System.InvalidOperationException\">Valid.</exception>\n" +
                "public void M()\n" +
                "{\n" +
                "    throw new System.InvalidOperationException();\n" +
                "}\n";

            List<Finding> findings = CheckAssert.FindSemanticExceptionFindingsForMember(member);

            Assert.DoesNotContain(findings,
                finding => finding.Smell.ID == XmlDocSmells.ExceptionCrefNotExceptionType.ID);
        }

        /// <summary>
        /// Ensures that a cref resolving to a non-exception type triggers DOC670.
        /// </summary>
        [Fact]
        public void NonExceptionType_IsDetected()
        {
            string member =
                "/// <summary>Test.</summary>\n" +
                "/// <exception cref=\"System.String\">Invalid.</exception>\n" +
                "public void M() { }\n";

            List<Finding> findings = CheckAssert.FindSemanticExceptionFindingsForMember(member);

            Finding finding = Assert.Single(findings);

            Assert.Equal(XmlDocSmells.ExceptionCrefNotExceptionType.ID, finding.Smell.ID);
            Assert.Equal("exception", finding.TagName);
        }

        /// <summary>
        /// Ensures that a custom exception type does not trigger DOC670.
        /// </summary>
        [Fact]
        public void CustomExceptionType_IsNotDetected()
        {
            string source =
                "public class CustomException : System.Exception { }\n" +
                "public class TestClass\n" +
                "{\n" +
                "    /// <summary>Test.</summary>\n" +
                "    /// <exception cref=\"CustomException\">Valid.</exception>\n" +
                "    public void M()\n" +
                "    {\n" +
                "        throw new CustomException();\n" +
                "    }\n" +
                "}";

            List<Finding> findings = CheckAssert.FindSemanticExceptionFindingsForSource(source);

            Assert.DoesNotContain(findings,
                finding => finding.Smell.ID == XmlDocSmells.ExceptionCrefNotExceptionType.ID);
        }

        /// <summary>
        /// Ensures that when multiple exception tags exist,
        /// only the non-exception cref triggers DOC670.
        /// </summary>
        [Fact]
        public void MixedExceptionTypes_ReportOnlyInvalidOnes()
        {
            string member =
                "/// <summary>Test.</summary>\n" +
                "/// <exception cref=\"System.InvalidOperationException\">Valid.</exception>\n" +
                "/// <exception cref=\"System.String\">Invalid.</exception>\n" +
                "public void M()\n" +
                "{\n" +
                "    throw new System.InvalidOperationException();\n" +
                "}\n";

            List<Finding> findings = CheckAssert.FindSemanticExceptionFindingsForMember(member);

            Assert.Single(findings);

            Finding finding = findings[0];

            Assert.Equal(XmlDocSmells.ExceptionCrefNotExceptionType.ID, finding.Smell.ID);
            Assert.Equal("exception", finding.TagName);
        }
    }
}
