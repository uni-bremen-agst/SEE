using Microsoft.CodeAnalysis;

namespace XMLDocNormalizer.Checks.Infrastructure.Exception.Flow
{
    /// <summary>
    /// Receives statically discovered summary targets without requiring a
    /// concrete summary graph or a pending-context queue.
    /// </summary>
    internal interface IExceptionFlowSummaryTargetSink
    {
        /// <summary>Records a target in the receiver's own construction domain.</summary>
        /// <param name="symbol">The source-bound target, including its construction.</param>
        /// <param name="context">The call-site context for the target.</param>
        /// <returns>The key used by the local call edge.</returns>
        ExceptionFlowCallableKey RegisterTarget(ISymbol symbol, ExceptionFlowCallContext context);

        /// <summary>Observes local coverage before enclosing catch suppression.</summary>
        /// <param name="fragment">The local, not yet suppressed fragment.</param>
        void ObserveLocalFragment(ExceptionFlowSummaryFragment fragment);
    }
}
