using Microsoft.CodeAnalysis;

namespace XMLDocNormalizer.Checks.Infrastructure.Exception.Flow
{
    /// <summary>Owns independently reconstructed source projects in an isolated Worker.</summary>
    internal sealed partial class ExceptionFlowSemanticEnvironment
    {
        private readonly ExceptionFlowSemanticScope scope;
        private readonly Dictionary<SyntaxTree, SemanticModel> models = new(ReferenceEqualityComparer.Instance);
        private readonly IReadOnlyList<ExceptionFlowSemanticScope> scopes;

        /// <summary>Owns a complete single-compilation source set and stable models per exact tree.</summary>
        internal ExceptionFlowSemanticEnvironment(Compilation compilation)
            : this(compilation, Array.Empty<Compilation>()) { }

        /// <summary>Registers only the explicit graph's complete projects; never widens a local profile.</summary>
        internal ExceptionFlowSemanticEnvironment(Compilation compilation, IReadOnlyList<Compilation> supporting)
        {
            ArgumentNullException.ThrowIfNull(compilation);
            SyntaxTree[] trees = compilation.SyntaxTrees.ToArray();
            if (trees.Length == 0)
            {
                throw new ArgumentException("Historical bounded host requires source-owned trees.", nameof(compilation));
            }

            scope = new ExceptionFlowSemanticScope(compilation);
            var owned = new[] { scope }.Concat(supporting.Select(project => new ExceptionFlowSemanticScope(project)))
                .OrderBy(item => item.Compilation.AssemblyName, StringComparer.Ordinal).ToArray();
            foreach (var item in owned)
            {
                if (!item.Compilation.SyntaxTrees.Any()) { throw new ArgumentException("Project sources are incomplete.", nameof(supporting)); }
                foreach (var tree in item.Compilation.SyntaxTrees) { models.Add(tree, item.Compilation.GetSemanticModel(tree)); }
            }
            scopes = Array.AsReadOnly(owned);
        }

        /// <summary>Only the exact owned tree can retrieve the request's stable model.</summary>
        internal bool TryGetSemanticModel(SyntaxTree tree, out SemanticModel semanticModel)
        {
            bool found = models.TryGetValue(tree, out semanticModel!);
            ObserveAdmissionModel(tree, found ? semanticModel : null);
            return found;
        }

        /// <summary>Returns the request-owned deterministic project scopes.</summary>
        internal IReadOnlyList<ExceptionFlowSemanticScope> GetAnalysisScopes() => scopes;

        /// <summary>Returns genuine local source, never a metadata-name approximation.</summary>
        internal bool TryResolveSupportingSourceMethod(
            IMethodSymbol methodSymbol, out ExceptionFlowSupportingSourceMethod resolution)
        {
            ExcludeAdmissionReuse();
            var destination = scopes.SingleOrDefault(item => item.Compilation.Assembly.Identity.Equals(methodSymbol.ContainingAssembly.Identity));
            var resolved = destination == null ? null
                : SymbolEqualityComparer.Default.Equals(methodSymbol.ContainingAssembly, destination.Compilation.Assembly)
                    ? methodSymbol : scopes.Count > 1
                        ? ExceptionFlowCrossCompilationResolver.ResolveMethod(methodSymbol, destination.Compilation) : null;
            if (resolved != null && destination != null
                && SymbolEqualityComparer.Default.Equals(resolved.ContainingAssembly, destination.Compilation.Assembly)
                && ((!resolved.DeclaringSyntaxReferences.IsDefaultOrEmpty
                        && resolved.DeclaringSyntaxReferences.All(reference => models.ContainsKey(reference.SyntaxTree)))
                    || (scopes.Count > 1 && resolved.IsImplicitlyDeclared && !resolved.ContainingType.DeclaringSyntaxReferences.IsDefaultOrEmpty
                        && resolved.ContainingType.DeclaringSyntaxReferences.All(reference => models.ContainsKey(reference.SyntaxTree)))))
            {
                resolution = new ExceptionFlowSupportingSourceMethod(resolved, destination);
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
            ExcludeAdmissionReuse();
            if ((ReferenceEquals(bindingCompilation, scope.Compilation)
                    || scopes.Any(item => ReferenceEquals(item.Compilation, bindingCompilation)))
                && (SymbolEqualityComparer.Default.Equals(methodSymbol.ContainingAssembly, bindingCompilation.Assembly)
                    || IsBoundReference(bindingCompilation, methodSymbol.ContainingAssembly)))
            {
                return TryResolveSupportingSourceMethod(methodSymbol, out resolution);
            }

            resolution = null!;
            return false;
        }

        /// <summary>Maps an actually bound exact reference to its request-owned source project.</summary>
        internal bool TryGetExternalSupportingSourceScope(
            Compilation bindingCompilation, IAssemblySymbol assemblySymbol,
            out ExceptionFlowSemanticScope scope)
        {
            ExcludeAdmissionReuse();
            scope = null!;
            if (!scopes.Any(item => ReferenceEquals(item.Compilation, bindingCompilation))
                || !IsBoundReference(bindingCompilation, assemblySymbol)) { return false; }
            scope = scopes.SingleOrDefault(item => item.Compilation.Assembly.Identity.Equals(assemblySymbol.Identity))!;
            return scope != null;
        }

        private static bool IsBoundReference(Compilation compilation, IAssemblySymbol assembly)
            => compilation.References.Any(reference => SymbolEqualityComparer.Default.Equals(
                compilation.GetAssemblyOrModuleSymbol(reference), assembly));
    }
}
