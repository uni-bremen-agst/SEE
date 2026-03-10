using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Syntax.Exception
{
    /// <summary>
    /// Tests for DOC670 (ExceptionCrefNotExceptionType).
    /// </summary>
    public sealed class DOC670_ExceptionCrefNotExceptionTypeTests
    {
        /// <summary>
        /// Ensures that referencing a non-exception type triggers DOC670.
        /// </summary>
        [Fact]
        public void NonExceptionTypeCref_IsDetected()
        {
            string member =
                "/// <summary>Test.</summary>\n" +
                "/// <exception cref=\"System.String\">Invalid.</exception>\n" +
                "public void M() { }\n";

            List<Finding> findings =
                CheckAssert.FindSemanticExceptionFindingsForMember(member);

            Finding finding = Assert.Single(findings);

            Assert.Equal(XmlDocSmells.ExceptionCrefNotExceptionType.ID, finding.Smell.ID);
        }

        /// <summary>
        /// Ensures that valid exception types do not trigger DOC670.
        /// </summary>
        [Fact]
        public void ValidExceptionType_IsNotDetected()
        {
            string member =
                "/// <summary>Test.</summary>\n" +
                "/// <exception cref=\"System.InvalidOperationException\">Valid.</exception>\n" +
                "public void M() { }\n";

            List<Finding> findings =
                CheckAssert.FindSemanticExceptionFindingsForMember(member);

            Assert.Empty(findings);
        }

        /// <summary>
        /// Ensures that user-defined exception types are accepted.
        /// </summary>
        [Fact]
        public void CustomExceptionType_IsNotDetected()
        {
            string source =
                "using System;\n" +
                "\n" +
                "public class CustomException : Exception {}\n" +
                "\n" +
                "public class TestClass\n" +
                "{\n" +
                "    /// <summary>Test.</summary>\n" +
                "    /// <exception cref=\"CustomException\">Valid.</exception>\n" +
                "    public void M() {}\n" +
                "}\n";

            List<Finding> findings =
                CheckAssert.FindSemanticExceptionFindingsForSource(source);

            Assert.Empty(findings);
        }
    }
}
