using Microsoft.CodeAnalysis;

namespace XMLDocNormalizer.Checks.Infrastructure.Exception.Flow
{
    /// <summary>
    /// Stores local exception sources, call edges, and uncertainty collected
    /// from one syntax fragment.
    /// </summary>
    /// <remarks>
    /// Fragments allow try-block content to be filtered before it is merged
    /// into the containing callable summary.
    /// </remarks>
    internal sealed class ExceptionFlowSummaryFragment
    {
        /// <summary>
        /// Stores local exception sources.
        /// </summary>
        private readonly List<ExceptionFlowSummarySource> sources =
            new();

        /// <summary>
        /// Stores source-level call edges.
        /// </summary>
        private readonly List<ExceptionFlowSummaryCallEdge> callEdges =
            new();

        /// <summary>
        /// Stores targets whose executable exception flow was unavailable.
        /// </summary>
        private readonly HashSet<string> uncertainTargets =
            new(StringComparer.Ordinal);

        /// <summary>
        /// Gets the directly collected exception sources.
        /// </summary>
        /// <value>The local exception sources in insertion order.</value>
        public IReadOnlyList<ExceptionFlowSummarySource> Sources =>
            sources;

        /// <summary>
        /// Gets the collected call edges.
        /// </summary>
        /// <value>The source-level call edges in insertion order.</value>
        public IReadOnlyList<ExceptionFlowSummaryCallEdge> CallEdges =>
            callEdges;

        /// <summary>
        /// Gets the unresolved callable targets.
        /// </summary>
        /// <value>The unresolved target display names.</value>
        public IReadOnlySet<string> UncertainTargets =>
            uncertainTargets;

        /// <summary>
        /// Adds one local exception source.
        /// </summary>
        /// <param name="source">The source to add.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="source"/> is
        /// <see langword="null"/>.
        /// </exception>
        public void AddSource(
            ExceptionFlowSummarySource source)
        {
            ArgumentNullException.ThrowIfNull(source);

            sources.Add(source);
        }

        /// <summary>
        /// Adds one source-level call edge.
        /// </summary>
        /// <param name="callEdge">The call edge to add.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="callEdge"/> is
        /// <see langword="null"/>.
        /// </exception>
        public void AddCallEdge(
            ExceptionFlowSummaryCallEdge callEdge)
        {
            ArgumentNullException.ThrowIfNull(callEdge);

            callEdges.Add(callEdge);
        }

        /// <summary>
        /// Adds one unresolved callable target.
        /// </summary>
        /// <param name="target">
        /// The display name of the unresolved callable.
        /// </param>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="target"/> is null, empty, or consists
        /// only of white-space characters.
        /// </exception>
        public void AddUncertainTarget(
            string target)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(target);

            uncertainTargets.Add(target);
        }

        /// <summary>
        /// Transfers the content of another fragment into this fragment.
        /// </summary>
        /// <param name="source">
        /// The source fragment whose content should be transferred. Supplying
        /// this fragment itself leaves its content unchanged.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="source"/> is
        /// <see langword="null"/>.
        /// </exception>
        public void Merge(
            ExceptionFlowSummaryFragment source)
        {
            ArgumentNullException.ThrowIfNull(source);

            if (ReferenceEquals(
                    this,
                    source))
            {
                return;
            }

            sources.AddRange(
                source.sources);

            callEdges.AddRange(
                source.callEdges);

            uncertainTargets.UnionWith(
                source.uncertainTargets);

            source.sources.Clear();
            source.callEdges.Clear();
            source.uncertainTargets.Clear();
        }

        /// <summary>
        /// Applies one typed catch clause to this fragment.
        /// </summary>
        /// <param name="caughtType">
        /// The exception type handled by the catch clause.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="caughtType"/> is
        /// <see langword="null"/>.
        /// </exception>
        /// <remarks>
        /// Matching local sources are removed immediately. Call edges remain
        /// present because their targets may produce additional exception
        /// types; the caught type is therefore attached as an edge filter.
        /// Uncertainty remains because a typed catch cannot prove that every
        /// unknown exception is handled.
        /// </remarks>
        public void SuppressCaughtException(
            INamedTypeSymbol caughtType)
        {
            ArgumentNullException.ThrowIfNull(caughtType);

            sources.RemoveAll(
                source =>
                    ExceptionFlowCaughtTypeFilter
                        .IsSameOrDerivedFrom(
                            source.ExceptionType,
                            caughtType));

            foreach (ExceptionFlowSummaryCallEdge callEdge
                     in callEdges)
            {
                callEdge.AddCaughtExceptionType(
                    caughtType);
            }
        }

        /// <summary>
        /// Applies a catch-all clause to this fragment.
        /// </summary>
        /// <remarks>
        /// A catch-all removes all sources, call edges, and uncertainty from
        /// the protected try-block because none of that flow can escape.
        /// </remarks>
        public void SuppressAll()
        {
            sources.Clear();
            callEdges.Clear();
            uncertainTargets.Clear();
        }
    }
}
