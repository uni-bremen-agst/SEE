using XMLDocNormalizer.Models;

namespace XMLDocNormalizer.Checks.Infrastructure.Exception.Flow.Canonical
{
    /// <summary>
    /// Identifies the effective catch behavior attached to a canonical call edge.
    /// </summary>
    internal enum CanonicalExceptionFlowCatchKind
    {
        /// <summary>
        /// No catch suppression applies to the edge.
        /// </summary>
        None,

        /// <summary>
        /// One or more exact caught type identities suppress matching flow.
        /// </summary>
        Typed,

        /// <summary>
        /// A catch-all suppresses all flow. Existing summaries normally remove
        /// such edges before canonical conversion.
        /// </summary>
        CatchAll
    }

    /// <summary>
    /// Identifies a currently observed uncertainty category.
    /// </summary>
    internal enum CanonicalExceptionFlowUncertaintyKind
    {
        /// <summary>
        /// Legacy analysis retained only display evidence for the uncertainty.
        /// </summary>
        LegacyUnclassified,

        /// <summary>
        /// A callable target could not be resolved exactly.
        /// </summary>
        CallableUnresolved,

        /// <summary>
        /// A callable body was unavailable for analysis.
        /// </summary>
        BodyUnavailable,

        /// <summary>
        /// Runtime dispatch may have additional targets.
        /// </summary>
        IncompleteRuntimeDispatch,

        /// <summary>
        /// Dynamic binding could not be resolved statically.
        /// </summary>
        DynamicBinding,

        /// <summary>
        /// A source operation was unsupported by the current analysis.
        /// </summary>
        AnalysisUnsupported
    }

    /// <summary>
    /// Represents a transportable exception-flow path.
    /// </summary>
    internal sealed class CanonicalExceptionFlowPath
        : IEquatable<CanonicalExceptionFlowPath>
    {
        /// <summary>
        /// Stores path steps in semantic traversal order.
        /// </summary>
        private readonly ExceptionFlowPathStep[] steps;

        /// <summary>
        /// Initializes a canonical path.
        /// </summary>
        /// <param name="steps">The ordered path steps.</param>
        /// <exception cref="System.ArgumentException">Thrown when the supplied input cannot be processed.</exception>
        /// <exception cref="System.ArgumentNullException">Thrown when the supplied input cannot be processed.</exception>
        public CanonicalExceptionFlowPath(ExceptionFlowPathStep[] steps)
        {
            ArgumentNullException.ThrowIfNull(steps);

            if (steps.Length == 0)
            {
                throw new ArgumentException(
                    "A canonical exception-flow path requires at least one step.",
                    nameof(steps));
            }

            this.steps = steps.ToArray();
        }

        /// <summary>
        /// Gets a copy of the ordered path steps.
        /// </summary>
        /// <value>The value described by this property.</value>
        public ExceptionFlowPathStep[] Steps => steps.ToArray();

        /// <inheritdoc/>
        public bool Equals(CanonicalExceptionFlowPath? other)
        {
            return other != null && steps.SequenceEqual(other.steps);
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            return Equals(obj as CanonicalExceptionFlowPath);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            HashCode hash = new();

            foreach (ExceptionFlowPathStep step in steps)
            {
                hash.Add(step);
            }

            return hash.ToHashCode();
        }
    }

    /// <summary>
    /// Represents one direct canonical exception source.
    /// </summary>
    internal sealed class CanonicalExceptionFlowSummarySource
        : IEquatable<CanonicalExceptionFlowSummarySource>
    {
        /// <summary>
        /// Initializes a canonical summary source.
        /// </summary>
        /// <param name="exceptionType">The exception type identity.</param>
        /// <param name="path">The source-local exception path.</param>
        /// <param name="kind">The evidence strength.</param>
        /// <exception cref="System.ArgumentNullException">Thrown when the supplied input cannot be processed.</exception>
        public CanonicalExceptionFlowSummarySource(
            CanonicalTypeIdentity exceptionType,
            CanonicalExceptionFlowPath path,
            ExceptionFlowSourceKind kind)
        {
            ArgumentNullException.ThrowIfNull(exceptionType);
            ArgumentNullException.ThrowIfNull(path);

            ExceptionType = exceptionType;
            Path = path;
            Kind = kind;
        }

        /// <summary>
        /// Gets the exception type identity.
        /// </summary>
        /// <value>The value described by this property.</value>
        public CanonicalTypeIdentity ExceptionType { get; }

        /// <summary>
        /// Gets the source-local path.
        /// </summary>
        /// <value>The value described by this property.</value>
        public CanonicalExceptionFlowPath Path { get; }

        /// <summary>
        /// Gets the evidence strength.
        /// </summary>
        /// <value>The value described by this property.</value>
        public ExceptionFlowSourceKind Kind { get; }

        /// <inheritdoc/>
        public bool Equals(CanonicalExceptionFlowSummarySource? other)
        {
            return other != null
                && ExceptionType.Equals(other.ExceptionType)
                && Path.Equals(other.Path)
                && Kind == other.Kind;
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            return Equals(obj as CanonicalExceptionFlowSummarySource);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return HashCode.Combine(ExceptionType, Path, Kind);
        }
    }

