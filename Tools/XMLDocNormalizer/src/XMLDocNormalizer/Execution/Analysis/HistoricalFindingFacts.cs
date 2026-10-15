using XMLDocNormalizer.Checks.Infrastructure.Exception.Flow.Canonical;
using XMLDocNormalizer.Execution.Historical;
using XMLDocNormalizer.HistoricalWorker;

namespace XMLDocNormalizer.Execution.Analysis
{
    /// <summary>Validation-owned immutable independent Finding facts. No Declared inventory or relevance decision.</summary>
    internal sealed record HistoricalFindingFacts(CanonicalExceptionFlowAnalysisResult DirectResult,
        CanonicalExceptionFlowAnalysisResult FlowResult, DeclaredFindingDocumentationFacts Documentation,
        WorkerDeclaredPolicyMode Mode, WorkerDeclaredExecution Execution, string OwnershipSnapshot,
        HistoricalWorkerClient.DocumentationExecution DocumentationExecution);
}
