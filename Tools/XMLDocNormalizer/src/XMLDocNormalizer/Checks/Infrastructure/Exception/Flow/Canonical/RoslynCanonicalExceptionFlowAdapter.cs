using Microsoft.CodeAnalysis;
using XMLDocNormalizer.Models;
using XMLDocNormalizer.Models.DTO;

namespace XMLDocNormalizer.Checks.Infrastructure.Exception.Flow.Canonical
{
    /// <summary>
    /// Converts active Roslyn-backed exception-flow state to and from canonical IR.
    /// </summary>
    internal static class RoslynCanonicalExceptionFlowAdapter
    {
        /// <summary>
        /// Converts one completed local summary to canonical IR.
        /// </summary>
        /// <param name="key">The key value.</param>
        /// <param name="summary">The summary value.</param>
        /// <param name="canonicalSummary">The canonicalSummary value.</param>
        /// <returns>The operation result.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown when the supplied input cannot be processed.</exception>
        public static bool TryCreateSummary(
            ExceptionFlowCallableKey key,
            ExceptionFlowSummary summary,
            out CanonicalExceptionFlowSummary? canonicalSummary)
        {
            ArgumentNullException.ThrowIfNull(key);
            ArgumentNullException.ThrowIfNull(summary);

            if (key.CanonicalIdentity == null || key.CanonicalContext == null)
            {
                canonicalSummary = null;
                return false;
            }

            CanonicalCallableIdentity callable = key.CanonicalIdentity;
            CanonicalExceptionFlowCallContext callContext = key.CanonicalContext;

            CanonicalExceptionFlowSummarySource[] sources = summary.Sources
                .Select(
                    source => new CanonicalExceptionFlowSummarySource(
                        RoslynCanonicalIdentityFactory.CreateTypeIdentity(source.ExceptionType),
                        CreateCanonicalPath(source.LocalPath),
                        source.Kind))
                .ToArray();
            CanonicalExceptionFlowCallEdge[] edges = summary.CallEdges
                .Select(edge => CreateCanonicalEdge(callable, edge))
                .ToArray();
            CanonicalExceptionFlowUncertainty[] uncertainties = summary.UncertainTargets
                .Select(
                    static display => new CanonicalExceptionFlowUncertainty(
                        CanonicalExceptionFlowUncertaintyKind.LegacyUnclassified,
                        target: null,
                        display))
                .ToArray();

            canonicalSummary = new CanonicalExceptionFlowSummary(
                callable,
                callContext,
                summary.HasExecutableBody,
                sources,
                edges,
                uncertainties);
            return true;
        }

