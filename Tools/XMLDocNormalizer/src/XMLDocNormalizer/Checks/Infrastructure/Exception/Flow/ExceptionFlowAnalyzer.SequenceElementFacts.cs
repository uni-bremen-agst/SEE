using static XMLDocNormalizer.Checks.Infrastructure.Exception.Flow.ExceptionFlowSymbolUsageFacts;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ExceptionFlowDataFlowFacts = XMLDocNormalizer.Checks.Infrastructure.Exception.Flow.ExceptionFlowDataFlowFactsProvider.ExceptionFlowDataFlowFacts;

namespace XMLDocNormalizer.Checks.Infrastructure.Exception.Flow
{
    /// <summary>
    /// Contains sequence-element non-null reasoning used by exception-flow
    /// analysis.
    /// </summary>
    internal static partial class ExceptionFlowAnalyzer
    {
        /// <summary>
        /// Determines whether a local sequence expression still represents an
        /// initializer whose elements are proven to exclude
        /// <see langword="null"/>.
        /// </summary>
        /// <param name="expression">
        /// The local sequence expression being inspected.
        /// </param>
        /// <param name="localSymbol">
        /// The local symbol represented by the expression.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model associated with the use site.
        /// </param>
        /// <param name="callContext">
        /// The call-site facts known for the current callable.
        /// </param>
        /// <param name="inspectedSequenceSources">
        /// The sequence-producing symbols currently being inspected.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when the local still contains an initializer
        /// whose sequence excludes <see langword="null"/> elements; otherwise
        /// <see langword="false"/>.
        /// </returns>
        private static bool
     IsLocalSequenceExpressionProvenToExcludeNullElements(
         ExpressionSyntax expression,
         ILocalSymbol localSymbol,
         SemanticModel semanticModel,
         ExceptionFlowCallContext callContext,
         HashSet<ISymbol> inspectedSequenceSources)
        {
            if (localSymbol.DeclaringSyntaxReferences.Length != 1 ||
                !inspectedSequenceSources.Add(localSymbol))
            {
                return false;
            }

            try
            {
                SyntaxNode declarationNode =
                    localSymbol.DeclaringSyntaxReferences[0]
                        .GetSyntax();

                SemanticModel? declarationSemanticModel =
                    ExceptionFlowSemanticScope.GetSemanticModelForSyntaxTree(
                        semanticModel,
                        declarationNode.SyntaxTree);

                if (declarationSemanticModel == null)
                {
                    return false;
                }

                if (IsForeachGroupingLocalProvenToExcludeNullElements(
                        localSymbol,
                        declarationNode,
                        declarationSemanticModel,
                        callContext,
                        inspectedSequenceSources))
                {
                    return true;
                }

                if (declarationNode
                        is not VariableDeclaratorSyntax variableDeclarator ||
                    variableDeclarator.Initializer == null)
                {
                    return false;
                }

                if (IsLocalListWithRangeAddsProvenToExcludeNullElements(
                        expression,
                        localSymbol,
                        declarationSemanticModel,
                        callContext,
                        inspectedSequenceSources))
                {
                    return true;
                }

                if (!ExceptionFlowSequenceContentPreservationFactsProvider.IsLocalSequenceInitializerStillCurrent(
                        expression,
                        localSymbol,
                        variableDeclarator,
                        semanticModel))
                {
                    return false;
                }

                return IsSequenceExpressionProvenToExcludeNullElements(
                    variableDeclarator.Initializer.Value,
                    declarationSemanticModel,
                    callContext,
                    inspectedSequenceSources);
            }
            finally
            {
                inspectedSequenceSources.Remove(localSymbol);
            }
        }









        /// <summary>
        /// Determines whether a dictionary <c>Values</c> expression is proven
        /// to contain no <see langword="null"/> elements because the local
        /// dictionary starts empty and every insertion supplies a value proven
        /// to be non-null.
        /// </summary>
        /// <param name="memberAccess">
        /// The possible dictionary <c>Values</c> access.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for symbol and value analysis.
        /// </param>
        /// <param name="inspectedSequenceSources">
        /// The sequence and collection symbols currently being inspected.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when the values are proven to exclude
        /// <see langword="null"/>; otherwise <see langword="false"/>.
        /// </returns>
        private static bool
            IsDictionaryValuesExpressionProvenToExcludeNullElements(
                MemberAccessExpressionSyntax memberAccess,
                SemanticModel semanticModel,
                HashSet<ISymbol> inspectedSequenceSources)
        {
            SymbolInfo memberSymbolInfo =
                semanticModel.GetSymbolInfo(
                    memberAccess);

            if (memberSymbolInfo.Symbol
                    is not IPropertySymbol propertySymbol ||
                !ExceptionFlowSequenceCollectionFactsProvider.IsDictionaryValuesProperty(
                    propertySymbol))
            {
                return false;
            }

            ExpressionSyntax dictionaryExpression =
                UnwrapParenthesizedExpression(
                    memberAccess.Expression);

            SymbolInfo dictionarySymbolInfo =
                semanticModel.GetSymbolInfo(
                    dictionaryExpression);

            if (dictionarySymbolInfo.Symbol
                is not ILocalSymbol dictionaryLocal)
            {
                return false;
            }

            return IsLocalDictionaryProvenToExcludeNullValues(
                dictionaryLocal,
                memberAccess,
                semanticModel,
                inspectedSequenceSources);
        }


