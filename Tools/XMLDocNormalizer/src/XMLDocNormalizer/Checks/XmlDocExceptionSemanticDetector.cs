using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using XMLDocNormalizer.Checks.Infrastructure;
using XMLDocNormalizer.Checks.Infrastructure.Exception;
using XMLDocNormalizer.Checks.Infrastructure.Exception.Flow;
using XMLDocNormalizer.Checks.Infrastructure.Tags;
using XMLDocNormalizer.Configuration;
using XMLDocNormalizer.Execution.Semantic;
using XMLDocNormalizer.Models;
using XMLDocNormalizer.Models.DTO;
using XMLDocNormalizer.Utils;
using XMLDocNormalizer.Utils.Extensions;

namespace XMLDocNormalizer.Checks
{
    /// <summary>
    /// Detects exception documentation smells that require semantic analysis.
    /// </summary>
    /// <remarks>
    /// Direct mode raises DOC610 and DOC630.
    /// Transitive modes raise DOC610 for directly thrown exceptions, DOC611 for transitively
    /// thrown exceptions, DOC631 for undecidable documented exception flow and DOC632 for
    /// documented exceptions that are not found within the configured transitive scope.
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
                    BuildTagInfos(tags, semanticModel, member, filePath);

                ExceptionFlowAnalysisResult directFlowResult =
                    ExceptionFlowAnalyzer.AnalyzeDirectlyThrownExceptions(member, semanticContext);

                ExceptionFlowAnalysisResult flowResult;

                if (IsTransitiveMode(options))
                {
                    flowResult =
                        ExceptionFlowAnalyzer.AnalyzeTransitivelyThrownExceptions(member, semanticContext);
                }
                else
                {
                    flowResult = directFlowResult;
                }

                bool suppressMissingExceptionTagFindings = doc.HasInheritdoc();

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

                    if (!suppressMissingExceptionTagFindings)
                    {
                        AddMissingDirectExceptionTagFindings(
                            findings,
                            tree,
                            filePath,
                            member,
                            tagInfos,
                            exceptionBase,
                            directFlowResult,
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
                            directFlowResult,
                            options,
                            semanticContext);
                    }
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

