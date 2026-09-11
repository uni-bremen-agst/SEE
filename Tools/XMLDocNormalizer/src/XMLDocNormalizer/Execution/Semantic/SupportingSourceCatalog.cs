using Microsoft.CodeAnalysis;

namespace XMLDocNormalizer.Execution.Semantic
{
    /// <summary>
    /// Stores source-backed external dependency compilations available to one
    /// semantic-analysis context.
    /// </summary>
    internal sealed class SupportingSourceCatalog
    {
        /// <summary>
        /// Stores supporting scopes by compilation object identity.
        /// </summary>
        private readonly Dictionary<Compilation, SemanticCompilationScope> scopesByCompilation =
            new(ReferenceEqualityComparer.Instance);

        /// <summary>
        /// Maps exact supporting assembly identities to their semantic scopes.
        /// </summary>
        private readonly Dictionary<AssemblyIdentity, SemanticCompilationScope> scopesByAssemblyIdentity =
            new();

        /// <summary>
        /// Maps supporting syntax trees to their semantic compilation scopes.
        /// </summary>
        private readonly Dictionary<SyntaxTree, SemanticCompilationScope> scopesBySyntaxTree =
            new(ReferenceEqualityComparer.Instance);

        /// <summary>
        /// Stores supporting scopes in registration order.
        /// </summary>
        private readonly List<SemanticCompilationScope> scopes = new();

        /// <summary>
        /// Gets the monotonically increasing catalog version.
        /// </summary>
        /// <value>
        /// The number of distinct compilation-object registrations performed.
        /// </value>
        public long Version { get; private set; }

        /// <summary>
        /// Registers one supporting source compilation or returns its existing scope.
        /// </summary>
        /// <param name="compilation">The supporting source compilation.</param>
        /// <returns>The unique supporting scope for the compilation object.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="compilation"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the exact assembly identity or a syntax tree in
        /// <paramref name="compilation"/> already belongs to another
        /// supporting source compilation.
        /// </exception>
        public SemanticCompilationScope Register(Compilation compilation)
        {
            ArgumentNullException.ThrowIfNull(compilation);

            if (scopesByCompilation.TryGetValue(
                    compilation,
                    out SemanticCompilationScope? existingScope))
            {
                return existingScope;
            }

            AssemblyIdentity assemblyIdentity = compilation.Assembly.Identity;

            if (scopesByAssemblyIdentity.ContainsKey(assemblyIdentity))
            {
                throw new InvalidOperationException(
                    "An exact assembly identity cannot belong to multiple supporting source compilations.");
            }

            foreach (SyntaxTree syntaxTree in compilation.SyntaxTrees)
            {
                if (scopesBySyntaxTree.ContainsKey(syntaxTree))
                {
                    throw new InvalidOperationException(
                        "A syntax tree cannot belong to multiple supporting source compilations.");
                }
            }

            SemanticCompilationScope scope =
                SemanticCompilationScope.CreateSupportingSourceDependency(compilation);

            scopesByCompilation.Add(compilation, scope);
            scopesByAssemblyIdentity.Add(assemblyIdentity, scope);
            scopes.Add(scope);

            foreach (SyntaxTree syntaxTree in compilation.SyntaxTrees)
            {
                scopesBySyntaxTree.Add(syntaxTree, scope);
            }

            Version++;
            return scope;
        }

        /// <summary>
        /// Gets a snapshot of all registered supporting scopes.
        /// </summary>
        /// <returns>The scopes in deterministic registration order.</returns>
        public IReadOnlyList<SemanticCompilationScope> GetScopes()
        {
            return scopes.ToArray();
        }

        /// <summary>
        /// Tries to locate the supporting scope that owns a syntax tree.
        /// </summary>
        /// <param name="tree">The supporting syntax tree.</param>
        /// <param name="scope">The owning supporting scope when found.</param>
        /// <returns>
        /// <see langword="true"/> when the tree is registered; otherwise
        /// <see langword="false"/>.
        /// </returns>
        public bool TryGetScope(SyntaxTree tree, out SemanticCompilationScope scope)
        {
            if (scopesBySyntaxTree.TryGetValue(
                    tree,
                    out SemanticCompilationScope? registeredScope))
            {
                scope = registeredScope;
                return true;
            }

            scope = null!;
            return false;
        }

        /// <summary>
        /// Tries to locate a supporting scope by its exact assembly identity.
        /// </summary>
        /// <param name="assemblyIdentity">The exact supporting assembly identity.</param>
        /// <param name="scope">The matching supporting scope when found.</param>
        /// <returns>
        /// <see langword="true"/> when the identity is registered; otherwise
        /// <see langword="false"/>.
        /// </returns>
        public bool TryGetScope(
            AssemblyIdentity assemblyIdentity,
            out SemanticCompilationScope scope)
        {
            if (scopesByAssemblyIdentity.TryGetValue(
                    assemblyIdentity,
                    out SemanticCompilationScope? registeredScope))
            {
                scope = registeredScope;
                return true;
            }

            scope = null!;
            return false;
        }
    }
}
