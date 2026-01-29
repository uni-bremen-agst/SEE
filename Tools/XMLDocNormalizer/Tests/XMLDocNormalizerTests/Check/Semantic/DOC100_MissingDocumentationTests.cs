using XMLDocNormalizer.Configuration;
using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Semantic
{
    /// <summary>
    /// Tests for DOC100 (MissingDocumentation) across all analyzed declaration kinds.
    /// </summary>
    public sealed class DOC100_MissingDocumentationTests
    {
        /// <summary>
        /// Ensures that a member without any XML doc comment triggers DOC100.
        /// </summary>
        [Fact]
        public void MemberWithoutDoc_IsDetected()
        {
            string member =
                "public void M() { }\n";

            List<Finding> findings = CheckAssert.FindBasicFindingsForMember(member);

            Finding finding = Assert.Single(findings);
            Assert.Equal("DOC100", finding.Smell.Id);
            Assert.Equal("documentation", finding.TagName);
        }

        /// <summary>
        /// Provides code samples for declaration kinds that are expected to produce exactly one DOC100 finding.
        /// Enum declarations are intentionally excluded because enum members may produce additional DOC100 findings
        /// depending on <see cref="XmlDocOptions.CheckEnumMembers"/>.
        /// </summary>
        /// <returns>An enumeration of test cases for the DOC100 rule.</returns>
        public static IEnumerable<object[]> DeclarationSources_ExcludeEnums()
        {
            // Top-level type declarations (excluding enum)
            yield return new object[] { "public class C\n{\n}\n" };
            yield return new object[] { "public struct S\n{\n}\n" };
            yield return new object[] { "public interface I\n{\n}\n" };
            yield return new object[] { "public delegate void D();\n" };

            // Members inside a type
            yield return new object[] { "public void M() { }\n" };
            yield return new object[] { "public Wrapper() { }\n" };
            yield return new object[] { "public int P { get; }\n" };
            yield return new object[] { "public event System.EventHandler? Evt;\n" }; // EventFieldDeclarationSyntax
            yield return new object[] { "public int Field;\n" }; // FieldDeclarationSyntax
        }

        /// <summary>
        /// Ensures that missing XML documentation is detected for each declaration kind and yields exactly one DOC100 finding.
        /// </summary>
        /// <param name="code">The code snippet to analyze.</param>
        [Theory]
        [MemberData(nameof(DeclarationSources_ExcludeEnums))]
        public void MissingDocumentation_IsDetected_ForEachDeclarationKind(string code)
        {
            XmlDocOptions options = new()
            {
                CheckEnumMembers = true,
                RequireSummaryForFields = true
            };

            List<Finding> findings = Run(code, options);

            List<Finding> doc100 = findings.Where(f => f.Smell.Id == "DOC100").ToList();

            Assert.Single(doc100);
            Assert.Equal("documentation", doc100[0].TagName);
        }

        /// <summary>
        /// Ensures that enum members are reported when enum-member checking is enabled.
        /// </summary>
        [Fact]
        public void Enum_WhenEnumMembersEnabled_ReportsTypeAndMember()
        {
            string source =
                "public enum E\n" +
                "{\n" +
                "    A,\n" +
                "}\n";

            XmlDocOptions options = new()
            {
                CheckEnumMembers = true,
                RequireSummaryForFields = true
            };

            List<Finding> findings = CheckAssert.FindBasicFindingsForSource(source, options);

            List<Finding> doc100 = findings.Where(f => f.Smell.Id == "DOC100").ToList();

            Assert.True(doc100.Count >= 2);
            Assert.All(doc100, f => Assert.Equal("documentation", f.TagName));
        }

        /// <summary>
        /// Ensures that enum members are not reported when enum-member checking is disabled.
        /// </summary>
        [Fact]
        public void Enum_WhenEnumMembersDisabled_ReportsTypeOnly()
        {
            string source =
                "public enum E\n" +
                "{\n" +
                "    A,\n" +
                "}\n";

            XmlDocOptions options = new()
            {
                CheckEnumMembers = false,
                RequireSummaryForFields = true
            };

            List<Finding> findings = CheckAssert.FindBasicFindingsForSource(source, options);

            List<Finding> doc100 = findings.Where(f => f.Smell.Id == "DOC100").ToList();

            Assert.Single(doc100);
            Assert.Equal("documentation", doc100[0].TagName);
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
