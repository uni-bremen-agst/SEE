using Microsoft.CodeAnalysis;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp;
using XMLDocNormalizer.Execution.Semantic;
using XMLDocNormalizer.HistoricalWorker;
using XMLDocNormalizer.Models;

namespace XMLDocNormalizer.Execution.Analysis
{
    /// <summary>Existing validated external data and a PE-bound selector; never raw source/selector strings.</summary>
    internal sealed record HistoricalWorkerPayloadProjectionInput(
        ExternalCompilationProvenanceDescriptor Provenance, ExternalCSharpSyntaxTree Source,
        IMethodSymbol Root, ExceptionAnalysisMode AnalysisMode, ExternalMetadataReferenceSet? ReferenceSet = null,
        ExternalCSharpCompilationConfiguration? ParseConfiguration = null, ExternalCSharpSyntaxTreeSet? SourceSet = null,
        ImmutableArray<HistoricalWorkerPayloadProjectionInput> SupportingProjects = default,
        WorkerDeclaredPolicyRequest? DeclaredPolicy = null);

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
            if (input?.DeclaredPolicy != null && (input.AnalysisMode != ExceptionAnalysisMode.ProjectTransitive
                || input.SourceSet == null || !input.SupportingProjects.IsDefault))
            { return Fail(HistoricalWorkerPayloadProjectionFailureCode.UnsupportedProfile, "Declared policy facts require an explicit complete single ProjectTransitive project, not mode activation or a solution graph."); }
            if (input != null && !input.SupportingProjects.IsDefault) { return HistoricalWorkerSolutionProjection.Project(input); }
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
            if (input.ParseConfiguration != null && (!ReferenceEquals(input.ParseConfiguration, configuration)
                || !ExternalCSharpCompilationConfigurationFactory.IsDerivedFrom(input.ParseConfiguration, provenance)))
            {
                return Fail(HistoricalWorkerPayloadProjectionFailureCode.ConflictingInputs, "Parse context must be the original P5G object used by this P5I handoff.");
            }
            WorkerParseContext? parseContext = null;
            if (input.ParseConfiguration != null && (input.ReferenceSet == null
                || !HistoricalWorkerParseProjection.TryProject(input.ParseConfiguration, out parseContext)))
            {
                return Fail(HistoricalWorkerPayloadProjectionFailureCode.UnsupportedProfile, "Explicit parse context requires exact validated references and the bounded regular parse policy.");
            }
            if ((input.SourceSet == null && (input.AnalysisMode != ExceptionAnalysisMode.SolutionTransitive || configuration.SourceFileCount != 1
                || provenance.PortablePdb.Documents.Length != 1)) || target!.Modules.Length != 1 || target.IsReferenceAssembly
                || (parseContext == null && (parse.LanguageVersion != LanguageVersion.CSharp12 || parse.PreprocessorSymbolNames.Any()))
                || parse.Kind != SourceCodeKind.Regular || parse.DocumentationMode != DocumentationMode.Parse
                || options.NullableContextOptions != NullableContextOptions.Enable || options.OutputKind != OutputKind.DynamicallyLinkedLibrary
                || options.Platform != Platform.AnyCpu || options.OptimizationLevel != OptimizationLevel.Debug
                || options.CheckOverflow || options.AllowUnsafe || configuration.SigningProvenance.State != ExternalAssemblySigningState.Unsigned)
            {
                return Fail(HistoricalWorkerPayloadProjectionFailureCode.UnsupportedProfile, "Recorded inputs cannot be represented by the existing single-source Worker profile.");
            }
            // Published analysis policies, not guesses about original references/assembly identity.
            var context = new WorkerCompilationContext(AnalysisMode: "solution-transitive", ReferenceProfile: "net8-runtime-bounded-v1",
                LanguageVersion: "12", NullableContext: "enable", AssemblyName: "HistoricalWorkerInput");
            if (input.ReferenceSet != null)
            {
                if (!HistoricalWorkerReferenceProjection.TryProject(input.ReferenceSet, provenance, target, out context))
                {
                    return Fail(HistoricalWorkerPayloadProjectionFailureCode.UnsupportedPayload, "Exact references require the original bounded P5C/P5E handoff for this P5A provenance.");
                }
            }
            if (parseContext != null)
            {
                context = context with { LanguageVersion = parseContext.LanguageVersion!, ParseContext = parseContext, ParseProfile = "validated-parse-v1" };
            }
            if (input.SourceSet != null && (parseContext == null || input.ReferenceSet == null
                || !HistoricalWorkerScopeProjection.TryProject(input.SourceSet, input.Source, configuration, provenance,
                    context, input.AnalysisMode, out context)))
            {
                return Fail(HistoricalWorkerPayloadProjectionFailureCode.UnsupportedProfile, "Scope requires the original complete P5J/P5I project, explicit parse and exact references; unsupported modes are not substituted.");
            }
            if (input.DeclaredPolicy != null)
            {
                if (!HistoricalWorkerDeclaredPolicyProjection.TryProject(input, out var policy))
                { return Fail(HistoricalWorkerPayloadProjectionFailureCode.UnsupportedProfile, "Policy opt-in requires the original bound complete nongeneric PE exception definitions, not caller-supplied declarations."); }
                context = context with { DeclaredPolicy = policy };
            }
            var payload = new WorkerAnalysisInput(input.Source.Text.ToString(), MetadataName(method.ContainingType), method.MetadataName, context,
                LogicalPath: input.Source.Document.Name);
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
