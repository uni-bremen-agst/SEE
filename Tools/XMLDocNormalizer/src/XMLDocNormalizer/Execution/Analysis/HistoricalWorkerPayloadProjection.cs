using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using XMLDocNormalizer.Execution.Semantic;
using XMLDocNormalizer.HistoricalWorker;
using XMLDocNormalizer.Models;

namespace XMLDocNormalizer.Execution.Analysis
{
    /// <summary>Existing validated external data and a PE-bound selector; never raw source/selector strings.</summary>
    internal sealed record HistoricalWorkerPayloadProjectionInput(
        ExternalCompilationProvenanceDescriptor Provenance, ExternalCSharpSyntaxTree Source,
        IMethodSymbol Root, ExceptionAnalysisMode AnalysisMode);

    internal enum HistoricalWorkerPayloadProjectionFailureCode
    {
        MissingInput, UnvalidatedProvenance, MissingValidatedPe, UnvalidatedSource,
        ConflictingInputs, UnsupportedProfile, UnsupportedSelector, UnsupportedPayload
    }

    internal sealed record HistoricalWorkerPayloadProjectionFailure(HistoricalWorkerPayloadProjectionFailureCode Code, string Message);

    /// <summary>Original Main-local provenance accompanies the existing bounded neutral payload, not IPC extensions.</summary>
    internal sealed record HistoricalWorkerPayloadProjectionValue(
        ExternalCompilationProvenanceDescriptor Provenance, ExternalCSharpSyntaxTree Source,
        IMethodSymbol Root, WorkerAnalysisInput Payload);

    internal sealed record HistoricalWorkerPayloadProjectionResult(
        HistoricalWorkerPayloadProjectionValue? Value, HistoricalWorkerPayloadProjectionFailure? Failure)
    {
        internal bool Succeeded => Value != null && Failure == null;
    }

    /// <summary>External-input adoption outcome; dispatch is absent if payload prerequisites failed.</summary>
    internal sealed record ExternalExceptionFlowAnalysisResult(
        HistoricalWorkerPayloadProjectionResult PayloadProjection, ExceptionFlowAnalysisDispatchResult? Dispatch)
    {
        internal bool Succeeded => PayloadProjection.Succeeded && Dispatch?.Succeeded == true;
        internal Checks.Infrastructure.Exception.Flow.Canonical.CanonicalExceptionFlowAnalysisResult? Result
            => Succeeded ? Dispatch!.Result : null;
    }

