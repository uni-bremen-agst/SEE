using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using XMLDocNormalizer.Checks.Infrastructure.Exception;
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
        internal static void AnalyzeSummaryInvocations(
            SyntaxNode node,
            SemanticModel semanticModel,
            ExceptionFlowSemanticEnvironment semanticContext,
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
                        semanticContext,
                        graph,
                        fragment,
                        callContext);

                    continue;
                }

                SummaryInvocationPlan? invocationPlan =
                    TryCreateSupportingSourceInvocationPlan(
                        invocation,
                        methodSymbol,
                        semanticModel,
                        semanticContext,
                        callContext);

                bool hasCompleteSourceCoverage =
                    invocationPlan?.SourceCoverage ==
                    SummaryInvocationSourceCoverage
                        .CompleteExecutableSourceCoverage;

                if (!hasCompleteSourceCoverage
                    && TryAddKnownFrameworkSummarySources(
                        invocation,
                        methodSymbol,
                        semanticModel,
                        fragment,
                        callContext))
                {
                    continue;
                }

                if (!hasCompleteSourceCoverage)
                {
                    ExceptionFlowExternalDocumentationEvidence.AddToSummaryFragment(
                        invocation,
                        methodSymbol,
                        semanticModel,
                        fragment);
                }

                CollectSummaryDelegateFactorySources(
                    invocation,
                    methodSymbol,
                    semanticContext,
                    fragment);

                if (invocationPlan != null)
                {
                    AddSummaryInvocationEdges(
                        invocationPlan,
                        semanticContext,
                        graph,
                        fragment);
                }
                else
                {
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
        /// <param name="semanticContext">
        /// The project-closure semantic context.
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
            ExceptionFlowSemanticEnvironment semanticContext,
            ExceptionFlowSummaryGraph graph,
            ExceptionFlowSummaryFragment fragment,
            ExceptionFlowCallContext callContext)
        {
            if (!ExceptionFlowDelegateTargetResolver.TryResolveDelegateTarget(
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
                ExceptionFlowContextualFactEvaluator.CreateCallContext(
                    targetMethod,
                    invocation.ArgumentList.Arguments,
                    semanticModel,
                    callContext);

            ExceptionFlowCallableKey targetKey = ExceptionFlowSummaryTargetRegistrar.RegisterMethodTarget(
                targetMethod,
                targetContext,
                semanticContext,
                graph,
                semanticModel.Compilation);

            fragment.AddCallEdge(
                new ExceptionFlowSummaryCallEdge(
                    targetKey,
                    ExceptionFlowPathFactory.CreateStep(
                        ExceptionFlowPathStepKind
                            .DelegateInvocation,
                        targetMethod,
                        invocation)));
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
            KnownFrameworkExceptionContractEvaluation evaluation =
                EvaluateKnownFrameworkContract(
                    methodSymbol,
                    invocation.ArgumentList.Arguments,
                    semanticModel,
                    callContext);

            if (!evaluation.IsMatch)
            {
                return false;
            }

            AddKnownFrameworkContractSummarySources(
                evaluation,
                fragment,
                methodSymbol,
                invocation);

            return evaluation.ClosesExternalAnalysis;
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
            ExceptionFlowSemanticEnvironment semanticContext,
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
                    ExceptionFlowArgumentMapper.GetParameterIndex(
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
                    ExceptionFlowPathFactory.CreateStep(
                        ExceptionFlowPathStepKind.MethodCall,
                        methodSymbol,
                        invocation);

                ExceptionFlowPathStep factoryStep =
                    ExceptionFlowPathFactory.CreateStep(
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
        /// <param name="semanticContext">
        /// The project-closure semantic context.
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
        internal static void AnalyzeSummaryObjectCreations(
            SyntaxNode node,
            SemanticModel semanticModel,
            ExceptionFlowSemanticEnvironment semanticContext,
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
                    ExceptionFlowContextualFactEvaluator.CreateCallContext(
                        constructorSymbol,
                        arguments,
                        semanticModel,
                        callContext);

                ExceptionFlowCallableKey targetKey = ExceptionFlowSummaryTargetRegistrar.RegisterMethodTarget(
                    constructorSymbol,
                    targetContext,
                    semanticContext,
                    graph,
                    semanticModel.Compilation);

                fragment.AddCallEdge(
                    new ExceptionFlowSummaryCallEdge(
                        targetKey,
                        ExceptionFlowPathFactory.CreateStep(
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
        internal static void AnalyzeSummaryPropertyAndIndexerAccesses(
            SyntaxNode node,
            SemanticModel semanticModel,
            ExceptionFlowSemanticEnvironment semanticContext,
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
