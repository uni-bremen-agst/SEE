using System.Collections.Immutable;
using XMLDocNormalizer.Checks.Infrastructure.Exception.Flow.Canonical;
using XMLDocNormalizer.Execution.Historical;
using XMLDocNormalizer.HistoricalWorker;
using XMLDocNormalizer.Models;

namespace XMLDocNormalizer.Execution.Analysis
{
    internal sealed record DeclaredFindingAnchor(string LogicalPath, int Position, int Line, int Column, FindingContext Context);
    internal sealed record DeclaredFindingTag(WorkerExceptionTagSyntax Syntax, WorkerCrefBindingKind Kind, CanonicalTypeIdentity? Type,
        bool? IsException, DeclaredFindingAnchor Anchor, string Snippet);
    /// <summary>Immutable complete selected-member reporting inputs, not Findings or a productive mode activation.</summary>
    internal sealed record DeclaredFindingDocumentationFacts(CanonicalCallableIdentity Method, bool HasDocumentation,
        bool HasInheritdoc, DeclaredFindingAnchor Member, ImmutableArray<DeclaredFindingTag> Tags,
        ImmutableArray<WorkerTypeDisplayFact> TypeDisplays);
    internal sealed record DeclaredFindingDocumentationResult(DeclaredFindingDocumentationFacts? Facts, DeclaredFindingPolicyFailure? Failure)
    {
        internal bool Succeeded => Facts != null && Failure == null;
    }
    internal static class DeclaredFindingDocumentationContract
    {
        internal static DeclaredFindingDocumentationResult Read(HistoricalWorkerCallResult? call, WorkerDeclaredPolicyMode? mode)
        {
            if (mode != WorkerDeclaredPolicyMode.ProjectTransitiveDeclaredExceptions)
            { return new(null, new(DeclaredFindingPolicyFailureCode.InvalidMode, "An explicit Declared reporting mode is required.")); }
            if (!HistoricalWorkerResponseValidation.TryGetDeclaredPolicyFacts(call, out var facts) || facts!.Documentation == null)
            { return new(null, new(DeclaredFindingPolicyFailureCode.UnvalidatedEvidence, "A complete original validated B19 documentation receipt is required.")); }
            return new(facts.Documentation, null);
        }
    }
}