        /// <summary>
        /// Determines whether a local dictionary is proven to exclude null
        /// values when its <c>Values</c> collection is consumed.
        /// </summary>
        /// <param name="dictionaryLocal">
        /// The dictionary local to inspect.
        /// </param>
        /// <param name="valuesAccess">
        /// The <c>Values</c> access whose dictionary is being analyzed.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model associated with the access.
        /// </param>
        /// <param name="inspectedSequenceSources">
        /// The sequence and collection symbols currently being inspected.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when the local starts empty, does not escape
        /// through an unsupported use, and every insertion is proven non-null;
        /// otherwise <see langword="false"/>.
        /// </returns>
        private static bool IsLocalDictionaryProvenToExcludeNullValues(
            ILocalSymbol dictionaryLocal,
            MemberAccessExpressionSyntax valuesAccess,
            SemanticModel semanticModel,
            HashSet<ISymbol> inspectedSequenceSources)
        {
            if (!ExceptionFlowSequenceCollectionFactsProvider.IsDictionaryType(
                    dictionaryLocal.Type) ||
                dictionaryLocal.DeclaringSyntaxReferences.Length != 1 ||
                !inspectedSequenceSources.Add(
                    dictionaryLocal))
            {
                return false;
            }

            try
            {
                SyntaxNode declarationNode =
                    dictionaryLocal.DeclaringSyntaxReferences[0]
                        .GetSyntax();

                if (declarationNode
                        is not VariableDeclaratorSyntax variableDeclarator ||
                    variableDeclarator.Initializer == null)
                {
                    return false;
                }

                SemanticModel? declarationSemanticModel =
                    ExceptionFlowSemanticScope.GetSemanticModelForSyntaxTree(
                        semanticModel,
                        variableDeclarator.SyntaxTree);

                if (declarationSemanticModel == null ||
                    !ExceptionFlowSequenceCollectionFactsProvider.IsKnownEmptyDictionaryCreation(
                        variableDeclarator.Initializer.Value,
                        declarationSemanticModel))
                {
                    return false;
                }

                SyntaxNode? containingCallable =
                    variableDeclarator.Ancestors()
                        .FirstOrDefault(
                            static node =>
                                node is MethodDeclarationSyntax ||
                                node is LocalFunctionStatementSyntax);

                if (containingCallable == null ||
                    containingCallable.SyntaxTree !=
                        valuesAccess.SyntaxTree)
                {
                    return false;
                }

                IEnumerable<IdentifierNameSyntax> references =
                    containingCallable.DescendantNodes()
                        .OfType<IdentifierNameSyntax>()
                        .Where(
                            identifier =>
                                identifier.SpanStart >
                                    variableDeclarator.Span.End &&
                                ExpressionReferencesSymbol(
                                    identifier,
                                    dictionaryLocal,
                                    declarationSemanticModel));

                foreach (IdentifierNameSyntax reference
                         in references)
                {
                    if (!IsDictionaryReferenceSafeForNonNullValues(
                            reference,
                            declarationSemanticModel,
                            inspectedSequenceSources))
                    {
                        return false;
                    }
                }

                return true;
            }
            finally
            {
                inspectedSequenceSources.Remove(
                    dictionaryLocal);
            }
        }




