using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Traversal.Syntax
{
    /// <summary>
    /// Traversal tests ensuring that the exception detector analyzes nested types and their members.
    /// </summary>
    public sealed class Traversal_NestedTypes_ExceptionDetectorTests
    {
        /// <summary>
        /// Ensures that a duplicate exception tag inside a nested type is detected.
        /// </summary>
        [Fact]
        public void DuplicateExceptionTag_InNestedType_IsDetected()
        {
            string source =
                "public class Outer\n" +
                "{\n" +
                "    public class Inner\n" +
                "    {\n" +
                "        /// <summary>Test</summary>\n" +
                "        /// <exception cref=\"System.InvalidOperationException\">first</exception>\n" +
                "        /// <exception cref=\"System.InvalidOperationException\">second</exception>\n" +
                "        public void M() { }\n" +
                "    }\n" +
                "}\n";

            var findings = CheckAssert.FindExceptionFindingsForSource(source);

            FindingAsserts.HasExactlySmells(findings, "DOC650");

            Finding finding = findings.Single();
            Assert.Equal("exception", finding.TagName);
        }

        /// <summary>
        /// Ensures that valid exception documentation in a nested type produces no findings.
        /// </summary>
        [Fact]
        public void ValidExceptionTag_InNestedType_ProducesNoFindings()
        {
            string source =
                "public class Outer\n" +
                "{\n" +
                "    public class Inner\n" +
                "    {\n" +
                "        /// <summary>Test</summary>\n" +
                "        /// <exception cref=\"System.InvalidOperationException\">Ok</exception>\n" +
                "        public void M() { }\n" +
                "    }\n" +
                "}\n";

            var findings = CheckAssert.FindExceptionFindingsForSource(source);

            Assert.Empty(findings);
        }
    }
}