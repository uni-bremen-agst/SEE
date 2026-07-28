using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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
        /// Collects method-call edges and locally modeled invocation sources.
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
                     in GetDescendantsAndSelfExcludingNestedTry
                         <InvocationExpressionSyntax>(node))
            {
                SymbolInfo symbolInfo =
                    semanticModel.GetSymbolInfo(invocation);

                if (symbolInfo.Symbol
                    is not IMethodSymbol methodSymbol)
                {
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

                ExceptionFlowCallContext targetContext =
                    CreateCallContext(
                        methodSymbol,
                        invocation.ArgumentList.Arguments,
                        semanticModel,
                        callContext);

                ExceptionFlowCallableKey targetKey =
                    new(
                        methodSymbol,
                        targetContext.Key);

                graph.GetOrAdd(
                    targetKey,
                    targetContext);

                fragment.AddCallEdge(
                    new ExceptionFlowSummaryCallEdge(
                        targetKey,
                        CreatePathStep(
                            ExceptionFlowPathStepKind.MethodCall,
                            methodSymbol,
                            invocation)));
            }
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
                     in GetDescendantsAndSelfExcludingNestedTry
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
        /// Collects property and indexer getter edges.
        /// </summary>
        /// <param name="node">The node to inspect.</param>
        /// <param name="semanticModel">
        /// The semantic model used for symbol resolution.
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
            ExceptionFlowSummaryGraph graph,
            ExceptionFlowSummaryFragment fragment,
            ExceptionFlowCallContext callContext)
        {
            foreach (MemberAccessExpressionSyntax memberAccess
                     in GetDescendantsAndSelfExcludingNestedTry
                         <MemberAccessExpressionSyntax>(node))
            {
                SymbolInfo symbolInfo =
                    semanticModel.GetSymbolInfo(memberAccess);

                if (symbolInfo.Symbol
                    is not IPropertySymbol propertySymbol)
                {
                    continue;
                }

                ISymbol targetSymbol;

                if (propertySymbol.GetMethod
                    is IMethodSymbol propertyGetter)
                {
                    targetSymbol = propertyGetter;
                }
                else
                {
                    targetSymbol = propertySymbol;
                }

                ExceptionFlowCallContext targetContext =
                    new(targetSymbol);

                ExceptionFlowCallableKey targetKey =
                    new(
                        targetSymbol,
                        targetContext.Key);

                graph.GetOrAdd(
                    targetKey,
                    targetContext);

                fragment.AddCallEdge(
                    new ExceptionFlowSummaryCallEdge(
                        targetKey,
                        CreatePathStep(
                            ExceptionFlowPathStepKind.PropertyGetter,
                            propertySymbol,
                            memberAccess)));
            }

            foreach (ElementAccessExpressionSyntax elementAccess
                     in GetDescendantsAndSelfExcludingNestedTry
                         <ElementAccessExpressionSyntax>(node))
            {
                SymbolInfo symbolInfo =
                    semanticModel.GetSymbolInfo(elementAccess);

                if (symbolInfo.Symbol
                    is not IPropertySymbol indexerSymbol)
                {
                    continue;
                }

                ISymbol targetSymbol;

                if (indexerSymbol.GetMethod
                    is IMethodSymbol indexerGetterSymbol)
                {
                    targetSymbol = indexerGetterSymbol;
                }
                else
                {
                    targetSymbol = indexerSymbol;
                }

                ExceptionFlowCallContext targetContext =
                    indexerSymbol.GetMethod
                        is IMethodSymbol indexerGetter
                            ? CreateCallContext(
                                indexerGetter,
                                elementAccess.ArgumentList.Arguments,
                                semanticModel,
                                callContext)
                            : new ExceptionFlowCallContext(
                                indexerSymbol);

                ExceptionFlowCallableKey targetKey =
                    new(
                        targetSymbol,
                        targetContext.Key);

                graph.GetOrAdd(
                    targetKey,
                    targetContext);

                fragment.AddCallEdge(
                    new ExceptionFlowSummaryCallEdge(
                        targetKey,
                        CreatePathStep(
                            ExceptionFlowPathStepKind.IndexerGetter,
                            indexerSymbol,
                            elementAccess)));
            }
        }
    }
}
