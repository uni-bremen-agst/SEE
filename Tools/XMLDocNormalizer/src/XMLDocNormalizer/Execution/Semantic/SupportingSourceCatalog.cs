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
        /// Maps exact original external binary identities to their registered
        /// source-backed scopes.
        /// </summary>
        private readonly Dictionary<
            ExternalAssemblyReferenceDescriptor,
            SemanticCompilationScope> externalScopesByBinaryIdentity = new();

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
        /// Stores supporting scopes that the current analyzer traversal is
        /// already allowed to enumerate.
        /// </summary>
        private readonly List<SemanticCompilationScope> analysisScopes = new();

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
            analysisScopes.Add(scope);

            foreach (SyntaxTree syntaxTree in compilation.SyntaxTrees)
            {
                scopesBySyntaxTree.Add(syntaxTree, scope);
            }

            Version++;
            return scope;
        }

        /// <summary>
        /// Tries to register a validated external supporting-source
        /// compilation without activating analyzer traversal for it.
        /// </summary>
        /// <param name="supportingSource">
        /// The validated external binary identity and exact P5K compilation.
        /// </param>
        /// <param name="scope">
        /// The registered supporting-source scope when successful.
        /// </param>
        /// <returns>
        /// <see langword="true"/> for a new registration or an idempotent
        /// registration of the same binary identity and compilation instance;
        /// otherwise <see langword="false"/>. Conflicts never replace an
        /// existing registration.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="supportingSource"/> is
        /// <see langword="null"/>.
        /// </exception>
        public bool TryRegisterExternal(
            ExternalSupportingSourceCompilation supportingSource,
            out SemanticCompilationScope scope)
        {
            ArgumentNullException.ThrowIfNull(supportingSource);

            ExternalAssemblyReferenceDescriptor binaryIdentity =
                supportingSource.TargetAssembly;
            Compilation compilation = supportingSource.Compilation;

            if (externalScopesByBinaryIdentity.TryGetValue(
                    binaryIdentity,
                    out SemanticCompilationScope? existingBinaryScope))
            {
                scope = existingBinaryScope;
                return ReferenceEquals(existingBinaryScope.Compilation, compilation);
            }

            if (scopesByCompilation.ContainsKey(compilation))
            {
                scope = null!;
                return false;
            }

            foreach (SyntaxTree syntaxTree in compilation.SyntaxTrees)
            {
                if (scopesBySyntaxTree.ContainsKey(syntaxTree))
                {
                    scope = null!;
                    return false;
                }
            }

            scope = SemanticCompilationScope.CreateSupportingSourceDependency(
                compilation);
            externalScopesByBinaryIdentity.Add(binaryIdentity, scope);
            scopesByCompilation.Add(compilation, scope);
            scopes.Add(scope);

            foreach (SyntaxTree syntaxTree in compilation.SyntaxTrees)
            {
                scopesBySyntaxTree.Add(syntaxTree, scope);
            }

            Version++;
            return true;
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
        /// Gets supporting scopes enabled for the current analyzer traversal.
        /// </summary>
        /// <returns>
        /// The legacy supporting scopes in deterministic registration order.
        /// External P6A registrations are excluded until their explicit
        /// callable-resolution integration is introduced.
        /// </returns>
        public IReadOnlyList<SemanticCompilationScope> GetAnalysisScopes()
        {
            return analysisScopes.ToArray();
        }

        /// <summary>
        /// Tries to locate the supporting scope for a compilation object.
        /// </summary>
        /// <param name="compilation">The supporting compilation.</param>
        /// <param name="scope">The registered supporting scope when found.</param>
        /// <returns>
        /// <see langword="true"/> when the exact compilation object is
        /// registered; otherwise <see langword="false"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="compilation"/> is
        /// <see langword="null"/>.
        /// </exception>
        public bool TryGetScope(Compilation compilation, out SemanticCompilationScope scope)
        {
            ArgumentNullException.ThrowIfNull(compilation);

            if (scopesByCompilation.TryGetValue(
                    compilation,
                    out SemanticCompilationScope? registeredScope))
            {
                scope = registeredScope;
                return true;
            }

            scope = null!;
            return false;
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

        /// <summary>
        /// Tries to locate an external supporting scope by the exact original
        /// binary identity.
        /// </summary>
        /// <param name="binaryIdentity">
        /// The full P3 identity. Its file path does not participate in equality.
        /// </param>
        /// <param name="scope">The exact registered scope when found.</param>
        /// <returns>
        /// <see langword="true"/> when an equal external binary identity is
        /// registered; otherwise <see langword="false"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="binaryIdentity"/> is
        /// <see langword="null"/>.
        /// </exception>
        public bool TryGetExternalScope(
            ExternalAssemblyReferenceDescriptor binaryIdentity,
            out SemanticCompilationScope scope)
        {
            ArgumentNullException.ThrowIfNull(binaryIdentity);

            if (externalScopesByBinaryIdentity.TryGetValue(
                    binaryIdentity,
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
