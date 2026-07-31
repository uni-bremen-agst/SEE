using Microsoft.CodeAnalysis;

namespace XMLDocNormalizer.Execution.Semantic
{
    /// <summary>
    /// Provides semantic access to the reporting project set and its semantic analysis scope.
    /// </summary>
    internal sealed class ProjectClosureSemanticContext
    {
        /// <summary>
        /// The projects for which findings may be reported.
        /// </summary>
        private readonly HashSet<ProjectId> reportingProjectIds;

        /// <summary>
        /// The projects that may be used for semantic or transitive analysis.
        /// </summary>
        private readonly HashSet<ProjectId> analysisProjectIds;

        /// <summary>
        /// The available compilations keyed by project id.
        /// </summary>
        private readonly Dictionary<ProjectId, Compilation> compilations;

        /// <summary>
        /// Maps each syntax tree in the analysis scope to its owning project.
        /// </summary>
        private readonly Dictionary<SyntaxTree, ProjectId> syntaxTreeToProjectId;

        /// <summary>
        /// Caches semantic models per syntax tree to avoid repeated lookup.
        /// </summary>
        private readonly Dictionary<SyntaxTree, SemanticModel> semanticModelCache = new();

        /// <summary>
        /// Caches the compilations and recursively collected source types that
        /// participate in semantic analysis.
        /// </summary>
        private IReadOnlyList<ProjectClosureCompilationScope>? analysisCompilationScopes;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProjectClosureSemanticContext"/> class.
        /// </summary>
        /// <param name="reportingProjectIds">The projects for which findings may be reported.</param>
        /// <param name="analysisProjectIds">The projects that may be used for semantic analysis.</param>
        /// <param name="compilations">The compilations available in the analysis scope.</param>
        /// <param name="syntaxTreeToProjectId">Maps syntax trees to their owning projects.</param>
        public ProjectClosureSemanticContext(
            HashSet<ProjectId> reportingProjectIds,
            HashSet<ProjectId> analysisProjectIds,
            Dictionary<ProjectId, Compilation> compilations,
            Dictionary<SyntaxTree, ProjectId> syntaxTreeToProjectId)
        {
            this.reportingProjectIds = reportingProjectIds;
            this.analysisProjectIds = analysisProjectIds;
            this.compilations = compilations;
            this.syntaxTreeToProjectId = syntaxTreeToProjectId;
        }

        /// <summary>
        /// Determines whether the given syntax tree belongs to the reporting scope.
        /// </summary>
        /// <param name="tree">The syntax tree to inspect.</param>
        /// <returns>
        /// <see langword="true"/> if findings may be reported for the tree; otherwise <see langword="false"/>.
        /// </returns>
        public bool IsInReportingScope(SyntaxTree tree)
        {
            return TryGetOwningProjectId(tree, out ProjectId projectId)
                && reportingProjectIds.Contains(projectId);
        }

        /// <summary>
        /// Determines whether the given syntax tree belongs to the semantic analysis scope.
        /// </summary>
        /// <param name="tree">The syntax tree to inspect.</param>
        /// <returns>
        /// <see langword="true"/> if the tree may be used for semantic analysis; otherwise <see langword="false"/>.
        /// </returns>
        public bool IsInAnalysisScope(SyntaxTree tree)
        {
            return TryGetOwningProjectId(tree, out ProjectId projectId)
                && analysisProjectIds.Contains(projectId);
        }

        /// <summary>
        /// Determines whether the specified type symbol is declared in the reporting scope.
        /// </summary>
        /// <param name="typeSymbol">The type symbol to inspect.</param>
        /// <returns>
        /// <see langword="true"/> if the type is declared in source code belonging to a reporting project;
        /// otherwise <see langword="false"/>.
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
        /// Gets the analysis compilations together with all named source types
        /// declared by their assemblies.
        /// </summary>
        /// <returns>
        /// The cached compilation scopes in deterministic assembly-name order.
        /// </returns>
        public IReadOnlyList<ProjectClosureCompilationScope>
            GetAnalysisCompilationScopes()
        {
            if (analysisCompilationScopes != null)
            {
                return analysisCompilationScopes;
            }

            List<ProjectClosureCompilationScope> scopes =
                new();

            foreach (KeyValuePair<ProjectId, Compilation> pair
                     in compilations
                         .Where(
                             pair =>
                                 analysisProjectIds.Contains(
                                     pair.Key))
                         .OrderBy(
                             static pair =>
                                 pair.Value.AssemblyName ??
                                 string.Empty,
                             StringComparer.Ordinal))
            {
                List<INamedTypeSymbol> sourceTypes =
                    new();

                CollectSourceTypes(
                    pair.Value.Assembly.GlobalNamespace,
                    sourceTypes);

                sourceTypes.Sort(
                    static (left, right) =>
                        StringComparer.Ordinal.Compare(
                            left.ToDisplayString(
                                SymbolDisplayFormat
                                    .FullyQualifiedFormat),
                            right.ToDisplayString(
                                SymbolDisplayFormat
                                    .FullyQualifiedFormat)));

                scopes.Add(
                    new ProjectClosureCompilationScope(
                        pair.Value,
                        sourceTypes));
            }

            analysisCompilationScopes = scopes;
            return analysisCompilationScopes;
        }