        /// <summary>
        /// Rebinds a canonical summary inside one explicit compilation.
        /// </summary>
        /// <param name="canonicalSummary">The canonicalSummary value.</param>
        /// <param name="resolver">The resolver value.</param>
        /// <param name="resolution">The resolution value.</param>
        /// <returns>The operation result.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown when the supplied input cannot be processed.</exception>
        /// <exception cref="System.ArgumentException">Thrown when the supplied input cannot be processed.</exception>
        /// <exception cref="System.NotSupportedException">Thrown when the supplied input cannot be processed.</exception>
        public static bool TryResolveSummary(
            CanonicalExceptionFlowSummary canonicalSummary,
            RoslynCanonicalIdentityResolver resolver,
            out RoslynExceptionFlowSummaryResolution? resolution)
        {
            ArgumentNullException.ThrowIfNull(canonicalSummary);
            ArgumentNullException.ThrowIfNull(resolver);

            ISymbol? callable = resolver.ResolveCallableSymbol(canonicalSummary.Callable);

            if (callable == null
                || !TryResolveContext(canonicalSummary.CallContext, resolver, out ExceptionFlowCallContext? context)
                || context == null)
            {
                resolution = null;
                return false;
            }

            ExceptionFlowSummaryFragment fragment = new();

            foreach (CanonicalExceptionFlowSummarySource source in canonicalSummary.Sources)
            {
                if (resolver.ResolveType(source.ExceptionType) is not INamedTypeSymbol exceptionType)
                {
                    resolution = null;
                    return false;
                }

                fragment.AddSource(
                    new ExceptionFlowSummarySource(
                        exceptionType,
                        CreateRoslynPath(source.Path),
                        source.Kind));
            }

            foreach (CanonicalExceptionFlowCallEdge edge in canonicalSummary.CallEdges)
            {
                ISymbol? target = resolver.ResolveCallableSymbol(edge.Target);

                if (target == null
                    || !TryResolveContext(edge.TargetContext, resolver, out ExceptionFlowCallContext? targetContext)
                    || targetContext == null)
                {
                    resolution = null;
                    return false;
                }

                ExceptionFlowSummaryCallEdge resolvedEdge = new(
                    new ExceptionFlowCallableKey(target, targetContext),
                    edge.CallSite);

                if (edge.CatchBehavior.Kind == CanonicalExceptionFlowCatchKind.CatchAll)
                {
                    resolution = null;
                    return false;
                }

                foreach (CanonicalTypeIdentity caughtTypeIdentity
                         in edge.CatchBehavior.CaughtTypes)
                {
                    if (resolver.ResolveType(caughtTypeIdentity) is not INamedTypeSymbol caughtType)
                    {
                        resolution = null;
                        return false;
                    }

                    resolvedEdge.AddCaughtExceptionType(caughtType);
                }

                fragment.AddCallEdge(resolvedEdge);
            }

            foreach (CanonicalExceptionFlowUncertainty uncertainty
                     in canonicalSummary.Uncertainties)
            {
                if (uncertainty.Target != null
                    && resolver.ResolveCallableSymbol(uncertainty.Target) == null)
                {
                    resolution = null;
                    return false;
                }

                fragment.AddUncertainTarget(uncertainty.DisplayText);
            }

            ExceptionFlowSummary summary = new();

            if (canonicalSummary.HasExecutableBody)
            {
                summary.MarkExecutableBodyAnalyzed();
            }

            summary.Merge(fragment);
            ExceptionFlowCallableKey key = new(callable, context);

            if (key.CanonicalIdentity == null
                || context.CanonicalContext == null
                || !key.CanonicalIdentity.Equals(canonicalSummary.Callable)
                || !context.CanonicalContext.Equals(canonicalSummary.CallContext))
            {
                resolution = null;
                return false;
            }

            resolution = new RoslynExceptionFlowSummaryResolution(key, context, summary);
            return true;
        }

        /// <summary>
        /// Converts a complete analysis result to canonical IR.
        /// </summary>
        /// <param name="result">The result value.</param>
        /// <returns>The operation result.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown when the supplied input cannot be processed.</exception>
        /// <exception cref="System.ArgumentException">Thrown when the supplied input cannot be processed.</exception>
        /// <exception cref="System.NotSupportedException">Thrown when the supplied input cannot be processed.</exception>
        public static CanonicalExceptionFlowAnalysisResult CreateAnalysisResult(
            ExceptionFlowAnalysisResult result)
        {
            ArgumentNullException.ThrowIfNull(result);

            List<CanonicalExceptionFlowResultEntry> entries = new();

            foreach (INamedTypeSymbol exceptionType in result.ThrownExceptions)
            {
                entries.Add(
                    CreateResultEntry(
                        exceptionType,
                        CanonicalExceptionFlowEvidenceKind.Proven,
                        result.GetExceptionPaths(exceptionType),
                        result.ArePathsTruncated(exceptionType)));
            }

            foreach (INamedTypeSymbol exceptionType
                     in result.ExternalDocumentationEvidenceExceptions)
            {
                entries.Add(
                    CreateResultEntry(
                        exceptionType,
                        CanonicalExceptionFlowEvidenceKind.ExternalDocumentation,
                        result.GetExternalDocumentationEvidencePaths(exceptionType),
                        result.AreExternalDocumentationEvidencePathsTruncated(exceptionType)));
            }

            CanonicalExceptionFlowUncertainty[] uncertainties = result.UncertainTargets
                .Select(
                    static display => new CanonicalExceptionFlowUncertainty(
                        CanonicalExceptionFlowUncertaintyKind.LegacyUnclassified,
                        target: null,
                        display))
                .ToArray();

            return new CanonicalExceptionFlowAnalysisResult(entries.ToArray(), uncertainties);
        }

