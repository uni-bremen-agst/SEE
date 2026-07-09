using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using XMLDocNormalizer.Checks.Infrastructure;
using XMLDocNormalizer.Checks.Infrastructure.Inheritdoc;
using XMLDocNormalizer.Checks.Infrastructure.Tags;
using XMLDocNormalizer.Models;
using XMLDocNormalizer.Utils;

namespace XMLDocNormalizer.Checks
{
    /// <summary>
    /// Detects inheritdoc-related smells that require semantic analysis.
    /// </summary>
    internal static class XmlDocInheritdocSemanticDetector
    {
        /// <summary>
        /// Scans the syntax tree and returns inheritdoc-related findings that require semantic analysis.
        /// </summary>
        /// <param name="tree">The syntax tree to analyze.</param>
        /// <param name="filePath">The file path used for reporting.</param>
        /// <param name="semanticModel">The semantic model for the syntax tree.</param>
        /// <returns>A list of findings.</returns>
        public static List<Finding> FindInheritdocSmells(
            SyntaxTree tree,
            string filePath,
            SemanticModel semanticModel)
        {
            List<Finding> findings = new();

            CompilationUnitSyntax root = tree.GetCompilationUnitRoot();

            IEnumerable<SyntaxNode> nodes =
                root.DescendantNodes()
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

                foreach (XmlEmptyElementSyntax inheritdocElement in XmlDocTagExtraction.EmptyElementsByName(doc, "inheritdoc"))
                {
                    AnalyzeInheritdocElement(
                        findings,
                        tree,
                        filePath,
                        semanticModel,
                        node,
                        inheritdocElement);
                }
            }

            return findings;
        }

        /// <summary>
        /// Analyzes a single <c>inheritdoc</c> element and adds all applicable findings.
        /// </summary>
        /// <param name="findings">The findings collection to append to.</param>
        /// <param name="tree">The syntax tree being analyzed.</param>
        /// <param name="filePath">The file path used for reporting.</param>
        /// <param name="semanticModel">The semantic model for the syntax tree.</param>
        /// <param name="node">The documented declaration node.</param>
        /// <param name="inheritdocElement">The inheritdoc element to analyze.</param>
        private static void AnalyzeInheritdocElement(
            List<Finding> findings,
            SyntaxTree tree,
            string filePath,
            SemanticModel semanticModel,
            SyntaxNode node,
            XmlEmptyElementSyntax inheritdocElement)
        {
            XmlCrefAttributeSyntax? crefAttribute =
                SyntaxUtils.GetAttribute<XmlCrefAttributeSyntax>(inheritdocElement, "cref");

            if (crefAttribute != null)
            {
                AnalyzeInheritdocCref(
                    findings,
                    tree,
                    filePath,
                    semanticModel,
                    node,
                    inheritdocElement,
                    crefAttribute);

                return;
            }

            List<ISymbol> sources =
                ImplicitInheritdocSourceResolver.GetImplicitInheritdocSources(node, semanticModel);

            if (sources.Count > 1)
            {
                findings.Add(FindingFactory.AtPosition(
                    tree,
                    filePath,
                    tagName: "inheritdoc",
                    XmlDocSmells.AmbiguousInheritdocSource,
                    inheritdocElement.SpanStart,
                    CreateInheritdocContext(
                        node,
                        targetName: null,
                        filePath: filePath),
                    snippet: SyntaxUtils.GetSnippet(inheritdocElement)));

                return;
            }

            if (sources.Count == 0)
            {
                findings.Add(FindingFactory.AtPosition(
                    tree,
                    filePath,
                    tagName: "inheritdoc",
                    XmlDocSmells.InheritdocNoSource,
                    inheritdocElement.SpanStart,
                    CreateInheritdocContext(
                        node,
                        targetName: null,
                        filePath: filePath),
                    snippet: SyntaxUtils.GetSnippet(inheritdocElement)));
                return;
            }

            ISymbol sourceSymbol = sources[0];

            if (!InheritdocDocumentationInspector.HasUsefulDocumentation(sourceSymbol))
            {
                findings.Add(FindingFactory.AtPosition(
                    tree,
                    filePath,
                    tagName: "inheritdoc",
                    XmlDocSmells.RedundantInheritdoc,
                    inheritdocElement.SpanStart,
                    CreateInheritdocContext(
                        node,
                        targetName: sourceSymbol.ToDisplayString(),
                        filePath: filePath),
                    snippet: SyntaxUtils.GetSnippet(inheritdocElement)));
            }
        }

