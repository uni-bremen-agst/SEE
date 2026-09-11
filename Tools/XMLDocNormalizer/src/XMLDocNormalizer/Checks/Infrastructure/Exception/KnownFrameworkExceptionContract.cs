using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using XMLDocNormalizer.Checks.Infrastructure.Exception.Flow;

namespace XMLDocNormalizer.Checks.Infrastructure.Exception
{
    /// <summary>
    /// Identifies whether a curated framework exception contract closes
    /// normal unknown-target analysis for its callable.
    /// </summary>
    internal enum KnownFrameworkExceptionContractCompleteness
    {
        /// <summary>
        /// The contract provides positive evidence without claiming to model
        /// every relevant exception flow of the callable.
        /// </summary>
        Partial,

        /// <summary>
        /// The contract completely models the callable for exception-flow
        /// analysis and replaces unknown-target traversal.
        /// </summary>
        Complete
    }

    /// <summary>
    /// Contains the statically proven information for one argument supplied
    /// to a known framework exception contract.
    /// </summary>
    internal readonly struct KnownFrameworkExceptionContractArgument
    {
        /// <summary>
        /// Initializes argument information for contract evaluation.
        /// </summary>
        /// <param name="facts">The value facts proven for the argument.</param>
        public KnownFrameworkExceptionContractArgument(ExceptionFlowValueFacts facts)
        {
            Facts = facts.Normalize();
        }

        /// <summary>
        /// Gets the value facts proven for the argument.
        /// </summary>
        /// <value>The normalized value facts.</value>
        public ExceptionFlowValueFacts Facts { get; }
    }

    /// <summary>
    /// Contains the immutable possible-exception set produced by one contract
    /// evaluator.
    /// </summary>
    internal readonly struct KnownFrameworkExceptionContractResult
    {
        /// <summary>
        /// Stores the possible exception types.
        /// </summary>
        private readonly ImmutableArray<INamedTypeSymbol> possibleExceptionTypes;

        /// <summary>
        /// Initializes a contract result from an owned immutable collection.
        /// </summary>
        /// <param name="possibleExceptionTypes">
        /// The possible exception types. A default immutable array represents
        /// an empty result.
        /// </param>
        public KnownFrameworkExceptionContractResult(
            ImmutableArray<INamedTypeSymbol> possibleExceptionTypes)
        {
            this.possibleExceptionTypes = possibleExceptionTypes.IsDefault
                ? ImmutableArray<INamedTypeSymbol>.Empty
                : possibleExceptionTypes;
        }

        /// <summary>
        /// Gets exception types that remain possible at the evaluated call site.
        /// </summary>
        /// <value>The immutable possible exception types.</value>
        public ImmutableArray<INamedTypeSymbol> PossibleExceptionTypes =>
            possibleExceptionTypes.IsDefault
                ? ImmutableArray<INamedTypeSymbol>.Empty
                : possibleExceptionTypes;
    }

    /// <summary>
    /// Contains the explicit outcome of looking up and evaluating one curated
    /// framework exception contract at a call site.
    /// </summary>
    internal sealed class KnownFrameworkExceptionContractEvaluation
    {
        /// <summary>
        /// Stores the shared no-match result.
        /// </summary>
        private static readonly KnownFrameworkExceptionContractEvaluation noMatch =
            new(
                isMatch: false,
                completeness: null,
                result: default);

        /// <summary>
        /// Initializes a framework contract evaluation.
        /// </summary>
        /// <param name="isMatch">Whether a registered contract matched.</param>
        /// <param name="completeness">
        /// The matched contract completeness, or <see langword="null"/> when
        /// no contract matched.
        /// </param>
        /// <param name="result">The evaluated possible exceptions.</param>
        private KnownFrameworkExceptionContractEvaluation(
            bool isMatch,
            KnownFrameworkExceptionContractCompleteness? completeness,
            KnownFrameworkExceptionContractResult result)
        {
            IsMatch = isMatch;
            Completeness = completeness;
            Result = result;
        }

        /// <summary>
        /// Gets the evaluation used when no registered contract matches.
        /// </summary>
        /// <value>The shared no-match evaluation.</value>
        public static KnownFrameworkExceptionContractEvaluation NoMatch => noMatch;

        /// <summary>
        /// Gets whether a registered contract matched the callable.
        /// </summary>
        /// <value>
        /// <see langword="true"/> when a contract matched; otherwise
        /// <see langword="false"/>.
        /// </value>
        public bool IsMatch { get; }

        /// <summary>
        /// Gets the completeness of the matched contract.
        /// </summary>
        /// <value>
        /// The contract completeness, or <see langword="null"/> when no
        /// contract matched.
        /// </value>
        public KnownFrameworkExceptionContractCompleteness? Completeness { get; }

        /// <summary>
        /// Gets exception types that remain possible at the evaluated call
        /// site.
        /// </summary>
        /// <value>The possible exception types.</value>
        public IReadOnlyList<INamedTypeSymbol> PossibleExceptionTypes =>
            Result.PossibleExceptionTypes;

