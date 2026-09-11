using Microsoft.CodeAnalysis;

namespace XMLDocNormalizer.Execution.Semantic
{
    /// <summary>
    /// Identifies the semantic role of one source-backed compilation.
    /// </summary>
    internal enum SemanticCompilationScopeKind
    {
        /// <summary>
        /// A selected project whose documentation may produce findings.
        /// </summary>
        AnalysisTarget,

        /// <summary>
        /// A referenced Workspace project used only for semantic analysis.
        /// </summary>
        ReferencedProject,

        /// <summary>
        /// Source for a referenced binary dependency used only for semantic
        /// analysis.
        /// </summary>
        SupportingSourceDependency
    }

    /// <summary>
    /// Groups one source-backed semantic compilation with its scope role,
    /// optional Workspace project identity, and lazily collected source types.
    /// </summary>
    internal sealed class SemanticCompilationScope
    {
        /// <summary>
        /// Caches source-declared named types after their first access.
        /// </summary>
        private IReadOnlyList<INamedTypeSymbol>? sourceTypes;

        /// <summary>
        /// Initializes a semantic compilation scope.
        /// </summary>
        /// <param name="compilation">The represented compilation.</param>
        /// <param name="kind">The semantic scope role.</param>
        /// <param name="projectId">
        /// The Workspace project identity, or <see langword="null"/> for a
        /// supporting source dependency.
        /// </param>
        private SemanticCompilationScope(
            Compilation compilation,
            SemanticCompilationScopeKind kind,
            ProjectId? projectId)
        {
            Compilation = compilation;
            Kind = kind;
            ProjectId = projectId;
        }

        /// <summary>
        /// Gets the represented compilation.
        /// </summary>
        /// <value>The source-backed compilation.</value>
        public Compilation Compilation { get; }

        /// <summary>
        /// Gets the semantic role of the compilation.
        /// </summary>
        /// <value>The semantic scope kind.</value>
        public SemanticCompilationScopeKind Kind { get; }

        /// <summary>
        /// Gets the Workspace project identity when the scope is project-backed.
        /// </summary>
        /// <value>
        /// The real Workspace project identity, or <see langword="null"/> for
        /// a supporting source dependency.
        /// </value>
        public ProjectId? ProjectId { get; }

        /// <summary>
        /// Gets all named source types declared by the compilation, including
        /// nested source types.
        /// </summary>
        /// <value>
        /// The cached source types in deterministic fully-qualified-name order.
        /// </value>
        public IReadOnlyList<INamedTypeSymbol> SourceTypes
        {
            get
            {
                sourceTypes ??= CollectSourceTypes(Compilation);
                return sourceTypes;
            }
        }

        /// <summary>
        /// Creates a scope for a selected analysis target.
        /// </summary>
        /// <param name="compilation">The target project compilation.</param>
        /// <param name="projectId">The target Workspace project identity.</param>
        /// <returns>The analysis-target scope.</returns>
        public static SemanticCompilationScope CreateAnalysisTarget(
            Compilation compilation,
            ProjectId projectId)
        {
            return new SemanticCompilationScope(
                compilation,
                SemanticCompilationScopeKind.AnalysisTarget,
                projectId);
        }

        /// <summary>
        /// Creates a scope for a referenced Workspace project.
        /// </summary>
        /// <param name="compilation">The referenced project compilation.</param>
        /// <param name="projectId">The referenced Workspace project identity.</param>
        /// <returns>The referenced-project scope.</returns>
        public static SemanticCompilationScope CreateReferencedProject(
            Compilation compilation,
            ProjectId projectId)
        {
            return new SemanticCompilationScope(
                compilation,
                SemanticCompilationScopeKind.ReferencedProject,
                projectId);
        }

        /// <summary>
        /// Creates a non-project scope for a source-backed external dependency.
        /// </summary>
        /// <param name="compilation">The supporting source compilation.</param>
        /// <returns>The supporting-source dependency scope.</returns>
        public static SemanticCompilationScope CreateSupportingSourceDependency(
            Compilation compilation)
        {
            return new SemanticCompilationScope(
                compilation,
                SemanticCompilationScopeKind.SupportingSourceDependency,
                projectId: null);
        }

        /// <summary>
        /// Collects and orders source-declared named types from one compilation.
        /// </summary>
        /// <param name="compilation">The compilation to inspect.</param>
        /// <returns>The recursively collected source types.</returns>
        private static IReadOnlyList<INamedTypeSymbol> CollectSourceTypes(Compilation compilation)
        {
            List<INamedTypeSymbol> collectedTypes = new();
            CollectSourceTypes(compilation.Assembly.GlobalNamespace, collectedTypes);

            collectedTypes.Sort(
                static (left, right) =>
                    StringComparer.Ordinal.Compare(
                        left.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                        right.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)));

            return collectedTypes;
        }

        /// <summary>
        /// Recursively collects source types from one namespace.
        /// </summary>
        /// <param name="namespaceSymbol">The namespace to traverse.</param>
        /// <param name="collectedTypes">The destination list.</param>
        private static void CollectSourceTypes(
            INamespaceSymbol namespaceSymbol,
            List<INamedTypeSymbol> collectedTypes)
        {
            foreach (INamedTypeSymbol typeSymbol in namespaceSymbol.GetTypeMembers())
            {
                CollectSourceType(typeSymbol, collectedTypes);
            }

            foreach (INamespaceSymbol nestedNamespace in namespaceSymbol.GetNamespaceMembers())
            {
                CollectSourceTypes(nestedNamespace, collectedTypes);
            }
        }

        /// <summary>
        /// Adds one source type and recursively adds its nested source types.
        /// </summary>
        /// <param name="typeSymbol">The type to inspect.</param>
        /// <param name="collectedTypes">The destination list.</param>
        private static void CollectSourceType(
            INamedTypeSymbol typeSymbol,
            List<INamedTypeSymbol> collectedTypes)
        {
            if (!typeSymbol.DeclaringSyntaxReferences.IsDefaultOrEmpty)
            {
                collectedTypes.Add(typeSymbol);
            }

            foreach (INamedTypeSymbol nestedType in typeSymbol.GetTypeMembers())
            {
                CollectSourceType(nestedType, collectedTypes);
            }
        }
    }
}
