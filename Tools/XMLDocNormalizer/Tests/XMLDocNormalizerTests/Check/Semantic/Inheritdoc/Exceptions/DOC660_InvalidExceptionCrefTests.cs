using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Semantic.Exception
{
    /// <summary>
    /// Tests for DOC660 (InvalidExceptionCref).
    /// </summary>
    public sealed class DOC660_InvalidExceptionCrefTests
    {
        /// <summary>
        /// Ensures that an unknown exception cref on a method triggers DOC660.
        /// </summary>
        [Fact]
        public void UnknownExceptionCrefOnMethod_IsDetected()
        {
            string member =
                "/// <summary>Does something.</summary>\n" +
                "/// <exception cref=\"NotExistingException\">Invalid.</exception>\n" +
                "public void M() { }\n";

            List<Finding> findings = CheckAssert.FindSemanticExceptionFindingsForMember(member);

            Finding finding = Assert.Single(findings);
            Assert.Equal(XmlDocSmells.InvalidExceptionCref.ID, finding.Smell.ID);
            Assert.Equal("exception", finding.TagName);
        }

        /// <summary>
        /// Ensures that an unknown qualified exception cref on a method triggers DOC660.
        /// </summary>
        [Fact]
        public void UnknownQualifiedExceptionCrefOnMethod_IsDetected()
        {
            string member =
                "/// <summary>Does something.</summary>\n" +
                "/// <exception cref=\"Unknown.Namespace.CustomException\">Invalid.</exception>\n" +
                "public void M() { }\n";

            List<Finding> findings = CheckAssert.FindSemanticExceptionFindingsForMember(member);

            Finding finding = Assert.Single(findings);
            Assert.Equal(XmlDocSmells.InvalidExceptionCref.ID, finding.Smell.ID);
            Assert.Equal("exception", finding.TagName);
        }

        /// <summary>
        /// Ensures that a valid framework exception cref does not trigger DOC660.
        /// </summary>
        [Fact]
        public void ValidFrameworkExceptionCref_IsNotDetected()
        {
            string member =
                "/// <summary>Does something.</summary>\n" +
                "/// <exception cref=\"System.InvalidOperationException\">Valid.</exception>\n" +
                "public void M() { }\n";

            List<Finding> findings = CheckAssert.FindSemanticExceptionFindingsForMember(member);

            Assert.Empty(findings);
        }

        /// <summary>
        /// Ensures that a valid user-defined exception cref does not trigger DOC660.
        /// </summary>
        [Fact]
        public void ValidUserDefinedExceptionCref_IsNotDetected()
        {
            string source =
                "using System;\n" +
                "\n" +
                "public class CustomException : Exception\n" +
                "{\n" +
                "}\n" +
                "\n" +
                "public class TestClass\n" +
                "{\n" +
                "    /// <summary>Does something.</summary>\n" +
                "    /// <exception cref=\"CustomException\">Valid.</exception>\n" +
                "    public void M() { }\n" +
                "}\n";

            List<Finding> findings = CheckAssert.FindSemanticExceptionFindingsForSource(source);

            Assert.Empty(findings);
        }

        /// <summary>
        /// Ensures that a missing cref attribute is ignored here because DOC600 handles that case.
        /// </summary>
        [Fact]
        public void MissingCref_IsIgnoredHere()
        {
            string member =
                "/// <summary>Does something.</summary>\n" +
                "/// <exception>Missing cref.</exception>\n" +
                "public void M() { }\n";

            List<Finding> findings = CheckAssert.FindSemanticExceptionFindingsForMember(member);

            Assert.Empty(findings);
        }

        /// <summary>
        /// Ensures that multiple exception tags report DOC660 only for the invalid cref entries.
        /// </summary>
        [Fact]
        public void MixedValidAndInvalidExceptionCrefs_ReportOnlyInvalidOnes()
        {
            string member =
                "/// <summary>Does something.</summary>\n" +
                "/// <exception cref=\"System.InvalidOperationException\">Valid.</exception>\n" +
                "/// <exception cref=\"NotExistingException\">Invalid.</exception>\n" +
                "public void M() { }\n";

            List<Finding> findings = CheckAssert.FindSemanticExceptionFindingsForMember(member);

            Finding finding = Assert.Single(findings);
            Assert.Equal(XmlDocSmells.InvalidExceptionCref.ID, finding.Smell.ID);
            Assert.Equal("exception", finding.TagName);
        }
    }
}