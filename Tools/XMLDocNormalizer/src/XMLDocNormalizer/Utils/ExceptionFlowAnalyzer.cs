using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using XMLDocNormalizer.Execution.Semantic;
using XMLDocNormalizer.Models.DTO;

namespace XMLDocNormalizer.Utils
{
    /// <summary>
    /// Performs direct and transitive analysis of exceptions that may escape from a member.
    /// </summary>
    /// <remarks>
    /// The analysis is conservative and attempts to suppress exceptions that are fully handled
    /// by surrounding catch-clauses. Catch filters are treated conservatively and therefore do
    /// not suppress the caught exception flow.
    /// </remarks>
    internal static class ExceptionFlowAnalyzer
    {
        /// <summary>
        /// Determines how exception flow should be traversed.
        /// </summary>
        private enum ExceptionFlowTraversalMode
        {
            /// <summary>
            /// Only directly thrown exceptions inside the analyzed member are considered.
            /// </summary>
            Direct,

            /// <summary>
            /// Exceptions are analyzed transitively through invoked members and other reachable constructs.
            /// </summary>
            Transitive
        }

        /// <summary>
        /// Analyzes all exception types that may escape directly from the specified member.
        /// Exceptions that are fully caught and handled within the member are suppressed.
        /// </summary>
        /// <param name="member">The member whose direct exception flow should be analyzed.</param>
        /// <param name="semanticContext">The project-closure semantic context.</param>
        /// <returns>
        /// A result object containing all proven directly escaping exception types.
        /// </returns>
        public static ExceptionFlowAnalysisResult AnalyzeDirectlyThrownExceptions(
            MemberDeclarationSyntax member,
            ProjectClosureSemanticContext semanticContext)
        {
            ExceptionFlowAnalysisResult result = new();

            if (!semanticContext.TryGetSemanticModel(member.SyntaxTree, out SemanticModel semanticModel) ||
                semanticModel == null)
            {
                return result;
            }

            if (!SyntaxUtils.TryGetMemberBody(member, out SyntaxNode? body) || body == null)
            {
                return result;
            }

            HashSet<ISymbol> visited = new(SymbolEqualityComparer.Default);

            AnalyzeNode(
                body,
                semanticModel,
                semanticContext,
                result,
                visited,
                ExceptionFlowTraversalMode.Direct);

            return result;
        }

        /// <summary>
        /// Analyzes all exception types that may escape directly or transitively from the specified member.
        /// Exceptions that are fully caught and handled within the analyzed member bodies are suppressed.
        /// </summary>
        /// <param name="member">The member whose transitive exception flow should be analyzed.</param>
        /// <param name="semanticContext">The project-closure semantic context.</param>
        /// <returns>
        /// A result object containing all proven transitively escaping exception types and any uncertainty
        /// that could not be resolved safely.
        /// </returns>
        public static ExceptionFlowAnalysisResult AnalyzeTransitivelyThrownExceptions(
            MemberDeclarationSyntax member,
            ProjectClosureSemanticContext semanticContext)
        {
            ExceptionFlowAnalysisResult result = new();

            if (!semanticContext.TryGetSemanticModel(member.SyntaxTree, out SemanticModel semanticModel) ||
                semanticModel == null)
            {
                return result;
            }

            if (!SyntaxUtils.TryGetMemberBody(member, out SyntaxNode? body) || body == null)
            {
                return result;
            }

            HashSet<ISymbol> visited = new(SymbolEqualityComparer.Default);

            AnalyzeNode(
                body,
                semanticModel,
                semanticContext,
                result,
                visited,
                ExceptionFlowTraversalMode.Transitive);

            return result;
        }

        /// <summary>
        /// Analyzes a syntax node and all nested try-statements below it.
        /// Nested try-statements are processed separately so that catch-based suppression can be applied.
        /// </summary>
        /// <param name="node">The node to analyze.</param>
        /// <param name="semanticModel">The semantic model used for symbol resolution.</param>
        /// <param name="semanticContext">The project-closure semantic context.</param>
        /// <param name="result">The accumulated exception-flow result.</param>
        /// <param name="visited">The set of already visited symbols used to prevent recursion cycles.</param>
        /// <param name="mode">The traversal mode.</param>
        private static void AnalyzeNode(
            SyntaxNode node,
            SemanticModel semanticModel,
            ProjectClosureSemanticContext semanticContext,
            ExceptionFlowAnalysisResult result,
            HashSet<ISymbol> visited,
            ExceptionFlowTraversalMode mode)
        {
            if (node is TryStatementSyntax tryStatement)
            {
                AnalyzeTryStatement(tryStatement, semanticModel, semanticContext, result, visited, mode);
                return;
            }

            AnalyzeSimpleNode(node, semanticModel, semanticContext, result, visited, mode);

            foreach (TryStatementSyntax nestedTry in GetNestedTryStatements(node))
            {
                AnalyzeTryStatement(nestedTry, semanticModel, semanticContext, result, visited, mode);
            }
        }

