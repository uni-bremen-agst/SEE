using Microsoft.CodeAnalysis;

namespace XMLDocNormalizer.Checks.Infrastructure.Exception.Flow
{
    /// <summary>
    /// Describes call-site facts that are known while analyzing a callable
    /// symbol.
    /// </summary>
    internal sealed class ExceptionFlowCallContext
    {
        /// <summary>
        /// Maps parameter indexes to the value facts proven for the current
        /// call.
        /// </summary>
        private readonly Dictionary<int, ExceptionFlowValueFacts> parameterFacts;

        /// <summary>
        /// Maps parameter indexes to stable members that are proven non-null
        /// for the corresponding parameter value at method entry.
        /// </summary>
        private readonly Dictionary<int, HashSet<ISymbol>> nonNullParameterMembers;

        /// <summary>
        /// Initializes an empty call context for the specified callable
        /// symbol.
        /// </summary>
        /// <param name="callableSymbol">
        /// The callable symbol whose body is being analyzed.
        /// </param>
        public ExceptionFlowCallContext(ISymbol? callableSymbol)
            : this(
                callableSymbol,
                Array.Empty<KeyValuePair<int, ExceptionFlowValueFacts>>(),
                Array.Empty<KeyValuePair<int, ISymbol>>())
        {
        }

        /// <summary>
        /// Initializes a call context with parameter value facts.
        /// </summary>
        /// <param name="callableSymbol">
        /// The callable symbol whose body is being analyzed.
        /// </param>
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
            : this(
                callableSymbol,
                knownParameterFacts,
                Array.Empty<KeyValuePair<int, ISymbol>>())
        {
        }

        /// <summary>
        /// Initializes a call context with parameter and stable parameter-member
        /// facts.
        /// </summary>
        /// <param name="callableSymbol">
        /// The callable symbol whose body is being analyzed.
        /// </param>
        /// <param name="knownParameterFacts">
        /// The value facts proven for parameters at the call site.
        /// </param>
        /// <param name="knownNonNullParameterMembers">
        /// Stable members proven non-null for parameter values at the call site.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="knownParameterFacts"/> or
        /// <paramref name="knownNonNullParameterMembers"/> is
        /// <see langword="null"/>.
        /// </exception>
        public ExceptionFlowCallContext(
            ISymbol? callableSymbol,
            IEnumerable<KeyValuePair<int, ExceptionFlowValueFacts>> knownParameterFacts,
            IEnumerable<KeyValuePair<int, ISymbol>> knownNonNullParameterMembers)
        {
            ArgumentNullException.ThrowIfNull(knownParameterFacts);
            ArgumentNullException.ThrowIfNull(knownNonNullParameterMembers);

            CallableSymbol = NormalizeSymbol(callableSymbol);

            parameterFacts = new Dictionary<int, ExceptionFlowValueFacts>();
            nonNullParameterMembers = new Dictionary<int, HashSet<ISymbol>>();

            foreach (KeyValuePair<int, ExceptionFlowValueFacts> pair in knownParameterFacts)
            {
                if (pair.Key < 0)
                {
                    continue;
                }

                ExceptionFlowValueFacts normalizedFacts = pair.Value.Normalize();

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
                    parameterFacts.Add(pair.Key, normalizedFacts);
                }
            }

            foreach (KeyValuePair<int, ISymbol> pair in knownNonNullParameterMembers)
            {
                if (pair.Key < 0)
                {
                    continue;
                }

                ISymbol normalizedMember = NormalizeSymbol(pair.Value) ?? pair.Value;

                if (!nonNullParameterMembers.TryGetValue(
                        pair.Key,
                        out HashSet<ISymbol>? members))
                {
                    members = new HashSet<ISymbol>(SymbolEqualityComparer.Default);
                    nonNullParameterMembers.Add(pair.Key, members);
                }

                members.Add(normalizedMember);
            }

            List<string> keyParts = parameterFacts
                .Select(
                    static pair =>
                        $"p:{pair.Key}:{(int)pair.Value}")
                .ToList();

