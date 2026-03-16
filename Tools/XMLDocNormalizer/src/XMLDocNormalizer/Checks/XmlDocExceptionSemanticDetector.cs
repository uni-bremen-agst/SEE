using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using XMLDocNormalizer.Checks.Infrastructure;
using XMLDocNormalizer.Checks.Infrastructure.Exception;
using XMLDocNormalizer.Configuration;
using XMLDocNormalizer.Execution.Semantic;
using XMLDocNormalizer.Models;
using XMLDocNormalizer.Models.DTO;
using XMLDocNormalizer.Utils;

namespace XMLDocNormalizer.Checks
{
    /// <summary>
    /// Detects exception documentation smells that require semantic analysis.
    /// </summary>
    /// <remarks>
    /// Direct mode raises DOC610 and DOC630.
    /// Transitive modes raise DOC611, DOC631 and DOC632.
    /// DOC660 and DOC670 are independent of the selected exception analysis mode.
    /// </remarks>
    internal static class XmlDocExceptionSemanticDetector
    {
        /// <summary>
        /// Scans the syntax tree and returns exception-related findings that require semantic analysis.
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

            return FindExceptionSmells(
                tree,
                filePath,
                semanticModel,
                semanticContext,
                new XmlDocOptions());
        }

        /// <summary>
        /// Scans the syntax tree and returns exception-related findings that require semantic analysis.
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
            return FindExceptionSmells(
                tree,
                filePath,
                semanticModel,
                semanticContext,
                new XmlDocOptions());
        }

        /// <summary>
        /// Scans the syntax tree and returns exception-related findings that require semantic analysis.
        /// </summary>
        /// <param name="tree">The syntax tree to analyze.</param>
        /// <param name="filePath">The file path used for reporting.</param>
        /// <param name="semanticModel">The semantic model for the syntax tree.</param>
        /// <param name="semanticContext">The project-closure semantic context.</param>
        /// <param name="options">The XML documentation analysis options.</param>
        /// <returns>A list of findings.</returns>
        public static List<Finding> FindExceptionSmells(
            SyntaxTree tree,
            string filePath,
            SemanticModel semanticModel,
            ProjectClosureSemanticContext semanticContext,
            XmlDocOptions options)
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
                root.DescendantNodes().OfType<MemberDeclarationSyntax>();

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

                ExceptionFlowAnalysisResult flowResult = options.ExceptionAnalysisMode switch
                {
                    ExceptionAnalysisMode.Direct =>
                        ExceptionFlowAnalyzer.AnalyzeDirectlyThrownExceptions(member, semanticContext),

                    _ =>
                        ExceptionFlowAnalyzer.AnalyzeTransitivelyThrownExceptions(member, semanticContext)
                };

                AddInvalidExceptionCrefFindings(findings, tree, filePath, tagInfos);
                AddExceptionCrefNotExceptionTypeFindings(findings, tree, filePath, tagInfos, exceptionBase);

                if (IsTransitiveMode(options))
                {
                    AddExceptionFlowNotDecidableFindings(
                        findings,
                        tree,
                        filePath,
                        tagInfos,
                        exceptionBase,
                        flowResult,
                        options,
                        semanticContext);

                    AddDocumentedExceptionWithoutTransitiveThrowFindings(
                        findings,
                        tree,
                        filePath,
                        tagInfos,
                        exceptionBase,
                        flowResult,
                        options,
                        semanticContext);

                    AddMissingTransitiveExceptionTagFindings(
                        findings,
                        tree,
                        filePath,
                        member,
                        tagInfos,
                        exceptionBase,
                        flowResult,
                        options,
                        semanticContext);
                }
                else
                {
                    AddDocumentedExceptionWithoutDirectThrowFindings(
                        findings,
                        tree,
                        filePath,
                        tagInfos,
                        exceptionBase,
                        flowResult);

                    AddMissingDirectExceptionTagFindings(
                        findings,
                        tree,
                        filePath,
                        member,
                        tagInfos,
                        exceptionBase,
                        flowResult);
                }
            }

            return findings;
        }

        /// <summary>
        /// Determines whether the configured exception analysis mode is transitive.
        /// </summary>
        /// <param name="options">The XML documentation analysis options.</param>
        /// <returns>
        /// <see langword="true"/> if a transitive exception analysis mode is active;
        /// otherwise <see langword="false"/>.
        /// </returns>
        private static bool IsTransitiveMode(XmlDocOptions options)
        {
            return options.ExceptionAnalysisMode != ExceptionAnalysisMode.Direct;
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
                if (string.IsNullOrWhiteSpace(info.Tag.RawAttributeValue) ||
                    info.CrefAttribute == null ||
                    info.CrefAttribute.Cref == null ||
                    info.ResolvedSymbol != null)
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
        /// Adds DOC670 findings for exception tags whose cref resolves to a symbol that is not an exception type.
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
                if (string.IsNullOrWhiteSpace(info.Tag.RawAttributeValue) ||
                    info.CrefAttribute == null ||
                    info.CrefAttribute.Cref == null ||
                    info.ResolvedTypeSymbol == null)
                {
                    continue;
                }

                if (info.ResolvedTypeSymbol.InheritsFromOrEquals(exceptionBase))
                {
                    continue;
                }

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

        /// <summary>
        /// Adds DOC631 findings for relevant documented exception tags whose transitive flow
        /// could not be decided completely and that are not already covered by proven thrown exceptions.
        /// </summary>
        private static void AddExceptionFlowNotDecidableFindings(
            List<Finding> findings,
            SyntaxTree tree,
            string filePath,
            List<ExceptionTagSemanticInfo> tagInfos,
            INamedTypeSymbol exceptionBase,
            ExceptionFlowAnalysisResult flowResult,
            XmlDocOptions options,
            ProjectClosureSemanticContext semanticContext)
        {
            if (!flowResult.HasUncertainPaths)
            {
                return;
            }

            string summary = SummarizeUncertainTargets(flowResult.UncertainTargets, 3);

            foreach (ExceptionTagSemanticInfo info in tagInfos)
            {
                if (!IsRelevantDocumentedException(info, exceptionBase, options, semanticContext))
                {
                    continue;
                }

                if (IsDocumentedExceptionCoveredByThrownTypes(flowResult.ThrownExceptions, info.ResolvedTypeSymbol!))
                {
                    continue;
                }

                findings.Add(FindingFactory.AtPosition(
                    tree,
                    filePath,
                    tagName: "exception",
                    XmlDocSmells.ExceptionFlowNotDecidable,
                    info.CrefAttribute!.SpanStart,
                    snippet: SyntaxUtils.GetSnippet(info.Tag.Element),
                    info.Tag.RawAttributeValue!,
                    summary));
            }
        }

        /// <summary>
        /// Adds DOC630 findings for documented exceptions that are not directly thrown by the member.
        /// </summary>
        private static void AddDocumentedExceptionWithoutDirectThrowFindings(
            List<Finding> findings,
            SyntaxTree tree,
            string filePath,
            List<ExceptionTagSemanticInfo> tagInfos,
            INamedTypeSymbol exceptionBase,
            ExceptionFlowAnalysisResult flowResult)
        {
            foreach (ExceptionTagSemanticInfo info in tagInfos)
            {
                if (string.IsNullOrWhiteSpace(info.Tag.RawAttributeValue) ||
                    info.CrefAttribute == null ||
                    info.CrefAttribute.Cref == null ||
                    info.ResolvedTypeSymbol == null ||
                    !info.ResolvedTypeSymbol.InheritsFromOrEquals(exceptionBase))
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
        /// Adds DOC632 findings for relevant documented exceptions that were not found
        /// within the configured transitive analysis scope.
        /// </summary>
        private static void AddDocumentedExceptionWithoutTransitiveThrowFindings(
            List<Finding> findings,
            SyntaxTree tree,
            string filePath,
            List<ExceptionTagSemanticInfo> tagInfos,
            INamedTypeSymbol exceptionBase,
            ExceptionFlowAnalysisResult flowResult,
            XmlDocOptions options,
            ProjectClosureSemanticContext semanticContext)
        {
            if (flowResult.HasUncertainPaths)
            {
                return;
            }

            foreach (ExceptionTagSemanticInfo info in tagInfos)
            {
                if (!IsRelevantDocumentedException(info, exceptionBase, options, semanticContext))
                {
                    continue;
                }

                if (IsDocumentedExceptionCoveredByThrownTypes(flowResult.ThrownExceptions, info.ResolvedTypeSymbol!))
                {
                    continue;
                }

                findings.Add(FindingFactory.AtPosition(
                    tree,
                    filePath,
                    tagName: "exception",
                    XmlDocSmells.ExceptionTagWithoutTransitiveThrow,
                    info.CrefAttribute!.SpanStart,
                    snippet: SyntaxUtils.GetSnippet(info.Tag.Element),
                    info.Tag.RawAttributeValue!));
            }
        }

        /// <summary>
        /// Adds DOC610 findings for directly thrown exceptions that are not covered by any <exception> tag.
        /// </summary>
        private static void AddMissingDirectExceptionTagFindings(
            List<Finding> findings,
            SyntaxTree tree,
            string filePath,
            MemberDeclarationSyntax member,
            List<ExceptionTagSemanticInfo> tagInfos,
            INamedTypeSymbol exceptionBase,
            ExceptionFlowAnalysisResult flowResult)
        {
            HashSet<INamedTypeSymbol> documentedExceptions =
                CollectDirectDocumentedExceptionTypes(tagInfos, exceptionBase);

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
        /// Adds DOC611 findings for transitively thrown exceptions that are not covered by any relevant <exception> tag.
        /// </summary>
        private static void AddMissingTransitiveExceptionTagFindings(
            List<Finding> findings,
            SyntaxTree tree,
            string filePath,
            MemberDeclarationSyntax member,
            List<ExceptionTagSemanticInfo> tagInfos,
            INamedTypeSymbol exceptionBase,
            ExceptionFlowAnalysisResult flowResult,
            XmlDocOptions options,
            ProjectClosureSemanticContext semanticContext)
        {
            HashSet<INamedTypeSymbol> documentedExceptions =
                CollectRelevantDocumentedExceptionTypes(tagInfos, exceptionBase, options, semanticContext);

            foreach (INamedTypeSymbol thrownType in flowResult.ThrownExceptions)
            {
                if (!IsRelevantThrownException(thrownType, exceptionBase, options, semanticContext))
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
                    XmlDocSmells.MissingTransitiveExceptionDocumentation,
                    MemberAnchorResolver.GetAnchorPosition(member),
                    snippet: string.Empty,
                    thrownType.ToDisplayString()));
            }
        }

        /// <summary>
        /// Determines whether the documented exception is relevant in the configured mode.
        /// </summary>
        private static bool IsRelevantDocumentedException(
            ExceptionTagSemanticInfo info,
            INamedTypeSymbol exceptionBase,
            XmlDocOptions options,
            ProjectClosureSemanticContext semanticContext)
        {
            if (string.IsNullOrWhiteSpace(info.Tag.RawAttributeValue) ||
                info.CrefAttribute == null ||
                info.CrefAttribute.Cref == null ||
                info.ResolvedTypeSymbol == null ||
                !info.ResolvedTypeSymbol.InheritsFromOrEquals(exceptionBase))
            {
                return false;
            }

            if (options.ExceptionAnalysisMode == ExceptionAnalysisMode.ProjectTransitiveProjectExceptions)
            {
                return semanticContext.IsDeclaredInReportingScope(info.ResolvedTypeSymbol);
            }

            return true;
        }

        /// <summary>
        /// Determines whether the thrown exception is relevant in the configured mode.
        /// </summary>
        private static bool IsRelevantThrownException(
            INamedTypeSymbol thrownType,
            INamedTypeSymbol exceptionBase,
            XmlDocOptions options,
            ProjectClosureSemanticContext semanticContext)
        {
            if (!thrownType.InheritsFromOrEquals(exceptionBase))
            {
                return false;
            }

            if (options.ExceptionAnalysisMode == ExceptionAnalysisMode.ProjectTransitiveProjectExceptions)
            {
                return semanticContext.IsDeclaredInReportingScope(thrownType);
            }

            return true;
        }

        /// <summary>
        /// Collects all documented exception types for direct exception analysis.
        /// </summary>
        private static HashSet<INamedTypeSymbol> CollectDirectDocumentedExceptionTypes(
            List<ExceptionTagSemanticInfo> tagInfos,
            INamedTypeSymbol exceptionBase)
        {
            HashSet<INamedTypeSymbol> documented = new(SymbolEqualityComparer.Default);

            foreach (ExceptionTagSemanticInfo info in tagInfos)
            {
                if (string.IsNullOrWhiteSpace(info.Tag.RawAttributeValue) ||
                    info.ResolvedTypeSymbol == null ||
                    !info.ResolvedTypeSymbol.InheritsFromOrEquals(exceptionBase))
                {
                    continue;
                }

                documented.Add(info.ResolvedTypeSymbol);
            }

            return documented;
        }

        /// <summary>
        /// Collects all relevant documented exception types.
        /// </summary>
        private static HashSet<INamedTypeSymbol> CollectRelevantDocumentedExceptionTypes(
            List<ExceptionTagSemanticInfo> tagInfos,
            INamedTypeSymbol exceptionBase,
            XmlDocOptions options,
            ProjectClosureSemanticContext semanticContext)
        {
            HashSet<INamedTypeSymbol> documented = new(SymbolEqualityComparer.Default);

            foreach (ExceptionTagSemanticInfo info in tagInfos)
            {
                if (IsRelevantDocumentedException(info, exceptionBase, options, semanticContext))
                {
                    documented.Add(info.ResolvedTypeSymbol!);
                }
            }

            return documented;
        }

        /// <summary>
        /// Determines whether the documented exception type is covered by one of the thrown exception types.
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
        /// Determines whether the thrown exception type is covered by one of the documented exception types.
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
