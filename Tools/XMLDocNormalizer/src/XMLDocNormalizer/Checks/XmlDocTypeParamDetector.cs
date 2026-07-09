using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using XMLDocNormalizer.Checks.Infrastructure;
using XMLDocNormalizer.Models;
using XMLDocNormalizer.Utils;

namespace XMLDocNormalizer.Checks
{
    /// <summary>
    /// Detects XML documentation smells related to typeparam and typeparamref tags for types, methods, and delegates.
    /// </summary>
    /// <remarks>
    /// This detector reports missing typeparam tags, empty typeparam descriptions, unknown typeparam tags,
    /// duplicate typeparam tags, and invalid type parameter references inside typeparamref tags.
    /// The analysis is syntax-based and does not require semantic model access.
    /// </remarks>
    internal static class XmlDocTypeParamDetector
    {
        /// <summary>
        /// Defines the set of named-tag smells handled by this detector.
        /// </summary>
        /// <remarks>
        /// The smell set contains all rule definitions required for analyzing type parameter documentation tags.
        /// </remarks>
        private static readonly NamedTagSmellSet Smells = new(
            XmlDocSmells.MissingTypeParamTag,
            XmlDocSmells.EmptyTypeParamDescription,
            XmlDocSmells.UnknownTypeParamTag,
            XmlDocSmells.DuplicateTypeParamTag);

        /// <summary>
        /// Scans the syntax tree and returns findings for type parameter documentation smells.
        /// </summary>
        /// <param name="tree">The syntax tree to analyze.</param>
        /// <param name="filePath">The file path used for reporting.</param>
        /// <returns>
        /// A list of findings produced by the type parameter documentation detector.
        /// </returns>
        public static List<Finding> FindTypeParamSmells(SyntaxTree tree, string filePath)
        {
            List<Finding> findings = new List<Finding>();

            CompilationUnitSyntax root = tree.GetCompilationUnitRoot();

            IEnumerable<SyntaxNode> declarations =
                root.DescendantNodes()
                    .Where(node =>
                        node is MethodDeclarationSyntax ||
                        node is DelegateDeclarationSyntax ||
                        node is TypeDeclarationSyntax);

            foreach (SyntaxNode declaration in declarations)
            {
                DocumentationCommentTriviaSyntax? doc = XmlDocUtils.TryGetDocComment(declaration);

                if (doc == null)
                {
                    continue;
                }

                TypeParameterListSyntax? typeParameters = TryGetTypeParameterList(declaration);

                Dictionary<string, int> anchorByName = new Dictionary<string, int>(StringComparer.Ordinal);
                HashSet<string> declaredNames = new HashSet<string>(StringComparer.Ordinal);

                if (typeParameters != null && typeParameters.Parameters.Count > 0)
                {
                    anchorByName =
                        AnchorMapBuilder.BuildAnchors(
                            typeParameters.Parameters,
                            typeParameter => typeParameter.Identifier);

                    declaredNames = new HashSet<string>(anchorByName.Keys, StringComparer.Ordinal);

                    List<ExtractedXmlDocTag> docTags =
                        XmlDocTagExtraction.ExtractTags(doc, "typeparam", NamedTagAnalyzer.ExtractReferencedName);

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
                        snippetProvider: SyntaxUtils.GetSnippet,
                        contextProvider: name => FindingContextBuilder.ForDeclaration(
                            declaration,
                            "TypeParameter",
                            targetName: name,
                            filePath: filePath));
                }

                ReferenceTagAnalyzer.Analyze(
                    findings,
                    tree,
                    filePath,
                    doc,
                    declaration,
                    xmlTagName: "typeparamref",
                    declaredNames,
                    missingNameSmell: XmlDocSmells.TypeParamRefMissingName,
                    unknownReferenceSmell: XmlDocSmells.UnknownTypeParamRef,
                    invalidAttributeSmell: XmlDocSmells.InvalidTypeParamRefAttribute,
                    subjectKind: "TypeParamRefTag");
            }

            return findings;
        }

        /// <summary>
        /// Tries to retrieve the type parameter list for a supported declaration node.
        /// </summary>
        /// <param name="declaration">The declaration node to inspect.</param>
        /// <returns>
        /// The type parameter list if present; otherwise null.
        /// </returns>
        private static TypeParameterListSyntax? TryGetTypeParameterList(SyntaxNode declaration)
        {
            if (declaration is MethodDeclarationSyntax methodDeclaration)
            {
                return methodDeclaration.TypeParameterList;
            }

            if (declaration is DelegateDeclarationSyntax delegateDeclaration)
            {
                return delegateDeclaration.TypeParameterList;
            }

            if (declaration is TypeDeclarationSyntax typeDeclaration)
            {
                return typeDeclaration.TypeParameterList;
            }

            return null;
        }
    }
}
