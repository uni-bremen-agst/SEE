using Microsoft.CodeAnalysis;

namespace XMLDocNormalizer.Checks.Infrastructure.Exception.Flow
{
    /// <summary>
    /// Completes the compile-local host contract for the permanent build target.
    /// Contains no analysis policy. An isolated executable host belongs to the
    /// next runtime package; this assembly must not execute or enter Main.
    /// </summary>
    internal sealed partial class ExceptionFlowSemanticEnvironment
    {
        /// <summary>Prevents accidental construction of a non-executable host.</summary>
        internal ExceptionFlowSemanticEnvironment()
            => throw new NotSupportedException("Historical build target requires an isolated runtime host.");

        /// <summary>Rejects execution; no semantic model is substituted.</summary>
        internal bool TryGetSemanticModel(SyntaxTree tree, out SemanticModel semanticModel)
            => throw new NotSupportedException("Historical build target has no runtime host.");

        /// <summary>Rejects execution; no analysis scope is substituted.</summary>
        internal IReadOnlyList<ExceptionFlowSemanticScope> GetAnalysisScopes()
            => throw new NotSupportedException("Historical build target has no runtime host.");

        /// <summary>Rejects execution; no supporting-source resolution is substituted.</summary>
        internal bool TryResolveSupportingSourceMethod(
            IMethodSymbol methodSymbol, out ExceptionFlowSupportingSourceMethod resolution)
            => throw new NotSupportedException("Historical build target has no runtime host.");

        /// <summary>Rejects execution; no supporting-source resolution is substituted.</summary>
        internal bool TryResolveSupportingSourceMethod(
            IMethodSymbol methodSymbol, Compilation bindingCompilation,
            out ExceptionFlowSupportingSourceMethod resolution)
            => throw new NotSupportedException("Historical build target has no runtime host.");

        /// <summary>Rejects execution; no external scope is substituted.</summary>
        internal bool TryGetExternalSupportingSourceScope(
            Compilation bindingCompilation, IAssemblySymbol assemblySymbol,
            out ExceptionFlowSemanticScope scope)
            => throw new NotSupportedException("Historical build target has no runtime host.");
    }
}
