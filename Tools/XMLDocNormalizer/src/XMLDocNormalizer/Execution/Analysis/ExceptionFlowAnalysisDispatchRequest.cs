using Microsoft.CodeAnalysis.CSharp;
using XMLDocNormalizer.Execution.Semantic;
using XMLDocNormalizer.HistoricalWorker;

namespace XMLDocNormalizer.Execution.Analysis
{
    /// <summary>Closed Main-local evidence forms; neither a raw compiler string nor an engine choice is an input.</summary>
    internal abstract class ExceptionFlowAnalysisDispatchRequest
    {
        private ExceptionFlowAnalysisDispatchRequest() { }

        internal abstract ExceptionFlowAnalyzerSelectionProjectionInput ProjectionInput { get; }
        internal abstract ExceptionFlowAnalysisRoutingRequest Route(ExceptionFlowAnalyzerSelection selection);

        /// <summary>Native compiler evidence and the caller's existing sequential Current member/session.</summary>
        internal sealed class CurrentCompilation(CSharpCompilation compilation, CurrentExceptionFlowAnalysisInput analysis)
            : ExceptionFlowAnalysisDispatchRequest
        {
            internal override ExceptionFlowAnalyzerSelectionProjectionInput ProjectionInput => new(CurrentCompilation: compilation);
            internal override ExceptionFlowAnalysisRoutingRequest Route(ExceptionFlowAnalyzerSelection selection) => new(selection, Current: analysis);
        }

        /// <summary>Existing PDB evidence and one prepared execution payload; engine choice still belongs to B6.</summary>
        /// <remarks>Compiler provenance alone does not prove full external source/reference/options equivalence.</remarks>
        internal sealed class PortablePdb(ExternalCompilationProvenanceDescriptor provenance, ExecutionPayload payload)
            : ExceptionFlowAnalysisDispatchRequest
        {
            internal override ExceptionFlowAnalyzerSelectionProjectionInput ProjectionInput => new(PortablePdbProvenance: provenance);
            internal override ExceptionFlowAnalysisRoutingRequest Route(ExceptionFlowAnalyzerSelection selection)
                => payload?.Route(selection) ?? new(selection);
        }

        /// <summary>Closed execution payloads describe available data, not a requested engine or fallback.</summary>
        internal abstract class ExecutionPayload
        {
            private ExecutionPayload() { }
            internal abstract ExceptionFlowAnalysisRoutingRequest Route(ExceptionFlowAnalyzerSelection selection);

            internal sealed class CurrentSession(CurrentExceptionFlowAnalysisInput analysis) : ExecutionPayload
            {
                internal override ExceptionFlowAnalysisRoutingRequest Route(ExceptionFlowAnalyzerSelection selection) => new(selection, Current: analysis);
            }

            internal sealed class Worker(WorkerAnalysisInput analysis) : ExecutionPayload
            {
                internal override ExceptionFlowAnalysisRoutingRequest Route(ExceptionFlowAnalyzerSelection selection) => new(selection, Historical: analysis);
            }
        }
    }
}