        /// <summary>
        /// Rebinds a canonical analysis result inside one explicit compilation.
        /// </summary>
        /// <param name="canonicalResult">The canonicalResult value.</param>
        /// <param name="resolver">The resolver value.</param>
        /// <param name="result">The result value.</param>
        /// <returns>The operation result.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown when the supplied input cannot be processed.</exception>
        /// <exception cref="System.ArgumentException">Thrown when the supplied input cannot be processed.</exception>
        /// <exception cref="System.NotSupportedException">Thrown when the supplied input cannot be processed.</exception>
        public static bool TryResolveAnalysisResult(
            CanonicalExceptionFlowAnalysisResult canonicalResult,
            RoslynCanonicalIdentityResolver resolver,
            out ExceptionFlowAnalysisResult? result)
        {
            ArgumentNullException.ThrowIfNull(canonicalResult);
            ArgumentNullException.ThrowIfNull(resolver);

            ExceptionFlowAnalysisResult resolved = new();

            foreach (CanonicalExceptionFlowResultEntry entry in canonicalResult.Entries)
            {
                if (resolver.ResolveType(entry.ExceptionType) is not INamedTypeSymbol exceptionType)
                {
                    result = null;
                    return false;
                }

                foreach (CanonicalExceptionFlowPath path in entry.Paths)
                {
                    ExceptionFlowPath resolvedPath = CreateRoslynPath(path);

                    if (entry.EvidenceKind == CanonicalExceptionFlowEvidenceKind.Proven)
                    {
                        resolved.AddExceptionPath(exceptionType, resolvedPath);
                    }
                    else
                    {
                        resolved.AddExternalDocumentationEvidencePath(exceptionType, resolvedPath);
                    }
                }

                if (entry.PathsTruncated)
                {
                    if (entry.EvidenceKind == CanonicalExceptionFlowEvidenceKind.Proven)
                    {
                        resolved.MarkPathsTruncated(exceptionType);
                    }
                    else
                    {
                        resolved.MarkExternalDocumentationEvidencePathsTruncated(exceptionType);
                    }
                }
            }

            foreach (CanonicalExceptionFlowUncertainty uncertainty
                     in canonicalResult.Uncertainties)
            {
                if (uncertainty.Target != null
                    && resolver.ResolveCallableSymbol(uncertainty.Target) == null)
                {
                    result = null;
                    return false;
                }

                resolved.UncertainTargets.Add(uncertainty.DisplayText);
            }

            result = resolved;
            return true;
        }

        /// <summary>Creates one canonical result entry.</summary>
        /// <param name="exceptionType">The Roslyn exception type.</param>
        /// <param name="evidenceKind">The kind of exception evidence.</param>
        /// <param name="paths">The evidence paths.</param>
        /// <param name="pathsTruncated">Whether the path collection was truncated.</param>
        /// <returns>The canonical result entry.</returns>
        /// <exception cref="System.ArgumentException">Thrown when the supplied input cannot be processed.</exception>
        /// <exception cref="System.ArgumentNullException">Thrown when the supplied input cannot be processed.</exception>
        /// <exception cref="System.NotSupportedException">Thrown when the supplied input cannot be processed.</exception>
        private static CanonicalExceptionFlowResultEntry CreateResultEntry(
            INamedTypeSymbol exceptionType,
            CanonicalExceptionFlowEvidenceKind evidenceKind,
            IReadOnlyList<ExceptionFlowPath> paths,
            bool pathsTruncated)
        {
            return new CanonicalExceptionFlowResultEntry(
                RoslynCanonicalIdentityFactory.CreateTypeIdentity(exceptionType),
                evidenceKind,
                paths.Select(CreateCanonicalPath).ToArray(),
                pathsTruncated);
        }

