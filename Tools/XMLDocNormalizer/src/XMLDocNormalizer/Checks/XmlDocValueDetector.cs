using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using XMLDocNormalizer.Checks.Infrastructure;
using XMLDocNormalizer.Models;
using XMLDocNormalizer.Utils;

namespace XMLDocNormalizer.Checks
{
    /// <summary>
    /// Detects value-related XML documentation smells (DOC800-DOC831).
    /// </summary>
    internal static class XmlDocValueDetector
    {
        /// <summary>
        /// Classifies the member kind for value-tag analysis.
        /// </summary>
        private enum ValueTargetKind
        {
            ReadableProperty,
            WriteOnlyProperty,
            Indexer,
            InvalidMember
        }

        /// <summary>
        /// Carries all precomputed information required to analyze value-related smells for one member.
        /// </summary>
        private sealed class ValueAnalysisContext
        {
            /// <summary>
            /// Gets the analyzed member.
            /// </summary>
            public required MemberDeclarationSyntax Member { get; init; }

            /// <summary>
            /// Gets the XML documentation comment of the member.
            /// </summary>
            public required DocumentationCommentTriviaSyntax Doc { get; init; }

            /// <summary>
            /// Gets all value tags found in the XML documentation comment.
            /// </summary>
            public required List<XmlElementSyntax> ValueTags { get; init; }

            /// <summary>
            /// Gets the classified value-target kind of the member.
            /// </summary>
            public required ValueTargetKind TargetKind { get; init; }

            /// <summary>
            /// Gets the member name used for smell message formatting where applicable.
            /// </summary>
            public string? MemberName { get; init; }
        }

        /// <summary>
        /// Scans the syntax tree and returns value-related findings.
        /// </summary>
        /// <param name="tree">The syntax tree to analyze.</param>
        /// <param name="filePath">The file path used for reporting.</param>
        /// <returns>A list of findings.</returns>
        public static List<Finding> FindValueSmells(SyntaxTree tree, string filePath)
        {
            List<Finding> findings = new();

            CompilationUnitSyntax root = tree.GetCompilationUnitRoot();
            IEnumerable<MemberDeclarationSyntax> members = root.DescendantNodes().OfType<MemberDeclarationSyntax>();

            foreach (MemberDeclarationSyntax member in members)
            {
                ValueAnalysisContext? context = TryCreateContext(member);
                if (context == null)
                {
                    continue;
                }

                AddMissingValueFindings(findings, tree, filePath, context);
                AddEmptyValueFindings(findings, tree, filePath, context);
                AddDuplicateValueFindings(findings, tree, filePath, context);
                AddInvalidValueUsageFindings(findings, tree, filePath, context);
            }

            return findings;
        }

        /// <summary>
        /// Creates a value-analysis context for a documented member.
        /// </summary>
        /// <param name="member">The member to inspect.</param>
        /// <returns>
        /// A fully prepared analysis context, or <see langword="null"/> if the member has no XML documentation comment.
        /// </returns>
        private static ValueAnalysisContext? TryCreateContext(MemberDeclarationSyntax member)
        {
            DocumentationCommentTriviaSyntax? doc = XmlDocUtils.TryGetDocComment(member);
            if (doc == null)
            {
                return null;
            }

            return new ValueAnalysisContext
            {
                Member = member,
                Doc = doc,
                ValueTags = XmlDocElementQuery.AllByName(doc, "value").ToList(),
                TargetKind = ClassifyMember(member),
                MemberName = GetMemberName(member)
            };
        }

