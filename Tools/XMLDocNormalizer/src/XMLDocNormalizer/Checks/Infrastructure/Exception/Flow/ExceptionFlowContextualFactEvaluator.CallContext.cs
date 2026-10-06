using static XMLDocNormalizer.Checks.Infrastructure.Exception.Flow.ExceptionFlowSymbolUsageFacts;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace XMLDocNormalizer.Checks.Infrastructure.Exception.Flow
{
    /// <summary>
    /// Evaluates contextual value, symbol, sequence, and call facts.
    /// </summary>
    internal static partial class ExceptionFlowContextualFactEvaluator
    {

        /// <summary>
        /// Creates the call context for an invoked method or constructor while
        /// preserving an existing recursive value-source analysis guard.
        /// </summary>
        /// <param name="methodSymbol">
        /// The invoked method or constructor.
        /// </param>
        /// <param name="arguments">
        /// The arguments supplied at the call site.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for expression and constant analysis.
        /// </param>
        /// <param name="callerContext">
        /// The value facts known while analyzing the caller.
        /// </param>
        /// <param name="inspectedValueSources">
        /// The value-producing symbols currently inspected recursively.
        /// </param>
        /// <returns>
        /// The call context containing the value facts proven for the target
        /// parameters.
        /// </returns>
        internal static ExceptionFlowCallContext CreateCallContext(
            IMethodSymbol methodSymbol,
            SeparatedSyntaxList<ArgumentSyntax> arguments,
            SemanticModel semanticModel,
            ExceptionFlowCallContext callerContext,
            HashSet<ISymbol> inspectedValueSources)
        {
            Dictionary<int, ExceptionFlowValueFacts> knownParameterFacts = new();
            List<KeyValuePair<int, ISymbol>> knownNonNullParameterMembers = new();
            HashSet<int> suppliedParameterIndexes = new();

            AddExplicitArgumentFacts(
                methodSymbol,
                arguments,
                semanticModel,
                callerContext,
                knownParameterFacts,
                suppliedParameterIndexes,
                inspectedValueSources);

            ExceptionFlowCallContextFactProjector.AddExplicitArgumentNonNullMemberFacts(
                methodSymbol,
                arguments,
                semanticModel,
                callerContext,
                knownNonNullParameterMembers);

            ExceptionFlowCallContextFactProjector.AddDefaultParameterFacts(
                methodSymbol,
                knownParameterFacts,
                suppliedParameterIndexes);

            return new ExceptionFlowCallContext(
                methodSymbol,
                knownParameterFacts,
                knownNonNullParameterMembers);
        }

        /// <summary>
        /// Adds value facts for explicitly supplied call arguments.
        /// </summary>
        /// <param name="methodSymbol">
        /// The called method or accessor.
        /// </param>
        /// <param name="arguments">
        /// The explicitly supplied arguments.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for value analysis.
        /// </param>
        /// <param name="callerContext">
        /// The facts known in the caller.
        /// </param>
        /// <param name="knownParameterFacts">
        /// The destination parameter-fact map.
        /// </param>
        /// <param name="suppliedParameterIndexes">
        /// The destination set of explicitly supplied parameter indexes.
        /// </param>
        /// <param name="inspectedValueSources">
        /// The value-producing symbols already being inspected recursively, or
        /// <see langword="null"/> to start an independent value-fact analysis.
        /// </param>
        internal static void AddExplicitArgumentFacts(
            IMethodSymbol methodSymbol,
            SeparatedSyntaxList<ArgumentSyntax> arguments,
            SemanticModel semanticModel,
            ExceptionFlowCallContext callerContext,
            Dictionary<int, ExceptionFlowValueFacts> knownParameterFacts,
            HashSet<int> suppliedParameterIndexes,
            HashSet<ISymbol>? inspectedValueSources = null)
        {
            HashSet<ISymbol> effectiveInspectedValueSources =
                inspectedValueSources
                ?? new HashSet<ISymbol>(SymbolEqualityComparer.Default);

            for (int index = 0; index < arguments.Count; index++)
            {
                ArgumentSyntax argument =
                    arguments[index];

                int parameterIndex =
                    ExceptionFlowArgumentMapper.GetParameterIndex(
                        argument,
                        index,
                        methodSymbol);

                if (parameterIndex < 0
                    || parameterIndex >= methodSymbol.Parameters.Length)
                {
                    continue;
                }

                suppliedParameterIndexes.Add(
                    parameterIndex);

                if (argument.RefKindKeyword.IsKind(
                        SyntaxKind.OutKeyword))
                {
                    continue;
                }

                ExceptionFlowValueFacts facts =
                    GetExpressionValueFacts(
                        argument.Expression,
                        semanticModel,
                        callerContext,
                        effectiveInspectedValueSources);

                IParameterSymbol parameterSymbol =
                    methodSymbol.Parameters[parameterIndex];

                if (argument.RefKindKeyword.IsKind(SyntaxKind.None)
                    && !parameterSymbol.IsParams
                    && AreSequenceElementsProvenNonNull(
                        argument.Expression,
                        semanticModel,
                        callerContext,
                        effectiveInspectedValueSources))
                {
                    facts |=
                        ExceptionFlowValueFacts.NonNullElements;
                }

                if (argument.RefKindKeyword.IsKind(SyntaxKind.None)
                    && !parameterSymbol.IsParams
                    && AreDictionaryValuesProvenNonNull(
                        argument.Expression,
                        semanticModel,
                        callerContext))
                {
                    facts |= ExceptionFlowValueFacts.NonNullDictionaryValues;
                }

                if (argument.RefKindKeyword.IsKind(SyntaxKind.None)
                    && !parameterSymbol.IsParams
                    && AreSequenceElementsProvenDefinedEnumValues(
                        argument.Expression,
                        semanticModel,
                        callerContext))
                {
                    facts |=
                        ExceptionFlowValueFacts.DefinedEnumElements;
                }

                if (facts != ExceptionFlowValueFacts.None)
                {
                    knownParameterFacts[parameterIndex] =
                        facts.Normalize();
                }
            }
        }
    }
}
