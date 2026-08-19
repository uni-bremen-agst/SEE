using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace XMLDocNormalizer.Checks.Infrastructure.Exception.Flow
{
    /// <summary>
    /// Contains value-fact reasoning derived from successful completion of
    /// source-level helper calls.
    /// </summary>
    internal static partial class ExceptionFlowAnalyzer
    {
        /// <summary>
        /// Determines whether successful completion of a statement proves a
        /// local or parameter symbol to be non-null.
        /// </summary>
        /// <param name="statement">
        /// The statement whose successful completion is inspected.
        /// </param>
        /// <param name="symbol">
        /// The local or parameter symbol whose non-null state is requested.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for symbol, data-flow, and callable
        /// analysis.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if successful completion proves the symbol
        /// non-null; otherwise <see langword="false"/>.
        /// </returns>
        private static bool
            StatementSuccessfulCompletionProvesSymbolNonNull(
                StatementSyntax statement,
                ISymbol symbol,
                SemanticModel semanticModel)
        {
            HashSet<IMethodSymbol> inspectedMethods =
                new(SymbolEqualityComparer.Default);

            return StatementSuccessfulCompletionProvesSymbolNonNull(
                statement,
                symbol,
                semanticModel,
                inspectedMethods);
        }

        /// <summary>
        /// Determines whether successful completion of a statement proves a
        /// symbol non-null while preventing recursion between source helpers.
        /// </summary>
        /// <param name="statement">
        /// The statement whose successful completion is inspected.
        /// </param>
        /// <param name="symbol">
        /// The local or parameter symbol whose non-null state is requested.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for symbol and flow analysis.
        /// </param>
        /// <param name="inspectedMethods">
        /// The source methods already inspected on the current proof path.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if successful completion proves the symbol
        /// non-null; otherwise <see langword="false"/>.
        /// </returns>
        private static bool
            StatementSuccessfulCompletionProvesSymbolNonNull(
                StatementSyntax statement,
                ISymbol symbol,
                SemanticModel semanticModel,
                HashSet<IMethodSymbol> inspectedMethods)
        {
            if (StatementDefinitelyDereferencesSymbol(
                    statement,
                    symbol,
                    semanticModel))
            {
                return true;
            }

            switch (statement)
            {
                case ForEachStatementSyntax forEachStatement:
                    if (ExpressionDefinitelyDereferencesSymbol(
                            forEachStatement.Expression,
                            symbol,
                            semanticModel))
                    {
                        return true;
                    }

                    break;

                case ForEachVariableStatementSyntax
                        forEachVariableStatement:
                    if (ExpressionDefinitelyDereferencesSymbol(
                            forEachVariableStatement.Expression,
                            symbol,
                            semanticModel))
                    {
                        return true;
                    }

                    break;

                case ForStatementSyntax forStatement
                    when forStatement.Condition != null:
                    if (ExpressionDefinitelyDereferencesSymbol(
                            forStatement.Condition,
                            symbol,
                            semanticModel))
                    {
                        return true;
                    }

                    break;

                case WhileStatementSyntax whileStatement:
                    if (ExpressionDefinitelyDereferencesSymbol(
                            whileStatement.Condition,
                            symbol,
                            semanticModel))
                    {
                        return true;
                    }

                    break;
            }

            return StatementSuccessfulCalleeCompletionProvesSymbolNonNull(
                statement,
                symbol,
                semanticModel,
                inspectedMethods);
        }

        /// <summary>
        /// Determines whether a source-helper call that must complete for a
        /// statement to complete proves one of its arguments non-null.
        /// </summary>
        /// <param name="statement">
        /// The statement containing the helper call.
        /// </param>
        /// <param name="symbol">
        /// The caller symbol whose non-null state is requested.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for symbol resolution.
        /// </param>
        /// <param name="inspectedMethods">
        /// The source methods already inspected on the current proof path.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if successful helper completion proves the
        /// caller symbol non-null; otherwise <see langword="false"/>.
        /// </returns>
        private static bool
            StatementSuccessfulCalleeCompletionProvesSymbolNonNull(
                StatementSyntax statement,
                ISymbol symbol,
                SemanticModel semanticModel,
                HashSet<IMethodSymbol> inspectedMethods)
        {
            switch (statement)
            {
                case LocalDeclarationStatementSyntax localDeclaration:
                    foreach (VariableDeclaratorSyntax variable
                             in localDeclaration.Declaration.Variables)
                    {
                        if (variable.Initializer != null &&
                            ExpressionSuccessfulCalleeCompletionProvesSymbolNonNull(
                                variable.Initializer.Value,
                                symbol,
                                semanticModel,
                                inspectedMethods))
                        {
                            return true;
                        }
                    }

                    return false;

                case ExpressionStatementSyntax expressionStatement:
                    return ExpressionSuccessfulCalleeCompletionProvesSymbolNonNull(
                        expressionStatement.Expression,
                        symbol,
                        semanticModel,
                        inspectedMethods);

                default:
                    return false;
            }
        }

        /// <summary>
        /// Determines whether successful completion of a source-helper call
        /// contained in an expression proves a caller symbol non-null.
        /// </summary>
        /// <param name="expression">
        /// The expression whose required helper call is inspected.
        /// </param>
        /// <param name="symbol">
        /// The caller symbol whose non-null state is requested.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for symbol resolution.
        /// </param>
        /// <param name="inspectedMethods">
        /// The source methods already inspected on the current proof path.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if successful completion proves the symbol
        /// non-null; otherwise <see langword="false"/>.
        /// </returns>
        private static bool
            ExpressionSuccessfulCalleeCompletionProvesSymbolNonNull(
                ExpressionSyntax expression,
                ISymbol symbol,
                SemanticModel semanticModel,
                HashSet<IMethodSymbol> inspectedMethods)
        {
            ExpressionSyntax unwrappedExpression =
                UnwrapParenthesizedExpression(
                    expression);

            switch (unwrappedExpression)
            {
                case InvocationExpressionSyntax invocation:
                    return InvocationSuccessfulCompletionProvesSymbolNonNull(
                        invocation,
                        symbol,
                        semanticModel,
                        inspectedMethods);

                case CastExpressionSyntax castExpression:
                    return ExpressionSuccessfulCalleeCompletionProvesSymbolNonNull(
                        castExpression.Expression,
                        symbol,
                        semanticModel,
                        inspectedMethods);

                case CheckedExpressionSyntax checkedExpression:
                    return ExpressionSuccessfulCalleeCompletionProvesSymbolNonNull(
                        checkedExpression.Expression,
                        symbol,
                        semanticModel,
                        inspectedMethods);

                case AwaitExpressionSyntax:
                    return false;

                case AssignmentExpressionSyntax assignment:
                    return ExpressionSuccessfulCalleeCompletionProvesSymbolNonNull(
                        assignment.Right,
                        symbol,
                        semanticModel,
                        inspectedMethods);

                default:
                    return false;
            }
        }

        /// <summary>
        /// Determines whether successful completion of one invocation proves
        /// a caller local or parameter non-null through a source-level callee
        /// postcondition.
        /// </summary>
        /// <param name="invocation">
        /// The source invocation to inspect.
        /// </param>
        /// <param name="symbol">
        /// The caller symbol whose non-null state is requested.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for invocation and argument resolution.
        /// </param>
        /// <param name="inspectedMethods">
        /// The source methods already inspected on the current proof path.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if successful completion proves the caller
        /// symbol non-null; otherwise <see langword="false"/>.
        /// </returns>
        private static bool
            InvocationSuccessfulCompletionProvesSymbolNonNull(
                InvocationExpressionSyntax invocation,
                ISymbol symbol,
                SemanticModel semanticModel,
                HashSet<IMethodSymbol> inspectedMethods)
        {
            SymbolInfo symbolInfo =
                semanticModel.GetSymbolInfo(
                    invocation);

            if (symbolInfo.Symbol
                    is not IMethodSymbol methodSymbol ||
                !methodSymbol.IsStatic ||
                methodSymbol.IsAsync ||
                methodSymbol.DeclaringSyntaxReferences.Length == 0)
            {
                return false;
            }

            SeparatedSyntaxList<ArgumentSyntax> arguments =
                invocation.ArgumentList.Arguments;

            foreach (ArgumentSyntax argument
                     in arguments)
            {
                if (!argument.RefKindKeyword.IsKind(
                        SyntaxKind.None) &&
                    ExpressionReferencesSymbol(
                        argument.Expression,
                        symbol,
                        semanticModel))
                {
                    return false;
                }

                ITypeSymbol? argumentType =
                    semanticModel.GetTypeInfo(
                        argument.Expression).ConvertedType;

                if (argumentType?.TypeKind ==
                    TypeKind.Delegate)
                {
                    return false;
                }
            }

            for (int argumentIndex = 0;
                 argumentIndex < arguments.Count;
                 argumentIndex++)
            {
                ArgumentSyntax argument =
                    arguments[argumentIndex];

                if (!argument.RefKindKeyword.IsKind(
                        SyntaxKind.None) ||
                    !ExpressionReferencesSymbol(
                        argument.Expression,
                        symbol,
                        semanticModel))
                {
                    continue;
                }

                int parameterIndex =
                    GetParameterIndexForArgument(
                        argument,
                        argumentIndex,
                        methodSymbol);

                if (parameterIndex < 0 ||
                    parameterIndex >=
                        methodSymbol.Parameters.Length)
                {
                    continue;
                }

                if (MethodSuccessfulCompletionProvesParameterNonNull(
                        methodSymbol,
                        parameterIndex,
                        semanticModel.Compilation,
                        inspectedMethods))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Determines whether every normal completion of a source method proves one
        /// parameter non-null.
        /// </summary>
        /// <param name="methodSymbol">
        /// The source method to inspect.
        /// </param>
        /// <param name="parameterIndex">
        /// The ordinal of the parameter whose postcondition is requested.
        /// </param>
        /// <param name="compilation">
        /// The compilation containing the method implementation.
        /// </param>
        /// <param name="inspectedMethods">
        /// The source methods already inspected on the current proof path.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if every normal completion proves the parameter
        /// non-null; otherwise <see langword="false"/>.
        /// </returns>
        private static bool
            MethodSuccessfulCompletionProvesParameterNonNull(
                IMethodSymbol methodSymbol,
                int parameterIndex,
                Compilation compilation,
                HashSet<IMethodSymbol> inspectedMethods)
        {
            IMethodSymbol normalizedMethod =
                methodSymbol.OriginalDefinition;

            if (parameterIndex < 0 ||
                parameterIndex >=
                    normalizedMethod.Parameters.Length ||
                !normalizedMethod.IsStatic ||
                normalizedMethod.IsAsync)
            {
                return false;
            }

            if (!inspectedMethods.Add(
                    normalizedMethod))
            {
                return false;
            }

            bool result =
                false;

            try
            {
                foreach (SyntaxReference syntaxReference
                         in normalizedMethod.DeclaringSyntaxReferences)
                {
                    SyntaxNode declaration =
                        syntaxReference.GetSyntax();

                    if (DeclarationContainsYield(
                            declaration))
                    {
                        break;
                    }

                    SemanticModel targetSemanticModel;

                    try
                    {
                        targetSemanticModel =
                            compilation.GetSemanticModel(
                                declaration.SyntaxTree);
                    }
                    catch (ArgumentException)
                    {
                        continue;
                    }

                    if (!TryGetDeclaredMethodAndBody(
                            declaration,
                            targetSemanticModel,
                            out IMethodSymbol? declaredMethod,
                            out BlockSyntax? body,
                            out ArrowExpressionClauseSyntax?
                                expressionBody) ||
                        declaredMethod == null ||
                        parameterIndex >=
                            declaredMethod.Parameters.Length)
                    {
                        continue;
                    }

                    IParameterSymbol parameterSymbol =
                        declaredMethod.Parameters[
                            parameterIndex];

                    if (body != null)
                    {
                        result =
                            BlockSuccessfulCompletionProvesParameterNonNull(
                                body,
                                parameterSymbol,
                                targetSemanticModel,
                                inspectedMethods);

                        break;
                    }

                    if (expressionBody != null)
                    {
                        result =
                            ExpressionSuccessfulCompletionProvesSymbolNonNull(
                                expressionBody.Expression,
                                parameterSymbol,
                                targetSemanticModel,
                                inspectedMethods);

                        break;
                    }
                }
            }
            finally
            {
                inspectedMethods.Remove(
                    normalizedMethod);
            }

            return result;
        }

        /// <summary>
        /// Gets the declared method symbol and executable body represented by
        /// one source declaration.
        /// </summary>
        /// <param name="declaration">
        /// The source declaration.
        /// </param>
        /// <param name="semanticModel">
        /// The declaration's semantic model.
        /// </param>
        /// <param name="methodSymbol">
        /// The resolved declared method.
        /// </param>
        /// <param name="body">
        /// The block body, when present.
        /// </param>
        /// <param name="expressionBody">
        /// The expression body, when present.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when the declaration represents a supported
        /// source method; otherwise <see langword="false"/>.
        /// </returns>
        private static bool TryGetDeclaredMethodAndBody(
            SyntaxNode declaration,
            SemanticModel semanticModel,
            out IMethodSymbol? methodSymbol,
            out BlockSyntax? body,
            out ArrowExpressionClauseSyntax? expressionBody)
        {
            methodSymbol = null;
            body = null;
            expressionBody = null;

            switch (declaration)
            {
                case MethodDeclarationSyntax methodDeclaration:
                    methodSymbol =
                        semanticModel.GetDeclaredSymbol(
                            methodDeclaration);

                    body =
                        methodDeclaration.Body;

                    expressionBody =
                        methodDeclaration.ExpressionBody;

                    return methodSymbol != null;

                case LocalFunctionStatementSyntax localFunction:
                    methodSymbol =
                        semanticModel.GetDeclaredSymbol(
                            localFunction);

                    body =
                        localFunction.Body;

                    expressionBody =
                        localFunction.ExpressionBody;

                    return methodSymbol != null;

                default:
                    return false;
            }
        }

        /// <summary>
        /// Determines whether a declaration is iterator-based and therefore
        /// does not execute its body when the method itself is invoked.
        /// </summary>
        /// <param name="declaration">
        /// The source declaration to inspect.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the declaration contains a
        /// <c>yield</c> statement outside nested callables; otherwise
        /// <see langword="false"/>.
        /// </returns>
        private static bool DeclarationContainsYield(
            SyntaxNode declaration)
        {
            return declaration
                .DescendantNodes(
                    static node =>
                        node is not
                            AnonymousFunctionExpressionSyntax &&
                        node is not
                            LocalFunctionStatementSyntax)
                .OfType<YieldStatementSyntax>()
                .Any();
        }

        /// <summary>
        /// Determines whether every normal exit from a method block proves a
        /// parameter non-null.
        /// </summary>
        /// <param name="body">
        /// The executable method body.
        /// </param>
        /// <param name="parameterSymbol">
        /// The parameter whose successful-return postcondition is requested.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for flow analysis.
        /// </param>
        /// <param name="inspectedMethods">
        /// The source methods already inspected on the current proof path.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if every normal exit proves the parameter
        /// non-null; otherwise <see langword="false"/>.
        /// </returns>
        private static bool
            BlockSuccessfulCompletionProvesParameterNonNull(
                BlockSyntax body,
                IParameterSymbol parameterSymbol,
                SemanticModel semanticModel,
                HashSet<IMethodSymbol> inspectedMethods)
        {
            ReturnStatementSyntax[] returnStatements =
                body.DescendantNodes(
                        static node =>
                            node is not
                                AnonymousFunctionExpressionSyntax &&
                            node is not
                                LocalFunctionStatementSyntax)
                    .OfType<ReturnStatementSyntax>()
                    .ToArray();

            bool hasNormalExit =
                false;

            foreach (ReturnStatementSyntax returnStatement
                     in returnStatements)
            {
                hasNormalExit =
                    true;

                if (!ReturnSuccessfulCompletionProvesSymbolNonNull(
                        returnStatement,
                        parameterSymbol,
                        semanticModel,
                        inspectedMethods))
                {
                    return false;
                }
            }

            ControlFlowAnalysis? controlFlow =
                semanticModel.AnalyzeControlFlow(
                    body);

            if (controlFlow?.Succeeded != true)
            {
                return false;
            }

            if (controlFlow.EndPointIsReachable)
            {
                hasNormalExit =
                    true;

                if (!BlockEndSuccessfulCompletionProvesSymbolNonNull(
                        body,
                        parameterSymbol,
                        semanticModel,
                        inspectedMethods))
                {
                    return false;
                }
            }

            return hasNormalExit;
        }

        /// <summary>
        /// Determines whether successful evaluation of an expression proves
        /// a symbol non-null either directly or through a required helper
        /// invocation.
        /// </summary>
        /// <param name="expression">
        /// The expression being completed.
        /// </param>
        /// <param name="symbol">
        /// The parameter symbol whose non-null state is requested.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for expression analysis.
        /// </param>
        /// <param name="inspectedMethods">
        /// The source methods already inspected on the current proof path.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if successful expression completion proves
        /// the symbol non-null; otherwise <see langword="false"/>.
        /// </returns>
        private static bool
            ExpressionSuccessfulCompletionProvesSymbolNonNull(
                ExpressionSyntax expression,
                ISymbol symbol,
                SemanticModel semanticModel,
                HashSet<IMethodSymbol> inspectedMethods)
        {
            if (ExpressionDefinitelyDereferencesSymbol(
                    expression,
                    symbol,
                    semanticModel))
            {
                return true;
            }

            return ExpressionSuccessfulCalleeCompletionProvesSymbolNonNull(
                expression,
                symbol,
                semanticModel,
                inspectedMethods);
        }

        /// <summary>
        /// Determines whether one normal return path proves a parameter
        /// non-null.
        /// </summary>
        /// <param name="returnStatement">
        /// The return statement representing the normal exit.
        /// </param>
        /// <param name="symbol">
        /// The parameter symbol whose non-null state is requested.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for flow analysis.
        /// </param>
        /// <param name="inspectedMethods">
        /// The source methods already inspected on the current proof path.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the return path proves the parameter
        /// non-null; otherwise <see langword="false"/>.
        /// </returns>
        private static bool
            ReturnSuccessfulCompletionProvesSymbolNonNull(
                ReturnStatementSyntax returnStatement,
                ISymbol symbol,
                SemanticModel semanticModel,
                HashSet<IMethodSymbol> inspectedMethods)
        {
            if (returnStatement.Expression != null &&
                ExpressionSuccessfulCompletionProvesSymbolNonNull(
                    returnStatement.Expression,
                    symbol,
                    semanticModel,
                    inspectedMethods))
            {
                return true;
            }

            return ExecutionBeforeStatementProvesSymbolNonNull(
                returnStatement,
                symbol,
                semanticModel,
                inspectedMethods);
        }

        /// <summary>
        /// Determines whether reaching one statement proves a symbol non-null
        /// through preceding successful execution.
        /// </summary>
        /// <param name="statement">
        /// The later statement that has been reached.
        /// </param>
        /// <param name="symbol">
        /// The symbol whose non-null state is requested.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for flow analysis.
        /// </param>
        /// <param name="inspectedMethods">
        /// The source methods already inspected on the current proof path.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if preceding successful execution proves
        /// the symbol non-null; otherwise <see langword="false"/>.
        /// </returns>
        private static bool
            ExecutionBeforeStatementProvesSymbolNonNull(
                StatementSyntax statement,
                ISymbol symbol,
                SemanticModel semanticModel,
                HashSet<IMethodSymbol> inspectedMethods)
        {
            StatementSyntax? currentStatement =
                statement;

            while (currentStatement.Parent
                   is BlockSyntax containingBlock)
            {
                int currentStatementIndex =
                    containingBlock.Statements.IndexOf(
                        currentStatement);

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

                    if (StatementMayWriteSymbolForDereferenceFacts(
                            precedingStatement,
                            symbol,
                            semanticModel))
                    {
                        return false;
                    }

                    if (StatementSuccessfulCompletionProvesSymbolNonNull(
                            precedingStatement,
                            symbol,
                            semanticModel,
                            inspectedMethods))
                    {
                        return true;
                    }
                }

                if (EnclosingConditionProvesSuccessfulDereference(
                        containingBlock,
                        symbol,
                        semanticModel))
                {
                    return true;
                }

                currentStatement =
                    GetSafeContainingStatement(
                        containingBlock,
                        symbol,
                        semanticModel);

                if (currentStatement == null)
                {
                    return false;
                }
            }

            return false;
        }

        /// <summary>
        /// Determines whether normal fall-through at the end of a method body
        /// proves a parameter non-null.
        /// </summary>
        /// <param name="body">
        /// The method body whose reachable end point is inspected.
        /// </param>
        /// <param name="symbol">
        /// The parameter symbol whose non-null state is requested.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for flow analysis.
        /// </param>
        /// <param name="inspectedMethods">
        /// The source methods already inspected on the current proof path.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if reaching the block end proves the symbol
        /// non-null; otherwise <see langword="false"/>.
        /// </returns>
        private static bool
            BlockEndSuccessfulCompletionProvesSymbolNonNull(
                BlockSyntax body,
                ISymbol symbol,
                SemanticModel semanticModel,
                HashSet<IMethodSymbol> inspectedMethods)
        {
            for (int index = body.Statements.Count - 1;
                 index >= 0;
                 index--)
            {
                StatementSyntax statement =
                    body.Statements[index];

                if (StatementMayWriteSymbolForDereferenceFacts(
                        statement,
                        symbol,
                        semanticModel))
                {
                    return false;
                }

                if (StatementSuccessfulCompletionProvesSymbolNonNull(
                        statement,
                        symbol,
                        semanticModel,
                        inspectedMethods))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