        /// <summary>
        /// Analyzes a syntax node excluding nested try-statements.
        /// </summary>
        /// <param name="node">The node to analyze.</param>
        /// <param name="semanticModel">The semantic model used for symbol resolution.</param>
        /// <param name="semanticContext">The project-closure semantic context.</param>
        /// <param name="result">The accumulated exception-flow result.</param>
        /// <param name="visited">The set of already visited symbols used to prevent recursion cycles.</param>
        /// <param name="mode">The traversal mode.</param>
        private static void AnalyzeSimpleNode(
            SyntaxNode node,
            SemanticModel semanticModel,
            ProjectClosureSemanticContext semanticContext,
            ExceptionFlowAnalysisResult result,
            HashSet<ISymbol> visited,
            ExceptionFlowTraversalMode mode)
        {
            AnalyzeThrows(node, semanticModel, result);

            if (mode == ExceptionFlowTraversalMode.Direct)
            {
                return;
            }

            AnalyzeInvocations(node, semanticModel, semanticContext, result, visited);
            AnalyzeObjectCreations(node, semanticModel, semanticContext, result, visited);
            AnalyzePropertyAndIndexerAccesses(node, semanticModel, semanticContext, result, visited);
        }

        /// <summary>
        /// Analyzes a try-statement and suppresses exceptions from the try-block that are fully
        /// handled by one of its catch-clauses.
        /// </summary>
        /// <param name="tryStatement">The try-statement to analyze.</param>
        /// <param name="semanticModel">The semantic model used for symbol resolution.</param>
        /// <param name="semanticContext">The project-closure semantic context.</param>
        /// <param name="result">The accumulated exception-flow result.</param>
        /// <param name="visited">The set of already visited symbols used to prevent recursion cycles.</param>
        /// <param name="mode">The traversal mode.</param>
        private static void AnalyzeTryStatement(
            TryStatementSyntax tryStatement,
            SemanticModel semanticModel,
            ProjectClosureSemanticContext semanticContext,
            ExceptionFlowAnalysisResult result,
            HashSet<ISymbol> visited,
            ExceptionFlowTraversalMode mode)
        {
            ExceptionFlowAnalysisResult tryResult = new();
            AnalyzeNode(
                tryStatement.Block,
                semanticModel,
                semanticContext,
                tryResult,
                visited,
                mode);

            SuppressCaughtExceptionsFromTry(tryStatement, semanticModel, tryResult);

            MergeResults(result, tryResult);

            foreach (CatchClauseSyntax catchClause in tryStatement.Catches)
            {
                if (catchClause.Filter != null)
                {
                    AnalyzeNode(
                        catchClause.Filter.FilterExpression,
                        semanticModel,
                        semanticContext,
                        result,
                        visited,
                        mode);
                }

                if (catchClause.Block != null)
                {
                    AnalyzeNode(
                        catchClause.Block,
                        semanticModel,
                        semanticContext,
                        result,
                        visited,
                        mode);
                }
            }

            if (tryStatement.Finally != null)
            {
                AnalyzeNode(
                    tryStatement.Finally.Block,
                    semanticModel,
                    semanticContext,
                    result,
                    visited,
                    mode);
            }
        }

        /// <summary>
        /// Suppresses exceptions from a try-block that are fully handled by the associated catch-clauses.
        /// </summary>
        /// <param name="tryStatement">The try-statement whose catches should be evaluated.</param>
        /// <param name="semanticModel">The semantic model used for catch type resolution.</param>
        /// <param name="tryResult">The exception-flow result produced for the try-block.</param>
        private static void SuppressCaughtExceptionsFromTry(
            TryStatementSyntax tryStatement,
            SemanticModel semanticModel,
            ExceptionFlowAnalysisResult tryResult)
        {
            foreach (CatchClauseSyntax catchClause in tryStatement.Catches)
            {
                if (!CatchSuppressesOriginalException(catchClause))
                {
                    continue;
                }

                if (catchClause.Filter != null)
                {
                    continue;
                }

                if (IsCatchAll(catchClause, semanticModel))
                {
                    tryResult.ThrownExceptions.Clear();
                    tryResult.UncertainTargets.Clear();
                    return;
                }

                INamedTypeSymbol? caughtType = GetCaughtExceptionType(catchClause, semanticModel);
                if (caughtType == null)
                {
                    continue;
                }

                tryResult.ThrownExceptions.RemoveWhere(
                    thrownType => thrownType.InheritsFromOrEquals(caughtType));
            }
        }

