using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using XMLDocNormalizer.Models;
using XMLDocNormalizer.Utils;

namespace XMLDocNormalizer.Checks.Infrastructure.Tags
{
    /// <summary>
    /// Analyzes XML documentation reference tags that point to declared names.
    /// </summary>
    /// <remarks>
    /// This analyzer is used for reference tags such as paramref and typeparamref.
    /// It validates the required name attribute, rejects unsupported attributes, and reports references
    /// to names that do not exist on the owning declaration.
    /// </remarks>
    internal static class ReferenceTagAnalyzer
    {
        /// <summary>
        /// Analyzes named XML reference tags in a documentation comment.
        /// </summary>
        /// <param name="findings">The collection to which findings will be added.</param>
        /// <param name="tree">The syntax tree containing the documentation comment.</param>
        /// <param name="filePath">The file path used for reporting.</param>
        /// <param name="doc">The documentation comment to inspect.</param>
        /// <param name="declaration">The declaration that owns the documentation comment.</param>
        /// <param name="xmlTagName">The XML tag name to analyze.</param>
        /// <param name="declaredNames">The set of declared names that may be referenced by the tag.</param>
        /// <param name="missingNameSmell">The smell emitted when the reference tag has no name attribute.</param>
        /// <param name="unknownReferenceSmell">The smell emitted when the reference tag points to an unknown name.</param>
        /// <param name="invalidAttributeSmell">The smell emitted when the reference tag contains an unsupported attribute.</param>
        /// <param name="subjectKind">The finding context subject kind to use for produced findings.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="xmlTagName"/> is <see langword="null"/>,
        /// or when a smell definition used to create a finding is
        /// <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="filePath"/> is <see langword="null"/>,
        /// empty, or consists only of white-space characters, or when
        /// <paramref name="xmlTagName"/> is not a valid finding tag name.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when a source position derived from <paramref name="doc"/>
        /// is not valid for <paramref name="tree"/>.
        /// </exception>
        public static void Analyze(
            List<Finding> findings,
            SyntaxTree tree,
            string filePath,
            DocumentationCommentTriviaSyntax doc,
            SyntaxNode declaration,
            string xmlTagName,
            IReadOnlySet<string> declaredNames,
            XmlDocSmell missingNameSmell,
            XmlDocSmell unknownReferenceSmell,
            XmlDocSmell invalidAttributeSmell,
            string subjectKind)
        {
            foreach (XmlEmptyElementSyntax element in doc.DescendantNodes().OfType<XmlEmptyElementSyntax>())
            {
                if (!string.Equals(SyntaxUtils.GetLocalName(element), xmlTagName, StringComparison.Ordinal))
                {
                    continue;
                }

                AnalyzeEmptyElement(
                    findings,
                    tree,
                    filePath,
                    declaration,
                    xmlTagName,
                    declaredNames,
                    missingNameSmell,
                    unknownReferenceSmell,
                    invalidAttributeSmell,
                    subjectKind,
                    element);
            }

            foreach (XmlElementSyntax element in XmlDocElementQuery.ElementsByName(doc, xmlTagName))
            {
                AnalyzeElement(
                    findings,
                    tree,
                    filePath,
                    declaration,
                    xmlTagName,
                    declaredNames,
                    missingNameSmell,
                    unknownReferenceSmell,
                    invalidAttributeSmell,
                    subjectKind,
                    element);
            }
        }

