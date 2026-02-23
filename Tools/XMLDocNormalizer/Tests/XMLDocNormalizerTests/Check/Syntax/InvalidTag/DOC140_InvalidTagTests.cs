using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using XMLDocNormalizer.Checks.Infrastructure;
using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Syntax.InvalidTag
{
    /// <summary>
    /// Tests DOC140 – InvalidTagOnMember for all relevant member types and XML tags.
    /// Ensures that forbidden tags are detected and allowed tags do not trigger DOC140.
    /// </summary>
    public sealed class DOC140_FullMatrixTests
    {
        /// <summary>
        /// All XML documentation tags to be tested.
        /// </summary>
        private static readonly string[] Tags =
        {
            "summary", "remarks", "example", "seealso", "see", "inheritdoc",
            "param", "typeparam", "returns", "value", "exception"
        };

        /// <summary>
        /// Member declarations for testing.
        /// </summary>
        private static readonly (string Code, string Kind, bool IsTopLevel, bool IsEnumMember)[] Members =
        {
            // Methods
            ("public void M() {}", "Method", false, false),
            ("public int M() { return 0; }", "MethodWithReturn", false, false),

            // Constructors
            ("public Foo() {}", "Constructor", false, false),
            ("public Foo(int x) {}", "ConstructorWithParam", false, false),

            // Properties / Indexer
            ("public int P { get; set; }", "Property", false, false),
            ("public int this[int i] { get { return i; } }", "Indexer", false, false),

            // Fields
            ("public int F;", "Field", false, false),

            // Delegate
            ("public delegate void D();", "Delegate", false, false),

            // Event
            ("public event System.Action E;", "Event", false, false),

            // --------- Top-Level ---------
            ("public class C {}", "Class", true, false),
            ("public struct S {}", "Struct", true, false),
            ("public interface I {}", "Interface", true, false),
            ("public record R(int X);", "Record", true, false),
            ("public enum E { A }", "Enum", true, false),

            // --------- Enum member (special case) ---------
            ("public enum E { A }", "EnumMember", true, true)
        };


        /// <summary>
        /// Generates all member × tag combinations.
        /// </summary>
        public static IEnumerable<object[]> GenerateAllCombinations()
        {
            foreach ((string code, string kind, bool isTopLevel, bool isEnumMember) in Members)
            {
                foreach (string tag in Tags)
                {
                    yield return new object[] { code, tag, isTopLevel, isEnumMember };
                }
            }
        }

        /// <summary>
        /// Runs the detector on all combinations and asserts correct DOC140 reporting.
        /// </summary>
        [Theory]
        [MemberData(nameof(GenerateAllCombinations))]
        public void Detector_CorrectlyDetectsAllowedAndForbiddenTags(
            string memberCode, string tag, bool isTopLevel, bool isEnumMember)
        {
            string fullSource = GetFullSource();

            List<Finding> findings =
                CheckAssert.FindMemberTagFindingsForSource(fullSource);

            // Check via AllowedTagMatrix
            SyntaxTree tree = CSharpSyntaxTree.ParseText(fullSource);
            SyntaxNode node = GetSyntaxNode();

            bool isAllowed = AllowedTagMatrix.IsTagAllowed(node, tag);

            if (isAllowed)
            {
                Assert.DoesNotContain(findings, f => f.Smell.Id == "DOC140");
            }
            else
            {
                FindingAsserts.HasExactlySmells(findings, "DOC140");
                Finding finding = findings.Single();
                Assert.Equal(tag, finding.TagName);
            }

            string GetFullSource()
            {
                if (isEnumMember)
                {
                    return $@"
                        public enum E 
                        {{
                            /// <{tag}>Test</{tag}>
                            A
                        }}";
                }
                else
                {
                    string memberWithDoc =
                        $"/// <{tag}>Test</{tag}>\n{memberCode}";

                    return isTopLevel
                        ? memberWithDoc
                        : Wrapper.WrapInClass(memberWithDoc);
                }
            }

            SyntaxNode GetSyntaxNode()
            {
                return isEnumMember
                    ? tree.GetRoot()
                            .DescendantNodes()
                            .OfType<EnumMemberDeclarationSyntax>()
                            .First()
                    : tree.GetRoot()
                            .DescendantNodes()
                            .OfType<MemberDeclarationSyntax>()
                            .First(m => isTopLevel || m is not ClassDeclarationSyntax);
            }
        }
    }
}
