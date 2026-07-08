using XMLDocNormalizer.Configuration;
using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Syntax.Basic
{
    /// <summary>
    /// Tests for the generic missing-documentation smell across all supported declaration kinds.
    /// </summary>
    public sealed class DOC110To130_MissingDocumentationTests
    {
        /// <summary>
        /// Ensures that a member without XML documentation produces the generic missing-documentation finding.
        /// </summary>
        [Fact]
        public void MemberWithoutDoc_IsDetected()
        {
            string member =
                "public void M() { }\n";

            List<Finding> findings = CheckAssert.FindBasicFindingsForMember(member);

            Finding finding = Assert.Single(findings);
            AssertMissingDocumentationFinding(
                finding,
                expectedOwnerKind: "Method",
                expectedSymbolName: "M",
                expectedMessage: "XML documentation for method 'M' is missing.");
        }

        /// <summary>
        /// Provides declaration samples that are expected to produce exactly one generic missing-documentation finding.
        /// </summary>
        /// <returns>
        /// An enumeration of declaration code, expected owner kind, expected symbol name and expected formatted message.
        /// </returns>
        public static IEnumerable<object[]> DeclarationSources()
        {
            yield return new object[]
            {
                "public class C\n{\n}\n",
                "Class",
                "C",
                "XML documentation for class 'C' is missing."
            };

            yield return new object[]
            {
                "public struct S\n{\n}\n",
                "Struct",
                "S",
                "XML documentation for struct 'S' is missing."
            };

            yield return new object[]
            {
                "public interface I\n{\n}\n",
                "Interface",
                "I",
                "XML documentation for interface 'I' is missing."
            };

            yield return new object[]
            {
                "public enum E\n{\n}\n",
                "Enum",
                "E",
                "XML documentation for enum 'E' is missing."
            };

            yield return new object[]
            {
                "public delegate void D();\n",
                "Delegate",
                "D",
                "XML documentation for delegate 'D' is missing."
            };

            yield return new object[]
            {
                "public record R;\n",
                "Record",
                "R",
                "XML documentation for record 'R' is missing."
            };

            yield return new object[]
            {
                "public record struct RS;\n",
                "RecordStruct",
                "RS",
                "XML documentation for record struct 'RS' is missing."
            };

            yield return new object[]
            {
                "public C() { }\n",
                "Constructor",
                "C",
                "XML documentation for constructor 'C' is missing."
            };

            yield return new object[]
            {
                "public void M() { }\n",
                "Method",
                "M",
                "XML documentation for method 'M' is missing."
            };

            yield return new object[]
            {
                "public int P { get; }\n",
                "Property",
                "P",
                "XML documentation for property 'P' is missing."
            };

            yield return new object[]
            {
                "public int this[int i] => i;\n",
                "Indexer",
                "this[]",
                "XML documentation for indexer 'this[]' is missing."
            };

            yield return new object[]
            {
                "public int Field;\n",
                "Field",
                "Field",
                "XML documentation for field 'Field' is missing."
            };

            yield return new object[]
            {
                "public event System.EventHandler? Evt;\n",
                "EventField",
                "Evt",
                "XML documentation for event 'Evt' is missing."
            };

            yield return new object[]
            {
                "public event System.EventHandler? Changed\n" +
                "{\n" +
                "    add { }\n" +
                "    remove { }\n" +
                "}\n",
                "Event",
                "Changed",
                "XML documentation for event 'Changed' is missing."
            };

            yield return new object[]
            {
                "public static C operator +(C left, C right) => left;\n",
                "Operator",
                "operator +",
                "XML documentation for operator 'operator +' is missing."
            };

            yield return new object[]
            {
                "public static implicit operator int(C value) => 0;\n",
                "ConversionOperator",
                "operator int",
                "XML documentation for conversion operator 'implicit operator int' is missing."
            };

            yield return new object[]
            {
                "~C() { }\n",
                "Destructor",
                "C",
                "XML documentation for destructor '~C' is missing."
            };
        }

        /// <summary>
        /// Ensures that missing XML documentation is detected for each supported declaration kind
        /// and yields the generic missing-documentation smell with declaration context.
        /// </summary>
        /// <param name="code">
        /// The code snippet to analyze.
        /// </param>
        /// <param name="expectedOwnerKind">
        /// The expected owner kind in the finding context.
        /// </param>
        /// <param name="expectedSymbolName">
        /// The expected symbol name in the finding context.
        /// </param>
        /// <param name="expectedMessage">
        /// The expected formatted finding message.
        /// </param>
        [Theory]
        [MemberData(nameof(DeclarationSources))]
        public void MissingDocumentation_IsDetected_ForEachDeclarationKind(
            string code,
            string expectedOwnerKind,
            string expectedSymbolName,
            string expectedMessage)
        {
            XmlDocOptions options = new()
            {
                CheckEnumMembers = true,
                RequireSummaryForFields = true
            };

            List<Finding> findings = Run(code, options);

            Finding finding = Assert.Single(findings);
            AssertMissingDocumentationFinding(
                finding,
                expectedOwnerKind,
                expectedSymbolName,
                expectedMessage);
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

            FindingAsserts.ContainsSmellTimes(findings, XmlDocSmells.MissingDocumentation.ID, 2);

            Finding enumFinding = Assert.Single(findings.Where(f =>
                f.Context.OwnerKind == "Enum"
                && f.Context.SymbolName == "E"));

            AssertMissingDocumentationFinding(
                enumFinding,
                expectedOwnerKind: "Enum",
                expectedSymbolName: "E",
                expectedMessage: "XML documentation for enum 'E' is missing.");

            Finding enumMemberFinding = Assert.Single(findings.Where(f =>
                f.Context.OwnerKind == "EnumMember"
                && f.Context.SymbolName == "A"));

            AssertMissingDocumentationFinding(
                enumMemberFinding,
                expectedOwnerKind: "EnumMember",
                expectedSymbolName: "A",
                expectedMessage: "XML documentation for enum member 'A' is missing.");
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
            AssertMissingDocumentationFinding(
                finding,
                expectedOwnerKind: "Enum",
                expectedSymbolName: "E",
                expectedMessage: "XML documentation for enum 'E' is missing.");
        }

        /// <summary>
        /// Runs the basic detector on either a top-level declaration or a wrapped member snippet,
        /// depending on the provided code.
        /// </summary>
        /// <param name="code">
        /// The code snippet to analyze. This is either a complete top-level declaration
        /// or a member declaration intended to be wrapped into a containing type.
        /// </param>
        /// <param name="options">
        /// The documentation options to apply.
        /// </param>
        /// <returns>
        /// The produced list of findings.
        /// </returns>
        private static List<Finding> Run(string code, XmlDocOptions options)
        {
            if (DeclarationTestHelpers.IsTopLevelDeclaration(code))
            {
                return CheckAssert.FindBasicFindingsForSource(code, options);
            }

            return CheckAssert.FindBasicFindingsForMember(code, options);
        }

        /// <summary>
        /// Asserts that a finding is the generic missing-documentation finding with the expected context.
        /// </summary>
        /// <param name="finding">
        /// The finding to assert.
        /// </param>
        /// <param name="expectedOwnerKind">
        /// The expected owner kind in the finding context.
        /// </param>
        /// <param name="expectedSymbolName">
        /// The expected symbol name in the finding context.
        /// </param>
        /// <param name="expectedMessage">
        /// The expected formatted message.
        /// </param>
        private static void AssertMissingDocumentationFinding(
            Finding finding,
            string expectedOwnerKind,
            string expectedSymbolName,
            string expectedMessage)
        {
            Assert.Equal(XmlDocSmells.MissingDocumentation.ID, finding.Smell.ID);
            Assert.Equal("documentation", finding.TagName);
            Assert.Equal(expectedOwnerKind, finding.Context.OwnerKind);
            Assert.Equal("Declaration", finding.Context.SubjectKind);
            Assert.Equal(expectedSymbolName, finding.Context.SymbolName);
            Assert.Equal(expectedMessage, finding.Message);
            Assert.DoesNotContain("{0}", finding.Message, StringComparison.Ordinal);
            Assert.DoesNotContain("{1}", finding.Message, StringComparison.Ordinal);
        }
    }
}
