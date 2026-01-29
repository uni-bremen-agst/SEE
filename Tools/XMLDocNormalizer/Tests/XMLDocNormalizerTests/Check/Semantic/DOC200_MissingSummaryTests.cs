using System.Collections.Generic;
using System.Linq;
using XMLDocNormalizer.Configuration;
using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Semantic
{
    /// <summary>
    /// Tests for DOC200 (MissingSummary) across all analyzed declaration kinds.
    /// </summary>
    public sealed class DOC200_MissingSummaryTests
    {
        /// <summary>
        /// Provides code samples for declaration kinds that must report DOC200 when a doc comment exists
        /// but no &lt;summary&gt; is present. Enum declarations are excluded here to avoid additional DOC100 noise
        /// from enum members when <see cref="XmlDocOptions.CheckEnumMembers"/> is enabled.
        /// </summary>
        /// <returns>An enumeration of test cases for the DOC200 rule.</returns>
        public static IEnumerable<object[]> DeclarationSources_ExcludeEnums()
        {
            // Top-level type declarations (excluding enum)
            yield return new object[] { "/// <remarks>no summary</remarks>\npublic class C\n{\n}\n" };
            yield return new object[] { "/// <remarks>no summary</remarks>\npublic struct S\n{\n}\n" };
            yield return new object[] { "/// <remarks>no summary</remarks>\npublic interface I\n{\n}\n" };
            yield return new object[] { "/// <remarks>no summary</remarks>\npublic delegate void D();\n" };

            // Members inside a type
            yield return new object[] { "/// <remarks>no summary</remarks>\npublic void M() { }\n" };
            yield return new object[] { "/// <remarks>no summary</remarks>\npublic Wrapper() { }\n" };
            yield return new object[] { "/// <remarks>no summary</remarks>\npublic int P { get; }\n" };
        }

        /// <summary>
        /// Ensures that DOC200 is produced for each declaration kind when summary is missing.
        /// </summary>
        /// <param name="code">The code snippet to analyze.</param>
        [Theory]
        [MemberData(nameof(DeclarationSources_ExcludeEnums))]
        public void MissingSummary_IsDetected_ForEachDeclarationKind(string code)
        {
            XmlDocOptions options = new XmlDocOptions
            {
                CheckEnumMembers = false,
                RequireSummaryForFields = true
            };

            List<Finding> findings = Run(code, options);

            Assert.Contains(findings, f => f.Smell.Id == "DOC200" && f.TagName == "summary");
        }

        /// <summary>
        /// Ensures that fields report DOC200 when field summaries are required.
        /// </summary>
        [Fact]
        public void Field_WhenSummaryRequired_ReportsDOC200()
        {
            string member =
                "/// <remarks>no summary</remarks>\n" +
                "public int Field;\n";

            XmlDocOptions options = new XmlDocOptions
            {
                CheckEnumMembers = true,
                RequireSummaryForFields = true
            };

            List<Finding> findings = CheckAssert.FindBasicFindingsForMember(member, options);

            Assert.Contains(findings, f => f.Smell.Id == "DOC200" && f.TagName == "summary");
        }

        /// <summary>
        /// Ensures that fields do not report DOC200 when field summaries are not required.
        /// </summary>
        [Fact]
        public void Field_WhenSummaryNotRequired_DoesNotReportDOC200()
        {
            string member =
                "/// <remarks>no summary</remarks>\n" +
                "public int Field;\n";

            XmlDocOptions options = new XmlDocOptions
            {
                CheckEnumMembers = true,
                RequireSummaryForFields = false
            };

            List<Finding> findings = CheckAssert.FindBasicFindingsForMember(member, options);

            Assert.DoesNotContain(findings, f => f.Smell.Id == "DOC200");
        }

        /// <summary>
        /// Ensures that event fields report DOC200 when field summaries are required.
        /// </summary>
        [Fact]
        public void EventField_WhenSummaryRequired_ReportsDOC200()
        {
            string member =
                "/// <remarks>no summary</remarks>\n" +
                "public event System.EventHandler? Evt;\n";

            XmlDocOptions options = new XmlDocOptions
            {
                CheckEnumMembers = true,
                RequireSummaryForFields = true
            };

            List<Finding> findings = CheckAssert.FindBasicFindingsForMember(member, options);

            Assert.Contains(findings, f => f.Smell.Id == "DOC200" && f.TagName == "summary");
        }

        /// <summary>
        /// Ensures that event fields do not report DOC200 when field summaries are not required.
        /// </summary>
        [Fact]
        public void EventField_WhenSummaryNotRequired_DoesNotReportDOC200()
        {
            string member =
                "/// <remarks>no summary</remarks>\n" +
                "public event System.EventHandler? Evt;\n";

            XmlDocOptions options = new XmlDocOptions
            {
                CheckEnumMembers = true,
                RequireSummaryForFields = false
            };

            List<Finding> findings = CheckAssert.FindBasicFindingsForMember(member, options);

            Assert.DoesNotContain(findings, f => f.Smell.Id == "DOC200");
        }

        /// <summary>
        /// Runs the basic detector on either a top-level declaration or a wrapped member snippet,
        /// depending on the provided code.
        /// </summary>
        /// <param name="code">
        /// The code snippet to analyze. This is either a complete top-level declaration (e.g. a class)
        /// or a member declaration intended to be wrapped into a containing type.
        /// </param>
        /// <param name="options">The documentation options to apply.</param>
        /// <returns>The produced list of findings.</returns>
        private static List<Finding> Run(string code, XmlDocOptions options)
        {
            if (DeclarationTestHelpers.IsTopLevelDeclaration(code))
            {
                return CheckAssert.FindBasicFindingsForSource(code, options);
            }

            return CheckAssert.FindBasicFindingsForMember(code, options);
        }
    }
}