        /// <summary>
        /// Adds missing-value findings (DOC800/DOC801).
        /// </summary>
        /// <param name="findings">The target finding list.</param>
        /// <param name="tree">The syntax tree used for location calculation.</param>
        /// <param name="filePath">The file path used for reporting.</param>
        /// <param name="context">The prepared member analysis context.</param>
        private static void AddMissingValueFindings(
            List<Finding> findings,
            SyntaxTree tree,
            string filePath,
            ValueAnalysisContext context)
        {
            if (context.ValueTags.Count != 0)
            {
                return;
            }

            switch (context.TargetKind)
            {
                case ValueTargetKind.ReadableProperty:
                    findings.Add(FindingFactory.AtPosition(
                        tree,
                        filePath,
                        tagName: "value",
                        XmlDocSmells.MissingValueOnProperty,
                        MemberAnchorResolver.GetAnchorPosition(context.Member),
                        snippet: string.Empty,
                        context.MemberName!));
                    break;

                case ValueTargetKind.Indexer:
                    findings.Add(FindingFactory.AtPosition(
                        tree,
                        filePath,
                        tagName: "value",
                        XmlDocSmells.MissingValueOnIndexer,
                        MemberAnchorResolver.GetAnchorPosition(context.Member),
                        snippet: string.Empty));
                    break;
            }
        }

        /// <summary>
        /// Adds empty-value findings (DOC810/DOC811).
        /// </summary>
        /// <param name="findings">The target finding list.</param>
        /// <param name="tree">The syntax tree used for location calculation.</param>
        /// <param name="filePath">The file path used for reporting.</param>
        /// <param name="context">The prepared member analysis context.</param>
        private static void AddEmptyValueFindings(
            List<Finding> findings,
            SyntaxTree tree,
            string filePath,
            ValueAnalysisContext context)
        {
            foreach (XmlElementSyntax valueTag in context.ValueTags)
            {
                if (XmlDocUtils.HasMeaningfulContent(valueTag))
                {
                    continue;
                }

                switch (context.TargetKind)
                {
                    case ValueTargetKind.ReadableProperty:
                        findings.Add(FindingFactory.AtPosition(
                            tree,
                            filePath,
                            tagName: "value",
                            XmlDocSmells.EmptyValueOnProperty,
                            valueTag.SpanStart,
                            snippet: valueTag.ToString(),
                            context.MemberName!));
                        break;

                    case ValueTargetKind.Indexer:
                        findings.Add(FindingFactory.AtPosition(
                            tree,
                            filePath,
                            tagName: "value",
                            XmlDocSmells.EmptyValueOnIndexer,
                            valueTag.SpanStart,
                            snippet: valueTag.ToString()));
                        break;
                }
            }
        }

        /// <summary>
        /// Adds duplicate-value findings (DOC820/DOC821).
        /// </summary>
        /// <param name="findings">The target finding list.</param>
        /// <param name="tree">The syntax tree used for location calculation.</param>
        /// <param name="filePath">The file path used for reporting.</param>
        /// <param name="context">The prepared member analysis context.</param>
        private static void AddDuplicateValueFindings(
            List<Finding> findings,
            SyntaxTree tree,
            string filePath,
            ValueAnalysisContext context)
        {
            if (context.ValueTags.Count < 2)
            {
                return;
            }

            foreach (XmlElementSyntax duplicateTag in context.ValueTags.Skip(1))
            {
                switch (context.TargetKind)
                {
                    case ValueTargetKind.ReadableProperty:
                        findings.Add(FindingFactory.AtPosition(
                            tree,
                            filePath,
                            tagName: "value",
                            XmlDocSmells.DuplicateValueOnProperty,
                            duplicateTag.SpanStart,
                            snippet: duplicateTag.ToString(),
                            context.MemberName!));
                        break;

                    case ValueTargetKind.Indexer:
                        findings.Add(FindingFactory.AtPosition(
                            tree,
                            filePath,
                            tagName: "value",
                            XmlDocSmells.DuplicateValueOnIndexer,
                            duplicateTag.SpanStart,
                            snippet: duplicateTag.ToString()));
                        break;
                }
            }
        }

