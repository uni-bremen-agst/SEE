using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using XMLDocNormalizer.Checks.Infrastructure;
using XMLDocNormalizer.Models;
using XMLDocNormalizer.Utils;

namespace XMLDocNormalizer.Checks
{
    /// <summary>
    /// Detects <c>see</c> and <c>seealso</c> documentation smells that require semantic analysis.
    /// </summary>
    internal static class XmlDocSeeSemanticDetector
    {
        /// <summary>
        /// Scans the syntax tree and returns findings for unresolved <c>cref</c> targets
        /// on <c>see</c> and <c>seealso</c> elements.
        /// </summary>
        /// <param name="tree">The syntax tree to analyze.</param>
        /// <param name="filePath">The file path used for reporting.</param>
        /// <param name="semanticModel">The semantic model for the syntax tree.</param>
        /// <returns>A list of findings.</returns>
        public static List<Finding> FindSeeSmells(
            SyntaxTree tree,
            string filePath,
            SemanticModel semanticModel)
        {
            List<Finding> findings = new List<Finding>();
            CompilationUnitSyntax root = tree.GetCompilationUnitRoot();

            IEnumerable<XmlCrefAttributeSyntax> crefAttributes =
                root.DescendantNodes(descendIntoTrivia: true)
                    .OfType<XmlCrefAttributeSyntax>();

            foreach (XmlCrefAttributeSyntax crefAttribute in crefAttributes)
            {
                AnalyzeCrefAttribute(
                    findings,
                    tree,
                    filePath,
                    semanticModel,
                    crefAttribute);
            }

            return findings;
        }

        /// <summary>
        /// Analyzes a single XML <c>cref</c> attribute on a <c>see</c> or <c>seealso</c> tag.
        /// </summary>
        /// <param name="findings">The findings collection to append to.</param>
        /// <param name="tree">The syntax tree used for reporting.</param>
        /// <param name="filePath">The file path used for reporting.</param>
        /// <param name="semanticModel">The semantic model for symbol resolution.</param>
        /// <param name="crefAttribute">The cref attribute to analyze.</param>
        private static void AnalyzeCrefAttribute(
            List<Finding> findings,
            SyntaxTree tree,
            string filePath,
            SemanticModel semanticModel,
            XmlCrefAttributeSyntax crefAttribute)
        {
            CrefOwnerInfo? ownerInfo = TryGetCrefOwnerInfo(crefAttribute);

            if (ownerInfo == null)
            {
                return;
            }

            string tagName = ownerInfo.TagName;

            if ((tagName != "see") && (tagName != "seealso"))
            {
                return;
            }

            CrefSyntax? crefSyntax = crefAttribute.Cref;

            if (crefSyntax == null)
            {
                return;
            }

            SymbolInfo symbolInfo = semanticModel.GetSymbolInfo(crefSyntax);

            if (symbolInfo.Symbol != null)
            {
                return;
            }

            string crefValue = crefSyntax.ToString();
            XmlDocSmell smell =
                tagName == "see"
                    ? XmlDocSmells.InvalidSeeCref
                    : XmlDocSmells.InvalidSeeAlsoCref;

            findings.Add(
                FindingFactory.AtPosition(
                    tree,
                    filePath,
                    tagName,
                    smell,
                    crefAttribute.SpanStart,
                    SyntaxUtils.GetSnippet(ownerInfo.OwnerNode),
                    crefValue));
        }

        /// <summary>
        /// Tries to determine the owning XML tag of a cref attribute.
        /// </summary>
        /// <param name="crefAttribute">The cref attribute to inspect.</param>
        /// <returns>
        /// Information about the owning XML tag if it belongs to an XML element;
        /// otherwise <see langword="null"/>.
        /// </returns>
        private static CrefOwnerInfo? TryGetCrefOwnerInfo(XmlCrefAttributeSyntax crefAttribute)
        {
            SyntaxNode? parent = crefAttribute.Parent;

            if (parent is XmlEmptyElementSyntax emptyElement)
            {
                return new CrefOwnerInfo(
                    emptyElement.Name.LocalName.Text,
                    emptyElement);
            }

            if (parent is XmlElementStartTagSyntax startTag)
            {
                XmlElementSyntax? element = startTag.Parent as XmlElementSyntax;

                if (element == null)
                {
                    return null;
                }

                return new CrefOwnerInfo(
                    startTag.Name.LocalName.Text,
                    element);
            }

            return null;
        }

        /// <summary>
        /// Represents the owning XML tag information of a cref attribute.
        /// </summary>
        private sealed class CrefOwnerInfo
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="CrefOwnerInfo"/> class.
            /// </summary>
            /// <param name="tagName">The owning XML tag name.</param>
            /// <param name="ownerNode">The owning XML node.</param>
            public CrefOwnerInfo(string tagName, XmlNodeSyntax ownerNode)
            {
                TagName = tagName;
                OwnerNode = ownerNode;
            }

            /// <summary>
            /// Gets the owning XML tag name.
            /// </summary>
            public string TagName { get; }

            /// <summary>
            /// Gets the owning XML node.
            /// </summary>
            public XmlNodeSyntax OwnerNode { get; }
        }
    }
}