    /// <summary>Transforms already validated P4/P5A/P5G/P5H/P5I handoffs into the unchanged bounded Worker contract.</summary>
    internal static class HistoricalWorkerPayloadProjection
    {
        internal static HistoricalWorkerPayloadProjectionResult Project(HistoricalWorkerPayloadProjectionInput? input)
        {
            HistoricalWorkerPayloadProjectionResult Fail(HistoricalWorkerPayloadProjectionFailureCode code, string message)
                => new(null, new(code, message));
            if (input?.Provenance == null || input.Source == null || input.Root == null)
            {
                return Fail(HistoricalWorkerPayloadProjectionFailureCode.MissingInput, "Existing provenance, source and PE method are required.");
            }
            var provenance = input.Provenance;
            if (!ExternalCompilationProvenanceDescriptorFactory.IsValidated(provenance))
            {
                return Fail(HistoricalWorkerPayloadProjectionFailureCode.UnvalidatedProvenance, "P5A provenance must retain its original validation receipt.");
            }
            if (!ExternalPeDebugDirectoryDescriptorFactory.TryGetValidatedTarget(provenance.DebugDirectory, out var target))
            {
                return Fail(HistoricalWorkerPayloadProjectionFailureCode.MissingValidatedPe, "The original successful P4 PE binding is required.");
            }
            if (!ExternalCSharpSyntaxTreeFactory.TryGetConfiguration(input.Source, out var configuration))
            {
                return Fail(HistoricalWorkerPayloadProjectionFailureCode.UnvalidatedSource, "An original successful P5I handoff is required.");
            }
            if (!ExternalCSharpCompilationConfigurationFactory.IsDerivedFrom(configuration, provenance)
                || !provenance.PortablePdb.Documents.Any(document => ReferenceEquals(document, input.Source.Document)))
            {
                return Fail(HistoricalWorkerPayloadProjectionFailureCode.ConflictingInputs, "Source/options must belong to the same existing P5A provenance.");
            }
            IMethodSymbol method = input.Root;
            try
            {
                ModuleMetadata? module = method.ContainingModule.GetMetadata();
                if (module == null || !method.Locations.Any(location => location.IsInMetadata)
                    || !method.ContainingAssembly.Identity.Equals(target!.AssemblyIdentity)
                    || provenance.DebugDirectory.ManifestModule != new ExternalModuleIdentity(module.Name, module.GetModuleVersionId()))
                {
                    return Fail(HistoricalWorkerPayloadProjectionFailureCode.ConflictingInputs, "Method must belong to the original validated PE manifest module.");
                }
            }
            catch (Exception exception) when (exception is BadImageFormatException or ObjectDisposedException or InvalidOperationException)
            {
                return Fail(HistoricalWorkerPayloadProjectionFailureCode.UnsupportedSelector, "The retained PE method metadata is unavailable.");
            }
            if (method.MethodKind != MethodKind.Ordinary || !method.IsStatic || method.Arity != 0
                || method.Parameters.Length != 0 || !method.IsDefinition || method.IsAbstract || method.IsExtern
                || method.ContainingType.IsGenericType)
            {
                return Fail(HistoricalWorkerPayloadProjectionFailureCode.UnsupportedSelector, "Worker requires a non-generic parameterless static ordinary method.");
            }
            var parse = configuration!.ParseOptions;
            var options = configuration.CompilationOptions;
            if (input.AnalysisMode != ExceptionAnalysisMode.SolutionTransitive || configuration.SourceFileCount != 1
                || provenance.PortablePdb.Documents.Length != 1 || target!.Modules.Length != 1 || target.IsReferenceAssembly
                || parse.LanguageVersion != LanguageVersion.CSharp12 || parse.Kind != SourceCodeKind.Regular
                || parse.DocumentationMode != DocumentationMode.Parse || parse.PreprocessorSymbolNames.Any()
                || options.NullableContextOptions != NullableContextOptions.Enable || options.OutputKind != OutputKind.DynamicallyLinkedLibrary
                || options.Platform != Platform.AnyCpu || options.OptimizationLevel != OptimizationLevel.Debug
                || options.CheckOverflow || options.AllowUnsafe || configuration.SigningProvenance.State != ExternalAssemblySigningState.Unsigned)
            {
                return Fail(HistoricalWorkerPayloadProjectionFailureCode.UnsupportedProfile, "Recorded inputs cannot be represented by the existing single-source Worker profile.");
            }
            // Published analysis policies, not guesses about original references/assembly identity.
            var context = new WorkerCompilationContext(AnalysisMode: "solution-transitive", ReferenceProfile: "net8-runtime-bounded-v1",
                LanguageVersion: "12", NullableContext: "enable", AssemblyName: "HistoricalWorkerInput");
            var payload = new WorkerAnalysisInput(input.Source.Text.ToString(), MetadataName(method.ContainingType), method.MetadataName, context);
            if (!WorkerInputValidation.IsSupported(payload))
            {
                return Fail(HistoricalWorkerPayloadProjectionFailureCode.UnsupportedPayload, "Mapped source/selector exceeds the existing Worker contract.");
            }
            return new(new(provenance, input.Source, method, payload), null);
        }

        private static string MetadataName(INamedTypeSymbol type)
            => type.ContainingType != null ? MetadataName(type.ContainingType) + "+" + type.MetadataName
                : (type.ContainingNamespace.IsGlobalNamespace ? "" : NamespaceMetadataName(type.ContainingNamespace) + ".") + type.MetadataName;

        private static string NamespaceMetadataName(INamespaceSymbol symbol)
            => (symbol.ContainingNamespace.IsGlobalNamespace ? "" : NamespaceMetadataName(symbol.ContainingNamespace) + ".") + symbol.MetadataName;
    }
}
