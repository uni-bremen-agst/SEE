using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using XMLDocNormalizer.Checks.Infrastructure;
using XMLDocNormalizer.Checks.Infrastructure.Namespace;
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
        /// <summary>
        /// Scans the syntax tree and returns findings for DOC100/DOC200/DOC210 with initial <see cref="XmlDocOptions"/>.
        /// </summary>
        /// <param name="tree">The syntax tree to analyze.</param>
        /// <param name="filePath">The file path used for reporting.</param>
        /// <returns>A list of findings.</returns>
        public static List<Finding> FindBasicSmells(SyntaxTree tree, string filePath)
        {
            return FindBasicSmells(tree, filePath, new XmlDocOptions());
        }

        /// <summary>
        /// Scans the syntax tree and returns findings for DOC100/DOC200/DOC210.
        /// </summary>
        /// <param name="tree">The syntax tree to analyze.</param>
        /// <param name="filePath">The file path used for reporting.</param>
        /// <param name="options">The XMLDocOptions for this tool run.</param>
        /// <returns>A list of findings.</returns>
        public static List<Finding> FindBasicSmells(SyntaxTree tree, string filePath, XmlDocOptions options)
        {
            NamespaceDocumentationAggregator aggregartor =
                new(enabled: false);

            return FindBasicSmells(tree, filePath, options, aggregartor);
            // List<Finding> findings = new();

            // CompilationUnitSyntax root = tree.GetCompilationUnitRoot();

            // IEnumerable<MemberDeclarationSyntax> members =
            //     root.DescendantNodes()
            //         .OfType<MemberDeclarationSyntax>();

            // foreach (MemberDeclarationSyntax member in members)
            // {
            //     if (!options.CheckEnumMembers && member is EnumMemberDeclarationSyntax
            //         || !options.RequireDocumentationForNamespaces && member is NamespaceDeclarationSyntax)
            //     {
            //         continue;
            //     }

            //     DocumentationCommentTriviaSyntax? doc = XmlDocUtils.TryGetDocComment(member);

            //     if (doc == null)
            //     {
            //         // DOC100 – Missing documentation comment.
            //         findings.Add(FindingFactory.AtPosition(
            //             tree,
            //             filePath,
            //             tagName: "documentation",
            //             XmlDocSmells.MissingDocumentation,
            //             MemberAnchorResolver.GetAnchorPosition(member)));
            //         continue;
            //     }

            //     // If fields are allowed to omit <summary>, skip DOC200/DOC210 for fields.
            //     if (!options.RequireSummaryForFields
            //         && (member is FieldDeclarationSyntax || member is EventFieldDeclarationSyntax))
            //     {
            //         continue;
            //     }

            //     XmlElementSyntax? summaryElement = XmlDocElementQuery.FirstByName(doc, "summary");

            //     if (summaryElement == null)
            //     {
            //         // DOC200 – Missing <summary>.
            //         findings.Add(FindingFactory.AtPosition(
            //             tree,
            //             filePath,
            //             tagName: "summary",
            //             XmlDocSmells.MissingSummary,
            //             doc.SpanStart));
            //         continue;
            //     }

            //     if (!XmlDocUtils.HasMeaningfulContent(summaryElement))
            //     {
            //         // DOC210 – Empty <summary>.
            //         findings.Add(FindingFactory.AtPosition(
            //             tree,
            //             filePath,
            //             tagName: "summary",
            //             XmlDocSmells.EmptySummary,
            //             summaryElement.SpanStart));
            //     }
            // }

            // return findings;
        }

        /// <summary>
        /// Scans the syntax tree and returns findings for DOC100/DOC200/DOC210.
        /// </summary>
        /// <param name="tree">The syntax tree to analyze.</param>
        /// <param name="filePath">The file path used for reporting.</param>
        /// <param name="options">The XMLDocOptions for this tool run.</param>
        /// <param name="namespaceAggregator">
        /// Aggregates missing namespace documentation into a single DOC101 per (directory, namespace) and enforces
        /// that namespace documentation lives in the preferred dedicated namespace doc file.
        /// </param>
        /// <returns>A list of findings.</returns>
        public static List<Finding> FindBasicSmells(
            SyntaxTree tree,
            string filePath,
            XmlDocOptions options,
            NamespaceDocumentationAggregator namespaceAggregator)
        {
            ArgumentNullException.ThrowIfNull(namespaceAggregator);

            List<Finding> findings = new();

            CompilationUnitSyntax root = tree.GetCompilationUnitRoot();

            IEnumerable<MemberDeclarationSyntax> members =
                root.DescendantNodes()
                    .OfType<MemberDeclarationSyntax>();

            foreach (MemberDeclarationSyntax member in members)
            {
                if (!options.CheckEnumMembers && member is EnumMemberDeclarationSyntax)
                {
                    continue;
                }

                if (!options.RequireDocumentationForNamespaces && member is BaseNamespaceDeclarationSyntax)
                {
                    continue;
                }

                DocumentationCommentTriviaSyntax? doc = XmlDocUtils.TryGetDocComment(member);

                // Namespace handling:
                // - Missing namespace documentation is aggregated (DOC101) and not emitted as DOC100.
                // - Namespace summary validation (DOC200/DOC210) is only applied in the preferred namespace doc file.
                if (options.RequireDocumentationForNamespaces && member is BaseNamespaceDeclarationSyntax ns)
                {
                    string namespaceName = ns.Name.ToString();

                    if (doc == null)
                    {
                        namespaceAggregator.RegisterMissingNamespaceDocumentation(
                            tree,
                            filePath,
                            namespaceName,
                            MemberAnchorResolver.GetAnchorPosition(member));

                        // Important: no DOC100 for namespaces; DOC101 is emitted at end of run.
                        continue;
                    }

                    namespaceAggregator.RegisterDocumentedNamespace(filePath, namespaceName);

                    if (!namespaceAggregator.IsPreferredNamespaceDocFile(filePath, namespaceName))
                    {
                        continue;
                    }
                }

                if (doc == null)
                {
                    // DOC100 – Missing documentation comment.
                    findings.Add(FindingFactory.AtPosition(
                        tree,
                        filePath,
                        tagName: "documentation",
                        XmlDocSmells.MissingDocumentation,
                        MemberAnchorResolver.GetAnchorPosition(member)));
                    continue;
                }

                // If fields are allowed to omit <summary>, skip DOC200/DOC210 for fields.
                if (!options.RequireSummaryForFields
                    && (member is FieldDeclarationSyntax || member is EventFieldDeclarationSyntax))
                {
                    continue;
                }

                XmlElementSyntax? summaryElement = XmlDocElementQuery.FirstByName(doc, "summary");

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
    }
}
