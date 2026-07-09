using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using XMLDocNormalizer.Checks.Infrastructure;
using XMLDocNormalizer.Models;
using XMLDocNormalizer.Utils;

namespace XMLDocNormalizer.Checks
{
    /// <summary>
    /// Detects XML documentation smells related to param and paramref tags for methods, constructors, delegates, indexers, and operators.
    /// </summary>
    /// <remarks>
    /// This detector reports missing param tags, empty param descriptions, unknown param tags, duplicate param tags,
    /// and invalid parameter references inside paramref tags.
    /// The analysis is syntax-based and does not require semantic model access.
    /// </remarks>
    internal static class XmlDocParamDetector
    {
        /// <summary>
        /// Defines the set of named-tag smells handled by this detector.
        /// </summary>
        /// <remarks>
        /// The smell set contains all rule definitions required for analyzing param documentation tags.
        /// </remarks>
        private static readonly NamedTagSmellSet Smells = new(
            XmlDocSmells.MissingParamTag,
            XmlDocSmells.EmptyParamDescription,
            XmlDocSmells.UnknownParamTag,
            XmlDocSmells.DuplicateParamTag);

        /// <summary>
        /// Scans the syntax tree and returns findings for param documentation smells.
        /// </summary>
        /// <param name="tree">The syntax tree to analyze.</param>
        /// <param name="filePath">The file path used for reporting.</param>
        /// <returns>
        /// A list of findings produced by the parameter documentation detector.
        /// </returns>
        public static List<Finding> FindParamSmells(SyntaxTree tree, string filePath)
        {
            List<Finding> findings = new List<Finding>();

            CompilationUnitSyntax root = tree.GetCompilationUnitRoot();

            IEnumerable<SyntaxNode> declarations =
                root.DescendantNodes()
                    .Where(node =>
                        node is MethodDeclarationSyntax ||
                        node is ConstructorDeclarationSyntax ||
                        node is DelegateDeclarationSyntax ||
                        node is IndexerDeclarationSyntax ||
                        node is OperatorDeclarationSyntax ||
                        node is ConversionOperatorDeclarationSyntax);

            foreach (SyntaxNode declaration in declarations)
            {
                if (!TryGetParameters(declaration, out SeparatedSyntaxList<ParameterSyntax> parameters))
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
                        parameters,
                        parameter => parameter.Identifier);

                HashSet<string> declaredNames = new HashSet<string>(anchorByName.Keys, StringComparer.Ordinal);

                List<ExtractedXmlDocTag> tags =
                    XmlDocTagExtraction.ExtractTags(doc, "param", NamedTagAnalyzer.ExtractReferencedName);

                NamedTagAnalyzer.Analyze(
                    findings,
                    tree,
                    filePath,
                    xmlTagName: "param",
                    declaredNames,
                    tags,
                    Smells,
                    missingAnchorProvider: name => anchorByName[name],
                    hasMeaningfulContent: XmlDocUtils.HasMeaningfulContent,
                    snippetProvider: SyntaxUtils.GetSnippet,
                    contextProvider: name => FindingContextBuilder.ForDeclaration(
                        declaration,
                        "Parameter",
                        targetName: name,
                        filePath: filePath));

                AddParamRefFindings(
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
        /// Adds findings for invalid paramref tags in a documentation comment.
        /// </summary>
        /// <param name="findings">The collection to which findings will be added.</param>
        /// <param name="tree">The syntax tree containing the documentation comment.</param>
        /// <param name="filePath">The file path used for reporting.</param>
        /// <param name="declaration">The declaration that owns the documentation comment.</param>
        /// <param name="declaredNames">The set of declared parameter names for the declaration.</param>
        /// <param name="doc">The documentation comment to inspect.</param>
        private static void AddParamRefFindings(
            List<Finding> findings,
            SyntaxTree tree,
            string filePath,
            SyntaxNode declaration,
            IReadOnlySet<string> declaredNames,
            DocumentationCommentTriviaSyntax doc)
        {
            foreach (XmlEmptyElementSyntax element in doc.DescendantNodes().OfType<XmlEmptyElementSyntax>())
            {
                if (!string.Equals(SyntaxUtils.GetLocalName(element), "paramref", StringComparison.Ordinal))
                {
                    continue;
                }

                AddParamRefFindingsForEmptyElement(
                    findings,
                    tree,
                    filePath,
                    declaration,
                    declaredNames,
                    element);
            }

            foreach (XmlElementSyntax element in XmlDocElementQuery.ElementsByName(doc, "paramref"))
            {
                AddParamRefFindingsForElement(
                    findings,
                    tree,
                    filePath,
                    declaration,
                    declaredNames,
                    element);
            }
        }

        /// <summary>
        /// Adds findings for an invalid paramref XML element.
        /// </summary>
        /// <param name="findings">The collection to which findings will be added.</param>
        /// <param name="tree">The syntax tree containing the documentation comment.</param>
        /// <param name="filePath">The file path used for reporting.</param>
        /// <param name="declaration">The declaration that owns the documentation comment.</param>
        /// <param name="declaredNames">The set of declared parameter names for the declaration.</param>
        /// <param name="element">The paramref XML element to inspect.</param>
        private static void AddParamRefFindingsForElement(
            List<Finding> findings,
            SyntaxTree tree,
            string filePath,
            SyntaxNode declaration,
            IReadOnlySet<string> declaredNames,
            XmlElementSyntax element)
        {
            string? targetName = XmlDocTagExtraction.TryGetNameAttributeValue(element);

            AddInvalidParamRefAttributeFindings(
                findings,
                tree,
                filePath,
                declaration,
                SyntaxUtils.GetAttributes(element),
                element);

            AddMissingOrUnknownParamRefFinding(
                findings,
                tree,
                filePath,
                declaration,
                declaredNames,
                element,
                targetName);
        }

        /// <summary>
        /// Adds findings for an invalid empty paramref XML element.
        /// </summary>
        /// <param name="findings">The collection to which findings will be added.</param>
        /// <param name="tree">The syntax tree containing the documentation comment.</param>
        /// <param name="filePath">The file path used for reporting.</param>
        /// <param name="declaration">The declaration that owns the documentation comment.</param>
        /// <param name="declaredNames">The set of declared parameter names for the declaration.</param>
        /// <param name="element">The empty paramref XML element to inspect.</param>
        private static void AddParamRefFindingsForEmptyElement(
            List<Finding> findings,
            SyntaxTree tree,
            string filePath,
            SyntaxNode declaration,
            IReadOnlySet<string> declaredNames,
            XmlEmptyElementSyntax element)
        {
            string? targetName = TryGetNameAttributeValue(element);

            AddInvalidParamRefAttributeFindings(
                findings,
                tree,
                filePath,
                declaration,
                SyntaxUtils.GetAttributes(element),
                element);

            AddMissingOrUnknownParamRefFinding(
                findings,
                tree,
                filePath,
                declaration,
                declaredNames,
                element,
                targetName);
        }

        /// <summary>
        /// Adds findings for invalid attributes on a paramref tag.
        /// </summary>
        /// <param name="findings">The collection to which findings will be added.</param>
        /// <param name="tree">The syntax tree containing the documentation comment.</param>
        /// <param name="filePath">The file path used for reporting.</param>
        /// <param name="declaration">The declaration that owns the documentation comment.</param>
        /// <param name="attributes">The attributes to inspect.</param>
        /// <param name="snippetNode">The syntax node used to create the finding snippet.</param>
        private static void AddInvalidParamRefAttributeFindings(
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
                    tagName: "paramref",
                    XmlDocSmells.InvalidParamRefAttribute,
                    attribute.SpanStart,
                    FindingContextBuilder.ForDeclaration(
                        declaration,
                        "ParamRefTag",
                        targetName: attributeName,
                        filePath: filePath),
                    snippet: SyntaxUtils.GetSnippet(snippetNode),
                    attributeName));
            }
        }

