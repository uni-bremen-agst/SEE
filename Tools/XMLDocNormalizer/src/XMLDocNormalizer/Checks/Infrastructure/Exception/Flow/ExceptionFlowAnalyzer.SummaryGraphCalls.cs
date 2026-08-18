using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using XMLDocNormalizer.Checks.Infrastructure.Exception;
using XMLDocNormalizer.Execution.Semantic;
using XMLDocNormalizer.Models;

namespace XMLDocNormalizer.Checks.Infrastructure.Exception.Flow
{
    /// <summary>
    /// Contains collection of callable edges and modeled invocation sources
    /// for summary graphs.
    /// </summary>
    internal static partial class ExceptionFlowAnalyzer
    {
        /// <summary>
        /// Collects method-call edges, delegate-call edges, and locally
        /// modeled invocation sources.
        /// </summary>
        /// <param name="node">The node to inspect.</param>
        /// <param name="semanticModel">
        /// The semantic model used for symbol resolution.
        /// </param>
        /// <param name="semanticContext">
        /// The project-closure semantic context.
        /// </param>
        /// <param name="graph">
        /// The graph receiving discovered callable nodes.
        /// </param>
        /// <param name="fragment">
        /// The local summary fragment.
        /// </param>
        /// <param name="callContext">
        /// The value facts known for the current callable.
        /// </param>
        private static void AnalyzeSummaryInvocations(
            SyntaxNode node,
            SemanticModel semanticModel,
            ProjectClosureSemanticContext semanticContext,
            ExceptionFlowSummaryGraph graph,
            ExceptionFlowSummaryFragment fragment,
            ExceptionFlowCallContext callContext)
        {
            foreach (InvocationExpressionSyntax invocation
                     in GetSummaryDescendantsAndSelf
                         <InvocationExpressionSyntax>(node))
            {
                SymbolInfo symbolInfo =
                    semanticModel.GetSymbolInfo(invocation);

                if (symbolInfo.Symbol
                    is not IMethodSymbol methodSymbol)
                {
                    continue;
                }

                if (methodSymbol.MethodKind ==
                    MethodKind.DelegateInvoke)
                {
                    AnalyzeSummaryDelegateInvocation(
                        invocation,
                        semanticModel,
                        graph,
                        fragment,
                        callContext);

                    continue;
                }

                if (TryAddKnownFrameworkSummarySources(
                        invocation,
                        methodSymbol,
                        semanticModel,
                        fragment,
                        callContext))
                {
                    continue;
                }

                CollectSummaryDelegateFactorySources(
                    invocation,
                    methodSymbol,
                    semanticContext,
                    fragment);

                AddSummaryInvocationEdges(
                    invocation,
                    methodSymbol,
                    semanticModel,
                    semanticContext,
                    graph,
                    fragment,
                    callContext);
            }
        }

        /// <summary>
        /// Resolves and records an invocation through a delegate.
        /// </summary>
        /// <param name="invocation">
        /// The delegate invocation.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used to resolve the concrete delegate target.
        /// </param>
        /// <param name="graph">
        /// The graph receiving the resolved target node.
        /// </param>
        /// <param name="fragment">
        /// The local summary fragment receiving the call edge or uncertainty.
        /// </param>
        /// <param name="callContext">
        /// The value facts known while analyzing the caller.
        /// </param>
        private static void AnalyzeSummaryDelegateInvocation(
            InvocationExpressionSyntax invocation,
            SemanticModel semanticModel,
            ExceptionFlowSummaryGraph graph,
            ExceptionFlowSummaryFragment fragment,
            ExceptionFlowCallContext callContext)
        {
            if (!TryResolveDelegateTarget(
                    invocation.Expression,
                    semanticModel,
                    out IMethodSymbol? targetMethod) ||
                targetMethod == null)
            {
                fragment.AddUncertainTarget(
                    "Delegate invocation");

                return;
            }

            ExceptionFlowCallContext targetContext =
                CreateCallContext(
                    targetMethod,
                    invocation.ArgumentList.Arguments,
                    semanticModel,
                    callContext);

            ExceptionFlowCallableKey targetKey =
                new(
                    targetMethod,
                    targetContext.Key);

            graph.GetOrAdd(
                targetKey,
                targetContext);

            fragment.AddCallEdge(
                new ExceptionFlowSummaryCallEdge(
                    targetKey,
                    CreatePathStep(
                        ExceptionFlowPathStepKind
                            .DelegateInvocation,
                        targetMethod,
                        invocation)));
        }

