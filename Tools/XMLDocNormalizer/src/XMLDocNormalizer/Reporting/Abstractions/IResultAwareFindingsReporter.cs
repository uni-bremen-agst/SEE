using XMLDocNormalizer.Models;

namespace XMLDocNormalizer.Reporting.Abstractions
{
    /// <summary>
    /// Extends a findings reporter with access to the aggregated run result.
    /// </summary>
    internal interface IResultAwareFindingsReporter : IFindingsReporter
    {
        /// <summary>
        /// Finalizes reporting using the aggregated <paramref name="result"/>.
        /// </summary>
        /// <param name="result">The aggregated run result.</param>
        void Complete(RunResult result);
    }
}