        /// <summary>Creates one canonical call edge.</summary>
        /// <param name="source">The canonical source callable.</param>
        /// <param name="edge">The Roslyn-backed call edge.</param>
        /// <returns>The canonical call edge.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the target has no exact canonical identity or context.
        /// </exception>
        /// <exception cref="System.ArgumentNullException">Thrown when the supplied input cannot be processed.</exception>
        private static CanonicalExceptionFlowCallEdge CreateCanonicalEdge(
            CanonicalCallableIdentity source,
            ExceptionFlowSummaryCallEdge edge)
        {
            CanonicalTypeIdentity[] caughtTypes = edge.CaughtExceptionTypes
                .Select(RoslynCanonicalIdentityFactory.CreateTypeIdentity)
                .ToArray();

            return new CanonicalExceptionFlowCallEdge(
                source,
                edge.Target.CanonicalIdentity
                    ?? throw new InvalidOperationException(
                        "A transportable call edge requires a canonical target identity."),
                edge.Target.CanonicalContext
                    ?? throw new InvalidOperationException(
                        "A transportable call edge requires a canonical target context."),
                edge.CallSiteStep,
                new CanonicalExceptionFlowCatch(
                    caughtTypes.Length == 0
                        ? CanonicalExceptionFlowCatchKind.None
                        : CanonicalExceptionFlowCatchKind.Typed,
                    hasFilter: false,
                    caughtTypes));
        }

        /// <summary>Attempts to rebind one canonical call context.</summary>
        /// <param name="canonicalContext">The canonical context to rebind.</param>
        /// <param name="resolver">The compilation-bound resolver.</param>
        /// <param name="context">Receives the rebound context.</param>
        /// <returns><see langword="true"/> when every identity resolves exactly.</returns>
        /// <exception cref="System.ArgumentException">Thrown when the supplied input cannot be processed.</exception>
        /// <exception cref="System.ArgumentNullException">Thrown when the supplied input cannot be processed.</exception>
        /// <exception cref="System.NotSupportedException">Thrown when the supplied input cannot be processed.</exception>
        private static bool TryResolveContext(
            CanonicalExceptionFlowCallContext canonicalContext,
            RoslynCanonicalIdentityResolver resolver,
            out ExceptionFlowCallContext? context)
        {
            ISymbol? callable = canonicalContext.Callable == null
                ? null
                : resolver.ResolveCallableSymbol(canonicalContext.Callable);

            if (canonicalContext.Callable != null && callable == null)
            {
                context = null;
                return false;
            }

            List<KeyValuePair<int, ISymbol>> members = new();

            foreach (CanonicalParameterMemberFact fact in canonicalContext.MemberFacts)
            {
                ISymbol? member = resolver.ResolveStableMember(fact.Member);

                if (member == null)
                {
                    context = null;
                    return false;
                }

                members.Add(new KeyValuePair<int, ISymbol>(fact.ParameterOrdinal, member));
            }

            context = new ExceptionFlowCallContext(
                callable,
                canonicalContext.ParameterFacts.Select(
                    static fact => new KeyValuePair<int, ExceptionFlowValueFacts>(
                        fact.ParameterOrdinal,
                        fact.Facts)),
                members);
            return context.CanonicalContext?.Equals(canonicalContext) == true;
        }

        /// <summary>Creates a canonical exception path.</summary>
        /// <param name="path">The Roslyn-backed path.</param>
        /// <returns>The canonical exception path.</returns>
        /// <exception cref="System.ArgumentException">Thrown when the supplied input cannot be processed.</exception>
        /// <exception cref="System.ArgumentNullException">Thrown when the supplied input cannot be processed.</exception>
        private static CanonicalExceptionFlowPath CreateCanonicalPath(ExceptionFlowPath path)
        {
            return new CanonicalExceptionFlowPath(path.Steps.ToArray());
        }

        /// <summary>Creates an in-process exception path.</summary>
        /// <param name="path">The canonical path.</param>
        /// <returns>The in-process exception path.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown when the supplied input cannot be processed.</exception>
        private static ExceptionFlowPath CreateRoslynPath(CanonicalExceptionFlowPath path)
        {
            ExceptionFlowPathStep[] steps = path.Steps;
            ExceptionFlowPath result = new(steps[^1]);

            for (int index = steps.Length - 2; index >= 0; index--)
            {
                result = result.Prepend(steps[index]);
            }

            return result;
        }
    }

    /// <summary>
    /// Contains one exactly rebound Roslyn-backed summary node.
    /// </summary>
    /// <param name="Key">The Key value.</param>
    /// <param name="Context">The Context value.</param>
    /// <param name="Summary">The Summary value.</param>
    internal sealed record RoslynExceptionFlowSummaryResolution(
        ExceptionFlowCallableKey Key,
        ExceptionFlowCallContext Context,
        ExceptionFlowSummary Summary);
}
