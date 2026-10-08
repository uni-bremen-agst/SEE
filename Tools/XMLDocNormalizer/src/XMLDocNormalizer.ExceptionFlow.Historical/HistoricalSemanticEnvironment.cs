using Microsoft.CodeAnalysis;

namespace XMLDocNormalizer.Checks.Infrastructure.Exception.Flow
{
    /// <summary>Owns one source compilation in an isolated Worker; no external source registry.</summary>
    internal sealed partial class ExceptionFlowSemanticEnvironment
    {
        private readonly ExceptionFlowSemanticScope scope;
        private readonly SemanticModel model;
        private readonly IReadOnlyList<ExceptionFlowSemanticScope> scopes;

        /// <summary>Owns exactly one tree and one stable semantic model for the request.</summary>
        internal ExceptionFlowSemanticEnvironment(Compilation compilation)
        {
            ArgumentNullException.ThrowIfNull(compilation);
            SyntaxTree[] trees = compilation.SyntaxTrees.ToArray();
            if (trees.Length != 1)
            {
                throw new ArgumentException("Historical bounded host requires exactly one source tree.", nameof(compilation));
            }

            scope = new ExceptionFlowSemanticScope(compilation);
            model = compilation.GetSemanticModel(trees[0]);
            scopes = Array.AsReadOnly(new[] { scope });
        }

        /// <summary>Only the exact owned tree can retrieve the request's stable model.</summary>
        internal bool TryGetSemanticModel(SyntaxTree tree, out SemanticModel semanticModel)
        {
            if (ReferenceEquals(tree, model.SyntaxTree))
            {
                semanticModel = model;
                return true;
            }

            semanticModel = null!;
            return false;
        }

        /// <summary>Returns only the request-owned deterministic single-compilation scope.</summary>
        internal IReadOnlyList<ExceptionFlowSemanticScope> GetAnalysisScopes() => scopes;

        /// <summary>Returns genuine local source, never a metadata-name approximation.</summary>
        internal bool TryResolveSupportingSourceMethod(
            IMethodSymbol methodSymbol, out ExceptionFlowSupportingSourceMethod resolution)
        {
            if (SymbolEqualityComparer.Default.Equals(methodSymbol.ContainingAssembly, scope.Compilation.Assembly)
                && !methodSymbol.DeclaringSyntaxReferences.IsDefaultOrEmpty
                && methodSymbol.DeclaringSyntaxReferences.All(reference => ReferenceEquals(reference.SyntaxTree, model.SyntaxTree)))
            {
                resolution = new ExceptionFlowSupportingSourceMethod(methodSymbol, scope);
                return true;
            }

            resolution = null!;
            return false;
        }

        /// <summary>Rejects foreign compilations even when their public assembly identities agree.</summary>
        internal bool TryResolveSupportingSourceMethod(
            IMethodSymbol methodSymbol, Compilation bindingCompilation,
            out ExceptionFlowSupportingSourceMethod resolution)
        {
            if (ReferenceEquals(bindingCompilation, scope.Compilation))
            {
                return TryResolveSupportingSourceMethod(methodSymbol, out resolution);
            }

            resolution = null!;
            return false;
        }

        /// <summary>No external supporting-source provenance is installed by this bounded host.</summary>
        internal bool TryGetExternalSupportingSourceScope(
            Compilation bindingCompilation, IAssemblySymbol assemblySymbol,
            out ExceptionFlowSemanticScope scope)
        {
            scope = null!;
            return false;
        }
    }
}
