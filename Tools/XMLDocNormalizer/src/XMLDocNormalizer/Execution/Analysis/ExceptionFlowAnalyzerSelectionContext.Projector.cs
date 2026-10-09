using System.Reflection;
using Microsoft.CodeAnalysis.CSharp;
using XMLDocNormalizer.Execution.Semantic;

namespace XMLDocNormalizer.Execution.Analysis
{
    /// <summary>Exactly one typed source; raw compiler-version strings are not projection inputs.</summary>
    internal sealed record ExceptionFlowAnalyzerSelectionProjectionInput(
        CSharpCompilation? CurrentCompilation = null,
        ExternalCompilationProvenanceDescriptor? PortablePdbProvenance = null);

    /// <summary>Projection failures are distinct from B6 selection and B5 execution failures.</summary>
    internal enum ExceptionFlowAnalyzerSelectionProjectionFailureCode
    {
        MissingInput, ConflictingInputs, UnvalidatedProvenance, MissingCompilationOptions,
        UnsupportedCompilationOptions, MissingCompilerVersion, MissingCompilerIdentity
    }

    internal sealed record ExceptionFlowAnalyzerSelectionProjectionFailure(
        ExceptionFlowAnalyzerSelectionProjectionFailureCode Code, string Message);

    /// <summary>A failed projection has no partially trusted context.</summary>
    internal sealed record ExceptionFlowAnalyzerSelectionProjectionResult(
        ExceptionFlowAnalyzerSelectionContext? Context, ExceptionFlowAnalyzerSelectionProjectionFailure? Failure)
    {
        internal bool Succeeded => Context != null && Failure == null;
    }

    internal sealed partial record ExceptionFlowAnalyzerSelectionContext
    {
        /// <summary>Sole successful context constructor owner; nested access keeps raw construction private.</summary>
        internal static class Projector
        {
            /// <summary>Projects existing typed evidence without selecting an engine, executing or acquiring artifacts.</summary>
            internal static ExceptionFlowAnalyzerSelectionProjectionResult Project(ExceptionFlowAnalyzerSelectionProjectionInput? input)
            {
                ExceptionFlowAnalyzerSelectionProjectionResult Fail(ExceptionFlowAnalyzerSelectionProjectionFailureCode code, string message)
                    => new(null, new(code, message));

                if (input == null || (input.CurrentCompilation == null && input.PortablePdbProvenance == null))
                {
                    return Fail(ExceptionFlowAnalyzerSelectionProjectionFailureCode.MissingInput, "One existing typed provenance source is required.");
                }
                if (input.CurrentCompilation != null && input.PortablePdbProvenance != null)
                {
                    return Fail(ExceptionFlowAnalyzerSelectionProjectionFailureCode.ConflictingInputs, "Multiple sources cannot be silently preferred or merged.");
                }
                if (input.CurrentCompilation != null)
                {
                    // This actual Current object already belongs to the loaded Main Roslyn universe; no dynamic loading.
                    string? version = input.CurrentCompilation.GetType().Assembly
                        .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
                    if (string.IsNullOrWhiteSpace(version))
                    {
                        return Fail(ExceptionFlowAnalyzerSelectionProjectionFailureCode.MissingCompilerIdentity, "The existing Current compiler has no identity metadata.");
                    }
                    return new(new(ExceptionFlowCompilerProvenance.CurrentCompilation, version), null);
                }

                ExternalCompilationProvenanceDescriptor descriptor = input.PortablePdbProvenance!;
                if (!ExternalCompilationProvenanceDescriptorFactory.IsValidated(descriptor))
                {
                    return Fail(ExceptionFlowAnalyzerSelectionProjectionFailureCode.UnvalidatedProvenance, "PDB provenance must be an actual existing validator output.");
                }
                ExternalCompilationOptionsDescriptor? options = descriptor.CompilationOptions;
                if (options == null)
                {
                    return Fail(ExceptionFlowAnalyzerSelectionProjectionFailureCode.MissingCompilationOptions, "Validated PDB compilation options are absent.");
                }
                if (!options.TryGetValue("language", out string language) || language != "C#"
                    || !options.TryGetValue("version", out string schema) || schema != "2")
                {
                    return Fail(ExceptionFlowAnalyzerSelectionProjectionFailureCode.UnsupportedCompilationOptions, "Validated C# options schema 2 is required.");
                }
                if (!options.TryGetValue("compiler-version", out string compilerVersion) || string.IsNullOrWhiteSpace(compilerVersion))
                {
                    return Fail(ExceptionFlowAnalyzerSelectionProjectionFailureCode.MissingCompilerVersion, "Validated PDB compiler-version evidence is absent.");
                }
                return new(new(ExceptionFlowCompilerProvenance.ValidatedPortablePdb, compilerVersion), null);
            }
        }
    }
}
