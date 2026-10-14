using System.Collections.Immutable;
using XMLDocNormalizer.Checks.Infrastructure.Exception.Flow.Canonical;
using XMLDocNormalizer.Execution.Historical;
using XMLDocNormalizer.HistoricalWorker;

namespace XMLDocNormalizer.Execution.Analysis
{
    /// <summary>Finding purpose, not canonical analyzer evidence strength or a guessed path-step kind.</summary>
    internal enum DeclaredFindingSourceKind { Unspecified, Direct, Summary, Documented }
    internal enum DeclaredFindingPolicyFailureCode { InvalidMode, MissingTypeIdentity, InvalidSourceKind, UnvalidatedEvidence, UnknownExceptionType }
    internal sealed record DeclaredFindingPolicyQuery(WorkerDeclaredPolicyMode? Mode, CanonicalTypeIdentity? ExceptionType, DeclaredFindingSourceKind SourceKind);
    internal sealed record DeclaredFindingPolicyFailure(DeclaredFindingPolicyFailureCode Code, string Message);
    internal sealed record DeclaredFindingPolicyDecision(bool LocallyDeclared, bool SummaryEnabled, bool HasDirectSource,
        bool HasSummarySource, DeclaredFindingSourceKind SourceKind, bool Relevant);
    internal sealed record DeclaredFindingPolicyResult(DeclaredFindingPolicyDecision? Decision, DeclaredFindingPolicyFailure? Failure)
    {
        internal bool Succeeded => Decision != null && Failure == null;
    }

    /// <summary>Immutable neutral snapshot, consumable only via the validation-owned call receipt.</summary>
    internal sealed class DeclaredPolicyFacts(WorkerDeclaredPolicyEvidence evidence, CanonicalExceptionFlowAnalysisResult summary,
        DeclaredFindingDocumentationFacts? documentation = null, HistoricalWorkerClient.DocumentationExecution? documentationExecution = null,
        WorkerDeclaredExecution? execution = null)
    {
        internal ImmutableHashSet<CanonicalTypeIdentity> Declarations { get; } = evidence.Declarations!.Select(item => item.Type!).ToImmutableHashSet();
        internal ImmutableHashSet<CanonicalTypeIdentity> Direct { get; } = evidence.DirectResult!.Entries.Select(item => item.ExceptionType).ToImmutableHashSet();
        internal ImmutableHashSet<CanonicalTypeIdentity> Summary { get; } = summary.Entries.Select(item => item.ExceptionType).ToImmutableHashSet();
        internal DeclaredFindingDocumentationFacts? Documentation { get; } = documentation;
        internal HistoricalWorkerClient.DocumentationExecution? DocumentationExecution { get; } = documentationExecution;
        internal WorkerDeclaredExecution? Execution { get; } = execution;
        internal CanonicalExceptionFlowAnalysisResult DirectResult { get; } = evidence.DirectResult!;
        internal CanonicalExceptionFlowAnalysisResult FlowResult { get; } = summary;
        internal string OwnershipSnapshot { get; } = System.Text.Json.JsonSerializer.Serialize(evidence.Declarations, WorkerProtocol.JsonOptions);
        internal ImmutableHashSet<CanonicalTypeIdentity> Documented { get; } = documentation?.Tags.Where(tag => tag.IsException == true)
            .Select(tag => tag.Type!).ToImmutableHashSet() ?? ImmutableHashSet<CanonicalTypeIdentity>.Empty;
    }

    /// <summary>Main detector eligibility over validated facts; no Analyzer execution or Finding rendering.</summary>
    internal static class DeclaredExceptionFindingPolicy
    {
        internal static DeclaredFindingPolicyResult Evaluate(HistoricalWorkerCallResult? call, DeclaredFindingPolicyQuery? query)
        {
            DeclaredFindingPolicyResult Fail(DeclaredFindingPolicyFailureCode code, string message) => new(null, new(code, message));
            if (query?.Mode != WorkerDeclaredPolicyMode.ProjectTransitiveDeclaredExceptions)
            { return Fail(DeclaredFindingPolicyFailureCode.InvalidMode, "An explicit Declared detector-policy mode is required."); }
            if (query.ExceptionType == null) { return Fail(DeclaredFindingPolicyFailureCode.MissingTypeIdentity, "A known canonical exception identity is required."); }
            if (query.SourceKind is not (DeclaredFindingSourceKind.Direct or DeclaredFindingSourceKind.Summary or DeclaredFindingSourceKind.Documented))
            { return Fail(DeclaredFindingPolicyFailureCode.InvalidSourceKind, "An explicit supported Finding source purpose is required."); }
            if (!HistoricalWorkerResponseValidation.TryGetDeclaredPolicyFacts(call, out var facts))
            { return Fail(DeclaredFindingPolicyFailureCode.UnvalidatedEvidence, "Only the original successfully validated complete policy call is accepted."); }
            var validated = facts!;
            bool local = validated.Declarations.Contains(query.ExceptionType);
            bool direct = validated.Direct.Contains(query.ExceptionType);
            bool summary = validated.Summary.Contains(query.ExceptionType);
            if (!local && !direct && !summary && !validated.Documented.Contains(query.ExceptionType))
            { return Fail(DeclaredFindingPolicyFailureCode.UnknownExceptionType, "No source-definition or proven-flow exception fact exists for this identity."); }
            bool enabled = validated.Declarations.Count != 0; // Complete inventory, not exceptions observed in Result.
            bool relevant = query.SourceKind switch
            {
                DeclaredFindingSourceKind.Direct => direct,
                DeclaredFindingSourceKind.Summary => enabled && summary && !direct && local,
                DeclaredFindingSourceKind.Documented => local,
                _ => false
            };
            return new(new(local, enabled, direct, summary, query.SourceKind, relevant), null);
        }
    }
}
