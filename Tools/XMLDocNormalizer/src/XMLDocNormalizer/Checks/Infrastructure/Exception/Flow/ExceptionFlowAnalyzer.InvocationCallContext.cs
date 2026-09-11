using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using XMLDocNormalizer.Execution.Semantic;

namespace XMLDocNormalizer.Checks.Infrastructure.Exception.Flow
{
    /// <summary>
    /// Contains call-context mapping for explicitly written method
    /// invocations.
    /// </summary>
    internal static partial class ExceptionFlowAnalyzer
    {
        /// <summary>
        /// Gets the source-level callable whose body represents an invocation.
        /// Reduced extension methods are mapped back to their original static
        /// declaration only when that declaration is available as source.
        /// </summary>
        /// <param name="selectedMethod">
        /// The method symbol selected by Roslyn at the invocation.
        /// </param>
        /// <returns>
        /// The unreduced source extension method when one is available;
        /// otherwise <paramref name="selectedMethod"/>.
        /// </returns>
        private static IMethodSymbol GetInvocationAnalysisTarget(
            IMethodSymbol selectedMethod)
        {
            IMethodSymbol? unreducedMethod =
                selectedMethod.ReducedFrom;

            if (unreducedMethod == null)
            {
                return selectedMethod;
            }

            if (unreducedMethod.DeclaringSyntaxReferences.Length == 0)
            {
                return selectedMethod;
            }

            return unreducedMethod;
        }

        /// <summary>
        /// Gets the callable whose body represents a summary invocation and
        /// resolves a metadata reduced extension declaration once when its
        /// supporting source is registered.
        /// </summary>
        /// <param name="selectedMethod">
        /// The method symbol selected by Roslyn at the invocation.
        /// </param>
        /// <param name="semanticContext">
        /// The semantic context containing registered supporting sources.
        /// </param>
        /// <param name="resolvedSupportingSourceTarget">
        /// The resolved supporting source method when a metadata reduced
        /// extension method can be analyzed through its unreduced declaration.
        /// </param>
        /// <param name="resolvedSupportingSourceScope">
        /// The supporting scope that owns
        /// <paramref name="resolvedSupportingSourceTarget"/>.
        /// </param>
        /// <returns>
        /// The source-defined or supporting-source-backed unreduced extension
        /// method when available; otherwise <paramref name="selectedMethod"/>.
        /// </returns>
        private static IMethodSymbol GetSummaryInvocationAnalysisTarget(
            IMethodSymbol selectedMethod,
            ProjectClosureSemanticContext semanticContext,
            out IMethodSymbol? resolvedSupportingSourceTarget,
            out SemanticCompilationScope? resolvedSupportingSourceScope)
        {
            resolvedSupportingSourceTarget = null;
            resolvedSupportingSourceScope = null;

            IMethodSymbol? unreducedMethod = selectedMethod.ReducedFrom;

            if (unreducedMethod == null)
            {
                return selectedMethod;
            }

            if (unreducedMethod.DeclaringSyntaxReferences.Length != 0)
            {
                return unreducedMethod;
            }

            if (SupportingSourceSymbolResolver.TryResolveMethod(
                    unreducedMethod,
                    semanticContext,
                    out IMethodSymbol sourceMethod,
                    out SemanticCompilationScope supportingScope))
            {
                resolvedSupportingSourceTarget = sourceMethod;
                resolvedSupportingSourceScope = supportingScope;
                return unreducedMethod;
            }

            return selectedMethod;
        }

        /// <summary>
        /// Creates the call context for an explicitly written invocation,
        /// including receiver-to-parameter mapping for source-defined reduced
        /// extension methods.
        /// </summary>
        /// <param name="invocation">
        /// The invocation syntax whose arguments are mapped.
        /// </param>
        /// <param name="selectedMethod">
        /// The method symbol selected by Roslyn at the invocation.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for value-fact analysis.
        /// </param>
        /// <param name="callerContext">
        /// The value facts known while analyzing the caller.
        /// </param>
        /// <returns>
        /// A context associated with the actual analysis target and containing
        /// all safely transferable call-site facts.
        /// </returns>
        private static ExceptionFlowCallContext
            CreateInvocationCallContext(
                InvocationExpressionSyntax invocation,
                IMethodSymbol selectedMethod,
                SemanticModel semanticModel,
                ExceptionFlowCallContext callerContext)
        {
            return CreateInvocationCallContext(
                invocation,
                selectedMethod,
                GetInvocationAnalysisTarget(selectedMethod),
                semanticModel,
                callerContext);
        }

