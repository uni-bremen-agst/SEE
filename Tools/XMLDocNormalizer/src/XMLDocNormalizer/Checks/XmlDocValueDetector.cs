using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using XMLDocNormalizer.Checks.Infrastructure;
using XMLDocNormalizer.Checks.Infrastructure.Value;
using XMLDocNormalizer.Models;
using XMLDocNormalizer.Utils;

namespace XMLDocNormalizer.Checks
{
    /// <summary>
    /// Detects value-related XML documentation smells.
    /// </summary>
    /// <remarks>
    /// This detector reports missing value tags, empty value descriptions, duplicate value tags,
    /// value tags on write-only properties, and value tags on unsupported members.
    /// The analysis is syntax-based and does not require semantic model access.
    /// </remarks>
    internal static class XmlDocValueDetector
    {
        /// <summary>
        /// Scans the syntax tree and returns value-related findings.
        /// </summary>
        /// <param name="tree">The syntax tree to analyze.</param>
        /// <param name="filePath">The file path used for reporting.</param>
        /// <returns>
        /// A list of value-related findings.
        /// </returns>
        public static List<Finding> FindValueSmells(SyntaxTree tree, string filePath)
        {
            List<Finding> findings = new List<Finding>();

            CompilationUnitSyntax root = tree.GetCompilationUnitRoot();
            IEnumerable<MemberDeclarationSyntax> members = root.DescendantNodes().OfType<MemberDeclarationSyntax>();

            foreach (MemberDeclarationSyntax member in members)
            {
                ValueAnalysisContext? context = TryCreateContext(member, filePath);

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
        /// <param name="filePath">The file path used for reporting and file classification metadata.</param>
        /// <returns>
        /// A prepared analysis context if the member has an XML documentation comment; otherwise null.
        /// </returns>
        private static ValueAnalysisContext? TryCreateContext(MemberDeclarationSyntax member, string filePath)
        {
            DocumentationCommentTriviaSyntax? doc = XmlDocUtils.TryGetDocComment(member);

            if (doc == null)
            {
                return null;
            }

            string? memberName = GetMemberName(member);

            return new ValueAnalysisContext
            {
                Member = member,
                Doc = doc,
                ValueTags = XmlDocElementQuery.AllByName(doc, "value").ToList(),
                TargetKind = ClassifyMember(member),
                MemberName = memberName,
                FindingContext = FindingContextBuilder.ForDeclaration(
                    member,
                    "ValueTag",
                    targetName: memberName,
                    filePath: filePath)
            };
        }

        /// <summary>
        /// Adds missing-value findings.
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
                    {
                        findings.Add(FindingFactory.AtPosition(
                            tree,
                            filePath,
                            tagName: "value",
                            XmlDocSmells.MissingValueOnProperty,
                            MemberAnchorResolver.GetAnchorPosition(context.Member),
                            context.FindingContext,
                            snippet: string.Empty,
                            context.MemberName!));

                        break;
                    }

                case ValueTargetKind.Indexer:
                    {
                        findings.Add(FindingFactory.AtPosition(
                            tree,
                            filePath,
                            tagName: "value",
                            XmlDocSmells.MissingValueOnIndexer,
                            MemberAnchorResolver.GetAnchorPosition(context.Member),
                            context.FindingContext,
                            snippet: string.Empty));

                        break;
                    }
            }
        }

        /// <summary>
        /// Adds empty-value findings.
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
                        {
                            findings.Add(FindingFactory.AtPosition(
                                tree,
                                filePath,
                                tagName: "value",
                                XmlDocSmells.EmptyValueOnProperty,
                                valueTag.SpanStart,
                                context.FindingContext,
                                snippet: valueTag.ToString(),
                                context.MemberName!));

                            break;
                        }

