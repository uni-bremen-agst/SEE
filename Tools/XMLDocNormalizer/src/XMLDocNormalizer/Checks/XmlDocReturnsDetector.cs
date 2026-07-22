using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using XMLDocNormalizer.Checks.Infrastructure;
using XMLDocNormalizer.Models;
using XMLDocNormalizer.Utils;
using XMLDocNormalizer.Utils.Extensions;

namespace XMLDocNormalizer.Checks
{
    /// <summary>
    /// Detects XML documentation smells related to returns tags.
    /// </summary>
    /// <remarks>
    /// This detector reports missing returns tags, empty returns descriptions, returns tags on void-like members,
    /// duplicate returns tags, and invalid returns tags on write-only properties or indexers.
    /// The analysis is syntax-based and does not require semantic model access.
    /// </remarks>
    internal static class XmlDocReturnsDetector
    {
        /// <summary>
        /// Scans the syntax tree and returns findings for returns documentation smells.
        /// </summary>
        /// <param name="tree">The syntax tree to analyze.</param>
        /// <param name="filePath">The file path used for reporting.</param>
        /// <returns>
        /// A list of findings produced by the returns documentation detector.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when a smell definition required to create a returns-related
        /// finding is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="filePath"/> is <see langword="null"/>,
        /// empty, or consists only of white-space characters and a finding is
        /// created.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when a source position derived from a member declaration or
        /// returns tag does not identify a valid position in
        /// <paramref name="tree"/> and a finding is created.
        /// </exception>
        public static List<Finding> FindReturnsSmells(SyntaxTree tree, string filePath)
        {
            List<Finding> findings = new List<Finding>();

            CompilationUnitSyntax root = tree.GetCompilationUnitRoot();

            IEnumerable<MemberDeclarationSyntax> members =
                root.DescendantNodes()
                    .OfType<MemberDeclarationSyntax>();

            foreach (MemberDeclarationSyntax member in members)
            {
                if (!SupportsReturnsAnalysis(member))
                {
                    continue;
                }

                DocumentationCommentTriviaSyntax? doc = XmlDocUtils.TryGetDocComment(member);

                if (doc == null)
                {
                    continue;
                }

                List<XmlElementSyntax> returnsTags = XmlDocElementQuery.ElementsByName(doc, "returns").ToList();

                if (member is PropertyDeclarationSyntax propertyDeclaration)
                {
                    AddReturnsOnWriteOnlyPropertyFinding(
                        findings,
                        tree,
                        filePath,
                        propertyDeclaration,
                        returnsTags);

                    continue;
                }

                if (member is IndexerDeclarationSyntax indexerDeclaration)
                {
                    AddReturnsOnIndexerFinding(
                        findings,
                        tree,
                        filePath,
                        indexerDeclaration,
                        returnsTags);

                    continue;
                }

                FindingContext context = FindingContextBuilder.ForDeclaration(
                    member,
                    "ReturnValue",
                    filePath: filePath);

                bool isVoid = IsVoidLike(member);

                if (returnsTags.Count > 1)
                {
                    for (int i = 1; i < returnsTags.Count; i++)
                    {
                        XmlElementSyntax element = returnsTags[i];

                        findings.Add(FindingFactory.AtPosition(
                            tree,
                            filePath,
                            tagName: "returns",
                            XmlDocSmells.DuplicateReturnsTag,
                            element.SpanStart,
                            context,
                            snippet: SyntaxUtils.GetSnippet(element)));
                    }
                }

                if (isVoid)
                {
                    if (returnsTags.Count > 0)
                    {
                        XmlElementSyntax first = returnsTags[0];

                        findings.Add(FindingFactory.AtPosition(
                            tree,
                            filePath,
                            tagName: "returns",
                            XmlDocSmells.ReturnsOnVoidMember,
                            first.SpanStart,
                            context,
                            snippet: SyntaxUtils.GetSnippet(first)));
                    }

                    continue;
                }

                if (returnsTags.Count == 0)
                {
                    if (!doc.HasInheritdoc())
                    {
                        findings.Add(FindingFactory.AtPosition(
                            tree,
                            filePath,
                            tagName: "returns",
                            XmlDocSmells.MissingReturns,
                            MemberAnchorResolver.GetAnchorPosition(member),
                            context,
                            snippet: string.Empty));
                    }

                    continue;
                }

                XmlElementSyntax returnsElement = returnsTags[0];

                if (!XmlDocUtils.HasMeaningfulContent(returnsElement))
                {
                    findings.Add(FindingFactory.AtPosition(
                        tree,
                        filePath,
                        tagName: "returns",
                        XmlDocSmells.EmptyReturns,
                        returnsElement.SpanStart,
                        context,
                        snippet: SyntaxUtils.GetSnippet(returnsElement)));
                }
            }

            return findings;
        }