        /// <summary>
        /// Determines whether a catch-clause fully handles the original caught exception
        /// instead of rethrowing it.
        /// </summary>
        /// <param name="catchClause">The catch-clause to inspect.</param>
        /// <returns>
        /// <see langword="true"/> if the original exception is not rethrown by the catch-clause;
        /// otherwise <see langword="false"/>.
        /// </returns>
        private static bool CatchSuppressesOriginalException(CatchClauseSyntax catchClause)
        {
            if (catchClause.Block == null)
            {
                return true;
            }

            string? caughtIdentifier = catchClause.Declaration?.Identifier.ValueText;
            if (string.IsNullOrWhiteSpace(caughtIdentifier))
            {
                caughtIdentifier = null;
            }

            foreach (ThrowStatementSyntax throwStatement in catchClause.Block.DescendantNodesAndSelf().OfType<ThrowStatementSyntax>())
            {
                if (throwStatement.Expression == null)
                {
                    return false;
                }

                if (caughtIdentifier != null &&
                    throwStatement.Expression is IdentifierNameSyntax identifier &&
                    string.Equals(identifier.Identifier.ValueText, caughtIdentifier, StringComparison.Ordinal))
                {
                    return false;
                }
            }

            foreach (ThrowExpressionSyntax throwExpression in catchClause.Block.DescendantNodesAndSelf().OfType<ThrowExpressionSyntax>())
            {
                if (throwExpression.Expression is IdentifierNameSyntax identifier &&
                    caughtIdentifier != null &&
                    string.Equals(identifier.Identifier.ValueText, caughtIdentifier, StringComparison.Ordinal))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Determines whether the catch-clause catches all exceptions.
        /// </summary>
        /// <param name="catchClause">The catch-clause to inspect.</param>
        /// <param name="semanticModel">The semantic model used for type resolution.</param>
        /// <returns>
        /// <see langword="true"/> if the catch-clause catches all exceptions; otherwise <see langword="false"/>.
        /// </returns>
        private static bool IsCatchAll(
            CatchClauseSyntax catchClause,
            SemanticModel semanticModel)
        {
            if (catchClause.Declaration == null)
            {
                return true;
            }

            INamedTypeSymbol? caughtType = GetCaughtExceptionType(catchClause, semanticModel);
            if (caughtType == null)
            {
                return false;
            }

            return IsSystemExceptionType(caughtType);
        }

        /// <summary>
        /// Resolves the caught exception type of a catch-clause.
        /// </summary>
        /// <param name="catchClause">The catch-clause to inspect.</param>
        /// <param name="semanticModel">The semantic model used for type resolution.</param>
        /// <returns>The caught exception type if it can be resolved; otherwise <see langword="null"/>.</returns>
        private static INamedTypeSymbol? GetCaughtExceptionType(
            CatchClauseSyntax catchClause,
            SemanticModel semanticModel)
        {
            if (catchClause.Declaration?.Type == null)
            {
                return null;
            }

            SymbolInfo symbolInfo = semanticModel.GetSymbolInfo(catchClause.Declaration.Type);

            return symbolInfo.Symbol as INamedTypeSymbol;
        }

        /// <summary>
        /// Determines whether the given type is <see cref="System.Exception"/>.
        /// </summary>
        /// <param name="typeSymbol">The type to inspect.</param>
        /// <returns>
        /// <see langword="true"/> if the type is <see cref="System.Exception"/>; otherwise <see langword="false"/>.
        /// </returns>
        private static bool IsSystemExceptionType(INamedTypeSymbol typeSymbol)
        {
            return typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == "global::System.Exception";
        }

        /// <summary>
        /// Merges one exception-flow result into another.
        /// </summary>
        /// <param name="target">The target result.</param>
        /// <param name="source">The source result.</param>
        private static void MergeResults(
            ExceptionFlowAnalysisResult target,
            ExceptionFlowAnalysisResult source)
        {
            foreach (INamedTypeSymbol exceptionType in source.ThrownExceptions)
            {
                target.ThrownExceptions.Add(exceptionType);
            }

            foreach (string uncertainTarget in source.UncertainTargets)
            {
                target.UncertainTargets.Add(uncertainTarget);
            }
        }

        /// <summary>
        /// Returns all nested try-statements below the specified node without descending into
        /// nested try-statements more than once.
        /// </summary>
        /// <param name="node">The node to inspect.</param>
        /// <returns>An enumeration of nested try-statements.</returns>
        private static IEnumerable<TryStatementSyntax> GetNestedTryStatements(SyntaxNode node)
        {
            return node.DescendantNodes(
                    descendIntoChildren: child => child is not TryStatementSyntax)
                .OfType<TryStatementSyntax>();
        }

        /// <summary>
        /// Returns all nodes of the given type below the specified node while excluding
        /// content inside nested try-statements.
        /// </summary>
        /// <typeparam name="TNode">The node type to return.</typeparam>
        /// <param name="node">The root node.</param>
        /// <returns>An enumeration of matching nodes.</returns>
        private static IEnumerable<TNode> GetDescendantsAndSelfExcludingNestedTry<TNode>(SyntaxNode node)
            where TNode : SyntaxNode
        {
            return node.DescendantNodesAndSelf(
                    descendIntoChildren: child => ReferenceEquals(child, node) || child is not TryStatementSyntax)
                .OfType<TNode>();
        }

        /// <summary>
        /// Collects exception types that are thrown directly within the specified node,
        /// excluding nested try-statements.
        /// </summary>
        /// <param name="node">The node to inspect for throw statements and throw expressions.</param>
        /// <param name="semanticModel">The semantic model used for symbol resolution.</param>
        /// <param name="result">The accumulated exception-flow result.</param>
        private static void AnalyzeThrows(
            SyntaxNode node,
            SemanticModel semanticModel,
            ExceptionFlowAnalysisResult result)
        {
            foreach (ThrowStatementSyntax throwStatement in GetDescendantsAndSelfExcludingNestedTry<ThrowStatementSyntax>(node))
            {
                AddThrownExceptionType(result, semanticModel, throwStatement.Expression);
            }

            foreach (ThrowExpressionSyntax throwExpression in GetDescendantsAndSelfExcludingNestedTry<ThrowExpressionSyntax>(node))
            {
                AddThrownExceptionType(result, semanticModel, throwExpression.Expression);
            }
        }

        /// <summary>
        /// Resolves the exception type from a thrown expression and adds it to the result
        /// if it represents an object creation of a named type.
        /// </summary>
        /// <param name="result">The accumulated exception-flow result.</param>
        /// <param name="semanticModel">The semantic model used for symbol resolution.</param>
        /// <param name="expression">The thrown expression to inspect.</param>
        private static void AddThrownExceptionType(
            ExceptionFlowAnalysisResult result,
            SemanticModel semanticModel,
            ExpressionSyntax? expression)
        {
            if (expression is not ObjectCreationExpressionSyntax creation)
            {
                return;
            }

            SymbolInfo symbolInfo = semanticModel.GetSymbolInfo(creation.Type);

            if (symbolInfo.Symbol is INamedTypeSymbol typeSymbol)
            {
                result.ThrownExceptions.Add(typeSymbol);
            }
        }

        /// <summary>
        /// Resolves method invocations within the specified node and recursively
        /// analyzes the bodies of the invoked methods, excluding nested try-statements.
        /// </summary>
        /// <param name="node">The node to inspect for invocations.</param>
        /// <param name="semanticModel">The semantic model used for symbol resolution.</param>
        /// <param name="semanticContext">The project-closure semantic context.</param>
        /// <param name="result">The accumulated exception-flow result.</param>
        /// <param name="visited">The set of already visited callable symbols used to prevent recursive cycles.</param>
        private static void AnalyzeInvocations(
            SyntaxNode node,
            SemanticModel semanticModel,
            ProjectClosureSemanticContext semanticContext,
            ExceptionFlowAnalysisResult result,
            HashSet<ISymbol> visited)
        {
            foreach (InvocationExpressionSyntax invocation in GetDescendantsAndSelfExcludingNestedTry<InvocationExpressionSyntax>(node))
            {
                SymbolInfo symbolInfo = semanticModel.GetSymbolInfo(invocation);

                if (symbolInfo.Symbol is not IMethodSymbol methodSymbol)
                {
                    continue;
                }

                CollectThrownExceptionsFromDelegateFactoryCall(
                    invocation,
                    methodSymbol,
                    semanticContext,
                    result);

                if (!visited.Add(methodSymbol))
                {
                    continue;
                }

                if (!AnalyzeSymbol(methodSymbol, semanticContext, result, visited))
                {
                    MarkUncertain(result, methodSymbol);
                }
            }
        }

        /// <summary>
        /// Collects exception types from invocations where the callee throws the result
        /// of a delegate parameter invocation and the call site supplies a lambda or
        /// anonymous method that directly creates an exception object.
        /// </summary>
        /// <param name="invocation">The invocation to inspect.</param>
        /// <param name="methodSymbol">The resolved target method symbol.</param>
        /// <param name="semanticContext">The project-closure semantic context.</param>
        /// <param name="result">The accumulated exception-flow result.</param>
        private static void CollectThrownExceptionsFromDelegateFactoryCall(
            InvocationExpressionSyntax invocation,
            IMethodSymbol methodSymbol,
            ProjectClosureSemanticContext semanticContext,
            ExceptionFlowAnalysisResult result)
        {
            HashSet<int> throwingDelegateParameterIndexes =
                FindThrowingDelegateParameterIndexes(methodSymbol, semanticContext);

            if (throwingDelegateParameterIndexes.Count == 0)
            {
                return;
            }

            SeparatedSyntaxList<ArgumentSyntax> arguments = invocation.ArgumentList.Arguments;

            for (int i = 0; i < arguments.Count; i++)
            {
                ArgumentSyntax argument = arguments[i];
                int parameterIndex = GetParameterIndexForArgument(argument, i, methodSymbol);

                if (!throwingDelegateParameterIndexes.Contains(parameterIndex))
                {
                    continue;
                }

                ObjectCreationExpressionSyntax? creation =
                    GetExceptionObjectCreation(argument.Expression);

                if (creation == null)
                {
                    continue;
                }

                if (!semanticContext.TryGetSemanticModel(creation.SyntaxTree, out SemanticModel creationSemanticModel) ||
                    creationSemanticModel == null)
                {
                    continue;
                }

                SymbolInfo creationSymbolInfo = creationSemanticModel.GetSymbolInfo(creation.Type);

                if (creationSymbolInfo.Symbol is INamedTypeSymbol typeSymbol)
                {
                    result.ThrownExceptions.Add(typeSymbol);
                }
            }
        }

        /// <summary>
        /// Finds the parameter indexes of delegate-typed parameters whose invocation result
        /// is directly thrown inside the callee body.
        /// </summary>
        /// <param name="methodSymbol">The method symbol to inspect.</param>
        /// <param name="semanticContext">The project-closure semantic context.</param>
        /// <returns>
        /// The indexes of parameters that are treated as exception factory delegates.
        /// </returns>
        private static HashSet<int> FindThrowingDelegateParameterIndexes(
            IMethodSymbol methodSymbol,
            ProjectClosureSemanticContext semanticContext)
        {
            HashSet<int> indexes = new();

            if (methodSymbol.DeclaringSyntaxReferences.Length == 0)
            {
                return indexes;
            }

            foreach (SyntaxReference syntaxRef in methodSymbol.DeclaringSyntaxReferences)
            {
                SyntaxNode node = syntaxRef.GetSyntax();

                if (!semanticContext.TryGetSemanticModel(node.SyntaxTree, out SemanticModel nodeSemanticModel) ||
                    nodeSemanticModel == null)
                {
                    continue;
                }

                BaseMethodDeclarationSyntax? declaration = node as BaseMethodDeclarationSyntax;
                if (declaration == null)
                {
                    continue;
                }

                ParameterListSyntax? parameterList = declaration.ParameterList;
                if (parameterList == null)
                {
                    continue;
                }

                Dictionary<string, int> parameterNameToIndex = new(StringComparer.Ordinal);

                for (int i = 0; i < parameterList.Parameters.Count; i++)
                {
                    ParameterSyntax parameter = parameterList.Parameters[i];
                    parameterNameToIndex[parameter.Identifier.ValueText] = i;
                }

                IEnumerable<ThrowStatementSyntax> throwStatements =
                    declaration.DescendantNodes().OfType<ThrowStatementSyntax>();

                foreach (ThrowStatementSyntax throwStatement in throwStatements)
                {
                    if (throwStatement.Expression is not InvocationExpressionSyntax delegateInvocation)
                    {
                        continue;
                    }

                    if (delegateInvocation.Expression is not IdentifierNameSyntax identifier)
                    {
                        continue;
                    }

                    if (!parameterNameToIndex.TryGetValue(identifier.Identifier.ValueText, out int parameterIndex))
                    {
                        continue;
                    }

                    if (parameterIndex < 0 || parameterIndex >= methodSymbol.Parameters.Length)
                    {
                        continue;
                    }

                    IParameterSymbol parameterSymbol = methodSymbol.Parameters[parameterIndex];

                    if (IsExceptionFactoryDelegate(parameterSymbol.Type))
                    {
                        indexes.Add(parameterIndex);
                    }
                }
            }

            return indexes;
        }

        /// <summary>
        /// Determines whether the specified type is a delegate type that returns
        /// <see cref="System.Exception"/> or a derived exception type.
        /// </summary>
        /// <param name="typeSymbol">The type symbol to inspect.</param>
        /// <returns>
        /// <see langword="true"/> if the type is treated as an exception factory delegate;
        /// otherwise <see langword="false"/>.
        /// </returns>
        private static bool IsExceptionFactoryDelegate(ITypeSymbol typeSymbol)
        {
            if (typeSymbol is not INamedTypeSymbol namedType)
            {
                return false;
            }

            IMethodSymbol? invokeMethod = namedType.DelegateInvokeMethod;
            if (invokeMethod == null)
            {
                return false;
            }

            if (invokeMethod.Parameters.Length != 0)
            {
                return false;
            }

            return IsExceptionTypeByName(invokeMethod.ReturnType);
        }

        /// <summary>
        /// Determines whether the specified type symbol represents
        /// <see cref="System.Exception"/> or a derived type.
        /// </summary>
        /// <param name="typeSymbol">The type symbol to inspect.</param>
        /// <returns>
        /// <see langword="true"/> if the type is an exception type; otherwise <see langword="false"/>.
        /// </returns>
        private static bool IsExceptionTypeByName(ITypeSymbol typeSymbol)
        {
            INamedTypeSymbol? current = typeSymbol as INamedTypeSymbol;

            while (current != null)
            {
                if (current.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == "global::System.Exception")
                {
                    return true;
                }

                current = current.BaseType;
            }

            return false;
        }

        /// <summary>
        /// Gets the effective target parameter index for an argument, taking named
        /// arguments into account.
        /// </summary>
        /// <param name="argument">The argument to inspect.</param>
        /// <param name="fallbackIndex">The positional fallback index.</param>
        /// <param name="methodSymbol">The target method symbol.</param>
        /// <returns>The resolved parameter index, or the fallback index if no named match exists.</returns>
        private static int GetParameterIndexForArgument(
            ArgumentSyntax argument,
            int fallbackIndex,
            IMethodSymbol methodSymbol)
        {
            if (argument.NameColon == null)
            {
                return fallbackIndex;
            }

            string name = argument.NameColon.Name.Identifier.ValueText;

            for (int i = 0; i < methodSymbol.Parameters.Length; i++)
            {
                if (string.Equals(methodSymbol.Parameters[i].Name, name, StringComparison.Ordinal))
                {
                    return i;
                }
            }

            return fallbackIndex;
        }

        /// <summary>
        /// Extracts an exception object creation from a lambda or anonymous method
        /// used as an exception factory argument.
        /// </summary>
        /// <param name="expression">The argument expression to inspect.</param>
        /// <returns>
        /// The extracted exception object creation if found; otherwise <see langword="null"/>.
        /// </returns>
        private static ObjectCreationExpressionSyntax? GetExceptionObjectCreation(
            ExpressionSyntax expression)
        {
            switch (expression)
            {
                case ParenthesizedLambdaExpressionSyntax parenthesizedLambda:
                    return GetExceptionObjectCreationFromLambdaBody(parenthesizedLambda.Body);

                case SimpleLambdaExpressionSyntax simpleLambda:
                    return GetExceptionObjectCreationFromLambdaBody(simpleLambda.Body);

                case AnonymousMethodExpressionSyntax anonymousMethod:
                    if (anonymousMethod.Block != null)
                    {
                        ReturnStatementSyntax? returnStatement =
                            anonymousMethod.Block.Statements.OfType<ReturnStatementSyntax>().FirstOrDefault();

                        if (returnStatement?.Expression is ObjectCreationExpressionSyntax objectCreation)
                        {
                            return objectCreation;
                        }
                    }

                    break;
            }

            return null;
        }

        /// <summary>
        /// Extracts an exception object creation from a lambda body.
        /// </summary>
        /// <param name="body">The lambda body to inspect.</param>
        /// <returns>
        /// The extracted exception object creation if found; otherwise <see langword="null"/>.
        /// </returns>
        private static ObjectCreationExpressionSyntax? GetExceptionObjectCreationFromLambdaBody(
            CSharpSyntaxNode body)
        {
            if (body is ObjectCreationExpressionSyntax directCreation)
            {
                return directCreation;
            }

            if (body is BlockSyntax block)
            {
                ReturnStatementSyntax? returnStatement =
                    block.Statements.OfType<ReturnStatementSyntax>().FirstOrDefault();

                if (returnStatement?.Expression is ObjectCreationExpressionSyntax blockCreation)
                {
                    return blockCreation;
                }
            }

            return null;
        }

        /// <summary>
        /// Resolves constructor calls within the specified node and recursively
        /// analyzes the bodies of the called constructors.
        /// Object creations that are part of a direct throw are ignored here because
        /// they are already handled by direct throw analysis.
        /// </summary>
        /// <param name="node">The node to inspect for object creation expressions.</param>
        /// <param name="semanticModel">The semantic model used for symbol resolution.</param>
        /// <param name="semanticContext">The project-closure semantic context.</param>
        /// <param name="result">The accumulated exception-flow result.</param>
        /// <param name="visited">The set of already visited callable symbols used to prevent recursive cycles.</param>
        private static void AnalyzeObjectCreations(
            SyntaxNode node,
            SemanticModel semanticModel,
            ProjectClosureSemanticContext semanticContext,
            ExceptionFlowAnalysisResult result,
            HashSet<ISymbol> visited)
        {
            foreach (ObjectCreationExpressionSyntax creation in GetDescendantsAndSelfExcludingNestedTry<ObjectCreationExpressionSyntax>(node))
            {
                if (IsPartOfDirectThrow(creation))
                {
                    continue;
                }

                SymbolInfo symbolInfo = semanticModel.GetSymbolInfo(creation);

                if (symbolInfo.Symbol is not IMethodSymbol constructorSymbol)
                {
                    continue;
                }

                if (!visited.Add(constructorSymbol))
                {
                    continue;
                }

                if (!AnalyzeSymbol(constructorSymbol, semanticContext, result, visited))
                {
                    MarkUncertain(result, constructorSymbol);
                }
            }
        }

        /// <summary>
        /// Resolves property and indexer accesses within the specified node and recursively
        /// analyzes the bodies of the accessed members.
        /// </summary>
        /// <param name="node">The node to inspect for property and indexer access.</param>
        /// <param name="semanticModel">The semantic model used for symbol resolution.</param>
        /// <param name="semanticContext">The project-closure semantic context.</param>
        /// <param name="result">The accumulated exception-flow result.</param>
        /// <param name="visited">The set of already visited callable symbols used to prevent recursive cycles.</param>
        private static void AnalyzePropertyAndIndexerAccesses(
            SyntaxNode node,
            SemanticModel semanticModel,
            ProjectClosureSemanticContext semanticContext,
            ExceptionFlowAnalysisResult result,
            HashSet<ISymbol> visited)
        {
            foreach (MemberAccessExpressionSyntax memberAccess in GetDescendantsAndSelfExcludingNestedTry<MemberAccessExpressionSyntax>(node))
            {
                SymbolInfo symbolInfo = semanticModel.GetSymbolInfo(memberAccess);

                if (symbolInfo.Symbol is IPropertySymbol propertySymbol)
                {
                    if (!AnalyzePropertyLikeSymbol(propertySymbol, semanticContext, result, visited))
                    {
                        MarkUncertain(result, propertySymbol);
                    }
                }
            }

            foreach (ElementAccessExpressionSyntax elementAccess in GetDescendantsAndSelfExcludingNestedTry<ElementAccessExpressionSyntax>(node))
            {
                SymbolInfo symbolInfo = semanticModel.GetSymbolInfo(elementAccess);

                if (symbolInfo.Symbol is IPropertySymbol indexerSymbol)
                {
                    if (!AnalyzePropertyLikeSymbol(indexerSymbol, semanticContext, result, visited))
                    {
                        MarkUncertain(result, indexerSymbol);
                    }
                }
            }
        }

        /// <summary>
        /// Determines whether the specified object creation is part of a direct throw statement
        /// or throw expression and is therefore already covered by direct throw analysis.
        /// </summary>
        /// <param name="creation">The object creation to inspect.</param>
        /// <returns>
        /// <see langword="true"/> if the object creation is directly thrown; otherwise <see langword="false"/>.
        /// </returns>
        private static bool IsPartOfDirectThrow(ObjectCreationExpressionSyntax creation)
        {
            return creation.Parent is ThrowStatementSyntax
                || creation.Parent is ThrowExpressionSyntax;
        }

        /// <summary>
        /// Marks the given callable target as uncertain because its exception flow could not be analyzed.
        /// </summary>
        /// <param name="result">The accumulated exception-flow result.</param>
        /// <param name="symbol">The symbol whose flow could not be decided.</param>
        private static void MarkUncertain(
            ExceptionFlowAnalysisResult result,
            ISymbol symbol)
        {
            string display = symbol.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat);

            if (string.IsNullOrWhiteSpace(display))
            {
                display = symbol.Name;
            }

            if (!string.IsNullOrWhiteSpace(display))
            {
                result.UncertainTargets.Add(display);
            }
        }

        /// <summary>
        /// Analyzes a property-like symbol by first trying its getter symbol and
        /// then falling back to the property or indexer declaration itself.
        /// </summary>
        /// <param name="propertySymbol">The property or indexer symbol to analyze.</param>
        /// <param name="semanticContext">The project-closure semantic context.</param>
        /// <param name="result">The accumulated exception-flow result.</param>
        /// <param name="visited">The set of already visited callable symbols used to prevent recursive cycles.</param>
        /// <returns>
        /// <see langword="true"/> if at least one executable body was analyzed for the symbol;
        /// otherwise <see langword="false"/>.
        /// </returns>
        private static bool AnalyzePropertyLikeSymbol(
            IPropertySymbol propertySymbol,
            ProjectClosureSemanticContext semanticContext,
            ExceptionFlowAnalysisResult result,
            HashSet<ISymbol> visited)
        {
            bool analyzedGetter = false;

            if (propertySymbol.GetMethod != null && visited.Add(propertySymbol.GetMethod))
            {
                analyzedGetter = AnalyzeSymbol(propertySymbol.GetMethod, semanticContext, result, visited);
            }

            if (!analyzedGetter && visited.Add(propertySymbol))
            {
                return AnalyzeSymbol(propertySymbol, semanticContext, result, visited);
            }

            return analyzedGetter;
        }

        /// <summary>
        /// Analyzes the syntax declarations of a callable symbol and recursively
        /// processes any executable bodies found there.
        /// </summary>
        /// <param name="symbol">The callable symbol to analyze.</param>
        /// <param name="semanticContext">The project-closure semantic context.</param>
        /// <param name="result">The accumulated exception-flow result.</param>
        /// <param name="visited">The set of already visited callable symbols used to prevent recursive cycles.</param>
        /// <returns>
        /// <see langword="true"/> if at least one executable body was analyzed for the symbol;
        /// otherwise <see langword="false"/>.
        /// </returns>
        private static bool AnalyzeSymbol(
            ISymbol symbol,
            ProjectClosureSemanticContext semanticContext,
            ExceptionFlowAnalysisResult result,
            HashSet<ISymbol> visited)
        {
            bool analyzedAnyBody = false;

            if (symbol.DeclaringSyntaxReferences.Length == 0)
            {
                return false;
            }

            foreach (SyntaxReference syntaxRef in symbol.DeclaringSyntaxReferences)
            {
                SyntaxNode node = syntaxRef.GetSyntax();

                if (!semanticContext.TryGetSemanticModel(node.SyntaxTree, out SemanticModel nodeSemanticModel) ||
                    nodeSemanticModel == null)
                {
                    continue;
                }

                if (node is MethodDeclarationSyntax method)
                {
                    if (SyntaxUtils.TryGetMemberBody(method, out SyntaxNode? body) && body != null)
                    {
                        AnalyzeNode(
                            body,
                            nodeSemanticModel,
                            semanticContext,
                            result,
                            visited,
                            ExceptionFlowTraversalMode.Transitive);
                        analyzedAnyBody = true;
                    }

                    continue;
                }

                if (node is ConstructorDeclarationSyntax constructor)
                {
                    if (SyntaxUtils.TryGetMemberBody(constructor, out SyntaxNode? body) && body != null)
                    {
                        AnalyzeNode(
                            body,
                            nodeSemanticModel,
                            semanticContext,
                            result,
                            visited,
                            ExceptionFlowTraversalMode.Transitive);
                        analyzedAnyBody = true;
                    }

                    continue;
                }

                if (node is PropertyDeclarationSyntax property)
                {
                    if (SyntaxUtils.TryGetMemberBody(property, out SyntaxNode? body) && body != null)
                    {
                        AnalyzeNode(
                            body,
                            nodeSemanticModel,
                            semanticContext,
                            result,
                            visited,
                            ExceptionFlowTraversalMode.Transitive);
                        analyzedAnyBody = true;
                    }

                    AccessorDeclarationSyntax? getter = property.AccessorList?.Accessors
                        .FirstOrDefault(static accessor => accessor.Keyword.IsKind(SyntaxKind.GetKeyword));

                    if (getter != null)
                    {
                        if (!semanticContext.TryGetSemanticModel(getter.SyntaxTree, out SemanticModel getterSemanticModel) ||
                            getterSemanticModel == null)
                        {
                            continue;
                        }

                        if (getter.Body != null)
                        {
                            AnalyzeNode(
                                getter.Body,
                                getterSemanticModel,
                                semanticContext,
                                result,
                                visited,
                                ExceptionFlowTraversalMode.Transitive);
                            analyzedAnyBody = true;
                        }
                        else if (getter.ExpressionBody != null)
                        {
                            AnalyzeNode(
                                getter.ExpressionBody.Expression,
                                getterSemanticModel,
                                semanticContext,
                                result,
                                visited,
                                ExceptionFlowTraversalMode.Transitive);
                            analyzedAnyBody = true;
                        }
                    }

                    continue;
                }

                if (node is IndexerDeclarationSyntax indexer)
                {
                    if (SyntaxUtils.TryGetMemberBody(indexer, out SyntaxNode? body) && body != null)
                    {
                        AnalyzeNode(
                            body,
                            nodeSemanticModel,
                            semanticContext,
                            result,
                            visited,
                            ExceptionFlowTraversalMode.Transitive);
                        analyzedAnyBody = true;
                    }

                    AccessorDeclarationSyntax? getter = indexer.AccessorList?.Accessors
                        .FirstOrDefault(static accessor => accessor.Keyword.IsKind(SyntaxKind.GetKeyword));

                    if (getter != null)
                    {
                        if (!semanticContext.TryGetSemanticModel(getter.SyntaxTree, out SemanticModel getterSemanticModel) ||
                            getterSemanticModel == null)
                        {
                            continue;
                        }

                        if (getter.Body != null)
                        {
                            AnalyzeNode(
                                getter.Body,
                                getterSemanticModel,
                                semanticContext,
                                result,
                                visited,
                                ExceptionFlowTraversalMode.Transitive);
                            analyzedAnyBody = true;
                        }
                        else if (getter.ExpressionBody != null)
                        {
                            AnalyzeNode(
                                getter.ExpressionBody.Expression,
                                getterSemanticModel,
                                semanticContext,
                                result,
                                visited,
                                ExceptionFlowTraversalMode.Transitive);
                            analyzedAnyBody = true;
                        }
                    }

                    continue;
                }

                if (node is AccessorDeclarationSyntax accessor)
                {
                    if (accessor.Body != null)
                    {
                        AnalyzeNode(
                            accessor.Body,
                            nodeSemanticModel,
                            semanticContext,
                            result,
                            visited,
                            ExceptionFlowTraversalMode.Transitive);
                        analyzedAnyBody = true;
                    }
                    else if (accessor.ExpressionBody != null)
                    {
                        AnalyzeNode(
                            accessor.ExpressionBody.Expression,
                            nodeSemanticModel,
                            semanticContext,
                            result,
                            visited,
                            ExceptionFlowTraversalMode.Transitive);
                        analyzedAnyBody = true;
                    }
                }
            }

            return analyzedAnyBody;
        }
    }
}