        /// <summary>
        /// Adds a missing-name or unknown-reference finding for a paramref tag.
        /// </summary>
        /// <param name="findings">The collection to which findings will be added.</param>
        /// <param name="tree">The syntax tree containing the documentation comment.</param>
        /// <param name="filePath">The file path used for reporting.</param>
        /// <param name="declaration">The declaration that owns the documentation comment.</param>
        /// <param name="declaredNames">The set of declared parameter names for the declaration.</param>
        /// <param name="element">The paramref syntax node to inspect.</param>
        /// <param name="targetName">The referenced parameter name, if present.</param>
        private static void AddMissingOrUnknownParamRefFinding(
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
                    tagName: "paramref",
                    XmlDocSmells.ParamRefMissingName,
                    element.Span,
                    FindingContextBuilder.ForDeclaration(
                        declaration,
                        "ParamRefTag",
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
                tagName: "paramref",
                XmlDocSmells.UnknownParamRef,
                element.Span,
                FindingContextBuilder.ForDeclaration(
                    declaration,
                    "ParamRefTag",
                    targetName: targetName,
                    filePath: filePath),
                snippet: SyntaxUtils.GetSnippet(element),
                targetName));
        }

        /// <summary>
        /// Tries to extract the name attribute value from an empty paramref element.
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
        /// Tries to get the parameters for a supported declaration node.
        /// </summary>
        /// <param name="declaration">The declaration node to inspect.</param>
        /// <param name="parameters">The extracted parameters if the declaration is supported.</param>
        /// <returns>
        /// True if parameters could be extracted; otherwise false.
        /// </returns>
        private static bool TryGetParameters(SyntaxNode declaration, out SeparatedSyntaxList<ParameterSyntax> parameters)
        {
            if (declaration is MethodDeclarationSyntax methodDeclaration)
            {
                parameters = methodDeclaration.ParameterList.Parameters;
                return true;
            }

            if (declaration is ConstructorDeclarationSyntax constructorDeclaration)
            {
                parameters = constructorDeclaration.ParameterList.Parameters;
                return true;
            }

            if (declaration is DelegateDeclarationSyntax delegateDeclaration)
            {
                parameters = delegateDeclaration.ParameterList.Parameters;
                return true;
            }

            if (declaration is IndexerDeclarationSyntax indexerDeclaration)
            {
                parameters = indexerDeclaration.ParameterList.Parameters;
                return true;
            }

            if (declaration is OperatorDeclarationSyntax operatorDeclaration)
            {
                parameters = operatorDeclaration.ParameterList.Parameters;
                return true;
            }

            if (declaration is ConversionOperatorDeclarationSyntax conversionOperatorDeclaration)
            {
                parameters = conversionOperatorDeclaration.ParameterList.Parameters;
                return true;
            }

            parameters = default;
            return false;
        }
    }
}