                    case ValueTargetKind.Indexer:
                        {
                            findings.Add(FindingFactory.AtPosition(
                                tree,
                                filePath,
                                tagName: "value",
                                XmlDocSmells.EmptyValueOnIndexer,
                                valueTag.SpanStart,
                                context.FindingContext,
                                snippet: valueTag.ToString()));

                            break;
                        }
                }
            }
        }

        /// <summary>
        /// Adds duplicate-value findings.
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
                        {
                            findings.Add(FindingFactory.AtPosition(
                                tree,
                                filePath,
                                tagName: "value",
                                XmlDocSmells.DuplicateValueOnProperty,
                                duplicateTag.SpanStart,
                                context.FindingContext,
                                snippet: duplicateTag.ToString(),
                                context.MemberName!));

                            break;
                        }

                    case ValueTargetKind.Indexer:
                        {
                            findings.Add(FindingFactory.AtPosition(
                                tree,
                                filePath,
                                tagName: "value",
                                XmlDocSmells.DuplicateValueOnIndexer,
                                duplicateTag.SpanStart,
                                context.FindingContext,
                                snippet: duplicateTag.ToString()));

                            break;
                        }
                }
            }
        }

        /// <summary>
        /// Adds invalid value-usage findings.
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
                        {
                            findings.Add(FindingFactory.AtPosition(
                                tree,
                                filePath,
                                tagName: "value",
                                XmlDocSmells.ValueOnWriteOnlyProperty,
                                valueTag.SpanStart,
                                context.FindingContext,
                                snippet: valueTag.ToString(),
                                context.MemberName!));

                            break;
                        }

                    case ValueTargetKind.InvalidMember:
                        {
                            findings.Add(FindingFactory.AtPosition(
                                tree,
                                filePath,
                                tagName: "value",
                                XmlDocSmells.ValueOnInvalidMember,
                                valueTag.SpanStart,
                                context.FindingContext,
                                snippet: valueTag.ToString()));

                            break;
                        }
                }
            }
        }

        /// <summary>
        /// Classifies the member for value-tag analysis.
        /// </summary>
        /// <param name="member">The member to classify.</param>
        /// <returns>
        /// The matching value target kind.
        /// </returns>
        private static ValueTargetKind ClassifyMember(MemberDeclarationSyntax member)
        {
            if (member is PropertyDeclarationSyntax property)
            {
                PropertyValueKind propertyKind = ClassifyProperty(property);

                if (propertyKind == PropertyValueKind.Readable)
                {
                    return ValueTargetKind.ReadableProperty;
                }

                if (propertyKind == PropertyValueKind.WriteOnly)
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
        /// Gets the member name used for smell message formatting and target metadata where applicable.
        /// </summary>
        /// <param name="member">The member to inspect.</param>
        /// <returns>
        /// The member name if available; otherwise null.
        /// </returns>
        private static string? GetMemberName(MemberDeclarationSyntax member)
        {
            switch (member)
            {
                case PropertyDeclarationSyntax property:
                    {
                        return property.Identifier.ValueText;
                    }

                case IndexerDeclarationSyntax:
                    {
                        return "this[]";
                    }

                default:
                    {
                        return null;
                    }
            }
        }

        /// <summary>
        /// Classifies the property for value-tag analysis.
        /// </summary>
        /// <param name="property">The property to classify.</param>
        /// <returns>
        /// The matching property value kind.
        /// </returns>
        private static PropertyValueKind ClassifyProperty(PropertyDeclarationSyntax property)
        {
            if (property.ExpressionBody != null)
            {
                return PropertyValueKind.Readable;
            }

            if (property.AccessorList == null)
            {
                return PropertyValueKind.Other;
            }

            bool hasGetter = property.AccessorList.Accessors.Any(
                accessor => accessor.Kind() == SyntaxKind.GetAccessorDeclaration);

            bool hasSetter = property.AccessorList.Accessors.Any(
                accessor => accessor.Kind() == SyntaxKind.SetAccessorDeclaration);

            if (hasGetter)
            {
                return PropertyValueKind.Readable;
            }

            if (hasSetter)
            {
                return PropertyValueKind.WriteOnly;
            }

            return PropertyValueKind.Other;
        }
    }
}
