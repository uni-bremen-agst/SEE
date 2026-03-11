using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using XMLDocNormalizer.Checks.Infrastructure;
using XMLDocNormalizer.Checks.Infrastructure.Exception;
using XMLDocNormalizer.Execution.Semantic;
using XMLDocNormalizer.Models;
using XMLDocNormalizer.Models.Dto;
using XMLDocNormalizer.Models.DTO;
using XMLDocNormalizer.Utils;

namespace XMLDocNormalizer.Checks
{
    /// <summary>
    /// Detects exception documentation smells that require semantic analysis:
    /// <list type="bullet">
    /// <item><description>DOC610: An exception is thrown directly or transitively but is not documented with an <exception> tag.</description></item>
    /// <item><description>DOC630: An <exception> tag documents an exception that is never thrown directly or transitively.</description></item>
    /// <item><description>DOC631: Exception flow cannot be decided completely, therefore DOC630 is suppressed.</description></item>
    /// <item><description>DOC660: An <exception> cref cannot be resolved to a type.</description></item>
    /// <item><description>DOC670: An <exception> cref resolves to a symbol that is not an exception type.</description></item>
    /// </list>
    /// </summary>
    internal static class XmlDocExceptionSemanticDetector
    {
        /// <summary>
        /// Scans the syntax tree and returns exception-related findings that require semantic analysis
        /// (DOC610/DOC630/DOC631/DOC660/DOC670).
        /// </summary>
        /// <param name="tree">The syntax tree to analyze.</param>
        /// <param name="filePath">The file path used for reporting.</param>
        /// <param name="semanticModel">The semantic model for the syntax tree.</param>
        /// <returns>A list of findings.</returns>
        public static List<Finding> FindExceptionSmells(
            SyntaxTree tree,
            string filePath,
            SemanticModel semanticModel)
        {
            ProjectClosureSemanticContext semanticContext =
                ProjectClosureSemanticContext.CreateSingleCompilationContext(
                    tree,
                    semanticModel.Compilation);

            return FindExceptionSmells(tree, filePath, semanticModel, semanticContext);
        }

        /// <summary>
        /// Scans the syntax tree and returns exception-related findings that require semantic analysis
        /// (DOC610/DOC630/DOC631/DOC660/DOC670).
        /// </summary>
        /// <param name="tree">The syntax tree to analyze.</param>
        /// <param name="filePath">The file path used for reporting.</param>
        /// <param name="semanticModel">The semantic model for the syntax tree.</param>
        /// <param name="semanticContext">The project-closure semantic context.</param>
        /// <returns>A list of findings.</returns>
        public static List<Finding> FindExceptionSmells(
            SyntaxTree tree,
            string filePath,
            SemanticModel semanticModel,
            ProjectClosureSemanticContext semanticContext)
        {
            List<Finding> findings = new();

            INamedTypeSymbol? exceptionBase =
                semanticModel.Compilation.GetTypeByMetadataName("System.Exception");

            if (exceptionBase == null)
            {
                return findings;
            }

            CompilationUnitSyntax root = tree.GetCompilationUnitRoot();

            IEnumerable<MemberDeclarationSyntax> members =
                root.DescendantNodes()
                    .OfType<MemberDeclarationSyntax>();

            foreach (MemberDeclarationSyntax member in members)
            {
                DocumentationCommentTriviaSyntax? doc = XmlDocUtils.TryGetDocComment(member);
                if (doc == null)
                {
                    continue;
                }

                List<ExtractedXmlDocTag> tags =
                    XmlDocTagExtraction.ExtractTags(doc, "exception", ExtractExceptionCref);

                List<ExceptionTagSemanticInfo> tagInfos =
                    BuildTagInfos(tags, semanticModel);

                ExceptionFlowAnalysisResult flowResult =
                    ExceptionFlowAnalyzer.AnalyzeTransitivelyThrownExceptions(member, semanticContext);

                AddInvalidExceptionCrefFindings(
                    findings,
                    tree,
                    filePath,
                    tagInfos);

                AddExceptionCrefNotExceptionTypeFindings(
                    findings,
                    tree,
                    filePath,
                    tagInfos,
                    exceptionBase);

                AddExceptionFlowNotDecidableFinding(
                    findings,
                    tree,
                    filePath,
                    tagInfos,
                    exceptionBase,
                    flowResult);

                AddExceptionTagWithoutThrowFindings(
                    findings,
                    tree,
                    filePath,
                    tagInfos,
                    exceptionBase,
                    flowResult);

                AddMissingExceptionTagFindings(
                    findings,
                    tree,
                    filePath,
                    member,
                    tagInfos,
                    exceptionBase,
                    flowResult);
            }

            return findings;
        }