                    if (!suppressMissingExceptionTagFindings)
                    {
                        AddMissingDirectExceptionTagFindings(
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
        /// Extracts the raw cref value from an exception XML element.
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
        /// <param name="member">The member that owns the exception documentation.</param>
        /// <param name="filePath">The file path used for reporting.</param>
        /// <returns>
        /// A list containing one semantic information object per extracted exception tag.
        /// </returns>
        private static List<ExceptionTagSemanticInfo> BuildTagInfos(
            List<ExtractedXmlDocTag> tags,
            SemanticModel semanticModel,
            MemberDeclarationSyntax member,
            string filePath)
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
                    ResolvedSymbol = resolvedSymbol,
                    FindingContext = CreateExceptionTagContext(
                        member,
                        tag.RawAttributeValue,
                        filePath)
                });
            }

            return infos;
        }

        /// <summary>
        /// Adds DOC660 findings for exception tags whose cref cannot be resolved to a known type.
        /// </summary>
        /// <param name="findings">The finding list to append to.</param>
        /// <param name="tree">The syntax tree that contains the analyzed member.</param>
        /// <param name="filePath">The file path used for reporting.</param>
        /// <param name="tagInfos">The extracted exception tag semantic information.</param>
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
                    info.FindingContext,
                    snippet: SyntaxUtils.GetSnippet(info.Tag.Element),
                    info.Tag.RawAttributeValue));
            }
        }

        /// <summary>
        /// Adds DOC670 findings for exception tags whose cref resolves to a symbol that is not an exception type.
        /// </summary>
        /// <param name="findings">The finding list to append to.</param>
        /// <param name="tree">The syntax tree that contains the analyzed member.</param>
        /// <param name="filePath">The file path used for reporting.</param>
        /// <param name="tagInfos">The extracted exception tag semantic information.</param>
        /// <param name="exceptionBase">The System.Exception base type symbol.</param>
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
                    info.FindingContext,
                    snippet: SyntaxUtils.GetSnippet(info.Tag.Element),
                    info.Tag.RawAttributeValue));
            }
        }

        /// <summary>
        /// Adds DOC631 findings for relevant documented exception tags whose transitive flow
        /// could not be decided completely and that are not already covered by proven thrown exceptions.
        /// </summary>
        /// <param name="findings">The finding list to append to.</param>
        /// <param name="tree">The syntax tree that contains the analyzed member.</param>
        /// <param name="filePath">The file path used for reporting.</param>
        /// <param name="tagInfos">The extracted exception tag semantic information.</param>
        /// <param name="exceptionBase">The System.Exception base type symbol.</param>
        /// <param name="flowResult">The transitive exception-flow analysis result.</param>
        /// <param name="options">The XML documentation analysis options.</param>
        /// <param name="semanticContext">The project-closure semantic context.</param>
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
                    info.FindingContext,
                    snippet: SyntaxUtils.GetSnippet(info.Tag.Element),
                    info.Tag.RawAttributeValue!,
                    summary));
            }
        }

        /// <summary>
        /// Adds DOC630 findings for documented exceptions that are not directly thrown by the member.
        /// </summary>
        /// <param name="findings">The finding list to append to.</param>
        /// <param name="tree">The syntax tree that contains the analyzed member.</param>
        /// <param name="filePath">The file path used for reporting.</param>
        /// <param name="tagInfos">The extracted exception tag semantic information.</param>
        /// <param name="exceptionBase">The System.Exception base type symbol.</param>
        /// <param name="flowResult">The direct exception-flow analysis result.</param>
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
                    info.FindingContext,
                    snippet: SyntaxUtils.GetSnippet(info.Tag.Element),
                    info.Tag.RawAttributeValue));
            }
        }

        /// <summary>
        /// Adds DOC632 findings for relevant documented exceptions that were not found
        /// within the configured transitive analysis scope.
        /// </summary>
        /// <param name="findings">The finding list to append to.</param>
        /// <param name="tree">The syntax tree that contains the analyzed member.</param>
        /// <param name="filePath">The file path used for reporting.</param>
        /// <param name="tagInfos">The extracted exception tag semantic information.</param>
        /// <param name="exceptionBase">The System.Exception base type symbol.</param>
        /// <param name="flowResult">The transitive exception-flow analysis result.</param>
        /// <param name="options">The XML documentation analysis options.</param>
        /// <param name="semanticContext">The project-closure semantic context.</param>
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
                    info.FindingContext,
                    snippet: SyntaxUtils.GetSnippet(info.Tag.Element),
                    info.Tag.RawAttributeValue!));
            }
        }

        /// <summary>
        /// Adds DOC610 findings for directly thrown exceptions that are not covered by any relevant exception tag.
        /// </summary>
        /// <param name="findings">The finding list to append to.</param>
        /// <param name="tree">The syntax tree that contains the member.</param>
        /// <param name="filePath">The file path used for reporting.</param>
        /// <param name="member">The member whose direct exception flow is analyzed.</param>
        /// <param name="tagInfos">The extracted exception tag semantic information.</param>
        /// <param name="exceptionBase">The System.Exception base type symbol.</param>
        /// <param name="flowResult">The direct exception-flow result.</param>
        /// <param name="options">The XML documentation analysis options.</param>
        /// <param name="semanticContext">The project-closure semantic context.</param>
        private static void AddMissingDirectExceptionTagFindings(
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
                CollectRelevantDocumentedExceptionTypes(
                    tagInfos,
                    exceptionBase,
                    options,
                    semanticContext);

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

                string thrownTypeName = thrownType.ToDisplayString();

                findings.Add(FindingFactory.AtPosition(
                    tree,
                    filePath,
                    tagName: "exception",
                    XmlDocSmells.MissingExceptionTag,
                    MemberAnchorResolver.GetAnchorPosition(member),
                    CreateExceptionFlowContext(
                        member,
                        thrownTypeName,
                        filePath),
                    snippet: string.Empty,
                    thrownTypeName));
            }
        }

        /// <summary>
        /// Adds DOC611 findings for transitively thrown exceptions that are not covered by any relevant exception tag.
        /// Directly thrown exceptions are excluded because they are reported as DOC610.
        /// </summary>
        /// <param name="findings">The finding list to append to.</param>
        /// <param name="tree">The syntax tree that contains the member.</param>
        /// <param name="filePath">The file path used for reporting.</param>
        /// <param name="member">The member whose transitive exception flow is analyzed.</param>
        /// <param name="tagInfos">The extracted exception tag semantic information.</param>
        /// <param name="exceptionBase">The System.Exception base type symbol.</param>
        /// <param name="flowResult">The transitive exception-flow result.</param>
        /// <param name="directFlowResult">The direct exception-flow result for the same member.</param>
        /// <param name="options">The XML documentation analysis options.</param>
        /// <param name="semanticContext">The project-closure semantic context.</param>
        private static void AddMissingTransitiveExceptionTagFindings(
            List<Finding> findings,
            SyntaxTree tree,
            string filePath,
            MemberDeclarationSyntax member,
            List<ExceptionTagSemanticInfo> tagInfos,
            INamedTypeSymbol exceptionBase,
            ExceptionFlowAnalysisResult flowResult,
            ExceptionFlowAnalysisResult directFlowResult,
            XmlDocOptions options,
            ProjectClosureSemanticContext semanticContext)
        {
            HashSet<INamedTypeSymbol> documentedExceptions =
                CollectRelevantDocumentedExceptionTypes(
                    tagInfos,
                    exceptionBase,
                    options,
                    semanticContext);

            foreach (INamedTypeSymbol thrownType in flowResult.ThrownExceptions)
            {
                if (directFlowResult.ThrownExceptions.Contains(thrownType))
                {
                    continue;
                }

                if (!IsRelevantThrownException(thrownType, exceptionBase, options, semanticContext))
                {
                    continue;
                }

                if (IsThrownExceptionCoveredByDocumentedTypes(documentedExceptions, thrownType))
                {
                    continue;
                }

                string thrownTypeName = thrownType.ToDisplayString();

                findings.Add(FindingFactory.AtPosition(
                    tree,
                    filePath,
                    tagName: "exception",
                    XmlDocSmells.MissingTransitiveExceptionDocumentation,
                    MemberAnchorResolver.GetAnchorPosition(member),
                    CreateExceptionFlowContext(
                        member,
                        thrownTypeName,
                        filePath),
                    snippet: string.Empty,
                    thrownTypeName));
            }
        }

        /// <summary>
        /// Determines whether the documented exception is relevant in the configured mode.
        /// </summary>
        /// <param name="info">The semantic information for the documented exception tag.</param>
        /// <param name="exceptionBase">The base exception type used to validate exception inheritance.</param>
        /// <param name="options">The XML documentation analysis options.</param>
        /// <param name="semanticContext">The semantic context for project-closure checks.</param>
        /// <returns>
        /// True if the documented exception is relevant in the configured mode; otherwise false.
        /// </returns>
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

            if (options.ExceptionAnalysisMode == ExceptionAnalysisMode.ProjectTransitiveDeclaredExceptions)
            {
                return semanticContext.IsDeclaredInReportingScope(info.ResolvedTypeSymbol);
            }

            return true;
        }

        /// <summary>
        /// Determines whether the thrown exception is relevant in the configured mode.
        /// </summary>
        /// <param name="thrownType">The thrown exception type to inspect.</param>
        /// <param name="exceptionBase">The base exception type used to validate exception inheritance.</param>
        /// <param name="options">The XML documentation analysis options.</param>
        /// <param name="semanticContext">The semantic context for project-closure checks.</param>
        /// <returns>
        /// True if the thrown exception is relevant in the configured mode; otherwise false.
        /// </returns>
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

            if (options.ExceptionAnalysisMode == ExceptionAnalysisMode.ProjectTransitiveDeclaredExceptions)
            {
                return semanticContext.IsDeclaredInReportingScope(thrownType);
            }

            return true;
        }

        /// <summary>
        /// Collects all relevant documented exception types.
        /// </summary>
        /// <param name="tagInfos">The semantic information for documented exception tags.</param>
        /// <param name="exceptionBase">The base exception type used to validate exception inheritance.</param>
        /// <param name="options">The XML documentation analysis options.</param>
        /// <param name="semanticContext">The semantic context for project-closure checks.</param>
        /// <returns>
        /// A set containing all relevant documented exception type symbols.
        /// </returns>
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
        /// <param name="thrownExceptions">The thrown exception types.</param>
        /// <param name="documentedType">The documented exception type to check.</param>
        /// <returns>
        /// True if the documented exception type is covered by a thrown exception type; otherwise false.
        /// </returns>
        private static bool IsDocumentedExceptionCoveredByThrownTypes(
            IEnumerable<INamedTypeSymbol> thrownExceptions,
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
        /// <param name="documentedExceptions">The documented exception types.</param>
        /// <param name="thrownType">The thrown exception type to check.</param>
        /// <returns>
        /// True if the thrown exception type is covered by a documented exception type; otherwise false.
        /// </returns>
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
        /// <param name="targets">The uncertain target names to summarize.</param>
        /// <param name="maxItems">The maximum number of target names to include before summarizing the remaining count.</param>
        /// <returns>
        /// A readable summary of the uncertain target names.
        /// </returns>
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

        /// <summary>
        /// Creates finding context metadata for an existing exception documentation tag.
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
        /// Creates finding context metadata for an exception-flow finding.
        /// </summary>
        /// <param name="member">The member whose exception flow is affected.</param>
        /// <param name="targetName">The affected exception-flow target.</param>
        /// <param name="filePath">The file path used for reporting.</param>
        /// <returns>
        /// A populated finding context for an exception-flow finding.
        /// </returns>
        private static FindingContext CreateExceptionFlowContext(
            MemberDeclarationSyntax member,
            string? targetName,
            string filePath)
        {
            return FindingContextBuilder.ForDeclaration(
                member,
                "ExceptionFlow",
                targetName: targetName,
                filePath: filePath);
        }

        /// <summary>
        /// Creates a stable target name for an exception cref.
        /// </summary>
        /// <param name="rawCref">The raw cref value.</param>
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
