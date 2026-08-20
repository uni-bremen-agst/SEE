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
    /// Detects exception documentation smells that can be determined without semantic analysis.
    /// </summary>
    /// <remarks>
    /// This detector reports empty exception descriptions, rethrow statements, duplicate exception tags,
    /// and exception tags on members without executable bodies.
    /// Semantic exception mapping is handled by the semantic exception detector.
    /// </remarks>
    internal static class XmlDocExceptionDetector
    {
        /// <summary>
        /// Scans the syntax tree and returns syntax-based exception documentation findings.
        /// </summary>
        /// <param name="tree">The syntax tree to analyze.</param>
        /// <param name="filePath">The file path used for reporting.</param>
        /// <returns>
        /// A list of syntax-based exception documentation findings.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="tree"/> is <see langword="null"/>.
        /// </exception>
        public static List<Finding> FindExceptionSmells(SyntaxTree tree, string filePath)
        {
            List<Finding> findings = new List<Finding>();

            CompilationUnitSyntax root = tree.GetCompilationUnitRoot();

            IEnumerable<MemberDeclarationSyntax> members =
                root.DescendantNodes()
                    .OfType<MemberDeclarationSyntax>();

            foreach (MemberDeclarationSyntax member in members)
            {
                DocumentationCommentTriviaSyntax? doc = XmlDocUtils.TryGetDocComment(member);

                if (doc != null)
                {
                    List<ExtractedXmlDocTag> tags =
                        XmlDocTagExtraction.ExtractTags(doc, "exception", ExtractExceptionCref);

                    AddDuplicateExceptionTagFindings(findings, tree, filePath, member, tags);
                    AddEmptyExceptionDescriptionFindings(findings, tree, filePath, member, tags);
                    AddExceptionTagOnNonExecutableMemberFindings(findings, tree, filePath, member, tags);
                }

                AddRethrowCannotInferExceptionFindings(findings, tree, filePath, member);
            }

            return findings;
        }

        /// <summary>
        /// Extracts the raw cref value from an exception XML element.
        /// </summary>
        /// <param name="element">The exception XML element.</param>
        /// <returns>
        /// The raw cref value if present; otherwise null.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="element"/> is <see langword="null"/>.
        /// </exception>
        private static string? ExtractExceptionCref(XmlElementSyntax element)
        {
            XmlDocTagExtraction.TryGetCrefAttributeValue(element, out string? cref);
            return cref;
        }

        /// <summary>
        /// Adds findings for duplicate exception tags with the same raw cref string.
        /// </summary>
        /// <param name="findings">The finding sink.</param>
        /// <param name="tree">The syntax tree used for reporting.</param>
        /// <param name="filePath">The file path used for reporting.</param>
        /// <param name="member">The documented member.</param>
        /// <param name="tags">The extracted exception tags.</param>
        private static void AddDuplicateExceptionTagFindings(
            List<Finding> findings,
            SyntaxTree tree,
            string filePath,
            MemberDeclarationSyntax member,
            List<ExtractedXmlDocTag> tags)
        {
            Dictionary<string, List<ExtractedXmlDocTag>> grouped =
                new Dictionary<string, List<ExtractedXmlDocTag>>(StringComparer.Ordinal);

            foreach (ExtractedXmlDocTag tag in tags)
            {
                if (string.IsNullOrWhiteSpace(tag.RawAttributeValue))
                {
                    continue;
                }

                if (!grouped.TryGetValue(tag.RawAttributeValue, out List<ExtractedXmlDocTag>? list))
                {
                    list = new List<ExtractedXmlDocTag>();
                    grouped.Add(tag.RawAttributeValue, list);
                }

                list.Add(tag);
            }

            foreach (KeyValuePair<string, List<ExtractedXmlDocTag>> pair in grouped)
            {
                string rawCref = pair.Key;
                List<ExtractedXmlDocTag> occurrences = pair.Value;

                if (occurrences.Count <= 1)
                {
                    continue;
                }

                for (int i = 1; i < occurrences.Count; i++)
                {
                    ExtractedXmlDocTag duplicate = occurrences[i];

                    findings.Add(FindingFactory.AtPosition(
                        tree,
                        filePath,
                        tagName: "exception",
                        XmlDocSmells.DuplicateExceptionTag,
                        duplicate.Element.SpanStart,
                        CreateExceptionTagContext(member, rawCref, filePath),
                        snippet: SyntaxUtils.GetSnippet(duplicate.Element),
                        rawCref));
                }
            }
        }

        /// <summary>
        /// Adds findings for exception tags with empty descriptions.
        /// </summary>
        /// <param name="findings">The finding sink.</param>
        /// <param name="tree">The syntax tree used for reporting.</param>
        /// <param name="filePath">The file path used for reporting.</param>
        /// <param name="member">The documented member.</param>
        /// <param name="tags">The extracted exception tags.</param>
        private static void AddEmptyExceptionDescriptionFindings(
            List<Finding> findings,
            SyntaxTree tree,
            string filePath,
            MemberDeclarationSyntax member,
            List<ExtractedXmlDocTag> tags)
        {
            foreach (ExtractedXmlDocTag tag in tags)
            {
                if (string.IsNullOrWhiteSpace(tag.RawAttributeValue))
                {
                    continue;
                }

                if (!XmlDocUtils.HasMeaningfulContent(tag.Element))
                {
                    findings.Add(FindingFactory.AtPosition(
                        tree,
                        filePath,
                        tagName: "exception",
                        XmlDocSmells.EmptyExceptionDescription,
                        tag.Element.SpanStart,
                        CreateExceptionTagContext(member, tag.RawAttributeValue, filePath),
                        snippet: SyntaxUtils.GetSnippet(tag.Element),
                        tag.RawAttributeValue));
                }
            }
        }

        /// <summary>
        /// Adds findings for exception tags on members without executable bodies.
        /// </summary>
        /// <param name="findings">The finding sink.</param>
        /// <param name="tree">The syntax tree used for reporting.</param>
        /// <param name="filePath">The file path used for reporting.</param>
        /// <param name="member">The documented member.</param>
        /// <param name="tags">The extracted exception tags.</param>
        private static void AddExceptionTagOnNonExecutableMemberFindings(
            List<Finding> findings,
            SyntaxTree tree,
            string filePath,
            MemberDeclarationSyntax member,
            List<ExtractedXmlDocTag> tags)
        {
            if (tags.Count == 0)
            {
                return;
            }

            if (!IsNonExecutableExceptionTarget(member))
            {
                return;
            }

            foreach (ExtractedXmlDocTag tag in tags)
            {
                findings.Add(FindingFactory.AtPosition(
                    tree,
                    filePath,
                    tagName: "exception",
                    XmlDocSmells.ExceptionTagOnNonExecutableMember,
                    tag.Element.SpanStart,
                    CreateExceptionTagContext(member, tag.RawAttributeValue, filePath),
                    snippet: SyntaxUtils.GetSnippet(tag.Element)));
            }
        }

        /// <summary>
        /// Adds findings for rethrow statements whose exception type cannot be inferred syntactically.
        /// </summary>
        /// <param name="findings">The finding sink.</param>
        /// <param name="tree">The syntax tree used for reporting.</param>
        /// <param name="filePath">The file path used for reporting.</param>
        /// <param name="member">The member to inspect.</param>
        private static void AddRethrowCannotInferExceptionFindings(
            List<Finding> findings,
            SyntaxTree tree,
            string filePath,
            MemberDeclarationSyntax member)
        {
            if (!SyntaxUtils.TryGetMemberBody(member, out SyntaxNode? bodyNode))
            {
                return;
            }

            if (bodyNode == null)
            {
                return;
            }

            if (!SyntaxUtils.ContainsRethrow(bodyNode, out int rethrowAnchor))
            {
                return;
            }

            findings.Add(FindingFactory.AtPosition(
                tree,
                filePath,
                tagName: "exception",
                XmlDocSmells.RethrowCannotInferException,
                rethrowAnchor,
                FindingContextBuilder.ForDeclaration(
                    member,
                    "ExceptionFlow",
                    targetName: "throw;",
                    filePath: filePath),
                snippet: "throw;"));
        }

        /// <summary>
        /// Determines whether the documented member must not use exception documentation
        /// because it has no executable implementation of its own.
        /// </summary>
        /// <param name="member">The member to inspect.</param>
        /// <returns>
        /// True if the member has no executable implementation; otherwise false.
        /// </returns>
        private static bool IsNonExecutableExceptionTarget(MemberDeclarationSyntax member)
        {
            return SyntaxUtils.IsAbstractMember(member)
                || SyntaxUtils.IsExternMember(member)
                || !SyntaxUtils.HasExecutableBody(member);
        }

        /// <summary>
        /// Creates finding context metadata for an exception tag.
        /// </summary>
        /// <param name="member">The member that owns the exception documentation.</param>
        /// <param name="rawCref">The raw cref value of the exception tag.</param>
        /// <param name="filePath">The file path used for reporting.</param>
        /// <returns>
        /// A populated finding context for an exception tag.
        /// </returns>
        private static FindingContext CreateExceptionTagContext(
            MemberDeclarationSyntax member,
            string? rawCref,
            string filePath)
        {
            return FindingContextBuilder.ForDeclaration(
                member,
                "ExceptionTag",
                targetName: CreateExceptionTargetName(rawCref),
                filePath: filePath);
        }

        /// <summary>
        /// Creates a stable target name for an exception cref.
        /// </summary>
        /// <param name="rawCref">The raw cref value of the exception tag.</param>
        /// <returns>
        /// A stable target name if a cref value exists; otherwise null.
        /// </returns>
        private static string? CreateExceptionTargetName(string? rawCref)
        {
            if (string.IsNullOrWhiteSpace(rawCref))
            {
                return null;
            }

            return "cref:" + rawCref;
        }
    }
}