            foreach (KeyValuePair<int, HashSet<ISymbol>> parameterPair
                     in nonNullParameterMembers)
            {
                foreach (ISymbol memberSymbol in parameterPair.Value)
                {
                    string memberName = memberSymbol.ToDisplayString(
                        SymbolDisplayFormat.FullyQualifiedFormat);

                    keyParts.Add(
                        $"m:{parameterPair.Key}:{memberSymbol.Kind}:{memberName}");
                }
            }

            keyParts.Sort(StringComparer.Ordinal);
            Key = string.Join(",", keyParts);
        }

        /// <summary>
        /// Gets the callable symbol whose body is being analyzed.
        /// </summary>
        /// <value>
        /// The normalized callable symbol, or
        /// <see langword="null"/> if the context is not associated with a
        /// callable symbol.
        /// </value>
        public ISymbol? CallableSymbol { get; }

        /// <summary>
        /// Gets a deterministic key that identifies the known parameter and
        /// parameter-member facts.
        /// </summary>
        /// <value>
        /// The deterministic call-context key.
        /// </value>
        public string Key { get; }

        /// <summary>
        /// Gets the value facts stored for one parameter ordinal.
        /// </summary>
        /// <param name="parameterIndex">
        /// The zero-based parameter ordinal.
        /// </param>
        /// <returns>
        /// The stored facts, or
        /// <see cref="ExceptionFlowValueFacts.None"/> when no facts are
        /// registered for the ordinal.
        /// </returns>
        public ExceptionFlowValueFacts GetParameterFacts(int parameterIndex)
        {
            return parameterFacts.TryGetValue(
                parameterIndex,
                out ExceptionFlowValueFacts facts)
                    ? facts
                    : ExceptionFlowValueFacts.None;
        }

        /// <summary>
        /// Gets the value facts proven for the specified parameter.
        /// </summary>
        /// <param name="parameterSymbol">
        /// The parameter symbol to inspect.
        /// </param>
        /// <returns>
        /// The proven facts if the parameter belongs to the current callable
        /// symbol; otherwise
        /// <see cref="ExceptionFlowValueFacts.None"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="parameterSymbol"/> is
        /// <see langword="null"/>.
        /// </exception>
        public ExceptionFlowValueFacts GetParameterFacts(IParameterSymbol parameterSymbol)
        {
            ArgumentNullException.ThrowIfNull(parameterSymbol);

            if (!ParameterBelongsToCurrentCallable(parameterSymbol))
            {
                return ExceptionFlowValueFacts.None;
            }

            return GetParameterFacts(parameterSymbol.Ordinal);
        }

        /// <summary>
        /// Gets the stable members known non-null for one parameter ordinal.
        /// </summary>
        /// <param name="parameterIndex">
        /// The zero-based parameter ordinal.
        /// </param>
        /// <returns>
        /// The known non-null member symbols, or an empty collection when no
        /// member facts are registered.
        /// </returns>
        public IReadOnlyCollection<ISymbol> GetKnownNonNullParameterMembers(int parameterIndex)
        {
            return nonNullParameterMembers.TryGetValue(
                parameterIndex,
                out HashSet<ISymbol>? members)
                    ? members
                    : Array.Empty<ISymbol>();
        }

        /// <summary>
        /// Gets the stable members known non-null for the specified parameter.
        /// </summary>
        /// <param name="parameterSymbol">
        /// The parameter whose member facts are requested.
        /// </param>
        /// <returns>
        /// The known non-null member symbols, or an empty collection when the
        /// parameter does not belong to the current callable or no member facts
        /// are registered.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="parameterSymbol"/> is
        /// <see langword="null"/>.
        /// </exception>
        public IReadOnlyCollection<ISymbol> GetKnownNonNullParameterMembers(
            IParameterSymbol parameterSymbol)
        {
            ArgumentNullException.ThrowIfNull(parameterSymbol);

            if (!ParameterBelongsToCurrentCallable(parameterSymbol))
            {
                return Array.Empty<ISymbol>();
            }

            return GetKnownNonNullParameterMembers(parameterSymbol.Ordinal);
        }

