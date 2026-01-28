using System.Linq;
using Xunit;
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
            string memberCode =
                "    /// <foo>bar</foo>\n" +
                "    public void M() { }\n";

            // Snapshot-assert for stable line/column + message.
            CheckAssert.MemberEquals(
                memberCode,
                "[6,9] <foo>: Unknown XML documentation tag <foo>.");

            // Additional checks that are not part of the snapshot format (e.g. snippet).
            var findings = CheckAssert.FindWellFormedFindingsForMember(memberCode);

            Finding finding = Assert.Single(findings);
            Assert.Equal("DOC110", finding.Smell.Id);
            Assert.Equal("InMemory.cs", finding.FilePath);
            Assert.Equal("foo", finding.TagName);

            // Snippet is not included in MemberEquals output by design, so we assert it here.
            Assert.False(string.IsNullOrWhiteSpace(finding.Snippet));
            Assert.Contains("<foo>", finding.Snippet);
        }

        /// <summary>
        /// Ensures that findings created from raw documentation comment scanning (absolute position)
        /// report stable line/column positions.
        /// </summary>
        [Fact]
        public void MissingEndTag_from_raw_scan_reports_expected_line_column()
        {
            string memberCode =
                "    /// <summary>\n" +
                "    /// hello\n" +
                "    public void M() { }\n";

            // Snapshot-assert for stable line/column + message.
            CheckAssert.MemberEquals(
                memberCode,
                "[6,9] <summary>: Missing end tag (unclosed XML element).");

            // Additional checks: snippet must be empty for raw-scan findings.
            var findings = CheckAssert.FindWellFormedFindingsForMember(memberCode);

            Finding finding = Assert.Single(findings);
            Assert.Equal("DOC120", finding.Smell.Id);
            Assert.Equal("summary", finding.TagName);
            Assert.Equal(string.Empty, finding.Snippet);
        }

        /// <summary>
        /// Ensures that an unclosed tag reports the expected line/column.
        /// This corresponds to the previous ReturnsChecker "leading blank line" scenario.
        /// </summary>
        [Fact]
        public void MissingEndTag_WithLeadingBlankLine_ReportsExpectedLocation()
        {
            string memberCode =
                "    /// <returns>Foo\n" +
                "    int M() { return 0; }\n";

            // Wrapper produces:
            // 1. /// <summary>
            // 2. /// This is a test class.
            // 3. /// </summary>
            // 4: class C
            // 5: {
            // 6:     /// <returns>Foo
            // => '<' at column 9.
            CheckAssert.MemberEquals(
                memberCode,
                "[6,9] <returns>: Missing end tag (unclosed XML element).");
        }

        /// <summary>
        /// Ensures that placeholder formatting for unknown tags includes the tag name in the message.
        /// </summary>
        [Fact]
        public void UnknownTag_Message_Contains_TagName()
        {
            string memberCode =
                "/// <summray>Test</summray>\n" +
                "int M() { return 0; }\n";

            // Wrapper produces:
            // 1. /// <summary>
            // 2. /// This is a test class.
            // 3. /// </summary>
            // 4: class C
            // 5: {
            // 6: /// <summray>Test</summray>
            // => '<' at column 5.
            CheckAssert.MemberEquals(
                memberCode,
                "[6,5] <summray>: Unknown XML documentation tag <summray>.");
        }
    }
}
