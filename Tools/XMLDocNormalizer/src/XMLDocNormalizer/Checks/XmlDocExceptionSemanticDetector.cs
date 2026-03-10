using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using XMLDocNormalizer.Checks.Infrastructure;
using XMLDocNormalizer.Models;
using XMLDocNormalizer.Utils;

namespace XMLDocNormalizer.Checks
{
    /// <summary>
    /// Detects exception documentation smells that require semantic analysis:
    /// <list type="bullet">
    /// <item><description>DOC610: An exception is thrown directly or transitively but is not documented with an <exception> tag.</description></item>
    /// <item><description>DOC630: An <exception> tag documents an exception that is never thrown.</description></item>
    /// <item><description>DOC660: An <exception> cref cannot be resolved.</description></item>
    /// <item><description>DOC670: An <exception> cref resolves to a symbol that is not an exception type.</description></item>
    /// </list>
    /// </summary>
    internal static class XmlDocExceptionSemanticDetector
    {
        /// <summary>
        /// Scans the syntax tree and returns exception-related findings that require semantic analysis
        /// (DOC610/DOC630/DOC660/DOC670).
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

                AddExceptionTagWithoutThrowFindings(
                    findings,
                    tree,
                    filePath,
                    semanticModel,
                    member,
                    tags);

                AddMissingExceptionTagFindings(
                    findings,
                    tree,
                    filePath,
                    semanticModel,
                    member,
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

        /// <summary>
        /// Adds DOC630 findings for documented exceptions that are not thrown
        /// directly or transitively by the documented member.
        /// </summary>
        /// <param name="findings">The finding sink.</param>
        /// <param name="tree">The syntax tree.</param>
        /// <param name="filePath">The file path.</param>
        /// <param name="semanticModel">The semantic model.</param>
        /// <param name="member">The documented member.</param>
        /// <param name="tags">The extracted exception tags.</param>
        private static void AddExceptionTagWithoutThrowFindings(
            List<Finding> findings,
            SyntaxTree tree,
            string filePath,
            SemanticModel semanticModel,
            MemberDeclarationSyntax member,
            List<ExtractedXmlDocTag> tags)
        {
            INamedTypeSymbol? exceptionBase =
                semanticModel.Compilation.GetTypeByMetadataName("System.Exception");

            if (exceptionBase == null)
            {
                return;
            }

            HashSet<INamedTypeSymbol> thrownExceptions =
                ExceptionFlowAnalyzer.CollectTransitivelyThrownExceptions(member, semanticModel);

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
                    continue;
                }

                SymbolInfo symbolInfo = semanticModel.GetSymbolInfo(crefAttribute.Cref);

                if (symbolInfo.Symbol is not INamedTypeSymbol documentedType)
                {
                    // DOC660 handles unresolved cref.
                    continue;
                }

                if (!documentedType.InheritsFromOrEquals(exceptionBase))
                {
                    // DOC670 handles cref targets that are not exception types.
                    continue;
                }

                if (ContainsExceptionOrDerived(thrownExceptions, documentedType))
                {
                    continue;
                }

                findings.Add(FindingFactory.AtPosition(
                    tree,
                    filePath,
                    tagName: "exception",
                    XmlDocSmells.ExceptionTagWithoutDirectThrow,
                    crefAttribute.SpanStart,
                    snippet: SyntaxUtils.GetSnippet(tag.Element),
                    tag.RawAttributeValue));
            }
        }

