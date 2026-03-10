using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using XMLDocNormalizer.Checks.Infrastructure;
using XMLDocNormalizer.Models;
using XMLDocNormalizer.Utils;

namespace XMLDocNormalizer.Checks
{
    /// <summary>
    /// Detects exception documentation smells that require semantic analysis.
    /// </summary>
    internal static class XmlDocExceptionSemanticDetector
    {
        /// <summary>
        /// Scans the syntax tree and returns exception-related findings that require semantic analysis.
        /// </summary>
        /// <param name="tree">The syntax tree to analyze.</param>
        /// <param name="filePath">The file path used for reporting.</param>
        /// <param name="semanticModel">The semantic model for the syntax tree.</param>
        /// <returns>A list of findings.</returns>
        public static List<Finding> FindExceptionSmells(
            SyntaxTree tree,
            string filePath,
            SemanticModel semanticModel)
        {
            List<Finding> findings = new();

            CompilationUnitSyntax root = tree.GetCompilationUnitRoot();

            IEnumerable<MemberDeclarationSyntax> members =
                root.DescendantNodes()
                    .OfType<MemberDeclarationSyntax>();

            foreach (MemberDeclarationSyntax member in members)
            {
                DocumentationCommentTriviaSyntax? doc = XmlDocUtils.TryGetDocComment(member);
                if (doc == null)
                {
                    continue;
                }

                List<ExtractedXmlDocTag> tags =
                    XmlDocTagExtraction.ExtractTags(doc, "exception", ExtractExceptionCref);

                AddInvalidExceptionCrefFindings(
                    findings,
                    tree,
                    filePath,
                    semanticModel,
                    tags);

                AddExceptionCrefNotExceptionTypeFindings(
                    findings,
                    tree,
                    filePath,
                    semanticModel,
                    tags);
            }

            return findings;
        }

        /// <summary>
        /// Extracts the raw cref value from an <exception> XML element.
        /// </summary>
        /// <param name="element">The exception XML element.</param>
        /// <returns>The raw cref value if present; otherwise null.</returns>
        private static string? ExtractExceptionCref(XmlElementSyntax element)
        {
            XmlDocTagExtraction.TryGetCrefAttributeValue(element, out string? cref);
            return cref;
        }

        /// <summary>
        /// Adds DOC660 findings for exception tags whose cref cannot be resolved to a known type.
        /// </summary>
        /// <param name="findings">The finding sink.</param>
        /// <param name="tree">The syntax tree.</param>
        /// <param name="filePath">The file path.</param>
        /// <param name="semanticModel">The semantic model for the syntax tree.</param>
        /// <param name="tags">The extracted exception tags.</param>
        private static void AddInvalidExceptionCrefFindings(
            List<Finding> findings,
            SyntaxTree tree,
            string filePath,
            SemanticModel semanticModel,
            List<ExtractedXmlDocTag> tags)
        {
            foreach (ExtractedXmlDocTag tag in tags)
            {
                if (string.IsNullOrWhiteSpace(tag.RawAttributeValue))
                {
                    // DOC600 handles missing cref.
                    continue;
                }

                XmlCrefAttributeSyntax? crefAttribute =
                    SyntaxUtils.GetAttribute<XmlCrefAttributeSyntax>(tag.Element, "cref");

                if (crefAttribute == null || crefAttribute.Cref == null)
                {
                    // DOC600 / well-formedness checks handle malformed or missing cref syntax.
                    continue;
                }

                SymbolInfo symbolInfo = semanticModel.GetSymbolInfo(crefAttribute.Cref);

                if (symbolInfo.Symbol != null)
                {
                    continue;
                }

                findings.Add(FindingFactory.AtPosition(
                    tree,
                    filePath,
                    tagName: "exception",
                    XmlDocSmells.InvalidExceptionCref,
                    crefAttribute.SpanStart,
                    snippet: SyntaxUtils.GetSnippet(tag.Element),
                    tag.RawAttributeValue));
            }
        }

        /// <summary>
        /// Adds DOC670 findings for exception tags whose cref resolves
        /// to a symbol that is not an exception type.
        /// </summary>
        /// <param name="findings">The finding sink.</param>
        /// <param name="tree">The syntax tree.</param>
        /// <param name="filePath">The file path.</param>
        /// <param name="semanticModel">The semantic model.</param>
        /// <param name="tags">The extracted exception tags.</param>
        private static void AddExceptionCrefNotExceptionTypeFindings(
            List<Finding> findings,
            SyntaxTree tree,
            string filePath,
            SemanticModel semanticModel,
            List<ExtractedXmlDocTag> tags)
        {
            INamedTypeSymbol? exceptionBase =
                semanticModel.Compilation.GetTypeByMetadataName("System.Exception");

            if (exceptionBase == null)
            {
                return;
            }

            foreach (ExtractedXmlDocTag tag in tags)
            {
                if (string.IsNullOrWhiteSpace(tag.RawAttributeValue))
                {
                    continue;
                }

                XmlCrefAttributeSyntax? crefAttribute =
                    SyntaxUtils.GetAttribute<XmlCrefAttributeSyntax>(tag.Element, "cref");

                if (crefAttribute == null || crefAttribute.Cref == null)
                {
                    continue;
                }

                SymbolInfo symbolInfo = semanticModel.GetSymbolInfo(crefAttribute.Cref);

                if (symbolInfo.Symbol is not INamedTypeSymbol typeSymbol)
                {
                    continue;
                }

                // Skip if already invalid (DOC660)
                if (symbolInfo.Symbol == null)
                {
                    continue;
                }

                if (!typeSymbol.InheritsFromOrEquals(exceptionBase))
                {
                    findings.Add(FindingFactory.AtPosition(
                        tree,
                        filePath,
                        tagName: "exception",
                        XmlDocSmells.ExceptionCrefNotExceptionType,
                        crefAttribute.SpanStart,
                        snippet: SyntaxUtils.GetSnippet(tag.Element),
                        tag.RawAttributeValue));
                }
            }
        }

        /// <summary>
        /// Determines whether the specified type is identical to or derives from the given base type.
        /// </summary>
        /// <param name="type">The type to inspect.</param>
        /// <param name="baseType">The base type that should be matched or inherited from.</param>
        /// <returns>
        /// <see langword="true"/> if <paramref name="type"/> is the same as
        /// <paramref name="baseType"/> or derives from it; otherwise <see langword="false"/>.
        /// </returns>
        /// <remarks>
        /// This helper walks the inheritance chain of the inspected type using
        /// <see cref="INamedTypeSymbol.BaseType"/> and compares each level using
        /// <see cref="SymbolEqualityComparer.Default"/>.
        /// </remarks>
        public static bool InheritsFromOrEquals(
            this INamedTypeSymbol type,
            INamedTypeSymbol baseType)
        {
            INamedTypeSymbol? current = type;

            while (current != null)
            {
                if (SymbolEqualityComparer.Default.Equals(current, baseType))
                {
                    return true;
                }

                current = current.BaseType;
            }

            return false;
        }
    }
}
