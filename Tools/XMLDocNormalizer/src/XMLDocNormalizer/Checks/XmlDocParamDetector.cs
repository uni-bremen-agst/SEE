using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using XMLDocNormalizer.Checks.Infrastructure;
using XMLDocNormalizer.Models;
using XMLDocNormalizer.Utils;

namespace XMLDocNormalizer.Checks
{
    /// <summary>
    /// Detects XML documentation smells related to param tags for methods, constructors, delegates, indexers, and operators.
    /// </summary>
    /// <remarks>
    /// This detector reports missing param tags, empty param descriptions, unknown param tags, and duplicate param tags.
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

                if (parameters.Count == 0)
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
            }

            return findings;
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