        /// <summary>
        /// Analyzes a non-empty XML reference element.
        /// </summary>
        /// <param name="findings">The collection to which findings will be added.</param>
        /// <param name="tree">The syntax tree containing the documentation comment.</param>
        /// <param name="filePath">The file path used for reporting.</param>
        /// <param name="declaration">The declaration that owns the documentation comment.</param>
        /// <param name="xmlTagName">The XML tag name being analyzed.</param>
        /// <param name="declaredNames">The set of declared names that may be referenced by the tag.</param>
        /// <param name="missingNameSmell">The smell emitted when the reference tag has no name attribute.</param>
        /// <param name="unknownReferenceSmell">The smell emitted when the reference tag points to an unknown name.</param>
        /// <param name="invalidAttributeSmell">The smell emitted when the reference tag contains an unsupported attribute.</param>
        /// <param name="subjectKind">The finding context subject kind to use for produced findings.</param>
        /// <param name="element">The XML element to analyze.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="element"/> is <see langword="null"/>,
        /// or when a smell definition used to create a finding is
        /// <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="filePath"/> or
        /// <paramref name="xmlTagName"/> is <see langword="null"/>, empty,
        /// or consists only of white-space characters and a finding is created.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when a source position derived from
        /// <paramref name="element"/> is not valid for
        /// <paramref name="tree"/>.
        /// </exception>
        private static void AnalyzeElement(
            List<Finding> findings,
            SyntaxTree tree,
            string filePath,
            SyntaxNode declaration,
            string xmlTagName,
            IReadOnlySet<string> declaredNames,
            XmlDocSmell missingNameSmell,
            XmlDocSmell unknownReferenceSmell,
            XmlDocSmell invalidAttributeSmell,
            string subjectKind,
            XmlElementSyntax element)
        {
            string? targetName = TryGetNameAttributeValue(element);

            AddInvalidAttributeFindings(
                findings,
                tree,
                filePath,
                declaration,
                xmlTagName,
                invalidAttributeSmell,
                subjectKind,
                SyntaxUtils.GetAttributes(element),
                element);

            AddMissingOrUnknownReferenceFinding(
                findings,
                tree,
                filePath,
                declaration,
                xmlTagName,
                declaredNames,
                missingNameSmell,
                unknownReferenceSmell,
                subjectKind,
                element,
                targetName);
        }

        /// <summary>
        /// Analyzes an empty XML reference element.
        /// </summary>
        /// <param name="findings">The collection to which findings will be added.</param>
        /// <param name="tree">The syntax tree containing the documentation comment.</param>
        /// <param name="filePath">The file path used for reporting.</param>
        /// <param name="declaration">The declaration that owns the documentation comment.</param>
        /// <param name="xmlTagName">The XML tag name being analyzed.</param>
        /// <param name="declaredNames">The set of declared names that may be referenced by the tag.</param>
        /// <param name="missingNameSmell">The smell emitted when the reference tag has no name attribute.</param>
        /// <param name="unknownReferenceSmell">The smell emitted when the reference tag points to an unknown name.</param>
        /// <param name="invalidAttributeSmell">The smell emitted when the reference tag contains an unsupported attribute.</param>
        /// <param name="subjectKind">The finding context subject kind to use for produced findings.</param>
        /// <param name="element">The empty XML element to analyze.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="element"/> is <see langword="null"/>,
        /// or when a smell definition used to create a finding is
        /// <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="filePath"/> or
        /// <paramref name="xmlTagName"/> is <see langword="null"/>, empty,
        /// or consists only of white-space characters and a finding is created.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when a source position derived from
        /// <paramref name="element"/> is not valid for
        /// <paramref name="tree"/>.
        /// </exception>
        private static void AnalyzeEmptyElement(
            List<Finding> findings,
            SyntaxTree tree,
            string filePath,
            SyntaxNode declaration,
            string xmlTagName,
            IReadOnlySet<string> declaredNames,
            XmlDocSmell missingNameSmell,
            XmlDocSmell unknownReferenceSmell,
            XmlDocSmell invalidAttributeSmell,
            string subjectKind,
            XmlEmptyElementSyntax element)
        {
            string? targetName = TryGetNameAttributeValue(element);

            AddInvalidAttributeFindings(
                findings,
                tree,
                filePath,
                declaration,
                xmlTagName,
                invalidAttributeSmell,
                subjectKind,
                SyntaxUtils.GetAttributes(element),
                element);

            AddMissingOrUnknownReferenceFinding(
                findings,
                tree,
                filePath,
                declaration,
                xmlTagName,
                declaredNames,
                missingNameSmell,
                unknownReferenceSmell,
                subjectKind,
                element,
                targetName);
        }