        /// <summary>
        /// Extracts the raw cref value from an <exception> XML element.
        /// </summary>
        /// <param name="element">The exception XML element.</param>
        /// <returns>The raw cref value if present; otherwise null.</returns>
        private static string? ExtractExceptionCref(XmlElementSyntax element)
        {
            XmlDocTagExtraction.TryGetCrefAttributeValue(element, out string? cref);
            return cref;
        }

        /// <summary>
        /// Builds semantic information for all extracted exception tags.
        /// </summary>
        /// <param name="tags">The extracted exception tags.</param>
        /// <param name="semanticModel">The semantic model used for symbol resolution.</param>
        /// <returns>A list containing one semantic info object per extracted tag.</returns>
        private static List<ExceptionTagSemanticInfo> BuildTagInfos(
            List<ExtractedXmlDocTag> tags,
            SemanticModel semanticModel)
        {
            List<ExceptionTagSemanticInfo> infos = new();

            foreach (ExtractedXmlDocTag tag in tags)
            {
                XmlCrefAttributeSyntax? crefAttribute =
                    SyntaxUtils.GetAttribute<XmlCrefAttributeSyntax>(tag.Element, "cref");

                ISymbol? resolvedSymbol = null;

                if (crefAttribute?.Cref != null)
                {
                    resolvedSymbol = semanticModel.GetSymbolInfo(crefAttribute.Cref).Symbol;
                }

                infos.Add(new ExceptionTagSemanticInfo
                {
                    Tag = tag,
                    CrefAttribute = crefAttribute,
                    ResolvedSymbol = resolvedSymbol
                });
            }

            return infos;
        }

        /// <summary>
        /// Adds DOC660 findings for exception tags whose cref cannot be resolved to a known type.
        /// </summary>
        private static void AddInvalidExceptionCrefFindings(
            List<Finding> findings,
            SyntaxTree tree,
            string filePath,
            List<ExceptionTagSemanticInfo> tagInfos)
        {
            foreach (ExceptionTagSemanticInfo info in tagInfos)
            {
                if (string.IsNullOrWhiteSpace(info.Tag.RawAttributeValue))
                {
                    continue;
                }

                if (info.CrefAttribute == null || info.CrefAttribute.Cref == null)
                {
                    continue;
                }

                if (info.ResolvedSymbol != null)
                {
                    continue;
                }

                findings.Add(FindingFactory.AtPosition(
                    tree,
                    filePath,
                    tagName: "exception",
                    XmlDocSmells.InvalidExceptionCref,
                    info.CrefAttribute.SpanStart,
                    snippet: SyntaxUtils.GetSnippet(info.Tag.Element),
                    info.Tag.RawAttributeValue));
            }
        }

        /// <summary>
        /// Adds DOC670 findings for exception tags whose cref resolves
        /// to a symbol that is not an exception type.
        /// </summary>
        private static void AddExceptionCrefNotExceptionTypeFindings(
            List<Finding> findings,
            SyntaxTree tree,
            string filePath,
            List<ExceptionTagSemanticInfo> tagInfos,
            INamedTypeSymbol exceptionBase)
        {
            foreach (ExceptionTagSemanticInfo info in tagInfos)
            {
                if (string.IsNullOrWhiteSpace(info.Tag.RawAttributeValue))
                {
                    continue;
                }

                if (info.CrefAttribute == null || info.CrefAttribute.Cref == null)
                {
                    continue;
                }

                if (info.ResolvedTypeSymbol == null)
                {
                    continue;
                }

                if (!info.ResolvedTypeSymbol.InheritsFromOrEquals(exceptionBase))
                {
                    findings.Add(FindingFactory.AtPosition(
                        tree,
                        filePath,
                        tagName: "exception",
                        XmlDocSmells.ExceptionCrefNotExceptionType,
                        info.CrefAttribute.SpanStart,
                        snippet: SyntaxUtils.GetSnippet(info.Tag.Element),
                        info.Tag.RawAttributeValue));
                }
            }
        }

