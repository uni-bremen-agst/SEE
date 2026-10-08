using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using XMLDocNormalizer.Utils;

namespace XMLDocNormalizer.Checks.Infrastructure.Exception.Flow
{
    /// <summary>
    /// Resolves executable source bodies at the summary-target boundary.
    /// </summary>
    internal static partial class ExceptionFlowSummaryTargetRegistrar
    {
        /// <summary>
        /// Determines whether an invocation target has a source body that the
        /// summary analyzer can inspect through the current semantic context.
        /// </summary>
        /// <param name="methodSymbol">The possible source invocation target.</param>
        /// <param name="semanticContext">The semantic context used by summary analysis.</param>
        /// <returns>
        /// <see langword="true"/> when the normal summary-declaration analysis
        /// would inspect an executable method or local-function body;
        /// otherwise <see langword="false"/>.
        /// </returns>
        internal static bool HasAnalyzableSummaryInvocationBody(
            IMethodSymbol methodSymbol,
            ExceptionFlowSemanticEnvironment semanticContext)
        {
            foreach (SyntaxReference syntaxReference
                     in methodSymbol.DeclaringSyntaxReferences)
            {
                SyntaxNode declarationNode = syntaxReference.GetSyntax();

                if (!semanticContext.TryGetSemanticModel(
                        declarationNode.SyntaxTree,
                        out SemanticModel semanticModel)
                    || semanticModel == null)
                {
                    continue;
                }

                if (TryGetSummaryInvocationBody(
                        declarationNode,
                        out SyntaxNode? body)
                    && body != null)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Gets the executable syntax body used by summary analysis for an
        /// explicitly invocable method or local function.
        /// </summary>
        /// <param name="declarationNode">The callable declaration syntax.</param>
        /// <param name="body">The executable body when one is available.</param>
        /// <returns>
        /// <see langword="true"/> when an executable body was found;
        /// otherwise <see langword="false"/>.
        /// </returns>
        internal static bool TryGetSummaryInvocationBody(
            SyntaxNode declarationNode,
            out SyntaxNode? body)
        {
            if (declarationNode is MethodDeclarationSyntax method)
            {
                return SyntaxUtils.TryGetMemberBody(method, out body);
            }

            if (declarationNode is LocalFunctionStatementSyntax localFunction)
            {
                body = localFunction.Body ??
                       (SyntaxNode?)localFunction.ExpressionBody?.Expression;

                return body != null;
            }

            body = null;
            return false;
        }
    }
}