        /// <summary>
        /// Adds findings for unsupported attributes on a reference tag.
        /// </summary>
        /// <param name="findings">The collection to which findings will be added.</param>
        /// <param name="tree">The syntax tree containing the documentation comment.</param>
        /// <param name="filePath">The file path used for reporting.</param>
        /// <param name="declaration">The declaration that owns the documentation comment.</param>
        /// <param name="xmlTagName">The XML tag name being analyzed.</param>
        /// <param name="invalidAttributeSmell">The smell emitted for unsupported attributes.</param>
        /// <param name="subjectKind">The finding context subject kind to use for produced findings.</param>
        /// <param name="attributes">The XML attributes to inspect.</param>
        /// <param name="snippetNode">The syntax node used to create the finding snippet.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="invalidAttributeSmell"/> is
        /// <see langword="null"/> and an unsupported attribute produces a finding.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="filePath"/> or
        /// <paramref name="xmlTagName"/> is <see langword="null"/>, empty,
        /// or consists only of white-space characters and an unsupported
        /// attribute produces a finding.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when the source position of an unsupported attribute is not
        /// valid for <paramref name="tree"/>.
        /// </exception>
        private static void AddInvalidAttributeFindings(
            List<Finding> findings,
            SyntaxTree tree,
            string filePath,
            SyntaxNode declaration,
            string xmlTagName,
            XmlDocSmell invalidAttributeSmell,
            string subjectKind,
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
                    tagName: xmlTagName,
                    invalidAttributeSmell,
                    attribute.SpanStart,
                    FindingContextBuilder.ForDeclaration(
                        declaration,
                        subjectKind,
                        targetName: attributeName,
                        filePath: filePath),
                    snippet: SyntaxUtils.GetSnippet(snippetNode),
                    attributeName));
            }
        }

        /// <summary>
        /// Adds a missing-name or unknown-reference finding for a reference tag.
        /// </summary>
        /// <param name="findings">The collection to which findings will be added.</param>
        /// <param name="tree">The syntax tree containing the documentation comment.</param>
        /// <param name="filePath">The file path used for reporting.</param>
        /// <param name="declaration">The declaration that owns the documentation comment.</param>
        /// <param name="xmlTagName">The XML tag name being analyzed.</param>
        /// <param name="declaredNames">The set of declared names that may be referenced by the tag.</param>
        /// <param name="missingNameSmell">The smell emitted when the reference tag has no name attribute.</param>
        /// <param name="unknownReferenceSmell">The smell emitted when the reference tag points to an unknown name.</param>
        /// <param name="subjectKind">The finding context subject kind to use for produced findings.</param>
        /// <param name="element">The reference tag syntax node.</param>
        /// <param name="targetName">The referenced name, if present.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="missingNameSmell"/> is
        /// <see langword="null"/> while reporting a missing name, or when
        /// <paramref name="unknownReferenceSmell"/> is
        /// <see langword="null"/> while reporting an unknown reference.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="filePath"/> or
        /// <paramref name="xmlTagName"/> is <see langword="null"/>, empty,
        /// or consists only of white-space characters and a finding is created.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when the start of <paramref name="element"/> is not a valid
        /// source position in <paramref name="tree"/>.
        /// </exception>
        private static void AddMissingOrUnknownReferenceFinding(
            List<Finding> findings,
            SyntaxTree tree,
            string filePath,
            SyntaxNode declaration,
            string xmlTagName,
            IReadOnlySet<string> declaredNames,
            XmlDocSmell missingNameSmell,
            XmlDocSmell unknownReferenceSmell,
            string subjectKind,
            SyntaxNode element,
            string? targetName)
        {
            if (string.IsNullOrWhiteSpace(targetName))
            {
                findings.Add(FindingFactory.AtSpanStart(
                    tree,
                    filePath,
                    tagName: xmlTagName,
                    missingNameSmell,
                    element.Span,
                    FindingContextBuilder.ForDeclaration(
                        declaration,
                        subjectKind,
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
                tagName: xmlTagName,
                unknownReferenceSmell,
                element.Span,
                FindingContextBuilder.ForDeclaration(
                    declaration,
                    subjectKind,
                    targetName: targetName,
                    filePath: filePath),
                snippet: SyntaxUtils.GetSnippet(element),
                targetName));
        }

        /// <summary>
        /// Tries to extract the name attribute value from an XML element.
        /// </summary>
        /// <param name="element">The XML element to inspect.</param>
        /// <returns>The name value if present; otherwise null.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="element"/> is <see langword="null"/>.
        /// </exception>
        private static string? TryGetNameAttributeValue(XmlElementSyntax element)
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
        /// Tries to extract the name attribute value from an empty XML element.
        /// </summary>
        /// <param name="element">The empty XML element to inspect.</param>
        /// <returns>The name value if present; otherwise null.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="element"/> is <see langword="null"/>.
        /// </exception>
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
    }
}