        /// <summary>
        /// Creates the call context for an explicitly written invocation and
        /// a previously selected analysis target.
        /// </summary>
        /// <param name="invocation">The invocation syntax whose arguments are mapped.</param>
        /// <param name="selectedMethod">The method selected at the call site.</param>
        /// <param name="analysisTarget">
        /// The selected method or its analyzable unreduced declaration.
        /// </param>
        /// <param name="semanticModel">The semantic model used for value facts.</param>
        /// <param name="callerContext">The facts known while analyzing the caller.</param>
        /// <returns>
        /// A context associated with <paramref name="analysisTarget"/> and
        /// containing all safely transferable call-site facts.
        /// </returns>
        private static ExceptionFlowCallContext CreateInvocationCallContext(
            InvocationExpressionSyntax invocation,
            IMethodSymbol selectedMethod,
            IMethodSymbol analysisTarget,
            SemanticModel semanticModel,
            ExceptionFlowCallContext callerContext)
        {
            IMethodSymbol? unreducedMethod =
                selectedMethod.ReducedFrom;

            if (unreducedMethod == null
                || !SymbolEqualityComparer.Default.Equals(
                    unreducedMethod.OriginalDefinition,
                    analysisTarget.OriginalDefinition))
            {
                return CreateCallContext(
                    selectedMethod,
                    invocation.ArgumentList.Arguments,
                    semanticModel,
                    callerContext);
            }

            if (unreducedMethod.Parameters.Length !=
                selectedMethod.Parameters.Length + 1)
            {
                return new ExceptionFlowCallContext(
                    unreducedMethod);
            }

            Dictionary<int, ExceptionFlowValueFacts>
                knownParameterFacts =
                    new();

            ExpressionSyntax? receiver =
                GetReducedExtensionReceiver(
                    invocation);

            if (receiver != null)
            {
                ExceptionFlowValueFacts receiverFacts =
                    GetExpressionValueFacts(
                        receiver,
                        semanticModel,
                        callerContext);

                if (receiverFacts !=
                    ExceptionFlowValueFacts.None)
                {
                    knownParameterFacts[0] =
                        receiverFacts.Normalize();
                }
            }

            ExceptionFlowCallContext reducedContext =
                CreateCallContext(
                    selectedMethod,
                    invocation.ArgumentList.Arguments,
                    semanticModel,
                    callerContext);

            for (int reducedParameterIndex = 0;
                 reducedParameterIndex <
                    selectedMethod.Parameters.Length;
                 reducedParameterIndex++)
            {
                ExceptionFlowValueFacts facts =
                    reducedContext.GetParameterFacts(
                        reducedParameterIndex);

                if (facts ==
                    ExceptionFlowValueFacts.None)
                {
                    continue;
                }

                int unreducedParameterIndex =
                    reducedParameterIndex + 1;

                knownParameterFacts[
                    unreducedParameterIndex] =
                        facts;
            }

            return new ExceptionFlowCallContext(
                analysisTarget,
                knownParameterFacts);
        }

        /// <summary>
        /// Gets the syntactic receiver expression of a reduced extension
        /// invocation when it is represented by ordinary member-access
        /// syntax.
        /// </summary>
        /// <param name="invocation">
        /// The reduced extension invocation.
        /// </param>
        /// <returns>
        /// The receiver expression, or <see langword="null"/> when no
        /// ordinary member-access receiver is available.
        /// </returns>
        private static ExpressionSyntax?
            GetReducedExtensionReceiver(
                InvocationExpressionSyntax invocation)
        {
            return invocation.Expression
                    is MemberAccessExpressionSyntax memberAccess
                ? memberAccess.Expression
                : null;
        }
    }
}