        /// <summary>
        /// Adds DOC631 for documented exception tags whose flow could not be decided completely
        /// and that are not already covered by proven thrown exceptions.
        /// </summary>
        /// <param name="findings">The finding sink.</param>
        /// <param name="tree">The syntax tree.</param>
        /// <param name="filePath">The file path.</param>
        /// <param name="tagInfos">The prepared semantic tag information.</param>
        /// <param name="exceptionBase">The System.Exception base symbol.</param>
        /// <param name="flowResult">The transitive exception-flow analysis result.</param>
        private static void AddExceptionFlowNotDecidableFinding(
            List<Finding> findings,
            SyntaxTree tree,
            string filePath,
            List<ExceptionTagSemanticInfo> tagInfos,
            INamedTypeSymbol exceptionBase,
            ExceptionFlowAnalysisResult flowResult)
        {
            if (!flowResult.HasUncertainPaths)
            {
                return;
            }

            string summary = SummarizeUncertainTargets(flowResult.UncertainTargets, 3);

            foreach (ExceptionTagSemanticInfo info in tagInfos)
            {
                if (string.IsNullOrWhiteSpace(info.Tag.RawAttributeValue))
                {
                    continue;
                }

                if (info.CrefAttribute == null || info.CrefAttribute.Cref == null)
                {
                    continue;
                }

                if (info.ResolvedTypeSymbol == null)
                {
                    continue;
                }

                if (!info.ResolvedTypeSymbol.InheritsFromOrEquals(exceptionBase))
                {
                    continue;
                }

                if (IsDocumentedExceptionCoveredByThrownTypes(
                    flowResult.ThrownExceptions,
                    info.ResolvedTypeSymbol))
                {
                    continue;
                }

                findings.Add(FindingFactory.AtPosition(
                    tree,
                    filePath,
                    tagName: "exception",
                    XmlDocSmells.ExceptionFlowNotDecidable,
                    info.CrefAttribute.SpanStart,
                    snippet: SyntaxUtils.GetSnippet(info.Tag.Element),
                    info.Tag.RawAttributeValue,
                    summary));
            }
        }

        /// <summary>
        /// Adds DOC630 findings for documented exceptions that are not thrown
        /// directly or transitively by the documented member.
        /// </summary>
        private static void AddExceptionTagWithoutThrowFindings(
            List<Finding> findings,
            SyntaxTree tree,
            string filePath,
            List<ExceptionTagSemanticInfo> tagInfos,
            INamedTypeSymbol exceptionBase,
            ExceptionFlowAnalysisResult flowResult)
        {
            if (flowResult.HasUncertainPaths)
            {
                return;
            }

            foreach (ExceptionTagSemanticInfo info in tagInfos)
            {
                if (string.IsNullOrWhiteSpace(info.Tag.RawAttributeValue))
                {
                    continue;
                }

                if (info.CrefAttribute == null || info.CrefAttribute.Cref == null)
                {
                    continue;
                }

                if (info.ResolvedTypeSymbol == null)
                {
                    continue;
                }

                if (!info.ResolvedTypeSymbol.InheritsFromOrEquals(exceptionBase))
                {
                    continue;
                }

                if (IsDocumentedExceptionCoveredByThrownTypes(flowResult.ThrownExceptions, info.ResolvedTypeSymbol))
                {
                    continue;
                }

                findings.Add(FindingFactory.AtPosition(
                    tree,
                    filePath,
                    tagName: "exception",
                    XmlDocSmells.ExceptionTagWithoutDirectThrow,
                    info.CrefAttribute.SpanStart,
                    snippet: SyntaxUtils.GetSnippet(info.Tag.Element),
                    info.Tag.RawAttributeValue));
            }
        }

