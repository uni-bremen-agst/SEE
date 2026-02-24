using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using XMLDocNormalizer.Checks.Infrastructure;
using XMLDocNormalizer.Models;
using XMLDocNormalizer.Utils;

namespace XMLDocNormalizer.Checks
{
    /// <summary>
    /// Detects type parameter documentation smells (DOC410/DOC420/DOC430/DOC450).
    /// </summary>
    internal static class XmlDocTypeParamDetector
    {
        private static readonly NamedTagSmellSet Smells = new(
            XmlDocSmells.MissingTypeParamTag,
            XmlDocSmells.EmptyTypeParamDescription,
            XmlDocSmells.UnknownTypeParamTag,
            XmlDocSmells.DuplicateTypeParamTag);

        /// <summary>
        /// Scans the syntax tree and returns findings for DOC410/DOC420/DOC430/DOC450.
        /// </summary>
        /// <param name="tree">The syntax tree to analyze.</param>
        /// <param name="filePath">The file path used for reporting.</param>
        /// <returns>A list of findings.</returns>
        public static List<Finding> FindTypeParamSmells(SyntaxTree tree, string filePath)
        {
            List<Finding> findings = new();

            CompilationUnitSyntax root = tree.GetCompilationUnitRoot();

            IEnumerable<SyntaxNode> declarations =
                root.DescendantNodes()
                    .Where(n =>
                        n is MethodDeclarationSyntax ||
                        n is DelegateDeclarationSyntax ||
                        n is TypeDeclarationSyntax);

            foreach (SyntaxNode declaration in declarations)
            {
                TypeParameterListSyntax? typeParams = TryGetTypeParameterList(declaration);
                if (typeParams == null || typeParams.Parameters.Count == 0)
                {
                    continue;
                }

                DocumentationCommentTriviaSyntax? doc = XmlDocUtils.TryGetDocComment(declaration);
                if (doc == null)
                {
                    continue;
                }

                Dictionary<string, int> anchorByName =
                    AnchorMapBuilder.BuildAnchors(
                        typeParams.Parameters,
                        tp => tp.Identifier);

                HashSet<string> declaredNames = new(anchorByName.Keys, StringComparer.Ordinal);

                List<NamedDocTag> docTags = XmlDocTagExtraction.GetNamedTags(doc, "typeparam");

                NamedTagAnalyzer.Analyze(
                    findings,
                    tree,
                    filePath,
                    xmlTagName: "typeparam",
                    declaredNames,
                    docTags,
                    Smells,
                    missingAnchorProvider: name => anchorByName[name],
                    hasMeaningfulContent: XmlDocUtils.HasMeaningfulContent,
                    snippetProvider: SyntaxUtils.GetSnippet);
            }

            return findings;
        }

        /// <summary>
        /// Tries to retrieve the <see cref="TypeParameterListSyntax"/> for a supported declaration node.
        /// </summary>
        /// <param name="declaration">The declaration node to inspect.</param>
        /// <returns>The type parameter list if present; otherwise <see langword="null"/>.</returns>
        private static TypeParameterListSyntax? TryGetTypeParameterList(SyntaxNode declaration)
        {
            if (declaration is MethodDeclarationSyntax methodDecl)
            {
                return methodDecl.TypeParameterList;
            }

            if (declaration is DelegateDeclarationSyntax delegateDecl)
            {
                return delegateDecl.TypeParameterList;
            }

            if (declaration is TypeDeclarationSyntax typeDecl)
            {
                return typeDecl.TypeParameterList;
            }

            return null;
        }
    }
}