        /// <summary>
        /// Determines whether one dictionary reference preserves the invariant
        /// that no null value is inserted or escapes through an unsupported
        /// mutation path.
        /// </summary>
        /// <param name="reference">
        /// The dictionary local or parameter reference.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model associated with the reference.
        /// </param>
        /// <param name="inspectedSequenceSources">
        /// The collection symbols currently being inspected recursively.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when the reference is a safe values read, a
        /// proven non-null insertion, or a supported source-helper argument;
        /// otherwise <see langword="false"/>.
        /// </returns>
        private static bool IsDictionaryReferenceSafeForNonNullValues(
            IdentifierNameSyntax reference,
            SemanticModel semanticModel,
            HashSet<ISymbol> inspectedSequenceSources)
        {
            if (reference.Parent is CommonForEachStatementSyntax foreachStatement
                && ReferenceEquals(foreachStatement.Expression, reference))
            {
                return true;
            }

            if (reference.Parent is MemberAccessExpressionSyntax memberAccess
                && ReferenceEquals(memberAccess.Expression, reference))
            {
                SymbolInfo memberSymbolInfo =
                    semanticModel.GetSymbolInfo(
                        memberAccess);

                if (memberSymbolInfo.Symbol
                        is IPropertySymbol propertySymbol &&
                    ExceptionFlowSequenceCollectionFactsProvider.IsDictionaryValuesProperty(
                        propertySymbol))
                {
                    return true;
                }

                if (memberAccess.Parent
                        is InvocationExpressionSyntax invocation &&
                    ReferenceEquals(
                        invocation.Expression,
                        memberAccess))
                {
                    return IsDictionaryMemberInvocationSafeForNonNullValues(
                        invocation,
                        semanticModel);
                }

                return false;
            }

            if (reference.Parent
                    is ArgumentSyntax argument &&
                ReferenceEquals(
                    argument.Expression,
                    reference) &&
                argument.Parent?.Parent
                    is InvocationExpressionSyntax helperInvocation)
            {
                return IsDictionarySourceHelperArgumentSafeForNonNullValues(
                    argument,
                    helperInvocation,
                    semanticModel,
                    inspectedSequenceSources);
            }

            return false;
        }

        /// <summary>
        /// Determines whether a direct framework dictionary invocation
        /// preserves the non-null value invariant.
        /// </summary>
        /// <param name="invocation">
        /// The dictionary invocation to inspect.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for method and value analysis.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when the invocation removes entries or
        /// inserts a value proven to be non-null; otherwise
        /// <see langword="false"/>.
        /// </returns>
        private static bool IsDictionaryMemberInvocationSafeForNonNullValues(
            InvocationExpressionSyntax invocation,
            SemanticModel semanticModel)
        {
            SymbolInfo symbolInfo =
                semanticModel.GetSymbolInfo(
                    invocation);

            if (symbolInfo.Symbol is not IMethodSymbol methodSymbol
                || !ExceptionFlowSequenceCollectionFactsProvider.IsDictionaryType(methodSymbol.ContainingType))
            {
                return false;
            }

            if (string.Equals(
                    methodSymbol.Name,
                    "Clear",
                    StringComparison.Ordinal)
                || string.Equals(
                    methodSymbol.Name,
                    "Remove",
                    StringComparison.Ordinal)
                || string.Equals(
                    methodSymbol.Name,
                    "TryGetValue",
                    StringComparison.Ordinal)
                || string.Equals(
                    methodSymbol.Name,
                    "ContainsKey",
                    StringComparison.Ordinal)
                || string.Equals(
                    methodSymbol.Name,
                    "ContainsValue",
                    StringComparison.Ordinal))
            {
                return true;
            }

            if (!string.Equals(
                    methodSymbol.Name,
                    "Add",
                    StringComparison.Ordinal) &&
                !string.Equals(
                    methodSymbol.Name,
                    "TryAdd",
                    StringComparison.Ordinal))
            {
                return false;
            }

            for (int argumentIndex = 0;
                 argumentIndex <
                    invocation.ArgumentList.Arguments.Count;
                 argumentIndex++)
            {
                ArgumentSyntax argument =
                    invocation.ArgumentList.Arguments[argumentIndex];

                int parameterIndex =
                    ExceptionFlowArgumentMapper.GetParameterIndex(
                        argument,
                        argumentIndex,
                        methodSymbol);

                if (parameterIndex != 1)
                {
                    continue;
                }

                return IsDictionaryInsertionValueProvenNonNull(
                    argument.Expression,
                    semanticModel);
            }

            return false;
        }

