using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace XMLDocNormalizer.Checks.Infrastructure.Exception.Flow
{
    /// <summary>
    /// Contains narrowly scoped source-value analysis used to transfer stable
    /// member facts into exception-flow call contexts.
    /// </summary>
    internal static partial class ExceptionFlowAnalyzer
    {
        /// <summary>
        /// Gets stable member facts for a guarded local whose still-current
        /// initializer is a directly bound source invocation returning a newly
        /// initialized object.
        /// </summary>
        /// <param name="expression">
        /// The local expression passed as an argument.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for local, invocation, and return analysis.
        /// </param>
        /// <returns>
        /// Stable member symbols proven non-null on every supported non-null
        /// return value of the source invocation.
        /// </returns>
        private static IReadOnlyCollection<ISymbol>
            GetStableNonNullMemberFactsFromGuardedLocalSourceInvocation(
                ExpressionSyntax expression,
                SemanticModel semanticModel)
        {
            ExpressionSyntax unwrappedExpression =
                UnwrapParenthesizedExpression(expression);

            if (unwrappedExpression is not IdentifierNameSyntax)
            {
                return Array.Empty<ISymbol>();
            }

            SymbolInfo symbolInfo = semanticModel.GetSymbolInfo(unwrappedExpression);

            if (symbolInfo.Symbol is not ILocalSymbol localSymbol
                || !GetFactsProvenByPrecedingGuard(
                        unwrappedExpression,
                        localSymbol,
                        semanticModel)
                    .ContainsAll(ExceptionFlowValueFacts.NonNull))
            {
                return Array.Empty<ISymbol>();
            }

            if (localSymbol.DeclaringSyntaxReferences.Length != 1
                || localSymbol.DeclaringSyntaxReferences[0].GetSyntax()
                    is not VariableDeclaratorSyntax declarator
                || declarator.Initializer?.Value is not InvocationExpressionSyntax)
            {
                return Array.Empty<ISymbol>();
            }

            if (!TryGetStraightLineCurrentLocalInitializerExpression(
                    unwrappedExpression,
                    localSymbol,
                    semanticModel,
                    out ExpressionSyntax? initializer)
                || initializer is not InvocationExpressionSyntax invocation)
            {
                return Array.Empty<ISymbol>();
            }

            return GetStableNonNullMemberFactsFromDirectSourceInvocation(
                invocation,
                semanticModel);
        }

        /// <summary>
        /// Gets stable member facts shared by every directly returned non-null
        /// object creation of a source method.
        /// </summary>
        /// <param name="invocation">
        /// The directly bound source invocation.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model associated with the invocation.
        /// </param>
        /// <returns>
        /// Stable member symbols proven non-null on every supported non-null
        /// return value.
        /// </returns>
        private static IReadOnlyCollection<ISymbol> GetStableNonNullMemberFactsFromDirectSourceInvocation(
            InvocationExpressionSyntax invocation,
            SemanticModel semanticModel)
        {
            SymbolInfo symbolInfo = semanticModel.GetSymbolInfo(invocation);

            if (symbolInfo.Symbol is not IMethodSymbol selectedMethod
                || selectedMethod.ReducedFrom != null
                || selectedMethod.ReturnsVoid
                || selectedMethod.IsAsync
                || selectedMethod.IsExtern
                || selectedMethod.IsAbstract
                || selectedMethod.IsIterator
                || selectedMethod.ReturnsByRef
                || selectedMethod.ReturnsByRefReadonly
                || RequiresSummaryRuntimeDispatch(selectedMethod))
            {
                return Array.Empty<ISymbol>();
            }

            IMethodSymbol targetMethod = selectedMethod.OriginalDefinition;

            if (targetMethod.DeclaringSyntaxReferences.Length != 1)
            {
                return Array.Empty<ISymbol>();
            }

            SyntaxNode declaration = targetMethod.DeclaringSyntaxReferences[0].GetSyntax();
            List<ExpressionSyntax> returnExpressions = GetSourceReturnExpressions(declaration);

            if (returnExpressions.Count == 0)
            {
                return Array.Empty<ISymbol>();
            }

            HashSet<ISymbol>? commonMembers = null;
            bool foundNonNullReturn = false;

            foreach (ExpressionSyntax returnExpression in returnExpressions)
            {
                SemanticModel? returnSemanticModel = GetSemanticModelForSyntaxTree(
                    semanticModel,
                    returnExpression.SyntaxTree);

                if (returnSemanticModel == null)
                {
                    return Array.Empty<ISymbol>();
                }

                Optional<object?> constantValue =
                    returnSemanticModel.GetConstantValue(returnExpression);

                if (constantValue.HasValue && constantValue.Value == null)
                {
                    continue;
                }

                if (!TryGetDirectObjectCreationInitializer(returnExpression, out InitializerExpressionSyntax? initializer)
                    || initializer == null)
                {
                    return Array.Empty<ISymbol>();
                }

                HashSet<ISymbol> currentMembers = new(
                    GetStableNonNullMembersFromObjectInitializer(
                        initializer,
                        returnSemanticModel),
                    SymbolEqualityComparer.Default);

                if (commonMembers == null)
                {
                    commonMembers = currentMembers;
                }
                else
                {
                    commonMembers.IntersectWith(currentMembers);
                }

                foundNonNullReturn = true;
            }

            return foundNonNullReturn
                ? commonMembers?.ToArray() ?? Array.Empty<ISymbol>()
                : Array.Empty<ISymbol>();
        }

        /// <summary>
        /// Gets the initializer of a directly returned explicit or implicit
        /// object creation.
        /// </summary>
        /// <param name="expression">
        /// The return expression to inspect.
        /// </param>
        /// <param name="initializer">
        /// The object initializer when supported.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when the expression directly creates an
        /// object with an initializer; otherwise <see langword="false"/>.
        /// </returns>
        private static bool TryGetDirectObjectCreationInitializer(
            ExpressionSyntax expression,
            out InitializerExpressionSyntax? initializer)
        {
            ExpressionSyntax unwrappedExpression =
                UnwrapParenthesizedExpression(expression);

            switch (unwrappedExpression)
            {
                case ObjectCreationExpressionSyntax objectCreation:
                    initializer = objectCreation.Initializer;
                    return initializer != null;

                case ImplicitObjectCreationExpressionSyntax implicitObjectCreation:
                    initializer = implicitObjectCreation.Initializer;
                    return initializer != null;

                default:
                    initializer = null;
                    return false;
            }
        }

        /// <summary>
        /// Gets stable properties explicitly assigned values that are proven
        /// non-null at an object initializer.
        /// </summary>
        /// <param name="initializer">
        /// The object initializer to inspect.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model associated with the initializer.
        /// </param>
        /// <returns>
        /// Stable member symbols whose assigned values are proven non-null.
        /// </returns>
        private static IReadOnlyCollection<ISymbol>
            GetStableNonNullMembersFromObjectInitializer(
                InitializerExpressionSyntax initializer,
                SemanticModel semanticModel)
        {
            HashSet<ISymbol> members = new(SymbolEqualityComparer.Default);

            foreach (ExpressionSyntax initializerExpression in initializer.Expressions)
            {
                if (initializerExpression is not AssignmentExpressionSyntax assignment)
                {
                    continue;
                }

                SymbolInfo memberSymbolInfo = semanticModel.GetSymbolInfo(assignment.Left);

                if (memberSymbolInfo.Symbol is not IPropertySymbol propertySymbol
                    || !IsSupportedStableAutoProperty(propertySymbol)
                    || !IsObjectInitializerValueProvenNonNull(
                        assignment.Right,
                        semanticModel))
                {
                    continue;
                }

                members.Add(propertySymbol.OriginalDefinition);
            }

            return members;
        }

        /// <summary>
        /// Determines whether an object-initializer value is proven non-null
        /// without recursively analyzing source invocation return values.
        /// </summary>
        /// <param name="expression">
        /// The assigned initializer value.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for constant and guard analysis.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when the value is proven non-null; otherwise
        /// <see langword="false"/>.
        /// </returns>
        private static bool IsObjectInitializerValueProvenNonNull(
            ExpressionSyntax expression,
            SemanticModel semanticModel)
        {
            ExpressionSyntax unwrappedExpression =
                UnwrapParenthesizedExpression(expression);

            Optional<object?> constantValue =
                semanticModel.GetConstantValue(unwrappedExpression);

            if (constantValue.HasValue)
            {
                return constantValue.Value != null;
            }

            if (unwrappedExpression is ObjectCreationExpressionSyntax
                || unwrappedExpression is ImplicitObjectCreationExpressionSyntax
                || unwrappedExpression is ArrayCreationExpressionSyntax
                || unwrappedExpression is ImplicitArrayCreationExpressionSyntax
                || unwrappedExpression is AnonymousObjectCreationExpressionSyntax
                || unwrappedExpression is InterpolatedStringExpressionSyntax
                || unwrappedExpression is TypeOfExpressionSyntax)
            {
                return true;
            }

            SymbolInfo symbolInfo = semanticModel.GetSymbolInfo(unwrappedExpression);

            if (symbolInfo.Symbol is not ILocalSymbol
                && symbolInfo.Symbol is not IParameterSymbol)
            {
                return false;
            }

            return GetFactsProvenByPrecedingGuard(
                    unwrappedExpression,
                    symbolInfo.Symbol,
                    semanticModel)
                .ContainsAll(ExceptionFlowValueFacts.NonNull);
        }
    }
}
