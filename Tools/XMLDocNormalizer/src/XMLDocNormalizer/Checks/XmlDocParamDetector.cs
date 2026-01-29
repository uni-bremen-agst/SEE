using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using XMLDocNormalizer.Checks.Infrastructure;
using XMLDocNormalizer.Models;
using XMLDocNormalizer.Utils;

namespace XMLDocNormalizer.Checks
{
    /// <summary>
    /// Detects parameter documentation smells (DOC310/DOC320/DOC330/DOC350).
    /// </summary>
    internal static class XmlDocParamDetector
    {
        private static readonly NamedTagSmellSet Smells = new(
            XmlDocSmells.MissingParamTag,
            XmlDocSmells.EmptyParamDescription,
            XmlDocSmells.UnknownParamTag,
            XmlDocSmells.DuplicateParamTag);

        /// <summary>
        /// Scans the syntax tree and returns findings for DOC310/DOC320/DOC330/DOC350.
        /// </summary>
        /// <param name="tree">The syntax tree to analyze.</param>
        /// <param name="filePath">The file path used for reporting.</param>
        /// <returns>A list of findings.</returns>
        public static List<Finding> FindParamSmells(SyntaxTree tree, string filePath)
        {
            List<Finding> findings = new();

            CompilationUnitSyntax root = tree.GetCompilationUnitRoot();

            IEnumerable<SyntaxNode> declarations =
                root.DescendantNodes()
                    .Where(n =>
                        n is MethodDeclarationSyntax ||
                        n is ConstructorDeclarationSyntax ||
                        n is DelegateDeclarationSyntax ||
                        n is IndexerDeclarationSyntax ||
                        n is OperatorDeclarationSyntax ||
                        n is ConversionOperatorDeclarationSyntax);

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

                DocumentationCommentTriviaSyntax? doc = XmlDocTagExtraction.TryGetDocComment(declaration);
                if (doc == null)
                {
                    continue;
                }

                Dictionary<string, int> anchorByName = BuildAnchorByName(parameters);
                HashSet<string> declaredNames = new(anchorByName.Keys, StringComparer.Ordinal);

                List<NamedDocTag> docTags = GetNamedTags(doc, "param");

                NamedTagAnalyzer.Analyze(
                    findings,
                    tree,
                    filePath,
                    xmlTagName: "param",
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
        /// Tries to get the parameters for a supported declaration node.
        /// </summary>
        /// <param name="declaration">The declaration node.</param>
        /// <param name="parameters">The extracted parameters.</param>
        /// <returns><see langword="true"/> if parameters could be extracted; otherwise <see langword="false"/>.</returns>
        private static bool TryGetParameters(SyntaxNode declaration, out SeparatedSyntaxList<ParameterSyntax> parameters)
        {
            if (declaration is MethodDeclarationSyntax methodDecl)
            {
                parameters = methodDecl.ParameterList.Parameters;
                return true;
            }

            if (declaration is ConstructorDeclarationSyntax ctorDecl)
            {
                parameters = ctorDecl.ParameterList.Parameters;
                return true;
            }

            if (declaration is DelegateDeclarationSyntax delegateDecl)
            {
                parameters = delegateDecl.ParameterList.Parameters;
                return true;
            }

            if (declaration is IndexerDeclarationSyntax indexerDecl)
            {
                parameters = indexerDecl.ParameterList.Parameters;
                return true;
            }

            if (declaration is OperatorDeclarationSyntax operatorDecl)
            {
                parameters = operatorDecl.ParameterList.Parameters;
                return true;
            }

            if (declaration is ConversionOperatorDeclarationSyntax conversionDecl)
            {
                parameters = conversionDecl.ParameterList.Parameters;
                return true;
            }

            parameters = default;
            return false;
        }

        /// <summary>
        /// Builds a lookup table mapping parameter names to an absolute anchor position used for missing documentation findings.
        /// </summary>
        /// <param name="parameters">The parameter list.</param>
        /// <returns>A dictionary mapping parameter name to identifier span start.</returns>
        private static Dictionary<string, int> BuildAnchorByName(SeparatedSyntaxList<ParameterSyntax> parameters)
        {
            Dictionary<string, int> anchors = new(StringComparer.Ordinal);

            foreach (ParameterSyntax parameter in parameters)
            {
                string name = parameter.Identifier.ValueText;
                if (string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }

                if (!anchors.ContainsKey(name))
                {
                    anchors.Add(name, parameter.Identifier.SpanStart);
                }
            }

            return anchors;
        }

        /// <summary>
        /// Extracts all named documentation tags with the given XML local name.
        /// Tags without a name attribute are ignored because well-formedness handles those cases.
        /// </summary>
        /// <param name="doc">The documentation comment.</param>
        /// <param name="xmlTagName">The XML tag name ("param").</param>
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