        /// <summary>
        /// Determines whether passing a dictionary to one statically bound
        /// source helper preserves the absence of null values.
        /// </summary>
        /// <param name="argument">
        /// The dictionary argument.
        /// </param>
        /// <param name="invocation">
        /// The helper invocation containing the argument.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model associated with the call site.
        /// </param>
        /// <param name="inspectedSequenceSources">
        /// The collection symbols currently being inspected recursively.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when the helper has a single analyzable
        /// source declaration and every use of the corresponding dictionary
        /// parameter preserves the invariant; otherwise
        /// <see langword="false"/>.
        /// </returns>
        private static bool
            IsDictionarySourceHelperArgumentSafeForNonNullValues(
                ArgumentSyntax argument,
                InvocationExpressionSyntax invocation,
                SemanticModel semanticModel,
                HashSet<ISymbol> inspectedSequenceSources)
        {
            if (!argument.RefKindKeyword.IsKind(
                    SyntaxKind.None))
            {
                return false;
            }

            SymbolInfo symbolInfo =
                semanticModel.GetSymbolInfo(
                    invocation);

            if (symbolInfo.Symbol
                    is not IMethodSymbol selectedMethod ||
                selectedMethod.ReducedFrom != null ||
                !selectedMethod.IsStatic ||
                selectedMethod.IsAbstract ||
                selectedMethod.IsExtern ||
                ExceptionFlowRuntimeDispatchClassifier.RequiresRuntimeDispatch(
                    selectedMethod) ||
                selectedMethod.DeclaringSyntaxReferences.Length != 1)
            {
                return false;
            }

            int fallbackIndex =
                invocation.ArgumentList.Arguments.IndexOf(
                    argument);

            if (fallbackIndex < 0)
            {
                return false;
            }

            int parameterIndex =
                ExceptionFlowArgumentMapper.GetParameterIndex(
                    argument,
                    fallbackIndex,
                    selectedMethod);

            if (parameterIndex < 0 ||
                parameterIndex >=
                    selectedMethod.Parameters.Length)
            {
                return false;
            }

            IParameterSymbol parameterSymbol =
                selectedMethod.Parameters[parameterIndex];

            if (parameterSymbol.RefKind !=
                    RefKind.None ||
                !ExceptionFlowSequenceCollectionFactsProvider.IsDictionaryType(
                    parameterSymbol.Type))
            {
                return false;
            }

            return DoesSourceDictionaryParameterPreserveNonNullValues(
                parameterSymbol,
                semanticModel,
                inspectedSequenceSources);
        }

        /// <summary>
        /// Determines whether all uses of one source-helper dictionary
        /// parameter preserve the invariant that existing non-null values stay
        /// free of null insertions.
        /// </summary>
        /// <param name="parameterSymbol">
        /// The dictionary parameter to inspect.
        /// </param>
        /// <param name="semanticModel">
        /// A semantic model from the caller compilation.
        /// </param>
        /// <param name="inspectedSequenceSources">
        /// The collection symbols currently being inspected recursively.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when every parameter use is a supported safe
        /// operation; otherwise <see langword="false"/>.
        /// </returns>
        private static bool
            DoesSourceDictionaryParameterPreserveNonNullValues(
                IParameterSymbol parameterSymbol,
                SemanticModel semanticModel,
                HashSet<ISymbol> inspectedSequenceSources)
        {
            if (parameterSymbol.DeclaringSyntaxReferences.Length != 1 ||
                !inspectedSequenceSources.Add(
                    parameterSymbol))
            {
                return false;
            }

            try
            {
                SyntaxNode parameterDeclaration =
                    parameterSymbol.DeclaringSyntaxReferences[0]
                        .GetSyntax();

                SyntaxNode? containingCallable =
                    parameterDeclaration.Ancestors()
                        .FirstOrDefault(
                            static node =>
                                node is MethodDeclarationSyntax ||
                                node is LocalFunctionStatementSyntax);

                if (containingCallable == null)
                {
                    return false;
                }

                SemanticModel? declarationSemanticModel =
                    ExceptionFlowSemanticScope.GetSemanticModelForSyntaxTree(
                        semanticModel,
                        containingCallable.SyntaxTree);

                if (declarationSemanticModel == null)
                {
                    return false;
                }

                IEnumerable<IdentifierNameSyntax> references =
                    containingCallable.DescendantNodes()
                        .OfType<IdentifierNameSyntax>()
                        .Where(
                            identifier =>
                                ExpressionReferencesSymbol(
                                    identifier,
                                    parameterSymbol,
                                    declarationSemanticModel));

                foreach (IdentifierNameSyntax reference
                         in references)
                {
                    if (!IsDictionaryReferenceSafeForNonNullValues(
                            reference,
                            declarationSemanticModel,
                            inspectedSequenceSources))
                    {
                        return false;
                    }
                }

                return true;
            }
            finally
            {
                inspectedSequenceSources.Remove(
                    parameterSymbol);
            }
        }
    }
}