    /// <summary>
    /// Represents catch suppression without retaining Roslyn exception symbols.
    /// </summary>
    internal sealed class CanonicalExceptionFlowCatch
        : IEquatable<CanonicalExceptionFlowCatch>
    {
        /// <summary>
        /// Stores caught types in deterministic identity order.
        /// </summary>
        private readonly CanonicalTypeIdentity[] caughtTypes;

        /// <summary>
        /// Initializes canonical catch information.
        /// </summary>
        /// <param name="kind">The effective catch kind.</param>
        /// <param name="hasFilter">Whether the originating catch had a filter.</param>
        /// <param name="caughtTypes">The caught exception type identities.</param>
        public CanonicalExceptionFlowCatch(
            CanonicalExceptionFlowCatchKind kind,
            bool hasFilter,
            CanonicalTypeIdentity[]? caughtTypes)
        {
            Kind = kind;
            HasFilter = hasFilter;
            this.caughtTypes = (caughtTypes ?? Array.Empty<CanonicalTypeIdentity>())
                .OrderBy(CanonicalIdentityKeyWriter.Write, StringComparer.Ordinal)
                .ToArray();
        }

        /// <summary>
        /// Gets the effective catch kind.
        /// </summary>
        /// <value>The value described by this property.</value>
        public CanonicalExceptionFlowCatchKind Kind { get; }

        /// <summary>
        /// Gets whether the originating catch had a filter.
        /// </summary>
        /// <value>The value described by this property.</value>
        public bool HasFilter { get; }

        /// <summary>
        /// Gets a copy of the caught exception type identities.
        /// </summary>
        /// <value>The value described by this property.</value>
        public CanonicalTypeIdentity[] CaughtTypes => caughtTypes.ToArray();

        /// <inheritdoc/>
        public bool Equals(CanonicalExceptionFlowCatch? other)
        {
            return other != null
                && Kind == other.Kind
                && HasFilter == other.HasFilter
                && caughtTypes.SequenceEqual(other.caughtTypes);
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            return Equals(obj as CanonicalExceptionFlowCatch);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            HashCode hash = new();
            hash.Add(Kind);
            hash.Add(HasFilter);

            foreach (CanonicalTypeIdentity caughtType in caughtTypes)
            {
                hash.Add(caughtType);
            }

            return hash.ToHashCode();
        }
    }

    /// <summary>
    /// Represents one canonical source-level call edge.
    /// </summary>
    internal sealed class CanonicalExceptionFlowCallEdge
        : IEquatable<CanonicalExceptionFlowCallEdge>
    {
        /// <summary>
        /// Initializes a canonical call edge.
        /// </summary>
        /// <param name="source">The source value.</param>
        /// <param name="target">The target value.</param>
        /// <param name="targetContext">The targetContext value.</param>
        /// <param name="callSite">The callSite value.</param>
        /// <param name="catchBehavior">The catchBehavior value.</param>
        /// <exception cref="System.ArgumentNullException">Thrown when the supplied input cannot be processed.</exception>
        public CanonicalExceptionFlowCallEdge(
            CanonicalCallableIdentity source,
            CanonicalCallableIdentity target,
            CanonicalExceptionFlowCallContext targetContext,
            ExceptionFlowPathStep callSite,
            CanonicalExceptionFlowCatch catchBehavior)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(target);
            ArgumentNullException.ThrowIfNull(targetContext);
            ArgumentNullException.ThrowIfNull(callSite);
            ArgumentNullException.ThrowIfNull(catchBehavior);

            Source = source;
            Target = target;
            TargetContext = targetContext;
            CallSite = callSite;
            CatchBehavior = catchBehavior;
        }