        /// <summary>
        /// Analyzes an <c>inheritdoc</c> element with a <c>cref</c> attribute.
        /// </summary>
        /// <param name="findings">The findings collection to append to.</param>
        /// <param name="tree">The syntax tree being analyzed.</param>
        /// <param name="filePath">The file path used for reporting.</param>
        /// <param name="semanticModel">The semantic model for the syntax tree.</param>
        /// <param name="node">The documented declaration node.</param>
        /// <param name="inheritdocElement">The inheritdoc element to analyze.</param>
        /// <param name="crefAttribute">The cref attribute of the inheritdoc element.</param>
        private static void AnalyzeInheritdocCref(
            List<Finding> findings,
            SyntaxTree tree,
            string filePath,
            SemanticModel semanticModel,
            SyntaxNode node,
            XmlEmptyElementSyntax inheritdocElement,
            XmlCrefAttributeSyntax crefAttribute)
        {
            SymbolInfo symbolInfo = semanticModel.GetSymbolInfo(crefAttribute.Cref);

            if (symbolInfo.Symbol == null)
            {
                findings.Add(FindingFactory.AtPosition(
                    tree,
                    filePath,
                    tagName: "inheritdoc",
                    XmlDocSmells.InvalidInheritdocCref,
                    crefAttribute.SpanStart,
                    CreateInheritdocContext(
                        node,
                        targetName: CreateCrefTargetName(crefAttribute),
                        filePath: filePath),
                    snippet: SyntaxUtils.GetSnippet(inheritdocElement)));
                return;
            }

            ISymbol? sourceSymbol =
                            ExplicitInheritdocSourceResolver.GetValidExplicitInheritdocSource(
                                node,
                                symbolInfo.Symbol,
                                semanticModel);

            if (sourceSymbol == null)
            {
                findings.Add(FindingFactory.AtPosition(
                    tree,
                    filePath,
                    tagName: "inheritdoc",
                    XmlDocSmells.InheritdocIncompatibleCref,
                    crefAttribute.SpanStart,
                    CreateInheritdocContext(
                        node,
                        targetName: CreateCrefTargetName(crefAttribute),
                        filePath: filePath),
                    snippet: SyntaxUtils.GetSnippet(inheritdocElement)));
                return;
            }

            if (!InheritdocDocumentationInspector.HasUsefulDocumentation(sourceSymbol))
            {
                findings.Add(FindingFactory.AtPosition(
                    tree,
                    filePath,
                    tagName: "inheritdoc",
                    XmlDocSmells.RedundantInheritdoc,
                    crefAttribute.SpanStart,
                    CreateInheritdocContext(
                        node,
                        targetName: CreateCrefTargetName(crefAttribute),
                        filePath: filePath),
                    snippet: SyntaxUtils.GetSnippet(inheritdocElement)));
            }
        }

        /// <summary>
        /// Creates finding context metadata for an inheritdoc-related finding.
        /// </summary>
        /// <param name="node">The declaration node that owns the inheritdoc documentation.</param>
        /// <param name="targetName">The affected inheritdoc source or cref target.</param>
        /// <param name="filePath">The file path used for reporting.</param>
        /// <returns>
        /// A populated finding context for an inheritdoc finding.
        /// </returns>
        private static FindingContext CreateInheritdocContext(
            SyntaxNode node,
            string? targetName,
            string filePath)
        {
            return FindingContextBuilder.ForDeclaration(
                node,
                "InheritdocTag",
                targetName: targetName,
                filePath: filePath);
        }

        /// <summary>
        /// Creates a stable target name for an inheritdoc cref attribute.
        /// </summary>
        /// <param name="crefAttribute">The cref attribute to inspect.</param>
        /// <returns>
        /// A stable cref target name if a cref value exists; otherwise null.
        /// </returns>
        private static string? CreateCrefTargetName(XmlCrefAttributeSyntax crefAttribute)
        {
            if (crefAttribute.Cref == null)
            {
                return null;
            }

            string rawCref = crefAttribute.Cref.ToString();

            if (string.IsNullOrWhiteSpace(rawCref))
            {
                return null;
            }

            return "cref:" + rawCref;
        }
    }
}
