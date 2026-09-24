namespace XMLDocNormalizer.Checks.Infrastructure.Exception.Flow.Canonical
{
    /// <summary>
    /// Represents value facts associated with one parameter ordinal.
    /// </summary>
    internal sealed class CanonicalParameterValueFact
        : IEquatable<CanonicalParameterValueFact>
    {
        /// <summary>
        /// Initializes a canonical parameter value fact.
        /// </summary>
        /// <param name="parameterOrdinal">The zero-based parameter ordinal.</param>
        /// <param name="facts">The normalized value facts.</param>
        public CanonicalParameterValueFact(
            int parameterOrdinal,
            ExceptionFlowValueFacts facts)
        {
            ParameterOrdinal = parameterOrdinal;
            Facts = facts.Normalize();
        }

        /// <summary>
        /// Gets the zero-based parameter ordinal.
        /// </summary>
        /// <value>The value described by this property.</value>
        public int ParameterOrdinal { get; }

        /// <summary>
        /// Gets the normalized value facts.
        /// </summary>
        /// <value>The value described by this property.</value>
        public ExceptionFlowValueFacts Facts { get; }

        /// <inheritdoc/>
        public bool Equals(CanonicalParameterValueFact? other)
        {
            return other != null
                && ParameterOrdinal == other.ParameterOrdinal
                && Facts == other.Facts;
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            return Equals(obj as CanonicalParameterValueFact);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return HashCode.Combine(ParameterOrdinal, Facts);
        }
    }

    /// <summary>
    /// Represents a stable non-null member fact for one parameter ordinal.
    /// </summary>
    internal sealed class CanonicalParameterMemberFact
        : IEquatable<CanonicalParameterMemberFact>
    {
        /// <summary>
        /// Initializes a canonical stable-member fact.
        /// </summary>
        /// <param name="parameterOrdinal">The zero-based parameter ordinal.</param>
        /// <param name="member">The canonical stable-member identity.</param>
        /// <exception cref="System.ArgumentNullException">Thrown when the supplied input cannot be processed.</exception>
        public CanonicalParameterMemberFact(
            int parameterOrdinal,
            CanonicalStableMemberIdentity member)
        {
            ArgumentNullException.ThrowIfNull(member);

            ParameterOrdinal = parameterOrdinal;
            Member = member;
        }

        /// <summary>
        /// Gets the zero-based parameter ordinal.
        /// </summary>
        /// <value>The value described by this property.</value>
        public int ParameterOrdinal { get; }

        /// <summary>
        /// Gets the stable-member identity.
        /// </summary>
        /// <value>The value described by this property.</value>
        public CanonicalStableMemberIdentity Member { get; }

        /// <inheritdoc/>
        public bool Equals(CanonicalParameterMemberFact? other)
        {
            return other != null
                && ParameterOrdinal == other.ParameterOrdinal
                && Member.Equals(other.Member);
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            return Equals(obj as CanonicalParameterMemberFact);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return HashCode.Combine(ParameterOrdinal, Member);
        }
    }

    /// <summary>
    /// Represents immutable Roslyn-independent call-site facts.
    /// </summary>
    internal sealed class CanonicalExceptionFlowCallContext
        : IEquatable<CanonicalExceptionFlowCallContext>
    {
        /// <summary>
        /// Stores value facts in canonical ordinal order.
        /// </summary>
        private readonly CanonicalParameterValueFact[] parameterFacts;

        /// <summary>
        /// Stores stable-member facts in canonical structural order.
        /// </summary>
        private readonly CanonicalParameterMemberFact[] memberFacts;

        /// <summary>
        /// Initializes a canonical call context.
        /// </summary>
        /// <param name="callable">The optional callable identity.</param>
        /// <param name="parameterFacts">The parameter value facts.</param>
        /// <param name="memberFacts">The stable-member facts.</param>
        public CanonicalExceptionFlowCallContext(
            CanonicalCallableIdentity? callable,
            CanonicalParameterValueFact[]? parameterFacts,
            CanonicalParameterMemberFact[]? memberFacts)
        {
            Callable = callable;
            this.parameterFacts = (parameterFacts ?? Array.Empty<CanonicalParameterValueFact>())
                .OrderBy(static fact => fact.ParameterOrdinal)
                .ThenBy(static fact => (int)fact.Facts)
                .ToArray();
            this.memberFacts = (memberFacts ?? Array.Empty<CanonicalParameterMemberFact>())
                .OrderBy(static fact => fact.ParameterOrdinal)
                .ThenBy(
                    static fact => CanonicalIdentityKeyWriter.Write(fact.Member),
                    StringComparer.Ordinal)
                .ToArray();
        }

        /// <summary>
        /// Gets the optional callable identity.
        /// </summary>
        /// <value>The value described by this property.</value>
        public CanonicalCallableIdentity? Callable { get; }

        /// <summary>
        /// Gets a copy of the parameter value facts.
        /// </summary>
        /// <value>The value described by this property.</value>
        public CanonicalParameterValueFact[] ParameterFacts => parameterFacts.ToArray();

        /// <summary>
        /// Gets a copy of the stable-member facts.
        /// </summary>
        /// <value>The value described by this property.</value>
        public CanonicalParameterMemberFact[] MemberFacts => memberFacts.ToArray();

        /// <summary>
        /// Gets the deterministic structural key used by the existing summary graph.
        /// </summary>
        /// <value>The value described by this property.</value>
        public string Key => CanonicalIdentityKeyWriter.Write(this);

        /// <inheritdoc/>
        public bool Equals(CanonicalExceptionFlowCallContext? other)
        {
            return other != null
                && Equals(Callable, other.Callable)
                && parameterFacts.SequenceEqual(other.parameterFacts)
                && memberFacts.SequenceEqual(other.memberFacts);
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            return Equals(obj as CanonicalExceptionFlowCallContext);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            HashCode hash = new();
            hash.Add(Callable);

            foreach (CanonicalParameterValueFact fact in parameterFacts)
            {
                hash.Add(fact);
            }

            foreach (CanonicalParameterMemberFact fact in memberFacts)
            {
                hash.Add(fact);
            }

            return hash.ToHashCode();
        }
    }
}
