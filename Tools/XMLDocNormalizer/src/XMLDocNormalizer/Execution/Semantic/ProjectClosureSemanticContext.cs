using Microsoft.CodeAnalysis;
using XMLDocNormalizer.Utils;

namespace XMLDocNormalizer.Execution.Semantic
{
    /// <summary>
    /// Provides semantic access to reporting targets, referenced projects,
    /// and registered supporting source dependencies.
    /// </summary>
    internal sealed class ProjectClosureSemanticContext
    {
        /// <summary>
        /// Stores project-backed semantic scopes in deterministic assembly order.
        /// </summary>
        private readonly IReadOnlyList<SemanticCompilationScope> projectCompilationScopes;

        /// <summary>
        /// Maps project syntax trees to their semantic compilation scopes.
        /// </summary>
        private readonly Dictionary<SyntaxTree, SemanticCompilationScope> projectScopesBySyntaxTree;

        /// <summary>
        /// Gets the supporting source catalog owned by this context.
        /// </summary>
        /// <value>The context-local supporting source catalog.</value>
        private SupportingSourceCatalog SupportingSources { get; } = new();

        /// <summary>
        /// Caches semantic models per syntax tree to avoid repeated lookup.
        /// </summary>
        private readonly Dictionary<SyntaxTree, SemanticModel> semanticModelCache =
            new(ReferenceEqualityComparer.Instance);

        /// <summary>
        /// Caches all project and supporting compilation scopes.
        /// </summary>
        private IReadOnlyList<SemanticCompilationScope>? analysisCompilationScopes;

        /// <summary>
        /// Stores the supporting-source catalog version represented by the
        /// combined scope cache.
        /// </summary>
        private long analysisCompilationScopesCatalogVersion = -1;

        /// <summary>
        /// Caches whether the reporting scope declares at least one exception type.
        /// </summary>
        private bool? hasDeclaredExceptionTypesInReportingScope;

        /// <summary>
        /// Initializes a project-closure semantic context.
        /// </summary>
        /// <param name="projectCompilationScopes">
        /// The project-backed scopes included by the configured analysis mode.
        /// </param>
        /// <param name="projectScopesBySyntaxTree">
        /// The project-backed syntax-tree ownership map.
        /// </param>
        public ProjectClosureSemanticContext(
            IReadOnlyCollection<SemanticCompilationScope> projectCompilationScopes,
            Dictionary<SyntaxTree, SemanticCompilationScope> projectScopesBySyntaxTree)
        {
            this.projectCompilationScopes = projectCompilationScopes
                .OrderBy(
                    static scope => scope.Compilation.AssemblyName ?? string.Empty,
                    StringComparer.Ordinal)
                .ToArray();
            this.projectScopesBySyntaxTree = projectScopesBySyntaxTree;
        }

        /// <summary>
        /// Registers a supporting source compilation with this semantic context.
        /// </summary>
        /// <param name="compilation">The supporting source compilation.</param>
        /// <returns>The unique supporting scope for the compilation object.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="compilation"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when a syntax tree in <paramref name="compilation"/> already
        /// belongs to a project or another supporting source compilation.
        /// </exception>
        public SemanticCompilationScope RegisterSupportingSource(Compilation compilation)
        {
            ArgumentNullException.ThrowIfNull(compilation);

            foreach (SyntaxTree syntaxTree in compilation.SyntaxTrees)
            {
                if (projectScopesBySyntaxTree.ContainsKey(syntaxTree))
                {
                    throw new InvalidOperationException(
                        "A supporting source syntax tree already belongs to a project scope.");
                }
            }

            return SupportingSources.Register(compilation);
        }

        /// <summary>
        /// Determines whether the given syntax tree belongs to a reporting target.
        /// </summary>
        /// <param name="tree">The syntax tree to inspect.</param>
        /// <returns>
        /// <see langword="true"/> if findings may be reported for the tree;
        /// otherwise <see langword="false"/>.
        /// </returns>
        public bool IsInReportingScope(SyntaxTree tree)
        {
            return projectScopesBySyntaxTree.TryGetValue(
                       tree,
                       out SemanticCompilationScope? scope)
                && scope.Kind == SemanticCompilationScopeKind.AnalysisTarget;
        }

        /// <summary>
        /// Determines whether the given syntax tree belongs to the semantic
        /// analysis scope.
        /// </summary>
        /// <param name="tree">The syntax tree to inspect.</param>
        /// <returns>
        /// <see langword="true"/> if the tree may be used for semantic analysis;
        /// otherwise <see langword="false"/>.
        /// </returns>
        public bool IsInAnalysisScope(SyntaxTree tree)
        {
            return TryGetCompilationScope(tree, out _);
        }

