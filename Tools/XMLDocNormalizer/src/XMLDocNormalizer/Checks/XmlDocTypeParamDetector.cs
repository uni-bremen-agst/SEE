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

                Dictionary<string, int> anchorByName = BuildAnchorByName(typeParams);
                HashSet<string> declaredNames = new(anchorByName.Keys, StringComparer.Ordinal);

                List<NamedDocTag> docTags = GetNamedTags(doc, "typeparam");

                NamedTagAnalyzer.Analyze(
                    findings,
                    tree,
                    filePath,
                    xmlTagName: "typeparam",
                    declaredNames,
                    docTags,
                    Smells,
                    missingAnchorProvider: name => anchorByName[name],
                    hasMeaningfulContent: XmlDocTagExtraction.HasMeaningfulContent,
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

        /// <summary>
        /// Builds a lookup table mapping type parameter names to an absolute anchor position used for missing documentation findings.
        /// </summary>
        /// <param name="typeParams">The type parameter list.</param>
        /// <returns>A dictionary mapping type parameter name to identifier span start.</returns>
        private static Dictionary<string, int> BuildAnchorByName(TypeParameterListSyntax typeParams)
        {
            Dictionary<string, int> anchors = new(StringComparer.Ordinal);

            foreach (TypeParameterSyntax typeParam in typeParams.Parameters)
            {
                string name = typeParam.Identifier.ValueText;
                if (string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }

                if (!anchors.ContainsKey(name))
                {
                    anchors.Add(name, typeParam.Identifier.SpanStart);
                }
            }

            return anchors;
        }

        /// <summary>
        /// Extracts all named documentation tags with the given XML local name.
        /// Tags without a name attribute are ignored because well-formedness handles those cases.
        /// </summary>
        /// <param name="doc">The documentation comment.</param>
        /// <param name="xmlTagName">The XML tag name ("typeparam").</param>
        /// <returns>A list of named documentation tags.</returns>
        private static List<NamedDocTag> GetNamedTags(DocumentationCommentTriviaSyntax doc, string xmlTagName)
        {
            List<NamedDocTag> tags = new();

            IEnumerable<XmlElementSyntax> elements =
                doc.DescendantNodes()
                    .OfType<XmlElementSyntax>();

            foreach (XmlElementSyntax element in elements)
            {
                string localName = element.StartTag.Name.LocalName.Text;
                if (!string.Equals(localName, xmlTagName, StringComparison.Ordinal))
                {
                    continue;
                }

                string? name = XmlDocTagExtraction.TryGetNameAttributeValue(element);
                if (string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }

                tags.Add(new NamedDocTag(name, element));
            }

            return tags;
        }
    }
}
