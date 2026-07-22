using Microsoft.CodeAnalysis;

namespace XMLDocNormalizer.Checks.Infrastructure.Exception.Flow
{
    /// <summary>
    /// Describes call-site facts that are known while analyzing a callable symbol.
    /// </summary>
    internal sealed class ExceptionFlowCallContext
    {
        /// <summary>
        /// Maps parameter indexes to the value facts proven for the current call.
        /// </summary>
        private readonly Dictionary<int, ExceptionFlowValueFacts> parameterFacts;

        /// <summary>
        /// Initializes an empty call context for the specified callable symbol.
        /// </summary>
        /// <param name="callableSymbol">
        /// The callable symbol whose body is being analyzed.
        /// </param>
        public ExceptionFlowCallContext(ISymbol? callableSymbol)
            : this(callableSymbol, Array.Empty<KeyValuePair<int, ExceptionFlowValueFacts>>())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExceptionFlowCallContext"/> class.
        /// </summary>
        /// <param name="callableSymbol">The callable symbol whose body is being analyzed.</param>
        /// <param name="knownParameterFacts">
        /// The value facts proven for parameters at the call site.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="knownParameterFacts"/> is
        /// <see langword="null"/>.
        /// </exception>
        public ExceptionFlowCallContext(
            ISymbol? callableSymbol,
            IEnumerable<KeyValuePair<int, ExceptionFlowValueFacts>> knownParameterFacts)
        {
            ArgumentNullException.ThrowIfNull(knownParameterFacts);

            CallableSymbol = NormalizeSymbol(callableSymbol);
            parameterFacts = new Dictionary<int, ExceptionFlowValueFacts>();

            foreach (KeyValuePair<int, ExceptionFlowValueFacts> pair
                     in knownParameterFacts)
            {
                if (pair.Key < 0)
                {
                    continue;
                }

                ExceptionFlowValueFacts normalizedFacts =
                    pair.Value.Normalize();

                if (normalizedFacts == ExceptionFlowValueFacts.None)
                {
                    continue;
                }

                if (parameterFacts.TryGetValue(
                        pair.Key,
                        out ExceptionFlowValueFacts existingFacts))
                {
                    parameterFacts[pair.Key] =
                        (existingFacts | normalizedFacts).Normalize();
                }
                else
                {
                    parameterFacts.Add(
                        pair.Key,
                        normalizedFacts);
                }
            }

            Key = string.Join(
                ",",
                parameterFacts
                    .OrderBy(static pair => pair.Key)
                    .Select(static pair =>
                        $"{pair.Key}:{(int)pair.Value}"));
        }

        /// <summary>
        /// Gets the callable symbol whose body is being analyzed.
        /// </summary>
        /// <value>
        /// The normalized callable symbol, or <see langword="null"/> if the context
        /// is not associated with a callable symbol.
        /// </value>
        public ISymbol? CallableSymbol { get; }

        /// <summary>
        /// Gets a deterministic key that identifies the known parameter facts.
        /// </summary>
        /// <value>
        /// A comma-separated sequence of sorted parameter indexes and their fact values,
        /// or an empty string if no parameter facts are known.
        /// </value>
        public string Key { get; }

        /// <summary>
        /// Gets the value facts proven for the specified parameter.
        /// </summary>
        /// <param name="parameterSymbol">The parameter symbol to inspect.</param>
        /// <returns>
        /// The proven facts if the parameter belongs to the current callable symbol;
        /// otherwise <see cref="ExceptionFlowValueFacts.None"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="parameterSymbol"/> is <see langword="null"/>.
        /// </exception>
        public ExceptionFlowValueFacts GetParameterFacts(
            IParameterSymbol parameterSymbol)
        {
            ArgumentNullException.ThrowIfNull(parameterSymbol);

            ISymbol normalizedContainingSymbol =
                NormalizeSymbol(parameterSymbol.ContainingSymbol) ??
                parameterSymbol.ContainingSymbol;

            if (CallableSymbol == null ||
                !SymbolEqualityComparer.Default.Equals(
                    CallableSymbol,
                    normalizedContainingSymbol))
            {
                return ExceptionFlowValueFacts.None;
            }

            return parameterFacts.TryGetValue(
                parameterSymbol.Ordinal,
                out ExceptionFlowValueFacts facts)
                    ? facts
                    : ExceptionFlowValueFacts.None;
        }

        /// <summary>
        /// Determines whether the specified parameter is proven to be non-null in this context.
        /// </summary>
        /// <param name="parameterSymbol">The parameter symbol to inspect.</param>
        /// <returns>
        /// <see langword="true"/> if the parameter belongs to the current callable symbol and
        /// is proven to be non-null; otherwise <see langword="false"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="parameterSymbol"/> is <see langword="null"/>.
        /// </exception>
        public bool IsParameterKnownNonNull(
            IParameterSymbol parameterSymbol)
        {
            return GetParameterFacts(parameterSymbol)
                .ContainsAll(ExceptionFlowValueFacts.NonNull);
        }

        /// <summary>
        /// Creates parameter facts from a sequence of indexes known to be non-null.
        /// </summary>
        /// <param name="knownNonNullParameterIndexes">
        /// The indexes to convert into non-null facts.
        /// </param>
        /// <returns>The created parameter facts.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="knownNonNullParameterIndexes"/> is
        /// <see langword="null"/>.
        /// </exception>
        private static IEnumerable<KeyValuePair<int, ExceptionFlowValueFacts>>
            CreateNonNullParameterFacts(
                IEnumerable<int> knownNonNullParameterIndexes)
        {
            ArgumentNullException.ThrowIfNull(knownNonNullParameterIndexes);

            return knownNonNullParameterIndexes.Select(
                static index =>
                    new KeyValuePair<int, ExceptionFlowValueFacts>(
                        index,
                        ExceptionFlowValueFacts.NonNull));
        }

        /// <summary>
        /// Normalizes a symbol so that constructed generic symbols and their declarations
        /// share the same identity during exception-flow analysis.
        /// </summary>
        /// <param name="symbol">The symbol to normalize.</param>
        /// <returns>
        /// The normalized symbol, or <see langword="null"/> if no symbol was supplied.
        /// </returns>
        private static ISymbol? NormalizeSymbol(ISymbol? symbol)
        {
            return symbol?.OriginalDefinition;
        }
    }
}