        /// <summary>
        /// Determines whether the specified type symbol is declared in a
        /// reporting target.
        /// </summary>
        /// <param name="typeSymbol">The type symbol to inspect.</param>
        /// <returns>
        /// <see langword="true"/> if the type is declared in source belonging
        /// to an analysis target; otherwise <see langword="false"/>.
        /// </returns>
        public bool IsDeclaredInReportingScope(INamedTypeSymbol typeSymbol)
        {
            foreach (SyntaxReference syntaxReference in typeSymbol.DeclaringSyntaxReferences)
            {
                if (IsInReportingScope(syntaxReference.SyntaxTree))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Determines whether the reporting scope declares at least one
        /// exception type.
        /// </summary>
        /// <returns>
        /// <see langword="true"/> if a named source type declared by an
        /// analysis target derives from <see cref="Exception"/>; otherwise
        /// <see langword="false"/>.
        /// </returns>
        public bool HasDeclaredExceptionTypesInReportingScope()
        {
            if (hasDeclaredExceptionTypesInReportingScope.HasValue)
            {
                return hasDeclaredExceptionTypesInReportingScope.Value;
            }

            foreach (SemanticCompilationScope scope in projectCompilationScopes)
            {
                if (scope.Kind != SemanticCompilationScopeKind.AnalysisTarget)
                {
                    continue;
                }

                INamedTypeSymbol? exceptionBase =
                    scope.Compilation.GetTypeByMetadataName("System.Exception");

                if (exceptionBase == null)
                {
                    continue;
                }

                foreach (INamedTypeSymbol sourceType in scope.SourceTypes)
                {
                    if (!sourceType.InheritsFromOrEquals(exceptionBase))
                    {
                        continue;
                    }

                    hasDeclaredExceptionTypesInReportingScope = true;
                    return true;
                }
            }

            hasDeclaredExceptionTypesInReportingScope = false;
            return false;
        }

        /// <summary>
        /// Gets all project and supporting compilation scopes participating in
        /// semantic analysis.
        /// </summary>
        /// <returns>
        /// The cached scopes in deterministic assembly-name and role order.
        /// </returns>
        public IReadOnlyList<SemanticCompilationScope> GetAnalysisCompilationScopes()
        {
            long catalogVersion = SupportingSources.Version;

            if (analysisCompilationScopes != null
                && analysisCompilationScopesCatalogVersion == catalogVersion)
            {
                return analysisCompilationScopes;
            }

            analysisCompilationScopes = projectCompilationScopes
                .Concat(SupportingSources.GetScopes())
                .OrderBy(
                    static scope => scope.Compilation.AssemblyName ?? string.Empty,
                    StringComparer.Ordinal)
                .ThenBy(static scope => scope.Kind)
                .ToArray();
            analysisCompilationScopesCatalogVersion = catalogVersion;
            return analysisCompilationScopes;
        }

        /// <summary>
        /// Tries to resolve the Workspace project that owns a syntax tree.
        /// </summary>
        /// <param name="tree">The syntax tree to inspect.</param>
        /// <param name="projectId">The owning real project identity if found.</param>
        /// <returns>
        /// <see langword="true"/> for project-backed source; otherwise
        /// <see langword="false"/>, including for supporting source.
        /// </returns>
        public bool TryGetOwningProjectId(SyntaxTree tree, out ProjectId projectId)
        {
            if (projectScopesBySyntaxTree.TryGetValue(
                    tree,
                    out SemanticCompilationScope? scope)
                && scope.ProjectId != null)
            {
                projectId = scope.ProjectId;
                return true;
            }

            projectId = null!;
            return false;
        }

        /// <summary>
        /// Tries to get the semantic model for the specified syntax tree.
        /// </summary>
        /// <param name="tree">The syntax tree whose model should be returned.</param>
        /// <param name="semanticModel">The semantic model if available.</param>
        /// <returns>
        /// <see langword="true"/> if a semantic model could be provided;
        /// otherwise <see langword="false"/>.
        /// </returns>
        public bool TryGetSemanticModel(SyntaxTree tree, out SemanticModel semanticModel)
        {
            if (semanticModelCache.TryGetValue(tree, out SemanticModel? cached))
            {
                semanticModel = cached;
                return true;
            }

            if (!TryGetCompilationScope(
                    tree,
                    out SemanticCompilationScope? scope))
            {
                semanticModel = null!;
                return false;
            }

            semanticModel = scope.Compilation.GetSemanticModel(tree);
            semanticModelCache[tree] = semanticModel;
            return true;
        }

        /// <summary>
        /// Creates a semantic context for a single compilation and reporting tree.
        /// This is primarily intended for isolated tests and local analysis.
        /// </summary>
        /// <param name="reportingTree">The tree treated as reporting source.</param>
        /// <param name="compilation">The compilation containing available trees.</param>
        /// <returns>A semantic context limited to the compilation.</returns>
        public static ProjectClosureSemanticContext CreateSingleCompilationContext(
            SyntaxTree reportingTree,
            Compilation compilation)
        {
            ProjectId pseudoProjectId = ProjectId.CreateNewId();
            SemanticCompilationScope scope =
                SemanticCompilationScope.CreateAnalysisTarget(
                    compilation,
                    pseudoProjectId);
            Dictionary<SyntaxTree, SemanticCompilationScope> scopesBySyntaxTree =
                new(ReferenceEqualityComparer.Instance);

            foreach (SyntaxTree syntaxTree in compilation.SyntaxTrees)
            {
                scopesBySyntaxTree.TryAdd(syntaxTree, scope);
            }

            scopesBySyntaxTree.TryAdd(reportingTree, scope);

            return new ProjectClosureSemanticContext(
                new[] { scope },
                scopesBySyntaxTree);
        }

        /// <summary>
        /// Tries to locate the semantic compilation scope that owns a syntax tree.
        /// </summary>
        /// <param name="tree">The syntax tree to resolve.</param>
        /// <param name="scope">The owning scope when found.</param>
        /// <returns>
        /// <see langword="true"/> when an owning project or supporting scope
        /// exists; otherwise <see langword="false"/>.
        /// </returns>
        private bool TryGetCompilationScope(
            SyntaxTree tree,
            out SemanticCompilationScope scope)
        {
            if (projectScopesBySyntaxTree.TryGetValue(
                    tree,
                    out SemanticCompilationScope? projectScope))
            {
                scope = projectScope;
                return true;
            }

            return SupportingSources.TryGetScope(tree, out scope);
        }
    }
}
