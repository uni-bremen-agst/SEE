using Microsoft.CodeAnalysis;
using XMLDocNormalizer.Checks.Infrastructure.Exception.Flow.Canonical;

namespace XMLDocNormalizer.Checks.Infrastructure.Exception.Flow
{
    /// <summary>
    /// Identifies one context-sensitive callable node in the exception-flow
    /// summary graph.
    /// </summary>
    internal sealed class ExceptionFlowCallableKey
        : IEquatable<ExceptionFlowCallableKey>
    {
        /// <summary>
        /// Retains the local call context for on-demand canonical transport.
        /// </summary>
        private readonly ExceptionFlowCallContext? callContext;

        /// <summary>
        /// Initializes a new callable key.
        /// </summary>
        /// <param name="symbol">
        /// The callable symbol represented by the graph node.
        /// </param>
        /// <param name="contextKey">
        /// The deterministic key describing the known call-site facts.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="symbol"/> or
        /// <paramref name="contextKey"/> is <see langword="null"/>.
        /// </exception>
        public ExceptionFlowCallableKey(
            ISymbol symbol,
            string contextKey)
        {
            ArgumentNullException.ThrowIfNull(symbol);
            ArgumentNullException.ThrowIfNull(contextKey);

            Symbol = symbol.OriginalDefinition;
            ContextKey = contextKey;
        }

        /// <summary>
        /// Initializes a callable key from a canonicalized call context.
        /// </summary>
        /// <param name="symbol">The local Roslyn callable handle.</param>
        /// <param name="callContext">The canonicalizable call context.</param>
        /// <exception cref="System.ArgumentNullException">Thrown when the supplied input cannot be processed.</exception>
        public ExceptionFlowCallableKey(
            ISymbol symbol,
            ExceptionFlowCallContext callContext)
            : this(
                symbol,
                callContext.Key)
        {
            this.callContext = callContext;
        }

        /// <summary>
        /// Gets the normalized callable symbol.
        /// </summary>
        /// <value>
        /// The callable symbol normalized to its original definition.
        /// </value>
        public ISymbol Symbol { get; }

        /// <summary>
        /// Gets the Roslyn-independent callable identity used for durable key
        /// comparison.
        /// </summary>
        /// <value>The canonical callable identity.</value>
        public CanonicalCallableIdentity? CanonicalIdentity
        {
            get { return TryCreateCanonicalIdentity(Symbol); }
        }

        /// <summary>
        /// Gets the deterministic call-context key.
        /// </summary>
        /// <value>
        /// The key describing the parameter facts known for the callable.
        /// </value>
        public string ContextKey { get; }

        /// <summary>
        /// Gets the canonical call context when this key was created by the
        /// production summary graph.
        /// </summary>
        /// <value>The value described by this property.</value>
        public CanonicalExceptionFlowCallContext? CanonicalContext
        {
            get { return callContext?.CanonicalContext; }
        }

        /// <summary>
        /// Determines whether this key identifies the same callable and call
        /// context as another key.
        /// </summary>
        /// <param name="other">
        /// The other key to compare.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if both keys identify the same normalized
        /// symbol and context; otherwise <see langword="false"/>.
        /// </returns>
        public bool Equals(
            ExceptionFlowCallableKey? other)
        {
            if (ReferenceEquals(
                    this,
                    other))
            {
                return true;
            }

            if (other is null)
            {
                return false;
            }

            return SymbolEqualityComparer.Default.Equals(Symbol, other.Symbol)
                && StringComparer.Ordinal.Equals(ContextKey, other.ContextKey);
        }

        /// <summary>
        /// Determines whether this key equals another object.
        /// </summary>
        /// <param name="obj">
        /// The object to compare.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the object is an equivalent callable
        /// key; otherwise <see langword="false"/>.
        /// </returns>
        public override bool Equals(
            object? obj)
        {
            return obj
                       is ExceptionFlowCallableKey other &&
                   Equals(other);
        }

        /// <summary>
        /// Gets a hash code based on Roslyn symbol identity and the call
        /// context key.
        /// </summary>
        /// <returns>
        /// The hash code for this callable key.
        /// </returns>
        public override int GetHashCode()
        {
            return HashCode.Combine(
                SymbolEqualityComparer.Default.GetHashCode(Symbol),
                StringComparer.Ordinal.GetHashCode(ContextKey));
        }

        /// <summary>
        /// Creates a canonical callable identity without allowing an unsupported
        /// representation to alter the active analyzer's exception behavior.
        /// </summary>
        /// <param name="symbol">The normalized callable symbol.</param>
        /// <returns>The exact canonical identity, or <see langword="null"/> when no exact representation exists.</returns>
        private static CanonicalCallableIdentity? TryCreateCanonicalIdentity(ISymbol symbol)
        {
            try
            {
                return RoslynCanonicalIdentityFactory.CreateCallableIdentity(
                    symbol,
                    normalizeToOriginalDefinition: true);
            }
            catch (ArgumentException)
            {
                return null;
            }
            catch (NotSupportedException)
            {
                return null;
            }
        }

        /// <summary>
        /// Determines whether two callable keys are equal.
        /// </summary>
        /// <param name="left">
        /// The left key.
        /// </param>
        /// <param name="right">
        /// The right key.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the keys are equal; otherwise
        /// <see langword="false"/>.
        /// </returns>
        public static bool operator ==(
            ExceptionFlowCallableKey? left,
            ExceptionFlowCallableKey? right)
        {
            if (ReferenceEquals(
                    left,
                    right))
            {
                return true;
            }

            if (left is null ||
                right is null)
            {
                return false;
            }

            return left.Equals(right);
        }

        /// <summary>
        /// Determines whether two callable keys are different.
        /// </summary>
        /// <param name="left">
        /// The left key.
        /// </param>
        /// <param name="right">
        /// The right key.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the keys are different; otherwise
        /// <see langword="false"/>.
        /// </returns>
        public static bool operator !=(
            ExceptionFlowCallableKey? left,
            ExceptionFlowCallableKey? right)
        {
            return !(left == right);
        }
    }
}