        /// <summary>
        /// Attempts to resolve the concrete target of a delegate expression.
        /// </summary>
        /// <param name="expression">
        /// The expression invoked through the delegate.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for target resolution.
        /// </param>
        /// <param name="targetMethod">
        /// The resolved anonymous function, local function, or method-group
        /// target.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if one stable target was resolved;
        /// otherwise <see langword="false"/>.
        /// </returns>
        private static bool TryResolveDelegateTarget(
            ExpressionSyntax expression,
            SemanticModel semanticModel,
            out IMethodSymbol? targetMethod)
        {
            HashSet<ISymbol> inspectedSymbols =
                new(SymbolEqualityComparer.Default);

            return TryResolveDelegateTarget(
                expression,
                semanticModel,
                inspectedSymbols,
                out targetMethod);
        }

        /// <summary>
        /// Attempts to resolve a delegate target while preventing cycles
        /// between local delegate variables.
        /// </summary>
        /// <param name="expression">
        /// The delegate-valued expression.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for target resolution.
        /// </param>
        /// <param name="inspectedSymbols">
        /// The local symbols already followed during the current resolution.
        /// </param>
        /// <param name="targetMethod">
        /// The resolved callable target.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if one stable target was resolved;
        /// otherwise <see langword="false"/>.
        /// </returns>
        private static bool TryResolveDelegateTarget(
            ExpressionSyntax expression,
            SemanticModel semanticModel,
            HashSet<ISymbol> inspectedSymbols,
            out IMethodSymbol? targetMethod)
        {
            targetMethod = null;

            ExpressionSyntax unwrappedExpression =
                UnwrapDelegateExpression(
                    expression);

            if (unwrappedExpression
                    is AnonymousFunctionExpressionSyntax
                        anonymousFunction &&
                semanticModel.GetOperation(anonymousFunction)
                    is IAnonymousFunctionOperation anonymousOperation)
            {
                targetMethod = anonymousOperation.Symbol;
                return true;
            }

            if (unwrappedExpression
                    is ObjectCreationExpressionSyntax creation &&
                creation.ArgumentList?.Arguments.Count == 1)
            {
                return TryResolveDelegateTarget(
                    creation.ArgumentList.Arguments[0].Expression,
                    semanticModel,
                    inspectedSymbols,
                    out targetMethod);
            }

            if (unwrappedExpression
                    is ImplicitObjectCreationExpressionSyntax
                        implicitCreation &&
                implicitCreation.ArgumentList.Arguments.Count == 1)
            {
                return TryResolveDelegateTarget(
                    implicitCreation.ArgumentList.Arguments[0].Expression,
                    semanticModel,
                    inspectedSymbols,
                    out targetMethod);
            }

            SymbolInfo symbolInfo =
                semanticModel.GetSymbolInfo(
                    unwrappedExpression);

            if (symbolInfo.Symbol is IMethodSymbol methodSymbol &&
                methodSymbol.MethodKind !=
                    MethodKind.DelegateInvoke)
            {
                targetMethod = methodSymbol;
                return true;
            }

            if (symbolInfo.Symbol is ILocalSymbol localSymbol)
            {
                return TryResolveStableDelegateLocal(
                    localSymbol,
                    semanticModel,
                    inspectedSymbols,
                    out targetMethod);
            }

            IMethodSymbol[] candidateMethods =
                symbolInfo.CandidateSymbols
                    .OfType<IMethodSymbol>()
                    .Where(
                        static candidate =>
                            candidate.MethodKind !=
                            MethodKind.DelegateInvoke)
                    .ToArray();

            if (candidateMethods.Length == 1)
            {
                targetMethod = candidateMethods[0];
                return true;
            }

            return false;
        }

