using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using XMLDocNormalizer.Checks.Infrastructure;
using XMLDocNormalizer.Models;
using XMLDocNormalizer.Utils;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Syntax.InvalidTag
{
    /// <summary>
    /// Tests DOC143 – InvalidTagOnMember for all relevant member types and XML tags.
    /// Ensures that forbidden tags are detected and allowed tags do not trigger DOC140.
    /// </summary>
    public sealed class DOC143_FullMatrixTests
    {
        /// <summary>
        /// All XML documentation tags to be tested by the generic invalid-tag detector.
        /// Tags handled by specialized detectors, such as <value>, are excluded here.
        /// </summary>
        private static readonly string[] Tags =
        {
            "summary", "remarks", "example", "seealso", "see", "inheritdoc",
            "param", "typeparam", "returns", "exception"
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
            bool isCoveredBySpecializedRule = IsCoveredBySpecializedInvalidTargetRule(node, tag);

            if (isAllowed || isCoveredBySpecializedRule)
            {
                Assert.DoesNotContain(findings, f => f.Smell.ID == XmlDocSmells.InvalidTagOnMember.ID);
            }
            else
            {
                FindingAsserts.HasExactlySmells(findings, XmlDocSmells.InvalidTagOnMember.ID);
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

        /// <summary>
        /// Determines whether a tag placement is expected to be covered by a more specific detector.
        /// </summary>
        /// <param name="node">The syntax node being documented.</param>
        /// <param name="tagName">The XML documentation tag name.</param>
        /// <returns>
        /// True if a more specific detector covers the tag placement; otherwise false.
        /// </returns>
        private static bool IsCoveredBySpecializedInvalidTargetRule(SyntaxNode node, string tagName)
        {
            if (tagName == "returns")
            {
                return HasSpecificReturnsInvalidTargetRule(node);
            }

            if (tagName == "exception")
            {
                return HasSpecificExceptionInvalidTargetRule(node);
            }

            return false;
        }

        /// <summary>
        /// Determines whether a returns tag placement is covered by a more specific returns rule.
        /// </summary>
        /// <param name="node">The syntax node being documented.</param>
        /// <returns>
        /// True if a more specific returns rule covers the tag placement; otherwise false.
        /// </returns>
        private static bool HasSpecificReturnsInvalidTargetRule(SyntaxNode node)
        {
            if (node is MethodDeclarationSyntax methodDeclaration)
            {
                return IsVoidReturnType(methodDeclaration.ReturnType);
            }

            if (node is DelegateDeclarationSyntax delegateDeclaration)
            {
                return IsVoidReturnType(delegateDeclaration.ReturnType);
            }

            if (node is OperatorDeclarationSyntax operatorDeclaration)
            {
                return IsVoidReturnType(operatorDeclaration.ReturnType);
            }

            if (node is PropertyDeclarationSyntax propertyDeclaration)
            {
                return IsWriteOnlyProperty(propertyDeclaration);
            }

            return false;
        }

        /// <summary>
        /// Determines whether an exception tag placement is covered by a more specific exception rule.
        /// </summary>
        /// <param name="node">The syntax node being documented.</param>
        /// <returns>
        /// True if a more specific exception rule covers the tag placement; otherwise false.
        /// </returns>
        private static bool HasSpecificExceptionInvalidTargetRule(SyntaxNode node)
        {
            if (node is not MemberDeclarationSyntax member)
            {
                return false;
            }

            return SyntaxUtils.IsAbstractMember(member)
                || SyntaxUtils.IsExternMember(member)
                || !SyntaxUtils.HasExecutableBody(member);
        }

        /// <summary>
        /// Determines whether a return type is void.
        /// </summary>
        /// <param name="returnType">The return type syntax to inspect.</param>
        /// <returns>
        /// True if the return type is void; otherwise false.
        /// </returns>
        private static bool IsVoidReturnType(TypeSyntax returnType)
        {
            return returnType is PredefinedTypeSyntax predefinedReturnType
                && predefinedReturnType.Keyword.IsKind(SyntaxKind.VoidKeyword);
        }

        /// <summary>
        /// Determines whether a property has a setter but no getter.
        /// </summary>
        /// <param name="propertyDeclaration">The property declaration to inspect.</param>
        /// <returns>
        /// True if the property is write-only; otherwise false.
        /// </returns>
        private static bool IsWriteOnlyProperty(PropertyDeclarationSyntax propertyDeclaration)
        {
            if (propertyDeclaration.ExpressionBody != null)
            {
                return false;
            }

            if (propertyDeclaration.AccessorList == null)
            {
                return false;
            }

            bool hasGetter = propertyDeclaration.AccessorList.Accessors.Any(
                static accessor => accessor.IsKind(SyntaxKind.GetAccessorDeclaration));

            bool hasSetter = propertyDeclaration.AccessorList.Accessors.Any(
                static accessor => accessor.IsKind(SyntaxKind.SetAccessorDeclaration));

            return hasSetter && !hasGetter;
        }
    }
}
