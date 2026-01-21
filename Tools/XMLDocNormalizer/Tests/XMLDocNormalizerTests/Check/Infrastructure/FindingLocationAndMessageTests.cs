
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using XMLDocNormalizer.Checks;
using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Infrastructure
{
    /// <summary>
    /// Tests for location and message formatting behavior of findings.
    /// </summary>
    public sealed class FindingLocationAndMessageTests
    {
        /// <summary>
        /// Ensures that findings created from a syntax node report stable line/column positions
        /// and that message placeholders are formatted correctly.
        /// </summary>
        [Fact]
        public void UnknownTag_reports_expected_line_column_and_formats_message()
        {
            // Line numbers below are 1-based, exactly as reported by Finding.
            string source =
                "public class C\n" +
                "{\n" +
                "    /// <foo>bar</foo>\n" +
                "    public void M() { }\n" +
                "}\n";

            SyntaxTree tree = CSharpSyntaxTree.ParseText(source);
            var findings = XmlDocWellFormedChecker.FindMalformedTags(tree, filePath: "Test.cs");

            // Checks that only one smell is found.
            Assert.Single(findings);
            FindingAsserts.ContainsSingleSmell(findings, "DOC110");

            Finding finding = findings.Single(f => f.Smell.Id == "DOC110");

            Assert.Equal("Test.cs", finding.FilePath);
            Assert.Equal("foo", finding.TagName);

            // The XML doc line is line 3 in the source above.
            Assert.Equal(3, finding.Line);

            // Column is 1-based. For "    /// <foo>bar</foo>", '<' starts after 4 spaces + "/// " (4 chars) => 4 + 4 + 1 = 9.
            Assert.Equal(9, finding.Column);

            Assert.Contains("<foo>", finding.Message);
            Assert.Contains("<foo>", finding.Snippet);
        }

        /// <summary>
        /// Ensures that findings created from raw documentation comment scanning (absolute position)
        /// report stable line/column positions.
        /// </summary>
        [Fact]
        public void MissingEndTag_from_raw_scan_reports_expected_line_column()
        {
            // Intentionally missing </summary>. This should be detected by the raw-text scan.
            string source =
                "public class C\n" +
                "{\n" +
                "    /// <summary>\n" +
                "    /// hello\n" +
                "    public void M() { }\n" +
                "}\n";

            SyntaxTree tree = CSharpSyntaxTree.ParseText(source);
            var findings = XmlDocWellFormedChecker.FindMalformedTags(tree, filePath: "Test.cs");

            // Checks that only one smell is found.
            Assert.Single(findings);
            FindingAsserts.ContainsSmell(findings, "DOC120");

            Finding finding = findings.First(f => f.Smell.Id == "DOC120");

            Assert.Equal("Test.cs", finding.FilePath);
            Assert.Equal("summary", finding.TagName);

            // The opening "<summary>" starts on line 3 in this source.
            Assert.Equal(3, finding.Line);

            // Column is 1-based. For "    /// <summary>", '<' starts at column 9 (same reasoning as above).
            Assert.Equal(9, finding.Column);

            // Raw-scan findings use an empty snippet by design.
            Assert.Equal(string.Empty, finding.Snippet);
        }

        /// <summary>
        /// Ensures that an unclosed tag with a leading blank line reports the expected line/column.
        /// This corresponds to the previous ReturnsChecker "leading blank line" scenario.
        /// </summary>
        [Fact]
        public void MissingEndTag_WithLeadingBlankLine_ReportsExpectedLocation()
        {
            string memberCode =
                "\n" +
                "    /// <returns>Foo\n" +
                "    int M() { return 0; }\n";

            string source = Wrapper.WrapInClass(memberCode);
            SyntaxTree tree = CSharpSyntaxTree.ParseText(source);

            var findings = XmlDocWellFormedChecker.FindMalformedTags(tree, filePath: "InMemory.cs");

            Finding finding = Assert.Single(findings);
            Assert.Equal("DOC120", finding.Smell.Id);
            Assert.Equal("returns", finding.TagName);

            // Preserve the previously asserted location.
            Assert.Equal(4, finding.Line);
            Assert.Equal(9, finding.Column);
        }

        /// <summary>
        /// Ensures that placeholder formatting for unknown tags includes the tag name in the message.
        /// </summary>
        [Fact]
        public void UnknownTag_Message_Contains_TagName()
        {
            string source =
                "/// <summray>Test</summray>\n" +
                "int M() { return 0; }\n";

            string wrapped = Wrapper.WrapInClass(source);
            SyntaxTree tree = CSharpSyntaxTree.ParseText(wrapped);

            var findings = XmlDocWellFormedChecker.FindMalformedTags(tree, filePath: "InMemory.cs");

            Finding finding = Assert.Single(findings);
            Assert.Equal("DOC110", finding.Smell.Id);

            Assert.Contains("summray", finding.Message);
        }
    }
}
