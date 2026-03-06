using XMLDocNormalizer.Configuration;
using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Syntax.Basic
{
    /// <summary>
    /// Tests for missing-documentation smells across all supported declaration kinds.
    /// </summary>
    public sealed class DOC110To130_MissingDocumentationTests
    {
        /// <summary>
        /// Ensures that each supported declaration kind without XML documentation
        /// produces exactly one declaration-specific missing-documentation finding.
        /// </summary>
        [Fact]
        public void MemberWithoutDoc_IsDetected()
        {
            string member =
                "public void M() { }\n";

            List<Finding> findings = CheckAssert.FindBasicFindingsForMember(member);

            Finding finding = Assert.Single(findings);
            Assert.Equal(XmlDocSmells.MissingMethodDocumentation.ID, finding.Smell.ID);
            Assert.Equal("documentation", finding.TagName);
        }

        /// <summary>
        /// Provides declaration samples that are expected to produce exactly one specific
        /// missing-documentation finding.
        /// </summary>
        /// <returns>An enumeration of declaration code and expected smell id.</returns>
        public static IEnumerable<object[]> DeclarationSources()
        {
            // Top-level types
            yield return new object[] { "public class C\n{\n}\n", XmlDocSmells.MissingClassDocumentation.ID };
            yield return new object[] { "public struct S\n{\n}\n", XmlDocSmells.MissingStructDocumentation.ID };
            yield return new object[] { "public interface I\n{\n}\n", XmlDocSmells.MissingInterfaceDocumentation.ID };
            yield return new object[] { "public enum E\n{\n}\n", XmlDocSmells.MissingEnumDocumentation.ID };
            yield return new object[] { "public delegate void D();\n", XmlDocSmells.MissingDelegateDocumentation.ID };
            yield return new object[] { "public record R;\n", XmlDocSmells.MissingRecordDocumentation.ID };
            yield return new object[] { "public record struct RS;\n", XmlDocSmells.MissingRecordStructDocumentation.ID };

            // Members inside a type
            yield return new object[] { "public void M() { }\n", XmlDocSmells.MissingMethodDocumentation.ID };
            yield return new object[] { "public int P { get; }\n", XmlDocSmells.MissingPropertyDocumentation.ID };
            yield return new object[] { "public int this[int i] => i;\n", XmlDocSmells.MissingIndexerDocumentation.ID };
            yield return new object[] { "public int Field;\n", XmlDocSmells.MissingFieldDocumentation.ID };
            yield return new object[] { "public event System.EventHandler? Evt;\n", XmlDocSmells.MissingEventFieldDocumentation.ID };
            yield return new object[]
            {
                "public event System.EventHandler? Changed\n" +
                "{\n" +
                "    add { }\n" +
                "    remove { }\n" +
                "}\n",
                XmlDocSmells.MissingEventDocumentation.ID
            };
        }

        /// <summary>
        /// Ensures that missing XML documentation is detected for each supported declaration kind
        /// and yields exactly one expected smell.
        /// </summary>
        /// <param name="code">The code snippet to analyze.</param>
        /// <param name="expectedSmellId">The expected smell id.</param>
        [Theory]
        [MemberData(nameof(DeclarationSources))]
        public void MissingDocumentation_IsDetected_ForEachDeclarationKind(string code, string expectedSmellId)
        {
            XmlDocOptions options = new()
            {
                CheckEnumMembers = true,
                RequireSummaryForFields = true
            };

            List<Finding> findings = Run(code, options);

            Finding finding = Assert.Single(findings);
            Assert.Equal(expectedSmellId, finding.Smell.ID);
            Assert.Equal("documentation", finding.TagName);
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

            FindingAsserts.ContainsSmellTimes(findings, XmlDocSmells.MissingEnumDocumentation.ID, 1);
            FindingAsserts.ContainsSmellTimes(findings, XmlDocSmells.MissingEnumMemberDocumentation.ID, 1);

            Assert.All(
                findings.Where(f =>
                    f.Smell.ID == XmlDocSmells.MissingEnumDocumentation.ID ||
                    f.Smell.ID == XmlDocSmells.MissingEnumMemberDocumentation.ID),
                f => Assert.Equal("documentation", f.TagName));
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

            Finding finding = Assert.Single(findings);
            Assert.Equal(XmlDocSmells.MissingEnumDocumentation.ID, finding.Smell.ID);
            Assert.Equal("documentation", finding.TagName);
        }

        /// <summary>
        /// Runs the basic detector on either a top-level declaration or a wrapped member snippet,
        /// depending on the provided code.
        /// </summary>
        /// <param name="code">
        /// The code snippet to analyze. This is either a complete top-level declaration
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

        /// <summary>
        /// Ensures that a constructor without XML documentation triggers the constructor-specific
        /// missing-documentation smell.
        /// </summary>
        [Fact]
        public void ConstructorWithoutDoc_IsDetected()
        {
            string source = Wrapper.WrapInClass(
                "    public C() { }");

            List<Finding> findings = CheckAssert.FindBasicFindingsForSource(source);

            Finding finding = Assert.Single(
                findings.Where(f => f.Smell.ID == XmlDocSmells.MissingConstructorDocumentation.ID));

            Assert.Equal("documentation", finding.TagName);
        }

        /// <summary>
        /// Ensures that a destructor without XML documentation triggers the destructor-specific
        /// missing-documentation smell.
        /// </summary>
        [Fact]
        public void DestructorWithoutDoc_IsDetected()
        {
            string source = Wrapper.WrapInClass(
                "    ~C() { }");

            List<Finding> findings = CheckAssert.FindBasicFindingsForSource(source);

            Finding finding = Assert.Single(
                findings.Where(f => f.Smell.ID == XmlDocSmells.MissingDestructorDocumentation.ID));

            Assert.Equal("documentation", finding.TagName);
        }

        /// <summary>
        /// Ensures that an operator without XML documentation triggers the operator-specific
        /// missing-documentation smell.
        /// </summary>
        [Fact]
        public void OperatorWithoutDoc_IsDetected()
        {
            string source = Wrapper.WrapInClass(
                "    public static C operator +(C left, C right) => left;");

            List<Finding> findings = CheckAssert.FindBasicFindingsForSource(source);

            Finding finding = Assert.Single(
                findings.Where(f => f.Smell.ID == XmlDocSmells.MissingOperatorDocumentation.ID));

            Assert.Equal("documentation", finding.TagName);
        }

        /// <summary>
        /// Ensures that a conversion operator without XML documentation triggers the conversion-operator-specific
        /// missing-documentation smell.
        /// </summary>
        [Fact]
        public void ConversionOperatorWithoutDoc_IsDetected()
        {
            string source = Wrapper.WrapInClass(
                "    public static implicit operator int(C value) => 0;");

            List<Finding> findings = CheckAssert.FindBasicFindingsForSource(source);

            Finding finding = Assert.Single(
                findings.Where(f => f.Smell.ID == XmlDocSmells.MissingConversionOperatorDocumentation.ID));

            Assert.Equal("documentation", finding.TagName);
        }
    }
}