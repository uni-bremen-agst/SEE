using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace XMLDocNormalizer.Checks.Infrastructure.Exception.Flow
{
    /// <summary>
    /// Contains sequence-element facts established by successful completion of
    /// source-level helper calls.
    /// </summary>
    internal static partial class ExceptionFlowAnalyzer
    {
        /// <summary>
        /// Determines whether an earlier successfully completed source helper
        /// proves that every element currently contained in a local or parameter
        /// sequence is non-null.
        /// </summary>
        /// <param name="expression">
        /// The later use of the sequence.
        /// </param>
        /// <param name="sequenceSymbol">
        /// The local or parameter sequence symbol.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for flow and source-helper analysis.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when an earlier helper successfully validated
        /// every current sequence element and no intervening statement can
        /// invalidate that fact; otherwise <see langword="false"/>.
        /// </returns>
        private static bool IsSequenceSymbolProvenToContainNonNullElementsBySuccessfulHelper(
            ExpressionSyntax expression,
            ISymbol sequenceSymbol,
            SemanticModel semanticModel)
        {
            if (sequenceSymbol is not ILocalSymbol
                && sequenceSymbol is not IParameterSymbol)
            {
                return false;
            }

            StatementSyntax? currentStatement =
                expression.AncestorsAndSelf()
                    .OfType<StatementSyntax>()
                    .FirstOrDefault();

            if (currentStatement?.Parent is not BlockSyntax containingBlock)
            {
                return false;
            }

            int currentStatementIndex =
                containingBlock.Statements.IndexOf(currentStatement);

            if (currentStatementIndex < 0)
            {
                return false;
            }

            for (int index = currentStatementIndex - 1;
                 index >= 0;
                 index--)
            {
                StatementSyntax precedingStatement =
                    containingBlock.Statements[index];

                if (StatementSuccessfulCompletionProvesSequenceElementsNonNull(
                        precedingStatement,
                        sequenceSymbol,
                        semanticModel))
                {
                    return true;
                }

                if (!DoesStatementPreserveSequenceSymbolContents(
                        precedingStatement,
                        sequenceSymbol,
                        semanticModel))
                {
                    return false;
                }
            }

            return false;
        }

        /// <summary>
        /// Determines whether a statement preserves the identity, contents, and
        /// ownership assumptions of a sequence whose element facts are being
        /// reused.
        /// </summary>
        /// <param name="statement">
        /// The intervening statement to inspect.
        /// </param>
        /// <param name="sequenceSymbol">
        /// The local or parameter sequence symbol.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for symbol and data-flow analysis.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when the statement does not modify, replace,
        /// or expose the sequence through an unsupported operation; otherwise
        /// <see langword="false"/>.
        /// </returns>
        private static bool DoesStatementPreserveSequenceSymbolContents(
            StatementSyntax statement,
            ISymbol sequenceSymbol,
            SemanticModel semanticModel)
        {
            DataFlowAnalysis? dataFlow =
                semanticModel.AnalyzeDataFlow(statement);

            if (dataFlow?.Succeeded != true
                || dataFlow.WrittenInside.Any(
                    writtenSymbol =>
                        SymbolEqualityComparer.Default.Equals(
                            writtenSymbol,
                            sequenceSymbol)))
            {
                return false;
            }

            IEnumerable<IdentifierNameSyntax> references =
                statement.DescendantNodes()
                    .OfType<IdentifierNameSyntax>()
                    .Where(
                        identifier =>
                            ExpressionReferencesSymbol(
                                identifier,
                                sequenceSymbol,
                                semanticModel));

            foreach (IdentifierNameSyntax reference in references)
            {
                if (IsSupportedReadOnlySequenceObservation(
                        reference,
                        semanticModel)
                    || IsSupportedSequenceNullObservation(reference)
                    || IsSourceHelperArgumentProvenToPreserveSequenceContents(
                        reference,
                        semanticModel))
                {
                    continue;
                }

                return false;
            }

            return true;
        }

        /// <summary>
        /// Determines whether successful completion of a statement proves
        /// every element of a supplied local or parameter sequence non-null.
        /// </summary>
        /// <param name="statement">
        /// The successfully completed statement.
        /// </param>
        /// <param name="sequenceSymbol">
        /// The caller sequence symbol.
        /// </param>
        /// <param name="semanticModel">
        /// The caller semantic model.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when the statement contains a supported
        /// source-helper invocation that validates every sequence element;
        /// otherwise <see langword="false"/>.
        /// </returns>
        private static bool StatementSuccessfulCompletionProvesSequenceElementsNonNull(
            StatementSyntax statement,
            ISymbol sequenceSymbol,
            SemanticModel semanticModel)
        {
            if (statement is not ExpressionStatementSyntax expressionStatement
                || expressionStatement.Expression
                    is not InvocationExpressionSyntax invocation)
            {
                return false;
            }

            return InvocationSuccessfulCompletionProvesSequenceElementsNonNull(
                invocation,
                sequenceSymbol,
                semanticModel);
        }

        /// <summary>
        /// Determines whether successful completion of a source invocation
        /// validates every element of one supplied local or parameter sequence.
        /// </summary>
        /// <param name="invocation">
        /// The invocation to inspect.
        /// </param>
        /// <param name="sequenceSymbol">
        /// The local or parameter sequence supplied to the invocation.
        /// </param>
        /// <param name="semanticModel">
        /// The caller semantic model.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when the corresponding source parameter is
        /// completely enumerated and every iteration necessarily dereferences
        /// its element; otherwise <see langword="false"/>.
        /// </returns>
        private static bool InvocationSuccessfulCompletionProvesSequenceElementsNonNull(
            InvocationExpressionSyntax invocation,
            ISymbol sequenceSymbol,
            SemanticModel semanticModel)
        {
            SymbolInfo symbolInfo =
                semanticModel.GetSymbolInfo(invocation);

            if (symbolInfo.Symbol is not IMethodSymbol selectedMethod
                || selectedMethod.ReducedFrom != null
                || selectedMethod.IsAsync
                || selectedMethod.IsAbstract
                || selectedMethod.IsExtern
                || RequiresSummaryRuntimeDispatch(selectedMethod)
                || selectedMethod.DeclaringSyntaxReferences.Length != 1)
            {
                return false;
            }

            SeparatedSyntaxList<ArgumentSyntax> arguments =
                invocation.ArgumentList.Arguments;

            for (int argumentIndex = 0;
                 argumentIndex < arguments.Count;
                 argumentIndex++)
            {
                ArgumentSyntax argument = arguments[argumentIndex];

                if (!argument.RefKindKeyword.IsKind(SyntaxKind.None))
                {
                    continue;
                }

                ExpressionSyntax argumentExpression =
                    UnwrapParenthesizedExpression(argument.Expression);

                SymbolInfo argumentSymbolInfo =
                    semanticModel.GetSymbolInfo(argumentExpression);

                if (!SymbolEqualityComparer.Default.Equals(
                        argumentSymbolInfo.Symbol,
                        sequenceSymbol))
                {
                    continue;
                }

                int parameterIndex =
                    GetParameterIndexForArgument(
                        argument,
                        argumentIndex,
                        selectedMethod);

                if (parameterIndex < 0
                    || parameterIndex >= selectedMethod.Parameters.Length)
                {
                    continue;
                }

                if (MethodSuccessfulCompletionProvesParameterElementsNonNull(
                        selectedMethod,
                        parameterIndex,
                        semanticModel))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Determines whether every normal completion of a source method proves
        /// every element of one sequence parameter non-null.
        /// </summary>
        /// <param name="methodSymbol">
        /// The source method to inspect.
        /// </param>
        /// <param name="parameterIndex">
        /// The sequence parameter ordinal.
        /// </param>
        /// <param name="semanticModel">
        /// A semantic model from the caller compilation.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when the method completely validates every
        /// element on every non-vacuous normal path; otherwise
        /// <see langword="false"/>.
        /// </returns>
        private static bool MethodSuccessfulCompletionProvesParameterElementsNonNull(
            IMethodSymbol methodSymbol,
            int parameterIndex,
            SemanticModel semanticModel)
        {
            IMethodSymbol normalizedMethod =
                methodSymbol.OriginalDefinition;

            if (parameterIndex < 0
                || parameterIndex >= normalizedMethod.Parameters.Length
                || normalizedMethod.DeclaringSyntaxReferences.Length != 1)
            {
                return false;
            }

            SyntaxNode declaration =
                normalizedMethod.DeclaringSyntaxReferences[0].GetSyntax();

            if (DeclarationContainsYield(declaration))
            {
                return false;
            }

            SemanticModel? declarationSemanticModel =
                GetSemanticModelForSyntaxTree(
                    semanticModel,
                    declaration.SyntaxTree);

            if (declarationSemanticModel == null
                || !TryGetDeclaredMethodAndBody(
                    declaration,
                    declarationSemanticModel,
                    out IMethodSymbol? declaredMethod,
                    out BlockSyntax? body,
                    out ArrowExpressionClauseSyntax? _)
                || declaredMethod == null
                || body == null
                || parameterIndex >= declaredMethod.Parameters.Length)
            {
                return false;
            }

            IParameterSymbol parameterSymbol =
                declaredMethod.Parameters[parameterIndex];

            if (parameterSymbol.RefKind != RefKind.None
                || !DoesSourceParameterPreserveSequenceContents(
                    parameterSymbol,
                    declarationSemanticModel))
            {
                return false;
            }

            for (int statementIndex = 0;
                 statementIndex < body.Statements.Count;
                 statementIndex++)
            {
                if (body.Statements[statementIndex]
                        is not ForEachStatementSyntax foreachStatement
                    || !ForeachDirectlyEnumeratesParameter(
                        foreachStatement,
                        parameterSymbol,
                        declarationSemanticModel))
                {
                    continue;
                }

                if (!AllPrecedingReturnsAreVacuousSequenceGuards(
                        body,
                        statementIndex,
                        parameterSymbol,
                        declarationSemanticModel))
                {
                    return false;
                }

                return ForeachBodyNecessarilyDereferencesEveryIteration(
                    foreachStatement,
                    declarationSemanticModel);
            }

            return false;
        }

        /// <summary>
        /// Determines whether a foreach statement directly enumerates the
        /// specified sequence parameter.
        /// </summary>
        /// <param name="foreachStatement">
        /// The foreach statement to inspect.
        /// </param>
        /// <param name="parameterSymbol">
        /// The expected sequence parameter.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for symbol resolution.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when the foreach source directly references
        /// the parameter; otherwise <see langword="false"/>.
        /// </returns>
        private static bool ForeachDirectlyEnumeratesParameter(
            ForEachStatementSyntax foreachStatement,
            IParameterSymbol parameterSymbol,
            SemanticModel semanticModel)
        {
            ExpressionSyntax sourceExpression =
                UnwrapParenthesizedExpression(
                    foreachStatement.Expression);

            SymbolInfo sourceSymbolInfo =
                semanticModel.GetSymbolInfo(sourceExpression);

            return SymbolEqualityComparer.Default.Equals(
                sourceSymbolInfo.Symbol,
                parameterSymbol);
        }

        /// <summary>
        /// Determines whether every normal return before a validating foreach
        /// can only occur when the supplied sequence has no elements requiring
        /// validation.
        /// </summary>
        /// <param name="body">
        /// The source method body.
        /// </param>
        /// <param name="foreachStatementIndex">
        /// The index of the validating foreach statement.
        /// </param>
        /// <param name="parameterSymbol">
        /// The sequence parameter being validated.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for control-flow and symbol analysis.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when every earlier return is restricted to a
        /// vacuous sequence case; otherwise <see langword="false"/>.
        /// </returns>
        private static bool AllPrecedingReturnsAreVacuousSequenceGuards(
            BlockSyntax body,
            int foreachStatementIndex,
            IParameterSymbol parameterSymbol,
            SemanticModel semanticModel)
        {
            for (int index = 0;
                 index < foreachStatementIndex;
                 index++)
            {
                StatementSyntax statement = body.Statements[index];

                ReturnStatementSyntax[] returns =
                    statement.DescendantNodesAndSelf(
                            static node =>
                                node is not AnonymousFunctionExpressionSyntax
                                && node is not LocalFunctionStatementSyntax)
                        .OfType<ReturnStatementSyntax>()
                        .ToArray();

                if (returns.Length == 0)
                {
                    continue;
                }

                if (statement is not IfStatementSyntax ifStatement
                    || returns.Length != 1
                    || !IsSingleVoidReturn(ifStatement.Statement)
                    || !ConditionTrueImpliesSequenceHasNoElements(
                        ifStatement.Condition,
                        parameterSymbol,
                        semanticModel))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Determines whether a statement consists solely of one parameterless
        /// return statement.
        /// </summary>
        /// <param name="statement">
        /// The statement to inspect.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when the statement is a direct void return or
        /// a block containing only one void return; otherwise
        /// <see langword="false"/>.
        /// </returns>
        private static bool IsSingleVoidReturn(StatementSyntax statement)
        {
            if (statement is ReturnStatementSyntax directReturn)
            {
                return directReturn.Expression == null;
            }

            return statement is BlockSyntax block
                && block.Statements.Count == 1
                && block.Statements[0] is ReturnStatementSyntax blockReturn
                && blockReturn.Expression == null;
        }

        /// <summary>
        /// Determines whether a true condition proves that a sequence parameter
        /// is null or empty.
        /// </summary>
        /// <param name="condition">
        /// The condition guarding an early return.
        /// </param>
        /// <param name="parameterSymbol">
        /// The sequence parameter being inspected.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for constant and symbol analysis.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when every way for the condition to evaluate
        /// to <see langword="true"/> implies that the sequence is null or
        /// contains no elements; otherwise <see langword="false"/>.
        /// </returns>
        private static bool ConditionTrueImpliesSequenceHasNoElements(
            ExpressionSyntax condition,
            IParameterSymbol parameterSymbol,
            SemanticModel semanticModel)
        {
            ExpressionSyntax unwrappedCondition =
                UnwrapParenthesizedExpression(condition);

            if (unwrappedCondition is BinaryExpressionSyntax logicalOr
                && logicalOr.IsKind(SyntaxKind.LogicalOrExpression))
            {
                return ConditionTrueImpliesSequenceHasNoElements(
                           logicalOr.Left,
                           parameterSymbol,
                           semanticModel)
                    && ConditionTrueImpliesSequenceHasNoElements(
                           logicalOr.Right,
                           parameterSymbol,
                           semanticModel);
            }

            if (IsSymbolComparedEqualToNull(
                    unwrappedCondition,
                    parameterSymbol,
                    semanticModel)
                || IsSymbolMatchedAgainstNullPattern(
                    unwrappedCondition,
                    parameterSymbol,
                    semanticModel))
            {
                return true;
            }

            if (unwrappedCondition
                    is not BinaryExpressionSyntax equalsExpression
                || !equalsExpression.IsKind(SyntaxKind.EqualsExpression))
            {
                return false;
            }

            return IsSequenceCountComparedEqualToZero(
                       equalsExpression.Left,
                       equalsExpression.Right,
                       parameterSymbol,
                       semanticModel)
                || IsSequenceCountComparedEqualToZero(
                       equalsExpression.Right,
                       equalsExpression.Left,
                       parameterSymbol,
                       semanticModel);
        }

        /// <summary>
        /// Determines whether one side of an equality is the sequence count
        /// and the other side is the constant zero.
        /// </summary>
        /// <param name="countExpression">
        /// The expression expected to represent the sequence count.
        /// </param>
        /// <param name="zeroExpression">
        /// The expression expected to represent the constant zero.
        /// </param>
        /// <param name="parameterSymbol">
        /// The sequence parameter being inspected.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for constant and symbol analysis.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when the expressions represent
        /// <c>sequence.Count == 0</c>; otherwise <see langword="false"/>.
        /// </returns>
        private static bool IsSequenceCountComparedEqualToZero(
            ExpressionSyntax countExpression,
            ExpressionSyntax zeroExpression,
            IParameterSymbol parameterSymbol,
            SemanticModel semanticModel)
        {
            Optional<object?> constantValue =
                semanticModel.GetConstantValue(zeroExpression);

            if (!constantValue.HasValue
                || constantValue.Value is not int integerValue
                || integerValue != 0
                || countExpression
                    is not MemberAccessExpressionSyntax memberAccess)
            {
                return false;
            }

            if (!ExpressionReferencesSymbol(
                    memberAccess.Expression,
                    parameterSymbol,
                    semanticModel))
            {
                return false;
            }

            SymbolInfo memberSymbolInfo =
                semanticModel.GetSymbolInfo(memberAccess);

            return memberSymbolInfo.Symbol is IPropertySymbol propertySymbol
                && IsFrameworkCollectionCountProperty(propertySymbol);
        }

        /// <summary>
        /// Determines whether every completed foreach iteration necessarily
        /// dereferences its iteration variable and the loop cannot terminate early
        /// while leaving later elements unvalidated.
        /// </summary>
        /// <param name="foreachStatement">
        /// The validating foreach statement.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for iteration-symbol analysis.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when every element must be successfully
        /// dereferenced before the loop can complete normally; otherwise
        /// <see langword="false"/>.
        /// </returns>
        private static bool ForeachBodyNecessarilyDereferencesEveryIteration(
            ForEachStatementSyntax foreachStatement,
            SemanticModel semanticModel)
        {
            ISymbol? iterationSymbol =
                semanticModel.GetDeclaredSymbol(foreachStatement);

            if (iterationSymbol == null)
            {
                return false;
            }

            if (ForeachBodyCanExitBeforeRemainingElementsAreValidated(
                    foreachStatement))
            {
                return false;
            }

            if (foreachStatement.Statement is not BlockSyntax body)
            {
                return StatementDefinitelyDereferencesSymbol(
                    foreachStatement.Statement,
                    iterationSymbol,
                    semanticModel);
            }

            foreach (StatementSyntax statement in body.Statements)
            {
                if (StatementDefinitelyDereferencesSymbol(
                        statement,
                        iterationSymbol,
                        semanticModel))
                {
                    return true;
                }

                if (statement is IfStatementSyntax
                    || statement is SwitchStatementSyntax
                    || statement is CommonForEachStatementSyntax
                    || statement is ForStatementSyntax
                    || statement is WhileStatementSyntax
                    || statement is DoStatementSyntax
                    || statement is TryStatementSyntax)
                {
                    return false;
                }
            }

            return false;
        }

        /// <summary>
        /// Determines whether control flow inside a validating foreach can terminate
        /// or skip the target foreach before all remaining sequence elements have
        /// been validated.
        /// </summary>
        /// <param name="foreachStatement">
        /// The validating foreach statement.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when a return, goto, yield, or control statement
        /// targeting the validating foreach can bypass remaining elements; otherwise
        /// <see langword="false"/>.
        /// </returns>
        private static bool ForeachBodyCanExitBeforeRemainingElementsAreValidated(
            ForEachStatementSyntax foreachStatement)
        {
            IEnumerable<SyntaxNode> bodyNodes =
                foreachStatement.Statement.DescendantNodesAndSelf(
                    static node =>
                        node is not AnonymousFunctionExpressionSyntax
                        && node is not LocalFunctionStatementSyntax);

            foreach (SyntaxNode node in bodyNodes)
            {
                switch (node)
                {
                    case ReturnStatementSyntax:
                    case GotoStatementSyntax:
                    case YieldStatementSyntax:
                        return true;

                    case BreakStatementSyntax breakStatement:
                        if (BreakTargetsForeach(
                                breakStatement,
                                foreachStatement))
                        {
                            return true;
                        }

                        break;

                    case ContinueStatementSyntax continueStatement:
                        if (ContinueTargetsForeach(
                                continueStatement,
                                foreachStatement))
                        {
                            return true;
                        }

                        break;
                }
            }

            return false;
        }

        /// <summary>
        /// Determines whether a break statement exits the specified foreach rather
        /// than a nested switch or loop.
        /// </summary>
        /// <param name="breakStatement">
        /// The break statement to inspect.
        /// </param>
        /// <param name="targetForeach">
        /// The foreach whose completion is being analyzed.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when the break targets
        /// <paramref name="targetForeach"/>; otherwise <see langword="false"/>.
        /// </returns>
        private static bool BreakTargetsForeach(
            BreakStatementSyntax breakStatement,
            ForEachStatementSyntax targetForeach)
        {
            foreach (SyntaxNode ancestor in breakStatement.Ancestors())
            {
                switch (ancestor)
                {
                    case SwitchStatementSyntax:
                        return false;

                    case ForEachStatementSyntax foreachStatement:
                        return ReferenceEquals(
                            foreachStatement,
                            targetForeach);

                    case ForEachVariableStatementSyntax:
                    case ForStatementSyntax:
                    case WhileStatementSyntax:
                    case DoStatementSyntax:
                        return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Determines whether a continue statement continues the specified foreach
        /// rather than a nested loop.
        /// </summary>
        /// <param name="continueStatement">
        /// The continue statement to inspect.
        /// </param>
        /// <param name="targetForeach">
        /// The foreach whose completion is being analyzed.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when the continue targets
        /// <paramref name="targetForeach"/>; otherwise <see langword="false"/>.
        /// </returns>
        private static bool ContinueTargetsForeach(
            ContinueStatementSyntax continueStatement,
            ForEachStatementSyntax targetForeach)
        {
            foreach (SyntaxNode ancestor in continueStatement.Ancestors())
            {
                switch (ancestor)
                {
                    case ForEachStatementSyntax foreachStatement:
                        return ReferenceEquals(
                            foreachStatement,
                            targetForeach);

                    case ForEachVariableStatementSyntax:
                    case ForStatementSyntax:
                    case WhileStatementSyntax:
                    case DoStatementSyntax:
                        return false;
                }
            }

            return true;
        }
    }
}
