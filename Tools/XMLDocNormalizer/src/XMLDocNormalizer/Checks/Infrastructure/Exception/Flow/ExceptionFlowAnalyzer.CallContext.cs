using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace XMLDocNormalizer.Checks.Infrastructure.Exception.Flow
{
    /// <summary>
    /// Contains creation and mapping of exception-flow call contexts.
    /// </summary>
    internal static partial class ExceptionFlowAnalyzer
    {
        /// <summary>
        /// Creates the initial call context for a top-level member analysis.
        /// </summary>
        /// <param name="member">
        /// The member whose body is analyzed.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used to resolve the member symbol.
        /// </param>
        /// <returns>
        /// A call context without assumed parameter facts.
        /// </returns>
        private static ExceptionFlowCallContext CreateRootCallContext(
            MemberDeclarationSyntax member,
            SemanticModel semanticModel)
        {
            ISymbol? memberSymbol =
                semanticModel.GetDeclaredSymbol(
                    member);

            return new ExceptionFlowCallContext(
                memberSymbol);
        }

        /// <summary>
        /// Creates the call context for an invoked method or constructor.
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
        /// <returns>
        /// The call context containing the value facts proven for the target
        /// parameters.
        /// </returns>
        private static ExceptionFlowCallContext CreateCallContext(
            IMethodSymbol methodSymbol,
            SeparatedSyntaxList<ArgumentSyntax> arguments,
            SemanticModel semanticModel,
            ExceptionFlowCallContext callerContext)
        {
            HashSet<ISymbol> inspectedValueSources =
                new(SymbolEqualityComparer.Default);

            return CreateCallContext(
                methodSymbol,
                arguments,
                semanticModel,
                callerContext,
                inspectedValueSources);
        }

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
        private static ExceptionFlowCallContext CreateCallContext(
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

            AddExplicitArgumentNonNullMemberFacts(
                methodSymbol,
                arguments,
                semanticModel,
                callerContext,
                knownNonNullParameterMembers);

            AddDefaultParameterFacts(
                methodSymbol,
                knownParameterFacts,
                suppliedParameterIndexes);

            return new ExceptionFlowCallContext(
                methodSymbol,
                knownParameterFacts,
                knownNonNullParameterMembers);
        }

        /// <summary>
        /// Creates the call context for one runtime dispatch target while
        /// retaining the compile-time argument-to-parameter mapping and
        /// optional default values of the call site.
        /// </summary>
        /// <param name="callSiteMethodSymbol">
        /// The method selected by compile-time binding.
        /// </param>
        /// <param name="targetMethodSymbol">
        /// The runtime target whose body will be analyzed.
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
        /// <returns>
        /// A target context whose facts use the runtime target's parameter
        /// ordinals while preserving compile-time named-argument and optional
        /// default semantics.
        /// </returns>
        private static ExceptionFlowCallContext
            CreateDispatchCallContext(
                IMethodSymbol callSiteMethodSymbol,
                IMethodSymbol targetMethodSymbol,
                SeparatedSyntaxList<ArgumentSyntax> arguments,
                SemanticModel semanticModel,
                ExceptionFlowCallContext callerContext)
        {
            ExceptionFlowCallContext callSiteContext =
                CreateCallContext(
                    callSiteMethodSymbol,
                    arguments,
                    semanticModel,
                    callerContext);

            return CreateDispatchTargetContext(
                callSiteMethodSymbol,
                targetMethodSymbol,
                callSiteContext);
        }

        /// <summary>
        /// Transfers parameter facts from a statically selected callable to
        /// one runtime target by parameter ordinal.
        /// </summary>
        /// <param name="callSiteMethodSymbol">
        /// The method or accessor selected by compile-time binding.
        /// </param>
        /// <param name="targetMethodSymbol">
        /// The runtime target receiving the facts.
        /// </param>
        /// <param name="callSiteContext">
        /// The context already created for the statically selected callable.
        /// </param>
        /// <returns>
        /// A context associated with the runtime target and containing every
        /// transferable parameter fact.
        /// </returns>
        private static ExceptionFlowCallContext
            CreateDispatchTargetContext(
                IMethodSymbol callSiteMethodSymbol,
                IMethodSymbol targetMethodSymbol,
                ExceptionFlowCallContext callSiteContext)
        {
            Dictionary<int, ExceptionFlowValueFacts>
                knownParameterFacts =
                    new();

            int parameterCount =
                Math.Min(
                    callSiteMethodSymbol.Parameters.Length,
                    targetMethodSymbol.Parameters.Length);

            for (int parameterIndex = 0;
                 parameterIndex < parameterCount;
                 parameterIndex++)
            {
                ExceptionFlowValueFacts facts =
                    callSiteContext.GetParameterFacts(
                        parameterIndex);

                if (facts !=
                    ExceptionFlowValueFacts.None)
                {
                    knownParameterFacts[parameterIndex] =
                        facts;
                }
            }

            return new ExceptionFlowCallContext(
                targetMethodSymbol,
                knownParameterFacts);
        }

        /// <summary>
        /// Creates the call context for a property, indexer, or event
        /// accessor receiving a synthetic <c>value</c> parameter.
        /// </summary>
        /// <param name="accessorSymbol">
        /// The setter, init, add, or remove accessor.
        /// </param>
        /// <param name="indexArguments">
        /// The explicit indexer arguments, or an empty list for properties
        /// and events.
        /// </param>
        /// <param name="valueExpression">
        /// The expression supplied to the accessor's <c>value</c> parameter,
        /// or <see langword="null"/> when the final value cannot be proven
        /// from the syntax.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for value analysis.
        /// </param>
        /// <param name="callerContext">
        /// The value facts known while analyzing the caller.
        /// </param>
        /// <returns>
        /// The context containing proven index and value-parameter facts.
        /// </returns>
        private static ExceptionFlowCallContext
            CreateAccessorCallContext(
                IMethodSymbol accessorSymbol,
                SeparatedSyntaxList<ArgumentSyntax> indexArguments,
                ExpressionSyntax? valueExpression,
                SemanticModel semanticModel,
                ExceptionFlowCallContext callerContext)
        {
            Dictionary<int, ExceptionFlowValueFacts>
                knownParameterFacts =
                    new();

            HashSet<int> suppliedParameterIndexes =
                new();

            AddExplicitArgumentFacts(
                accessorSymbol,
                indexArguments,
                semanticModel,
                callerContext,
                knownParameterFacts,
                suppliedParameterIndexes);

            if (accessorSymbol.Parameters.Length > 0)
            {
                int valueParameterIndex =
                    accessorSymbol.Parameters.Length - 1;

                suppliedParameterIndexes.Add(
                    valueParameterIndex);

                if (valueExpression != null)
                {
                    ExceptionFlowValueFacts valueFacts =
                        GetExpressionValueFacts(
                            valueExpression,
                            semanticModel,
                            callerContext);

                    if (valueFacts !=
                        ExceptionFlowValueFacts.None)
                    {
                        knownParameterFacts[valueParameterIndex] =
                            valueFacts;
                    }
                }
            }

            AddDefaultParameterFacts(
                accessorSymbol,
                knownParameterFacts,
                suppliedParameterIndexes);

            return new ExceptionFlowCallContext(
                accessorSymbol,
                knownParameterFacts);
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
        private static void AddExplicitArgumentFacts(
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
                    GetParameterIndexForArgument(
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
                        callerContext))
                {
                    facts |=
                        ExceptionFlowValueFacts.NonNullElements;
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

        /// <summary>
        /// Adds stable non-null member facts for explicitly supplied call arguments.
        /// </summary>
        /// <param name="methodSymbol">
        /// The called method.
        /// </param>
        /// <param name="arguments">
        /// The explicitly supplied arguments.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for symbol and flow analysis.
        /// </param>
        /// <param name="callerContext">
        /// The facts known while analyzing the caller.
        /// </param>
        /// <param name="knownNonNullParameterMembers">
        /// The destination parameter-member fact collection.
        /// </param>
        private static void AddExplicitArgumentNonNullMemberFacts(
            IMethodSymbol methodSymbol,
            SeparatedSyntaxList<ArgumentSyntax> arguments,
            SemanticModel semanticModel,
            ExceptionFlowCallContext callerContext,
            List<KeyValuePair<int, ISymbol>> knownNonNullParameterMembers)
        {
            for (int index = 0; index < arguments.Count; index++)
            {
                ArgumentSyntax argument = arguments[index];

                int parameterIndex = GetParameterIndexForArgument(
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
                    && IsParameterValueStillCurrentSinceEntry(
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
                    GetStablePropertiesProvenNonNullByPrecedingSuccessfulDereference(
                        argumentExpression,
                        semanticModel);

                foreach (IPropertySymbol propertySymbol in locallyProvenProperties)
                {
                    knownNonNullParameterMembers.Add(
                        new KeyValuePair<int, ISymbol>(
                            parameterIndex,
                            propertySymbol));
                }

                IReadOnlyCollection<ISymbol> sourceMembers = GetStableNonNullMemberFactsFromGuardedLocalSourceInvocation(
                    argumentExpression, semanticModel);

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
        /// Adds facts implied by omitted optional and <c>params</c>
        /// parameters.
        /// </summary>
        /// <param name="methodSymbol">
        /// The called method or accessor.
        /// </param>
        /// <param name="knownParameterFacts">
        /// The destination parameter-fact map.
        /// </param>
        /// <param name="suppliedParameterIndexes">
        /// The explicitly supplied parameter indexes.
        /// </param>
        private static void AddDefaultParameterFacts(
            IMethodSymbol methodSymbol,
            Dictionary<int, ExceptionFlowValueFacts>
                knownParameterFacts,
            HashSet<int> suppliedParameterIndexes)
        {
            foreach (IParameterSymbol parameterSymbol
                     in methodSymbol.Parameters)
            {
                if (suppliedParameterIndexes.Contains(
                        parameterSymbol.Ordinal))
                {
                    continue;
                }

                ExceptionFlowValueFacts facts =
                    ExceptionFlowValueFacts.None;

                if (parameterSymbol.IsParams)
                {
                    facts =
                        ExceptionFlowValueFacts.NonNull;
                }
                else if (parameterSymbol.HasExplicitDefaultValue)
                {
                    facts =
                        GetConstantValueFacts(
                            parameterSymbol.ExplicitDefaultValue);
                }

                if (facts !=
                    ExceptionFlowValueFacts.None)
                {
                    knownParameterFacts[parameterSymbol.Ordinal] =
                        facts;
                }
            }
        }

        /// <summary>
        /// Gets the effective target parameter index for an argument, taking
        /// named arguments into account.
        /// </summary>
        /// <param name="argument">
        /// The argument to inspect.
        /// </param>
        /// <param name="fallbackIndex">
        /// The positional fallback index.
        /// </param>
        /// <param name="methodSymbol">
        /// The target method symbol.
        /// </param>
        /// <returns>
        /// The resolved parameter index, or the fallback index if no named
        /// match exists.
        /// </returns>
        private static int GetParameterIndexForArgument(
            ArgumentSyntax argument,
            int fallbackIndex,
            IMethodSymbol methodSymbol)
        {
            if (argument.NameColon == null)
            {
                return fallbackIndex;
            }

            string name =
                argument.NameColon.Name.Identifier.ValueText;

            for (int index = 0;
                 index < methodSymbol.Parameters.Length;
                 index++)
            {
                if (string.Equals(
                        methodSymbol.Parameters[index].Name,
                        name,
                        StringComparison.Ordinal))
                {
                    return index;
                }
            }

            return fallbackIndex;
        }
    }
}
