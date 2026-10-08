using Microsoft.CodeAnalysis;

namespace XMLDocNormalizer.Checks.Infrastructure.Exception.Flow
{
    /// <summary>
    /// Type-checks the already explicit A6F host seam. Never execute this probe.
    /// This is not a historical runtime host, supporting-source policy or B2 adapter.
    /// </summary>
    internal sealed partial class ExceptionFlowSemanticEnvironment
    {
        /// <summary>Rejects execution of the compile-only host.</summary>
        /// <param name="tree">The requested tree.</param>
        /// <param name="semanticModel">The requested model.</param>
        /// <returns>No runtime result; this compile-only member always throws.</returns>
        internal bool TryGetSemanticModel(SyntaxTree tree, out SemanticModel semanticModel)
            => throw new NotSupportedException("B1 compile-only host must never execute.");

        /// <summary>Rejects execution of the compile-only host.</summary>
        /// <returns>No runtime scopes; this compile-only member always throws.</returns>
        internal IReadOnlyList<ExceptionFlowSemanticScope> GetAnalysisScopes()
            => throw new NotSupportedException("B1 compile-only host must never execute.");

        /// <summary>Rejects execution of the compile-only host.</summary>
        /// <param name="methodSymbol">The requested method.</param>
        /// <param name="resolution">The requested supporting resolution.</param>
        /// <returns>No runtime result; this compile-only member always throws.</returns>
        internal bool TryResolveSupportingSourceMethod(
            IMethodSymbol methodSymbol, out ExceptionFlowSupportingSourceMethod resolution)
            => throw new NotSupportedException("B1 compile-only host must never execute.");

        /// <summary>Rejects execution of the compile-only host.</summary>
        /// <param name="methodSymbol">The requested method.</param>
        /// <param name="bindingCompilation">The owning compilation.</param>
        /// <param name="resolution">The requested supporting resolution.</param>
        /// <returns>No runtime result; this compile-only member always throws.</returns>
        internal bool TryResolveSupportingSourceMethod(
            IMethodSymbol methodSymbol, Compilation bindingCompilation,
            out ExceptionFlowSupportingSourceMethod resolution)
            => throw new NotSupportedException("B1 compile-only host must never execute.");

        /// <summary>Rejects execution of the compile-only host.</summary>
        /// <param name="bindingCompilation">The owning compilation.</param>
        /// <param name="assemblySymbol">The requested assembly.</param>
        /// <param name="scope">The requested supporting scope.</param>
        /// <returns>No runtime result; this compile-only member always throws.</returns>
        internal bool TryGetExternalSupportingSourceScope(
            Compilation bindingCompilation, IAssemblySymbol assemblySymbol,
            out ExceptionFlowSemanticScope scope)
            => throw new NotSupportedException("B1 compile-only host must never execute.");
    }
}