        /// <summary>
        /// Adds a finding when a write-only property contains returns documentation.
        /// </summary>
        /// <param name="findings">The collection to which findings will be added.</param>
        /// <param name="tree">The syntax tree containing the property declaration.</param>
        /// <param name="filePath">The file path used for reporting.</param>
        /// <param name="propertyDeclaration">The property declaration to inspect.</param>
        /// <param name="returnsTags">The returns tags found on the property.</param>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="filePath"/> is <see langword="null"/>,
        /// empty, or consists only of white-space characters and the write-only
        /// property contains returns documentation.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when the source position of the first returns tag does not
        /// identify a valid position in <paramref name="tree"/>.
        /// </exception>
        private static void AddReturnsOnWriteOnlyPropertyFinding(
            List<Finding> findings,
            SyntaxTree tree,
            string filePath,
            PropertyDeclarationSyntax propertyDeclaration,
            IReadOnlyList<XmlElementSyntax> returnsTags)
        {
            if (returnsTags.Count == 0)
            {
                return;
            }

            if (!IsWriteOnlyProperty(propertyDeclaration))
            {
                return;
            }

            string propertyName = propertyDeclaration.Identifier.ValueText;
            XmlElementSyntax firstReturnsTag = returnsTags[0];

            findings.Add(FindingFactory.AtPosition(
                tree,
                filePath,
                tagName: "returns",
                XmlDocSmells.ReturnsOnWriteOnlyProperty,
                firstReturnsTag.SpanStart,
                FindingContextBuilder.ForDeclaration(
                    propertyDeclaration,
                    "ReturnValue",
                    targetName: propertyName,
                    filePath: filePath),
                snippet: SyntaxUtils.GetSnippet(firstReturnsTag),
                propertyName));
        }

        /// <summary>
        /// Adds a finding when an indexer contains returns documentation.
        /// </summary>
        /// <param name="findings">The collection to which findings will be added.</param>
        /// <param name="tree">The syntax tree containing the indexer declaration.</param>
        /// <param name="filePath">The file path used for reporting.</param>
        /// <param name="indexerDeclaration">The indexer declaration to inspect.</param>
        /// <param name="returnsTags">The returns tags found on the indexer.</param>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="filePath"/> is <see langword="null"/>,
        /// empty, or consists only of white-space characters and the indexer
        /// contains returns documentation.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when the source position of the first returns tag does not
        /// identify a valid position in <paramref name="tree"/>.
        /// </exception>
        private static void AddReturnsOnIndexerFinding(
            List<Finding> findings,
            SyntaxTree tree,
            string filePath,
            IndexerDeclarationSyntax indexerDeclaration,
            IReadOnlyList<XmlElementSyntax> returnsTags)
        {
            if (returnsTags.Count == 0)
            {
                return;
            }

            string indexerName = "this[]";
            XmlElementSyntax firstReturnsTag = returnsTags[0];

            findings.Add(FindingFactory.AtPosition(
                tree,
                filePath,
                tagName: "returns",
                XmlDocSmells.ReturnsOnIndexer,
                firstReturnsTag.SpanStart,
                FindingContextBuilder.ForDeclaration(
                    indexerDeclaration,
                    "ReturnValue",
                    targetName: indexerName,
                    filePath: filePath),
                snippet: SyntaxUtils.GetSnippet(firstReturnsTag),
                indexerName));
        }

        /// <summary>
        /// Determines whether returns analysis applies to the given member.
        /// </summary>
        /// <param name="member">The member to inspect.</param>
        /// <returns>
        /// True if returns analysis applies; otherwise false.
        /// </returns>
        private static bool SupportsReturnsAnalysis(MemberDeclarationSyntax member)
        {
            if (member is MethodDeclarationSyntax)
            {
                return true;
            }

            if (member is DelegateDeclarationSyntax)
            {
                return true;
            }

            if (member is OperatorDeclarationSyntax)
            {
                return true;
            }

            if (member is ConversionOperatorDeclarationSyntax)
            {
                return true;
            }

            if (member is PropertyDeclarationSyntax)
            {
                return true;
            }

            if (member is IndexerDeclarationSyntax)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Determines whether the given property is write-only.
        /// </summary>
        /// <param name="propertyDeclaration">The property declaration to inspect.</param>
        /// <returns>
        /// True if the property has a setter but no getter; otherwise false.
        /// </returns>
        private static bool IsWriteOnlyProperty(PropertyDeclarationSyntax propertyDeclaration)
        {
            if (propertyDeclaration.ExpressionBody != null)
            {
                return false;
            }

            if (propertyDeclaration.AccessorList == null)
            {
                return false;
            }

            bool hasGetter = propertyDeclaration.AccessorList.Accessors.Any(
                static accessor => accessor.Kind() == SyntaxKind.GetAccessorDeclaration);

            bool hasSetter = propertyDeclaration.AccessorList.Accessors.Any(
                static accessor => accessor.Kind() == SyntaxKind.SetAccessorDeclaration);

            return hasSetter && !hasGetter;
        }

        /// <summary>
        /// Determines whether the given member is considered void-like for returns checks.
        /// </summary>
        /// <param name="member">The member to inspect.</param>
        /// <returns>
        /// True if the member is void-like; otherwise false.
        /// </returns>
        private static bool IsVoidLike(MemberDeclarationSyntax member)
        {
            if (member is MethodDeclarationSyntax methodDeclaration)
            {
                if (methodDeclaration.ReturnType is PredefinedTypeSyntax predefinedReturnType)
                {
                    return predefinedReturnType.Keyword.IsKind(SyntaxKind.VoidKeyword);
                }

                return false;
            }

            if (member is OperatorDeclarationSyntax operatorDeclaration)
            {
                if (operatorDeclaration.ReturnType is PredefinedTypeSyntax predefinedReturnType)
                {
                    return predefinedReturnType.Keyword.IsKind(SyntaxKind.VoidKeyword);
                }

                return false;
            }

            if (member is ConversionOperatorDeclarationSyntax)
            {
                return false;
            }

            if (member is DelegateDeclarationSyntax delegateDeclaration)
            {
                if (delegateDeclaration.ReturnType is PredefinedTypeSyntax predefinedReturnType)
                {
                    return predefinedReturnType.Keyword.IsKind(SyntaxKind.VoidKeyword);
                }

                return false;
            }

            return false;
        }
    }
}
