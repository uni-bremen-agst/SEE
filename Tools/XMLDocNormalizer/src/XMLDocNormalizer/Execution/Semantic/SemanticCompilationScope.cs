using Microsoft.CodeAnalysis;
using XMLDocNormalizer.Checks.Infrastructure.Exception.Flow;

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
    internal sealed class SemanticCompilationScope : ExceptionFlowSemanticScope
    {
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
            : base(compilation)
        {
            Kind = kind;
            ProjectId = projectId;
        }

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

    }
}
