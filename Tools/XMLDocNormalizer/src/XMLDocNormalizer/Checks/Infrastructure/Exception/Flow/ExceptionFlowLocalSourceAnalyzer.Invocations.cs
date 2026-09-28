using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using XMLDocNormalizer.Checks.Infrastructure.Exception;
using XMLDocNormalizer.Models;
using XMLDocNormalizer.Models.DTO;
using static XMLDocNormalizer.Checks.Infrastructure.Exception.Flow.ExceptionFlowAnalyzer;

namespace XMLDocNormalizer.Checks.Infrastructure.Exception.Flow
{
    /// <summary>
    /// Contains method-invocation and exception-factory analysis.
    /// </summary>
    internal static partial class ExceptionFlowLocalSourceAnalyzer
    {
        /// <summary>
        /// Resolves method invocations within the specified node, recognizes
        /// known framework exception sources, and optionally analyzes invoked
        /// method bodies transitively.
        /// </summary>
        /// <param name="node">The node to inspect for invocations.</param>
        /// <param name="semanticModel">
        /// The semantic model used for symbol resolution.
        /// </param>
        /// <param name="semanticContext">
        /// The project-closure semantic context.
        /// </param>
        /// <param name="result">
        /// The accumulated exception-flow result.
        /// </param>
        /// <param name="traversalState">
        /// The traversal state used to prevent recursive analysis cycles.
        /// </param>
        /// <param name="mode">The traversal mode.</param>
        /// <param name="callContext">
        /// The call-site facts known for the currently analyzed callable.
        /// </param>
        private static void AnalyzeInvocations(
            SyntaxNode node,
            SemanticModel semanticModel,
            ExceptionFlowSemanticEnvironment semanticContext,
            ExceptionFlowAnalysisResult result,
            ExceptionFlowTraversalState traversalState,
            ExceptionFlowTraversalMode mode,
            ExceptionFlowCallContext callContext)
        {
            foreach (InvocationExpressionSyntax invocation
                     in GetCurrentCallableDescendantsAndSelf
                         <InvocationExpressionSyntax>(node))
            {
                SymbolInfo symbolInfo =
                    semanticModel.GetSymbolInfo(invocation);

                if (symbolInfo.Symbol
                    is not IMethodSymbol methodSymbol)
                {
                    continue;
                }

                if (TryAddKnownFrameworkThrownExceptions(
                        invocation,
                        methodSymbol,
                        semanticModel,
                        result,
                        callContext))
                {
                    continue;
                }

                if (mode == ExceptionFlowTraversalMode.Direct)
                {
                    continue;
                }

                if (methodSymbol.MethodKind ==
                    MethodKind.DelegateInvoke)
                {
                    AnalyzeDelegateInvocation(
                        invocation,
                        methodSymbol,
                        semanticModel,
                        semanticContext,
                        result,
                        traversalState,
                        callContext);

                    continue;
                }

                ExceptionFlowExternalDocumentationEvidence.AddToAnalysisResult(
                    invocation,
                    methodSymbol,
                    semanticModel,
                    result);

                CollectThrownExceptionsFromDelegateFactoryCall(
                    invocation,
                    methodSymbol,
                    semanticContext,
                    result);

                if (TryAnalyzeRecursiveRuntimeDispatch(
                        invocation,
                        methodSymbol,
                        semanticModel,
                        semanticContext,
                        result,
                        traversalState,
                        callContext))
                {
                    continue;
                }

                IMethodSymbol targetMethod =
                    GetInvocationAnalysisTarget(
                        methodSymbol);

                ExceptionFlowCallContext calleeContext =
                    CreateInvocationCallContext(
                        invocation,
                        methodSymbol,
                        semanticModel,
                        callContext);

                if (!traversalState.TryMarkAnalyzed(
                        targetMethod,
                        calleeContext))
                {
                    continue;
                }

                if (!AnalyzeSymbol(
                        targetMethod,
                        semanticContext,
                        result,
                        traversalState,
                        calleeContext))
                {
                    ExceptionFlowUncertaintyRecorder.AddSymbol(
                        result,
                        targetMethod);
                }
            }
        }

        /// <summary>
        /// Adds exceptions from a known framework throw helper while
        /// suppressing exception types whose preconditions are proven false
        /// at the current call site.
        /// </summary>
        /// <param name="invocation">
        /// The framework helper invocation.
        /// </param>
        /// <param name="methodSymbol">
        /// The resolved framework helper symbol.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for expression analysis.
        /// </param>
        /// <param name="result">
        /// The accumulated exception-flow result.
        /// </param>
        /// <param name="callContext">
        /// The call-site facts known for the current callable.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the invocation is a known framework
        /// exception source; otherwise <see langword="false"/>.
        /// </returns>
        private static bool TryAddKnownFrameworkThrownExceptions(
            InvocationExpressionSyntax invocation,
            IMethodSymbol methodSymbol,
            SemanticModel semanticModel,
            ExceptionFlowAnalysisResult result,
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

            AddKnownFrameworkContractExceptions(
                evaluation,
                result,
                methodSymbol,
                invocation);

            return evaluation.ClosesExternalAnalysis;
        }

        /// <summary>
        /// Collects exception types from invocations where the callee throws
        /// the result of a delegate parameter invocation and the call site
        /// supplies a lambda or anonymous method that directly creates an
        /// exception object.
        /// </summary>
        /// <param name="invocation">
        /// The invocation to inspect.
        /// </param>
        /// <param name="methodSymbol">
        /// The resolved target method symbol.
        /// </param>
        /// <param name="semanticContext">
        /// The project-closure semantic context.
        /// </param>
        /// <param name="result">
        /// The accumulated exception-flow result.
        /// </param>
        private static void
            CollectThrownExceptionsFromDelegateFactoryCall(
                InvocationExpressionSyntax invocation,
                IMethodSymbol methodSymbol,
                ExceptionFlowSemanticEnvironment semanticContext,
                ExceptionFlowAnalysisResult result)
        {
            HashSet<int> throwingDelegateParameterIndexes =
                FindThrowingDelegateParameterIndexes(
                    methodSymbol,
                    semanticContext);

            if (throwingDelegateParameterIndexes.Count == 0)
            {
                return;
            }

            SeparatedSyntaxList<ArgumentSyntax> arguments =
                invocation.ArgumentList.Arguments;

            for (int i = 0; i < arguments.Count; i++)
            {
                ArgumentSyntax argument = arguments[i];

                int parameterIndex =
                    ExceptionFlowArgumentMapper.GetParameterIndex(
                        argument,
                        i,
                        methodSymbol);

                if (!throwingDelegateParameterIndexes.Contains(
                        parameterIndex))
                {
                    continue;
                }

                ObjectCreationExpressionSyntax? creation =
                    GetExceptionObjectCreation(
                        argument.Expression);

                if (creation == null)
                {
                    continue;
                }

                if (!semanticContext.TryGetSemanticModel(
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
                    is INamedTypeSymbol typeSymbol)
                {
                    ExceptionFlowPathStep invocationStep =
                        ExceptionFlowPathFactory.CreateStep(
                            ExceptionFlowPathStepKind.MethodCall,
                            methodSymbol,
                            invocation);

                    ExceptionFlowPathStep factoryStep =
                        ExceptionFlowPathFactory.CreateStep(
                            ExceptionFlowPathStepKind
                                .DelegateExceptionFactory,
                            typeSymbol,
                            creation);

                    result.AddExceptionPath(
                        typeSymbol,
                        new ExceptionFlowPath(factoryStep)
                            .Prepend(invocationStep));
                }
            }
        }

    }
}
