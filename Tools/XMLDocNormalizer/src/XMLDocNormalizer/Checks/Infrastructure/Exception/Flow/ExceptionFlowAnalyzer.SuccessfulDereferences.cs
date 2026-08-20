using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace XMLDocNormalizer.Checks.Infrastructure.Exception.Flow
{
    /// <summary>
    /// Contains value-fact reasoning based on earlier successful runtime
    /// dereferences.
    /// </summary>
    internal static partial class ExceptionFlowAnalyzer
    {
        /// <summary>
        /// Gets facts proven for a local or parameter because execution has
        /// already continued past an earlier statement or entered a nested
        /// construct after successful evaluation that necessarily dereferenced
        /// the same symbol.
        /// </summary>
        /// <param name="expression">
        /// The later symbol expression being evaluated.
        /// </param>
        /// <param name="symbol">
        /// The local or parameter symbol to inspect.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for symbol and data-flow analysis.
        /// </param>
        /// <returns>
        /// <see cref="ExceptionFlowValueFacts.NonNull"/> if an earlier
        /// successful dereference proves the symbol non-null; otherwise
        /// <see cref="ExceptionFlowValueFacts.None"/>.
        /// </returns>
        private static ExceptionFlowValueFacts
            GetFactsProvenByPrecedingSuccessfulDereference(
                ExpressionSyntax expression,
                ISymbol symbol,
                SemanticModel semanticModel)
        {
            StatementSyntax? currentStatement =
                expression.AncestorsAndSelf()
                    .OfType<StatementSyntax>()
                    .FirstOrDefault();

            if (currentStatement == null)
            {
                return ExceptionFlowValueFacts.None;
            }

            while (currentStatement.Parent
                   is BlockSyntax containingBlock)
            {
                int currentStatementIndex =
                    containingBlock.Statements.IndexOf(
                        currentStatement);

                if (currentStatementIndex < 0)
                {
                    break;
                }

                bool earlierFactsInvalidated =
                    false;

                for (int index = currentStatementIndex - 1;
                     index >= 0;
                     index--)
                {
                    StatementSyntax precedingStatement =
                        containingBlock.Statements[index];

                    if (StatementMayWriteSymbolForDereferenceFacts(
                            precedingStatement,
                            symbol,
                            semanticModel))
                    {
                        earlierFactsInvalidated =
                            true;
                        break;
                    }

                    if (StatementSuccessfulCompletionProvesSymbolNonNull(
                        precedingStatement,
                        symbol,
                        semanticModel))
                    {
                        return ExceptionFlowValueFacts.NonNull;
                    }
                }

                if (earlierFactsInvalidated)
                {
                    break;
                }

                if (EnclosingConditionProvesSuccessfulDereference(
                        containingBlock,
                        symbol,
                        semanticModel))
                {
                    return ExceptionFlowValueFacts.NonNull;
                }

                if (containingBlock.Parent
                        is CommonForEachStatementSyntax forEachStatement &&
                    ExpressionDefinitelyDereferencesSymbol(
                        forEachStatement.Expression,
                        symbol,
                        semanticModel))
                {
                    DataFlowAnalysis? sourceDataFlow =
                        semanticModel.AnalyzeDataFlow(
                            forEachStatement.Expression);

                    bool sourceMayWriteSymbol =
                        sourceDataFlow?.Succeeded != true ||
                        sourceDataFlow.WrittenInside.Any(
                            writtenSymbol =>
                                SymbolEqualityComparer.Default.Equals(
                                    writtenSymbol,
                                    symbol));

                    if (!sourceMayWriteSymbol &&
                        !StatementMayWriteSymbolForDereferenceFacts(
                            forEachStatement.Statement,
                            symbol,
                            semanticModel))
                    {
                        return ExceptionFlowValueFacts.NonNull;
                    }
                }

                currentStatement =
                    GetSafeContainingStatement(
                        containingBlock,
                        symbol,
                        semanticModel);

                if (currentStatement == null)
                {
                    break;
                }
            }

            return ExceptionFlowValueFacts.None;
        }

        /// <summary>
        /// Determines whether entering the supplied block proves a symbol to be
        /// non-null because the enclosing branch condition necessarily
        /// dereferenced that symbol while being evaluated successfully.
        /// </summary>
        /// <param name="block">
        /// The branch body containing the later symbol use.
        /// </param>
        /// <param name="symbol">
        /// The local or parameter symbol whose non-null fact is requested.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for symbol and data-flow analysis.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the enclosing condition necessarily
        /// dereferences <paramref name="symbol"/> without writing it; otherwise
        /// <see langword="false"/>.
        /// </returns>
        private static bool EnclosingConditionProvesSuccessfulDereference(
            BlockSyntax block,
            ISymbol symbol,
            SemanticModel semanticModel)
        {
            ExpressionSyntax? condition = null;

            if (block.Parent is IfStatementSyntax ifStatement)
            {
                condition =
                    ifStatement.Condition;
            }
            else if (block.Parent is ElseClauseSyntax elseClause &&
                     elseClause.Parent
                         is IfStatementSyntax elseIfStatement)
            {
                condition =
                    elseIfStatement.Condition;
            }

            if (condition == null)
            {
                return false;
            }

            DataFlowAnalysis? dataFlow =
                semanticModel.AnalyzeDataFlow(
                    condition);

            if (dataFlow?.Succeeded != true)
            {
                return false;
            }

            if (dataFlow.WrittenInside.Any(
                    writtenSymbol =>
                        SymbolEqualityComparer.Default.Equals(
                            writtenSymbol,
                            symbol)))
            {
                return false;
            }

            return ExpressionDefinitelyDereferencesSymbol(
                condition,
                symbol,
                semanticModel);
        }

        /// <summary>
        /// Determines conservatively whether a statement may write a
        /// specified symbol.
        /// </summary>
        /// <param name="statement">
        /// The statement to inspect.
        /// </param>
        /// <param name="symbol">
        /// The symbol whose writes are detected.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for data-flow analysis.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the statement writes the symbol or the
        /// data-flow analysis is unavailable; otherwise
        /// <see langword="false"/>.
        /// </returns>
        private static bool StatementMayWriteSymbolForDereferenceFacts(
            StatementSyntax statement,
            ISymbol symbol,
            SemanticModel semanticModel)
        {
            DataFlowAnalysis? dataFlow =
                semanticModel.AnalyzeDataFlow(
                    statement);

            if (dataFlow?.Succeeded != true)
            {
                return true;
            }

            return dataFlow.WrittenInside.Any(
                writtenSymbol =>
                    SymbolEqualityComparer.Default.Equals(
                        writtenSymbol,
                        symbol));
        }

        /// <summary>
        /// Determines whether successful completion of a statement requires a
        /// runtime dereference of a specified symbol.
        /// </summary>
        /// <param name="statement">
        /// The statement to inspect.
        /// </param>
        /// <param name="symbol">
        /// The local or parameter symbol whose dereference is sought.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for symbol resolution.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if normal completion of the statement
        /// requires the symbol to have been non-null; otherwise
        /// <see langword="false"/>.
        /// </returns>
        private static bool StatementDefinitelyDereferencesSymbol(
            StatementSyntax statement,
            ISymbol symbol,
            SemanticModel semanticModel)
        {
            switch (statement)
            {
                case LocalDeclarationStatementSyntax localDeclaration:
                    foreach (VariableDeclaratorSyntax variable
                             in localDeclaration.Declaration.Variables)
                    {
                        if (variable.Initializer != null &&
                            ExpressionDefinitelyDereferencesSymbol(
                                variable.Initializer.Value,
                                symbol,
                                semanticModel))
                        {
                            return true;
                        }
                    }

                    return false;

                case ExpressionStatementSyntax expressionStatement:
                    return ExpressionDefinitelyDereferencesSymbol(
                        expressionStatement.Expression,
                        symbol,
                        semanticModel);

                case IfStatementSyntax ifStatement:
                    return ExpressionDefinitelyDereferencesSymbol(
                        ifStatement.Condition,
                        symbol,
                        semanticModel);

                case SwitchStatementSyntax switchStatement:
                    return ExpressionDefinitelyDereferencesSymbol(
                        switchStatement.Expression,
                        symbol,
                        semanticModel);

                default:
                    return false;
            }
        }

        /// <summary>
        /// Determines whether evaluating an expression to completion
        /// necessarily performs a runtime dereference of a specified symbol.
        /// </summary>
        /// <param name="expression">
        /// The expression to inspect.
        /// </param>
        /// <param name="symbol">
        /// The local or parameter symbol whose dereference is sought.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for symbol resolution.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if evaluating the expression necessarily
        /// dereferences the symbol; otherwise <see langword="false"/>.
        /// </returns>
        private static bool ExpressionDefinitelyDereferencesSymbol(
            ExpressionSyntax expression,
            ISymbol symbol,
            SemanticModel semanticModel)
        {
            ExpressionSyntax unwrappedExpression =
                UnwrapParenthesizedExpression(
                    expression);

            switch (unwrappedExpression)
            {
                case MemberAccessExpressionSyntax memberAccess:
                    if (IsDirectRuntimeDereference(
                            memberAccess,
                            symbol,
                            semanticModel))
                    {
                        return true;
                    }

                    return ExpressionDefinitelyDereferencesSymbol(
                        memberAccess.Expression,
                        symbol,
                        semanticModel);

                case ElementAccessExpressionSyntax elementAccess:
                    if (ExpressionReferencesSymbol(
                            elementAccess.Expression,
                            symbol,
                            semanticModel))
                    {
                        return true;
                    }

                    if (ExpressionDefinitelyDereferencesSymbol(
                            elementAccess.Expression,
                            symbol,
                            semanticModel))
                    {
                        return true;
                    }

                    return elementAccess.ArgumentList.Arguments.Any(
                        argument =>
                            ExpressionDefinitelyDereferencesSymbol(
                                argument.Expression,
                                symbol,
                                semanticModel));

                case InvocationExpressionSyntax invocation:
                    return InvocationDefinitelyDereferencesSymbol(
                        invocation,
                        symbol,
                        semanticModel);

                case ObjectCreationExpressionSyntax creation:
                    return creation.ArgumentList?.Arguments.Any(
                               argument =>
                                   ExpressionDefinitelyDereferencesSymbol(
                                       argument.Expression,
                                       symbol,
                                       semanticModel)) ==
                           true;

                case ImplicitObjectCreationExpressionSyntax creation:
                    return creation.ArgumentList.Arguments.Any(
                        argument =>
                            ExpressionDefinitelyDereferencesSymbol(
                                argument.Expression,
                                symbol,
                                semanticModel));

                case AssignmentExpressionSyntax assignment:
                    if (assignment.IsKind(
                            SyntaxKind.CoalesceAssignmentExpression))
                    {
                        return ExpressionDefinitelyDereferencesSymbol(
                            assignment.Left,
                            symbol,
                            semanticModel);
                    }

                    return ExpressionDefinitelyDereferencesSymbol(
                               assignment.Left,
                               symbol,
                               semanticModel) ||
                           ExpressionDefinitelyDereferencesSymbol(
                               assignment.Right,
                               symbol,
                               semanticModel);

                case BinaryExpressionSyntax binaryExpression:
                    if (binaryExpression.IsKind(
                            SyntaxKind.LogicalAndExpression) ||
                        binaryExpression.IsKind(
                            SyntaxKind.LogicalOrExpression) ||
                        binaryExpression.IsKind(
                            SyntaxKind.CoalesceExpression))
                    {
                        return ExpressionDefinitelyDereferencesSymbol(
                            binaryExpression.Left,
                            symbol,
                            semanticModel);
                    }

                    return ExpressionDefinitelyDereferencesSymbol(
                               binaryExpression.Left,
                               symbol,
                               semanticModel) ||
                           ExpressionDefinitelyDereferencesSymbol(
                               binaryExpression.Right,
                               symbol,
                               semanticModel);

                case ConditionalExpressionSyntax conditionalExpression:
                    if (ExpressionDefinitelyDereferencesSymbol(
                            conditionalExpression.Condition,
                            symbol,
                            semanticModel))
                    {
                        return true;
                    }

                    return ExpressionDefinitelyDereferencesSymbol(
                               conditionalExpression.WhenTrue,
                               symbol,
                               semanticModel) &&
                           ExpressionDefinitelyDereferencesSymbol(
                               conditionalExpression.WhenFalse,
                               symbol,
                               semanticModel);

                case IsPatternExpressionSyntax isPatternExpression:
                    return ExpressionDefinitelyDereferencesSymbol(
                        isPatternExpression.Expression,
                        symbol,
                        semanticModel);

                case CastExpressionSyntax castExpression:
                    return ExpressionDefinitelyDereferencesSymbol(
                        castExpression.Expression,
                        symbol,
                        semanticModel);

                case CheckedExpressionSyntax checkedExpression:
                    return ExpressionDefinitelyDereferencesSymbol(
                        checkedExpression.Expression,
                        symbol,
                        semanticModel);

                case PrefixUnaryExpressionSyntax prefixExpression:
                    return ExpressionDefinitelyDereferencesSymbol(
                        prefixExpression.Operand,
                        symbol,
                        semanticModel);

                case PostfixUnaryExpressionSyntax postfixExpression:
                    return ExpressionDefinitelyDereferencesSymbol(
                        postfixExpression.Operand,
                        symbol,
                        semanticModel);

                case AwaitExpressionSyntax awaitExpression:
                    return ExpressionDefinitelyDereferencesSymbol(
                        awaitExpression.Expression,
                        symbol,
                        semanticModel);

                case InterpolatedStringExpressionSyntax interpolatedString:
                    return interpolatedString.Contents
                        .OfType<InterpolationSyntax>()
                        .Any(
                            interpolation =>
                                ExpressionDefinitelyDereferencesSymbol(
                                    interpolation.Expression,
                                    symbol,
                                    semanticModel));

                case ConditionalAccessExpressionSyntax conditionalAccess:
                    return ExpressionDefinitelyDereferencesSymbol(
                        conditionalAccess.Expression,
                        symbol,
                        semanticModel);

                default:
                    return false;
            }
        }

        /// <summary>
        /// Determines whether evaluating an invocation necessarily
        /// dereferences a specified symbol.
        /// </summary>
        /// <param name="invocation">
        /// The invocation to inspect.
        /// </param>
        /// <param name="symbol">
        /// The local or parameter symbol whose dereference is sought.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for method resolution.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the invocation necessarily dereferences
        /// the symbol; otherwise <see langword="false"/>.
        /// </returns>
        private static bool InvocationDefinitelyDereferencesSymbol(
            InvocationExpressionSyntax invocation,
            ISymbol symbol,
            SemanticModel semanticModel)
        {
            if (invocation.Expression
                    is IdentifierNameSyntax identifier &&
                identifier.Identifier.ValueText ==
                    "nameof")
            {
                return false;
            }

            if (invocation.Expression
                    is MemberAccessExpressionSyntax memberAccess)
            {
                if (IsDirectRuntimeDereference(
                        memberAccess,
                        symbol,
                        semanticModel))
                {
                    return true;
                }

                if (ExpressionDefinitelyDereferencesSymbol(
                        memberAccess.Expression,
                        symbol,
                        semanticModel))
                {
                    return true;
                }
            }
            else if (ExpressionReferencesSymbol(
                         invocation.Expression,
                         symbol,
                         semanticModel) &&
                     semanticModel.GetTypeInfo(
                         invocation.Expression).Type?.TypeKind ==
                         TypeKind.Delegate)
            {
                return true;
            }

            return invocation.ArgumentList.Arguments.Any(
                argument =>
                    ExpressionDefinitelyDereferencesSymbol(
                        argument.Expression,
                        symbol,
                        semanticModel));
        }

        /// <summary>
        /// Determines whether one member-access expression performs an
        /// instance dereference of a specified symbol.
        /// </summary>
        /// <param name="memberAccess">
        /// The member access to inspect.
        /// </param>
        /// <param name="symbol">
        /// The local or parameter symbol whose dereference is sought.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for member resolution.
        /// </param>
        /// <returns>
        /// <see langword="true"/> for an instance member access on the
        /// symbol; otherwise <see langword="false"/>.
        /// </returns>
        private static bool IsDirectRuntimeDereference(
            MemberAccessExpressionSyntax memberAccess,
            ISymbol symbol,
            SemanticModel semanticModel)
        {
            if (!ExpressionReferencesSymbol(
                    memberAccess.Expression,
                    symbol,
                    semanticModel))
            {
                return false;
            }

            SymbolInfo memberSymbolInfo =
                semanticModel.GetSymbolInfo(
                    memberAccess);

            return memberSymbolInfo.Symbol switch
            {
                IMethodSymbol methodSymbol =>
                    !methodSymbol.IsStatic &&
                    methodSymbol.ReducedFrom == null,

                IPropertySymbol propertySymbol =>
                    !propertySymbol.IsStatic,

                IFieldSymbol fieldSymbol =>
                    !fieldSymbol.IsStatic,

                IEventSymbol eventSymbol =>
                    !eventSymbol.IsStatic,

                _ => false
            };
        }
    }
}
