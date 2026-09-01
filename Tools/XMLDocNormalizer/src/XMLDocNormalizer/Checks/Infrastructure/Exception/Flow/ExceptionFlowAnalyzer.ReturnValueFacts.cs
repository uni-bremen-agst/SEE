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
                || RequiresSummaryRuntimeDispatch(selectedMethod))
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
                    GetSourceReturnExpressions(declaration);

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
                        GetSemanticModelForSyntaxTree(
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
                GetFactsProvenByDirectContainingReturnBranch(
                    unwrappedExpression,
                    semanticModel);

            SymbolInfo symbolInfo =
                semanticModel.GetSymbolInfo(unwrappedExpression);

            if (symbolInfo.Symbol is ILocalSymbol localSymbol
                && inspectedValueSources.Add(localSymbol))
            {
                try
                {
                    if (TryGetCurrentLocalInitializerExpression(
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
        /// Gets value facts established by the directly controlling
        /// <c>if</c> branch of a return statement.
        /// </summary>
        /// <param name="expression">
        /// The returned expression whose enclosing branch should be inspected.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for symbol and invocation resolution.
        /// </param>
        /// <returns>
        /// The facts established for the returned expression by the branch
        /// condition, or <see cref="ExceptionFlowValueFacts.None"/> when the
        /// branch is unsupported.
        /// </returns>
        private static ExceptionFlowValueFacts
            GetFactsProvenByDirectContainingReturnBranch(
                ExpressionSyntax expression,
                SemanticModel semanticModel)
        {
            if (expression.Parent is not ReturnStatementSyntax returnStatement
                || !TryGetDirectContainingIfBranch(
                    returnStatement,
                    out IfStatementSyntax? ifStatement,
                    out bool branchConditionValue)
                || ifStatement == null)
            {
                return ExceptionFlowValueFacts.None;
            }

            return GetStringFactsProvenForStableExpressionByCondition(
                ifStatement.Condition,
                expression,
                branchConditionValue,
                semanticModel);
        }

        /// <summary>
        /// Attempts to identify an <c>if</c> statement whose selected branch
        /// consists directly of the supplied return statement.
        /// </summary>
        /// <param name="returnStatement">
        /// The return statement to inspect.
        /// </param>
        /// <param name="ifStatement">
        /// The containing <c>if</c> statement when successful.
        /// </param>
        /// <param name="branchConditionValue">
        /// <see langword="true"/> for the true branch and
        /// <see langword="false"/> for the else branch.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when a supported direct containing branch
        /// exists; otherwise <see langword="false"/>.
        /// </returns>
        private static bool TryGetDirectContainingIfBranch(
            ReturnStatementSyntax returnStatement,
            out IfStatementSyntax? ifStatement,
            out bool branchConditionValue)
        {
            ifStatement = null;
            branchConditionValue = false;

            if (returnStatement.Parent is IfStatementSyntax directIf
                && ReferenceEquals(directIf.Statement, returnStatement))
            {
                ifStatement = directIf;
                branchConditionValue = true;
                return true;
            }

            if (returnStatement.Parent is ElseClauseSyntax directElse
                && directElse.Parent is IfStatementSyntax directElseIf
                && ReferenceEquals(directElse.Statement, returnStatement))
            {
                ifStatement = directElseIf;
                branchConditionValue = false;
                return true;
            }

            if (returnStatement.Parent is not BlockSyntax block
                || block.Statements.Count != 1
                || !ReferenceEquals(block.Statements[0], returnStatement))
            {
                return false;
            }

            if (block.Parent is IfStatementSyntax blockIf
                && ReferenceEquals(blockIf.Statement, block))
            {
                ifStatement = blockIf;
                branchConditionValue = true;
                return true;
            }

            if (block.Parent is ElseClauseSyntax blockElse
                && blockElse.Parent is IfStatementSyntax blockElseIf
                && ReferenceEquals(blockElse.Statement, block))
            {
                ifStatement = blockElseIf;
                branchConditionValue = false;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Gets string facts established for a stable expression by a
        /// supported boolean condition result.
        /// </summary>
        /// <param name="condition">
        /// The controlling condition.
        /// </param>
        /// <param name="valueExpression">
        /// The stable returned expression whose facts should be derived.
        /// </param>
        /// <param name="conditionValue">
        /// The condition result required to enter the return branch.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for expression and invocation resolution.
        /// </param>
        /// <returns>
        /// The string facts established by the condition result.
        /// </returns>
        private static ExceptionFlowValueFacts
            GetStringFactsProvenForStableExpressionByCondition(
                ExpressionSyntax condition,
                ExpressionSyntax valueExpression,
                bool conditionValue,
                SemanticModel semanticModel)
        {
            ExpressionSyntax unwrappedCondition =
                UnwrapParenthesizedExpression(condition);

            if (unwrappedCondition is PrefixUnaryExpressionSyntax negation
                && negation.IsKind(SyntaxKind.LogicalNotExpression))
            {
                return GetStringFactsProvenForStableExpressionByCondition(
                    negation.Operand,
                    valueExpression,
                    !conditionValue,
                    semanticModel);
            }

            if (unwrappedCondition is BinaryExpressionSyntax logicalAnd
                && logicalAnd.IsKind(SyntaxKind.LogicalAndExpression)
                && conditionValue)
            {
                ExceptionFlowValueFacts leftFacts =
                    GetStringFactsProvenForStableExpressionByCondition(
                        logicalAnd.Left,
                        valueExpression,
                        true,
                        semanticModel);

                ExceptionFlowValueFacts rightFacts =
                    GetStringFactsProvenForStableExpressionByCondition(
                        logicalAnd.Right,
                        valueExpression,
                        true,
                        semanticModel);

                return (leftFacts | rightFacts).Normalize();
            }

            if (unwrappedCondition is BinaryExpressionSyntax logicalOr
                && logicalOr.IsKind(SyntaxKind.LogicalOrExpression)
                && !conditionValue)
            {
                ExceptionFlowValueFacts leftFacts =
                    GetStringFactsProvenForStableExpressionByCondition(
                        logicalOr.Left,
                        valueExpression,
                        false,
                        semanticModel);

                ExceptionFlowValueFacts rightFacts =
                    GetStringFactsProvenForStableExpressionByCondition(
                        logicalOr.Right,
                        valueExpression,
                        false,
                        semanticModel);

                return (leftFacts | rightFacts).Normalize();
            }

            if (conditionValue
                || unwrappedCondition is not InvocationExpressionSyntax invocation
                || invocation.ArgumentList.Arguments.Count != 1
                || semanticModel.GetSymbolInfo(invocation).Symbol
                    is not IMethodSymbol methodSymbol
                || !methodSymbol.OriginalDefinition.IsStatic
                || methodSymbol.OriginalDefinition.ContainingType.SpecialType
                    != SpecialType.System_String)
            {
                return ExceptionFlowValueFacts.None;
            }

            ExpressionSyntax guardedExpression =
                invocation.ArgumentList.Arguments[0].Expression;

            if (!AreEquivalentStableValueExpressions(
                    guardedExpression,
                    valueExpression,
                    semanticModel))
            {
                return ExceptionFlowValueFacts.None;
            }

            if (methodSymbol.OriginalDefinition.Name
                == nameof(string.IsNullOrWhiteSpace))
            {
                return (
                    ExceptionFlowValueFacts.NonNull
                    | ExceptionFlowValueFacts.NonEmptyString
                    | ExceptionFlowValueFacts.NonWhiteSpaceString)
                    .Normalize();
            }

            if (methodSymbol.OriginalDefinition.Name
                == nameof(string.IsNullOrEmpty))
            {
                return (
                    ExceptionFlowValueFacts.NonNull
                    | ExceptionFlowValueFacts.NonEmptyString)
                    .Normalize();
            }

            return ExceptionFlowValueFacts.None;
        }

        /// <summary>
        /// Determines whether two expressions represent the same stable value
        /// for the duration of a direct guarded return.
        /// </summary>
        /// <param name="left">
        /// The first expression.
        /// </param>
        /// <param name="right">
        /// The second expression.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for stability analysis.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when the expressions are syntactically
        /// equivalent and represent supported stable values; otherwise
        /// <see langword="false"/>.
        /// </returns>
        private static bool AreEquivalentStableValueExpressions(
            ExpressionSyntax left,
            ExpressionSyntax right,
            SemanticModel semanticModel)
        {
            ExpressionSyntax unwrappedLeft =
                UnwrapParenthesizedExpression(left);

            ExpressionSyntax unwrappedRight =
                UnwrapParenthesizedExpression(right);

            return SyntaxFactory.AreEquivalent(
                       unwrappedLeft,
                       unwrappedRight)
                && IsStableGuardValueExpression(
                    unwrappedLeft,
                    semanticModel)
                && IsStableGuardValueExpression(
                    unwrappedRight,
                    semanticModel);
        }

        /// <summary>
        /// Determines whether an expression represents a value that cannot
        /// change between a supported string-validation condition and its
        /// direct return.
        /// </summary>
        /// <param name="expression">
        /// The expression to inspect.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for symbol resolution.
        /// </param>
        /// <returns>
        /// <see langword="true"/> for locals, non-ref parameters, and get-only
        /// auto-properties; otherwise <see langword="false"/>.
        /// </returns>
        private static bool IsStableGuardValueExpression(
            ExpressionSyntax expression,
            SemanticModel semanticModel)
        {
            SymbolInfo symbolInfo =
                semanticModel.GetSymbolInfo(expression);

            if (symbolInfo.Symbol is ILocalSymbol)
            {
                return true;
            }

            if (symbolInfo.Symbol is IParameterSymbol parameterSymbol)
            {
                return parameterSymbol.RefKind == RefKind.None;
            }

            if (symbolInfo.Symbol is not IPropertySymbol propertySymbol
                || propertySymbol.SetMethod != null
                || propertySymbol.DeclaringSyntaxReferences.Length != 1
                || propertySymbol.DeclaringSyntaxReferences[0].GetSyntax()
                    is not PropertyDeclarationSyntax propertyDeclaration
                || propertyDeclaration.ExpressionBody != null
                || propertyDeclaration.AccessorList == null)
            {
                return false;
            }

            return propertyDeclaration.AccessorList.Accessors.Any(
                static accessor =>
                    accessor.IsKind(SyntaxKind.GetAccessorDeclaration)
                    && accessor.Body == null
                    && accessor.ExpressionBody == null);
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

            if (IsRoslynCompilationUnitRootMethod(originalMethod)
                || IsSystemEnumToStringMethod(originalMethod))
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

        /// <summary>
        /// Determines whether a method is Roslyn's compilation-unit-root accessor.
        /// </summary>
        /// <param name="methodSymbol">
        /// The method to inspect.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when successful invocation is guaranteed to return
        /// a compilation-unit syntax node; otherwise <see langword="false"/>.
        /// </returns>
        private static bool IsRoslynCompilationUnitRootMethod(IMethodSymbol methodSymbol)
        {
            return string.Equals(
                    methodSymbol.Name,
                    "GetCompilationUnitRoot",
                    StringComparison.Ordinal)
                && string.Equals(
                    methodSymbol.ContainingAssembly?.Name,
                    "Microsoft.CodeAnalysis.CSharp",
                    StringComparison.Ordinal)
                && string.Equals(
                    methodSymbol.ContainingNamespace.ToDisplayString(),
                    "Microsoft.CodeAnalysis.CSharp",
                    StringComparison.Ordinal)
                && string.Equals(
                    methodSymbol.ReturnType.ToDisplayString(),
                    "Microsoft.CodeAnalysis.CSharp.Syntax.CompilationUnitSyntax",
                    StringComparison.Ordinal);
        }

        /// <summary>
        /// Determines whether a method is the framework implementation of
        /// <see cref="Enum.ToString()"/>.
        /// </summary>
        /// <param name="methodSymbol">
        /// The method to inspect.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when the method returns the textual representation
        /// of an enum value; otherwise <see langword="false"/>.
        /// </returns>
        private static bool IsSystemEnumToStringMethod(IMethodSymbol methodSymbol)
        {
            return string.Equals(methodSymbol.Name, nameof(ToString), StringComparison.Ordinal)
                && methodSymbol.Parameters.Length == 0
                && methodSymbol.ReturnType.SpecialType == SpecialType.System_String
                && string.Equals(
                    methodSymbol.ContainingType.ToDisplayString(),
                    "System.Enum",
                    StringComparison.Ordinal);
        }
    }
}
