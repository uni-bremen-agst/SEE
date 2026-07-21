using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using XMLDocNormalizer.Checks.Infrastructure.Exception;
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
            /// Only explicit throw operations and modeled framework throw helpers
            /// inside the analyzed member are considered.
            /// </summary>
            Direct,

            /// <summary>
            /// Exceptions are analyzed transitively through invoked members and other reachable constructs.
            /// </summary>
            Transitive
        }

        /// <summary>
        /// Analyzes all exception types that may escape directly from the specified member.
        /// Direct exception sources include explicit throw operations and modeled framework
        /// throw helpers. Exceptions that are fully caught and handled within the member
        /// are suppressed.
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

            if (!semanticContext.TryGetSemanticModel(
                    member.SyntaxTree,
                    out SemanticModel semanticModel) ||
                semanticModel == null)
            {
                return result;
            }

            if (!SyntaxUtils.TryGetMemberBody(member, out SyntaxNode? body) ||
                body == null)
            {
                return result;
            }

            ExceptionFlowTraversalState traversalState = new();
            ExceptionFlowCallContext callContext =
                CreateRootCallContext(member, semanticModel);

            AnalyzeNode(
                body,
                semanticModel,
                semanticContext,
                result,
                traversalState,
                ExceptionFlowTraversalMode.Direct,
                callContext);

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

            if (!semanticContext.TryGetSemanticModel(
                    member.SyntaxTree,
                    out SemanticModel semanticModel) ||
                semanticModel == null)
            {
                return result;
            }

            if (!SyntaxUtils.TryGetMemberBody(member, out SyntaxNode? body) ||
                body == null)
            {
                return result;
            }

            ExceptionFlowTraversalState traversalState = new();
            ExceptionFlowCallContext callContext =
                CreateRootCallContext(member, semanticModel);

            AnalyzeNode(
                body,
                semanticModel,
                semanticContext,
                result,
                traversalState,
                ExceptionFlowTraversalMode.Transitive,
                callContext);

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
        /// <param name="traversalState">The traversal state used to prevent recursive analysis cycles.</param>
        /// <param name="mode">The traversal mode.</param>
        /// <param name="callContext">The call-site facts known for the currently analyzed callable.</param>
        private static void AnalyzeNode(
            SyntaxNode node,
            SemanticModel semanticModel,
            ProjectClosureSemanticContext semanticContext,
            ExceptionFlowAnalysisResult result,
            ExceptionFlowTraversalState traversalState,
            ExceptionFlowTraversalMode mode,
            ExceptionFlowCallContext callContext)
        {
            if (node is TryStatementSyntax tryStatement)
            {
                AnalyzeTryStatement(
                    tryStatement,
                    semanticModel,
                    semanticContext,
                    result,
                    traversalState,
                    mode,
                    callContext);
                return;
            }

            AnalyzeSimpleNode(
                node,
                semanticModel,
                semanticContext,
                result,
                traversalState,
                mode,
                callContext);

            foreach (TryStatementSyntax nestedTry in GetNestedTryStatements(node))
            {
                AnalyzeTryStatement(
                    nestedTry,
                    semanticModel,
                    semanticContext,
                    result,
                    traversalState,
                    mode,
                    callContext);
            }
        }

        /// <summary>
        /// Analyzes a syntax node excluding nested try-statements.
        /// </summary>
        /// <param name="node">The node to analyze.</param>
        /// <param name="semanticModel">The semantic model used for symbol resolution.</param>
        /// <param name="semanticContext">The project-closure semantic context.</param>
        /// <param name="result">The accumulated exception-flow result.</param>
        /// <param name="traversalState">The traversal state used to prevent recursive analysis cycles.</param>
        /// <param name="mode">The traversal mode.</param>
        /// <param name="callContext">The call-site facts known for the currently analyzed callable.</param>
        private static void AnalyzeSimpleNode(
            SyntaxNode node,
            SemanticModel semanticModel,
            ProjectClosureSemanticContext semanticContext,
            ExceptionFlowAnalysisResult result,
            ExceptionFlowTraversalState traversalState,
            ExceptionFlowTraversalMode mode,
            ExceptionFlowCallContext callContext)
        {
            AnalyzeThrows(node, semanticModel, result);

            AnalyzeInvocations(
                node,
                semanticModel,
                semanticContext,
                result,
                traversalState,
                mode,
                callContext);

            if (mode == ExceptionFlowTraversalMode.Direct)
            {
                return;
            }

            AnalyzeObjectCreations(
                node,
                semanticModel,
                semanticContext,
                result,
                traversalState,
                callContext);

            AnalyzePropertyAndIndexerAccesses(
                node,
                semanticModel,
                semanticContext,
                result,
                traversalState,
                callContext);
        }

        /// <summary>
        /// Analyzes a try-statement and suppresses exceptions from the try-block that are fully
        /// handled by one of its catch-clauses.
        /// </summary>
        /// <param name="tryStatement">The try-statement to analyze.</param>
        /// <param name="semanticModel">The semantic model used for symbol resolution.</param>
        /// <param name="semanticContext">The project-closure semantic context.</param>
        /// <param name="result">The accumulated exception-flow result.</param>
        /// <param name="traversalState">The traversal state used to prevent recursive analysis cycles.</param>
        /// <param name="mode">The traversal mode.</param>
        /// <param name="callContext">The call-site facts known for the currently analyzed callable.</param>
        private static void AnalyzeTryStatement(
            TryStatementSyntax tryStatement,
            SemanticModel semanticModel,
            ProjectClosureSemanticContext semanticContext,
            ExceptionFlowAnalysisResult result,
            ExceptionFlowTraversalState traversalState,
            ExceptionFlowTraversalMode mode,
            ExceptionFlowCallContext callContext)
        {
            ExceptionFlowAnalysisResult tryResult = new();

            AnalyzeNode(
                tryStatement.Block,
                semanticModel,
                semanticContext,
                tryResult,
                traversalState,
                mode,
                callContext);

            SuppressCaughtExceptionsFromTry(
                tryStatement,
                semanticModel,
                tryResult);

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
                        traversalState,
                        mode,
                        callContext);
                }

                if (catchClause.Block != null)
                {
                    AnalyzeNode(
                        catchClause.Block,
                        semanticModel,
                        semanticContext,
                        result,
                        traversalState,
                        mode,
                        callContext);
                }
            }

            if (tryStatement.Finally != null)
            {
                AnalyzeNode(
                    tryStatement.Finally.Block,
                    semanticModel,
                    semanticContext,
                    result,
                    traversalState,
                    mode,
                    callContext);
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
        /// Creates the initial call context for a top-level member analysis.
        /// </summary>
        /// <param name="member">The member whose body is analyzed.</param>
        /// <param name="semanticModel">The semantic model used to resolve the member symbol.</param>
        /// <returns>A call context without assumed non-null parameter facts.</returns>
        private static ExceptionFlowCallContext CreateRootCallContext(
            MemberDeclarationSyntax member,
            SemanticModel semanticModel)
        {
            ISymbol? memberSymbol = semanticModel.GetDeclaredSymbol(member);

            return new ExceptionFlowCallContext(
                memberSymbol,
                Array.Empty<int>());
        }

        /// <summary>
        /// Determines whether an <see cref="ArgumentNullException"/>
        /// <c>ThrowIfNull</c> invocation is proven not to throw at its current
        /// call site.
        /// </summary>
        /// <param name="invocation">The framework helper invocation.</param>
        /// <param name="methodSymbol">The resolved framework helper symbol.</param>
        /// <param name="semanticModel">The semantic model used for expression analysis.</param>
        /// <param name="callContext">The call-site facts known for the current callable.</param>
        /// <returns>
        /// <see langword="true"/> if the guarded argument is proven to be non-null;
        /// otherwise <see langword="false"/>.
        /// </returns>
        private static bool IsNonThrowingArgumentNullGuard(
            InvocationExpressionSyntax invocation,
            IMethodSymbol methodSymbol,
            SemanticModel semanticModel,
            ExceptionFlowCallContext callContext)
        {
            if (!KnownFrameworkExceptionModel.IsArgumentNullThrowIfNull(
                    methodSymbol,
                    semanticModel.Compilation))
            {
                return false;
            }

            SeparatedSyntaxList<ArgumentSyntax> arguments =
                invocation.ArgumentList.Arguments;

            for (int i = 0; i < arguments.Count; i++)
            {
                ArgumentSyntax argument = arguments[i];
                int parameterIndex =
                    GetParameterIndexForArgument(
                        argument,
                        i,
                        methodSymbol);

                if (parameterIndex != 0)
                {
                    continue;
                }

                return IsDefinitelyNonNull(
                    argument.Expression,
                    semanticModel,
                    callContext);
            }

            return false;
        }

        /// <summary>
        /// Creates the call context for an invoked method or constructor.
        /// </summary>
        /// <param name="methodSymbol">The invoked method or constructor.</param>
        /// <param name="arguments">The arguments supplied at the call site.</param>
        /// <param name="semanticModel">The semantic model used for expression analysis.</param>
        /// <param name="callerContext">The call-site facts known for the caller.</param>
        /// <returns>The call context to use while analyzing the invoked callable.</returns>
        private static ExceptionFlowCallContext CreateCallContext(
            IMethodSymbol methodSymbol,
            SeparatedSyntaxList<ArgumentSyntax> arguments,
            SemanticModel semanticModel,
            ExceptionFlowCallContext callerContext)
        {
            HashSet<int> knownNonNullParameterIndexes = new();
            HashSet<int> suppliedParameterIndexes = new();

            for (int i = 0; i < arguments.Count; i++)
            {
                ArgumentSyntax argument = arguments[i];
                int parameterIndex =
                    GetParameterIndexForArgument(
                        argument,
                        i,
                        methodSymbol);

                if (parameterIndex < 0 ||
                    parameterIndex >= methodSymbol.Parameters.Length)
                {
                    continue;
                }

                suppliedParameterIndexes.Add(parameterIndex);

                if (argument.RefKindKeyword.IsKind(SyntaxKind.OutKeyword))
                {
                    continue;
                }

                if (IsDefinitelyNonNull(
                        argument.Expression,
                        semanticModel,
                        callerContext))
                {
                    knownNonNullParameterIndexes.Add(parameterIndex);
                }
            }

            foreach (IParameterSymbol parameterSymbol in methodSymbol.Parameters)
            {
                if (suppliedParameterIndexes.Contains(parameterSymbol.Ordinal))
                {
                    continue;
                }

                if (parameterSymbol.IsParams ||
                    parameterSymbol.HasExplicitDefaultValue &&
                    parameterSymbol.ExplicitDefaultValue != null)
                {
                    knownNonNullParameterIndexes.Add(parameterSymbol.Ordinal);
                }
            }

            return new ExceptionFlowCallContext(
                methodSymbol,
                knownNonNullParameterIndexes);
        }

        /// <summary>
        /// Determines whether an expression is proven to evaluate to a non-null value without
        /// relying only on nullable reference-type annotations.
        /// </summary>
        /// <param name="expression">The expression to inspect.</param>
        /// <param name="semanticModel">The semantic model used for symbol and constant resolution.</param>
        /// <param name="callContext">The call-site facts known for the current callable.</param>
        /// <returns>
        /// <see langword="true"/> if the expression is proven to be non-null;
        /// otherwise <see langword="false"/>.
        /// </returns>
        private static bool IsDefinitelyNonNull(
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
        /// Determines whether an expression is proven to evaluate to a non-null value while
        /// preventing recursive return-value analysis.
        /// </summary>
        /// <param name="expression">The expression to inspect.</param>
        /// <param name="semanticModel">The semantic model used for symbol and constant resolution.</param>
        /// <param name="callContext">The call-site facts known for the current callable.</param>
        /// <param name="inspectedReturnSymbols">
        /// The method symbols whose return values are currently being inspected.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the expression is proven to be non-null;
        /// otherwise <see langword="false"/>.
        /// </returns>
        private static bool IsDefinitelyNonNull(
            ExpressionSyntax expression,
            SemanticModel semanticModel,
            ExceptionFlowCallContext callContext,
            HashSet<ISymbol> inspectedReturnSymbols)
        {
            Optional<object?> constantValue =
                semanticModel.GetConstantValue(expression);

            if (constantValue.HasValue &&
                constantValue.Value != null)
            {
                return true;
            }

            TypeInfo typeInfo = semanticModel.GetTypeInfo(expression);
            ITypeSymbol? expressionType =
                typeInfo.ConvertedType ?? typeInfo.Type;

            if (expressionType != null &&
                expressionType.IsValueType &&
                !IsNullableValueType(expressionType))
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
                               inspectedReturnSymbols) &&
                           IsDefinitelyNonNull(
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
                return callContext.IsParameterKnownNonNull(parameterSymbol);
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

            return false;
        }

        /// <summary>
        /// Determines whether the specified type is a nullable value type.
        /// </summary>
        /// <param name="typeSymbol">The type symbol to inspect.</param>
        /// <returns>
        /// <see langword="true"/> if the type is a nullable value type;
        /// otherwise <see langword="false"/>.
        /// </returns>
        private static bool IsNullableValueType(ITypeSymbol typeSymbol)
        {
            return typeSymbol is INamedTypeSymbol namedType &&
                   namedType.OriginalDefinition.SpecialType ==
                   SpecialType.System_Nullable_T;
        }

        /// <summary>
        /// Determines whether a local variable is guaranteed to contain a non-null value
        /// because it was introduced by a non-null pattern, initialized with a value
        /// proven to be non-null, or protected by an earlier terminating null guard.
        /// </summary>
        /// <param name="expression">The local-variable expression being evaluated.</param>
        /// <param name="localSymbol">The local symbol to inspect.</param>
        /// <param name="semanticModel">The semantic model used for expression analysis.</param>
        /// <param name="callContext">The call-site facts known for the current callable.</param>
        /// <param name="inspectedReturnSymbols">
        /// The method symbols whose return values are currently being inspected.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the local variable is proven to be non-null;
        /// otherwise <see langword="false"/>.
        /// </returns>
        private static bool IsLocalGuaranteedNonNull(
            ExpressionSyntax expression,
            ILocalSymbol localSymbol,
            SemanticModel semanticModel,
            ExceptionFlowCallContext callContext,
            HashSet<ISymbol> inspectedReturnSymbols)
        {
            if (IsLocalProvenNonNullByPrecedingGuard(
                    expression,
                    localSymbol,
                    semanticModel))
            {
                return true;
            }

            foreach (SyntaxReference syntaxReference
                     in localSymbol.DeclaringSyntaxReferences)
            {
                SyntaxNode declarationNode = syntaxReference.GetSyntax();

                if (declarationNode is SingleVariableDesignationSyntax)
                {
                    PatternSyntax? declaringPattern = declarationNode.Ancestors()
                        .OfType<PatternSyntax>()
                        .FirstOrDefault();

                    if (declaringPattern is DeclarationPatternSyntax or
                        RecursivePatternSyntax or
                        ListPatternSyntax)
                    {
                        return true;
                    }

                    continue;
                }

                if (declarationNode is not VariableDeclaratorSyntax variableDeclarator ||
                    variableDeclarator.Initializer == null)
                {
                    continue;
                }

                SemanticModel? declarationSemanticModel =
                    GetSemanticModelForSyntaxTree(
                        semanticModel,
                        variableDeclarator.SyntaxTree);

                if (declarationSemanticModel == null)
                {
                    continue;
                }

                if (IsDefinitelyNonNull(
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
        /// Determines whether reaching the specified expression proves that a local variable
        /// is non-null because an earlier null guard terminates the current control-flow path.
        /// </summary>
        /// <param name="expression">The local-variable expression being evaluated.</param>
        /// <param name="localSymbol">The local symbol to inspect.</param>
        /// <param name="semanticModel">The semantic model used for symbol resolution.</param>
        /// <returns>
        /// <see langword="true"/> if an earlier terminating guard proves the local variable
        /// to be non-null; otherwise <see langword="false"/>.
        /// </returns>
        private static bool IsLocalProvenNonNullByPrecedingGuard(
            ExpressionSyntax expression,
            ILocalSymbol localSymbol,
            SemanticModel semanticModel)
        {
            StatementSyntax? currentStatement = expression
                .AncestorsAndSelf()
                .OfType<StatementSyntax>()
                .FirstOrDefault();

            if (currentStatement == null ||
                currentStatement.Parent is not BlockSyntax containingBlock)
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
                if (containingBlock.Statements[index] is not IfStatementSyntax ifStatement)
                {
                    continue;
                }

                if (!StatementAlwaysTerminatesCurrentPath(ifStatement.Statement))
                {
                    continue;
                }

                if (ConditionBeingFalseProvesLocalNonNull(
                        ifStatement.Condition,
                        localSymbol,
                        semanticModel))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Determines whether a condition evaluating to <see langword="false"/> proves
        /// that the specified local variable is non-null.
        /// </summary>
        /// <param name="condition">The condition to inspect.</param>
        /// <param name="localSymbol">The local symbol whose null state is evaluated.</param>
        /// <param name="semanticModel">The semantic model used for symbol resolution.</param>
        /// <returns>
        /// <see langword="true"/> if the false condition result proves the local variable
        /// to be non-null; otherwise <see langword="false"/>.
        /// </returns>
        private static bool ConditionBeingFalseProvesLocalNonNull(
            ExpressionSyntax condition,
            ILocalSymbol localSymbol,
            SemanticModel semanticModel)
        {
            ExpressionSyntax unwrappedCondition =
                UnwrapParenthesizedExpression(condition);

            if (unwrappedCondition is BinaryExpressionSyntax logicalOr &&
                logicalOr.IsKind(SyntaxKind.LogicalOrExpression))
            {
                return ConditionBeingFalseProvesLocalNonNull(
                           logicalOr.Left,
                           localSymbol,
                           semanticModel) ||
                       ConditionBeingFalseProvesLocalNonNull(
                           logicalOr.Right,
                           localSymbol,
                           semanticModel);
            }

            return IsLocalComparedEqualToNull(
                       unwrappedCondition,
                       localSymbol,
                       semanticModel) ||
                   IsLocalMatchedAgainstNullPattern(
                       unwrappedCondition,
                       localSymbol,
                       semanticModel);
        }

        /// <summary>
        /// Determines whether an expression compares the specified local variable
        /// to <see langword="null"/> using the equality operator.
        /// </summary>
        /// <param name="expression">The expression to inspect.</param>
        /// <param name="localSymbol">The expected local symbol.</param>
        /// <param name="semanticModel">The semantic model used for symbol resolution.</param>
        /// <returns>
        /// <see langword="true"/> if the expression is an equality comparison between
        /// the local variable and <see langword="null"/>; otherwise
        /// <see langword="false"/>.
        /// </returns>
        private static bool IsLocalComparedEqualToNull(
            ExpressionSyntax expression,
            ILocalSymbol localSymbol,
            SemanticModel semanticModel)
        {
            if (expression is not BinaryExpressionSyntax comparison ||
                !comparison.IsKind(SyntaxKind.EqualsExpression))
            {
                return false;
            }

            if (comparison.Left.IsKind(SyntaxKind.NullLiteralExpression))
            {
                return ExpressionReferencesLocal(
                    comparison.Right,
                    localSymbol,
                    semanticModel);
            }

            if (comparison.Right.IsKind(SyntaxKind.NullLiteralExpression))
            {
                return ExpressionReferencesLocal(
                    comparison.Left,
                    localSymbol,
                    semanticModel);
            }

            return false;
        }

        /// <summary>
        /// Determines whether an expression matches the specified local variable against
        /// the constant <see langword="null"/> pattern.
        /// </summary>
        /// <param name="expression">The expression to inspect.</param>
        /// <param name="localSymbol">The expected local symbol.</param>
        /// <param name="semanticModel">The semantic model used for symbol resolution.</param>
        /// <returns>
        /// <see langword="true"/> if the expression has the form
        /// <c>local is null</c>; otherwise <see langword="false"/>.
        /// </returns>
        private static bool IsLocalMatchedAgainstNullPattern(
            ExpressionSyntax expression,
            ILocalSymbol localSymbol,
            SemanticModel semanticModel)
        {
            if (expression is not IsPatternExpressionSyntax isPatternExpression ||
                isPatternExpression.Pattern is not ConstantPatternSyntax constantPattern ||
                !constantPattern.Expression.IsKind(SyntaxKind.NullLiteralExpression))
            {
                return false;
            }

            return ExpressionReferencesLocal(
                isPatternExpression.Expression,
                localSymbol,
                semanticModel);
        }

        /// <summary>
        /// Determines whether an expression resolves to the specified local symbol.
        /// </summary>
        /// <param name="expression">The expression to resolve.</param>
        /// <param name="localSymbol">The expected local symbol.</param>
        /// <param name="semanticModel">The semantic model used for symbol resolution.</param>
        /// <returns>
        /// <see langword="true"/> if the expression references the specified local;
        /// otherwise <see langword="false"/>.
        /// </returns>
        private static bool ExpressionReferencesLocal(
            ExpressionSyntax expression,
            ILocalSymbol localSymbol,
            SemanticModel semanticModel)
        {
            ExpressionSyntax unwrappedExpression =
                UnwrapParenthesizedExpression(expression);

            SymbolInfo symbolInfo =
                semanticModel.GetSymbolInfo(unwrappedExpression);

            return symbolInfo.Symbol is ILocalSymbol referencedLocal &&
                   SymbolEqualityComparer.Default.Equals(
                       referencedLocal,
                       localSymbol);
        }

        /// <summary>
        /// Removes surrounding parenthesized expressions.
        /// </summary>
        /// <param name="expression">The expression to unwrap.</param>
        /// <returns>The innermost non-parenthesized expression.</returns>
        private static ExpressionSyntax UnwrapParenthesizedExpression(
            ExpressionSyntax expression)
        {
            ExpressionSyntax currentExpression = expression;

            while (currentExpression is ParenthesizedExpressionSyntax parenthesized)
            {
                currentExpression = parenthesized.Expression;
            }

            return currentExpression;
        }

        /// <summary>
        /// Determines whether a statement always terminates the current control-flow path.
        /// </summary>
        /// <param name="statement">The statement to inspect.</param>
        /// <returns>
        /// <see langword="true"/> if execution cannot continue with the next statement
        /// in the containing block; otherwise <see langword="false"/>.
        /// </returns>
        private static bool StatementAlwaysTerminatesCurrentPath(
            StatementSyntax statement)
        {
            if (statement is ReturnStatementSyntax or
                ThrowStatementSyntax or
                ContinueStatementSyntax or
                BreakStatementSyntax or
                GotoStatementSyntax)
            {
                return true;
            }

            if (statement is BlockSyntax block)
            {
                if (block.Statements.Count == 0)
                {
                    return false;
                }

                return StatementAlwaysTerminatesCurrentPath(
                    block.Statements[block.Statements.Count - 1]);
            }

            return false;
        }

        /// <summary>
        /// Gets a semantic model for a syntax tree if the tree belongs to the same
        /// compilation as the supplied semantic model.
        /// </summary>
        /// <param name="semanticModel">The currently available semantic model.</param>
        /// <param name="syntaxTree">The syntax tree whose semantic model is required.</param>
        /// <returns>
        /// The semantic model for <paramref name="syntaxTree"/>, or
        /// <see langword="null"/> if the tree does not belong to the compilation.
        /// </returns>
        private static SemanticModel? GetSemanticModelForSyntaxTree(
            SemanticModel semanticModel,
            SyntaxTree syntaxTree)
        {
            if (semanticModel.SyntaxTree == syntaxTree)
            {
                return semanticModel;
            }

            if (!semanticModel.Compilation.SyntaxTrees.Contains(syntaxTree))
            {
                return null;
            }

            return semanticModel.Compilation.GetSemanticModel(syntaxTree);
        }

        /// <summary>
        /// Determines whether an invocation is guaranteed to return a non-null value.
        /// </summary>
        /// <param name="invocation">The invocation expression to inspect.</param>
        /// <param name="semanticModel">The semantic model used for symbol resolution.</param>
        /// <param name="callContext">The call-site facts known for the current callable.</param>
        /// <param name="inspectedReturnSymbols">
        /// The method symbols whose return values are currently being inspected.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if every source-level return value of the invoked
        /// method is proven to be non-null; otherwise <see langword="false"/>.
        /// </returns>
        private static bool IsInvocationResultDefinitelyNonNull(
            InvocationExpressionSyntax invocation,
            SemanticModel semanticModel,
            ExceptionFlowCallContext callContext,
            HashSet<ISymbol> inspectedReturnSymbols)
        {
            SymbolInfo symbolInfo = semanticModel.GetSymbolInfo(invocation);

            if (symbolInfo.Symbol is not IMethodSymbol methodSymbol)
            {
                return false;
            }

            if (IsKnownNonNullFrameworkFactory(methodSymbol))
            {
                return true;
            }

            IMethodSymbol originalMethod = methodSymbol.OriginalDefinition;

            if (originalMethod.IsAsync ||
                originalMethod.ReturnsByRef ||
                originalMethod.ReturnsByRefReadonly ||
                originalMethod.DeclaringSyntaxReferences.Length == 0)
            {
                return false;
            }

            if (!inspectedReturnSymbols.Add(originalMethod))
            {
                return false;
            }

            bool foundExecutableDeclaration = false;

            try
            {
                foreach (SyntaxReference syntaxReference
                         in originalMethod.DeclaringSyntaxReferences)
                {
                    SyntaxNode declarationNode = syntaxReference.GetSyntax();

                    SemanticModel? declarationSemanticModel =
                        GetSemanticModelForSyntaxTree(
                            semanticModel,
                            declarationNode.SyntaxTree);

                    if (declarationSemanticModel == null)
                    {
                        return false;
                    }

                    if (declarationNode is MethodDeclarationSyntax methodDeclaration)
                    {
                        if (methodDeclaration.ExpressionBody != null)
                        {
                            foundExecutableDeclaration = true;

                            if (!IsDefinitelyNonNull(
                                    methodDeclaration.ExpressionBody.Expression,
                                    declarationSemanticModel,
                                    callContext,
                                    inspectedReturnSymbols))
                            {
                                return false;
                            }

                            continue;
                        }

                        if (methodDeclaration.Body != null)
                        {
                            foundExecutableDeclaration = true;

                            if (!AreAllReturnValuesDefinitelyNonNull(
                                    methodDeclaration.Body,
                                    declarationSemanticModel,
                                    callContext,
                                    inspectedReturnSymbols))
                            {
                                return false;
                            }
                        }

                        continue;
                    }

                    if (declarationNode is LocalFunctionStatementSyntax localFunction)
                    {
                        if (localFunction.ExpressionBody != null)
                        {
                            foundExecutableDeclaration = true;

                            if (!IsDefinitelyNonNull(
                                    localFunction.ExpressionBody.Expression,
                                    declarationSemanticModel,
                                    callContext,
                                    inspectedReturnSymbols))
                            {
                                return false;
                            }

                            continue;
                        }

                        if (localFunction.Body != null)
                        {
                            foundExecutableDeclaration = true;

                            if (!AreAllReturnValuesDefinitelyNonNull(
                                    localFunction.Body,
                                    declarationSemanticModel,
                                    callContext,
                                    inspectedReturnSymbols))
                            {
                                return false;
                            }
                        }
                    }
                }

                return foundExecutableDeclaration;
            }
            finally
            {
                inspectedReturnSymbols.Remove(originalMethod);
            }
        }

        /// <summary>
        /// Determines whether all return statements in a block return values proven
        /// to be non-null.
        /// </summary>
        /// <param name="body">The method or local-function body to inspect.</param>
        /// <param name="semanticModel">The semantic model used for expression analysis.</param>
        /// <param name="callContext">The call-site facts known for the current callable.</param>
        /// <param name="inspectedReturnSymbols">
        /// The method symbols whose return values are currently being inspected.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the block contains at least one return statement
        /// and every returned expression is proven to be non-null; otherwise
        /// <see langword="false"/>.
        /// </returns>
        private static bool AreAllReturnValuesDefinitelyNonNull(
            BlockSyntax body,
            SemanticModel semanticModel,
            ExceptionFlowCallContext callContext,
            HashSet<ISymbol> inspectedReturnSymbols)
        {
            List<ReturnStatementSyntax> returnStatements = body
                .DescendantNodes(
                    static node =>
                        node is not AnonymousFunctionExpressionSyntax &&
                        node is not LocalFunctionStatementSyntax)
                .OfType<ReturnStatementSyntax>()
                .ToList();

            if (returnStatements.Count == 0)
            {
                return false;
            }

            foreach (ReturnStatementSyntax returnStatement in returnStatements)
            {
                if (returnStatement.Expression == null ||
                    !IsDefinitelyNonNull(
                        returnStatement.Expression,
                        semanticModel,
                        callContext,
                        inspectedReturnSymbols))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Determines whether a framework method is known to return a non-null value.
        /// </summary>
        /// <param name="methodSymbol">The resolved invoked method.</param>
        /// <returns>
        /// <see langword="true"/> if the method is a supported non-null framework
        /// factory; otherwise <see langword="false"/>.
        /// </returns>
        private static bool IsKnownNonNullFrameworkFactory(
            IMethodSymbol methodSymbol)
        {
            IMethodSymbol originalMethod = methodSymbol.OriginalDefinition;

            return originalMethod.IsStatic &&
                   originalMethod.Name == "Empty" &&
                   originalMethod.Arity == 1 &&
                   originalMethod.Parameters.Length == 0 &&
                   originalMethod.ContainingType.SpecialType ==
                   SpecialType.System_Array;
        }

        /// <summary>
        /// Resolves method invocations within the specified node, recognizes known
        /// framework exception sources, and optionally analyzes invoked method bodies
        /// transitively.
        /// </summary>
        /// <param name="node">The node to inspect for invocations.</param>
        /// <param name="semanticModel">The semantic model used for symbol resolution.</param>
        /// <param name="semanticContext">The project-closure semantic context.</param>
        /// <param name="result">The accumulated exception-flow result.</param>
        /// <param name="traversalState">The traversal state used to prevent recursive analysis cycles.</param>
        /// <param name="mode">The traversal mode.</param>
        /// <param name="callContext">The call-site facts known for the currently analyzed callable.</param>
        private static void AnalyzeInvocations(
            SyntaxNode node,
            SemanticModel semanticModel,
            ProjectClosureSemanticContext semanticContext,
            ExceptionFlowAnalysisResult result,
            ExceptionFlowTraversalState traversalState,
            ExceptionFlowTraversalMode mode,
            ExceptionFlowCallContext callContext)
        {
            foreach (InvocationExpressionSyntax invocation
                     in GetDescendantsAndSelfExcludingNestedTry<InvocationExpressionSyntax>(node))
            {
                SymbolInfo symbolInfo = semanticModel.GetSymbolInfo(invocation);

                if (symbolInfo.Symbol is not IMethodSymbol methodSymbol)
                {
                    continue;
                }

                if (IsNonThrowingArgumentNullGuard(
                        invocation,
                        methodSymbol,
                        semanticModel,
                        callContext))
                {
                    continue;
                }

                if (KnownFrameworkExceptionModel.TryAddThrownExceptionTypes(
                        methodSymbol,
                        semanticModel.Compilation,
                        result.ThrownExceptions))
                {
                    continue;
                }

                if (mode == ExceptionFlowTraversalMode.Direct)
                {
                    continue;
                }

                CollectThrownExceptionsFromDelegateFactoryCall(
                    invocation,
                    methodSymbol,
                    semanticContext,
                    result);

                ExceptionFlowCallContext calleeContext =
                    CreateCallContext(
                        methodSymbol,
                        invocation.ArgumentList.Arguments,
                        semanticModel,
                        callContext);

                if (!traversalState.TryMarkAnalyzed(
                        methodSymbol,
                        calleeContext))
                {
                    continue;
                }

                if (!AnalyzeSymbol(
                        methodSymbol,
                        semanticContext,
                        result,
                        traversalState,
                        calleeContext))
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
        /// <param name="traversalState">The traversal state used to prevent recursive analysis cycles.</param>
        /// <param name="callContext">The call-site facts known for the currently analyzed callable.</param>
        private static void AnalyzeObjectCreations(
            SyntaxNode node,
            SemanticModel semanticModel,
            ProjectClosureSemanticContext semanticContext,
            ExceptionFlowAnalysisResult result,
            ExceptionFlowTraversalState traversalState,
            ExceptionFlowCallContext callContext)
        {
            foreach (ObjectCreationExpressionSyntax creation
                     in GetDescendantsAndSelfExcludingNestedTry<ObjectCreationExpressionSyntax>(node))
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

                SeparatedSyntaxList<ArgumentSyntax> arguments =
                    creation.ArgumentList?.Arguments ?? default;

                ExceptionFlowCallContext constructorContext =
                    CreateCallContext(
                        constructorSymbol,
                        arguments,
                        semanticModel,
                        callContext);

                if (!traversalState.TryMarkAnalyzed(
                        constructorSymbol,
                        constructorContext))
                {
                    continue;
                }

                if (!AnalyzeSymbol(
                        constructorSymbol,
                        semanticContext,
                        result,
                        traversalState,
                        constructorContext))
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
        /// <param name="traversalState">The traversal state used to prevent recursive analysis cycles.</param>
        /// <param name="callContext">The call-site facts known for the currently analyzed callable.</param>
        private static void AnalyzePropertyAndIndexerAccesses(
            SyntaxNode node,
            SemanticModel semanticModel,
            ProjectClosureSemanticContext semanticContext,
            ExceptionFlowAnalysisResult result,
            ExceptionFlowTraversalState traversalState,
            ExceptionFlowCallContext callContext)
        {
            foreach (MemberAccessExpressionSyntax memberAccess
                     in GetDescendantsAndSelfExcludingNestedTry<MemberAccessExpressionSyntax>(node))
            {
                SymbolInfo symbolInfo = semanticModel.GetSymbolInfo(memberAccess);

                if (symbolInfo.Symbol is not IPropertySymbol propertySymbol)
                {
                    continue;
                }

                ISymbol propertyCallable;

                if (propertySymbol.GetMethod != null)
                {
                    propertyCallable = propertySymbol.GetMethod;
                }
                else
                {
                    propertyCallable = propertySymbol;
                }

                ExceptionFlowCallContext propertyContext =
                    new ExceptionFlowCallContext(
                        propertyCallable,
                        Array.Empty<int>());

                if (!AnalyzePropertyLikeSymbol(
                        propertySymbol,
                        semanticContext,
                        result,
                        traversalState,
                        propertyContext))
                {
                    MarkUncertain(result, propertySymbol);
                }
            }

            foreach (ElementAccessExpressionSyntax elementAccess
                     in GetDescendantsAndSelfExcludingNestedTry<ElementAccessExpressionSyntax>(node))
            {
                SymbolInfo symbolInfo = semanticModel.GetSymbolInfo(elementAccess);

                if (symbolInfo.Symbol is not IPropertySymbol indexerSymbol)
                {
                    continue;
                }

                ExceptionFlowCallContext indexerContext;

                if (indexerSymbol.GetMethod != null)
                {
                    indexerContext = CreateCallContext(
                        indexerSymbol.GetMethod,
                        elementAccess.ArgumentList.Arguments,
                        semanticModel,
                        callContext);
                }
                else
                {
                    indexerContext = new ExceptionFlowCallContext(
                        indexerSymbol,
                        Array.Empty<int>());
                }

                if (!AnalyzePropertyLikeSymbol(
                        indexerSymbol,
                        semanticContext,
                        result,
                        traversalState,
                        indexerContext))
                {
                    MarkUncertain(result, indexerSymbol);
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
        /// <param name="traversalState">The traversal state used to prevent recursive analysis cycles.</param>
        /// <param name="callContext">The call-site facts known for the property getter.</param>
        /// <returns>
        /// <see langword="true"/> if at least one executable body was analyzed for the symbol;
        /// otherwise <see langword="false"/>.
        /// </returns>
        private static bool AnalyzePropertyLikeSymbol(
            IPropertySymbol propertySymbol,
            ProjectClosureSemanticContext semanticContext,
            ExceptionFlowAnalysisResult result,
            ExceptionFlowTraversalState traversalState,
            ExceptionFlowCallContext callContext)
        {
            bool analyzedGetter = false;

            if (propertySymbol.GetMethod != null)
            {
                if (!traversalState.TryMarkAnalyzed(
                        propertySymbol.GetMethod,
                        callContext))
                {
                    return true;
                }

                analyzedGetter = AnalyzeSymbol(
                    propertySymbol.GetMethod,
                    semanticContext,
                    result,
                    traversalState,
                    callContext);
            }

            if (analyzedGetter)
            {
                return true;
            }

            ExceptionFlowCallContext propertyContext =
                new ExceptionFlowCallContext(
                    propertySymbol,
                    Array.Empty<int>());

            if (!traversalState.TryMarkAnalyzed(
                    propertySymbol,
                    propertyContext))
            {
                return true;
            }

            return AnalyzeSymbol(
                propertySymbol,
                semanticContext,
                result,
                traversalState,
                propertyContext);
        }

        /// <summary>
        /// Analyzes the syntax declarations of a callable symbol and recursively
        /// processes any executable bodies found there.
        /// </summary>
        /// <param name="symbol">The callable symbol to analyze.</param>
        /// <param name="semanticContext">The project-closure semantic context.</param>
        /// <param name="result">The accumulated exception-flow result.</param>
        /// <param name="traversalState">The traversal state used to prevent recursive analysis cycles.</param>
        /// <param name="callContext">The call-site facts known for the callable.</param>
        /// <returns>
        /// <see langword="true"/> if at least one executable body was analyzed for the symbol;
        /// otherwise <see langword="false"/>.
        /// </returns>
        private static bool AnalyzeSymbol(
            ISymbol symbol,
            ProjectClosureSemanticContext semanticContext,
            ExceptionFlowAnalysisResult result,
            ExceptionFlowTraversalState traversalState,
            ExceptionFlowCallContext callContext)
        {
            bool analyzedAnyBody = false;

            if (symbol.DeclaringSyntaxReferences.Length == 0)
            {
                return false;
            }

            foreach (SyntaxReference syntaxReference
                     in symbol.DeclaringSyntaxReferences)
            {
                SyntaxNode node = syntaxReference.GetSyntax();

                if (!semanticContext.TryGetSemanticModel(
                        node.SyntaxTree,
                        out SemanticModel nodeSemanticModel) ||
                    nodeSemanticModel == null)
                {
                    continue;
                }

                if (node is MethodDeclarationSyntax method)
                {
                    if (SyntaxUtils.TryGetMemberBody(
                            method,
                            out SyntaxNode? body) &&
                        body != null)
                    {
                        AnalyzeNode(
                            body,
                            nodeSemanticModel,
                            semanticContext,
                            result,
                            traversalState,
                            ExceptionFlowTraversalMode.Transitive,
                            callContext);

                        analyzedAnyBody = true;
                    }

                    continue;
                }

                if (node is ConstructorDeclarationSyntax constructor)
                {
                    if (SyntaxUtils.TryGetMemberBody(
                            constructor,
                            out SyntaxNode? body) &&
                        body != null)
                    {
                        AnalyzeNode(
                            body,
                            nodeSemanticModel,
                            semanticContext,
                            result,
                            traversalState,
                            ExceptionFlowTraversalMode.Transitive,
                            callContext);

                        analyzedAnyBody = true;
                    }

                    continue;
                }

                if (node is PropertyDeclarationSyntax property)
                {
                    if (SyntaxUtils.TryGetMemberBody(
                            property,
                            out SyntaxNode? body) &&
                        body != null)
                    {
                        AnalyzeNode(
                            body,
                            nodeSemanticModel,
                            semanticContext,
                            result,
                            traversalState,
                            ExceptionFlowTraversalMode.Transitive,
                            callContext);

                        analyzedAnyBody = true;
                    }

                    AccessorDeclarationSyntax? getter =
                        property.AccessorList?.Accessors
                            .FirstOrDefault(static accessor =>
                                accessor.Keyword.IsKind(SyntaxKind.GetKeyword));

                    if (getter != null)
                    {
                        if (!semanticContext.TryGetSemanticModel(
                                getter.SyntaxTree,
                                out SemanticModel getterSemanticModel) ||
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
                                traversalState,
                                ExceptionFlowTraversalMode.Transitive,
                                callContext);

                            analyzedAnyBody = true;
                        }
                        else if (getter.ExpressionBody != null)
                        {
                            AnalyzeNode(
                                getter.ExpressionBody.Expression,
                                getterSemanticModel,
                                semanticContext,
                                result,
                                traversalState,
                                ExceptionFlowTraversalMode.Transitive,
                                callContext);

                            analyzedAnyBody = true;
                        }
                    }

                    continue;
                }

                if (node is IndexerDeclarationSyntax indexer)
                {
                    if (SyntaxUtils.TryGetMemberBody(
                            indexer,
                            out SyntaxNode? body) &&
                        body != null)
                    {
                        AnalyzeNode(
                            body,
                            nodeSemanticModel,
                            semanticContext,
                            result,
                            traversalState,
                            ExceptionFlowTraversalMode.Transitive,
                            callContext);

                        analyzedAnyBody = true;
                    }

                    AccessorDeclarationSyntax? getter =
                        indexer.AccessorList?.Accessors
                            .FirstOrDefault(static accessor =>
                                accessor.Keyword.IsKind(SyntaxKind.GetKeyword));

                    if (getter != null)
                    {
                        if (!semanticContext.TryGetSemanticModel(
                                getter.SyntaxTree,
                                out SemanticModel getterSemanticModel) ||
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
                                traversalState,
                                ExceptionFlowTraversalMode.Transitive,
                                callContext);

                            analyzedAnyBody = true;
                        }
                        else if (getter.ExpressionBody != null)
                        {
                            AnalyzeNode(
                                getter.ExpressionBody.Expression,
                                getterSemanticModel,
                                semanticContext,
                                result,
                                traversalState,
                                ExceptionFlowTraversalMode.Transitive,
                                callContext);

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
                            traversalState,
                            ExceptionFlowTraversalMode.Transitive,
                            callContext);

                        analyzedAnyBody = true;
                    }
                    else if (accessor.ExpressionBody != null)
                    {
                        AnalyzeNode(
                            accessor.ExpressionBody.Expression,
                            nodeSemanticModel,
                            semanticContext,
                            result,
                            traversalState,
                            ExceptionFlowTraversalMode.Transitive,
                            callContext);

                        analyzedAnyBody = true;
                    }
                }
            }

            return analyzedAnyBody;
        }
    }
}
