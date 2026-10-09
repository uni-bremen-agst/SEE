using XMLDocNormalizer.Execution.Semantic;

namespace XMLDocNormalizer.Execution.Analysis
{
    /// <summary>Trusted Main provenance of the required compiler, not authentication of arbitrary metadata.</summary>
    internal enum ExceptionFlowCompilerProvenance
    {
        Unspecified = 0,
        CurrentCompilation = 1,
        ValidatedPortablePdb = 2
    }

    /// <summary>Immutable Main-local evidence produced only by the typed projection boundary, never raw strings.</summary>
    internal sealed partial record ExceptionFlowAnalyzerSelectionContext
    {
        private ExceptionFlowAnalyzerSelectionContext(ExceptionFlowCompilerProvenance provenance, string? compilerVersion)
        {
            Provenance = provenance;
            CompilerVersion = compilerVersion;
        }

        public ExceptionFlowCompilerProvenance Provenance { get; }
        public string? CompilerVersion { get; }

        /// <summary>Retains the B6 convenience API through the same trusted projector; failures stay non-executable.</summary>
        internal static ExceptionFlowAnalyzerSelectionContext FromValidatedPortablePdb(
            ExternalCompilationProvenanceDescriptor? descriptor)
            => Projector.Project(new(PortablePdbProvenance: descriptor)).Context
                ?? new(ExceptionFlowCompilerProvenance.Unspecified, null);
    }

    /// <summary>Selection failures never supply an executable engine choice.</summary>
    internal enum ExceptionFlowAnalyzerSelectionFailureCode
    {
        MissingContext, UnsupportedProvenance, MissingCompilerVersion, UnsupportedCompilerVersion, ConflictingEvidence
    }

    /// <summary>A typed decision failure, separate from routing and Worker failures.</summary>
    internal sealed record ExceptionFlowAnalyzerSelectionFailure(ExceptionFlowAnalyzerSelectionFailureCode Code, string Message);

    /// <summary>A decision only; execution remains owned by the existing B5 router.</summary>
    internal sealed record ExceptionFlowAnalyzerSelectionDecision(
        ExceptionFlowAnalyzerSelection Selection, ExceptionFlowAnalyzerSelectionFailure? Failure)
    {
        internal bool Succeeded => Failure == null
            && Selection is ExceptionFlowAnalyzerSelection.Current or ExceptionFlowAnalyzerSelection.Historical;
    }
}