        /// <summary>
        /// Gets the immutable evaluated contract result.
        /// </summary>
        /// <value>The evaluated contract result.</value>
        public KnownFrameworkExceptionContractResult Result { get; }

        /// <summary>
        /// Gets whether the evaluation completely replaces normal external
        /// exception-flow analysis.
        /// </summary>
        /// <value>
        /// <see langword="true"/> only for a matched complete contract;
        /// otherwise <see langword="false"/>.
        /// </value>
        public bool ClosesExternalAnalysis =>
            IsMatch
            && Completeness == KnownFrameworkExceptionContractCompleteness.Complete;

        /// <summary>
        /// Creates an evaluation for a matched contract.
        /// </summary>
        /// <param name="completeness">The contract completeness.</param>
        /// <param name="result">The evaluated possible exceptions.</param>
        /// <returns>The matched contract evaluation.</returns>
        public static KnownFrameworkExceptionContractEvaluation CreateMatched(
            KnownFrameworkExceptionContractCompleteness completeness,
            KnownFrameworkExceptionContractResult result)
        {
            return new KnownFrameworkExceptionContractEvaluation(
                isMatch: true,
                completeness,
                result);
        }
    }

    /// <summary>
    /// Represents one registered framework target matcher and its
    /// argument-based exception evaluator.
    /// </summary>
    internal sealed class KnownFrameworkExceptionContract
    {
        /// <summary>
        /// Matches a callable against a framework contract target.
        /// </summary>
        /// <param name="methodSymbol">The callable to inspect.</param>
        /// <param name="compilation">
        /// The compilation used to resolve exact framework symbols.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when the callable is the contract target;
        /// otherwise <see langword="false"/>.
        /// </returns>
        public delegate bool TargetMatcher(IMethodSymbol methodSymbol, Compilation compilation);

        /// <summary>
        /// Evaluates possible exceptions for one matched contract target.
        /// </summary>
        /// <param name="arguments">
        /// Argument facts ordered by parameter ordinal.
        /// </param>
        /// <param name="compilation">
        /// The compilation used to resolve exception symbols.
        /// </param>
        /// <returns>The possible exception types.</returns>
        public delegate KnownFrameworkExceptionContractResult ConditionEvaluator(
            IReadOnlyList<KnownFrameworkExceptionContractArgument> arguments,
            Compilation compilation);

        /// <summary>
        /// Stores the exact target matcher.
        /// </summary>
        private readonly TargetMatcher targetMatcher;

        /// <summary>
        /// Stores the argument-based condition evaluator.
        /// </summary>
        private readonly ConditionEvaluator conditionEvaluator;

        /// <summary>
        /// Initializes a registered framework exception contract.
        /// </summary>
        /// <param name="completeness">The contract completeness.</param>
        /// <param name="targetMatcher">The exact target matcher.</param>
        /// <param name="conditionEvaluator">The condition evaluator.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="targetMatcher"/> or
        /// <paramref name="conditionEvaluator"/> is
        /// <see langword="null"/>.
        /// </exception>
        public KnownFrameworkExceptionContract(
            KnownFrameworkExceptionContractCompleteness completeness,
            TargetMatcher targetMatcher,
            ConditionEvaluator conditionEvaluator)
        {
            ArgumentNullException.ThrowIfNull(targetMatcher);
            ArgumentNullException.ThrowIfNull(conditionEvaluator);

            Completeness = completeness;
            this.targetMatcher = targetMatcher;
            this.conditionEvaluator = conditionEvaluator;
        }

        /// <summary>
        /// Gets whether the contract closes unknown-target analysis.
        /// </summary>
        /// <value>The contract completeness.</value>
        public KnownFrameworkExceptionContractCompleteness Completeness { get; }

        /// <summary>
        /// Determines whether this contract matches a callable exactly.
        /// </summary>
        /// <param name="methodSymbol">The callable to inspect.</param>
        /// <param name="compilation">
        /// The compilation used to resolve framework symbols.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when the callable matches; otherwise
        /// <see langword="false"/>.
        /// </returns>
        public bool Matches(IMethodSymbol methodSymbol, Compilation compilation)
        {
            return targetMatcher(methodSymbol, compilation);
        }

        /// <summary>
        /// Evaluates this contract for arguments ordered by parameter ordinal.
        /// </summary>
        /// <param name="arguments">The argument information.</param>
        /// <param name="compilation">
        /// The compilation used to resolve exception symbols.
        /// </param>
        /// <returns>The explicit matched contract evaluation.</returns>
        public KnownFrameworkExceptionContractEvaluation Evaluate(
            IReadOnlyList<KnownFrameworkExceptionContractArgument> arguments,
            Compilation compilation)
        {
            KnownFrameworkExceptionContractResult result =
                conditionEvaluator(arguments, compilation);

            return KnownFrameworkExceptionContractEvaluation.CreateMatched(
                Completeness,
                result);
        }
    }
}
