using static XMLDocNormalizer.Checks.Infrastructure.Exception.Flow.ExceptionFlowSymbolUsageFacts;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using XMLDocNormalizer.Checks.Infrastructure.Exception;

namespace XMLDocNormalizer.Checks.Infrastructure.Exception.Flow
{
    /// <summary>
    /// Contains non-null reasoning used during exception-flow analysis.
    /// </summary>
    internal static partial class ExceptionFlowAnalyzer
    {
        /// <summary>
        /// Determines whether an expression is proven to evaluate to a
        /// non-null value without relying only on nullable reference-type
        /// annotations.
        /// </summary>
        /// <param name="expression">
        /// The expression to inspect.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for symbol and constant resolution.
        /// </param>
        /// <param name="callContext">
        /// The call-site facts known for the current callable.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the expression is proven to be non-null;
        /// otherwise <see langword="false"/>.
        /// </returns>
        internal static bool IsDefinitelyNonNull(
            ExpressionSyntax expression,
            SemanticModel semanticModel,
            ExceptionFlowCallContext callContext)
        {
            HashSet<ISymbol> inspectedReturnSymbols =
                new(SymbolEqualityComparer.Default);

            return IsDefinitelyNonNull(
                expression,
                semanticModel,
                callContext,
                inspectedReturnSymbols);
        }

