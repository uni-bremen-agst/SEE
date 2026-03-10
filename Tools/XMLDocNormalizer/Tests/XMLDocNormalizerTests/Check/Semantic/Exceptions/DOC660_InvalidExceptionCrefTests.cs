using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Semantic.Exception
{
    /// <summary>
    /// Tests for DOC660 (InvalidExceptionCref): the cref attribute of an
    /// <exception> tag cannot be resolved to a type.
    /// </summary>
    public sealed class DOC660_InvalidExceptionCrefTests
    {
        /// <summary>
        /// Ensures that a valid framework exception cref does not trigger DOC660.
        /// </summary>
        [Fact]
        public void ValidFrameworkExceptionCref_IsNotDetected()
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
                finding => finding.Smell.ID == XmlDocSmells.InvalidExceptionCref.ID);
        }

        /// <summary>
        /// Ensures that a user-defined exception cref does not trigger DOC660.
        /// </summary>
        [Fact]
        public void ValidUserDefinedExceptionCref_IsNotDetected()
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
                finding => finding.Smell.ID == XmlDocSmells.InvalidExceptionCref.ID);
        }

        /// <summary>
        /// Ensures that an unresolved cref triggers DOC660.
        /// </summary>
        [Fact]
        public void InvalidExceptionCref_IsDetected()
        {
            string member =
                "/// <summary>Test.</summary>\n" +
                "/// <exception cref=\"NotExistingException\">Invalid.</exception>\n" +
                "public void M() { }\n";

            List<Finding> findings = CheckAssert.FindSemanticExceptionFindingsForMember(member);

            Finding finding = Assert.Single(findings);

            Assert.Equal(XmlDocSmells.InvalidExceptionCref.ID, finding.Smell.ID);
            Assert.Equal("exception", finding.TagName);
        }

        /// <summary>
        /// Ensures that when valid and invalid crefs are mixed,
        /// only the invalid ones trigger DOC660.
        /// </summary>
        [Fact]
        public void MixedValidAndInvalidExceptionCrefs_ReportOnlyInvalidOnes()
        {
            string member =
                "/// <summary>Test.</summary>\n" +
                "/// <exception cref=\"System.InvalidOperationException\">Valid.</exception>\n" +
                "/// <exception cref=\"NotExistingException\">Invalid.</exception>\n" +
                "public void M()\n" +
                "{\n" +
                "    throw new System.InvalidOperationException();\n" +
                "}\n";

            List<Finding> findings = CheckAssert.FindSemanticExceptionFindingsForMember(member);

            Assert.Single(findings);

            Finding finding = findings[0];

            Assert.Equal(XmlDocSmells.InvalidExceptionCref.ID, finding.Smell.ID);
            Assert.Equal("exception", finding.TagName);
        }
    }
}
