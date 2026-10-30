using Microsoft.CodeAnalysis;

namespace XMLDocNormalizer.Checks.Infrastructure.Exception.Flow
{
    /// <summary>
    /// Provides the compilation-bound semantic capabilities required by the
    /// exception-flow analyzer.
    /// </summary>
    /// <remarks>
    /// One environment belongs to one analysis context. Its host-supplied
    /// partial implementation provides semantic-model lookup, analysis-scope
    /// enumeration, exact supporting-source method resolution, and exact
    /// external supporting-scope lookup. The nonvirtual compile-time seam
    /// preserves one Roslyn type universe and fails closed when identity or
    /// ownership cannot be proven.
    /// </remarks>
    internal sealed partial class ExceptionFlowSemanticEnvironment
    {
        private readonly Dictionary<Compilation, bool> blockingCompilationErrors =
            new(ReferenceEqualityComparer.Instance);

        // Only positive, exact model reads are replayable. Acquisition APIs
        // exclude the entire local inspection, including unsuccessful lookups.
        private AdmissionReads? admissionReads;

        internal AdmissionReads BeginAdmissionReads()
        {
            var reads = new AdmissionReads(this, admissionReads);
            admissionReads = reads;
            return reads;
        }

        internal void ExcludeAdmissionReuse()
        {
            for (var reads = admissionReads; reads != null; reads = reads.Parent)
                reads.Reusable = false;
        }

        private void ObserveAdmissionModel(SyntaxTree tree, SemanticModel? model)
        {
            for (var reads = admissionReads; reads != null; reads = reads.Parent)
            {
                if (model == null) reads.Reusable = false;
                else if (reads.Models.TryGetValue(tree, out var previous)
                    && !ReferenceEquals(previous, model.Compilation)) reads.Reusable = false;
                else reads.Models[tree] = model.Compilation;
            }
        }

        /// <summary>A sequential local read transaction; never retained by a published fact.</summary>
        internal sealed class AdmissionReads : IDisposable
        {
            private readonly ExceptionFlowSemanticEnvironment environment;
            internal AdmissionReads? Parent { get; }
            internal bool Reusable { get; set; } = true;
            internal Dictionary<SyntaxTree, Compilation> Models { get; } = new(ReferenceEqualityComparer.Instance);

            internal AdmissionReads(ExceptionFlowSemanticEnvironment environment, AdmissionReads? parent)
            { this.environment = environment; Parent = parent; }

            public void Dispose() => environment.admissionReads = Parent;
        }

        /// <summary>
        /// Gets the A9 blocking-error status once per exact compilation in this
        /// sequential environment. Executable packaging error CS5001 is ignored.
        /// </summary>
        /// <param name="compilation">The immutable compilation being inspected.</param>
        /// <returns>Whether a compiler error other than CS5001 is present.</returns>
        internal bool HasBlockingCompilationErrors(Compilation compilation)
        {
            ArgumentNullException.ThrowIfNull(compilation);
            if (blockingCompilationErrors.TryGetValue(compilation, out bool cached))
                return cached;

            bool value = compilation.GetDiagnostics().Any(diagnostic =>
                diagnostic.Severity == DiagnosticSeverity.Error && diagnostic.Id != "CS5001");
            // Failed computations propagate and never publish a status.
            blockingCompilationErrors.Add(compilation, value);
            return value;
        }
    }
}
