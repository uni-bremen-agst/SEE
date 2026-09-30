using static XMLDocNormalizer.Checks.Infrastructure.Exception.Flow.ExceptionFlowSymbolUsageFacts;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace XMLDocNormalizer.Checks.Infrastructure.Exception.Flow
{
    /// <summary>
    /// Contains value-fact analysis for values returned by directly bound
    /// source methods.
    /// </summary>
    internal static partial class ExceptionFlowAnalyzer
    {
        /// <summary>
        /// Attempts to derive facts guaranteed by every normal return value of
        /// a directly and statically bound source method.
        /// </summary>
        /// <param name="invocation">
        /// The invocation expression whose result is inspected.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model associated with the call site.
        /// </param>
        /// <param name="callerContext">
        /// The value facts known for the caller.
        /// </param>
        /// <param name="inspectedValueSources">
        /// The immutable members and source methods currently being inspected
        /// recursively.
        /// </param>
        /// <param name="facts">
        /// The facts guaranteed for every normal returned value when analysis
        /// succeeds.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when all supported normal return paths share
        /// at least one value fact; otherwise <see langword="false"/>.
        /// </returns>
        private static bool TryGetSourceInvocationReturnValueFacts(
            InvocationExpressionSyntax invocation,
            SemanticModel semanticModel,
            ExceptionFlowCallContext callerContext,
            HashSet<ISymbol> inspectedValueSources,
            out ExceptionFlowValueFacts facts)
        {
            facts = ExceptionFlowValueFacts.None;

            SymbolInfo symbolInfo =
                semanticModel.GetSymbolInfo(invocation);

            if (symbolInfo.Symbol is not IMethodSymbol selectedMethod
                || selectedMethod.ReducedFrom != null
                || selectedMethod.ReturnsVoid
                || selectedMethod.IsAsync
                || selectedMethod.IsExtern
                || selectedMethod.IsAbstract
                || selectedMethod.IsIterator
                || selectedMethod.ReturnsByRef
                || selectedMethod.ReturnsByRefReadonly
                || ExceptionFlowRuntimeDispatchClassifier.RequiresRuntimeDispatch(selectedMethod))
            {
                return false;
            }

            IMethodSymbol targetMethod =
                selectedMethod.OriginalDefinition;

            if (targetMethod.DeclaringSyntaxReferences.Length != 1
                || !inspectedValueSources.Add(targetMethod))
            {
                return false;
            }

            try
            {
                SyntaxNode declaration =
                    targetMethod.DeclaringSyntaxReferences[0].GetSyntax();

                List<ExpressionSyntax> returnExpressions =
                    ExceptionFlowEnumValueFactsProvider.GetSourceReturnExpressions(
                        declaration);

                if (returnExpressions.Count == 0)
                {
                    return false;
                }

                ExceptionFlowCallContext calleeContext =
                    CreateCallContext(
                        selectedMethod,
                        invocation.ArgumentList.Arguments,
                        semanticModel,
                        callerContext,
                        inspectedValueSources);

                ExceptionFlowValueFacts commonFacts =
                    ExceptionFlowValueFacts.None;

                bool hasReturnFacts =
                    false;

                foreach (ExpressionSyntax returnExpression in returnExpressions)
                {
                    SemanticModel? returnSemanticModel =
                        ExceptionFlowSemanticScope.GetSemanticModelForSyntaxTree(
                            semanticModel,
                            returnExpression.SyntaxTree);

                    if (returnSemanticModel == null)
                    {
                        return false;
                    }

                    ExceptionFlowValueFacts returnFacts =
                        GetSourceReturnExpressionValueFacts(
                            returnExpression,
                            returnSemanticModel,
                            calleeContext,
                            inspectedValueSources);

                    if (!hasReturnFacts)
                    {
                        commonFacts =
                            returnFacts;

                        hasReturnFacts =
                            true;
                    }
                    else
                    {
                        commonFacts &=
                            returnFacts;
                    }

                    if (commonFacts == ExceptionFlowValueFacts.None)
                    {
                        return false;
                    }
                }

                facts =
                    commonFacts.Normalize();

                return facts != ExceptionFlowValueFacts.None;
            }
            finally
            {
                inspectedValueSources.Remove(targetMethod);
            }
        }

        /// <summary>
        /// Gets value facts for one normal source return expression, including
        /// facts preserved through stable local initializers, supported
        /// framework return values, and directly controlling branch
        /// conditions.
        /// </summary>
        /// <param name="expression">
        /// The returned expression to inspect.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model associated with the returned expression.
        /// </param>
        /// <param name="callContext">
        /// The call-site facts known for the current source callable.
        /// </param>
        /// <param name="inspectedValueSources">
        /// The value-producing symbols currently being inspected recursively.
        /// </param>
        /// <returns>
        /// The facts guaranteed for the returned expression.
        /// </returns>
        private static ExceptionFlowValueFacts GetSourceReturnExpressionValueFacts(
            ExpressionSyntax expression,
            SemanticModel semanticModel,
            ExceptionFlowCallContext callContext,
            HashSet<ISymbol> inspectedValueSources)
        {
            ExpressionSyntax unwrappedExpression =
                UnwrapParenthesizedExpression(expression);

            if (unwrappedExpression is ConditionalExpressionSyntax conditionalExpression)
            {
                ExceptionFlowValueFacts trueFacts =
                    GetSourceReturnExpressionValueFacts(
                        conditionalExpression.WhenTrue,
                        semanticModel,
                        callContext,
                        inspectedValueSources);

                ExceptionFlowValueFacts falseFacts =
                    GetSourceReturnExpressionValueFacts(
                        conditionalExpression.WhenFalse,
                        semanticModel,
                        callContext,
                        inspectedValueSources);

                return (trueFacts & falseFacts).Normalize();
            }

            ExceptionFlowValueFacts facts =
                GetExpressionValueFacts(
                    unwrappedExpression,
                    semanticModel,
                    callContext,
                    inspectedValueSources);

            facts |=
                ExceptionFlowGuardFactsProvider
                    .GetFactsProvenByDirectContainingReturnBranch(
                    unwrappedExpression,
                    semanticModel);

            SymbolInfo symbolInfo =
                semanticModel.GetSymbolInfo(unwrappedExpression);

            if (symbolInfo.Symbol is ILocalSymbol localSymbol
                && inspectedValueSources.Add(localSymbol))
            {
                try
                {
                    if (ExceptionFlowSourcePositionValueFactsProvider.TryGetCurrentLocalInitializerExpression(
                            unwrappedExpression,
                            localSymbol,
                            semanticModel,
                            out ExpressionSyntax? initializer)
                        && initializer != null)
                    {
                        facts |=
                            GetSourceReturnExpressionValueFacts(
                                initializer,
                                semanticModel,
                                callContext,
                                inspectedValueSources);
                    }
                }
                finally
                {
                    inspectedValueSources.Remove(localSymbol);
                }
            }

            if (unwrappedExpression is InvocationExpressionSyntax invocation)
            {
                facts |=
                    GetKnownFrameworkInvocationValueFacts(
                        invocation,
                        semanticModel,
                        callContext,
                        inspectedValueSources);
            }

            return facts.Normalize();
        }

        /// <summary>
        /// Gets value facts guaranteed by supported framework invocation return
        /// values.
        /// </summary>
        /// <param name="invocation">
        /// The framework invocation to inspect.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for method and argument analysis.
        /// </param>
        /// <param name="callContext">
        /// The current source-call context.
        /// </param>
        /// <param name="inspectedValueSources">
        /// The value-producing symbols currently being inspected recursively.
        /// </param>
        /// <returns>
        /// The facts guaranteed for the supported framework return value.
        /// </returns>
        private static ExceptionFlowValueFacts GetKnownFrameworkInvocationValueFacts(
            InvocationExpressionSyntax invocation,
            SemanticModel semanticModel,
            ExceptionFlowCallContext callContext,
            HashSet<ISymbol> inspectedValueSources)
        {
            SymbolInfo symbolInfo = semanticModel.GetSymbolInfo(invocation);

            if (symbolInfo.Symbol is not IMethodSymbol methodSymbol)
            {
                return ExceptionFlowValueFacts.None;
            }

            IMethodSymbol originalMethod =
                methodSymbol.ReducedFrom?.OriginalDefinition ?? methodSymbol.OriginalDefinition;

            if (ExceptionFlowNullabilityFactsProvider
                    .IsRoslynCompilationUnitRootMethod(originalMethod)
                || ExceptionFlowNullabilityFactsProvider
                    .IsRoslynCSharpSyntaxTreeParseTextMethod(originalMethod)
                || ExceptionFlowNullabilityFactsProvider
                    .IsSystemEnumToStringMethod(originalMethod))
            {
                return ExceptionFlowValueFacts.NonNull;
            }

            if (IsConditionalWeakTableGetValueResultDefinitelyNonNull(
                    invocation,
                    semanticModel,
                    inspectedValueSources))
            {
                return ExceptionFlowValueFacts.NonNull;
            }

            if (originalMethod.ReturnType.SpecialType != SpecialType.System_String
                || !string.Equals(
                    originalMethod.ContainingType.ToDisplayString(),
                    "System.IO.Path",
                    StringComparison.Ordinal))
            {
                return ExceptionFlowValueFacts.None;
            }

            if (string.Equals(originalMethod.Name, nameof(Path.GetFileNameWithoutExtension), StringComparison.Ordinal)
                && invocation.ArgumentList.Arguments.Count >= 1)
            {
                ExceptionFlowValueFacts pathFacts =
                    GetExpressionValueFacts(
                        invocation.ArgumentList.Arguments[0].Expression,
                        semanticModel,
                        callContext,
                        inspectedValueSources);

                if (pathFacts.ContainsAll(ExceptionFlowValueFacts.NonNull))
                {
                    return ExceptionFlowValueFacts.NonNull;
                }
            }

            if (string.Equals(originalMethod.Name, nameof(Path.Combine), StringComparison.Ordinal))
            {
                ExceptionFlowValueFacts facts = ExceptionFlowValueFacts.NonNull;

                foreach (ArgumentSyntax argument in invocation.ArgumentList.Arguments)
                {
                    ExceptionFlowValueFacts argumentFacts = GetExpressionValueFacts(
                        argument.Expression,
                        semanticModel,
                        callContext,
                        inspectedValueSources);

                    if (argumentFacts.ContainsAll(ExceptionFlowValueFacts.NonWhiteSpaceString))
                    {
                        facts |= ExceptionFlowValueFacts.NonWhiteSpaceString;
                    }
                    else if (argumentFacts.ContainsAll(ExceptionFlowValueFacts.NonEmptyString))
                    {
                        facts |= ExceptionFlowValueFacts.NonEmptyString;
                    }
                }

                return facts.Normalize();
            }

            if (string.Equals(
                    originalMethod.Name,
                    nameof(Path.ChangeExtension),
                    StringComparison.Ordinal)
                && invocation.ArgumentList.Arguments.Count >= 1)
            {
                ExceptionFlowValueFacts pathFacts = GetExpressionValueFacts(
                    invocation.ArgumentList.Arguments[0].Expression,
                    semanticModel,
                    callContext,
                    inspectedValueSources);

                if (pathFacts.ContainsAll(ExceptionFlowValueFacts.NonNull))
                {
                    return ExceptionFlowValueFacts.NonNull;
                }
            }

            return ExceptionFlowValueFacts.None;
        }
    }
}