        /// <summary>
        /// Determines whether the collected thrown exceptions contain the documented exception
        /// type or a type derived from it.
        /// </summary>
        /// <param name="thrownExceptions">The collected thrown exception types.</param>
        /// <param name="documentedType">The documented exception type.</param>
        /// <returns>
        /// <see langword="true"/> if one of the thrown exception types is identical to or derives from
        /// the documented exception type; otherwise <see langword="false"/>.
        /// </returns>
        private static bool ContainsExceptionOrDerived(
            HashSet<INamedTypeSymbol> thrownExceptions,
            INamedTypeSymbol documentedType)
        {
            foreach (INamedTypeSymbol thrownType in thrownExceptions)
            {
                if (thrownType.InheritsFromOrEquals(documentedType))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Adds DOC610 findings for exceptions that are thrown directly or transitively
        /// by the documented member but are not covered by any <exception> tag.
        /// </summary>
        /// <param name="findings">The finding sink.</param>
        /// <param name="tree">The syntax tree.</param>
        /// <param name="filePath">The file path.</param>
        /// <param name="semanticModel">The semantic model.</param>
        /// <param name="member">The documented member.</param>
        /// <param name="tags">The extracted exception tags.</param>
        private static void AddMissingExceptionTagFindings(
            List<Finding> findings,
            SyntaxTree tree,
            string filePath,
            SemanticModel semanticModel,
            MemberDeclarationSyntax member,
            List<ExtractedXmlDocTag> tags)
        {
            INamedTypeSymbol? exceptionBase =
                semanticModel.Compilation.GetTypeByMetadataName("System.Exception");

            if (exceptionBase == null)
            {
                return;
            }

            HashSet<INamedTypeSymbol> documentedExceptions =
                CollectDocumentedExceptionTypes(tags, semanticModel, exceptionBase);

            HashSet<INamedTypeSymbol> thrownExceptions =
                ExceptionFlowAnalyzer.CollectTransitivelyThrownExceptions(member, semanticModel);

            foreach (INamedTypeSymbol thrownType in thrownExceptions)
            {
                if (!thrownType.InheritsFromOrEquals(exceptionBase))
                {
                    continue;
                }

                if (IsCoveredByDocumentedException(documentedExceptions, thrownType))
                {
                    continue;
                }

                findings.Add(FindingFactory.AtPosition(
                    tree,
                    filePath,
                    tagName: "exception",
                    XmlDocSmells.MissingExceptionTag,
                    MemberAnchorResolver.GetAnchorPosition(member),
                    snippet: string.Empty,
                    thrownType.ToDisplayString()));
            }
        }

        /// <summary>
        /// Collects all documented exception types that are valid exception types.
        /// Invalid cref values and non-exception cref targets are ignored because they are
        /// handled by DOC660 and DOC670.
        /// </summary>
        /// <param name="tags">The extracted exception tags.</param>
        /// <param name="semanticModel">The semantic model.</param>
        /// <param name="exceptionBase">The System.Exception base symbol.</param>
        /// <returns>A set of documented exception types.</returns>
        private static HashSet<INamedTypeSymbol> CollectDocumentedExceptionTypes(
            List<ExtractedXmlDocTag> tags,
            SemanticModel semanticModel,
            INamedTypeSymbol exceptionBase)
        {
            HashSet<INamedTypeSymbol> documented =
                new(SymbolEqualityComparer.Default);

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

                if (symbolInfo.Symbol is not INamedTypeSymbol documentedType)
                {
                    continue;
                }

                if (!documentedType.InheritsFromOrEquals(exceptionBase))
                {
                    continue;
                }

                documented.Add(documentedType);
            }

            return documented;
        }

        /// <summary>
        /// Determines whether the thrown exception type is covered by one of the documented
        /// exception types.
        /// </summary>
        /// <param name="documentedExceptions">The documented exception types.</param>
        /// <param name="thrownType">The thrown exception type.</param>
        /// <returns>
        /// <see langword="true"/> if the thrown type is identical to or derives from one of
        /// the documented exception types; otherwise <see langword="false"/>.
        /// </returns>
        private static bool IsCoveredByDocumentedException(
            HashSet<INamedTypeSymbol> documentedExceptions,
            INamedTypeSymbol thrownType)
        {
            foreach (INamedTypeSymbol documentedType in documentedExceptions)
            {
                if (thrownType.InheritsFromOrEquals(documentedType))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
