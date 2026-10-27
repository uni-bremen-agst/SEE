using System.Text.Json;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using XMLDocNormalizer.Execution.Historical;
using XMLDocNormalizer.HistoricalWorker;
using XMLDocNormalizer.Models;

namespace XMLDocNormalizer.Execution.Analysis
{
    /// <summary>Main-local, single-operation binding. Neither a wire claim nor an analyzer/profile implementation.</summary>
    internal sealed class HistoricalSolutionFindingOperation
    {
        internal HistoricalWorkerPayloadProjectionResult Direct { get; }
        internal HistoricalWorkerPayloadProjectionResult Summary { get; }
        internal string GraphSnapshot { get; }
        private readonly string directSnapshot;
        private readonly string summarySnapshot;
        private int consumed;
        private readonly object directCapture;
        private readonly object summaryCapture;
        private readonly ImmutableArray<IMethodSymbol> supportingPeRoots;

        private HistoricalSolutionFindingOperation(HistoricalWorkerPayloadProjectionResult direct, HistoricalWorkerPayloadProjectionResult summary,
            ImmutableArray<IMethodSymbol> supportingPeRoots)
        {
            Direct = direct; Summary = summary;
            this.supportingPeRoots = supportingPeRoots;
            directSnapshot = Json(direct.Value!.Payload);
            summarySnapshot = Json(summary.Value!.Payload);
            GraphSnapshot = Json(summary.Value.Payload.Context!.Solution);
            directCapture = HistoricalWorkerClient.RequestExecutionCapture(direct.Value.Payload);
            summaryCapture = HistoricalWorkerClient.RequestExecutionCapture(summary.Value.Payload);
        }

        private static string Json(object? value) => JsonSerializer.Serialize(value, WorkerProtocol.JsonOptions);

        internal static bool TryPrepare(HistoricalWorkerPayloadProjectionInput input,
            out HistoricalSolutionFindingOperation? operation, out HistoricalWorkerPayloadProjectionResult projection)
        {
            operation = null;
            projection = new(null, new(HistoricalWorkerPayloadProjectionFailureCode.UnsupportedProfile,
                "Solution Finding adoption is not admitted without an original bounded two/three-project chain and one reporting root."));
            if (input.AnalysisMode != ExceptionAnalysisMode.SolutionTransitive || input.DeclaredPolicy != null || input.Finding != null
                || input.SupportingProjects.IsDefault || input.SupportingProjects.Length is < 1 or > 2) { return false; }
            var summary = HistoricalWorkerPayloadProjection.Project(input);
            if (!summary.Succeeded) { projection = summary; return false; }
            var graph = summary.Value!.Payload.Context!.Solution!;
            var nodes = graph.Projects!;
            // All nodes are reachable, unique and acyclic (existing Worker input validator). Further bound to a chain.
            if (nodes.Length is < 2 or > 3 || nodes.Count(node => Equals(node.Identity, graph.Target)) != 1
                || nodes.Any(node => node.Dependencies!.Length > 1)
                || nodes.Count(node => node.Dependencies!.Length == 0) != 1
                || nodes.SelectMany(node => node.Dependencies!).GroupBy(Json).Any(group => group.Count() > 1)
                || nodes.Any(node => node.Input!.Context!.DeclaredPolicy != null || node.Input.Context.Finding != null
                    || node.Input.Context.DeclaredExecution != null)) { return false; }
            var direct = HistoricalWorkerPayloadProjection.ProjectForFindings(input with
            {
                AnalysisMode = ExceptionAnalysisMode.Direct, SupportingProjects = default,
                Finding = new(WorkerDeclaredExecution.DirectEvidence, new(WorkerFindingContract.Profile, WorkerDeclaredPolicyMode.Direct))
            }, WorkerDeclaredExecution.DirectEvidence);
            if (!direct.Succeeded) { projection = direct; return false; }
            // The local Direct subquery must be exactly the original graph target, not a relabelled summary.
            var target = nodes.Single(node => Equals(node.Identity, graph.Target)).Input!;
            var local = direct.Value!.Payload;
            if (Json(local with { Context = local.Context! with { AnalysisMode = target.Context!.AnalysisMode, Finding = null } }) != Json(target))
            { return false; }
            operation = new(direct, summary, input.SupportingProjects.Select(project => project.Root).ToImmutableArray()); projection = summary; return true;
        }

        internal bool TryAdmitDirect(HistoricalWorkerCallResult? call, out HistoricalFindingFacts? facts)
        {
            facts = null;
            return Volatile.Read(ref consumed) == 0 && IsUnchanged()
                && HistoricalWorkerResponseValidation.HasValidatedExecution(call, Direct.Value!.Payload, directSnapshot, directCapture)
                && HistoricalWorkerResponseValidation.TryGetFindingFacts(call, out facts)
                && facts!.Mode == WorkerDeclaredPolicyMode.Direct && facts.Execution == WorkerDeclaredExecution.DirectEvidence;
        }

        internal bool TryComplete(HistoricalWorkerCallResult? directCall, HistoricalWorkerCallResult? summaryCall,
            out HistoricalFindingFacts? direct, out DeclaredFindingDocumentationFacts? document)
        {
            document = null;
            if (!TryAdmitDirect(directCall, out direct) || summaryCall?.Result == null
                || !HistoricalWorkerResponseValidation.HasValidatedExecution(summaryCall, Summary.Value!.Payload, summarySnapshot, summaryCapture)
                || Json(directCall!.Identity) != Json(summaryCall.Identity)
                || !HistoricalWorkerDeclaredDocumentationProjection.TryExtendSolutionTypes(Direct.Value!.Payload,
                    direct!.Documentation, summaryCall.Result, supportingPeRoots, out document)) { return false; }
            // Consume only a valid pair. A different operation/run, clone or raw revalidation cannot acquire this binding.
            return Interlocked.CompareExchange(ref consumed, 1, 0) == 0;
        }

        private bool IsUnchanged() => Json(Direct.Value!.Payload) == directSnapshot && Json(Summary.Value!.Payload) == summarySnapshot;
    }
}
