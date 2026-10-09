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

    /// <summary>Immutable Main-local evidence; callers must supply a validated C# analysis context.</summary>
    /// <remarks>Current evidence is caller-owned; external evidence must originate from target-bound validated PDB provenance.</remarks>
    internal sealed record ExceptionFlowAnalyzerSelectionContext(
        ExceptionFlowCompilerProvenance Provenance = ExceptionFlowCompilerProvenance.Unspecified,
        string? CompilerVersion = null)
    {
        /// <summary>Projects existing validated C# PDB options, without acquiring artifacts or reconstructing a compilation.</summary>
        internal static ExceptionFlowAnalyzerSelectionContext FromValidatedPortablePdb(
            ExternalCompilationProvenanceDescriptor? descriptor)
        {
            ExternalCompilationOptionsDescriptor? options = descriptor?.CompilationOptions;
            if (options == null || !options.TryGetValue("language", out string language) || language != "C#"
                || !options.TryGetValue("version", out string schema) || schema != "2")
            {
                return new();
            }
            options.TryGetValue("compiler-version", out string compilerVersion);
            return new(ExceptionFlowCompilerProvenance.ValidatedPortablePdb, compilerVersion);
        }
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
