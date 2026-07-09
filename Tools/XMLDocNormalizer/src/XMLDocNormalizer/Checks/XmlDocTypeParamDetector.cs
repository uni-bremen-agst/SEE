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

                AddTypeParamRefFindings(
                    findings,
                    tree,
                    filePath,
                    declaration,
                    declaredNames,
                    doc);
            }

            return findings;
        }

        /// <summary>
        /// Adds findings for invalid typeparamref tags in a documentation comment.
        /// </summary>
        /// <param name="findings">The collection to which findings will be added.</param>
        /// <param name="tree">The syntax tree containing the documentation comment.</param>
        /// <param name="filePath">The file path used for reporting.</param>
        /// <param name="declaration">The declaration that owns the documentation comment.</param>
        /// <param name="declaredNames">The set of declared type parameter names for the declaration.</param>
        /// <param name="doc">The documentation comment to inspect.</param>
        private static void AddTypeParamRefFindings(
            List<Finding> findings,
            SyntaxTree tree,
            string filePath,
            SyntaxNode declaration,
            IReadOnlySet<string> declaredNames,
            DocumentationCommentTriviaSyntax doc)
        {
            foreach (XmlEmptyElementSyntax element in doc.DescendantNodes().OfType<XmlEmptyElementSyntax>())
            {
                if (!string.Equals(SyntaxUtils.GetLocalName(element), "typeparamref", StringComparison.Ordinal))
                {
                    continue;
                }

                AddTypeParamRefFindingsForEmptyElement(
                    findings,
                    tree,
                    filePath,
                    declaration,
                    declaredNames,
                    element);
            }

            foreach (XmlElementSyntax element in XmlDocElementQuery.ElementsByName(doc, "typeparamref"))
            {
                AddTypeParamRefFindingsForElement(
                    findings,
                    tree,
                    filePath,
                    declaration,
                    declaredNames,
                    element);
            }
        }

        /// <summary>
        /// Adds findings for an invalid typeparamref XML element.
        /// </summary>
        /// <param name="findings">The collection to which findings will be added.</param>
        /// <param name="tree">The syntax tree containing the documentation comment.</param>
        /// <param name="filePath">The file path used for reporting.</param>
        /// <param name="declaration">The declaration that owns the documentation comment.</param>
        /// <param name="declaredNames">The set of declared type parameter names for the declaration.</param>
        /// <param name="element">The typeparamref XML element to inspect.</param>
        private static void AddTypeParamRefFindingsForElement(
            List<Finding> findings,
            SyntaxTree tree,
            string filePath,
            SyntaxNode declaration,
            IReadOnlySet<string> declaredNames,
            XmlElementSyntax element)
        {
            string? targetName = XmlDocTagExtraction.TryGetNameAttributeValue(element);

            AddInvalidTypeParamRefAttributeFindings(
                findings,
                tree,
                filePath,
                declaration,
                SyntaxUtils.GetAttributes(element),
                element);

            AddMissingOrUnknownTypeParamRefFinding(
                findings,
                tree,
                filePath,
                declaration,
                declaredNames,
                element,
                targetName);
        }

        /// <summary>
        /// Adds findings for an invalid empty typeparamref XML element.
        /// </summary>
        /// <param name="findings">The collection to which findings will be added.</param>
        /// <param name="tree">The syntax tree containing the documentation comment.</param>
        /// <param name="filePath">The file path used for reporting.</param>
        /// <param name="declaration">The declaration that owns the documentation comment.</param>
        /// <param name="declaredNames">The set of declared type parameter names for the declaration.</param>
        /// <param name="element">The empty typeparamref XML element to inspect.</param>
        private static void AddTypeParamRefFindingsForEmptyElement(
            List<Finding> findings,
            SyntaxTree tree,
            string filePath,
            SyntaxNode declaration,
            IReadOnlySet<string> declaredNames,
            XmlEmptyElementSyntax element)
        {
            string? targetName = TryGetNameAttributeValue(element);

            AddInvalidTypeParamRefAttributeFindings(
                findings,
                tree,
                filePath,
                declaration,
                SyntaxUtils.GetAttributes(element),
                element);

            AddMissingOrUnknownTypeParamRefFinding(
                findings,
                tree,
                filePath,
                declaration,
                declaredNames,
                element,
                targetName);
        }

        /// <summary>
        /// Adds findings for invalid attributes on a typeparamref tag.
        /// </summary>
        /// <param name="findings">The collection to which findings will be added.</param>
        /// <param name="tree">The syntax tree containing the documentation comment.</param>
        /// <param name="filePath">The file path used for reporting.</param>
        /// <param name="declaration">The declaration that owns the documentation comment.</param>
        /// <param name="attributes">The attributes to inspect.</param>
        /// <param name="snippetNode">The syntax node used to create the finding snippet.</param>
        private static void AddInvalidTypeParamRefAttributeFindings(
            List<Finding> findings,
            SyntaxTree tree,
            string filePath,
            SyntaxNode declaration,
            IEnumerable<XmlAttributeSyntax> attributes,
            SyntaxNode snippetNode)
        {
            foreach (XmlAttributeSyntax attribute in attributes)
            {
                string attributeName = attribute.Name.LocalName.Text;

                if (string.Equals(attributeName, "name", StringComparison.Ordinal))
                {
                    continue;
                }

                findings.Add(FindingFactory.AtPosition(
                    tree,
                    filePath,
                    tagName: "typeparamref",
                    XmlDocSmells.InvalidTypeParamRefAttribute,
                    attribute.SpanStart,
                    FindingContextBuilder.ForDeclaration(
                        declaration,
                        "TypeParamRefTag",
                        targetName: attributeName,
                        filePath: filePath),
                    snippet: SyntaxUtils.GetSnippet(snippetNode),
                    attributeName));
            }
        }

        /// <summary>
        /// Adds a missing-name or unknown-reference finding for a typeparamref tag.
        /// </summary>
        /// <param name="findings">The collection to which findings will be added.</param>
        /// <param name="tree">The syntax tree containing the documentation comment.</param>
        /// <param name="filePath">The file path used for reporting.</param>
        /// <param name="declaration">The declaration that owns the documentation comment.</param>
        /// <param name="declaredNames">The set of declared type parameter names for the declaration.</param>
        /// <param name="element">The typeparamref syntax node to inspect.</param>
        /// <param name="targetName">The referenced type parameter name, if present.</param>
        private static void AddMissingOrUnknownTypeParamRefFinding(
            List<Finding> findings,
            SyntaxTree tree,
            string filePath,
            SyntaxNode declaration,
            IReadOnlySet<string> declaredNames,
            SyntaxNode element,
            string? targetName)
        {
            if (string.IsNullOrWhiteSpace(targetName))
            {
                findings.Add(FindingFactory.AtSpanStart(
                    tree,
                    filePath,
                    tagName: "typeparamref",
                    XmlDocSmells.TypeParamRefMissingName,
                    element.Span,
                    FindingContextBuilder.ForDeclaration(
                        declaration,
                        "TypeParamRefTag",
                        targetName: null,
                        filePath: filePath),
                    snippet: SyntaxUtils.GetSnippet(element)));

                return;
            }

            if (declaredNames.Contains(targetName))
            {
                return;
            }

            findings.Add(FindingFactory.AtSpanStart(
                tree,
                filePath,
                tagName: "typeparamref",
                XmlDocSmells.UnknownTypeParamRef,
                element.Span,
                FindingContextBuilder.ForDeclaration(
                    declaration,
                    "TypeParamRefTag",
                    targetName: targetName,
                    filePath: filePath),
                snippet: SyntaxUtils.GetSnippet(element),
                targetName));
        }

        /// <summary>
        /// Tries to extract the name attribute value from an empty typeparamref element.
        /// </summary>
        /// <param name="element">The empty XML element to inspect.</param>
        /// <returns>The name value if present; otherwise null.</returns>
        private static string? TryGetNameAttributeValue(XmlEmptyElementSyntax element)
        {
            XmlNameAttributeSyntax? nameAttribute =
                SyntaxUtils.GetAttribute<XmlNameAttributeSyntax>(element, "name");

            if (nameAttribute == null)
            {
                return null;
            }

            IdentifierNameSyntax? identifier = nameAttribute.Identifier;

            if (identifier == null)
            {
                return null;
            }

            return identifier.Identifier.ValueText;
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