        /// <summary>
        /// Determines whether the documented exception type is covered by one of the thrown
        /// exception types.
        /// </summary>
        private static bool IsDocumentedExceptionCoveredByThrownTypes(
            HashSet<INamedTypeSymbol> thrownExceptions,
            INamedTypeSymbol documentedType)
        {
            foreach (INamedTypeSymbol thrownType in thrownExceptions)
            {
                if (thrownType.InheritsFromOrEquals(documentedType))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Adds DOC610 findings for exceptions that are thrown directly or transitively
        /// by the documented member but are not covered by any <exception> tag.
        /// </summary>
        private static void AddMissingExceptionTagFindings(
            List<Finding> findings,
            SyntaxTree tree,
            string filePath,
            MemberDeclarationSyntax member,
            List<ExceptionTagSemanticInfo> tagInfos,
            INamedTypeSymbol exceptionBase,
            ExceptionFlowAnalysisResult flowResult)
        {
            HashSet<INamedTypeSymbol> documentedExceptions =
                CollectDocumentedExceptionTypes(tagInfos, exceptionBase);

            foreach (INamedTypeSymbol thrownType in flowResult.ThrownExceptions)
            {
                if (!thrownType.InheritsFromOrEquals(exceptionBase))
                {
                    continue;
                }

                if (IsThrownExceptionCoveredByDocumentedTypes(documentedExceptions, thrownType))
                {
                    continue;
                }

                findings.Add(FindingFactory.AtPosition(
                    tree,
                    filePath,
                    tagName: "exception",
                    XmlDocSmells.MissingExceptionTag,
                    MemberAnchorResolver.GetAnchorPosition(member),
                    snippet: string.Empty,
                    thrownType.ToDisplayString()));
            }
        }

        /// <summary>
        /// Collects all documented exception types that are valid exception types.
        /// Invalid cref values and non-exception cref targets are ignored because they are
        /// handled by DOC660 and DOC670.
        /// </summary>
        private static HashSet<INamedTypeSymbol> CollectDocumentedExceptionTypes(
            List<ExceptionTagSemanticInfo> tagInfos,
            INamedTypeSymbol exceptionBase)
        {
            HashSet<INamedTypeSymbol> documented =
                new(SymbolEqualityComparer.Default);

            foreach (ExceptionTagSemanticInfo info in tagInfos)
            {
                if (info.ResolvedTypeSymbol == null)
                {
                    continue;
                }

                if (!info.ResolvedTypeSymbol.InheritsFromOrEquals(exceptionBase))
                {
                    continue;
                }

                documented.Add(info.ResolvedTypeSymbol);
            }

            return documented;
        }

        /// <summary>
        /// Determines whether the thrown exception type is covered by one of the documented
        /// exception types.
        /// </summary>
        private static bool IsThrownExceptionCoveredByDocumentedTypes(
            HashSet<INamedTypeSymbol> documentedExceptions,
            INamedTypeSymbol thrownType)
        {
            foreach (INamedTypeSymbol documentedType in documentedExceptions)
            {
                if (thrownType.InheritsFromOrEquals(documentedType))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Summarizes uncertain transitive targets for display in DOC631.
        /// </summary>
        private static string SummarizeUncertainTargets(
            HashSet<string> targets,
            int maxItems)
        {
            List<string> orderedTargets =
                targets.OrderBy(static target => target, StringComparer.Ordinal).ToList();

            if (orderedTargets.Count == 0)
            {
                return "unknown targets";
            }

            if (orderedTargets.Count <= maxItems)
            {
                return string.Join(", ", orderedTargets);
            }

            List<string> shown = orderedTargets.Take(maxItems).ToList();
            int remaining = orderedTargets.Count - shown.Count;

            return string.Join(", ", shown) + $" and {remaining} more";
        }
    }
}
