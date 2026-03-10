using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using XMLDocNormalizer.Checks.Infrastructure;
using XMLDocNormalizer.Models;
using XMLDocNormalizer.Utils;

namespace XMLDocNormalizer.Checks
{
    /// <summary>
    /// Detects value-related XML documentation smells (DOC800-DOC831).
    /// The implementation is intentionally built up smell by smell.
    /// </summary>
    internal static class XmlDocValueDetector
    {
        /// <summary>
        /// Scans the syntax tree and returns value-related findings.
        /// </summary>
        /// <param name="tree">The syntax tree to analyze.</param>
        /// <param name="filePath">The file path used for reporting.</param>
        /// <returns>A list of findings.</returns>
        public static List<Finding> FindValueSmells(SyntaxTree tree, string filePath)
        {
            List<Finding> findings = new();

            CompilationUnitSyntax root = tree.GetCompilationUnitRoot();

            IEnumerable<MemberDeclarationSyntax> members =
                root.DescendantNodes()
                    .OfType<MemberDeclarationSyntax>();

            foreach (MemberDeclarationSyntax member in members)
            {
                AddMissingValueOnProperty(findings, tree, filePath, member);
                AddMissingValueOnIndexer(findings, tree, filePath, member);
            }

            return findings;
        }

        /// <summary>
        /// Adds DOC800 findings for readable properties that have documentation but no value tag.
        /// </summary>
        /// <param name="findings">The target finding list.</param>
        /// <param name="tree">The syntax tree used for location calculation.</param>
        /// <param name="filePath">The file path used for reporting.</param>
        /// <param name="member">The member to inspect.</param>
        private static void AddMissingValueOnProperty(
            List<Finding> findings,
            SyntaxTree tree,
            string filePath,
            MemberDeclarationSyntax member)
        {
            if (member is not PropertyDeclarationSyntax property)
            {
                return;
            }

            if (!IsReadableProperty(property))
            {
                return;
            }

            DocumentationCommentTriviaSyntax? doc = XmlDocUtils.TryGetDocComment(property);
            if (doc == null)
            {
                // Missing overall documentation is handled by the basic detector.
                return;
            }

            XmlElementSyntax? valueTag = XmlDocElementQuery.FirstByName(doc, "value");
            if (valueTag != null)
            {
                return;
            }

            findings.Add(FindingFactory.AtPosition(
                tree,
                filePath,
                tagName: "value",
                XmlDocSmells.MissingValueOnProperty,
                MemberAnchorResolver.GetAnchorPosition(property),
                snippet: string.Empty,
                property.Identifier.ValueText));
        }

        /// <summary>
        /// Determines whether the property is readable and therefore expected to have a value tag.
        /// </summary>
        /// <param name="property">The property to inspect.</param>
        /// <returns><see langword="true"/> if the property is readable; otherwise <see langword="false"/>.</returns>
        private static bool IsReadableProperty(PropertyDeclarationSyntax property)
        {
            if (property.ExpressionBody != null)
            {
                return true;
            }

            if (property.AccessorList == null)
            {
                return false;
            }

            return property.AccessorList.Accessors.Any(
                static accessor => accessor.Kind() == SyntaxKind.GetAccessorDeclaration);
        }

        /// <summary>
        /// Adds DOC801 findings for indexers that have documentation but no value tag.
        /// </summary>
        /// <param name="findings">The target finding list.</param>
        /// <param name="tree">The syntax tree used for location calculation.</param>
        /// <param name="filePath">The file path used for reporting.</param>
        /// <param name="member">The member to inspect.</param>
        private static void AddMissingValueOnIndexer(
            List<Finding> findings,
            SyntaxTree tree,
            string filePath,
            MemberDeclarationSyntax member)
        {
            if (member is not IndexerDeclarationSyntax indexer)
            {
                return;
            }

            DocumentationCommentTriviaSyntax? doc = XmlDocUtils.TryGetDocComment(indexer);
            if (doc == null)
            {
                // Missing overall documentation is handled by the basic detector.
                return;
            }

            XmlElementSyntax? valueTag = XmlDocElementQuery.FirstByName(doc, "value");
            if (valueTag != null)
            {
                return;
            }

            findings.Add(FindingFactory.AtPosition(
                tree,
                filePath,
                tagName: "value",
                XmlDocSmells.MissingValueOnIndexer,
                MemberAnchorResolver.GetAnchorPosition(indexer),
                snippet: string.Empty));
        }
    }
}