        /// <summary>
        /// Tries to resolve the project that owns the specified syntax tree.
        /// </summary>
        /// <param name="tree">The syntax tree to inspect.</param>
        /// <param name="projectId">The owning project id if found.</param>
        /// <returns>
        /// <see langword="true"/> if the owner could be determined; otherwise <see langword="false"/>.
        /// </returns>
        public bool TryGetOwningProjectId(SyntaxTree tree, out ProjectId projectId)
        {
            if (syntaxTreeToProjectId.TryGetValue(tree, out ProjectId? resolvedProjectId) &&
                resolvedProjectId != null)
            {
                projectId = resolvedProjectId;
                return true;
            }

            projectId = null!;
            return false;
        }

        /// <summary>
        /// Tries to get the semantic model for the specified syntax tree.
        /// </summary>
        /// <param name="tree">The syntax tree whose semantic model should be returned.</param>
        /// <param name="semanticModel">The semantic model if available.</param>
        /// <returns>
        /// <see langword="true"/> if a semantic model could be provided; otherwise <see langword="false"/>.
        /// </returns>
        public bool TryGetSemanticModel(SyntaxTree tree, out SemanticModel semanticModel)
        {
            if (semanticModelCache.TryGetValue(tree, out SemanticModel? cached) &&
                cached != null)
            {
                semanticModel = cached;
                return true;
            }

            if (!TryGetOwningProjectId(tree, out ProjectId projectId))
            {
                semanticModel = null!;
                return false;
            }

            if (!compilations.TryGetValue(projectId, out Compilation? compilation) ||
                compilation == null)
            {
                semanticModel = null!;
                return false;
            }

            semanticModel = compilation.GetSemanticModel(tree);
            semanticModelCache[tree] = semanticModel;
            return true;
        }

        /// <summary>
        /// Creates a semantic context for a single compilation and a single reporting tree.
        /// This is primarily intended for isolated tests and local semantic analysis.
        /// </summary>
        /// <param name="reportingTree">The syntax tree that should be treated as reporting scope.</param>
        /// <param name="compilation">The compilation containing the available syntax trees.</param>
        /// <returns>A semantic context limited to the given compilation.</returns>
        public static ProjectClosureSemanticContext CreateSingleCompilationContext(
            SyntaxTree reportingTree,
            Compilation compilation)
        {
            ProjectId pseudoProjectId = ProjectId.CreateNewId();

            HashSet<ProjectId> reportingProjectIds = new() { pseudoProjectId };
            HashSet<ProjectId> analysisProjectIds = new() { pseudoProjectId };
            Dictionary<ProjectId, Compilation> compilations = new()
            {
                [pseudoProjectId] = compilation
            };

            Dictionary<SyntaxTree, ProjectId> syntaxTreeToProjectId = new();
            foreach (SyntaxTree syntaxTree in compilation.SyntaxTrees)
            {
                syntaxTreeToProjectId[syntaxTree] = pseudoProjectId;
            }

            if (!syntaxTreeToProjectId.ContainsKey(reportingTree))
            {
                syntaxTreeToProjectId[reportingTree] = pseudoProjectId;
            }

            return new ProjectClosureSemanticContext(
                reportingProjectIds,
                analysisProjectIds,
                compilations,
                syntaxTreeToProjectId);
        }

        /// <summary>
        /// Recursively collects source types from one namespace.
        /// </summary>
        /// <param name="namespaceSymbol">
        /// The namespace to traverse.
        /// </param>
        /// <param name="sourceTypes">
        /// The destination list receiving source types.
        /// </param>
        private static void CollectSourceTypes(
            INamespaceSymbol namespaceSymbol,
            List<INamedTypeSymbol> sourceTypes)
        {
            foreach (INamedTypeSymbol typeSymbol
                     in namespaceSymbol.GetTypeMembers())
            {
                CollectSourceType(
                    typeSymbol,
                    sourceTypes);
            }

            foreach (INamespaceSymbol nestedNamespace
                     in namespaceSymbol.GetNamespaceMembers())
            {
                CollectSourceTypes(
                    nestedNamespace,
                    sourceTypes);
            }
        }

        /// <summary>
        /// Adds one source type and recursively adds its nested source types.
        /// </summary>
        /// <param name="typeSymbol">
        /// The type to add.
        /// </param>
        /// <param name="sourceTypes">
        /// The destination list receiving source types.
        /// </param>
        private static void CollectSourceType(
            INamedTypeSymbol typeSymbol,
            List<INamedTypeSymbol> sourceTypes)
        {
            if (!typeSymbol.DeclaringSyntaxReferences.IsDefaultOrEmpty)
            {
                sourceTypes.Add(
                    typeSymbol);
            }

            foreach (INamedTypeSymbol nestedType
                     in typeSymbol.GetTypeMembers())
            {
                CollectSourceType(
                    nestedType,
                    sourceTypes);
            }
        }
    }
}