        /// <summary>
        /// Removes syntax wrappers that do not change a delegate target.
        /// </summary>
        /// <param name="expression">
        /// The expression to unwrap.
        /// </param>
        /// <returns>The innermost delegate-valued expression.</returns>
        private static ExpressionSyntax UnwrapDelegateExpression(
            ExpressionSyntax expression)
        {
            ExpressionSyntax current =
                expression;

            while (true)
            {
                switch (current)
                {
                    case ParenthesizedExpressionSyntax parenthesized:
                        current = parenthesized.Expression;
                        continue;

                    case CastExpressionSyntax cast:
                        current = cast.Expression;
                        continue;

                    case CheckedExpressionSyntax checkedExpression:
                        current = checkedExpression.Expression;
                        continue;

                    case PostfixUnaryExpressionSyntax postfix
                        when postfix.IsKind(
                            SyntaxKind
                                .SuppressNullableWarningExpression):
                        current = postfix.Operand;
                        continue;

                    default:
                        return current;
                }
            }
        }

        /// <summary>
        /// Attempts to resolve a local delegate variable with exactly one
        /// stable initializer.
        /// </summary>
        /// <param name="localSymbol">
        /// The local delegate variable.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model associated with the invocation.
        /// </param>
        /// <param name="inspectedSymbols">
        /// The local symbols already followed during target resolution.
        /// </param>
        /// <param name="targetMethod">
        /// The resolved callable target.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the local has one stable target;
        /// otherwise <see langword="false"/>.
        /// </returns>
        private static bool TryResolveStableDelegateLocal(
            ILocalSymbol localSymbol,
            SemanticModel semanticModel,
            HashSet<ISymbol> inspectedSymbols,
            out IMethodSymbol? targetMethod)
        {
            targetMethod = null;

            ISymbol normalizedSymbol =
                localSymbol.OriginalDefinition;

            if (!inspectedSymbols.Add(
                    normalizedSymbol) ||
                localSymbol.DeclaringSyntaxReferences.Length != 1)
            {
                return false;
            }

            if (localSymbol.DeclaringSyntaxReferences[0]
                    .GetSyntax()
                is not VariableDeclaratorSyntax declarator ||
                declarator.Initializer == null)
            {
                return false;
            }

            SemanticModel? declarationSemanticModel =
                GetSemanticModelForSyntaxTree(
                    semanticModel,
                    declarator.SyntaxTree);

            if (declarationSemanticModel == null ||
                HasDelegateLocalWrites(
                    localSymbol,
                    declarator,
                    declarationSemanticModel))
            {
                return false;
            }

            return TryResolveDelegateTarget(
                declarator.Initializer.Value,
                declarationSemanticModel,
                inspectedSymbols,
                out targetMethod);
        }

