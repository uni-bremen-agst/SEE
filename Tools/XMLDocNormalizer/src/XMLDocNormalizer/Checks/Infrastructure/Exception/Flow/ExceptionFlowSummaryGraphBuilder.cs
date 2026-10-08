using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using XMLDocNormalizer.Utils;

namespace XMLDocNormalizer.Checks.Infrastructure.Exception.Flow
{
    /// <summary>
    /// Registers context-sensitive summary roots for one semantic environment.
    /// </summary>
    /// <remarks>
    /// One builder belongs to one sequential analysis session. It owns no
    /// graph state itself and is intentionally not thread-safe.
    /// </remarks>
    internal sealed class ExceptionFlowSummaryGraphBuilder
    {
        /// <summary>
        /// The semantic environment used to resolve every graph node.
        /// </summary>
        private readonly ExceptionFlowSemanticEnvironment semanticContext;

        /// <summary>
        /// Initializes a builder for one project-closure semantic environment.
        /// </summary>
        /// <param name="semanticContext">The semantic environment.</param>
        internal ExceptionFlowSummaryGraphBuilder(
            ExceptionFlowSemanticEnvironment semanticContext)
        {
            this.semanticContext =
                semanticContext;
        }

        /// <summary>
        /// Resolves and registers one source-level member as a summary-graph
        /// root.
        /// </summary>
        /// <param name="member">
        /// The member to register.
        /// </param>
        /// <param name="graph">
        /// The graph receiving the root summary.
        /// </param>
        /// <param name="rootKey">
        /// The registered context-sensitive root key.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the member symbol and semantic model were
        /// resolved; otherwise <see langword="false"/>.
        /// </returns>
        internal bool TryRegisterSummaryGraphRoot(
            MemberDeclarationSyntax member,
            ExceptionFlowSummaryGraph graph,
            out ExceptionFlowCallableKey? rootKey)
        {
            rootKey =
                null;

            if (!semanticContext.TryGetSemanticModel(
                    member.SyntaxTree,
                    out SemanticModel semanticModel) ||
                semanticModel == null)
            {
                return false;
            }

            if (semanticModel.GetDeclaredSymbol(
                    member)
                is not ISymbol rootSymbol)
            {
                return false;
            }

            ExceptionFlowCallContext rootContext =
                new(rootSymbol);

            rootKey =
                new ExceptionFlowCallableKey(
                    rootSymbol,
                    rootContext);

            graph.GetOrAdd(
                rootKey,
                rootContext);

            return true;
        }
    }
}