        /// <summary>
        /// Determines whether an expression is proven to evaluate to a
        /// non-null value while preventing recursive return-value analysis.
        /// </summary>
        /// <param name="expression">
        /// The expression to inspect.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for symbol and constant resolution.
        /// </param>
        /// <param name="callContext">
        /// The call-site facts known for the current callable.
        /// </param>
        /// <param name="inspectedReturnSymbols">
        /// The method symbols whose return values are currently being
        /// inspected.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the expression is proven to be non-null;
        /// otherwise <see langword="false"/>.
        /// </returns>
        internal static bool IsDefinitelyNonNull(
            ExpressionSyntax expression,
            SemanticModel semanticModel,
            ExceptionFlowCallContext callContext,
            HashSet<ISymbol> inspectedReturnSymbols)
        {
            Optional<object?> constantValue = semanticModel.GetConstantValue(expression);

            if (constantValue.HasValue && constantValue.Value != null)
            {
                return true;
            }

            TypeInfo typeInfo = semanticModel.GetTypeInfo(expression);
            ITypeSymbol? expressionType = typeInfo.ConvertedType ?? typeInfo.Type;

            if (expressionType != null
                && expressionType.IsValueType
                && !ExceptionFlowNullabilityFactsProvider.IsNullableValueType(
                    expressionType))
            {
                return true;
            }

            Conversion conversion = semanticModel.GetConversion(expression);

            if (conversion.IsMethodGroup && expressionType?.TypeKind == TypeKind.Delegate)
            {
                return true;
            }

            switch (expression)
            {
                case ParenthesizedExpressionSyntax parenthesizedExpression:
                    return IsDefinitelyNonNull(
                        parenthesizedExpression.Expression,
                        semanticModel,
                        callContext,
                        inspectedReturnSymbols);

                case CastExpressionSyntax castExpression:
                    return IsDefinitelyNonNull(
                        castExpression.Expression,
                        semanticModel,
                        callContext,
                        inspectedReturnSymbols);

                case CheckedExpressionSyntax checkedExpression:
                    return IsDefinitelyNonNull(
                        checkedExpression.Expression,
                        semanticModel,
                        callContext,
                        inspectedReturnSymbols);

                case ObjectCreationExpressionSyntax:
                case ImplicitObjectCreationExpressionSyntax:
                case AnonymousObjectCreationExpressionSyntax:
                case ArrayCreationExpressionSyntax:
                case ImplicitArrayCreationExpressionSyntax:
                case StackAllocArrayCreationExpressionSyntax:
                case ThisExpressionSyntax:
                case BaseExpressionSyntax:
                case TypeOfExpressionSyntax:
                case InterpolatedStringExpressionSyntax:
                case AnonymousFunctionExpressionSyntax:
                    return true;

                case ConditionalExpressionSyntax conditionalExpression:
                    return IsDefinitelyNonNull(
                            conditionalExpression.WhenTrue,
                            semanticModel,
                            callContext,
                            inspectedReturnSymbols)
                        && IsDefinitelyNonNull(
                            conditionalExpression.WhenFalse,
                            semanticModel,
                            callContext,
                            inspectedReturnSymbols);

                case BinaryExpressionSyntax binaryExpression
                    when binaryExpression.IsKind(SyntaxKind.CoalesceExpression):
                    return IsDefinitelyNonNull(
                        binaryExpression.Right,
                        semanticModel,
                        callContext,
                        inspectedReturnSymbols);

                case InvocationExpressionSyntax invocation:
                    return IsInvocationResultDefinitelyNonNull(
                        invocation,
                        semanticModel,
                        callContext,
                        inspectedReturnSymbols);
            }

            SymbolInfo symbolInfo = semanticModel.GetSymbolInfo(expression);

            if (symbolInfo.Symbol is IParameterSymbol parameterSymbol)
            {
                if (callContext.IsParameterKnownNonNull(parameterSymbol))
                {
                    return true;
                }

                ExceptionFlowValueFacts guardFacts = ExceptionFlowGuardFactsProvider
                    .GetFactsProvenByPrecedingGuard(
                    expression,
                    parameterSymbol,
                    semanticModel);

                if (guardFacts.ContainsAll(ExceptionFlowValueFacts.NonNull))
                {
                    return true;
                }

                ExceptionFlowValueFacts dereferenceFacts = ExceptionFlowDereferenceFactDiscovery.GetFactsProvenByPrecedingSuccessfulDereference(
                    expression,
                    parameterSymbol,
                    semanticModel);

                return dereferenceFacts.ContainsAll(ExceptionFlowValueFacts.NonNull);
            }

            if (symbolInfo.Symbol is ILocalSymbol localSymbol)
            {
                return IsLocalGuaranteedNonNull(
                    expression,
                    localSymbol,
                    semanticModel,
                    callContext,
                    inspectedReturnSymbols);
            }

            if (symbolInfo.Symbol is IFieldSymbol fieldSymbol
                && fieldSymbol.IsStatic
                && fieldSymbol.Name == nameof(string.Empty)
                && fieldSymbol.ContainingType.SpecialType == SpecialType.System_String)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Determines whether a local variable is guaranteed to contain a
        /// non-null value because it was introduced by a non-null pattern,
        /// initialized with a value proven to be non-null, obtained from a
        /// type-filtered sequence, or protected by an earlier terminating
        /// null guard.
        /// </summary>
        /// <param name="expression">
        /// The local-variable expression being evaluated.
        /// </param>
        /// <param name="localSymbol">
        /// The local symbol to inspect.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for expression analysis.
        /// </param>
        /// <param name="callContext">
        /// The call-site facts known for the current callable.
        /// </param>
        /// <param name="inspectedReturnSymbols">
        /// The method symbols whose return values are currently being
        /// inspected.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the local variable is proven to be
        /// non-null; otherwise <see langword="false"/>.
        /// </returns>
        private static bool IsLocalGuaranteedNonNull(
            ExpressionSyntax expression,
            ILocalSymbol localSymbol,
            SemanticModel semanticModel,
            ExceptionFlowCallContext callContext,
            HashSet<ISymbol> inspectedReturnSymbols)
        {
            if (ExceptionFlowNullabilityFactsProvider.IsPatternLocalGuaranteedNonNull(
                    localSymbol,
                    semanticModel))
            {
                return true;
            }

            if (IsForeachIterationVariableProvenNonNullByCallContext(
                    expression,
                    localSymbol,
                    semanticModel,
                    callContext))
            {
                return true;
            }

            if (IsForeachIterationVariableProvenNonNull(
                    expression,
                    localSymbol,
                    semanticModel,
                    callContext))
            {
                return true;
            }

            if (ExceptionFlowGuardFactsProvider.IsLocalProvenNonNullByPrecedingGuard(
                    expression,
                    localSymbol,
                    semanticModel))
            {
                return true;
            }

            foreach (SyntaxReference syntaxReference
                     in localSymbol.DeclaringSyntaxReferences)
            {
                SyntaxNode declarationNode =
                    syntaxReference.GetSyntax();

                if (IsForeachLocalProvenNonNull(
                        declarationNode,
                        semanticModel,
                        callContext))
                {
                    return true;
                }

                if (declarationNode
                        is not VariableDeclaratorSyntax
                            variableDeclarator ||
                    variableDeclarator.Initializer == null)
                {
                    continue;
                }

                SemanticModel? declarationSemanticModel =
                    ExceptionFlowSemanticScope.GetSemanticModelForSyntaxTree(
                        semanticModel,
                        variableDeclarator.SyntaxTree);

                if (declarationSemanticModel == null)
                {
                    continue;
                }

                if (IsLocalInitializerStillCurrent(
                        expression,
                        localSymbol,
                        variableDeclarator,
                        declarationSemanticModel) &&
                    IsDefinitelyNonNull(
                        variableDeclarator.Initializer.Value,
                        declarationSemanticModel,
                        callContext,
                        inspectedReturnSymbols))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Determines whether a local variable usage refers to the iteration
        /// variable of an enclosing foreach statement whose source sequence
        /// is proven to exclude <see langword="null"/> elements.
        /// </summary>
        /// <param name="expression">
        /// The local-variable usage being evaluated.
        /// </param>
        /// <param name="localSymbol">
        /// The local symbol represented by the expression.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for symbol and sequence analysis.
        /// </param>
        /// <param name="callContext">
        /// The call-site facts known for the current callable.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the local is the iteration variable of
        /// a sequence proven to exclude <see langword="null"/> elements;
        /// otherwise <see langword="false"/>.
        /// </returns>
        private static bool IsForeachIterationVariableProvenNonNull(
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

                HashSet<ISymbol> inspectedSequenceSources =
                    new(SymbolEqualityComparer.Default);

                return IsSequenceExpressionProvenToExcludeNullElements(
                    foreachStatement.Expression,
                    semanticModel,
                    callContext,
                    inspectedSequenceSources);
            }

            return false;
        }

        /// <summary>
        /// Determines whether a local declared by a foreach statement is
        /// proven non-null because the enumerated sequence filters its
        /// elements by type.
        /// </summary>
        /// <param name="declarationNode">
        /// The syntax node declaring the local symbol.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for sequence analysis.
        /// </param>
        /// <param name="callContext">
        /// The call-site facts known for the current callable.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the foreach source is proven to exclude
        /// <see langword="null"/> elements; otherwise
        /// <see langword="false"/>.
        /// </returns>
        private static bool IsForeachLocalProvenNonNull(
            SyntaxNode declarationNode,
            SemanticModel semanticModel,
            ExceptionFlowCallContext callContext)
        {
            ForEachStatementSyntax? foreachStatement =
                declarationNode as ForEachStatementSyntax ??
                declarationNode.AncestorsAndSelf()
                    .OfType<ForEachStatementSyntax>()
                    .FirstOrDefault();

            if (foreachStatement == null)
            {
                return false;
            }

            SemanticModel? declarationSemanticModel =
                ExceptionFlowSemanticScope.GetSemanticModelForSyntaxTree(
                    semanticModel,
                    foreachStatement.SyntaxTree);

            if (declarationSemanticModel == null)
            {
                return false;
            }

            HashSet<ISymbol> inspectedSequenceSources =
                new(SymbolEqualityComparer.Default);

            return IsSequenceExpressionProvenToExcludeNullElements(
                foreachStatement.Expression,
                declarationSemanticModel,
                callContext,
                inspectedSequenceSources);
        }

        /// <summary>
        /// Determines whether a sequence expression is proven to exclude
        /// <see langword="null"/> elements.
        /// </summary>
        /// <param name="expression">
        /// The sequence expression to inspect.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for symbol resolution.
        /// </param>
        /// <param name="callContext">
        /// The call-site facts known for the current callable.
        /// </param>
        /// <param name="inspectedSequenceSources">
        /// The sequence-producing methods, locals, and collection symbols
        /// currently being inspected.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if every element produced by the expression
        /// is proven to exclude <see langword="null"/>; otherwise
        /// <see langword="false"/>.
        /// </returns>
        private static bool
            IsSequenceExpressionProvenToExcludeNullElements(
                ExpressionSyntax expression,
                SemanticModel semanticModel,
                ExceptionFlowCallContext callContext,
                HashSet<ISymbol> inspectedSequenceSources)
        {
            ExpressionSyntax unwrappedExpression =
                UnwrapParenthesizedExpression(expression);

            if (unwrappedExpression
                is CastExpressionSyntax castExpression)
            {
                return IsSequenceExpressionProvenToExcludeNullElements(
                    castExpression.Expression,
                    semanticModel,
                    callContext,
                    inspectedSequenceSources);
            }

            if (unwrappedExpression
                is CheckedExpressionSyntax checkedExpression)
            {
                return IsSequenceExpressionProvenToExcludeNullElements(
                    checkedExpression.Expression,
                    semanticModel,
                    callContext,
                    inspectedSequenceSources);
            }

            SymbolInfo expressionSymbolInfo =
                semanticModel.GetSymbolInfo(
                    unwrappedExpression);

            if (expressionSymbolInfo.Symbol is IParameterSymbol parameterSymbol
                && callContext.GetParameterFacts(parameterSymbol)
                    .ContainsAll(ExceptionFlowValueFacts.NonNullElements)
                && IsSequenceParameterFactStillCurrentAtUse(
                    unwrappedExpression,
                    parameterSymbol,
                    semanticModel))
            {
                return true;
            }

            if (expressionSymbolInfo.Symbol
                    is ILocalSymbol localSymbol &&
                IsLocalSequenceExpressionProvenToExcludeNullElements(
                    unwrappedExpression,
                    localSymbol,
                    semanticModel,
                    callContext,
                    inspectedSequenceSources))
            {
                return true;
            }

            if (unwrappedExpression
                    is MemberAccessExpressionSyntax memberAccess &&
                IsDictionaryValuesExpressionProvenToExcludeNullElements(
                    memberAccess,
                    semanticModel,
                    inspectedSequenceSources))
            {
                return true;
            }

            if (unwrappedExpression
                is not InvocationExpressionSyntax invocation)
            {
                return false;
            }

            SymbolInfo symbolInfo =
                semanticModel.GetSymbolInfo(invocation);

            if (symbolInfo.Symbol
                is not IMethodSymbol methodSymbol)
            {
                return false;
            }

            IMethodSymbol originalMethod =
                methodSymbol.ReducedFrom?.OriginalDefinition ??
                methodSymbol.OriginalDefinition;

            if (ExceptionFlowNullabilityFactsProvider.IsOfTypeSequenceMethod(
                    originalMethod))
            {
                return true;
            }

            if (ExceptionFlowNullabilityFactsProvider.IsKnownFrameworkSequenceWithNonNullElements(
                    originalMethod,
                    semanticModel.Compilation))
            {
                return true;
            }

            if (TryGetElementPreservingSequenceSource(
                    invocation,
                    methodSymbol,
                    originalMethod,
                    out ExpressionSyntax? sourceExpression) &&
                sourceExpression != null)
            {
                return IsSequenceExpressionProvenToExcludeNullElements(
                    sourceExpression,
                    semanticModel,
                    callContext,
                    inspectedSequenceSources);
            }

            if (methodSymbol.ReducedFrom != null
                || methodSymbol.ReturnsVoid
                || methodSymbol.IsAsync
                || methodSymbol.IsExtern
                || methodSymbol.IsAbstract
                || methodSymbol.IsIterator
                || methodSymbol.ReturnsByRef
                || methodSymbol.ReturnsByRefReadonly
                || RequiresSummaryRuntimeDispatch(methodSymbol)
                || originalMethod.DeclaringSyntaxReferences.Length != 1
                || !inspectedSequenceSources.Add(originalMethod))
            {
                return false;
            }

            try
            {
                SyntaxNode declaration =
                    originalMethod.DeclaringSyntaxReferences[0].GetSyntax();

                List<ExpressionSyntax> returnExpressions =
                    GetSourceReturnExpressions(declaration);

                if (returnExpressions.Count == 0)
                {
                    return false;
                }

                ExceptionFlowCallContext calleeContext =
                    CreateCallContext(
                        methodSymbol,
                        invocation.ArgumentList.Arguments,
                        semanticModel,
                        callContext,
                        inspectedSequenceSources);

                foreach (ExpressionSyntax returnExpression in returnExpressions)
                {
                    SemanticModel? returnSemanticModel =
                        ExceptionFlowSemanticScope.GetSemanticModelForSyntaxTree(
                            semanticModel,
                            returnExpression.SyntaxTree);

                    if (returnSemanticModel == null
                        || !IsSequenceExpressionProvenToExcludeNullElements(
                            returnExpression,
                            returnSemanticModel,
                            calleeContext,
                            inspectedSequenceSources))
                    {
                        return false;
                    }
                }

                return true;
            }
            finally
            {
                inspectedSequenceSources.Remove(originalMethod);
            }
        }

    }
}
