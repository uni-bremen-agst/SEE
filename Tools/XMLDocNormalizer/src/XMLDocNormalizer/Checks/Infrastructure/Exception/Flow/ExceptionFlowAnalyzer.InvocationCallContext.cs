using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

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
            IMethodSymbol? unreducedMethod =
                selectedMethod.ReducedFrom;

            if (unreducedMethod == null ||
                unreducedMethod.DeclaringSyntaxReferences.Length == 0)
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
                unreducedMethod,
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
