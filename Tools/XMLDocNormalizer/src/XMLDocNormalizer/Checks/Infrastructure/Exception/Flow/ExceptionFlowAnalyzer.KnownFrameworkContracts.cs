using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using XMLDocNormalizer.Checks.Infrastructure.Exception;
using XMLDocNormalizer.Models;
using XMLDocNormalizer.Models.DTO;

namespace XMLDocNormalizer.Checks.Infrastructure.Exception.Flow
{
    /// <summary>
    /// Contains shared call-site evaluation and application for curated
    /// framework exception contracts.
    /// </summary>
    internal static partial class ExceptionFlowAnalyzer
    {
        /// <summary>
        /// Looks up and evaluates a framework exception contract without
        /// constructing argument facts for unregistered callables.
        /// </summary>
        /// <param name="methodSymbol">The resolved callable.</param>
        /// <param name="arguments">Arguments in source order.</param>
        /// <param name="semanticModel">
        /// The semantic model used for value facts.
        /// </param>
        /// <param name="callContext">
        /// The value facts known while analyzing the caller.
        /// </param>
        /// <returns>The explicit matched or unmatched contract evaluation.</returns>
        private static KnownFrameworkExceptionContractEvaluation EvaluateKnownFrameworkContract(
            IMethodSymbol methodSymbol,
            SeparatedSyntaxList<ArgumentSyntax> arguments,
            SemanticModel semanticModel,
            ExceptionFlowCallContext callContext)
        {
            KnownFrameworkExceptionContract? contract =
                KnownFrameworkExceptionModel.FindContract(
                    methodSymbol,
                    semanticModel.Compilation);

            if (contract == null)
            {
                return KnownFrameworkExceptionContractEvaluation.NoMatch;
            }

            KnownFrameworkExceptionContractArgument[] contractArguments =
                CreateKnownFrameworkContractArguments(
                    methodSymbol,
                    arguments,
                    semanticModel,
                    callContext);

            return contract.Evaluate(contractArguments, semanticModel.Compilation);
        }

        /// <summary>
        /// Creates contract arguments ordered by target parameter ordinal.
        /// </summary>
        /// <param name="methodSymbol">The resolved callable.</param>
        /// <param name="arguments">Arguments in source order.</param>
        /// <param name="semanticModel">
        /// The semantic model used for value facts.
        /// </param>
        /// <param name="callContext">
        /// The value facts known while analyzing the caller.
        /// </param>
        /// <returns>The contract arguments ordered by parameter ordinal.</returns>
        private static KnownFrameworkExceptionContractArgument[] CreateKnownFrameworkContractArguments(
            IMethodSymbol methodSymbol,
            SeparatedSyntaxList<ArgumentSyntax> arguments,
            SemanticModel semanticModel,
            ExceptionFlowCallContext callContext)
        {
            KnownFrameworkExceptionContractArgument[] contractArguments =
                new KnownFrameworkExceptionContractArgument[methodSymbol.Parameters.Length];

            for (int argumentIndex = 0; argumentIndex < arguments.Count; argumentIndex++)
            {
                ArgumentSyntax argument = arguments[argumentIndex];
                int parameterIndex = GetParameterIndexForArgument(
                    argument,
                    argumentIndex,
                    methodSymbol);

                if (parameterIndex < 0
                    || parameterIndex >= contractArguments.Length
                    || argument.RefKindKeyword.IsKind(SyntaxKind.OutKeyword))
                {
                    continue;
                }

                ExceptionFlowValueFacts facts = GetExpressionValueFacts(
                    argument.Expression,
                    semanticModel,
                    callContext);

                contractArguments[parameterIndex] =
                    new KnownFrameworkExceptionContractArgument(facts);
            }

            return contractArguments;
        }

        /// <summary>
        /// Adds the positive exception sources from an evaluated framework
        /// contract to a direct analysis result.
        /// </summary>
        /// <param name="evaluation">The evaluated framework contract.</param>
        /// <param name="result">The result receiving positive sources.</param>
        /// <param name="methodSymbol">The matched framework callable.</param>
        /// <param name="sourceNode">The source node invoking the callable.</param>
        internal static void AddKnownFrameworkContractExceptions(
            KnownFrameworkExceptionContractEvaluation evaluation,
            ExceptionFlowAnalysisResult result,
            IMethodSymbol methodSymbol,
            SyntaxNode sourceNode)
        {
            foreach (INamedTypeSymbol exceptionType in evaluation.PossibleExceptionTypes)
            {
                if (exceptionType == null)
                {
                    continue;
                }

                result.AddExceptionPath(
                    exceptionType,
                    CreateTerminalPath(
                        ExceptionFlowPathStepKind.FrameworkThrowHelper,
                        methodSymbol,
                        sourceNode));
            }
        }

        /// <summary>
        /// Adds the positive exception sources from an evaluated framework
        /// contract to a local summary fragment.
        /// </summary>
        /// <param name="evaluation">The evaluated framework contract.</param>
        /// <param name="fragment">The fragment receiving positive sources.</param>
        /// <param name="methodSymbol">The matched framework callable.</param>
        /// <param name="sourceNode">The source node invoking the callable.</param>
        private static void AddKnownFrameworkContractSummarySources(
            KnownFrameworkExceptionContractEvaluation evaluation,
            ExceptionFlowSummaryFragment fragment,
            IMethodSymbol methodSymbol,
            SyntaxNode sourceNode)
        {
            foreach (INamedTypeSymbol exceptionType in evaluation.PossibleExceptionTypes)
            {
                if (exceptionType == null)
                {
                    continue;
                }

                fragment.AddSource(
                    new ExceptionFlowSummarySource(
                        exceptionType,
                        CreateTerminalPath(
                            ExceptionFlowPathStepKind.FrameworkThrowHelper,
                            methodSymbol,
                            sourceNode)));
            }
        }
    }
}
