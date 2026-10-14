using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using XMLDocNormalizer.Checks.Infrastructure.Exception;
using XMLDocNormalizer.Execution.Analysis;
using XMLDocNormalizer.Models;
using XMLDocNormalizer.Utils;

namespace XMLDocNormalizer.Checks
{
    internal static partial class XmlDocExceptionSemanticDetector
    {
        // Main-only adapters supply trusted identity/ownership; this is the existing
        // detector policy, independent of the compiler's symbol representation.
        private sealed record FindingTag<T>(string? Raw, bool HasCref, bool Resolved,
            T? Type, DeclaredFindingAnchor Anchor, string Snippet) where T : class;

        private static bool RequiresSummary(ExceptionAnalysisMode mode, bool hasDeclaredTypes)
            => mode != ExceptionAnalysisMode.Direct
                && (mode != ExceptionAnalysisMode.ProjectTransitiveDeclaredExceptions || hasDeclaredTypes);

        private static DeclaredFindingAnchor FindingAnchor(SyntaxTree tree, string filePath, int position, FindingContext context)
        {
            var line = tree.GetLineSpan(new TextSpan(position, 1)).StartLinePosition;
            return new(filePath, position, line.Line + 1, line.Character + 1, context);
        }

        private static FindingTag<INamedTypeSymbol>[] NativeTags(SyntaxTree tree, string filePath, List<ExceptionTagSemanticInfo> tags)
            => tags.Select(info => new FindingTag<INamedTypeSymbol>(info.Tag.RawAttributeValue,
                info.CrefAttribute?.Cref != null, info.ResolvedSymbol != null, info.ResolvedTypeSymbol,
                FindingAnchor(tree, filePath, info.CrefAttribute?.SpanStart ?? info.Tag.Element.SpanStart, info.FindingContext),
                SyntaxUtils.GetSnippet(info.Tag.Element))).ToArray();

        private static void AddFinding(List<Finding> findings, DeclaredFindingAnchor anchor, XmlDocSmell smell,
            string snippet, params object[] arguments)
            => findings.Add(new(smell, anchor.LogicalPath, "exception", anchor.Line, anchor.Column, snippet, anchor.Context, arguments));

        private static void AddExceptionFindings<T>(List<Finding> findings, FindingTag<T>[] tags,
            Func<DeclaredFindingAnchor> member, IEnumerable<T> direct, IEnumerable<T> flow,
            IEqualityComparer<T> equality, Func<T, bool> isException, Func<T, bool> isLocal,
            Func<T, string> display, ExceptionAnalysisMode mode, bool inheritdoc,
            bool uncertain, HashSet<string> uncertainTargets, IEnumerable<T> externalDocumentation) where T : class
        {
            bool Valid(FindingTag<T> tag) => !string.IsNullOrWhiteSpace(tag.Raw) && tag.HasCref;
            bool Exception(FindingTag<T> tag) => Valid(tag) && tag.Type != null && isException(tag.Type);
            bool Relevant(T type) => isException(type)
                && (mode != ExceptionAnalysisMode.ProjectTransitiveDeclaredExceptions || isLocal(type));
            var thrown = new HashSet<T>(flow, equality);
            var external = new HashSet<T>(externalDocumentation, equality);
            var documented = new HashSet<T>(tags.Where(Exception).Select(tag => tag.Type!), equality);
            var relevantDocumented = new HashSet<T>(documented.Where(Relevant), equality);
            foreach (var tag in tags.Where(tag => Valid(tag) && !tag.Resolved))
            { AddFinding(findings, tag.Anchor, XmlDocSmells.InvalidExceptionCref, tag.Snippet, tag.Raw!); }
            foreach (var tag in tags.Where(tag => Valid(tag) && tag.Type != null && !isException(tag.Type)))
            { AddFinding(findings, tag.Anchor, XmlDocSmells.ExceptionCrefNotExceptionType, tag.Snippet, tag.Raw!); }
            if (mode != ExceptionAnalysisMode.Direct)
            {
                AddDocumentedTransitiveFindings(findings, tags, thrown, external, Relevant, uncertain, uncertainTargets);
                if (!inheritdoc)
                {
                    AddMissingExceptionFindings(findings, member, direct, [], documented, equality, isException, display, transitive: false);
                    AddMissingExceptionFindings(findings, member, flow, direct, relevantDocumented, equality, Relevant, display, transitive: true);
                }
            }
            else
            {
                foreach (var tag in tags.Where(tag => Exception(tag) && !thrown.Contains(tag.Type!)))
                { AddFinding(findings, tag.Anchor, XmlDocSmells.ExceptionTagWithoutDirectThrow, tag.Snippet, tag.Raw!); }
                if (!inheritdoc)
                { AddMissingExceptionFindings(findings, member, flow, [], documented, equality, isException, display, transitive: false); }
            }
        }

        private static void AddDocumentedTransitiveFindings<T>(List<Finding> findings, FindingTag<T>[] tags,
            HashSet<T> thrown, HashSet<T> external, Func<T, bool> relevant, bool uncertain,
            HashSet<string> uncertainTargets) where T : class
        {
            string summary = SummarizeUncertainTargets(uncertainTargets, 3);
            foreach (var tag in tags.Where(tag => !string.IsNullOrWhiteSpace(tag.Raw) && tag.HasCref
                && tag.Type != null && relevant(tag.Type) && !thrown.Contains(tag.Type)))
            {
                if (uncertain)
                {
                    string detail = summary;
                    if (external.Contains(tag.Type!))
                    { detail += ". External XML documentation lists this exception as a possible throw."; }
                    AddFinding(findings, tag.Anchor, XmlDocSmells.ExceptionFlowNotDecidable, tag.Snippet, tag.Raw!, detail);
                }
                else { AddFinding(findings, tag.Anchor, XmlDocSmells.ExceptionTagWithoutTransitiveThrow, tag.Snippet, tag.Raw!); }
            }
        }

        private static void AddMissingExceptionFindings<T>(List<Finding> findings, Func<DeclaredFindingAnchor> member,
            IEnumerable<T> thrown, IEnumerable<T> direct,
            HashSet<T> documented, IEqualityComparer<T> equality, Func<T, bool> relevant,
            Func<T, string> display, bool transitive) where T : class
        {
            var directlyThrown = new HashSet<T>(direct, equality);
            foreach (var type in thrown)
            {
                if (directlyThrown.Contains(type) || !relevant(type) || documented.Contains(type)) { continue; }
                string name = display(type);
                var anchor = member();
                AddFinding(findings, anchor with { Context = anchor.Context with { TargetName = name } },
                    transitive ? XmlDocSmells.MissingTransitiveExceptionDocumentation : XmlDocSmells.MissingExceptionTag,
                    string.Empty, name);
            }
        }
    }
}
