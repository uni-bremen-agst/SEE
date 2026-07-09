using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using XMLDocNormalizer.Checks.Infrastructure;
using XMLDocNormalizer.Checks.Infrastructure.Tags;
using XMLDocNormalizer.Models;
using XMLDocNormalizer.Utils;

namespace XMLDocNormalizer.Checks
{
    /// <summary>
    /// Detects XML documentation smells related to param and paramref tags for methods, constructors, delegates, indexers, and operators.
    /// </summary>
    /// <remarks>
    /// This detector reports missing param tags, empty param descriptions, unknown param tags, duplicate param tags,
    /// invalid parameter references inside paramref tags, and param documentation order mismatches.
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

                Dictionary<string, int> anchorByName = new Dictionary<string, int>(StringComparer.Ordinal);
                HashSet<string> declaredNames = new HashSet<string>(StringComparer.Ordinal);
                List<string> declaredOrder = new List<string>();

                if (parameters.Count > 0)
                {
                    anchorByName =
                        AnchorMapBuilder.BuildAnchors(
                            parameters,
                            parameter => parameter.Identifier);

                    declaredNames = new HashSet<string>(anchorByName.Keys, StringComparer.Ordinal);
                    declaredOrder = parameters.Select(parameter => parameter.Identifier.ValueText).ToList();

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

                    AddParamOrderMismatchFinding(
                        findings,
                        tree,
                        filePath,
                        declaration,
                        declaredNames,
                        declaredOrder,
                        tags);
                }

                ReferenceTagAnalyzer.Analyze(
                    findings,
                    tree,
                    filePath,
                    doc,
                    declaration,
                    xmlTagName: "paramref",
                    declaredNames,
                    missingNameSmell: XmlDocSmells.ParamRefMissingName,
                    unknownReferenceSmell: XmlDocSmells.UnknownParamRef,
                    invalidAttributeSmell: XmlDocSmells.InvalidParamRefAttribute,
                    subjectKind: "ParamRefTag");
            }

            return findings;
        }

        /// <summary>
        /// Adds a single order-mismatch finding if param documentation tags do not follow the declaration parameter order.
        /// </summary>
        /// <param name="findings">The collection to which findings will be added.</param>
        /// <param name="tree">The syntax tree containing the declaration.</param>
        /// <param name="filePath">The file path used for reporting.</param>
        /// <param name="declaration">The declaration that owns the documentation comment.</param>
        /// <param name="declaredNames">The set of declared parameter names.</param>
        /// <param name="declaredOrder">The declared parameter order.</param>
        /// <param name="tags">The extracted param documentation tags.</param>
        private static void AddParamOrderMismatchFinding(
            List<Finding> findings,
            SyntaxTree tree,
            string filePath,
            SyntaxNode declaration,
            IReadOnlySet<string> declaredNames,
            IReadOnlyList<string> declaredOrder,
            IReadOnlyList<ExtractedXmlDocTag> tags)
        {
            if (declaredOrder.Count < 2)
            {
                return;
            }

            if (tags.Count < 2)
            {
                return;
            }

            List<string> documentedOrder = new List<string>();
            HashSet<string> seenDocumentedNames = new HashSet<string>(StringComparer.Ordinal);

            foreach (ExtractedXmlDocTag tag in tags)
            {
                string? documentedName = tag.RawAttributeValue;

                if (string.IsNullOrWhiteSpace(documentedName))
                {
                    return;
                }

                if (!declaredNames.Contains(documentedName))
                {
                    return;
                }

                if (!seenDocumentedNames.Add(documentedName))
                {
                    return;
                }

                documentedOrder.Add(documentedName);
            }

            if (documentedOrder.Count != declaredOrder.Count)
            {
                return;
            }

            if (documentedOrder.SequenceEqual(declaredOrder, StringComparer.Ordinal))
            {
                return;
            }

            ExtractedXmlDocTag firstDocumentedTag = tags[0];

            findings.Add(FindingFactory.AtPosition(
                tree,
                filePath,
                tagName: "param",
                XmlDocSmells.ParamOrderMismatch,
                firstDocumentedTag.Element.SpanStart,
                FindingContextBuilder.ForDeclaration(
                    declaration,
                    "ParameterTag",
                    targetName: null,
                    filePath: filePath),
                snippet: SyntaxUtils.GetSnippet(firstDocumentedTag.Element)));
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