        /// <summary>
        /// Adds invalid value-usage findings (DOC830/DOC831).
        /// </summary>
        /// <param name="findings">The target finding list.</param>
        /// <param name="tree">The syntax tree used for location calculation.</param>
        /// <param name="filePath">The file path used for reporting.</param>
        /// <param name="context">The prepared member analysis context.</param>
        private static void AddInvalidValueUsageFindings(
            List<Finding> findings,
            SyntaxTree tree,
            string filePath,
            ValueAnalysisContext context)
        {
            foreach (XmlElementSyntax valueTag in context.ValueTags)
            {
                switch (context.TargetKind)
                {
                    case ValueTargetKind.WriteOnlyProperty:
                        findings.Add(FindingFactory.AtPosition(
                            tree,
                            filePath,
                            tagName: "value",
                            XmlDocSmells.ValueOnWriteOnlyProperty,
                            valueTag.SpanStart,
                            snippet: valueTag.ToString(),
                            context.MemberName!));
                        break;

                    case ValueTargetKind.InvalidMember:
                        findings.Add(FindingFactory.AtPosition(
                            tree,
                            filePath,
                            tagName: "value",
                            XmlDocSmells.ValueOnInvalidMember,
                            valueTag.SpanStart,
                            snippet: valueTag.ToString()));
                        break;
                }
            }
        }

        /// <summary>
        /// Classifies the member for value-tag analysis.
        /// </summary>
        /// <param name="member">The member to classify.</param>
        /// <returns>The matching value-target kind.</returns>
        private static ValueTargetKind ClassifyMember(MemberDeclarationSyntax member)
        {
            if (member is PropertyDeclarationSyntax property)
            {
                if (IsReadableProperty(property))
                {
                    return ValueTargetKind.ReadableProperty;
                }

                if (IsWriteOnlyProperty(property))
                {
                    return ValueTargetKind.WriteOnlyProperty;
                }

                return ValueTargetKind.InvalidMember;
            }

            if (member is IndexerDeclarationSyntax)
            {
                return ValueTargetKind.Indexer;
            }

            return ValueTargetKind.InvalidMember;
        }

        /// <summary>
        /// Gets the member name used for smell message formatting where applicable.
        /// </summary>
        /// <param name="member">The member to inspect.</param>
        /// <returns>The member name if available; otherwise <see langword="null"/>.</returns>
        private static string? GetMemberName(MemberDeclarationSyntax member)
        {
            return member switch
            {
                PropertyDeclarationSyntax property => property.Identifier.ValueText,
                _ => null
            };
        }

        /// <summary>
        /// Determines whether the property is readable and therefore expected to have a value tag.
        /// </summary>
        /// <param name="property">The property to inspect.</param>
        /// <returns><see langword="true"/> if the property is readable; otherwise <see langword="false"/>.</returns>
        private static bool IsReadableProperty(PropertyDeclarationSyntax property)
        {
            if (property.ExpressionBody != null)
            {
                return true;
            }

            if (property.AccessorList == null)
            {
                return false;
            }

            return property.AccessorList.Accessors.Any(
                static accessor => accessor.Kind() == SyntaxKind.GetAccessorDeclaration);
        }

        /// <summary>
        /// Determines whether the property is write-only.
        /// </summary>
        /// <param name="property">The property to inspect.</param>
        /// <returns><see langword="true"/> if the property is write-only; otherwise <see langword="false"/>.</returns>
        private static bool IsWriteOnlyProperty(PropertyDeclarationSyntax property)
        {
            if (property.ExpressionBody != null)
            {
                return false;
            }

            if (property.AccessorList == null)
            {
                return false;
            }

            bool hasGetter = property.AccessorList.Accessors.Any(
                static accessor => accessor.Kind() == SyntaxKind.GetAccessorDeclaration);

            bool hasSetter = property.AccessorList.Accessors.Any(
                static accessor => accessor.Kind() == SyntaxKind.SetAccessorDeclaration);

            return hasSetter && !hasGetter;
        }
    }
}
