using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using XMLDocNormalizer.Checks.Infrastructure;
using XMLDocNormalizer.Configuration;
using XMLDocNormalizer.Models;
using XMLDocNormalizer.Utils;

namespace XMLDocNormalizer.Checks
{
    /// <summary>
    /// Detects basic documentation smells:
    /// - DOC100: Missing documentation comment.
    /// - DOC200: Missing summary-tag.
    /// - DOC210: Empty summary-tag.
    /// </summary>
    internal static class XmlDocBasicDetector
    {
        public static List<Finding> FindBasicSmells(SyntaxTree tree, string filePath)
        {
            return FindBasicSmells(tree, filePath, new XmlDocOptions());
        }
        /// <summary>
        /// Scans the syntax tree and returns findings for DOC100/DOC200/DOC210.
        /// </summary>
        /// <param name="tree">The syntax tree to analyze.</param>
        /// <param name="filePath">The file path used for reporting.</param>
        /// <returns>A list of findings.</returns>
        public static List<Finding> FindBasicSmells(SyntaxTree tree, string filePath, XmlDocOptions options)
        {
            List<Finding> findings = new();

            CompilationUnitSyntax root = tree.GetCompilationUnitRoot();

            IEnumerable<MemberDeclarationSyntax> members =
                root.DescendantNodes()
                    .OfType<MemberDeclarationSyntax>();

            foreach (MemberDeclarationSyntax member in members)
            {
                if (!options.CheckEnumMembers && member is EnumMemberDeclarationSyntax
                    || !options.RequireDocumentationForNamespaces && member is NamespaceDeclarationSyntax)
                {
                    continue;
                }
                DocumentationCommentTriviaSyntax? doc = XmlDocUtils.TryGetDocComment(member);

                if (doc == null)
                {
                    // DOC100 – Missing documentation comment.
                    findings.Add(FindingFactory.AtPosition(
                        tree,
                        filePath,
                        tagName: "documentation",
                        XmlDocSmells.MissingDocumentation,
                        GetAnchorPosition(member)));
                    continue;
                }

                // If fields are allowed to omit <summary>, skip DOC200/DOC210 for fields.
                if (!options.RequireSummaryForFields
                    && (member is FieldDeclarationSyntax || member is EventFieldDeclarationSyntax))
                {
                    continue;
                }

                XmlElementSyntax? summaryElement = FindFirstElement(doc, "summary");

                if (summaryElement == null)
                {
                    // DOC200 – Missing <summary>.
                    findings.Add(FindingFactory.AtPosition(
                        tree,
                        filePath,
                        tagName: "summary",
                        XmlDocSmells.MissingSummary,
                        doc.SpanStart));
                    continue;
                }

                if (!XmlDocUtils.HasMeaningfulContent(summaryElement))
                {
                    // DOC210 – Empty <summary>.
                    findings.Add(FindingFactory.AtPosition(
                        tree,
                        filePath,
                        tagName: "summary",
                        XmlDocSmells.EmptySummary,
                        summaryElement.SpanStart));
                }
            }

            return findings;
        }

        /// <summary>
        /// Finds the first XML element with the given local name within the provided documentation comment.
        /// </summary>
        /// <param name="doc">The documentation comment syntax node.</param>
        /// <param name="localName">The expected local element name (e.g. summary in c-tag).</param>
        /// <returns>The first matching <see cref="XmlElementSyntax"/>, or null if none exists.</returns>
        private static XmlElementSyntax? FindFirstElement(DocumentationCommentTriviaSyntax doc, string localName)
        {
            IEnumerable<XmlElementSyntax> elements =
                doc.DescendantNodes()
                    .OfType<XmlElementSyntax>();

            foreach (XmlElementSyntax element in elements)
            {
                string name = element.StartTag.Name.LocalName.Text;
                if (string.Equals(name, localName, StringComparison.Ordinal))
                {
                    return element;
                }
            }

            return null;
        }

        /// <summary>
        /// Returns an anchor position within the member declaration that is suitable for reporting a finding.
        /// </summary>
        /// <param name="member">The member declaration to anchor the finding to.</param>
        /// <returns>The absolute position in the syntax tree used for line/column calculation.</returns>
        private static int GetAnchorPosition(MemberDeclarationSyntax member)
        {
            int fallback = member.GetFirstToken().SpanStart;

            return member switch
            {
                BaseTypeDeclarationSyntax typeDecl => typeDecl.Identifier.SpanStart,
                DelegateDeclarationSyntax delegateDecl => delegateDecl.Identifier.SpanStart,
                MethodDeclarationSyntax methodDecl => methodDecl.Identifier.SpanStart,
                ConstructorDeclarationSyntax ctorDecl => ctorDecl.Identifier.SpanStart,
                PropertyDeclarationSyntax propDecl => propDecl.Identifier.SpanStart,
                EventDeclarationSyntax eventDecl => eventDecl.Identifier.SpanStart,
                EnumMemberDeclarationSyntax enumMemberDecl => enumMemberDecl.Identifier.SpanStart,

                EventFieldDeclarationSyntax eventFieldDecl =>
                    eventFieldDecl.Declaration.Variables.FirstOrDefault() is { } variable
                        ? variable.Identifier.SpanStart
                        : fallback,

                FieldDeclarationSyntax fieldDecl =>
                    fieldDecl.Declaration.Variables.FirstOrDefault() is { } variable
                        ? variable.Identifier.SpanStart
                        : fallback,

                _ => fallback
            };
        }
    }
}