        /// <summary>
        /// Gets the source callable identity.
        /// </summary>
        /// <value>The value described by this property.</value>
        public CanonicalCallableIdentity Source { get; }

        /// <summary>
        /// Gets the target callable identity.
        /// </summary>
        /// <value>The value described by this property.</value>
        public CanonicalCallableIdentity Target { get; }

        /// <summary>
        /// Gets the target call context.
        /// </summary>
        /// <value>The value described by this property.</value>
        public CanonicalExceptionFlowCallContext TargetContext { get; }

        /// <summary>
        /// Gets the source-level call-site evidence.
        /// </summary>
        /// <value>The value described by this property.</value>
        public ExceptionFlowPathStep CallSite { get; }

        /// <summary>
        /// Gets the effective catch behavior.
        /// </summary>
        /// <value>The value described by this property.</value>
        public CanonicalExceptionFlowCatch CatchBehavior { get; }

        /// <inheritdoc/>
        public bool Equals(CanonicalExceptionFlowCallEdge? other)
        {
            return other != null
                && Source.Equals(other.Source)
                && Target.Equals(other.Target)
                && TargetContext.Equals(other.TargetContext)
                && CallSite.Equals(other.CallSite)
                && CatchBehavior.Equals(other.CatchBehavior);
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            return Equals(obj as CanonicalExceptionFlowCallEdge);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return HashCode.Combine(Source, Target, TargetContext, CallSite, CatchBehavior);
        }
    }

    /// <summary>
    /// Represents structured uncertainty while retaining existing display evidence.
    /// </summary>
    internal sealed class CanonicalExceptionFlowUncertainty
        : IEquatable<CanonicalExceptionFlowUncertainty>
    {
        /// <summary>
        /// Initializes a canonical uncertainty item.
        /// </summary>
        /// <param name="kind">The kind value.</param>
        /// <param name="target">The target value.</param>
        /// <param name="displayText">The displayText value.</param>
        /// <exception cref="System.ArgumentNullException">Thrown when the supplied input cannot be processed.</exception>
        /// <exception cref="System.ArgumentException">Thrown when the supplied input cannot be processed.</exception>
        public CanonicalExceptionFlowUncertainty(
            CanonicalExceptionFlowUncertaintyKind kind,
            CanonicalCallableIdentity? target,
            string displayText)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(displayText);

            Kind = kind;
            Target = target;
            DisplayText = displayText;
        }

        /// <summary>
        /// Gets the uncertainty category.
        /// </summary>
        /// <value>The value described by this property.</value>
        public CanonicalExceptionFlowUncertaintyKind Kind { get; }

        /// <summary>
        /// Gets the exact target identity when it was available.
        /// </summary>
        /// <value>The value described by this property.</value>
        public CanonicalCallableIdentity? Target { get; }

        /// <summary>
        /// Gets non-authoritative user-facing evidence retained from analysis.
        /// </summary>
        /// <value>The value described by this property.</value>
        public string DisplayText { get; }

        /// <inheritdoc/>
        public bool Equals(CanonicalExceptionFlowUncertainty? other)
        {
            return other != null
                && Kind == other.Kind
                && Equals(Target, other.Target)
                && StringComparer.Ordinal.Equals(DisplayText, other.DisplayText);
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            return Equals(obj as CanonicalExceptionFlowUncertainty);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return HashCode.Combine(
                Kind,
                Target,
                StringComparer.Ordinal.GetHashCode(DisplayText));
        }
    }

    /// <summary>
    /// Represents the complete local canonical summary of one callable context.
    /// </summary>
    internal sealed class CanonicalExceptionFlowSummary
        : IEquatable<CanonicalExceptionFlowSummary>
    {
        /// <summary>         /// Stores the sources values.         /// </summary>
        private readonly CanonicalExceptionFlowSummarySource[] sources;
        /// <summary>         /// Stores the callEdges values.         /// </summary>
        private readonly CanonicalExceptionFlowCallEdge[] callEdges;
        /// <summary>         /// Stores the uncertainties values.         /// </summary>
        private readonly CanonicalExceptionFlowUncertainty[] uncertainties;

        /// <summary>
        /// Initializes a canonical exception summary.
        /// </summary>
        /// <param name="callable">The callable value.</param>
        /// <param name="callContext">The callContext value.</param>
        /// <param name="hasExecutableBody">The hasExecutableBody value.</param>
        /// <param name="sources">The sources value.</param>
        /// <param name="callEdges">The callEdges value.</param>
        /// <param name="uncertainties">The uncertainties value.</param>
        /// <exception cref="System.ArgumentNullException">Thrown when the supplied input cannot be processed.</exception>
        public CanonicalExceptionFlowSummary(
            CanonicalCallableIdentity callable,
            CanonicalExceptionFlowCallContext callContext,
            bool hasExecutableBody,
            CanonicalExceptionFlowSummarySource[]? sources,
            CanonicalExceptionFlowCallEdge[]? callEdges,
            CanonicalExceptionFlowUncertainty[]? uncertainties)
        {
            ArgumentNullException.ThrowIfNull(callable);
            ArgumentNullException.ThrowIfNull(callContext);

            Callable = callable;
            CallContext = callContext;
            HasExecutableBody = hasExecutableBody;
            this.sources = sources?.ToArray()
                ?? Array.Empty<CanonicalExceptionFlowSummarySource>();
            this.callEdges = callEdges?.ToArray()
                ?? Array.Empty<CanonicalExceptionFlowCallEdge>();
            this.uncertainties = (uncertainties
                    ?? Array.Empty<CanonicalExceptionFlowUncertainty>())
                .OrderBy(static item => item.Kind)
                .ThenBy(
                    static item => item.Target == null
                        ? string.Empty
                        : CanonicalIdentityKeyWriter.Write(item.Target),
                    StringComparer.Ordinal)
                .ThenBy(static item => item.DisplayText, StringComparer.Ordinal)
                .ToArray();
        }

        /// <summary>
        /// Gets the summarized callable identity.
        /// </summary>
        /// <value>The value described by this property.</value>
        public CanonicalCallableIdentity Callable { get; }

        /// <summary>
        /// Gets the summarized call context.
        /// </summary>
        /// <value>The value described by this property.</value>
        public CanonicalExceptionFlowCallContext CallContext { get; }

        /// <summary>
        /// Gets whether an executable body was analyzed.
        /// </summary>
        /// <value>The value described by this property.</value>
        public bool HasExecutableBody { get; }

        /// <summary>
        /// Gets a copy of direct summary sources.
        /// </summary>
        /// <value>The value described by this property.</value>
        public CanonicalExceptionFlowSummarySource[] Sources => sources.ToArray();

        /// <summary>
        /// Gets a copy of outgoing call edges.
        /// </summary>
        /// <value>The value described by this property.</value>
        public CanonicalExceptionFlowCallEdge[] CallEdges => callEdges.ToArray();

        /// <summary>
        /// Gets a copy of structured uncertainties.
        /// </summary>
        /// <value>The value described by this property.</value>
        public CanonicalExceptionFlowUncertainty[] Uncertainties => uncertainties.ToArray();

        /// <inheritdoc/>
        public bool Equals(CanonicalExceptionFlowSummary? other)
        {
            return other != null
                && Callable.Equals(other.Callable)
                && CallContext.Equals(other.CallContext)
                && HasExecutableBody == other.HasExecutableBody
                && sources.SequenceEqual(other.sources)
                && callEdges.SequenceEqual(other.callEdges)
                && uncertainties.SequenceEqual(other.uncertainties);
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            return Equals(obj as CanonicalExceptionFlowSummary);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            HashCode hash = new();
            hash.Add(Callable);
            hash.Add(CallContext);
            hash.Add(HasExecutableBody);

            foreach (CanonicalExceptionFlowSummarySource source in sources)
            {
                hash.Add(source);
            }

            foreach (CanonicalExceptionFlowCallEdge edge in callEdges)
            {
                hash.Add(edge);
            }

            foreach (CanonicalExceptionFlowUncertainty uncertainty in uncertainties)
            {
                hash.Add(uncertainty);
            }

            return hash.ToHashCode();
        }
    }
}
