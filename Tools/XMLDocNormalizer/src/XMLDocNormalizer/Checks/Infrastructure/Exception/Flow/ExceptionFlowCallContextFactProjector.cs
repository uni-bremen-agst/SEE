using static XMLDocNormalizer.Checks.Infrastructure.Exception.Flow.ExceptionFlowSymbolUsageFacts;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace XMLDocNormalizer.Checks.Infrastructure.Exception.Flow
{
    /// <summary>
    /// Projects stable-member and omitted-parameter facts into call-context
    /// construction data.
    /// </summary>
    internal static class ExceptionFlowCallContextFactProjector
    {
        /// <summary>
        /// Adds stable non-null member facts for explicitly supplied call
        /// arguments.
        /// </summary>
        /// <param name="methodSymbol">The called method.</param>
        /// <param name="arguments">The explicitly supplied arguments.</param>
        /// <param name="semanticModel">
        /// The semantic model used for symbol and flow analysis.
        /// </param>
        /// <param name="callerContext">
        /// The facts known while analyzing the caller.
        /// </param>
        /// <param name="knownNonNullParameterMembers">
        /// The destination parameter-member fact collection.
        /// </param>
        internal static void AddExplicitArgumentNonNullMemberFacts(
            IMethodSymbol methodSymbol,
            SeparatedSyntaxList<ArgumentSyntax> arguments,
            SemanticModel semanticModel,
            ExceptionFlowCallContext callerContext,
            List<KeyValuePair<int, ISymbol>> knownNonNullParameterMembers)
        {
            for (int index = 0; index < arguments.Count; index++)
            {
                ArgumentSyntax argument = arguments[index];

                int parameterIndex = ExceptionFlowArgumentMapper.GetParameterIndex(
                    argument,
                    index,
                    methodSymbol);

                if (parameterIndex < 0 || parameterIndex >= methodSymbol.Parameters.Length)
                {
                    continue;
                }

                IParameterSymbol targetParameter = methodSymbol.Parameters[parameterIndex];

                if (!argument.RefKindKeyword.IsKind(SyntaxKind.None)
                    || targetParameter.RefKind != RefKind.None
                    || targetParameter.IsParams)
                {
                    continue;
                }

                ExpressionSyntax argumentExpression =
                    UnwrapParenthesizedExpression(argument.Expression);

                SymbolInfo argumentSymbolInfo =
                    semanticModel.GetSymbolInfo(argumentExpression);

                if (argumentSymbolInfo.Symbol is IParameterSymbol sourceParameter
                    && ExceptionFlowDereferenceFactDiscovery.IsParameterValueStillCurrentSinceEntry(
                        argumentExpression,
                        sourceParameter,
                        semanticModel))
                {
                    foreach (ISymbol memberSymbol
                             in callerContext.GetKnownNonNullParameterMembers(sourceParameter))
                    {
                        knownNonNullParameterMembers.Add(
                            new KeyValuePair<int, ISymbol>(
                                parameterIndex,
                                memberSymbol));
                    }
                }

                IReadOnlyCollection<IPropertySymbol> locallyProvenProperties =
                    ExceptionFlowDereferenceFactDiscovery.GetStablePropertiesProvenNonNullByPrecedingSuccessfulDereference(
                        argumentExpression,
                        semanticModel);

                foreach (IPropertySymbol propertySymbol in locallyProvenProperties)
                {
                    knownNonNullParameterMembers.Add(
                        new KeyValuePair<int, ISymbol>(
                            parameterIndex,
                            propertySymbol));
                }

                IReadOnlyCollection<ISymbol> sourceMembers =
                    ExceptionFlowStableSourceMemberFactsProvider.GetStableNonNullMemberFactsFromGuardedLocalSourceInvocation(
                        argumentExpression,
                        semanticModel);

                foreach (ISymbol memberSymbol in sourceMembers)
                {
                    knownNonNullParameterMembers.Add(
                        new KeyValuePair<int, ISymbol>(
                            parameterIndex,
                            memberSymbol));
                }
            }
        }

        /// <summary>
        /// Adds facts implied by omitted optional and <c>params</c> parameters.
        /// </summary>
        /// <param name="methodSymbol">The called method or accessor.</param>
        /// <param name="knownParameterFacts">
        /// The destination parameter-fact map.
        /// </param>
        /// <param name="suppliedParameterIndexes">
        /// The explicitly supplied parameter indexes.
        /// </param>
        internal static void AddDefaultParameterFacts(
            IMethodSymbol methodSymbol,
            Dictionary<int, ExceptionFlowValueFacts> knownParameterFacts,
            HashSet<int> suppliedParameterIndexes)
        {
            foreach (IParameterSymbol parameterSymbol in methodSymbol.Parameters)
            {
                if (suppliedParameterIndexes.Contains(parameterSymbol.Ordinal))
                {
                    continue;
                }

                ExceptionFlowValueFacts facts = ExceptionFlowValueFacts.None;

                if (parameterSymbol.IsParams)
                {
                    facts = ExceptionFlowValueFacts.NonNull;
                }
                else if (parameterSymbol.HasExplicitDefaultValue)
                {
                    facts = ExceptionFlowPrimitiveValueFactsProvider.GetConstantValueFacts(
                        parameterSymbol.ExplicitDefaultValue);
                }

                if (facts != ExceptionFlowValueFacts.None)
                {
                    knownParameterFacts[parameterSymbol.Ordinal] = facts;
                }
            }
        }

        /// <summary>
        /// Determines whether a foreach iteration variable is non-null because
        /// its source parameter received a sequence-element fact at the call
        /// site.
        /// </summary>
        /// <param name="expression">
        /// The iteration-variable usage being analyzed.
        /// </param>
        /// <param name="localSymbol">
        /// The iteration-variable symbol.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model of the callable body.
        /// </param>
        /// <param name="callContext">
        /// The parameter facts known for the callable.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when the iteration variable is proven
        /// non-null; otherwise <see langword="false"/>.
        /// </returns>
        internal static bool IsForeachIterationVariableProvenNonNullByCallContext(
            ExpressionSyntax expression,
            ILocalSymbol localSymbol,
            SemanticModel semanticModel,
            ExceptionFlowCallContext callContext)
        {
            IEnumerable<ForEachStatementSyntax> enclosingStatements =
                expression.Ancestors()
                    .OfType<ForEachStatementSyntax>();

            foreach (ForEachStatementSyntax foreachStatement
                     in enclosingStatements)
            {
                ISymbol? iterationVariable =
                    semanticModel.GetDeclaredSymbol(
                        foreachStatement);

                if (!SymbolEqualityComparer.Default.Equals(
                        iterationVariable,
                        localSymbol))
                {
                    continue;
                }

                ExpressionSyntax sourceExpression =
                    UnwrapParenthesizedExpression(
                        foreachStatement.Expression);

                SymbolInfo sourceSymbolInfo =
                    semanticModel.GetSymbolInfo(sourceExpression);

                if (sourceSymbolInfo.Symbol
                        is not IParameterSymbol parameterSymbol
                    || !callContext.GetParameterFacts(parameterSymbol)
                        .ContainsAll(
                            ExceptionFlowValueFacts.NonNullElements))
                {
                    return false;
                }

                return ExceptionFlowSequenceContentPreservationFactsProvider.IsSequenceParameterFactStillCurrent(
                    foreachStatement,
                    parameterSymbol,
                    semanticModel);
            }

            return false;
        }
    }
}