        /// <summary>
        /// Determines whether a stable member of the specified parameter is
        /// proven non-null at method entry.
        /// </summary>
        /// <param name="parameterSymbol">
        /// The parameter that receives the containing object.
        /// </param>
        /// <param name="memberSymbol">
        /// The stable member whose fact is requested.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when the member is proven non-null;
        /// otherwise <see langword="false"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="parameterSymbol"/> or
        /// <paramref name="memberSymbol"/> is <see langword="null"/>.
        /// </exception>
        public bool IsParameterMemberKnownNonNull(
            IParameterSymbol parameterSymbol,
            ISymbol memberSymbol)
        {
            ArgumentNullException.ThrowIfNull(parameterSymbol);
            ArgumentNullException.ThrowIfNull(memberSymbol);

            if (!ParameterBelongsToCurrentCallable(parameterSymbol)
                || !nonNullParameterMembers.TryGetValue(
                    parameterSymbol.Ordinal,
                    out HashSet<ISymbol>? members))
            {
                return false;
            }

            ISymbol normalizedMember = NormalizeSymbol(memberSymbol) ?? memberSymbol;

            return members.Contains(normalizedMember);
        }

        /// <summary>
        /// Determines whether the specified parameter is proven to be
        /// non-null in this context.
        /// </summary>
        /// <param name="parameterSymbol">
        /// The parameter symbol to inspect.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the parameter belongs to the current
        /// callable symbol and is proven to be non-null; otherwise
        /// <see langword="false"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="parameterSymbol"/> is
        /// <see langword="null"/>.
        /// </exception>
        public bool IsParameterKnownNonNull(IParameterSymbol parameterSymbol)
        {
            return GetParameterFacts(parameterSymbol)
                .ContainsAll(ExceptionFlowValueFacts.NonNull);
        }

        /// <summary>
        /// Determines whether a parameter belongs to the callable represented
        /// by this context.
        /// </summary>
        /// <param name="parameterSymbol">
        /// The parameter to inspect.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when the parameter belongs to the current
        /// callable; otherwise <see langword="false"/>.
        /// </returns>
        private bool ParameterBelongsToCurrentCallable(
            IParameterSymbol parameterSymbol)
        {
            ISymbol normalizedContainingSymbol =
                NormalizeSymbol(parameterSymbol.ContainingSymbol)
                ?? parameterSymbol.ContainingSymbol;

            return CallableSymbol != null
                && SymbolEqualityComparer.Default.Equals(
                    CallableSymbol,
                    normalizedContainingSymbol);
        }

        /// <summary>
        /// Creates parameter facts from a sequence of indexes known to be
        /// non-null.
        /// </summary>
        /// <param name="knownNonNullParameterIndexes">
        /// The indexes to convert into non-null facts.
        /// </param>
        /// <returns>
        /// The created parameter facts.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="knownNonNullParameterIndexes"/> is
        /// <see langword="null"/>.
        /// </exception>
        private static IEnumerable<KeyValuePair<int, ExceptionFlowValueFacts>>
            CreateNonNullParameterFacts(IEnumerable<int> knownNonNullParameterIndexes)
        {
            ArgumentNullException.ThrowIfNull(knownNonNullParameterIndexes);

            return knownNonNullParameterIndexes.Select(
                static index =>
                    new KeyValuePair<int, ExceptionFlowValueFacts>(
                        index,
                        ExceptionFlowValueFacts.NonNull));
        }

        /// <summary>
        /// Normalizes a symbol so that constructed generic symbols and their
        /// declarations share the same identity during exception-flow
        /// analysis.
        /// </summary>
        /// <param name="symbol">
        /// The symbol to normalize.
        /// </param>
        /// <returns>
        /// The normalized symbol, or <see langword="null"/> if no symbol was
        /// supplied.
        /// </returns>
        private static ISymbol? NormalizeSymbol(ISymbol? symbol)
        {
            return symbol?.OriginalDefinition;
        }
    }
}
