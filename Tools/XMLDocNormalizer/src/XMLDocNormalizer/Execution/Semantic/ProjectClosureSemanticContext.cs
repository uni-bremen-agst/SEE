using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
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
        /// Maps project compilation objects to their semantic scopes.
        /// </summary>
        private readonly Dictionary<Compilation, SemanticCompilationScope> projectScopesByCompilation;

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
        /// Gets the context-local demand-driven external reference catalog.
        /// </summary>
        /// <value>The external assembly reference catalog.</value>
        private ExternalAssemblyReferenceCatalog ExternalAssemblyReferences { get; } = new();

        /// <summary>
        /// Gets the context-local prepared reconstruction plans and attempt states.
        /// </summary>
        /// <value>The demand-driven reconstruction catalog.</value>
        private ExternalSupportingSourceReconstructionCatalog ExternalReconstructions { get; } =
            new();

        /// <summary>
        /// Gets the context-local explicit-root binary candidate discovery.
        /// </summary>
        /// <value>The local non-recursive discovery catalog.</value>
        private ExternalBinaryCandidateDiscovery ExternalBinaryCandidates { get; } = new();

        /// <summary>
        /// Gets the context-local exact Portable PDB acquisition catalog.
        /// </summary>
        /// <value>The demand-driven bounded local PDB acquisition cache.</value>
        private ExternalPortablePdbAcquisition ExternalPortablePdbs { get; } = new();

        /// <summary>
        /// Gets the context-local controlled external source acquisition.
        /// </summary>
        /// <value>The local-mapping and optional Source Link acquisition cache.</value>
        private ExternalSourceAcquisition ExternalSources { get; } = new();

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
            projectScopesByCompilation = new Dictionary<Compilation, SemanticCompilationScope>(
                ReferenceEqualityComparer.Instance);

            foreach (SemanticCompilationScope scope in this.projectCompilationScopes)
            {
                projectScopesByCompilation.TryAdd(scope.Compilation, scope);
            }
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
        /// Thrown when the exact assembly identity or a syntax tree in
        /// <paramref name="compilation"/> already belongs to a project or
        /// another supporting source compilation.
        /// </exception>
        public SemanticCompilationScope RegisterSupportingSource(Compilation compilation)
        {
            ArgumentNullException.ThrowIfNull(compilation);

            AssemblyIdentity assemblyIdentity = compilation.Assembly.Identity;

            if (projectCompilationScopes.Any(
                    scope => scope.Compilation.Assembly.Identity.Equals(assemblyIdentity)))
            {
                throw new InvalidOperationException(
                    "A supporting source assembly identity already belongs to a project scope.");
            }

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
        /// Tries to register one validated external compilation as a
        /// context-local supporting source dependency.
        /// </summary>
        /// <param name="supportingSource">
        /// The exact P3 binary identity coupled to the unchanged P5K
        /// compilation instance.
        /// </param>
        /// <param name="scope">
        /// The supporting-source scope when registration succeeds.
        /// </param>
        /// <returns>
        /// <see langword="true"/> for a new or idempotent exact registration;
        /// otherwise <see langword="false"/>. Conflicting identity,
        /// compilation, or syntax-tree ownership fails closed.
        /// </returns>
        /// <remarks>
        /// The registration provides source-backed semantic information but
        /// does not activate exception-summary traversal. The compilation is
        /// not an analysis target and independently produces no XML-
        /// documentation findings. Generator-output completeness and
        /// historical compiler fidelity remain caller preconditions.
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="supportingSource"/> is
        /// <see langword="null"/>.
        /// </exception>
        public bool TryRegisterExternalSupportingSource(
            ExternalSupportingSourceCompilation supportingSource,
            out SemanticCompilationScope scope)
        {
            ArgumentNullException.ThrowIfNull(supportingSource);

            CSharpCompilation compilation = supportingSource.Compilation;

            if (projectScopesByCompilation.ContainsKey(compilation))
            {
                scope = null!;
                return false;
            }

            foreach (SyntaxTree syntaxTree in compilation.SyntaxTrees)
            {
                if (projectScopesBySyntaxTree.ContainsKey(syntaxTree))
                {
                    scope = null!;
                    return false;
                }
            }

            return SupportingSources.TryRegisterExternal(
                supportingSource,
                out scope);
        }

        /// <summary>
        /// Registers explicitly prepared reconstruction inputs for one exact
        /// external binary without opening any candidate file.
        /// </summary>
        /// <param name="plan">
        /// The immutable, complete-P3-identity-bound reconstruction plan.
        /// </param>
        /// <returns>
        /// <see langword="true"/> for a new or semantically idempotent plan;
        /// otherwise <see langword="false"/> for a conflicting plan.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="plan"/> is <see langword="null"/>.
        /// </exception>
        public bool TryRegisterExternalSupportingSourceReconstructionPlan(
            ExternalSupportingSourceReconstructionPlan plan)
        {
            ArgumentNullException.ThrowIfNull(plan);

            return ExternalReconstructions.TryRegister(plan);
        }

        /// <summary>
        /// Configures all local roots that discovery-enabled reconstruction
        /// plans may search for P5A metadata-reference candidates.
        /// </summary>
        /// <param name="searchRoots">
        /// Fully qualified directory paths. Roots are normalized,
        /// deduplicated, and captured without filesystem access.
        /// </param>
        /// <returns>
        /// <see langword="true"/> for a new or idempotent root set; otherwise
        /// <see langword="false"/>. Roots cannot change after discovery starts.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="searchRoots"/> is
        /// <see langword="null"/>.
        /// </exception>
        /// <remarks>
        /// Enumeration is non-recursive and demand-driven. Discovery finds
        /// candidates only; existing P5 validation establishes usability.
        /// </remarks>
        public bool TryConfigureExternalBinarySearchRoots(
            IEnumerable<string> searchRoots)
        {
            ArgumentNullException.ThrowIfNull(searchRoots);

            return ExternalBinaryCandidates.TryConfigure(searchRoots);
        }

        /// <summary>
        /// Configures optional standard local artifact sources for discovery-enabled plans.
        /// </summary>
        /// <param name="configuration">
        /// The immutable loaded-reference, .NET, and NuGet source snapshot.
        /// </param>
        /// <returns>
        /// <see langword="true"/> for a new or idempotent configuration; otherwise
        /// <see langword="false"/> after discovery begins or for a conflicting snapshot.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="configuration"/> is <see langword="null"/>.
        /// </exception>
        /// <remarks>
        /// Configuration performs no artifact enumeration. Explicit-reference plans
        /// continue to bypass discovery and never consult these sources.
        /// </remarks>
        public bool TryConfigureExternalBinaryArtifactSources(
            ExternalReferenceArtifactSourceConfiguration configuration)
        {
            ArgumentNullException.ThrowIfNull(configuration);
            return ExternalBinaryCandidates.TryConfigureStandardArtifactSources(
                configuration);
        }

        /// <summary>
        /// Configures immutable local Portable PDB candidate sources before analysis.
        /// </summary>
        /// <param name="configuration">The context-local source snapshot.</param>
        /// <returns>
        /// <see langword="true"/> for a new or idempotent configuration;
        /// otherwise <see langword="false"/> after acquisition starts.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="configuration"/> is <see langword="null"/>.
        /// </exception>
        public bool TryConfigureExternalPortablePdbSources(
            ExternalPortablePdbAcquisitionConfiguration configuration)
        {
            ArgumentNullException.ThrowIfNull(configuration);
            return ExternalPortablePdbs.TryConfigure(configuration);
        }

        /// <summary>
        /// Configures all context-local document-prefix projections permitted
        /// for acquisition-enabled external source inputs.
        /// </summary>
        /// <param name="sourceMappings">
        /// The complete local mapping set. Roots must be fully qualified.
        /// </param>
        /// <returns>
        /// <see langword="true"/> for new or idempotent configuration;
        /// otherwise <see langword="false"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="sourceMappings"/> is
        /// <see langword="null"/>.
        /// </exception>
        /// <remarks>
        /// Configuration performs no source-file or network I/O. Document
        /// names are projected only inside the explicitly permitted roots and
        /// P5H remains the source-identity boundary.
        /// </remarks>
        public bool TryConfigureExternalSourceMappings(
            IEnumerable<ExternalSourcePathMapping> sourceMappings)
        {
            ArgumentNullException.ThrowIfNull(sourceMappings);

            return ExternalSources.TryConfigureMappings(sourceMappings);
        }

        /// <summary>
        /// Configures whether acquired external source bytes may be subjected
        /// to deterministic line-ending reconstruction before P5H validation.
        /// </summary>
        /// <param name="policy">The context-local reconstruction policy.</param>
        /// <returns>
        /// <see langword="true"/> for new or value-idempotent configuration;
        /// otherwise <see langword="false"/>.
        /// </returns>
        /// <remarks>
        /// Strict direct-byte validation remains the default. Configuration is
        /// immutable after the first external source acquisition attempt.
        /// </remarks>
        public bool TryConfigureExternalSourceReconstruction(
            ExternalSourceReconstructionPolicy policy)
        {
            return ExternalSources.TryConfigureReconstructionPolicy(policy);
        }

        /// <summary>
        /// Explicitly enables bounded HTTPS Source Link acquisition for this
        /// semantic context using the production network policy.
        /// </summary>
        /// <returns>
        /// <see langword="true"/> when the policy is newly configured;
        /// otherwise <see langword="false"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown transitively if runtime HTTP policy construction fails.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown transitively if a built-in positive network limit is invalid.
        /// </exception>
        /// <remarks>
        /// Source Link is disabled by default. Its URI is a retrieval hint,
        /// not a trust boundary; downloaded bytes must still pass P5H.
        /// </remarks>
        public bool TryEnableExternalSourceLink()
        {
            return ExternalSources.TryConfigureSourceLink(
                ExternalSourceLinkClient.CreateDefault());
        }

        /// <summary>
        /// Explicitly configures a testable context-local Source Link client.
        /// </summary>
        /// <param name="client">The bounded HTTPS client.</param>
        /// <returns>
        /// <see langword="true"/> for new or reference-idempotent
        /// configuration; otherwise <see langword="false"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="client"/> is <see langword="null"/>.
        /// </exception>
        internal bool TryEnableExternalSourceLink(ExternalSourceLinkClient client)
        {
            ArgumentNullException.ThrowIfNull(client);

            return ExternalSources.TryConfigureSourceLink(client);
        }

        /// <summary>
        /// Tries to locate a registered supporting source scope by its exact
        /// assembly identity.
        /// </summary>
        /// <param name="assemblyIdentity">The exact supporting assembly identity.</param>
        /// <param name="scope">The matching supporting scope when found.</param>
        /// <returns>
        /// <see langword="true"/> when the identity is registered as supporting
        /// source; otherwise <see langword="false"/>.
        /// </returns>
        public bool TryGetSupportingSourceScope(
            AssemblyIdentity assemblyIdentity,
            out SemanticCompilationScope scope)
        {
            return SupportingSources.TryGetScope(assemblyIdentity, out scope);
        }

        /// <summary>
        /// Tries to locate an external supporting source by the exact P3
        /// identity of its original binary.
        /// </summary>
        /// <param name="binaryIdentity">
        /// The full assembly identity, ordered module identities, and
        /// reference-assembly classification to match. Paths are ignored.
        /// </param>
        /// <param name="scope">The registered supporting scope when found.</param>
        /// <returns>
        /// <see langword="true"/> only for an exact binary-identity match;
        /// otherwise <see langword="false"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="binaryIdentity"/> is
        /// <see langword="null"/>.
        /// </exception>
        public bool TryGetExternalSupportingSourceScope(
            ExternalAssemblyReferenceDescriptor binaryIdentity,
            out SemanticCompilationScope scope)
        {
            return SupportingSources.TryGetExternalScope(binaryIdentity, out scope);
        }

        /// <summary>
        /// Tries to locate external supporting source for an assembly symbol
        /// bound by a compilation owned by this context.
        /// </summary>
        /// <param name="bindingCompilation">
        /// The owned compilation that bound <paramref name="assemblySymbol"/>.
        /// </param>
        /// <param name="assemblySymbol">The metadata assembly symbol.</param>
        /// <param name="scope">The exact registered supporting scope.</param>
        /// <returns>
        /// <see langword="true"/> when the existing external-reference catalog
        /// produces a descriptor whose complete P3 binary identity is
        /// registered; otherwise <see langword="false"/>. No assembly-name,
        /// assembly-identity-only, or path fallback is used.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="bindingCompilation"/> or
        /// <paramref name="assemblySymbol"/> is <see langword="null"/>.
        /// </exception>
        public bool TryGetExternalSupportingSourceScope(
            Compilation bindingCompilation,
            IAssemblySymbol assemblySymbol,
            out SemanticCompilationScope scope)
        {
            ArgumentNullException.ThrowIfNull(bindingCompilation);
            ArgumentNullException.ThrowIfNull(assemblySymbol);

            if (!TryGetExternalAssemblyReferenceDescriptor(
                    bindingCompilation,
                    assemblySymbol,
                    out ExternalAssemblyReferenceDescriptor binaryIdentity))
            {
                scope = null!;
                return false;
            }

            return TryEnsureExternalSupportingSourceScope(binaryIdentity, out scope);
        }

        /// <summary>
        /// Uses the P6A fast path or performs the single prepared P6C attempt
        /// before retrying the same exact supporting-source lookup.
        /// </summary>
        /// <param name="binaryIdentity">The complete P3 binary identity.</param>
        /// <param name="scope">The exact P6A scope when available.</param>
        /// <returns>
        /// <see langword="true"/> when an existing or newly reconstructed
        /// supporting source is registered; otherwise <see langword="false"/>.
        /// </returns>
        private bool TryEnsureExternalSupportingSourceScope(
            ExternalAssemblyReferenceDescriptor binaryIdentity,
            out SemanticCompilationScope scope)
        {
            if (binaryIdentity == null)
            {
                scope = null!;
                return false;
            }

            if (SupportingSources.TryGetExternalScope(binaryIdentity, out scope))
            {
                return true;
            }

            if (!ExternalReconstructions.TryBegin(
                    binaryIdentity,
                    out ExternalSupportingSourceReconstructionPlan plan))
            {
                scope = null!;
                return false;
            }

            bool succeeded = false;

            try
            {
                succeeded = ExternalSupportingSourceReconstructionOrchestrator.TryReconstruct(
                        plan,
                        ExternalBinaryCandidates,
                        ExternalPortablePdbs,
                        ExternalSources,
                        out ExternalSupportingSourceCompilation supportingSource)
                    && TryRegisterExternalSupportingSource(supportingSource, out _);
            }
            catch (ArgumentNullException)
            {
                succeeded = false;
            }
            finally
            {
                ExternalReconstructions.Complete(binaryIdentity, succeeded);
            }

            return succeeded
                && SupportingSources.TryGetExternalScope(binaryIdentity, out scope);
        }

        /// <summary>
        /// Tries to get the demand-driven external PE descriptor for an
        /// assembly bound by a compilation in this analysis context.
        /// </summary>
        /// <param name="compilation">The compilation that bound the assembly.</param>
        /// <param name="assemblySymbol">The bound assembly symbol.</param>
        /// <param name="descriptor">
        /// The cached or newly created external descriptor when available.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when the compilation belongs to the current
        /// analysis context and an external PE descriptor is available;
        /// otherwise <see langword="false"/>. Unknown compilations are not
        /// added to the external reference catalog.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="compilation"/> or
        /// <paramref name="assemblySymbol"/> is <see langword="null"/>.
        /// </exception>
        public bool TryGetExternalAssemblyReferenceDescriptor(
            Compilation compilation,
            IAssemblySymbol assemblySymbol,
            out ExternalAssemblyReferenceDescriptor descriptor)
        {
            ArgumentNullException.ThrowIfNull(compilation);
            ArgumentNullException.ThrowIfNull(assemblySymbol);

            if (!projectScopesByCompilation.ContainsKey(compilation)
                && !SupportingSources.TryGetScope(compilation, out _))
            {
                descriptor = null!;
                return false;
            }

            return ExternalAssemblyReferences.TryGetDescriptor(
                compilation,
                assemblySymbol,
                out descriptor);
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
                .Concat(SupportingSources.GetAnalysisScopes())
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
