using XMLDocNormalizer.Configuration;
using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Syntax.Basic
{
    /// <summary>
    /// Tests for DOC210 (EmptySummary).
    /// </summary>
    public sealed class DOC210_EmptySummaryTests
    {
        /// <summary>
        /// Ensures that an empty summary triggers DOC210 for a regular member.
        /// </summary>
        [Fact]
        public void EmptySummary_ForMethod_IsDetected()
        {
            string member =
                "/// <summary>\n" +
                "/// \n" +
                "/// </summary>\n" +
                "public void M() { }\n";

            XmlDocOptions options = new XmlDocOptions
            {
                CheckEnumMembers = true,
                RequireSummaryForFields = true
            };

            List<Finding> findings = CheckAssert.FindBasicFindingsForMember(member, options);

            Finding finding = Assert.Single(findings.Where(f => f.Smell.Id == "DOC210"));
            Assert.Equal("summary", finding.TagName);
        }

        /// <summary>
        /// Ensures that a summary containing nested elements is not treated as empty.
        /// </summary>
        [Fact]
        public void Summary_WithSee_IsNotEmpty()
        {
            string member =
                "/// <summary><see cref=\"System.String\"/></summary>\n" +
                "public void M() { }\n";

            List<Finding> findings = CheckAssert.FindBasicFindingsForMember(member);

            Assert.DoesNotContain(findings, f => f.Smell.Id == "DOC210");
        }

        /// <summary>
        /// Ensures that fields report DOC210 when field summaries are required.
        /// </summary>
        [Fact]
        public void Field_WhenSummaryRequired_ReportsDOC210()
        {
            string member =
                "/// <summary>\n" +
                "/// \n" +
                "/// </summary>\n" +
                "public int Field;\n";

            XmlDocOptions options = new XmlDocOptions
            {
                CheckEnumMembers = true,
                RequireSummaryForFields = true
            };

            List<Finding> findings = CheckAssert.FindBasicFindingsForMember(member, options);

            Assert.Contains(findings, f => f.Smell.Id == "DOC210");
        }

        /// <summary>
        /// Ensures that fields do not report DOC210 when field summaries are not required.
        /// </summary>
        [Fact]
        public void Field_WhenSummaryNotRequired_DoesNotReportDOC210()
        {
            string member =
                "/// <summary>\n" +
                "/// \n" +
                "/// </summary>\n" +
                "public int Field;\n";

            XmlDocOptions options = new XmlDocOptions
            {
                CheckEnumMembers = true,
                RequireSummaryForFields = false
            };

            List<Finding> findings = CheckAssert.FindBasicFindingsForMember(member, options);

            Assert.DoesNotContain(findings, f => f.Smell.Id == "DOC210");
        }

        /// <summary>
        /// Ensures that event fields do not report DOC210 when field summaries are not required.
        /// </summary>
        [Fact]
        public void EventField_WhenSummaryNotRequired_DoesNotReportDOC210()
        {
            string member =
                "/// <summary>\n" +
                "/// \n" +
                "/// </summary>\n" +
                "public event System.EventHandler? Evt;\n";

            XmlDocOptions options = new XmlDocOptions
            {
                CheckEnumMembers = true,
                RequireSummaryForFields = false
            };

            List<Finding> findings = CheckAssert.FindBasicFindingsForMember(member, options);

            Assert.DoesNotContain(findings, f => f.Smell.Id == "DOC210");
        }
    }
}