        /// <summary>
        /// Determines whether a local delegate can be reassigned or modified
        /// after its declaration initializer.
        /// </summary>
        /// <param name="localSymbol">
        /// The local delegate symbol.
        /// </param>
        /// <param name="declarator">
        /// The variable declaration containing its initial value.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for symbol comparison.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if another write may target the local;
        /// otherwise <see langword="false"/>.
        /// </returns>
        private static bool HasDelegateLocalWrites(
            ILocalSymbol localSymbol,
            VariableDeclaratorSyntax declarator,
            SemanticModel semanticModel)
        {
            SyntaxNode root =
                declarator.SyntaxTree.GetRoot();

            foreach (AssignmentExpressionSyntax assignment
                     in root.DescendantNodes()
                         .OfType<AssignmentExpressionSyntax>())
            {
                if (ContainsLocalSymbolReference(
                        assignment.Left,
                        localSymbol,
                        semanticModel))
                {
                    return true;
                }
            }

            foreach (PrefixUnaryExpressionSyntax prefix
                     in root.DescendantNodes()
                         .OfType<PrefixUnaryExpressionSyntax>())
            {
                if (!prefix.IsKind(
                        SyntaxKind.PreIncrementExpression) &&
                    !prefix.IsKind(
                        SyntaxKind.PreDecrementExpression))
                {
                    continue;
                }

                if (ContainsLocalSymbolReference(
                        prefix.Operand,
                        localSymbol,
                        semanticModel))
                {
                    return true;
                }
            }

            foreach (PostfixUnaryExpressionSyntax postfix
                     in root.DescendantNodes()
                         .OfType<PostfixUnaryExpressionSyntax>())
            {
                if (!postfix.IsKind(
                        SyntaxKind.PostIncrementExpression) &&
                    !postfix.IsKind(
                        SyntaxKind.PostDecrementExpression))
                {
                    continue;
                }

                if (ContainsLocalSymbolReference(
                        postfix.Operand,
                        localSymbol,
                        semanticModel))
                {
                    return true;
                }
            }

            foreach (ArgumentSyntax argument
                     in root.DescendantNodes()
                         .OfType<ArgumentSyntax>())
            {
                if (!argument.RefKindKeyword.IsKind(
                        SyntaxKind.RefKeyword) &&
                    !argument.RefKindKeyword.IsKind(
                        SyntaxKind.OutKeyword))
                {
                    continue;
                }

                if (ContainsLocalSymbolReference(
                        argument.Expression,
                        localSymbol,
                        semanticModel))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Determines whether an expression contains a reference to a
        /// specified local symbol.
        /// </summary>
        /// <param name="expression">
        /// The expression to inspect.
        /// </param>
        /// <param name="localSymbol">
        /// The expected local symbol.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for symbol resolution.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the expression references the local;
        /// otherwise <see langword="false"/>.
        /// </returns>
        private static bool ContainsLocalSymbolReference(
            ExpressionSyntax expression,
            ILocalSymbol localSymbol,
            SemanticModel semanticModel)
        {
            foreach (ExpressionSyntax candidate
                     in expression.DescendantNodesAndSelf()
                         .OfType<ExpressionSyntax>())
            {
                SymbolInfo symbolInfo =
                    semanticModel.GetSymbolInfo(
                        candidate);

                if (symbolInfo.Symbol != null &&
                    SymbolEqualityComparer.Default.Equals(
                        symbolInfo.Symbol.OriginalDefinition,
                        localSymbol.OriginalDefinition))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Adds locally modeled exception sources for one known framework
        /// throw helper.
        /// </summary>
        /// <param name="invocation">
        /// The framework-helper invocation.
        /// </param>
        /// <param name="methodSymbol">
        /// The resolved helper method.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for argument analysis.
        /// </param>
        /// <param name="fragment">
        /// The local summary fragment.
        /// </param>
        /// <param name="callContext">
        /// The value facts known for the current callable.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the invocation is a modeled helper;
        /// otherwise <see langword="false"/>.
        /// </returns>
        private static bool TryAddKnownFrameworkSummarySources(
            InvocationExpressionSyntax invocation,
            IMethodSymbol methodSymbol,
            SemanticModel semanticModel,
            ExceptionFlowSummaryFragment fragment,
            ExceptionFlowCallContext callContext)
        {
            HashSet<INamedTypeSymbol> modeledExceptions =
                new(SymbolEqualityComparer.Default);

            if (!KnownFrameworkExceptionModel
                    .TryAddThrownExceptionTypes(
                        methodSymbol,
                        semanticModel.Compilation,
                        modeledExceptions))
            {
                return false;
            }

            ExceptionFlowValueFacts guardedArgumentFacts =
                GetGuardedArgumentFacts(
                    invocation,
                    methodSymbol,
                    semanticModel,
                    callContext);

            bool isArgumentNullGuard =
                KnownFrameworkExceptionModel
                    .IsArgumentNullThrowIfNull(
                        methodSymbol,
                        semanticModel.Compilation);

            bool isNullOrEmptyGuard =
                KnownFrameworkExceptionModel
                    .IsArgumentExceptionThrowIfNullOrEmpty(
                        methodSymbol,
                        semanticModel.Compilation);

            bool isNullOrWhiteSpaceGuard =
                KnownFrameworkExceptionModel
                    .IsArgumentExceptionThrowIfNullOrWhiteSpace(
                        methodSymbol,
                        semanticModel.Compilation);

            foreach (INamedTypeSymbol exceptionType
                     in modeledExceptions)
            {
                if (exceptionType == null)
                {
                    continue;
                }

                if (IsSuppressedKnownFrameworkException(
                        exceptionType,
                        semanticModel.Compilation,
                        guardedArgumentFacts,
                        isArgumentNullGuard,
                        isNullOrEmptyGuard,
                        isNullOrWhiteSpaceGuard))
                {
                    continue;
                }

                fragment.AddSource(
                    new ExceptionFlowSummarySource(
                        exceptionType,
                        CreateTerminalPath(
                            ExceptionFlowPathStepKind
                                .FrameworkThrowHelper,
                            methodSymbol,
                            invocation)));
            }

            return true;
        }

        /// <summary>
        /// Collects a locally modeled delegate exception-factory source.
        /// </summary>
        /// <param name="invocation">
        /// The invocation supplying a delegate exception factory.
        /// </param>
        /// <param name="methodSymbol">
        /// The target method symbol.
        /// </param>
        /// <param name="semanticContext">
        /// The project-closure semantic context.
        /// </param>
        /// <param name="fragment">
        /// The local summary fragment.
        /// </param>
        private static void CollectSummaryDelegateFactorySources(
            InvocationExpressionSyntax invocation,
            IMethodSymbol methodSymbol,
            ProjectClosureSemanticContext semanticContext,
            ExceptionFlowSummaryFragment fragment)
        {
            HashSet<int> throwingParameterIndexes =
                FindThrowingDelegateParameterIndexes(
                    methodSymbol,
                    semanticContext);

            if (throwingParameterIndexes.Count == 0)
            {
                return;
            }

            SeparatedSyntaxList<ArgumentSyntax> arguments =
                invocation.ArgumentList.Arguments;

            for (int index = 0;
                 index < arguments.Count;
                 index++)
            {
                ArgumentSyntax argument =
                    arguments[index];

                int parameterIndex =
                    GetParameterIndexForArgument(
                        argument,
                        index,
                        methodSymbol);

                if (!throwingParameterIndexes.Contains(
                        parameterIndex))
                {
                    continue;
                }

                ObjectCreationExpressionSyntax? creation =
                    GetExceptionObjectCreation(
                        argument.Expression);

                if (creation == null ||
                    !semanticContext.TryGetSemanticModel(
                        creation.SyntaxTree,
                        out SemanticModel creationSemanticModel) ||
                    creationSemanticModel == null)
                {
                    continue;
                }

                SymbolInfo creationSymbolInfo =
                    creationSemanticModel.GetSymbolInfo(
                        creation.Type);

                if (creationSymbolInfo.Symbol
                    is not INamedTypeSymbol exceptionType)
                {
                    continue;
                }

                ExceptionFlowPathStep invocationStep =
                    CreatePathStep(
                        ExceptionFlowPathStepKind.MethodCall,
                        methodSymbol,
                        invocation);

                ExceptionFlowPathStep factoryStep =
                    CreatePathStep(
                        ExceptionFlowPathStepKind
                            .DelegateExceptionFactory,
                        exceptionType,
                        creation);

                fragment.AddSource(
                    new ExceptionFlowSummarySource(
                        exceptionType,
                        new ExceptionFlowPath(
                            factoryStep)
                            .Prepend(
                                invocationStep)));
            }
        }

        /// <summary>
        /// Collects constructor-call edges from object creation expressions.
        /// </summary>
        /// <param name="node">The node to inspect.</param>
        /// <param name="semanticModel">
        /// The semantic model used for symbol resolution.
        /// </param>
        /// <param name="graph">
        /// The graph receiving constructor nodes.
        /// </param>
        /// <param name="fragment">
        /// The local summary fragment.
        /// </param>
        /// <param name="callContext">
        /// The value facts known for the current callable.
        /// </param>
        private static void AnalyzeSummaryObjectCreations(
            SyntaxNode node,
            SemanticModel semanticModel,
            ExceptionFlowSummaryGraph graph,
            ExceptionFlowSummaryFragment fragment,
            ExceptionFlowCallContext callContext)
        {
            foreach (ObjectCreationExpressionSyntax creation
                     in GetSummaryDescendantsAndSelf
                         <ObjectCreationExpressionSyntax>(node))
            {
                if (IsPartOfDirectThrow(creation))
                {
                    continue;
                }

                SymbolInfo symbolInfo =
                    semanticModel.GetSymbolInfo(creation);

                if (symbolInfo.Symbol
                    is not IMethodSymbol constructorSymbol)
                {
                    continue;
                }

                SeparatedSyntaxList<ArgumentSyntax> arguments =
                    creation.ArgumentList?.Arguments ??
                    default;

                ExceptionFlowCallContext targetContext =
                    CreateCallContext(
                        constructorSymbol,
                        arguments,
                        semanticModel,
                        callContext);

                ExceptionFlowCallableKey targetKey =
                    new(
                        constructorSymbol,
                        targetContext.Key);

                graph.GetOrAdd(
                    targetKey,
                    targetContext);

                fragment.AddCallEdge(
                    new ExceptionFlowSummaryCallEdge(
                        targetKey,
                        CreatePathStep(
                            ExceptionFlowPathStepKind
                                .ConstructorCall,
                            constructorSymbol,
                            creation)));
            }
        }

        /// <summary>
        /// Collects property and indexer getter edges, including known runtime
        /// accessor implementations.
        /// </summary>
        /// <param name="node">The node to inspect.</param>
        /// <param name="semanticModel">
        /// The semantic model used for symbol and operation resolution.
        /// </param>
        /// <param name="semanticContext">
        /// The project-closure semantic context.
        /// </param>
        /// <param name="graph">
        /// The graph receiving getter nodes.
        /// </param>
        /// <param name="fragment">
        /// The local summary fragment.
        /// </param>
        /// <param name="callContext">
        /// The value facts known for the current callable.
        /// </param>
        private static void AnalyzeSummaryPropertyAndIndexerAccesses(
            SyntaxNode node,
            SemanticModel semanticModel,
            ProjectClosureSemanticContext semanticContext,
            ExceptionFlowSummaryGraph graph,
            ExceptionFlowSummaryFragment fragment,
            ExceptionFlowCallContext callContext)
        {
            foreach (MemberAccessExpressionSyntax memberAccess
                     in GetSummaryDescendantsAndSelf
                         <MemberAccessExpressionSyntax>(node))
            {
                SymbolInfo symbolInfo =
                    semanticModel.GetSymbolInfo(
                        memberAccess);

                if (symbolInfo.Symbol
                    is not IPropertySymbol propertySymbol)
                {
                    continue;
                }

                IPropertyReferenceOperation? propertyOperation =
                    semanticModel.GetOperation(
                        memberAccess)
                    as IPropertyReferenceOperation;

                AddSummaryPropertyGetterEdge(
                    propertySymbol,
                    memberAccess,
                    default,
                    propertyOperation,
                    semanticModel,
                    semanticContext,
                    graph,
                    fragment,
                    callContext);
            }

            foreach (ElementAccessExpressionSyntax elementAccess
                     in GetSummaryDescendantsAndSelf
                         <ElementAccessExpressionSyntax>(node))
            {
                SymbolInfo symbolInfo =
                    semanticModel.GetSymbolInfo(
                        elementAccess);

                if (symbolInfo.Symbol
                    is not IPropertySymbol indexerSymbol)
                {
                    continue;
                }

                IPropertyReferenceOperation? indexerOperation =
                    semanticModel.GetOperation(
                        elementAccess)
                    as IPropertyReferenceOperation;

                AddSummaryPropertyGetterEdge(
                    indexerSymbol,
                    elementAccess,
                    elementAccess.ArgumentList.Arguments,
                    indexerOperation,
                    semanticModel,
                    semanticContext,
                    graph,
                    fragment,
                    callContext);
            }
        }
    }
}
