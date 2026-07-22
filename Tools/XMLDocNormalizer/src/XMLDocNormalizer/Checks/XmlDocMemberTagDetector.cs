using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using XMLDocNormalizer.Checks.Infrastructure;
using XMLDocNormalizer.Models;
using XMLDocNormalizer.Utils;

namespace XMLDocNormalizer.Checks
{
    /// <summary>
    /// Detects XML documentation tags that are not allowed on the documented member kind.
    /// </summary>
    /// <remarks>
    /// This detector reports invalid generic tag placement.
    /// Specialized tags that are handled by dedicated detectors are skipped here to avoid duplicate findings.
    /// </remarks>
    internal static class XmlDocMemberTagDetector
    {
        /// <summary>
        /// Analyzes a syntax tree and returns findings for XML documentation tags that are not allowed on their owner member.
        /// </summary>
        /// <param name="tree">The syntax tree containing members to check.</param>
        /// <param name="filePath">The source file path used in reporting findings.</param>
        /// <returns>
        /// A list of findings for invalid XML documentation tags on members.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="filePath"/> or the name of an invalid XML
        /// documentation tag is <see langword="null"/>, empty, or consists only
        /// of white-space characters and a finding is created.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when the source position of an invalid XML documentation tag
        /// does not identify a valid position in <paramref name="tree"/>.
        /// </exception>
        public static List<Finding> FindInvalidTags(SyntaxTree tree, string filePath)
        {
            List<Finding> findings = new List<Finding>();

            IEnumerable<SyntaxNode> nodes = tree
                .GetRoot()
                .DescendantNodes()
                .Where(node =>
                    node is MemberDeclarationSyntax
                    || node is EnumMemberDeclarationSyntax);

            foreach (SyntaxNode node in nodes)
            {
                DocumentationCommentTriviaSyntax? doc = XmlDocUtils.TryGetDocComment(node);

                if (doc == null)
                {
                    continue;
                }

                IEnumerable<XmlElementSyntax> elements = XmlDocElementQuery.Elements(doc);

                foreach (XmlElementSyntax element in elements)
                {
                    string tagName = element.StartTag.Name.LocalName.Text;

                    if (AllowedTagMatrix.IsHandledBySpecializedDetector(tagName))
                    {
                        continue;
                    }

                    if (AllowedTagMatrix.IsTagAllowed(node, tagName))
                    {
                        continue;
                    }

                    if (IsCoveredBySpecializedInvalidTargetRule(node, tagName))
                    {
                        continue;
                    }

                    findings.Add(FindingFactory.AtSpanStart(
                        tree,
                        filePath,
                        tagName,
                        XmlDocSmells.InvalidTagOnMember,
                        element.Span,
                        FindingContextBuilder.ForDeclaration(
                            node,
                            "InvalidTagUsage",
                            targetName: tagName,
                            filePath: filePath),
                        snippet: SyntaxUtils.GetSnippet(element)));
                }
            }

            return findings;
        }

        /// <summary>
        /// Determines whether the invalid tag placement is already covered by a more specific detector.
        /// </summary>
        /// <param name="node">The syntax node being documented.</param>
        /// <param name="tagName">The XML documentation tag name.</param>
        /// <returns>
        /// True if a more specific invalid-target rule covers the tag placement; otherwise false.
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
        /// Determines whether a returns tag placement is handled by a specific returns rule.
        /// </summary>
        /// <param name="node">The syntax node being documented.</param>
        /// <returns>
        /// True if a specific returns rule covers the tag placement; otherwise false.
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
        /// Determines whether an exception tag placement is handled by a specific exception rule.
        /// </summary>
        /// <param name="node">The syntax node being documented.</param>
        /// <returns>
        /// True if a specific exception rule covers the tag placement; otherwise false.
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
